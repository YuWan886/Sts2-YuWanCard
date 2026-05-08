// Localization loading and entity-locale lookup

import { readdirSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'
import { readJson } from './utils.mjs'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '../..')
const LOC_ROOT = join(ROOT, 'YuWanCard', 'localization')

function escapeRegExp(str) {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

function parseDialogueLineKey(lineKey) {
  const m = lineKey.match(/^(\d+)-(\d+)([a-z]*)$/i)
  if (!m) return [Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER, lineKey]
  return [parseInt(m[1], 10), parseInt(m[2], 10), m[3] || '']
}

function extractAncientData(locMap, prefix) {
  const escapedPrefix = escapeRegExp(prefix)
  const pageDescRegex = new RegExp(`^${escapedPrefix}\\.pages\\.([^.]+)\\.description$`)
  const optionRegex = new RegExp(`^${escapedPrefix}\\.pages\\.([^.]+)\\.options\\.([^.]+)\\.(title|description)$`)
  const talkRegex = new RegExp(`^${escapedPrefix}\\.talk\\.([^.]+)\\.([^.]+)\\.(ancient|char|next)$`)

  const pageDescriptions = []
  const optionsMap = new Map()
  const dialogueMap = new Map()

  for (const [key, value] of Object.entries(locMap)) {
    const pageDescMatch = key.match(pageDescRegex)
    if (pageDescMatch) {
      pageDescriptions.push({ page: pageDescMatch[1], text: value })
      continue
    }

    const optionMatch = key.match(optionRegex)
    if (optionMatch) {
      const page = optionMatch[1]
      const optionKey = optionMatch[2]
      const field = optionMatch[3]
      const mapKey = `${page}|${optionKey}`
      if (!optionsMap.has(mapKey)) optionsMap.set(mapKey, { page, optionKey, title: '', description: '' })
      optionsMap.get(mapKey)[field] = value
      continue
    }

    const talkMatch = key.match(talkRegex)
    if (talkMatch) {
      const group = talkMatch[1]
      const lineKey = talkMatch[2]
      const role = talkMatch[3]
      const mapKey = `${group}|${lineKey}`
      if (!dialogueMap.has(mapKey)) dialogueMap.set(mapKey, { group, lineKey })
      dialogueMap.get(mapKey)[role] = value
    }
  }

  const pageOrder = page => {
    if (page === 'INITIAL') return 0
    if (page === 'DONE') return 100
    return 50
  }

  pageDescriptions.sort((a, b) =>
    pageOrder(a.page) - pageOrder(b.page) || a.page.localeCompare(b.page))

  const ancientOptions = [...optionsMap.values()].sort((a, b) =>
    pageOrder(a.page) - pageOrder(b.page)
    || a.page.localeCompare(b.page)
    || a.optionKey.localeCompare(b.optionKey))

  const groupOrder = group => {
    if (group === 'firstVisitEver') return 0
    if (group === 'ANY') return 1
    return 10
  }

  const ancientDialogues = [...dialogueMap.values()].sort((a, b) => {
    const groupCmp = groupOrder(a.group) - groupOrder(b.group) || a.group.localeCompare(b.group)
    if (groupCmp !== 0) return groupCmp

    const [aLeft, aRight, aSuffix] = parseDialogueLineKey(a.lineKey)
    const [bLeft, bRight, bSuffix] = parseDialogueLineKey(b.lineKey)
    if (aLeft !== bLeft) return aLeft - bLeft
    if (aRight !== bRight) return aRight - bRight
    return aSuffix.localeCompare(bSuffix)
  })

  const initialDescription = pageDescriptions.find(p => p.page === 'INITIAL')?.text || null

  return { initialDescription, pageDescriptions, ancientOptions, ancientDialogues }
}

function extractEventData(locMap, prefix) {
  const escapedPrefix = escapeRegExp(prefix)
  const pageDescRegex = new RegExp(`^${escapedPrefix}\\.pages\\.([^.]+)\\.description$`)
  const optionRegex = new RegExp(`^${escapedPrefix}\\.pages\\.([^.]+)\\.options\\.([^.]+)\\.(title|description)$`)

  const pageDescriptions = []
  const optionsMap = new Map()

  for (const [key, value] of Object.entries(locMap)) {
    const pageDescMatch = key.match(pageDescRegex)
    if (pageDescMatch) {
      pageDescriptions.push({ page: pageDescMatch[1], text: value })
      continue
    }

    const optionMatch = key.match(optionRegex)
    if (optionMatch) {
      const page = optionMatch[1]
      const optionKey = optionMatch[2]
      const field = optionMatch[3]
      const mapKey = `${page}|${optionKey}`
      if (!optionsMap.has(mapKey)) optionsMap.set(mapKey, { page, optionKey, title: '', description: '' })
      optionsMap.get(mapKey)[field] = value
    }
  }

  const pageOrder = page => {
    if (page === 'INITIAL') return 0
    if (page === 'DONE') return 100
    return 50
  }

  pageDescriptions.sort((a, b) =>
    pageOrder(a.page) - pageOrder(b.page) || a.page.localeCompare(b.page))

  const eventOptions = [...optionsMap.values()].sort((a, b) =>
    pageOrder(a.page) - pageOrder(b.page)
    || a.page.localeCompare(b.page)
    || a.optionKey.localeCompare(b.optionKey))

  const initialDescription = pageDescriptions.find(p => p.page === 'INITIAL')?.text || null
  return { initialDescription, pageDescriptions, eventOptions }
}

export function loadLocalization(lang) {
  const locDir = join(LOC_ROOT, lang)
  const result = {}
  for (const file of readdirSync(locDir)) {
    if (!file.endsWith('.json')) continue
    const data = readJson(join(locDir, file))
    if (!data) continue
    for (const [key, value] of Object.entries(data)) result[key] = value
  }
  return result
}

export function getLocaleData(locMap, entityId, entityType) {
  const idUpper = entityId.toUpperCase()
  const prefix = `YUWANCARD-${idUpper}`

  let title = locMap[`${prefix}.title`] || null
  let epithet = locMap[`${prefix}.epithet`] || null
  let description = locMap[`${prefix}.description`] || null
  let flavor = locMap[`${prefix}.flavor`] || null
  let smartDescription = locMap[`${prefix}.smartDescription`] || null

  if (!title) {
    for (const key of Object.keys(locMap)) {
      if (key.includes(idUpper) && key.endsWith('.title')) {
        const foundPrefix = key.replace('.title', '')
        title = locMap[key]
        epithet = locMap[`${foundPrefix}.epithet`] || epithet
        description = locMap[`${foundPrefix}.description`] || description
        flavor = locMap[`${foundPrefix}.flavor`] || flavor
        smartDescription = locMap[`${foundPrefix}.smartDescription`] || smartDescription
        break
      }
    }
  }

  if (!title && entityType === 'monster') {
    for (const key of Object.keys(locMap)) {
      if ((key.startsWith(`${idUpper}.`) || key.startsWith(`${idUpper}_`)) && key.endsWith('.name')) {
        title = locMap[key]
        break
      }
    }
  }

  if (entityType === 'ancient') {
    const ancientData = extractAncientData(locMap, prefix)
    if (!description) description = ancientData.initialDescription || ancientData.pageDescriptions[0]?.text || null
    return { title, epithet, description, flavor, smartDescription, ...ancientData }
  }

  if (entityType === 'event') {
    const eventData = extractEventData(locMap, prefix)
    if (!description) description = eventData.initialDescription || eventData.pageDescriptions[0]?.text || null
    return { title, epithet, description, flavor, smartDescription, ...eventData }
  }

  return { title, epithet, description, flavor, smartDescription }
}

export function buildLocaleLookup(entities, locZhs, locEng) {
  const lookup = { zhs: {}, eng: {} }
  for (const entity of entities) {
    lookup.zhs[entity.id] = getLocaleData(locZhs, entity.id, entity.type)
    lookup.eng[entity.id] = getLocaleData(locEng, entity.id, entity.type)
  }
  return lookup
}
