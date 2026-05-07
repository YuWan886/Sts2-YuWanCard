// Markdown generators: detail pages, list pages, search index, homepage

import { join } from 'path'
import { writeFileSync } from 'fs'
import {
  yamlValue, jsString, rarityBadge, typeBadge, BASE, basePath,
  bbcodeToHtml, stripBBCode, resolveListDesc, CATEGORY_NAMES, ensureDir
} from './utils.mjs'

// ---- Helpers ----

function varMap(entity) {
  const map = {}
  for (const v of (entity.variables || [])) {
    map[v.name] = String(v.base)
    if (v.upgrade) map[`_upg_${v.name}`] = String(v.upgrade)
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
      ${isZhs ? '分类' : 'Category'}: ${categoryLabel}
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
  const title = loc.title || entity.className
  const descRaw = (loc.description || '').replace(/\\n/g, '\\\\n')
  const vmap = varMap(entity)

  const extraInfo = []
  if (entity.minHp !== undefined) extraInfo.push(`<span><strong>HP:</strong> ${entity.minHp} - ${entity.maxHp}</span>`)
  if (entity.acts) extraInfo.push(`<span><strong>Acts:</strong> ${entity.acts.join(', ')}</span>`)
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
      description: resolveListDesc(loc.description, item.variables).substring(0, 100),
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
      description: resolveListDesc(loc.description || loc.smartDescription, entity.variables),
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
