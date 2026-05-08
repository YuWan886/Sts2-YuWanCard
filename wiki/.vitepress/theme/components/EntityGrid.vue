<!-- EntityGrid.vue — Unified filterable grid/list for all entity types -->
<template>
  <div class="entity-grid">
    <!-- Filter bar -->
    <SearchFilterBar
      v-if="filterGroups.length"
      :filter-groups="filterGroupsWithState"
      :sort-options="sortOptions"
      :sort-value="activeSort"
      :view-mode="viewMode"
      :show-view-toggle="true"
      :filtered-count="filteredItems.length"
      :total-count="items.length"
      :clear-label="t('clear')"
      :all-label="t('all')"
      :sort-label="t('sort')"
      :results-label="t('results')"
      :grid-title="t('grid_view')"
      :list-title="t('list_view')"
      @filter-change="onFilterChange"
      @sort-change="activeSort = $event"
      @view-change="viewMode = $event"
      @clear-filters="clearFilters"
    />

    <!-- Toolbar (when no filter groups, still show sort + view + count) -->
    <div v-else class="browser-toolbar">
      <span class="browser-count">
        <strong>{{ items.length }}</strong> {{ t('results') }}
      </span>
      <div class="toolbar-right">
        <select v-if="sortOptions.length" v-model="activeSort" class="tb-select">
          <option v-for="opt in sortOptions" :key="opt.key" :value="opt.key">{{ opt.label }}</option>
        </select>
        <button class="tb-btn icon-btn" :class="{ active: viewMode === 'grid' }"
          @click="viewMode = 'grid'" :title="t('grid_view')">
          <svg width="15" height="15" viewBox="0 0 16 16" fill="currentColor">
            <rect x="1" y="1" width="6" height="6" rx="1"/><rect x="9" y="1" width="6" height="6" rx="1"/>
            <rect x="1" y="9" width="6" height="6" rx="1"/><rect x="9" y="9" width="6" height="6" rx="1"/>
          </svg>
        </button>
        <button class="tb-btn icon-btn" :class="{ active: viewMode === 'list' }"
          @click="viewMode = 'list'" :title="t('list_view')">
          <svg width="15" height="15" viewBox="0 0 16 16" fill="currentColor">
            <rect x="1" y="2" width="14" height="3" rx="1"/><rect x="1" y="7" width="14" height="3" rx="1"/>
            <rect x="1" y="12" width="14" height="3" rx="1"/>
          </svg>
        </button>
      </div>
    </div>

    <!-- Grid view -->
    <div v-if="viewMode === 'grid'" class="card-grid">
      <EntityCard
        v-for="item in filteredItems" :key="item.id"
        :id="item.id" :title="item.title"
        :description="item.desc || item.description"
        :image="item.image" :url="item.url"
        :rarity="item.rarity" :card-type="item.cardType"
        :cost="item.cost" :pool="item.pool"
        :show-cost="showCost" :show-desc="showDesc"
      />
    </div>

    <!-- List view -->
    <div v-if="viewMode === 'list'" class="card-list-view">
      <div v-for="item in filteredItems" :key="item.id"
        class="card-list-row" @click="goTo(item.url)">
        <img v-if="item.image" :src="item.image" :alt="item.title" loading="lazy" />
        <div v-else class="no-image-sm"></div>
        <div class="list-info">
          <span class="list-title">{{ item.title }}</span>
          <span class="list-desc">{{ item.desc || item.description }}</span>
        </div>
        <div class="list-meta">
          <span v-if="showCost && item.cost !== undefined" class="cost-orb">{{ costLabel(item.cost) }}</span>
          <span v-if="item.rarity" :class="rarityBadge(item.rarity)">{{ item.rarity }}</span>
          <span v-if="item.cardType" :class="typeBadge(item.cardType)">{{ item.cardType }}</span>
        </div>
      </div>
    </div>

    <!-- Empty -->
    <div v-if="filteredItems.length === 0" class="search-result-empty">
      {{ t('no_matches') }}
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import SearchFilterBar from './SearchFilterBar.vue'
import EntityCard from './EntityCard.vue'

const props = defineProps({
  items: { type: Array, required: true },
  lang: { type: String, default: 'zhs' },
  /** Filter groups config: [{ key, label, chips: [{ key, label }] }] */
  filterGroups: { type: Array, default: () => [] },
  /** Sort options: [{ key, label }] */
  sortOptions: { type: Array, default: () => [] },
  showCost: { type: Boolean, default: true },
  showDesc: { type: Boolean, default: true }
})

const activeSort = ref('')
const viewMode = ref('grid')

// Active filter states — stored as { groupKey: chipKey }
const activeFilters = ref({})

function onFilterChange(groupKey, chipKey) {
  if (!chipKey) {
    delete activeFilters.value[groupKey]
    activeFilters.value = { ...activeFilters.value }
  } else {
    activeFilters.value = { ...activeFilters.value, [groupKey]: chipKey }
  }
}

function clearFilters() {
  activeFilters.value = {}
}

function costLabel(c) {
  if (c === undefined || c === null) return ''
  return c >= 0 ? String(c) : 'X'
}
function rarityBadge(r) { return r ? `rarity-badge rarity-${r.toLowerCase()}` : '' }
function typeBadge(t) { return t ? `rarity-badge type-${t.toLowerCase()}` : '' }

// I18n labels
const labels = computed(() => ({
  clear: props.lang === 'zhs' ? '清除' : 'Clear',
  all: props.lang === 'zhs' ? '全部' : 'All',
  sort: props.lang === 'zhs' ? '排序' : 'Sort',
  results: props.lang === 'zhs' ? '个' : '',
  grid_view: props.lang === 'zhs' ? '画廊视图' : 'Grid view',
  list_view: props.lang === 'zhs' ? '列表视图' : 'List view',
  no_matches: props.lang === 'zhs' ? '没有匹配的结果' : 'No items match the filters.'
}))
function t(k) { return labels.value[k] || k }

// Build filter groups with active state
const filterGroupsWithState = computed(() =>
  props.filterGroups.map(g => ({
    ...g,
    chips: g.chips.map(c => ({
      ...c,
      active: activeFilters.value[g.key] === c.key
    }))
  }))
)

// Filtered items
const filteredItems = computed(() => {
  let result = props.items

  // Apply active filters
  for (const [groupKey, chipKey] of Object.entries(activeFilters.value)) {
    // Find the filter function from the group config
    const group = props.filterGroups.find(g => g.key === groupKey)
    if (group && group.filterFn) {
      result = result.filter(item => group.filterFn(item, chipKey))
    }
  }

  // Sort
  if (activeSort.value) {
    const opt = props.sortOptions.find(o => o.key === activeSort.value)
    if (opt && opt.sortFn) {
      result = [...result].sort(opt.sortFn)
    }
  }

  return result
})

function goTo(url) { window.location.href = url }
</script>
