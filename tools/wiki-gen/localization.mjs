// Localization loading and entity-locale lookup

import { readdirSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'
import { readJson } from './utils.mjs'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '../..')
const LOC_ROOT = join(ROOT, 'YuWanCard', 'localization')

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
  let description = locMap[`${prefix}.description`] || null
  let flavor = locMap[`${prefix}.flavor`] || null
  let smartDescription = locMap[`${prefix}.smartDescription`] || null

  if (!title) {
    for (const key of Object.keys(locMap)) {
      if (key.includes(idUpper) && key.endsWith('.title')) {
        const foundPrefix = key.replace('.title', '')
        title = locMap[key]
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

  return { title, description, flavor, smartDescription }
}

export function buildLocaleLookup(entities, locZhs, locEng) {
  const lookup = { zhs: {}, eng: {} }
  for (const entity of entities) {
    lookup.zhs[entity.id] = getLocaleData(locZhs, entity.id, entity.type)
    lookup.eng[entity.id] = getLocaleData(locEng, entity.id, entity.type)
  }
  return lookup
}
