---
layout: home
---

<script setup>
import { onMounted } from 'vue'

onMounted(() => {
  const lang = navigator.language || ''
  const base = '/Sts2-YuWanCard'
  if (lang.startsWith('zh')) {
    window.location.replace(base + '/zhs/')
  } else {
    window.location.replace(base + '/eng/')
  }
})
</script>
