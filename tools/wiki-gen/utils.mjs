// Foundation utilities: path/fs helpers, BBCode, YAML, badges, images

import { readFileSync, mkdirSync, existsSync, copyFileSync, readdirSync } from 'fs'
import { join, basename } from 'path'

// GitHub Pages base path — change if repo is renamed/moved
export const BASE = '/Sts2-YuWanCard'
export function basePath(p) { return BASE + p }

export function pascalToSnake(name) {
  return name
    .replace(/([a-z])([A-Z])/g, '$1_$2')
    .replace(/([A-Z])([A-Z][a-z])/g, '$1_$2')
    .toLowerCase()
}

export function readFile(path) {
  try { return readFileSync(path, 'utf-8') } catch { return null }
}

export function readJson(path) {
  try { return JSON.parse(readFileSync(path, 'utf-8')) } catch { return null }
}

export function ensureDir(dir) {
  if (!existsSync(dir)) mkdirSync(dir, { recursive: true })
}

export function collectCsFiles(dir) {
  if (!existsSync(dir)) return []
  const results = []
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name)
    if (entry.isDirectory()) results.push(...collectCsFiles(full))
    else if (entry.name.endsWith('.cs') && !entry.name.endsWith('.cs.uid')) results.push(full)
  }
  return results
}

// ---- BBCode Parser ----

export function bbcodeToHtml(text) {
  if (!text) return ''
  let result = text.replace(/\\n/g, '\n').replace(/NL/g, '\n')

  const tags = ['gold', 'blue', 'red', 'green', 'purple', 'jitter', 'sine', 'i', 'b', 'thinky_dots']
  for (const tag of tags) {
    const open = new RegExp(`\\[${tag}\\]`, 'gi')
    const close = new RegExp(`\\[/${tag}\\]`, 'gi')
    let cssClass = `text-${tag}`
    if (tag === 'jitter') cssClass = 'animate-jitter'
    if (tag === 'sine') cssClass = 'animate-sine'
    if (tag === 'thinky_dots') cssClass = 'animate-thinky'
    result = result.replace(open, `<span class="${cssClass}">`).replace(close, '</span>')
  }

  result = result.replace(/\[font_size=(\d+)\](.*?)\[\/font_size\]/gi,
    (_, size, text) => `<span style="font-size:${size}px">${text}</span>`)

  return result
}

export function stripBBCode(text) {
  if (!text) return ''
  return text.replace(/\[.*?\]/g, '')
}

// ---- Variable Resolution ----

export function resolveVariables(description, vars) {
  if (!description || !vars) return description || ''

  let result = description

  result = result.replace(/\{(\w+):(\w+)\(\)\}/g, (match, name, format) => {
    const v = vars.find(v => v.name === name)
    if (!v) return match
    if (v.upgrade && v.upgrade > 0) {
      return `${v.base} <span class="stat-upgrade">(${v.base + v.upgrade})</span>`
    }
    return String(v.base)
  })

  result = result.replace(/\{(\w+)\}/g, (match, name) => {
    const v = vars.find(v => v.name === name)
    if (!v) return match
    return String(v.base)
  })

  result = result.replace(/\{IfUpgraded:show:(.*?)\|(.*?)\}/g, (_, upgraded, normal) => {
    return `<span class="if-upgraded">${upgraded} / ${normal}</span>`
  })

  return result
}

// Resolve description for list previews — base values only, no HTML, plain text
export function resolveListDesc(description, variables) {
  if (!description) return ''

  // Build lookup: name → base, also strip "Power" suffix for matching
  const lookup = {}
  for (const v of (variables || [])) {
    lookup[v.name] = String(v.base)
    const stripped = v.name.replace(/Power$/, '')
    if (stripped !== v.name && !lookup[stripped]) lookup[stripped] = String(v.base)
  }

  const firstVar = Object.keys(lookup)[0] ? lookup[Object.keys(lookup)[0]] : undefined

  function getVal(name) {
    if (lookup[name] !== undefined) return lookup[name]
    if (name.endsWith('Power')) return lookup[name.replace(/Power$/, '')]
    // {Amount} is a generic reference to the primary/only variable
    if (name === 'Amount' && firstVar !== undefined) return firstVar
    return undefined
  }

  let result = description

  // Resolve {Name:diff()} → base value only
  result = result.replace(/\{(\w+):diff\(\)\}/g, (_, name) => {
    const v = getVal(name)
    return v !== undefined ? v : `{${name}:diff()}`
  })

  // Resolve {Name:format()} → base value only (diff already handled above)
  result = result.replace(/\{(\w+):(\w+)\(\)\}/g, (_, name, format) => {
    const v = getVal(name)
    return v !== undefined ? v : `{${name}:${format}()}`
  })

  // Resolve {Name} patterns
  result = result.replace(/\{(\w+)\}/g, (_, name) => {
    const v = getVal(name)
    return v !== undefined ? v : `{${name}}`
  })

  // {IfUpgraded:show:A|B} → show non-upgraded (B), supports empty sides
  result = result.replace(/\{IfUpgraded:show:(.*?)\|(.*?)\}/g, (_, _upgraded, normal) => normal)

  // Strip BBCode
  result = result.replace(/\[.*?\]/g, '')

  return result
}

// ---- Image Matcher ----

const IMAGE_CATEGORY_MAP = {
  card: 'card_portraits', relic: 'relics', power: 'powers',
  enchantment: 'enchantments', orb: 'orbs', monster: 'monsters',
  event: 'events', ancient: 'ancients', modifier: 'modifiers',
  character: 'characters'
}

export function findImage(entityId, entityType, imgRoot) {
  const category = IMAGE_CATEGORY_MAP[entityType]
  if (!category) return null

  const imgDir = join(imgRoot, category)
  if (!existsSync(imgDir)) return null

  const exactPath = join(imgDir, `${entityId}.png`)
  if (existsSync(exactPath)) return exactPath

  const altNames = [
    entityId.replace(/_power$/, ''),
    entityId.replace(/_select$/, ''),
    entityId.replace(/_orb$/, ''),
  ]

  for (const file of readdirSync(imgDir)) {
    if (file.endsWith('.png.import')) continue
    const base = basename(file, '.png')
    if (base === entityId) return join(imgDir, file)
    for (const alt of altNames) {
      if (base === alt) return join(imgDir, file)
    }
  }

  return null
}

const COPY_CATEGORY_MAP = {
  card: 'cards', relic: 'relics', power: 'powers',
  enchantment: 'enchantments', orb: 'orbs', monster: 'monsters',
  event: 'events', ancient: 'ancients', modifier: 'modifiers',
  character: 'characters'
}

export function copyImages(entities, imgRoot, publicImgDir) {
  ensureDir(publicImgDir)
  const copied = new Set()

  for (const entity of entities) {
    const category = COPY_CATEGORY_MAP[entity.type]
    if (!category) continue
    const targetDir = join(publicImgDir, category)
    ensureDir(targetDir)

    const imgPath = findImage(entity.id, entity.type, imgRoot)
    if (imgPath) {
      const target = join(targetDir, basename(imgPath))
      if (!copied.has(target)) {
        copyFileSync(imgPath, target)
        copied.add(target)
        entity.image = `/images/${category}/${basename(imgPath)}`
      }
    }
  }
}

// ---- YAML / JS string helpers ----

export function yamlValue(val) {
  if (typeof val === 'number' || typeof val === 'boolean') return String(val)
  if (!val) return "''"
  if (/[:{}[\]&*?|>!%@`]|^\s*-|\n/.test(val)) {
    return `"${val.replace(/"/g, '\\"')}"`
  }
  return val
}

export function jsString(s) {
  return "'" + (s || '').replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/\n/g, '\\n') + "'"
}

// ---- Badges ----

export function rarityBadge(rarity) {
  const cls = (rarity || 'basic').toLowerCase()
  return `<span class="rarity-badge rarity-${cls}">${rarity || 'Basic'}</span>`
}

export function typeBadge(cardType) {
  const cls = (cardType || '').toLowerCase()
  return `<span class="rarity-badge type-${cls}">${cardType}</span>`
}

// ---- Category display names ----

export const CATEGORY_NAMES = {
  zhs: {
    pig: '猪猪', colorless: '无色', token: '衍生', quest: '任务',
    event: '事件', regent: '储君'
  },
  eng: {
    pig: 'Pig', colorless: 'Colorless', token: 'Token', quest: 'Quest',
    event: 'Event', regent: 'Regent'
  }
}

// ---- JSON-LD SEO helper ----

export function jsonldEntity(entity, loc, lang) {
  const title = loc.title || entity.className
  const descSource = entity.type === 'orb'
    ? (loc.smartDescription || loc.description || '')
    : entity.type === 'ancient'
      ? (loc.description || loc.initialDescription || loc.pageDescriptions?.[0]?.text || '')
      : (loc.description || loc.smartDescription || '')
  const desc = stripBBCode(descSource).substring(0, 300)
  const typeMap = {
    card: 'Card', relic: 'Relic', power: 'Power',
    enchantment: 'Enchantment', orb: 'Orb', monster: 'Monster',
    event: 'Event', ancient: 'Ancient', modifier: 'Modifier', character: 'Character'
  }
  const entityType = typeMap[entity.type] || entity.type
  const url = `${BASE}/${lang}/${entity.type}s/${entity.id}`
  const siteUrl = `https://yuwan886.github.io${url}`

  return JSON.stringify({
    '@context': 'https://schema.org',
    '@type': 'WebPage',
    name: title,
    description: desc,
    url: siteUrl,
    ...(entity.image ? { image: `https://yuwan886.github.io${BASE}${entity.image}` } : {}),
    about: {
      '@type': 'Thing',
      name: title,
      description: `A ${entityType} in the YuWanCard mod for Slay the Spire 2`
    },
    inLanguage: lang === 'zhs' ? 'zh-CN' : 'en-US',
    isPartOf: {
      '@type': 'WebSite',
      name: 'YuWanCard Wiki',
      url: 'https://yuwan886.github.io'
    }
  })
}
