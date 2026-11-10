<template>
  <details ref="picker" class="appearance-picker" @keydown.esc.stop.prevent="closePicker(true)">
    <summary ref="trigger" class="appearance-trigger" aria-label="Màu giao diện" title="Đổi màu giao diện">
      <span class="appearance-mark" aria-hidden="true"><i></i><i></i><i></i></span><span class="appearance-label">Màu giao diện</span><span class="appearance-chevron" aria-hidden="true">⌄</span>
    </summary>
    <div class="appearance-panel">
      <fieldset><legend>Màu giao diện</legend><p>Chọn tông màu em yêu thích.</p>
        <label v-for="option in options" :key="option.id" class="appearance-option" :class="{ selected: colorChoice === option.id }">
          <input type="radio" name="appearance-color" :value="option.id" :checked="colorChoice === option.id" @change="setColorChoice(option.id)" />
          <span class="color-preview" :class="option.id" aria-hidden="true"><template v-if="option.id === 'auto'"><i></i><i></i><i></i></template></span>
          <span class="option-copy"><strong>{{ option.label }}</strong><small>{{ option.description }}</small></span><span class="option-check" aria-hidden="true">{{ colorChoice === option.id ? '✓' : '' }}</span>
        </label>
      </fieldset>
      <div class="appearance-footnote" role="status">{{ colorChoice === 'auto' ? 'Màu tự đổi theo môn học.' : 'Đang áp dụng cho toàn bộ giao diện.' }}<span>Đã lưu lựa chọn trên thiết bị này.</span></div>
    </div>
  </details>
</template>

<script setup>
import { onMounted, onUnmounted, ref } from 'vue'
import { useAppearance, appearanceOptions } from '../../../composables/useAppearance'

const { colorChoice, setColorChoice } = useAppearance()
const picker = ref(null)
const trigger = ref(null)
const options = appearanceOptions
const closePicker = restoreFocus => {
  if (!picker.value?.open) return
  picker.value.open = false
  if (restoreFocus) trigger.value?.focus()
}
const onOutsideClick = event => {
  if (picker.value && !picker.value.contains(event.target)) closePicker(false)
}
onMounted(() => document.addEventListener('click', onOutsideClick))
onUnmounted(() => document.removeEventListener('click', onOutsideClick))
</script>

<style scoped>
.appearance-picker{position:relative;font-family:"Be Vietnam Pro","Segoe UI",sans-serif;z-index:30}.appearance-trigger{display:flex;align-items:center;justify-content:center;gap:8px;min-height:38px;padding:8px 10px;border:1px solid var(--subject-line);border-radius:11px;background:#fff;color:var(--subject-ink);font-size:11px;font-weight:650;cursor:pointer;list-style:none;user-select:none}.appearance-trigger::-webkit-details-marker{display:none}.appearance-trigger:hover,.appearance-picker[open] .appearance-trigger{background:var(--subject-soft);border-color:var(--subject-primary)}.appearance-trigger:focus-visible{outline:3px solid var(--subject-primary);outline-offset:3px}.appearance-mark{position:relative;display:flex;width:24px;height:19px;align-items:center}.appearance-mark i{position:absolute;width:13px;height:13px;border:2px solid #fff;border-radius:50%;background:var(--palette-toan-primary)}.appearance-mark i:nth-child(2){left:6px;top:0;background:var(--palette-tieng-anh-primary)}.appearance-mark i:nth-child(3){left:11px;top:7px;background:var(--palette-tieng-viet-primary)}.appearance-chevron{font-size:14px;color:var(--subject-muted)}.appearance-panel{max-height:calc(100dvh - 140px);overflow-y:auto;overscroll-behavior:contain;scrollbar-width:thin;position:absolute;top:calc(100% + 12px);right:0;width:292px;max-width:calc(100vw - 24px);padding:18px;border:1px solid var(--subject-line);border-radius:18px;background:#fff;box-shadow:0 16px 45px #39244f1f;color:var(--subject-ink);text-align:left}.appearance-panel fieldset{min-width:0;margin:0;padding:0;border:0}.appearance-panel legend{float:none;width:auto;margin:0 0 5px;font-size:16px;line-height:1.5;font-weight:750;letter-spacing:-.02em}.appearance-panel fieldset>p{margin:0 0 15px;color:var(--subject-muted);font-size:11px;line-height:1.6}.appearance-option{position:relative;display:flex;align-items:center;gap:10px;margin-top:8px;padding:11px 10px;border:1px solid #eeebf1;border-radius:11px;cursor:pointer;transition:border-color .15s,background .15s}.appearance-option:hover,.appearance-option.selected{border-color:var(--subject-primary);background:var(--subject-soft)}.appearance-option:focus-within{outline:2px solid var(--subject-primary);outline-offset:2px}.appearance-option input{position:absolute;width:1px;height:1px;opacity:0}.color-preview{display:flex;flex:0 0 30px;width:30px;height:30px;overflow:hidden;border-radius:10px;background:var(--palette-toan-primary);box-shadow:inset 0 0 0 1px #0000000a}.color-preview.tieng-anh{background:var(--palette-tieng-anh-primary)}.color-preview.tieng-viet{background:var(--palette-tieng-viet-primary)}.color-preview.auto i{flex:1;background:var(--palette-toan-primary)}.color-preview.auto i:nth-child(2){background:var(--palette-tieng-anh-primary)}.color-preview.auto i:nth-child(3){background:var(--palette-tieng-viet-primary)}.option-copy{display:grid;flex:1;gap:3px}.option-copy strong{color:var(--subject-ink);font-size:12px;font-weight:700}.appearance-panel .option-copy small{color:var(--subject-muted)!important;font-size:10px;line-height:1.5}.option-check{width:16px;color:var(--subject-strong);font-size:15px;font-weight:750}.appearance-footnote{display:grid;gap:4px;margin-top:15px;padding-top:12px;border-top:1px solid var(--subject-line);color:var(--subject-muted);font-size:10px;line-height:1.6}.appearance-footnote span{font-size:9px}
@media(max-width:1050px){.appearance-label,.appearance-chevron{display:none}.appearance-trigger{width:38px;padding:7px}}
@media(max-width:600px){.appearance-panel{position:fixed;max-height:calc(100dvh - 90px);top:74px;right:12px;width:292px}.appearance-trigger{min-height:36px;width:36px;border-radius:10px}}
@media(prefers-reduced-motion:reduce){.appearance-option{transition:none}}
.color-preview.emerald{background:var(--palette-emerald-primary)}
.color-preview.wine{background:var(--palette-wine-primary)}
.color-preview.navy{background:var(--palette-navy-primary)}
.color-preview.plum{background:var(--palette-plum-primary)}
</style>
