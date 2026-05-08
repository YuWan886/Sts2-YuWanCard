<!-- CardBrowser.vue — Card browser using unified EntityGrid -->
<template>
  <EntityGrid
    :items="itemsWithDesc"
    :lang="lang"
    :filter-groups="filterGroupConfig"
    :sort-options="sortOptionConfig"
    :show-cost="true"
    :show-desc="true"
  />
</template>

<script setup>
import { computed } from 'vue'
import EntityGrid from './EntityGrid.vue'

const props = defineProps({
  cardsData: { type: Array, required: true },
  lang: { type: String, default: 'zhs' }
})

const itemsWithDesc = computed(() =>
  props.cardsData.map(c => ({
    ...c,
    desc: c.desc || c.description || ''
  }))
)

const rarityOrder = { Basic: 0, Common: 1, Uncommon: 2, Rare: 3, Boss: 4, Ancient: 5, Starter: 6, Event: 7, Shop: 8, Token: 9 }

const rarityLabels = {
  Basic: { zhs: '基础', eng: 'Basic' },
  Common: { zhs: '普通', eng: 'Common' },
  Uncommon: { zhs: '罕见', eng: 'Uncommon' },
  Rare: { zhs: '稀有', eng: 'Rare' },
  Boss: { zhs: 'Boss', eng: 'Boss' },
  Ancient: { zhs: '先古', eng: 'Ancient' },
  Starter: { zhs: '初始', eng: 'Starter' },
  Event: { zhs: '事件', eng: 'Event' },
  Shop: { zhs: '商店', eng: 'Shop' },
  Token: { zhs: '衍生', eng: 'Token' }
}

const typeLabels = {
  Attack: { zhs: '攻击', eng: 'Attack' },
  Skill: { zhs: '技能', eng: 'Skill' },
  Power: { zhs: '能力', eng: 'Power' },
  Quest: { zhs: '任务', eng: 'Quest' },
  Status: { zhs: '状态', eng: 'Status' },
  Curse: { zhs: '诅咒', eng: 'Curse' }
}

const categoryOrder = { pig: 0, colorless: 1, event: 2, quest: 3, token: 4, regent: 5 }
const categoryLabels = {
  pig: { zhs: '猪猪', eng: 'Pig' },
  colorless: { zhs: '无色', eng: 'Colorless' },
  event: { zhs: '事件', eng: 'Event' },
  quest: { zhs: '任务', eng: 'Quest' },
  token: { zhs: '衍生', eng: 'Token' },
  regent: { zhs: '储君', eng: 'Regent' }
}

function labelFor(map, key) {
  const labels = map[key]
  if (!labels) return key
  return props.lang === 'zhs' ? labels.zhs : labels.eng
}

const categoryChips = computed(() =>
  [...new Set(props.cardsData.map(c => c.category).filter(Boolean))]
    .sort((a, b) => (categoryOrder[a] ?? 99) - (categoryOrder[b] ?? 99) || a.localeCompare(b))
    .map(key => ({ key, label: labelFor(categoryLabels, key) }))
)

const filterGroupConfig = computed(() => [
  {
    key: 'category', label: props.lang === 'zhs' ? '卡牌池' : 'Card Pool',
    chips: categoryChips.value,
    filterFn: (item, key) => item.category === key
  },
  {
    key: 'cost', label: props.lang === 'zhs' ? '费用' : 'Cost',
    chips: [
      { key: '-1', label: props.lang === 'zhs' ? 'X（-1）' : 'X (-1)' },
      { key: '0', label: '0' },
      { key: '1', label: '1' },
      { key: '2', label: '2' },
      { key: '3', label: '3' },
      { key: '4+', label: '4+' }
    ],
    filterFn: (item, key) => {
      if (key === '4+') return (item.cost ?? 0) >= 4
      return item.cost === parseInt(key)
    }
  },
  {
    key: 'type', label: props.lang === 'zhs' ? '类型' : 'Type',
    chips: [
      { key: 'Attack', label: labelFor(typeLabels, 'Attack') },
      { key: 'Skill', label: labelFor(typeLabels, 'Skill') },
      { key: 'Power', label: labelFor(typeLabels, 'Power') },
      { key: 'Quest', label: labelFor(typeLabels, 'Quest') },
      { key: 'Status', label: labelFor(typeLabels, 'Status') },
      { key: 'Curse', label: labelFor(typeLabels, 'Curse') }
    ],
    filterFn: (item, key) => item.cardType === key
  },
  {
    key: 'rarity', label: props.lang === 'zhs' ? '稀有度' : 'Rarity',
    chips: [
      { key: 'Basic', label: labelFor(rarityLabels, 'Basic') },
      { key: 'Common', label: labelFor(rarityLabels, 'Common') },
      { key: 'Uncommon', label: labelFor(rarityLabels, 'Uncommon') },
      { key: 'Rare', label: labelFor(rarityLabels, 'Rare') },
      { key: 'Ancient', label: labelFor(rarityLabels, 'Ancient') }
    ],
    filterFn: (item, key) => item.rarity === key
  }
])

const sortOptionConfig = [
  { key: 'default', label: props.lang === 'zhs' ? '默认排序' : 'Default', sortFn: null },
  { key: 'name', label: props.lang === 'zhs' ? '名称 A-Z' : 'Name A-Z',
    sortFn: (a, b) => a.title.localeCompare(b.title, props.lang === 'zhs' ? 'zh' : 'en') },
  { key: 'cost-asc', label: props.lang === 'zhs' ? '费用 ↑' : 'Cost ↑',
    sortFn: (a, b) => (a.cost ?? 99) - (b.cost ?? 99) },
  { key: 'cost-desc', label: props.lang === 'zhs' ? '费用 ↓' : 'Cost ↓',
    sortFn: (a, b) => (b.cost ?? 99) - (a.cost ?? 99) },
  { key: 'rarity', label: props.lang === 'zhs' ? '稀有度' : 'Rarity',
    sortFn: (a, b) => (rarityOrder[a.rarity] ?? 99) - (rarityOrder[b.rarity] ?? 99) }
]
</script>
