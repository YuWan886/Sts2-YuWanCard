<template>
  <div class="search-container">
    <input
      type="text"
      class="search-input"
      v-model="query"
      :placeholder="placeholder"
      autocomplete="off"
      @input="onSearch"
    />
    <div class="search-results">
      <div v-if="loading" class="search-no-results">{{ loadingText }}</div>
      <div v-else-if="results.length === 0 && query" class="search-no-results">{{ noResults }}</div>
      <a
        v-for="item in results"
        :key="item.id"
        :href="item.url"
        class="search-result-item"
      >
        <div class="result-info">
          <div class="result-title">{{ item.title }}</div>
          <div class="result-desc">{{ truncate(item.description, 120) }}</div>
          <div class="result-type">
            {{ item.typeName }}
            <template v-if="item.rarity"> · {{ item.rarity }}</template>
            <template v-if="item.cardType"> · {{ item.cardType }}</template>
          </div>
        </div>
      </a>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import Fuse from 'fuse.js'

const props = defineProps({
  lang: { type: String, required: true }
})

const placeholder = computed(() =>
  props.lang === 'zhs' ? '搜索卡牌、遗物、能力...' : 'Search cards, relics, powers...'
)
const noResults = computed(() =>
  props.lang === 'zhs' ? '未找到结果' : 'No results found'
)
const loadingText = computed(() =>
  props.lang === 'zhs' ? '加载中...' : 'Loading...'
)

const query = ref('')
const results = ref([])
const loading = ref(true)
let fuse = null

async function loadIndex() {
  try {
    const baseUrl = import.meta.env.BASE_URL || '/'
    const url = `${baseUrl}assets/data/search_index_${props.lang}.json`
    const resp = await fetch(url)
    const data = await resp.json()
    fuse = new Fuse(data, {
      keys: [
        { name: 'title', weight: 0.5 },
        { name: 'description', weight: 0.3 },
        { name: 'keywords', weight: 0.1 },
        { name: 'typeName', weight: 0.1 }
      ],
      threshold: 0.35,
      distance: 100,
      includeScore: true,
      minMatchCharLength: 1
    })
  } catch (e) {
    console.error('Failed to load search index:', e)
  }
  loading.value = false

  // Check for query param from global search box
  const params = new URLSearchParams(window.location.search)
  const q = params.get('q')
  if (q) {
    query.value = q
    onSearch()
  }
}

onMounted(() => {
  loadIndex()
})

function onSearch() {
  if (!fuse || !query.value.trim()) {
    results.value = []
    return
  }
  results.value = fuse.search(query.value).slice(0, 30).map(r => r.item)
}

function truncate(text, len) {
  if (!text) return ''
  return text.length > len ? text.substring(0, len) + '...' : text
}

</script>
