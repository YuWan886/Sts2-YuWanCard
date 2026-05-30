// C# source parsers — one function per entity type

import { basename, join, relative, dirname } from 'path'
import { fileURLToPath } from 'url'
import { readFile, pascalToSnake, collectCsFiles } from './utils.mjs'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '../..')
const SRC_ROOT = join(ROOT, 'YuWanCardCode')
const CARDS_ROOT = join(SRC_ROOT, 'Cards')

let cardClassFileMap = null
let classFileMap = null

function classId(filepath) {
  return pascalToSnake(basename(filepath, '.cs'))
}

function buildCardClassFileMap() {
  if (cardClassFileMap) return cardClassFileMap
  cardClassFileMap = new Map()
  for (const filepath of collectCsFiles(CARDS_ROOT)) {
    cardClassFileMap.set(basename(filepath, '.cs'), filepath)
  }
  return cardClassFileMap
}

function findCardFileByClass(className) {
  return buildCardClassFileMap().get(className) || null
}

function buildClassFileMap() {
  if (classFileMap) return classFileMap
  classFileMap = new Map()
  for (const filepath of collectCsFiles(SRC_ROOT)) {
    classFileMap.set(basename(filepath, '.cs'), filepath)
  }
  return classFileMap
}

function findClassFileByClass(className) {
  return buildClassFileMap().get(className) || null
}

// ---- Variable extractor helpers ----

// Build constant lookup from "const int/float/double Name = Value;"
function buildConstants(content) {
  const constants = {}
  for (const m of content.matchAll(/const\s+(int|float|double)\s+(\w+)\s*=\s*(-?[\d.]+)m?f?\s*;/g))
    constants[m[2]] = parseFloat(m[3])
  return constants
}

// Build map from inline DynamicVar subclass to its real name+base
// Matches: class FooVar(...) : DynamicVar("Name", N)
function buildClassVarMap(content) {
  const map = {}
  for (const m of content.matchAll(/class\s+(\w+).*?:\s*DynamicVar\s*\(\s*"(\w+)"\s*,\s*(-?[\d.]+)m?f?\s*\)/g)) {
    map[m[1]] = { name: m[2], base: parseFloat(m[3]) }
  }
  return map
}

// Parse CanonicalVars => [new Xxx(...), ...] — used by relics, powers, enchantments, etc.
function parseCanonicalVars(content, constants) {
  const vars = []
  const cvMatch = content.match(/CanonicalVars\s*=>\s*\[([\s\S]*?)\]\s*;/)
  if (!cvMatch) return vars

  const body = cvMatch[1]
  const classVarMap = buildClassVarMap(content)

  // Match all new expressions, including generic types like PowerVar<StrengthPower>
  for (const m of body.matchAll(/new\s+(\w+)(?:<(\w+)>)?\s*\(([^)]*)\)/g)) {
    const cls = m[1]
    const genericArg = m[2]
    const argsStr = m[3]
    let name = null
    let base = 0

    // Check if this is an inline DynamicVar subclass with known name
    if (classVarMap[cls]) {
      name = classVarMap[cls].name
      base = classVarMap[cls].base
    }
    // DynamicVar("Name", N)
    else if (cls === 'DynamicVar') {
      const strMatch = argsStr.match(/"(\w+)"/)
      const numMatch = argsStr.match(/(-?[\d.]+)m?f?/)
      if (strMatch) {
        name = strMatch[1]
        if (numMatch) base = parseFloat(numMatch[1])
      }
    }
    // PowerVar<SomethingPower>(N) → name = "Something"
    else if (cls === 'PowerVar' && genericArg) {
      name = genericArg.replace(/Power$/, '')
      const numMatch = argsStr.match(/(-?[\d.]+)m?f?/)
      if (numMatch) base = parseFloat(numMatch[1])
    }
    // CardsVar(N) → name = "Cards"
    else if (cls === 'CardsVar' || cls === 'CardsPerEnergyVar') {
      name = 'Cards'
      const numMatch = argsStr.match(/(-?[\d.]+)m?f?/)
      if (numMatch) base = parseFloat(numMatch[1])
    }
    // FooVar(N) → name = "Foo" (strip Var), base from first number
    // FooVar(this) → name = "Foo", base = 0 (dynamic)
    else {
      name = cls.replace(/Var$/, '')
      const numMatch = argsStr.match(/(-?[\d.]+)m?f?/)
      if (numMatch) base = parseFloat(numMatch[1])
      // Check if first arg is a constant name
      if (!numMatch) {
        const firstArg = argsStr.split(',')[0]?.trim()
        if (firstArg && constants[firstArg] !== undefined) base = constants[firstArg]
      }
    }

    if (name) vars.push({ name, base, upgrade: 0 })
  }

  return vars
}

function extractVariables(content) {
  const variables = []

  function addVar(name, base, upgrade) {
    const existing = variables.find(v => v.name === name)
    if (existing) {
      if (upgrade !== undefined) existing.upgrade = upgrade
      if (base !== undefined) existing.base = base
    } else {
      variables.push({ name, base: base || 0, upgrade: upgrade || 0 })
    }
  }

  const constants = buildConstants(content)

  for (const m of content.matchAll(/WithDamage\s*\(\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\)/g))
    addVar('Damage', parseInt(m[1]), m[2] ? parseInt(m[2]) : 0)
  for (const m of content.matchAll(/WithBlock\s*\(\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\)/g))
    addVar('Block', parseInt(m[1]), m[2] ? parseInt(m[2]) : 0)
  for (const m of content.matchAll(/WithEnergy\s*\(\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\)/g))
    addVar('Energy', parseInt(m[1]), m[2] ? parseInt(m[2]) : 0)
  for (const m of content.matchAll(/WithHeal\s*\(\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\)/g))
    addVar('Heal', parseInt(m[1]), m[2] ? parseInt(m[2]) : 0)
  for (const m of content.matchAll(/WithCards\s*\(\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\)/g))
    addVar('Cards', parseInt(m[1]), m[2] ? parseInt(m[2]) : 0)
  for (const m of content.matchAll(/WithPower\s*<\s*(\w+)\s*>\s*\(\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\)/g))
    addVar(m[1].replace(/Power$/, ''), parseInt(m[2]), m[3] ? parseInt(m[3]) : 0)
  for (const m of content.matchAll(/WithVar\s*\(\s*"(\w+)"\s*,\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\s*\)/g))
    addVar(m[1], parseInt(m[2]), m[3] ? parseInt(m[3]) : 0)

  // new DynamicVar("Name", value) — direct DynamicVar construction
  for (const m of content.matchAll(/new\s+DynamicVar\s*\(\s*"(\w+)"\s*,\s*(-?[\d.]+)m?f?\s*\)/g))
    addVar(m[1], parseFloat(m[2]), 0)

  // WithVars(new ClassNameVar(...)) — custom DynamicVar subclasses (multi-line aware)
  const withVarsBlock = content.match(/WithVars\s*\(\s*([\s\S]*?)\)\s*;/)
  if (withVarsBlock) {
    const body = withVarsBlock[1]
    for (const m of body.matchAll(/new\s+(\w+)\s*\(([^)]*)\)/g)) {
      const cls = m[1]
      const argsStr = m[2]
      let name
      if (cls === 'IntVar') {
        const strMatch = argsStr.match(/"(\w+)"/)
        if (strMatch) name = strMatch[1]
      } else if (cls === 'DynamicVar') {
        // Already handled above — skip
        continue
      } else {
        name = cls.replace(/Var$/, '')
      }
      if (!name) continue
      const args = argsStr.split(',').map(s => s.trim()).filter(Boolean)
      // No-arg constructor → dynamic variable, add with base 0 so {Name} resolves
      if (args.length === 0) {
        addVar(name, 0, 0)
        continue
      }
      const firstArg = args[0]
      const numMatch = firstArg.match(/^(-?[\d.]+)m?f?$/)
      let base = 0
      if (numMatch) {
        base = parseFloat(numMatch[1])
      } else if (constants[firstArg] !== undefined) {
        base = constants[firstArg]
      } else {
        continue
      }
      const lastArg = args[args.length - 1]
      let upgrade = 0
      if (constants[lastArg] !== undefined && args.length >= 3) {
        upgrade = constants[lastArg] - base
      }
      addVar(name, base, upgrade > 0 ? upgrade : 0)
    }
  }

  // DynamicVars["Name"].UpgradeValueBy(N) in constructor (outside OnUpgrade)
  for (const m of content.matchAll(/DynamicVars\["(\w+)"\]\.UpgradeValueBy\s*\(\s*(-?\d+)m?\s*\)/g))
    addVar(m[1], undefined, parseInt(m[2]))

  const upgradeBlock = content.match(/protected override void OnUpgrade\s*\(\)\s*\{([^}]*)\}/s)
  if (upgradeBlock) {
    const body = upgradeBlock[1]
    for (const m of body.matchAll(/DynamicVars\.(\w+)\.UpgradeValueBy\s*\(\s*(-?\d+)m?\s*\)/g))
      addVar(m[1], undefined, parseInt(m[2]))
    for (const m of body.matchAll(/DynamicVars\["(\w+)"\]\.UpgradeValueBy\s*\(\s*(-?\d+)m?\s*\)/g))
      addVar(m[1], undefined, parseInt(m[2]))
  }

  return variables
}

function extractTemporaryPowerWrapperVars(content) {
  const wrapperMatch = content.match(
    /class\s+(\w+)\s*:\s*YuWanTemporaryPowerModelWrapper\s*<\s*(\w+)\s*,\s*(\w+)\s*>/
  )
  if (!wrapperMatch) return []

  const wrapperClass = wrapperMatch[1]
  const originCardClass = wrapperMatch[2]
  const internalPowerClass = wrapperMatch[3]
  const originCardFile = findCardFileByClass(originCardClass)
  if (!originCardFile) return []

  const originCardContent = readFile(originCardFile)
  if (!originCardContent) return []

  const originVars = extractVariables(originCardContent)
  if (!originVars.length) return []

  const preferredNames = [
    wrapperClass.replace(/Power$/, ''),
    wrapperClass,
    internalPowerClass.replace(/Power$/, ''),
    internalPowerClass
  ]

  for (const preferredName of preferredNames) {
    const found = originVars.find(v => v.name === preferredName)
    if (found) return [{ ...found }]
  }

  // Fallback: keep at least one variable so {Amount} can resolve.
  return [{ ...originVars[0] }]
}

function stripTypeArgs(typeName) {
  return typeName.replace(/<.*>/g, '').trim()
}

function parseBaseClassNames(content) {
  const classMatch = content.match(/class\s+\w+(?:<[^>]+>)?\s*:\s*([^{\n]+)/)
  if (!classMatch) return []

  const inheritList = classMatch[1]
    .split(',')
    .map(s => stripTypeArgs(s.split('.').pop() || '').trim())
    .filter(Boolean)
  return inheritList
}

function parseCanonicalVarsFromInheritance(content, visited = new Set()) {
  const baseNames = parseBaseClassNames(content)
  for (const baseName of baseNames) {
    if (visited.has(baseName)) continue
    visited.add(baseName)

    const baseFile = findClassFileByClass(baseName)
    if (!baseFile) continue

    const baseContent = readFile(baseFile)
    if (!baseContent) continue

    const baseConstants = buildConstants(baseContent)
    const baseVars = parseCanonicalVars(baseContent, baseConstants)
    if (baseVars.length > 0) return baseVars

    const inherited = parseCanonicalVarsFromInheritance(baseContent, visited)
    if (inherited.length > 0) return inherited
  }
  return []
}

function extractTipReferences(content) {
  const refs = []
  const seen = new Set()

  function addRef(className) {
    if (!className || seen.has(className)) return
    seen.add(className)
    refs.push(className)
  }

  for (const m of content.matchAll(/WithTip\s*\(\s*typeof\s*\(\s*(\w+)\s*\)\s*\)/g))
    addRef(m[1])

  for (const m of content.matchAll(/HoverTipFactory\.From\w+\s*<\s*(\w+)\s*>/g))
    addRef(m[1])

  return refs
}

// ---- Card ----

export function parseCardFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null

  const className = basename(filepath, '.cs')
  const entityId = pascalToSnake(className)

  // Derive category from directory structure
  const relPath = relative(join(SRC_ROOT, 'Cards'), filepath).replace(/\\/g, '/')
  const category = (relPath.split('/')[0] || '').toLowerCase()

  let pool = 'shared'
  const poolMatch = content.match(/\[Pool\(typeof\((\w+)\)\)\]/)
  if (poolMatch) {
    const pn = poolMatch[1].toLowerCase()
    if (pn.includes('pig')) pool = 'pig'
    else if (pn.includes('shared')) pool = 'shared'
    else if (pn.includes('colorless')) pool = 'colorless'
    else pool = pn.replace('cardpool', '').replace('pool', '')
  }

  const costMatch = content.match(/baseCost:\s*(-?\d+)/)
  const typeMatch = content.match(/type:\s*CardType\.(\w+)/)
  const rarityMatch = content.match(/rarity:\s*CardRarity\.(\w+)/)
  const targetMatch = content.match(/target:\s*TargetType\.(\w+)/)
  const costUpgradeMatch = content.match(/WithCostUpgradeBy\s*\(\s*(-?\d+)\s*\)/)
  const showInLibraryMatch = content.match(/showInCardLibrary:\s*(false)/)

  const variables = extractVariables(content)

  const keywords = []
  for (const m of content.matchAll(/CardKeyword\.(\w+)/g)) keywords.push(m[1])
  const kwMatch = content.match(/WithKeywords\s*\(([^)]+)\)/)
  if (kwMatch) {
    for (const m of kwMatch[1].matchAll(/CardKeyword\.(\w+)/g))
      if (!keywords.includes(m[1])) keywords.push(m[1])
  }

  const tags = []
  for (const m of content.matchAll(/CardTag\.(\w+)/g)) tags.push(m[1])
  const tipRefs = extractTipReferences(content)

  return {
    id: entityId, className, type: 'card', category, pool,
    cost: costMatch ? parseInt(costMatch[1]) : 0,
    costUpgrade: costUpgradeMatch ? parseInt(costUpgradeMatch[1]) : 0,
    cardType: typeMatch ? typeMatch[1] : null,
    rarity: rarityMatch ? rarityMatch[1] : null,
    target: targetMatch ? targetMatch[1] : null,
    variables, keywords, tags, tipRefs,
    hiddenFromLibrary: !!showInLibraryMatch
  }
}

// ---- Relic ----

export function parseRelicFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null

  // Skip abstract base classes
  if (content.match(/abstract\s+class\s+\w+/)) return null

  const poolMatch = content.match(/\[Pool\(typeof\((\w+)\)\)\]/)
  const pool = poolMatch ? poolMatch[1].toLowerCase().replace('relicpool', '').replace('pool', '') : 'shared'
  const rarityMatch = content.match(/Rarity\s*(?:=>|{ get; set; })\s*(?:=>)?\s*RelicRarity\.(\w+)/)
    || content.match(/RelicRarity\.(\w+)/)

  const constants = buildConstants(content)
  const variables = parseCanonicalVars(content, constants)

  return {
    id: classId(filepath), className: basename(filepath, '.cs'),
    type: 'relic', pool, rarity: rarityMatch ? rarityMatch[1] : null, variables
  }
}

// ---- Power ----

export function parsePowerFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null

  const typeMatch = content.match(/Type\s*=>\s*PowerType\.(\w+)/)
  const stackMatch = content.match(/StackType\s*=>\s*PowerStackType\.(\w+)/)
  const constants = buildConstants(content)
  let variables = parseCanonicalVars(content, constants)
  if (variables.length === 0) {
    const inheritedVars = parseCanonicalVarsFromInheritance(content)
    if (inheritedVars.length) variables = inheritedVars
  }
  if (variables.length === 0) {
    const wrapperVars = extractTemporaryPowerWrapperVars(content)
    if (wrapperVars.length) variables = wrapperVars
  }

  return {
    id: classId(filepath), className: basename(filepath, '.cs'),
    type: 'power', powerType: typeMatch ? typeMatch[1] : null,
    stackType: stackMatch ? stackMatch[1] : null, variables
  }
}

// ---- Simple entity parsers ----

export function parseEnchantmentFile(filepath) {
  const content = readFile(filepath)
  const constants = content ? buildConstants(content) : {}
  const variables = content ? parseCanonicalVars(content, constants) : []
  return { id: classId(filepath), className: basename(filepath, '.cs'), type: 'enchantment', variables }
}

export function parseOrbFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null
  const constants = buildConstants(content)
  const variables = parseCanonicalVars(content, constants)
  const pm = content.match(/PassiveVal\s*=>\s*(-?\d+)/)
  const em = content.match(/EvokeVal\s*=>\s*(-?\d+)/)
  if (pm) variables.push({ name: 'Passive', base: parseInt(pm[1]), upgrade: 0 })
  if (em) variables.push({ name: 'Evoke', base: parseInt(em[1]), upgrade: 0 })
  return { id: classId(filepath), className: basename(filepath, '.cs'), type: 'orb', variables }
}

export function parseMonsterFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null
  const minHp = content.match(/MinInitialHp\s*=>\s*(-?\d+)/)
  const maxHp = content.match(/MaxInitialHp\s*=>\s*(-?\d+)/)
  return {
    id: classId(filepath), className: basename(filepath, '.cs'), type: 'monster',
    minHp: minHp ? parseInt(minHp[1]) : null, maxHp: maxHp ? parseInt(maxHp[1]) : null
  }
}

export function parseEventFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null
  const constants = buildConstants(content)
  const variables = parseCanonicalVars(content, constants)
  const acts = []
  for (const m of content.matchAll(/ActId\.(\w+)/g)) acts.push(m[1])
  return { id: classId(filepath), className: basename(filepath, '.cs'), type: 'event', acts, variables }
}

export function parseAncientFile(filepath) {
  return { id: classId(filepath), className: basename(filepath, '.cs'), type: 'ancient' }
}

export function parseModifierFile(filepath) {
  const className = basename(filepath, '.cs')
  return { id: pascalToSnake(className.replace(/Modifier$/, '')), className, type: 'modifier' }
}

// ---- Hextech Integration Relic ----

export function parseHextechRelicFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null

  const className = basename(filepath, '.cs')

  // Skip abstract base classes
  if (content.match(/abstract\s+class\s+\w+/)) return null

  const entityId = pascalToSnake(className)

  // Determine pool from base class
  let pool = 'hextech_pig'
  if (content.includes('HextechSharedRuneBase')) pool = 'shared'
  else if (content.includes('HextechPigRuneBase')) pool = 'hextech_pig'
  else if (content.includes('HextechPigForgeBase')) pool = 'hextech_pig'

  // Extract HextechRarity (from either HextechRuneRarity or HextechForgeRarity)
  const rarityMatch = content.match(/HextechRuneRarity\.(\w+)/) || content.match(/HextechForgeRarity\.(\w+)/)
  let rarity = rarityMatch ? rarityMatch[1] : null // Silver, Gold, Prismatic

  const constants = buildConstants(content)
  const variables = parseCanonicalVars(content, constants)

  return {
    id: entityId, className, type: 'relic', pool, rarity, variables,
    isHextech: true
  }
}

// ---- Character ----

export function parseCharacterFile(filepath) {
  const content = readFile(filepath)
  if (!content) return null
  const hpMatch = content.match(/StartingHp\s*=>\s*(\d+)/)
  const goldMatch = content.match(/StartingGold\s*=>\s*(\d+)/)
  return {
    id: classId(filepath), className: basename(filepath, '.cs'), type: 'character',
    startingHp: hpMatch ? parseInt(hpMatch[1]) : null,
    startingGold: goldMatch ? parseInt(goldMatch[1]) : null
  }
}
