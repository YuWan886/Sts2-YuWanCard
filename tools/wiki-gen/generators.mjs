// Markdown generators: detail pages, list pages, search index, homepage

import { join } from 'path'
import { writeFileSync } from 'fs'
import {
  yamlValue, jsString, rarityBadge, typeBadge, BASE, basePath,
  bbcodeToHtml, stripBBCode, resolveListDesc, CATEGORY_NAMES, ensureDir
} from './utils.mjs'

// ---- Helpers ----
function getEntityDescription(loc, entity) {
  if (!loc) return ''
  if (entity?.type === 'orb') return loc.smartDescription || loc.description || ''
  if (entity?.type === 'ancient') {
    return loc.description || loc.initialDescription || loc.pageDescriptions?.[0]?.text || ''
  }
  if (entity?.type === 'event') {
    return loc.description || loc.initialDescription || loc.pageDescriptions?.[0]?.text || ''
  }
  return loc.description || loc.smartDescription || ''
}

function escapeHtml(value) {
  return String(value || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

function richTextToHtml(text) {
  return bbcodeToHtml(text || '').replace(/\n/g, '<br />')
}

function resolveTextWithEntityVars(text, entity) {
  if (!text) return ''

  const lookup = {}
  for (const v of (entity?.variables || [])) {
    lookup[v.name] = String(v.base)
    const stripped = v.name.replace(/Power$/, '')
    if (stripped !== v.name && !lookup[stripped]) lookup[stripped] = String(v.base)
  }

  const firstVar = Object.keys(lookup)[0] ? lookup[Object.keys(lookup)[0]] : undefined
  const getVal = name => {
    if (lookup[name] !== undefined) return lookup[name]
    if (name.endsWith('Power')) return lookup[name.replace(/Power$/, '')]
    if (name === 'Amount' && firstVar !== undefined) return firstVar
    return undefined
  }

  let result = text
  result = result.replace(/\{(\w+):diff\(\)\}/g, (_, name) => {
    const v = getVal(name)
    return v !== undefined ? v : `{${name}:diff()}`
  })
  result = result.replace(/\{(\w+):(\w+)\(\)\}/g, (_, name, fmt) => {
    const v = getVal(name)
    return v !== undefined ? v : `{${name}:${fmt}()}`
  })
  result = result.replace(/\{(\w+)\}/g, (_, name) => {
    const v = getVal(name)
    return v !== undefined ? v : `{${name}}`
  })

  return result
}

function ancientGroupLabel(group, lang) {
  if (group === 'firstVisitEver') return lang === 'zhs' ? '首次遇见' : 'First Visit'
  if (group === 'ANY') return lang === 'zhs' ? '通用对话' : 'Generic'
  return group
}

function ancientSpeakerLabel(role, lang) {
  if (role === 'ancient') return lang === 'zhs' ? '先古' : 'Ancient'
  if (role === 'char') return lang === 'zhs' ? '角色' : 'Character'
  return lang === 'zhs' ? '下一步按钮' : 'Next Button'
}

function renderCardTips(entity, locale, lang) {
  const tips = entity.tipTargets || []
  if (!tips.length) return ''

  const sectionTitle = lang === 'zhs' ? '额外提示' : 'Extra Tips'
  const tags = tips.map(tip => {
    const linkedLoc = tip.id ? (locale[lang]?.[tip.id] || locale['zhs']?.[tip.id] || {}) : {}
    const label = escapeHtml(tip.className || linkedLoc.title || '')
    const titleAttr = linkedLoc.title ? ` title="${escapeHtml(linkedLoc.title)}"` : ''

    if (tip.id && tip.type) {
      const href = basePath(`/${lang}/${tip.type}s/${tip.id}`)
      return `<a class="keyword-tag" href="${href}"${titleAttr}>${label}</a>`
    }

    return `<span class="keyword-tag"${titleAttr}>${label}</span>`
  }).join('\n')

  return `<div style="margin:16px 0 8px"><span class="entity-section-title">${sectionTitle}</span><div style="margin-top:6px">${tags}</div></div>`
}

function renderAncientOptions(loc, lang) {
  const options = loc.ancientOptions || []
  if (!options.length) return ''

  const sectionTitle = lang === 'zhs' ? '选项' : 'Options'
  const pageLabel = lang === 'zhs' ? '页面' : 'Page'
  let html = `<div class="entity-section-title">${sectionTitle}</div>`

  for (const opt of options) {
    const title = richTextToHtml(opt.title || opt.optionKey)
    const desc = richTextToHtml(opt.description || '')
    html += `<div class="entity-description" style="margin:10px 0 14px">
  <div style="font-size:0.8rem;color:var(--text-muted);margin-bottom:8px">${pageLabel}: ${escapeHtml(opt.page)} / ${escapeHtml(opt.optionKey)}</div>
  <div style="font-weight:600;margin-bottom:${desc ? '6px' : '0'}">${title}</div>
  ${desc ? `<div>${desc}</div>` : ''}
</div>`
  }

  return html
}

function renderEventPageDescriptions(loc, entity, lang) {
  const pages = (loc.pageDescriptions || []).filter(p => p.page === 'INITIAL')
  if (!pages.length) return ''

  const sectionTitle = lang === 'zhs' ? '页面描述' : 'Page Descriptions'
  const pageLabel = lang === 'zhs' ? '页面' : 'Page'
  let html = `<div class="entity-section-title">${sectionTitle}</div>`

  for (const page of pages) {
    const text = richTextToHtml(resolveTextWithEntityVars(page.text || '', entity))
    html += `<div class="entity-description" style="margin:10px 0 14px">
  <div style="font-size:0.8rem;color:var(--text-muted);margin-bottom:8px">${pageLabel}: ${escapeHtml(page.page)}</div>
  <div>${text}</div>
</div>`
  }

  return html
}

function renderEventOptions(loc, entity, lang) {
  const options = (loc.eventOptions || []).filter(opt =>
    opt.page === 'INITIAL' && !/_LOCKED$/i.test(opt.optionKey || ''))
  if (!options.length) return ''

  const sectionTitle = lang === 'zhs' ? '选项' : 'Options'
  const pageLabel = lang === 'zhs' ? '页面' : 'Page'
  let html = `<div class="entity-section-title">${sectionTitle}</div>`

  for (const opt of options) {
    const title = richTextToHtml(resolveTextWithEntityVars(opt.title || opt.optionKey, entity))
    const desc = richTextToHtml(resolveTextWithEntityVars(opt.description || '', entity))
    html += `<div class="entity-description" style="margin:10px 0 14px">
  <div style="font-size:0.8rem;color:var(--text-muted);margin-bottom:8px">${pageLabel}: ${escapeHtml(opt.page)} / ${escapeHtml(opt.optionKey)}</div>
  <div style="font-weight:600;margin-bottom:${desc ? '6px' : '0'}">${title}</div>
  ${desc ? `<div>${desc}</div>` : ''}
</div>`
  }

  return html
}

function renderAncientDialogues(loc, lang) {
  const dialogues = loc.ancientDialogues || []
  if (!dialogues.length) return ''

  const grouped = new Map()
  for (const line of dialogues) {
    if (!grouped.has(line.group)) grouped.set(line.group, [])
    grouped.get(line.group).push(line)
  }

  const sectionTitle = lang === 'zhs' ? '对话' : 'Dialogues'
  const lineLabel = lang === 'zhs' ? '行' : 'Line'
  let html = `<div class="entity-section-title">${sectionTitle}</div>`

  for (const [group, lines] of grouped.entries()) {
    const summaryLabel = `${ancientGroupLabel(group, lang)} (${lines.length})`
    const openAttr = group === 'firstVisitEver' ? ' open' : ''
    html += `<details${openAttr} style="margin:8px 0 14px;border:1px solid var(--border-subtle);border-radius:var(--radius-md);background:var(--bg-card)">
  <summary style="cursor:pointer;list-style:none;padding:10px 14px;font-size:0.92rem;font-weight:600;color:var(--text-secondary)">
    ${escapeHtml(summaryLabel)}
  </summary>
  <div style="padding:0 10px 10px">`

    for (const line of lines) {
      const segments = []
      if (line.ancient) segments.push({ role: 'ancient', text: line.ancient })
      if (line.char) segments.push({ role: 'char', text: line.char })
      if (line.next) segments.push({ role: 'next', text: line.next })
      if (!segments.length) continue

      let segmentHtml = ''
      for (const seg of segments) {
        segmentHtml += `<div style="margin-top:8px">
  <div style="font-size:0.78rem;color:var(--text-muted);margin-bottom:4px">${escapeHtml(ancientSpeakerLabel(seg.role, lang))}</div>
  <div>${richTextToHtml(seg.text)}</div>
</div>`
      }

      html += `<div class="entity-description" style="margin:8px 0 12px">
  <div style="font-size:0.8rem;color:var(--text-muted);margin-bottom:4px">${lineLabel}: ${escapeHtml(line.lineKey)}</div>
  ${segmentHtml}
</div>`
    }

    html += `</div></details>`
  }

  return html
}

function generateAncientMd(entity, loc, lang) {
  const title = loc.title || entity.className
  const descRaw = getEntityDescription(loc, entity).replace(/\\n/g, '\\\\n')
  const vmap = varMap(entity)
  const epithet = loc.epithet || ''
  const optionsSection = renderAncientOptions(loc, lang)
  const dialoguesSection = renderAncientDialogues(loc, lang)
  const descriptionTitle = lang === 'zhs' ? '描述' : 'Description'

  return `${frontmatter({
    title, type: entity.type, id: entity.id, image: entity.image
  })}
<script setup>
const descRaw = ${jsString(descRaw)}
const varMap = ${JSON.stringify(vmap)}
</script>

# ${title}

<div class="entity-detail">
<div class="entity-header">
  ${entity.image ? `<img src="${entity.image}" alt="${title}" />` : ''}
  <div class="entity-info">
    <div class="entity-title">${title}</div>
    ${epithet ? `<div style="margin-top:8px;color:var(--text-secondary);font-size:0.95rem">${richTextToHtml(epithet)}</div>` : ''}
  </div>
</div>

${descRaw ? `<div class="entity-section-title">${descriptionTitle}</div>
<div class="entity-description">
<RichDescription :text="descRaw" :variables="varMap" />
</div>` : ''}
${optionsSection}
${dialoguesSection}
</div>`
}

function generateEventMd(entity, loc, lang) {
  const title = loc.title || entity.className
  const descRaw = getEntityDescription(loc, entity).replace(/\\n/g, '\\\\n')
  const vmap = varMap(entity)
  const optionsSection = renderEventOptions(loc, entity, lang)
  const descriptionTitle = lang === 'zhs' ? '描述' : 'Description'
  const actsInfo = (entity.acts && entity.acts.length)
    ? `<div style="display:flex;flex-wrap:wrap;gap:6px 16px;color:var(--text-secondary);font-size:0.85rem;margin-top:8px"><span><strong>Acts:</strong> ${escapeHtml(entity.acts.join(', '))}</span></div>`
    : ''

  return `${frontmatter({
    title, type: entity.type, id: entity.id, image: entity.image
  })}
<script setup>
const descRaw = ${jsString(descRaw)}
const varMap = ${JSON.stringify(vmap)}
</script>

# ${title}

<div class="entity-detail">
<div class="entity-header">
  ${entity.image ? `<img src="${entity.image}" alt="${title}" />` : ''}
  <div class="entity-info">
    <div class="entity-title">${title}</div>
    ${actsInfo}
  </div>
</div>

${descRaw ? `<div class="entity-section-title">${descriptionTitle}</div>
<div class="entity-description">
<RichDescription :text="descRaw" :variables="varMap" />
</div>` : ''}
${optionsSection}
</div>`
}

function varMap(entity) {
  const map = {}
  for (const v of (entity.variables || [])) {
    if (typeof v.base === 'number' && Number.isNaN(v.base)) continue
    if (String(v.base) === 'NaN') continue
    map[v.name] = String(v.base)
    if (v.upgrade && !(typeof v.upgrade === 'number' && Number.isNaN(v.upgrade))) map[`_upg_${v.name}`] = String(v.upgrade)
  }
  return map
}

function frontmatter(fields) {
  const lines = ['---']
  for (const [k, v] of Object.entries(fields)) {
    if (v !== undefined && v !== null && v !== '') lines.push(`${k}: ${yamlValue(v)}`)
  }
  lines.push('---\n')
  return lines.join('\n')
}

const RELIC_POOL_NAMES = {
  zhs: {
    shared: '共享',
    pig: '猪猪',
    event: '事件',
    whatif: '假如',
    regent: '储君'
  },
  eng: {
    shared: 'Shared',
    pig: 'Pig',
    event: 'Event',
    whatif: 'What If',
    regent: 'Regent'
  }
}

// ---- Card Detail Page ----

export function generateCardMd(entity, locale, lang) {
  const loc = locale[lang]?.[entity.id] || locale['zhs']?.[entity.id] || {}
  const title = loc.title || entity.className
  const descRaw = (loc.description || '').replace(/\\n/g, '\\\\n')
  const flavorRaw = loc.flavor || ''
  const flavor = flavorRaw ? bbcodeToHtml(flavorRaw) : null

  const stats = [`<tr><td>Cost</td><td>${entity.cost}</td><td>${entity.cost + entity.costUpgrade}</td></tr>`]
  for (const v of entity.variables) {
    if (v.upgrade > 0)
      stats.push(`<tr><td>${v.name}</td><td>${v.base}</td><td>${v.base + v.upgrade} (+${v.upgrade})</td></tr>`)
    else
      stats.push(`<tr><td>${v.name}</td><td>${v.base}</td><td>${v.base}</td></tr>`)
  }

  const badges = []
  if (entity.rarity) badges.push(rarityBadge(entity.rarity))
  if (entity.cardType) badges.push(typeBadge(entity.cardType))

  const kwTags = (entity.keywords || []).map(k => `<span class="keyword-tag">${k}</span>`).join('\n')
  const tagTags = (entity.tags || []).map(t => `<span class="keyword-tag">${t}</span>`).join('\n')
  const categoryLabel = CATEGORY_NAMES[lang]?.[entity.category] || entity.category

  const isZhs = lang === 'zhs'
  const vmap = varMap(entity)
  const tipTags = renderCardTips(entity, locale, lang)

  return `${frontmatter({
    title, type: 'card', rarity: entity.rarity, cost: entity.cost,
    cardType: entity.cardType, category: entity.category, id: entity.id, image: entity.image
  })}
<script setup>
const descRaw = ${jsString(descRaw)}
const varMap = ${JSON.stringify(vmap)}
</script>

# ${title}

<div class="entity-detail">

<div class="entity-header">
  ${entity.image ? `<img src="${entity.image}" alt="${title}" />` : ''}
  <div class="entity-info">
    <div class="entity-title">${title}</div>
    <div class="entity-badges">
      ${badges.join('\n')}
      ${entity.cost !== undefined ? `<span class="cost-orb cost-orb-lg">${entity.cost}</span>` : ''}
    </div>
    <div style="margin-top:8px;color:var(--text-secondary);font-size:0.85rem">
      ${isZhs ? '卡池' : 'Card Pool'}: ${categoryLabel}
    </div>
  </div>
</div>

<div class="stats-table-wrapper">
<table class="stats-table">
<thead><tr><th>${isZhs ? '属性' : 'Stat'}</th><th>${isZhs ? '基础' : 'Base'}</th><th>${isZhs ? '升级后' : 'Upgraded'}</th></tr></thead>
<tbody>${stats.join('\n')}</tbody>
</table>
</div>

<div class="entity-section-title">${isZhs ? '效果' : 'Effect'}</div>

<div class="entity-description">
<RichDescription :text="descRaw" :variables="varMap" />
</div>

${flavor ? `<div class="entity-flavor">${flavor}</div>` : ''}
${tipTags}
${kwTags ? `<div style="margin:16px 0 8px"><span class="entity-section-title">${isZhs ? '关键词' : 'Keywords'}</span><div style="margin-top:6px">${kwTags}</div></div>` : ''}
${tagTags ? `<div style="margin:16px 0 8px"><span class="entity-section-title">${isZhs ? '标签' : 'Tags'}</span><div style="margin-top:6px">${tagTags}</div></div>` : ''}

</div>
`
}

// ---- Relic Detail Page ----

export function generateRelicMd(entity, locale, lang) {
  const loc = locale[lang]?.[entity.id] || locale['zhs']?.[entity.id] || {}
  const title = loc.title || entity.className
  const descRaw = (loc.description || '').replace(/\\n/g, '\\\\n')
  const flavorRaw = loc.flavor || ''
  const flavor = flavorRaw ? bbcodeToHtml(flavorRaw) : null
  const vmap = varMap(entity)
  const isZhs = lang === 'zhs'
  const poolLabel = RELIC_POOL_NAMES[lang]?.[entity.pool] || entity.pool

  return `${frontmatter({
    title, type: 'relic', rarity: entity.rarity, pool: entity.pool, id: entity.id, image: entity.image
  })}
<script setup>
const descRaw = ${jsString(descRaw)}
const varMap = ${JSON.stringify(vmap)}
</script>

# ${title}

<div class="entity-detail">
<div class="entity-header">
  ${entity.image ? `<img src="${entity.image}" alt="${title}" />` : ''}
  <div class="entity-info">
    <div class="entity-title">${title}</div>
    <div class="entity-badges">${rarityBadge(entity.rarity)}</div>
    ${poolLabel ? `<div style="margin-top:8px;color:var(--text-secondary);font-size:0.85rem">
      ${isZhs ? '遗物池' : 'Relic Pool'}: ${poolLabel}
    </div>` : ''}
  </div>
</div>
<div class="entity-description">
<RichDescription :text="descRaw" :variables="varMap" />
</div>
${flavor ? `<div class="entity-flavor">${flavor}</div>` : ''}
</div>
`
}

// ---- Power Detail Page ----

export function generatePowerMd(entity, locale, lang) {
  const loc = locale[lang]?.[entity.id] || locale['zhs']?.[entity.id] || {}
  const title = loc.title || entity.className
  const descRaw = (loc.smartDescription || loc.description || '').replace(/\\n/g, '\\\\n')
  const vmap = varMap(entity)

  return `${frontmatter({
    title, type: 'power', powerType: entity.powerType, stackType: entity.stackType,
    id: entity.id, image: entity.image
  })}
<script setup>
const descRaw = ${jsString(descRaw)}
const varMap = ${JSON.stringify(vmap)}
</script>

# ${title}

<div class="entity-detail">
<div class="entity-header">
  ${entity.image ? `<img src="${entity.image}" alt="${title}" />` : ''}
  <div class="entity-info">
    <div class="entity-title">${title}</div>
    <div class="entity-badges">
      ${entity.powerType === 'Buff' ? '<span class="rarity-badge type-power">Buff</span>' : '<span class="rarity-badge type-attack">Debuff</span>'}
      <span class="rarity-badge rarity-common">${entity.stackType || ''}</span>
    </div>
  </div>
</div>
<div class="entity-description">
<RichDescription :text="descRaw" :variables="varMap" />
</div>
</div>
`
}

// ---- Generic Detail Page (enchantment, orb, monster, event, ancient, modifier, character) ----

export function generateSimpleMd(entity, locale, lang) {
  const loc = locale[lang]?.[entity.id] || locale['zhs']?.[entity.id] || {}
  if (entity.type === 'ancient') return generateAncientMd(entity, loc, lang)
  if (entity.type === 'event') return generateEventMd(entity, loc, lang)

  const title = loc.title || entity.className
  const descRaw = getEntityDescription(loc, entity).replace(/\\n/g, '\\\\n')
  const vmap = varMap(entity)

  const extraInfo = []
  if (entity.minHp !== undefined) extraInfo.push(`<span><strong>HP:</strong> ${entity.minHp} - ${entity.maxHp}</span>`)
  if (entity.acts && entity.acts.length) extraInfo.push(`<span><strong>Acts:</strong> ${entity.acts.join(', ')}</span>`)
  if (entity.startingHp !== undefined) extraInfo.push(`<span><strong>Starting HP:</strong> ${entity.startingHp}</span>`)
  if (entity.startingGold !== undefined) extraInfo.push(`<span><strong>Starting Gold:</strong> ${entity.startingGold}</span>`)
  if (entity.powerType) extraInfo.push(`<span><strong>Type:</strong> ${entity.powerType}</span>`)
  if (entity.stackType) extraInfo.push(`<span><strong>Stack:</strong> ${entity.stackType}</span>`)

  return `${frontmatter({
    title, type: entity.type, id: entity.id, image: entity.image
  })}
<script setup>
const descRaw = ${jsString(descRaw)}
const varMap = ${JSON.stringify(vmap)}
</script>

# ${title}

<div class="entity-detail">
<div class="entity-header">
  ${entity.image ? `<img src="${entity.image}" alt="${title}" />` : ''}
  <div class="entity-info">
    <div class="entity-title">${title}</div>
    ${extraInfo.length ? `<div style="display:flex;flex-wrap:wrap;gap:6px 16px;color:var(--text-secondary);font-size:0.85rem;margin-top:8px">${extraInfo.join('')}</div>` : ''}
  </div>
</div>
<div class="entity-description">
<RichDescription :text="descRaw" :variables="varMap" />
</div>
</div>`
}

// ---- List Pages ----

export function generateCardList(entities, locale, lang) {
  const title = lang === 'zhs' ? '卡牌' : 'Cards'
  const cards = []

  for (const e of entities) {
    if (e.type !== 'card' || e.hiddenFromLibrary) continue
    const loc = locale[lang]?.[e.id] || {}
    cards.push({
      id: e.id, title: loc.title || e.className,
      desc: resolveListDesc(loc.description, e.variables).substring(0, 100),
      image: e.image || '', url: basePath(`/${lang}/cards/${e.id}`),
      cost: e.cost, rarity: e.rarity || '', cardType: e.cardType || '',
      category: e.category || 'other'
    })
  }

  return `${frontmatter({ title, layout: 'page' })}
# ${title}

<CardBrowser :cards-data="cardsData" :lang="'${lang}'" />

<script setup>
const cardsData = ${JSON.stringify(cards)}
</script>
`
}

export function generateRelicList(entities, locale, lang) {
  const title = lang === 'zhs' ? '遗物' : 'Relics'
  const relics = []

  for (const e of entities) {
    if (e.type !== 'relic' || e.hiddenFromLibrary) continue
    const loc = locale[lang]?.[e.id] || {}
    relics.push({
      id: e.id, title: loc.title || e.className,
      desc: resolveListDesc(loc.description, e.variables).substring(0, 100),
      image: e.image || '', url: basePath(`/${lang}/relics/${e.id}`),
      rarity: e.rarity || '', pool: e.pool || ''
    })
  }

  return `${frontmatter({ title, layout: 'page' })}
# ${title}

<RelicBrowser :relics-data="relicsData" :lang="'${lang}'" />

<script setup>
const relicsData = ${JSON.stringify(relics)}
</script>
`
}

export function generateSimpleList(typeEntities, typeName, typeNames, locale, lang) {
  const title = typeNames[lang] || typeName
  const items = typeEntities.filter(e => !e.hiddenFromLibrary).map(item => {
    const loc = locale[lang]?.[item.id] || {}
    return {
      id: item.id, title: loc.title || item.className,
      description: resolveListDesc(getEntityDescription(loc, item), item.variables).substring(0, 100),
      image: item.image || '', url: basePath(`/${lang}/${typeName}s/${item.id}`),
      rarity: item.rarity || '', cardType: item.cardType || '',
      cost: item.cost, pool: item.pool || ''
    }
  })

  return `${frontmatter({ title, layout: 'page' })}
<script setup>
const itemsData = ${JSON.stringify(items)}
</script>

# ${title}

<EntityGrid :items="itemsData" :lang="'${lang}'" :show-cost="false" />
`
}

// ---- Search Index ----

const SEARCH_TYPE_NAMES = {
  zhs: {
    card: '卡牌', relic: '遗物', power: '能力', enchantment: '附魔',
    orb: '充能球', monster: '怪物', event: '事件', ancient: '先古之民',
    modifier: '修改器', character: '角色'
  },
  eng: {
    card: 'Card', relic: 'Relic', power: 'Power', enchantment: 'Enchantment',
    orb: 'Orb', monster: 'Monster', event: 'Event', ancient: 'Ancient',
    modifier: 'Modifier', character: 'Character'
  }
}

export function generateSearchIndex(entities, locale, lang) {
  const index = []
  const typeNames = SEARCH_TYPE_NAMES[lang] || SEARCH_TYPE_NAMES.eng

  for (const entity of entities) {
    if (entity.hiddenFromLibrary) continue
    const loc = locale[lang]?.[entity.id] || {}
    index.push({
      id: entity.id, type: entity.type, typeName: typeNames[entity.type] || entity.type,
      category: entity.category || '',
      title: loc.title || entity.className,
      description: resolveListDesc(getEntityDescription(loc, entity), entity.variables),
      image: entity.image || '', cost: entity.cost,
      rarity: entity.rarity || '', cardType: entity.cardType || '',
      keywords: [...(entity.keywords || []), ...(entity.tags || [])],
      url: basePath(`/${lang}/${entity.type}s/${entity.id}`)
    })
  }

  return index
}


// ---- Homepage ----

export function generateHomepage(entities, locale, lang) {
  const title = 'YuWanCard'
  const subtitle = lang === 'zhs'
    ? '杀戮尖塔2 · 猪角色模组文档'
    : 'Slay the Spire 2 · Pig Character Mod'

  const counts = {}
  for (const e of entities) {
    if (e.hiddenFromLibrary) continue
    counts[e.type] = (counts[e.type] || 0) + 1
  }
  const total = Object.values(counts).reduce((a, b) => a + b, 0)

  const links = [
    { type: 'card', icon: '🃏', zhs: '卡牌', eng: 'Cards' },
    { type: 'relic', icon: '🏺', zhs: '遗物', eng: 'Relics' },
    { type: 'power', icon: '⚡', zhs: '能力', eng: 'Powers' },
    { type: 'enchantment', icon: '✨', zhs: '附魔', eng: 'Enchantments' },
    { type: 'orb', icon: '🔮', zhs: '充能球', eng: 'Orbs' },
    { type: 'monster', icon: '👾', zhs: '怪物', eng: 'Monsters' },
    { type: 'event', icon: '📜', zhs: '事件', eng: 'Events' },
    { type: 'ancient', icon: '🏛️', zhs: '先古之民', eng: 'Ancients' },
    { type: 'modifier', icon: '⚙️', zhs: '修改器', eng: 'Modifiers' },
    { type: 'character', icon: '🐷', zhs: '角色', eng: 'Character' },
  ]

  let linkCards = ''
  for (const link of links) {
    const count = counts[link.type] || 0
    if (count === 0 && link.type !== 'character') continue
    const name = lang === 'zhs' ? link.zhs : link.eng
    const countStr = count > 0 ? (lang === 'zhs' ? `${count}个` : `${count}`) : ''
    linkCards += `<a href="${basePath(`/${lang}/${link.type}s/`)}" class="home-link-card">
  <div class="link-icon">${link.icon}</div>
  <div class="link-title">${name}</div>
  <div class="link-count">${countStr}</div>
</a>\n`
  }

  return `${frontmatter({ title, layout: 'home' })}
<div class="home-hero">
  <h1>${title}</h1>
  <p class="subtitle">${subtitle}</p>
  <p style="margin-top:20px">
  </p>
</div>

<div class="home-links">
${linkCards}
</div>

<div style="text-align:center;margin:40px 0 20px">
  <div style="font-size:2rem;font-weight:700;color:var(--accent-gold)">${total}</div>
  <div style="font-size:0.85rem;color:var(--text-muted);margin-top:4px">
    ${lang === 'zhs' ? '游戏内容条目 · 自动从源码生成' : 'game content entries · auto-generated from source'}
  </div>
</div>
`
}

// ---- Root redirect ----

export function generateRootRedirect() {
  return `---
layout: home
---

<script setup>
import { onMounted } from 'vue'

onMounted(() => {
  const lang = navigator.language || ''
  const base = '${BASE}'
  if (lang.startsWith('zh')) {
    window.location.replace(base + '/zhs/')
  } else {
    window.location.replace(base + '/eng/')
  }
})
</script>
`
}
