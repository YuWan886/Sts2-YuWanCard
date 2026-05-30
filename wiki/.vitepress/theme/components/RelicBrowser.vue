<!-- RelicBrowser.vue — Relic browser using unified EntityGrid -->
<template>
  <EntityGrid
    :items="itemsWithDesc"
    :lang="lang"
    :filter-groups="filterGroupConfig"
    :sort-options="sortOptionConfig"
    :show-cost="false"
    :show-desc="true"
  />
</template>

<script setup>
import { computed } from 'vue'
import EntityGrid from './EntityGrid.vue'

const props = defineProps({
  relicsData: { type: Array, required: true },
  lang: { type: String, default: 'zhs' }
})

const itemsWithDesc = computed(() =>
  props.relicsData.map(r => ({
    ...r,
    desc: r.desc || r.description || ''
  }))
)

const rarityOrder = { Starter: 0, Common: 1, Uncommon: 2, Rare: 3, Shop: 4, Event: 5, Ancient: 6, Silver: 7, Gold: 8, Prismatic: 9 }

const rarityLabels = {
  Starter: { zhs: '初始', eng: 'Starter' },
  Common: { zhs: '普通', eng: 'Common' },
  Uncommon: { zhs: '罕见', eng: 'Uncommon' },
  Rare: { zhs: '稀有', eng: 'Rare' },
  Shop: { zhs: '商店', eng: 'Shop' },
  Event: { zhs: '事件', eng: 'Event' },
  Ancient: { zhs: '先古', eng: 'Ancient' },
  Silver: { zhs: '白银', eng: 'Silver' },
  Gold: { zhs: '黄金', eng: 'Gold' },
  Prismatic: { zhs: '棱彩', eng: 'Prismatic' }
}

const poolOrder = { shared: 0, pig: 1, hextech_pig: 2, event: 3, whatif: 4, regent: 5 }
const poolLabels = {
  shared: { zhs: '共享', eng: 'Shared' },
  pig: { zhs: '猪猪', eng: 'Pig' },
  hextech_pig: { zhs: '猪猪海克斯', eng: 'Piggy Hextech' },
  event: { zhs: '事件', eng: 'Event' },
  whatif: { zhs: '假如', eng: 'What If' },
  regent: { zhs: '储君', eng: 'Regent' }
}

function labelFor(map, key) {
  const labels = map[key]
  if (!labels) return key
  return props.lang === 'zhs' ? labels.zhs : labels.eng
}

const poolChips = computed(() =>
  [...new Set(props.relicsData.map(r => r.pool).filter(Boolean))]
    .sort((a, b) => (poolOrder[a] ?? 99) - (poolOrder[b] ?? 99) || a.localeCompare(b))
    .map(key => ({ key, label: labelFor(poolLabels, key) }))
)

const filterGroupConfig = computed(() => [
  {
    key: 'pool', label: props.lang === 'zhs' ? '遗物池' : 'Relic Pool',
    chips: poolChips.value,
    filterFn: (item, key) => item.pool === key
  },
  {
    key: 'rarity', label: props.lang === 'zhs' ? '稀有度' : 'Rarity',
    chips: [
      { key: 'Starter', label: labelFor(rarityLabels, 'Starter') },
      { key: 'Common', label: labelFor(rarityLabels, 'Common') },
      { key: 'Uncommon', label: labelFor(rarityLabels, 'Uncommon') },
      { key: 'Rare', label: labelFor(rarityLabels, 'Rare') },
      { key: 'Shop', label: labelFor(rarityLabels, 'Shop') },
      { key: 'Event', label: labelFor(rarityLabels, 'Event') },
      { key: 'Ancient', label: labelFor(rarityLabels, 'Ancient') },
      { key: 'Silver', label: labelFor(rarityLabels, 'Silver') },
      { key: 'Gold', label: labelFor(rarityLabels, 'Gold') },
      { key: 'Prismatic', label: labelFor(rarityLabels, 'Prismatic') }
    ],
    filterFn: (item, key) => item.rarity === key
  }
])

const sortOptionConfig = [
  { key: 'default', label: props.lang === 'zhs' ? '默认排序' : 'Default', sortFn: null },
  { key: 'name', label: props.lang === 'zhs' ? '名称 A-Z' : 'Name A-Z',
    sortFn: (a, b) => a.title.localeCompare(b.title, props.lang === 'zhs' ? 'zh' : 'en') },
  { key: 'rarity', label: props.lang === 'zhs' ? '稀有度' : 'Rarity',
    sortFn: (a, b) => (rarityOrder[a.rarity] ?? 99) - (rarityOrder[b.rarity] ?? 99) }
]
</script>
