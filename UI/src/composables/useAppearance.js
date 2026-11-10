import { ref, watch } from 'vue'

const storageKey = 'hoc-vui-color-choice'
export const appearanceOptions = [
  { id: 'auto', label: 'Theo môn học', description: 'Mỗi môn một màu riêng' },
  { id: 'toan', label: 'Tím', description: 'Ấm áp và nhẹ nhàng' },
  { id: 'tieng-anh', label: 'Xanh', description: 'Tươi mát và dễ chịu' },
  { id: 'tieng-viet', label: 'Hồng', description: 'Mềm mại và gần gũi' },
  { id: 'navy', label: 'Xanh navy', description: 'Gọn gàng và hiện đại' },
  { id: 'emerald', label: 'Ngọc lục bảo', description: 'Sâu lắng và thanh lịch' },
  { id: 'plum', label: 'Tím mận', description: 'Tinh tế và khác biệt' },
  { id: 'wine', label: 'Đỏ rượu', description: 'Ấm áp và sang trọng' }
]
const choices = appearanceOptions.map(option => option.id)
const previousChoices = { sage: 'emerald', peach: 'wine', ocean: 'navy', caramel: 'plum' }
const validChoice = value => {
  const choice = previousChoices[value] || value
  return choices.includes(choice) ? choice : 'auto'
}
const readChoice = () => {
  try { return validChoice(localStorage.getItem(storageKey)) } catch { return 'auto' }
}
const colorChoice = ref(readChoice())

watch(colorChoice, value => {
  if (typeof document !== 'undefined') document.documentElement.dataset.colorChoice = value
  try { localStorage.setItem(storageKey, value) } catch { /* Keep the selection for this session. */ }
}, { immediate: true, flush: 'sync' })

if (typeof window !== 'undefined') {
  window.addEventListener('storage', event => {
    if (event.key === storageKey || event.key === null) colorChoice.value = readChoice()
  })
}

export const useAppearance = () => ({
  colorChoice,
  setColorChoice: value => { colorChoice.value = validChoice(value) }
})
