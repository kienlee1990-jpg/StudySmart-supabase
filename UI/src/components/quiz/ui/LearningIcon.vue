<template>
  <svg class="learning-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path v-for="(path, index) in paths[name] || paths.book" :key="index" :d="path" /></svg>
</template>

<script setup>
defineProps({ name: { type: String, default: 'book' } })
const paths = {
  math: ['M4 20h16L4 4v16Z', 'M8 16h4l-4-4v4Z', 'm4 9 2-2m2 6 2-2m2 6 2-2'],
  english: ['M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z', 'M3 12h18M12 3c5 5 5 13 0 18-5-5-5-13 0-18ZM5 6.5c4 2 10 2 14 0M5 17.5c4-2 10-2 14 0'],
  book: ['M12 5v15M3 4c3-1 6-1 9 1 3-2 6-2 9-1v15c-3-1-6-1-9 1-3-2-6-2-9-1V4Z', 'M6 8h3m-3 4h3m6-4h3m-3 4h3'],
  tower: ['M8 21V10h8v11M6 21h12M6 10h12l-2-4H8l-2 4ZM10 6V3h4v3M11 14h2m-2 3h2'],
  quiz: ['M9 4H5v17h14V4h-4M9 3h6v4H9V3Z', 'm8 12 1 1 2-2m2 1h3m-8 5 1 1 2-2m2 1h3'],
  clock: ['M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z', 'M12 7v5l3 2'],
  spark: ['m12 3 2.6 6.4L21 12l-6.4 2.6L12 21l-2.6-6.4L3 12l6.4-2.6L12 3Z'],
  trophy: ['M8 3h8v7a4 4 0 0 1-8 0V3Z', 'M8 5H4v3a4 4 0 0 0 4 4m8-7h4v3a4 4 0 0 1-4 4M12 14v6m-4 1h8'],
  arrow: ['M4 12h15m-6-6 6 6-6 6'],
  lock: ['M7 10V7a5 5 0 0 1 10 0v3M5 10h14v11H5V10Zm7 5v2'],
  play: ['m9 5 11 7-11 7V5Z'],
  star: ['m12 3 2.8 5.8 6.4.9-4.6 4.5 1.1 6.3-5.7-3-5.7 3 1.1-6.3-4.6-4.5 6.4-.9L12 3Z'],
  check: ['m5 12 4 4L19 6'],
  compass: ['M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z', 'm16 8-3 5-5 3 3-5 5-3Z'],
  bolt: ['m13 2-9 12h7l-1 8 10-13h-7l0-7Z']
}
</script>

<style scoped>
.learning-icon{display:inline-block;width:1em;height:1em;flex-shrink:0;vertical-align:-.15em}
</style>
