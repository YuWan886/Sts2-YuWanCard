<!-- SearchFilterBar.vue — Reusable filter bar with dropdown groups, sort, view toggle -->
<template>
  <div class="filter-bar">
    <!-- Text search input -->
    <label v-if="showTextSearch" class="filter-group filter-search-group">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
        stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="search-input-icon">
        <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
      </svg>
      <input
        type="text"
        class="filter-search-input"
        :value="textSearch"
        :placeholder="searchPlaceholder"
        @input="$emit('textSearchChange', ($event.target).value)"
        autocomplete="off"
      />
      <button v-if="textSearch" class="filter-search-clear" @click="$emit('textSearchChange', '')">&times;</button>
    </label>

    <!-- Filter dropdown groups -->
    <label v-for="group in filterGroups" :key="group.key" class="filter-group">
      <span class="filter-label">{{ group.label }}</span>
      <select
        class="tb-select filter-select"
        :value="getActiveChipKey(group)"
        @change="$emit('filterChange', group.key, ($event.target).value)">
        <option value="">{{ allLabel }}</option>
        <option v-for="chip in group.chips" :key="chip.key" :value="chip.key">
          {{ chip.label }}
        </option>
      </select>
    </label>

    <!-- Clear button -->
    <button v-if="hasActiveFilters || textSearch" class="tb-btn"
      @click="clearAll">
      {{ clearLabel }}
    </button>

    <!-- Spacer pushes toolbar items to the right -->
    <span style="flex:1"></span>

    <!-- Sort dropdown -->
    <template v-if="sortOptions && sortOptions.length">
      <span class="filter-label">{{ sortLabel }}</span>
      <select class="tb-select" :value="sortValue"
        @change="$emit('sortChange', ($event.target).value)">
        <option v-for="opt in sortOptions" :key="opt.key" :value="opt.key">{{ opt.label }}</option>
      </select>
    </template>

    <!-- View toggle -->
    <template v-if="showViewToggle">
      <button class="tb-btn icon-btn" :class="{ active: viewMode === 'grid' }"
        @click="$emit('viewChange', 'grid')" :title="gridTitle">
        <svg width="15" height="15" viewBox="0 0 16 16" fill="currentColor">
          <rect x="1" y="1" width="6" height="6" rx="1"/><rect x="9" y="1" width="6" height="6" rx="1"/>
          <rect x="1" y="9" width="6" height="6" rx="1"/><rect x="9" y="9" width="6" height="6" rx="1"/>
        </svg>
      </button>
      <button class="tb-btn icon-btn" :class="{ active: viewMode === 'list' }"
        @click="$emit('viewChange', 'list')" :title="listTitle">
        <svg width="15" height="15" viewBox="0 0 16 16" fill="currentColor">
          <rect x="1" y="2" width="14" height="3" rx="1"/><rect x="1" y="7" width="14" height="3" rx="1"/>
          <rect x="1" y="12" width="14" height="3" rx="1"/>
        </svg>
      </button>
    </template>
  </div>

  <!-- Result count -->
  <div v-if="showCount" class="browser-toolbar">
    <span class="browser-count">
      <strong>{{ filteredCount }}</strong> / {{ totalCount }} {{ resultsLabel }}
    </span>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  /** Array of filter groups: { key, label, chips: [{ key, label, active }] } */
  filterGroups: { type: Array, default: () => [] },
  /** Sort options: [{ key, label }] */
  sortOptions: { type: Array, default: () => [] },
  sortValue: { type: String, default: '' },
  viewMode: { type: String, default: 'grid' },
  showViewToggle: { type: Boolean, default: false },
  filteredCount: { type: Number, default: 0 },
  totalCount: { type: Number, default: 0 },
  showCount: { type: Boolean, default: true },
  // i18n
  clearLabel: { type: String, default: 'Clear' },
  allLabel: { type: String, default: 'All' },
  sortLabel: { type: String, default: 'Sort' },
  resultsLabel: { type: String, default: 'results' },
  gridTitle: { type: String, default: 'Grid view' },
  listTitle: { type: String, default: 'List view' },
  // text search
  showTextSearch: { type: Boolean, default: true },
  textSearch: { type: String, default: '' },
  searchPlaceholder: { type: String, default: 'Search...' }
})

const emit = defineEmits(['filterChange', 'sortChange', 'viewChange', 'clearFilters', 'textSearchChange'])

const hasActiveFilters = computed(() =>
  props.filterGroups.some(g => g.chips.some(c => c.active))
)

function clearAll() {
  emit('clearFilters')
  emit('textSearchChange', '')
}

function getActiveChipKey(group) {
  return group.chips.find(c => c.active)?.key ?? ''
}
</script>
