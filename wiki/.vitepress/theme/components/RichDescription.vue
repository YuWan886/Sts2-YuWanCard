<!-- RichDescription.vue — BBCode to formatted HTML renderer -->
<template>
  <span class="rich-desc" v-html="rendered" />
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  text: { type: String, default: '' },
  /** Optional map of variable name → string value for {Name} resolution */
  variables: { type: Object, default: () => ({}) }
})

const tagStyles = {
  gold: 'color:var(--accent-gold);font-weight:600',
  blue: 'color:var(--accent-blue);font-weight:600',
  red: 'color:var(--accent-red);font-weight:600',
  green: 'color:var(--accent-green);font-weight:600',
  purple: 'color:var(--accent-purple);font-weight:600',
  i: 'font-style:italic',
  b: 'font-weight:700'
}

const animClasses = {
  jitter: 'animate-jitter',
  sine: 'animate-sine',
  thinky_dots: 'animate-thinky'
}

// Variable lookup: tries exact name, then strips/adds "Power" suffix
function firstVar() {
  if (!props.variables) return undefined
  const keys = Object.keys(props.variables).filter(k => !k.startsWith('_upg_'))
  return keys.length > 0 ? props.variables[keys[0]] : undefined
}

function lookupVar(name) {
  if (!props.variables) return undefined
  if (props.variables[name] !== undefined) return props.variables[name]
  // Localization uses "VulnerablePower" but parser stores "Vulnerable"
  if (name.endsWith('Power')) return props.variables[name.replace(/Power$/, '')]
  // Parser may store with "Power" prefix (e.g. artifact counts)
  const withPower = props.variables[`${name}Power`]
  if (withPower !== undefined) return withPower
  // {Amount} is a generic reference to the primary/only variable
  if (name === 'Amount') return firstVar()
  return undefined
}

function resolveVars(text) {
  if (!props.variables || Object.keys(props.variables).length === 0) return text
  return text.replace(/\{(\w+)\}/g, (_, name) => {
    const v = lookupVar(name)
    return v !== undefined ? v : `{${name}}`
  })
}

function resolveUpgradeVars(text) {
  // Handle {Name:diff()} — shows base (upgraded) e.g. "6 (9)"
  return text.replace(/\{(\w+):diff\(\)\}/g, (_, name) => {
    const base = lookupVar(name)
    if (base === undefined) return `{${name}:diff()}`
    const delta = lookupVar(`_upg_${name}`) || props.variables[`_upg_${name.replace(/Power$/, '')}`]
    if (delta !== undefined) {
      const upgraded = parseInt(base) + parseInt(delta)
      return `${base} <span class="stat-upgrade">(${upgraded})</span>`
    }
    return base
  })
}

function bbcodeToHtml(text) {
  if (!text) return ''

  let result = text
    .replace(/\\n/g, '\n')
    .replace(/NL/g, '\n')

  // Inline tags with CSS
  for (const [tag, style] of Object.entries(tagStyles)) {
    const openRe = new RegExp(`\\[${tag}\\]`, 'gi')
    const closeRe = new RegExp(`\\[/${tag}\\]`, 'gi')
    result = result.replace(openRe, `<span style="${style}">`).replace(closeRe, '</span>')
  }

  // Animation tags
  for (const [tag, cls] of Object.entries(animClasses)) {
    const openRe = new RegExp(`\\[${tag}\\]`, 'gi')
    const closeRe = new RegExp(`\\[/${tag}\\]`, 'gi')
    result = result.replace(openRe, `<span class="${cls}">`).replace(closeRe, '</span>')
  }

  // Font size
  result = result.replace(/\[font_size=(\d+)\](.*?)\[\/font_size\]/gi,
    (_, size, inner) => `<span style="font-size:${size}px">${inner}</span>`)

  return result
}

function newlinesToBr(text) {
  // Convert actual newlines to <br> for HTML rendering
  return text.replace(/\n/g, '<br>')
}

const rendered = computed(() => {
  let t = props.text
  t = resolveVars(t)
  t = resolveUpgradeVars(t)

  // Handle {IfUpgraded:show:UPGRADED|NORMAL} — supports empty sides
  t = t.replace(/\{IfUpgraded:show:(.*?)\|(.*?)\}/g,
    (_, upgraded, normal) => `<span class="if-upgraded">${upgraded} / ${normal}</span>`)

  // Catch-all {Name:format()} fallback — already handled above for diff()
  t = t.replace(/\{(\w+):(\w+)\(\)\}/g, (match, name, format) => {
    const v = lookupVar(name)
    if (v !== undefined) return String(v)
    return match
  })

  t = bbcodeToHtml(t)
  t = newlinesToBr(t)
  return t
})
</script>

<style>
.rich-desc { line-height: 1.8; }
.rich-desc .stat-upgrade {
  color: var(--accent-green); font-size: 0.85em; margin-left: 2px;
}
.rich-desc .if-upgraded {
  color: var(--accent-gold); font-weight: 500;
}
</style>
