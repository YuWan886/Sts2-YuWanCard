<!-- EntityCard.vue — Unified card tile for any entity type -->
<template>
  <a :href="url" class="entity-card" :class="compact ? 'entity-card--compact' : ''">
    <!-- Image -->
    <div class="card-image-wrapper">
      <img v-if="image" :src="image" :alt="title" class="card-image" loading="lazy" />
      <div v-else class="no-image" style="position:static"></div>
      <!-- Cost badge (top-right) -->
      <span v-if="showCost" class="card-cost-badge">
        <span class="cost-orb">{{ costLabel }}</span>
      </span>
    </div>

    <!-- Body -->
    <div class="card-body">
      <div class="card-name">{{ title }}</div>
      <div class="card-meta">
        <span v-if="rarity" :class="rarityBadge(rarity)">{{ rarity }}</span>
        <span v-if="cardType" :class="typeBadge(cardType)">{{ cardType }}</span>
        <span v-if="pool && !rarity" :class="poolBadge(pool)">{{ pool }}</span>
      </div>
      <div v-if="showDesc && description" class="card-desc">{{ description }}</div>
    </div>
  </a>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  id: { type: String, required: true },
  title: { type: String, default: '' },
  description: { type: String, default: '' },
  image: { type: String, default: '' },
  url: { type: String, default: '' },
  rarity: { type: String, default: '' },
  cardType: { type: String, default: '' },
  cost: { type: Number, default: undefined },
  pool: { type: String, default: '' },
  showCost: { type: Boolean, default: true },
  showDesc: { type: Boolean, default: true },
  compact: { type: Boolean, default: false }
})

const costLabel = computed(() => {
  if (props.cost === undefined || props.cost === null) return ''
  return props.cost >= 0 ? String(props.cost) : 'X'
})

function rarityBadge(r) {
  return r ? `rarity-badge rarity-${r.toLowerCase()}` : ''
}
function typeBadge(t) {
  return t ? `rarity-badge type-${t.toLowerCase()}` : ''
}
function poolBadge(p) {
  return `rarity-badge rarity-common`
}
</script>
