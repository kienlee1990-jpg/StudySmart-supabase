<template>
  <div :id="`tools-${tableId}`" class="table-tools">
    <button type="button" class="table-tools-trigger" @click="toggleMenu">
      <i class="bi bi-layout-three-columns"></i>
      <span>Tùy chọn cột</span>
    </button>

    <div v-if="open" class="table-tools-menu">
      <div class="table-tools-head">
        <strong>Cột hiển thị</strong>
        <div class="table-tools-actions">
          <button type="button" @click="showAll">Tất cả</button>
          <button type="button" @click="hideAll">Bỏ hết</button>
        </div>
      </div>

      <label v-for="column in columns" :key="column.index" class="table-tools-option">
        <input v-model="visibleColumns" type="checkbox" :value="column.index" />
        <span>{{ column.label }}</span>
      </label>
    </div>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  tableId: {
    type: String,
    required: true
  }
})

const open = ref(false)
const columns = ref([])
const visibleColumns = ref([])

const hiddenSet = computed(() => new Set(columns.value.map(column => column.index).filter(index => !visibleColumns.value.includes(index))))

const getTable = () => document.getElementById(props.tableId)

const collectColumns = () => {
  const table = getTable()
  if (!table) return

  const headerCells = Array.from(table.querySelectorAll('thead th'))
  columns.value = headerCells.map((headerCell, index) => ({
    index,
    label: headerCell.textContent?.trim() || `Cột ${index + 1}`
  }))

  if (!visibleColumns.value.length || visibleColumns.value.length > columns.value.length) {
    visibleColumns.value = columns.value.map(column => column.index)
  } else {
    const existing = new Set(columns.value.map(column => column.index))
    visibleColumns.value = visibleColumns.value.filter(index => existing.has(index))
  }

  applyVisibility()
}

const applyVisibility = () => {
  const table = getTable()
  if (!table) return

  const rows = Array.from(table.querySelectorAll('tr'))
  rows.forEach(row => {
    const cells = Array.from(row.children)
    cells.forEach((cell, index) => {
      cell.style.display = hiddenSet.value.has(index) ? 'none' : ''
    })
  })
}

const toggleMenu = async () => {
  open.value = !open.value
  if (open.value) {
    await nextTick()
    collectColumns()
  }
}

const showAll = () => {
  visibleColumns.value = columns.value.map(column => column.index)
}

const hideAll = () => {
  visibleColumns.value = []
}

const handleDocumentClick = (event) => {
  const target = event.target
  if (!(target instanceof Node)) return
  const host = document.getElementById(`tools-${props.tableId}`)
  if (host && !host.contains(target)) {
    open.value = false
  }
}

watch(visibleColumns, () => {
  applyVisibility()
}, { deep: true })

onMounted(async () => {
  await nextTick()
  collectColumns()
  document.addEventListener('click', handleDocumentClick)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick)
})
</script>

<style>
.table-tools{position:sticky;left:0;display:flex;justify-content:flex-end;align-items:center;width:100%;margin-bottom:14px;padding-left:12px;z-index:5}
.table-tools-trigger{display:inline-flex;align-items:center;gap:8px;padding:9px 15px;border:1px solid #d7deea;border-radius:999px;background:linear-gradient(180deg,#ffffff 0%,#f8fbff 100%);color:#0f172a;font-size:13px;font-weight:700;box-shadow:0 10px 22px rgba(15,23,42,.06);transition:all .18s ease}
.table-tools-trigger:hover{background:#eef6ff;border-color:#bfdbfe;transform:translateY(-1px)}
.table-tools-menu{position:absolute;right:0;top:calc(100% + 8px);width:300px;max-height:360px;overflow:auto;padding:14px;background:#fff;border:1px solid #e2e8f0;border-radius:18px;box-shadow:0 18px 40px rgba(15,23,42,.16);z-index:40}
.table-tools-head{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:10px}
.table-tools-actions{display:flex;gap:10px}
.table-tools-actions button{border:none;background:transparent;color:#2563eb;font-size:12px;font-weight:700}
.table-tools-option{display:flex;align-items:center;gap:10px;padding:9px 6px;font-size:14px;color:#0f172a;border-radius:10px}
.table-tools-option:hover{background:#f8fafc}
.table-wrapper,.table-responsive{width:100% !important;overflow-x:auto !important;padding-bottom:4px}
table.managed-table{width:100% !important;border-collapse:separate !important;border-spacing:0 !important;background:#fff !important;border:1px solid #e2e8f0 !important;border-radius:18px !important;overflow:hidden !important}
table.managed-table thead th{position:sticky !important;top:0 !important;background:linear-gradient(180deg,#1d4ed8 0%,#2563eb 52%,#3b82f6 100%) !important;color:#fff !important;font-size:12px !important;font-weight:800 !important;text-transform:uppercase !important;letter-spacing:.04em !important;padding:15px 16px !important;border-bottom:1px solid rgba(191,219,254,.45) !important;white-space:nowrap !important;z-index:2 !important;box-shadow:inset 0 1px 0 rgba(255,255,255,.16),inset 0 -1px 0 rgba(15,23,42,.12) !important}
table.managed-table thead th:first-child{border-top-left-radius:18px !important}
table.managed-table thead th:last-child{border-top-right-radius:18px !important}
table.managed-table thead th::after{content:"";position:absolute;left:12px;right:12px;bottom:0;height:1px;background:rgba(255,255,255,.24)}
table.managed-table thead th:not(:last-child){border-right:1px solid rgba(255,255,255,.14) !important}
table.managed-table tbody td{padding:14px 16px !important;border-bottom:1px solid #eef2f7 !important;vertical-align:middle !important;color:#334155 !important;background:#fff !important}
table.managed-table tbody tr:nth-child(even) td{background:#fcfdff !important}
table.managed-table tbody tr:hover td{background:#f8fbff !important}
table.managed-table tbody tr:last-child td{border-bottom:none !important}
table.managed-table td .sub-label,
table.managed-table td .muted-line,
table.managed-table td .small,
table.managed-table td small{display:block !important;color:#64748b !important;font-size:12px !important;line-height:1.45 !important;margin-top:4px !important}
table.managed-table td strong{color:#0f172a !important}
table.managed-table .badge{font-weight:700 !important}
table.managed-table td.text-center,table.managed-table th.text-center{text-align:center !important}
table.managed-table td.text-right,table.managed-table th.text-right{text-align:right !important}
table.managed-table td .chip-wrap{display:flex !important;flex-wrap:wrap !important;gap:6px !important}
</style>
