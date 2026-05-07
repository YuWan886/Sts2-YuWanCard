<!-- GlobalSearch.vue — Modal overlay search triggered by button or Ctrl+K -->
<template>
  <!-- Trigger button -->
  <button class="search-trigger" :class="{ 'search-trigger--icon': iconOnly }"
    @click="open" :title="t('search')">
    <svg v-if="iconOnly" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
    </svg>
    <template v-else>
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
        stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="opacity:0.5">
        <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
      </svg>
      <span>{{ t('search_placeholder') }}</span>
      <kbd>{{ shortcutLabel }}</kbd>
    </template>
  </button>

  <!-- Modal overlay -->
  <Teleport to="body">
    <div v-if="isOpen" class="search-overlay" @click.self="close">
      <div class="search-modal" @click.stop>
        <div class="search-modal-header">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
            stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="search-icon">
            <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
          </svg>
          <input ref="inputRef" type="text" v-model="query" :placeholder="t('search_placeholder')"
            @input="onSearch" @keydown="onKeydown" autocomplete="off" />
          <button class="close-btn" @click="close" :aria-label="t('close')">&times;</button>
        </div>

        <div class="search-modal-body">
          <!-- Loading -->
          <div v-if="loading" class="search-result-empty">{{ t('loading') }}</div>

          <!-- Results -->
          <template v-else-if="results.length > 0">
            <a v-for="(item, i) in results" :key="item.id"
              :href="item.url" class="card-list-row"
              :class="{ 'search-row-active': i === activeIndex }"
              @click="close" @mouseenter="activeIndex = i">
              <img v-if="item.image" :src="item.image" :alt="item.title" loading="lazy" />
              <div v-else class="no-image-sm"></div>
              <div class="list-info">
                <span class="list-title">{{ item.title }}</span>
                <span class="list-desc">{{ truncate(item.description, 100) }}</span>
              </div>
              <div class="list-meta">
                <span :class="rarityBadgeClass(item.rarity)" v-if="item.rarity">{{ item.rarity }}</span>
                <span :class="typeBadgeClass(item.cardType)" v-if="item.cardType">{{ item.cardType }}</span>
                <span class="cost-orb cost-orb-sm" v-if="item.cost !== undefined">{{ item.cost }}</span>
              </div>
            </a>
          </template>

          <!-- No results -->
          <div v-else-if="query" class="search-result-empty">{{ t('no_results') }}</div>

          <!-- Empty state -->
          <div v-else class="search-result-empty">{{ t('start_typing') }}</div>
        </div>

        <div class="search-modal-footer">
          <kbd>Esc</kbd> {{ t('to_close') }}
          <span style="margin:0 8px;color:var(--border-default)">|</span>
          <kbd>Enter</kbd> {{ t('to_open') }}
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, nextTick, onMounted, onUnmounted } from 'vue'
import Fuse from 'fuse.js'

const props = defineProps({
  lang: { type: String, default: 'zhs' },
  iconOnly: { type: Boolean, default: false }
})

const labels = computed(() => ({
  search: props.lang === 'zhs' ? '搜索' : 'Search',
  search_placeholder: props.lang === 'zhs' ? '搜索卡牌、遗物、能力...' : 'Search cards, relics, powers...',
  loading: props.lang === 'zhs' ? '加载中...' : 'Loading...',
  no_results: props.lang === 'zhs' ? '未找到结果' : 'No results found',
  start_typing: props.lang === 'zhs' ? '输入关键词开始搜索' : 'Start typing to search',
  to_close: props.lang === 'zhs' ? '关闭' : 'to close',
  to_open: props.lang === 'zhs' ? '打开' : 'to open',
  close: props.lang === 'zhs' ? '关闭搜索' : 'Close search'
}))
const t = (k) => labels.value[k] || k

const shortcutLabel = ref('Ctrl+K')

const isOpen = ref(false)
const query = ref('')
const results = ref([])
const activeIndex = ref(-1)
const loading = ref(true)
const inputRef = ref(null)
let fuse = null
let allData = []

async function loadIndex() {
  try {
    const baseUrl = import.meta.env.BASE_URL || '/'
    const resp = await fetch(`${baseUrl}assets/data/search_index_${props.lang}.json`)
    allData = await resp.json()
    fuse = new Fuse(allData, {
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
    console.error('GlobalSearch: failed to load index', e)
  }
  loading.value = false
}

onMounted(() => {
  loadIndex()
  document.addEventListener('keydown', handleGlobalKey)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleGlobalKey)
})

function handleGlobalKey(e) {
  if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
    e.preventDefault()
    open()
  }
  if (e.key === 'Escape' && isOpen.value) {
    close()
  }
}

async function open() {
  isOpen.value = true
  query.value = ''
  results.value = []
  activeIndex.value = -1
  await nextTick()
  inputRef.value?.focus()
}

function close() {
  isOpen.value = false
}

function onSearch() {
  if (!fuse || !query.value.trim()) {
    results.value = []
    activeIndex.value = -1
    return
  }
  results.value = fuse.search(query.value).slice(0, 20).map(r => r.item)
  activeIndex.value = results.value.length > 0 ? 0 : -1
}

function onKeydown(e) {
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    if (activeIndex.value < results.value.length - 1) activeIndex.value++
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    if (activeIndex.value > 0) activeIndex.value--
  } else if (e.key === 'Enter') {
    e.preventDefault()
    if (activeIndex.value >= 0 && results.value[activeIndex.value]) {
      window.location.href = results.value[activeIndex.value].url
      close()
    }
  } else if (e.key === 'Escape') {
    close()
  }
}

function truncate(text, len) {
  if (!text) return ''
  return text.length > len ? text.substring(0, len) + '...' : text
}

function rarityBadgeClass(rarity) {
  return rarity ? `badge badge-rarity-${rarity.toLowerCase()}` : ''
}
function typeBadgeClass(cardType) {
  return cardType ? `badge badge-type-${cardType.toLowerCase()}` : ''
}

// Detect platform for shortcut display
onMounted(() => {
  shortcutLabel.value = navigator.platform.includes('Mac') ? '⌘K' : 'Ctrl+K'
})
</script>

<style scoped>
.search-trigger {
  display: inline-flex; align-items: center; gap: 8px;
  padding: 6px 16px; border-radius: var(--radius-full);
  border: 1px solid var(--border-default);
  background: var(--bg-card); color: var(--text-muted);
  cursor: pointer; font-size: 0.82rem; font-weight: 500;
  transition: all var(--transition-fast);
}
.search-trigger:hover {
  border-color: var(--accent-gold); color: var(--text-primary);
}
.search-trigger kbd {
  padding: 1px 6px; border-radius: 4px;
  background: var(--bg-primary); color: var(--text-muted);
  font-size: 0.7rem; font-family: var(--font-mono);
  border: 1px solid var(--border-subtle);
}

.search-trigger--icon {
  padding: 6px 8px; border: none; background: transparent;
  border-radius: var(--radius-md);
}
.search-trigger--icon:hover { background: var(--bg-card); }

.search-row-active {
  background: var(--bg-card-hover) !important;
  border-color: var(--accent-gold) !important;
}
</style>
