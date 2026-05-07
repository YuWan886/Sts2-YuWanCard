<!-- SearchFilterBar.vue — Reusable filter bar with chip groups, sort, view toggle -->
<template>
  <div class="filter-bar">
    <!-- Filter chip groups -->
    <template v-for="(group, gi) in filterGroups" :key="group.key">
      <span v-if="gi > 0" class="filter-divider"></span>
      <span class="filter-label">{{ group.label }}</span>
      <button v-for="chip in group.chips" :key="chip.key"
        class="filter-chip"
        :class="{ active: chip.active }"
        @click="$emit('filterChange', group.key, chip.key)">
        {{ chip.label }}
      </button>
    </template>

    <!-- Clear button -->
    <button v-if="hasActiveFilters" class="filter-chip clear"
      @click="$emit('clearFilters')">
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
  sortLabel: { type: String, default: 'Sort' },
  resultsLabel: { type: String, default: 'results' },
  gridTitle: { type: String, default: 'Grid view' },
  listTitle: { type: String, default: 'List view' }
})

defineEmits(['filterChange', 'sortChange', 'viewChange', 'clearFilters'])

const hasActiveFilters = computed(() =>
  props.filterGroups.some(g => g.chips.some(c => c.active))
)
</script>
