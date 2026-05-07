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

const filterGroupConfig = computed(() => [
  {
    key: 'cost', label: props.lang === 'zhs' ? '费用' : 'Cost',
    chips: [
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
      { key: 'Attack', label: props.lang === 'zhs' ? '攻击' : 'Attack' },
      { key: 'Skill', label: props.lang === 'zhs' ? '技能' : 'Skill' },
      { key: 'Power', label: props.lang === 'zhs' ? '能力' : 'Power' }
    ],
    filterFn: (item, key) => item.cardType === key
  },
  {
    key: 'rarity', label: props.lang === 'zhs' ? '稀有度' : 'Rarity',
    chips: [
      { key: 'Basic', label: 'Basic' },
      { key: 'Common', label: 'Common' },
      { key: 'Uncommon', label: 'Uncommon' },
      { key: 'Rare', label: 'Rare' },
      { key: 'Ancient', label: 'Ancient' }
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
