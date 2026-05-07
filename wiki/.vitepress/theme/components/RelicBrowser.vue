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

const rarityOrder = { Starter: 0, Common: 1, Uncommon: 2, Shop: 3, Rare: 4, Boss: 5, Ancient: 6, Event: 7 }

const filterGroupConfig = computed(() => [
  {
    key: 'rarity', label: props.lang === 'zhs' ? '稀有度' : 'Rarity',
    chips: [
      { key: 'Starter', label: 'Starter' },
      { key: 'Common', label: 'Common' },
      { key: 'Uncommon', label: 'Uncommon' },
      { key: 'Rare', label: 'Rare' },
      { key: 'Boss', label: 'Boss' },
      { key: 'Shop', label: 'Shop' },
      { key: 'Ancient', label: 'Ancient' }
    ],
    filterFn: (item, key) => item.rarity === key
  },
  {
    key: 'pool', label: props.lang === 'zhs' ? '来源' : 'Pool',
    chips: [
      { key: 'pig', label: 'Pig' },
      { key: 'shared', label: 'Shared' }
    ],
    filterFn: (item, key) => item.pool === key
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
