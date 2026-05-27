// YuWanCard Wiki Generator
// Parses C# source files and localization JSONs, generates VitePress markdown pages,
// copies image assets, and builds search indexes.

import { readFileSync, writeFileSync } from 'fs'
import { join, basename, dirname } from 'path'
import { fileURLToPath } from 'url'
import {
  collectCsFiles, ensureDir, copyImages, findImage
} from './wiki-gen/utils.mjs'
import {
  parseCardFile, parseRelicFile, parsePowerFile,
  parseEnchantmentFile, parseOrbFile, parseMonsterFile,
  parseEventFile, parseAncientFile, parseModifierFile, parseCharacterFile,
  parseHextechRelicFile
} from './wiki-gen/parsers.mjs'
import {
  loadLocalization, buildLocaleLookup
} from './wiki-gen/localization.mjs'
import {
  generateCardMd, generateRelicMd, generatePowerMd, generateSimpleMd,
  generateCardList, generateRelicList, generateSimpleList,
  generateSearchIndex, generateHomepage,
  generateRootRedirect
} from './wiki-gen/generators.mjs'

// ---- Paths ----

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const SRC_ROOT = join(ROOT, 'YuWanCardCode')
const IMG_ROOT = join(ROOT, 'YuWanCard', 'images')
const WIKI_ROOT = join(ROOT, 'wiki')
const OUTPUT_ZHS = join(WIKI_ROOT, 'zhs')
const OUTPUT_ENG = join(WIKI_ROOT, 'eng')
const PUBLIC_IMG = join(WIKI_ROOT, 'public', 'images')

// ---- Pipeline steps ----

function parseAllEntities() {
  const entities = []

  const cardDirs = ['Cards/Pig', 'Cards/Colorless', 'Cards/Token', 'Cards/Quest', 'Cards/Event', 'Cards/Regent']
  for (const dir of cardDirs) {
    const files = collectCsFiles(join(SRC_ROOT, dir))
    console.log(`  Cards/${basename(dir)}: ${files.length} files`)
    for (const f of files) {
      const card = parseCardFile(f)
      if (card) entities.push(card)
    }
  }

  const parsers = [
    { dir: 'Relics', fn: parseRelicFile },
    { dir: 'Powers', fn: parsePowerFile },
    { dir: 'Enchantments', fn: parseEnchantmentFile },
    { dir: 'Orbs', fn: parseOrbFile },
    { dir: 'Monsters', fn: parseMonsterFile },
    { dir: 'Events', fn: parseEventFile },
    { dir: 'Ancients', fn: parseAncientFile },
    { dir: 'Modifiers', fn: parseModifierFile },
  ]

  for (const { dir, fn } of parsers) {
    const files = collectCsFiles(join(SRC_ROOT, dir))
    console.log(`  ${dir}: ${files.length} files`)
    for (const f of files) {
      const entity = fn(f)
      if (entity) entities.push(entity)
    }
  }

  // Hextech integration relics
  const hextechRelicDirs = ['Integrations/Hextech/Relics']
  for (const dir of hextechRelicDirs) {
    const files = collectCsFiles(join(SRC_ROOT, dir))
    console.log(`  ${dir}: ${files.length} files`)
    for (const f of files) {
      const entity = parseHextechRelicFile(f)
      if (entity) entities.push(entity)
    }
  }

  // Hextech integration powers
  const hextechPowerDirs = ['Integrations/Hextech/Powers']
  for (const dir of hextechPowerDirs) {
    const files = collectCsFiles(join(SRC_ROOT, dir))
    console.log(`  ${dir}: ${files.length} files`)
    for (const f of files) {
      const entity = parsePowerFile(f)
      if (entity) {
        entity.isHextech = true
        entities.push(entity)
      }
    }
  }

  // Characters (filter out pool/config files)
  const charFiles = collectCsFiles(join(SRC_ROOT, 'Characters'))
  console.log(`  Characters: ${charFiles.length} files`)
  for (const f of charFiles) {
    const name = basename(f)
    if (name.includes('Pool') || name.includes('AllCards') || name.includes('Potion')) continue
    const ch = parseCharacterFile(f)
    if (ch) entities.push(ch)
  }

  return entities
}

function resolveCardTipTargets(entities) {
  const classToEntity = new Map()
  for (const entity of entities) {
    if (!entity.className) continue
    if (!classToEntity.has(entity.className)) classToEntity.set(entity.className, entity)
  }

  for (const entity of entities) {
    if (entity.type !== 'card') continue
    const refs = entity.tipRefs || []
    const targets = []
    const seen = new Set()

    for (const className of refs) {
      if (seen.has(className)) continue
      seen.add(className)

      const target = classToEntity.get(className)
      if (target) {
        targets.push({ className, id: target.id, type: target.type })
      } else {
        targets.push({ className })
      }
    }

    entity.tipTargets = targets
  }
}

function generatePages(entities, localeLookup) {
  const TYPE_CONFIG = [
    { type: 'card', single: 'card', multi: 'cards', genDetail: generateCardMd, genList: 'cardList', typeNames: { zhs: '卡牌', eng: 'Cards' } },
    { type: 'relic', single: 'relic', multi: 'relics', genDetail: generateRelicMd, genList: 'relicList', typeNames: { zhs: '遗物', eng: 'Relics' } },
    { type: 'power', single: 'power', multi: 'powers', genDetail: generatePowerMd, genList: 'simpleList', typeNames: { zhs: '能力', eng: 'Powers' } },
    { type: 'enchantment', single: 'enchantment', multi: 'enchantments', genDetail: generateSimpleMd, genList: 'simpleList', typeNames: { zhs: '附魔', eng: 'Enchantments' } },
    { type: 'orb', single: 'orb', multi: 'orbs', genDetail: generateSimpleMd, genList: 'simpleList', typeNames: { zhs: '充能球', eng: 'Orbs' } },
    { type: 'monster', single: 'monster', multi: 'monsters', genDetail: generateSimpleMd, genList: 'simpleList', typeNames: { zhs: '怪物', eng: 'Monsters' } },
    { type: 'event', single: 'event', multi: 'events', genDetail: generateSimpleMd, genList: 'simpleList', typeNames: { zhs: '事件', eng: 'Events' } },
    { type: 'ancient', single: 'ancient', multi: 'ancients', genDetail: generateSimpleMd, genList: 'simpleList', typeNames: { zhs: '先古之民', eng: 'Ancients' } },
    { type: 'modifier', single: 'modifier', multi: 'modifiers', genDetail: generateSimpleMd, genList: 'simpleList', typeNames: { zhs: '修改器', eng: 'Modifiers' } },
    { type: 'character', single: 'character', multi: 'characters', genDetail: generateSimpleMd, genList: 'simpleList', typeNames: { zhs: '角色', eng: 'Characters' } },
  ]

  for (const cfg of TYPE_CONFIG) {
    const typeEntities = entities.filter(e => e.type === cfg.type)
    console.log(`  ${cfg.typeNames.eng}: ${typeEntities.length} detail pages + 2 list pages`)

    for (const lang of ['zhs', 'eng']) {
      const outDir = lang === 'zhs' ? join(OUTPUT_ZHS, cfg.multi) : join(OUTPUT_ENG, cfg.multi)
      ensureDir(outDir)

      // List page
      let listContent
      if (cfg.genList === 'cardList') {
        listContent = generateCardList(entities, localeLookup, lang)
      } else if (cfg.genList === 'relicList') {
        listContent = generateRelicList(entities, localeLookup, lang)
      } else {
        listContent = generateSimpleList(typeEntities, cfg.type, cfg.typeNames, localeLookup, lang)
      }
      writeFileSync(join(outDir, 'index.md'), listContent, 'utf-8')

      // Detail pages
      for (const entity of typeEntities) {
        const content = cfg.genDetail(entity, localeLookup, lang)
        writeFileSync(join(outDir, `${entity.id}.md`), content, 'utf-8')
      }
    }
  }
}

function main() {
  console.log('=== YuWanCard Wiki Generator ===\n')

  // Step 1: Parse
  console.log('Parsing C# source files...')
  const entities = parseAllEntities()
  resolveCardTipTargets(entities)
  console.log(`\nTotal entities parsed: ${entities.length}`)

  // Step 2: Localization
  console.log('\nLoading localizations...')
  const locZhs = loadLocalization('zhs')
  const locEng = loadLocalization('eng')
  console.log(`  zhs keys: ${Object.keys(locZhs).length}`)
  console.log(`  eng keys: ${Object.keys(locEng).length}`)
  const localeLookup = buildLocaleLookup(entities, locZhs, locEng)

  // Step 3: Copy images
  console.log('\nCopying image assets...')
  copyImages(entities, IMG_ROOT, PUBLIC_IMG)
  const withImages = entities.filter(e => e.image).length
  console.log(`  Entities with images: ${withImages}/${entities.length}`)

  // Step 4: Generate pages
  console.log('\nGenerating Markdown pages...')
  generatePages(entities, localeLookup)

  // Step 5: Homepages & search
  console.log('\nGenerating homepages...')
  for (const lang of ['zhs', 'eng']) {
    const outDir = lang === 'zhs' ? OUTPUT_ZHS : OUTPUT_ENG
    writeFileSync(join(outDir, 'index.md'), generateHomepage(entities, localeLookup, lang), 'utf-8')
  }
  console.log('  Homepages generated')

  // Step 6: Search indexes
  console.log('\nGenerating search indexes...')
  const dataDir = join(WIKI_ROOT, 'public', 'assets', 'data')
  ensureDir(dataDir)
  for (const lang of ['zhs', 'eng']) {
    const index = generateSearchIndex(entities, localeLookup, lang)
    writeFileSync(join(dataDir, `search_index_${lang}.json`), JSON.stringify(index, null, 2), 'utf-8')
    console.log(`  ${lang}: ${index.length} entries`)
  }

  // Step 7: Root redirect
  console.log('\nGenerating root index...')
  writeFileSync(join(WIKI_ROOT, 'index.md'), generateRootRedirect(), 'utf-8')

  console.log('\n=== Wiki generation complete! ===')
  console.log(`Generated content for ${entities.length} entities in 2 languages`)
  console.log('Run "cd wiki && npm run build" to build the VitePress site')
  console.log('Run "cd wiki && npm run dev" to preview locally')
}

main()
