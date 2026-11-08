<template>
  <QuizLayout>
    <section class="admin-head"><div><span class="eyebrow">KHU VỰC GIÁO VIÊN</span><h1>Quản lý nội dung học tập</h1><p>Ngân hàng câu hỏi, điểm số và bài giảng được lưu trên Supabase.</p></div><RouterLink to="/" class="back-link">← Về trang học sinh</RouterLink></section>
    <div v-if="error" class="notice error">{{ error }}</div><div v-if="message" class="notice success">{{ message }}</div>
    <div class="tabs"><button :class="{ active: tab === 'questions' }" @click="tab = 'questions'">Ngân hàng câu hỏi</button><button :class="{ active: tab === 'lessons' }" @click="tab = 'lessons'">Bài giảng và lý thuyết</button></div>

    <template v-if="tab === 'questions'">
      <section class="excel-import-card">
        <div class="excel-copy"><span class="excel-icon">📊</span><div><span class="source-label">NHẬP HÀNG LOẠT</span><h2>Import câu hỏi từ Excel</h2><p>Tải mẫu, điền câu hỏi và đáp án rồi chọn tệp .xlsx để lưu vào ngân hàng câu hỏi.</p></div></div>
        <div class="excel-actions"><button class="template-button" :disabled="downloadingTemplate" @click="downloadTemplate">{{ downloadingTemplate ? 'Đang tải mẫu…' : '↓ Tải file Excel mẫu' }}</button><label class="file-picker">{{ importingExcel ? 'Đang nhập…' : (selectedExcelName || 'Chọn file Excel .xlsx') }}<input ref="excelFile" type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" @change="selectExcelFile" /></label><button class="source-button" :disabled="importingExcel || !selectedExcel" @click="importExcel">{{ importingExcel ? 'Đang nhập…' : 'Import câu hỏi' }}</button></div>
        <p class="source-note">Các cột bắt buộc: Lớp, Môn, Nội dung câu hỏi, Đáp án A–D, Đáp án đúng (A/B/C/D). Cột Giải thích không bắt buộc. Tối đa 500 câu, 5 MB mỗi lần; câu hỏi được lưu trên Supabase.</p>
      </section>
      <section class="editor-card"><h2>Thêm câu hỏi mới</h2><div class="form-grid"><label>Khối<select v-model.number="question.grade"><option v-for="item in grades" :key="item" :value="item">Lớp {{ item }}</option></select></label><label>Môn<select v-model="question.subject"><option v-for="item in subjects" :key="item">{{ item }}</option></select></label></div>
        <label>Nội dung câu hỏi<textarea v-model.trim="question.content" rows="3" maxlength="4000" placeholder="Nhập câu hỏi…"></textarea></label>
        <div class="option-editor"><label v-for="(option, index) in question.options" :key="index" class="option-row"><input type="radio" name="correct-option" :value="index" v-model.number="question.correctIndex" /><span>{{ letters[index] }}</span><input v-model.trim="option.content" :placeholder="`Nội dung đáp án ${letters[index]}`" maxlength="1000" /></label></div>
        <label>Giải thích đáp án (không bắt buộc)<textarea v-model.trim="question.explanation" rows="2" maxlength="2000" placeholder="Giải thích ngắn gọn để học sinh ôn lại"></textarea></label>
        <button class="primary-button" :disabled="saving" @click="createQuestion">{{ saving ? 'Đang lưu…' : 'Lưu câu hỏi' }}</button>
      </section>
      <section class="list-card"><div class="list-heading"><h2>Ngân hàng câu hỏi</h2><span>{{ questions.length }} câu</span></div><div v-if="!questions.length" class="empty">Chưa có câu hỏi.</div><article v-for="item in questions" :key="item.id" class="question-row"><div class="item-meta">Lớp {{ item.grade }} · {{ item.subject }}</div><strong>{{ item.content }}</strong><div class="option-list"><span v-for="(option, index) in item.options" :key="option.id" :class="{ correct: option.isCorrect }">{{ letters[index] }}. {{ option.content }}</span></div><div v-if="item.explanation" class="explanation">{{ item.explanation }}</div><button class="text-button" @click="deleteQuestion(item)">{{ item.isActive ? 'Xóa / lưu trữ' : 'Đã lưu trữ' }}</button></article></section>
    </template>

    <template v-else>
      <section class="editor-card"><h2>Thêm bài giảng hoặc lý thuyết</h2><div class="form-grid"><label>Tên bài<input v-model.trim="lesson.title" maxlength="200" placeholder="Ví dụ: Cộng hai phân số khác mẫu số" /></label><label>Khối<select v-model.number="lesson.grade"><option v-for="item in grades" :key="item" :value="item">Lớp {{ item }}</option></select></label><label>Môn<select v-model="lesson.subject"><option v-for="item in subjects" :key="item">{{ item }}</option></select></label></div>
        <label>Mô tả ngắn<textarea v-model.trim="lesson.summary" rows="2" maxlength="1000" placeholder="Giới thiệu nội dung bài học"></textarea></label>
        <label>Nội dung bài học<textarea v-model.trim="lesson.content" rows="9" maxlength="20000" placeholder="Nhập lý thuyết, ví dụ và ghi chú. Có thể xuống dòng để chia mục."></textarea></label>
        <button class="primary-button" :disabled="saving" @click="createLesson">{{ saving ? 'Đang lưu…' : 'Đăng bài học' }}</button>
      </section>
      <section class="list-card"><div class="list-heading"><h2>Bài học đã đăng</h2><span>{{ lessons.length }} bài</span></div><div v-if="!lessons.length" class="empty">Chưa có bài học.</div><article v-for="item in lessons" :key="item.id" class="lesson-row"><div class="item-meta">Lớp {{ item.grade }} · {{ item.subject }}</div><strong>{{ item.title }}</strong><p>{{ item.summary }}</p><button class="text-button" @click="deleteLesson(item)">Xóa bài học</button></article></section>
    </template>
  </QuizLayout>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'

const subjects = ['Toán', 'Tiếng Anh', 'Tiếng Việt']
const grades = Array.from({ length: 8 }, (_, index) => index + 5)
const letters = ['A', 'B', 'C', 'D', 'E', 'F']
const tab = ref('questions')
const questions = ref([])
const lessons = ref([])
const saving = ref(false)
const error = ref('')
const message = ref('')
const excelFile = ref(null)
const selectedExcel = ref(null)
const selectedExcelName = ref('')
const importingExcel = ref(false)
const downloadingTemplate = ref(false)
const question = reactive({ grade: 5, subject: 'Toán', content: '', explanation: '', correctIndex: 0, options: [{ content: '' }, { content: '' }, { content: '' }, { content: '' }] })
const lesson = reactive({ title: '', summary: '', content: '', grade: 5, subject: 'Toán' })

const loadData = async () => {
  try {
    const [questionResult, lessonResult] = await Promise.all([httpClient.get('/quiz-admin/questions'), httpClient.get('/quiz-admin/lessons')])
    questions.value = questionResult.data || []
    lessons.value = lessonResult.data || []
    error.value = ''
  } catch (err) { error.value = err?.response?.status === 403 ? 'Tài khoản chưa được cấp quyền quản lý nội dung.' : 'Không tải được dữ liệu quản trị.' }
}
const createLesson = async () => {
  error.value = ''; message.value = ''
  if (!lesson.title || !lesson.content) { error.value = 'Hãy nhập tên và nội dung bài học.'; return }
  saving.value = true
  try {
    await httpClient.post('/quiz-admin/lessons', { ...lesson })
    lesson.title = ''; lesson.summary = ''; lesson.content = ''
    message.value = 'Đã đăng bài học.'; await loadData()
  } catch (err) { error.value = err?.response?.data?.message || 'Chưa lưu được bài học.' }
  finally { saving.value = false }
}
const deleteLesson = async item => {
  if (!confirm(`Xóa bài học “${item.title}”?`)) return
  try { await httpClient.delete(`/quiz-admin/lessons/${item.id}`); message.value = 'Đã xóa bài học.'; await loadData() }
  catch { error.value = 'Không xóa được bài học.' }
}
const selectExcelFile = event => {
  selectedExcel.value = event.target.files?.[0] || null
  selectedExcelName.value = selectedExcel.value?.name || ''
}
const downloadTemplate = async () => {
  downloadingTemplate.value = true; error.value = ''
  try {
    const response = await httpClient.get('/quiz-admin/questions/import-template', { responseType: 'blob' })
    const url = URL.createObjectURL(response.data)
    const link = document.createElement('a'); link.href = url; link.download = 'mau-cau-hoi.xlsx'; link.click()
    URL.revokeObjectURL(url)
  } catch { error.value = 'Không tải được file Excel mẫu.' }
  finally { downloadingTemplate.value = false }
}
const importExcel = async () => {
  if (!selectedExcel.value) { error.value = 'Hãy chọn file Excel .xlsx.'; return }
  error.value = ''; message.value = ''; importingExcel.value = true
  try {
    const form = new FormData(); form.append('file', selectedExcel.value)
    const response = await httpClient.post('/quiz-admin/questions/import-excel', form, { timeout: 30000 })
    message.value = `${response.data.message} Bỏ qua ${response.data.skippedCount} câu trùng.`
    selectedExcel.value = null; selectedExcelName.value = ''; if (excelFile.value) excelFile.value.value = ''
    await loadData()
  } catch (err) { error.value = err?.response?.data?.message || 'Không import được file Excel.' }
  finally { importingExcel.value = false }
}
const createQuestion = async () => {
  error.value = ''; message.value = ''
  if (!question.content || question.options.some(item => !item.content.trim())) { error.value = 'Hãy nhập câu hỏi và đủ nội dung 4 đáp án.'; return }
  saving.value = true
  try {
    await httpClient.post('/quiz-admin/questions', { grade: question.grade, subject: question.subject, content: question.content, explanation: question.explanation, options: question.options.map((item, index) => ({ content: item.content, isCorrect: index === question.correctIndex })) })
    question.content = ''; question.explanation = ''; question.options.forEach(item => { item.content = '' }); question.correctIndex = 0
    message.value = 'Đã lưu câu hỏi vào ngân hàng.'; await loadData()
  } catch (err) { error.value = err?.response?.data?.message || 'Chưa lưu được câu hỏi.' }
  finally { saving.value = false }
}
const deleteQuestion = async item => {
  if (!item.isActive) return
  if (!confirm('Xóa câu hỏi này? Nếu đã nằm trong đề, câu hỏi sẽ được lưu trữ thay vì xóa.')) return
  try { await httpClient.delete(`/quiz-admin/questions/${item.id}`); message.value = 'Đã cập nhật câu hỏi.'; await loadData() }
  catch { error.value = 'Không xóa được câu hỏi.' }
}
onMounted(loadData)
</script>

<style scoped>
.eyebrow{color:#747bd4;font-size:11px;font-weight:900;letter-spacing:.13em}.admin-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:22px}.admin-head h1{color:#2a3865;margin:6px 0}.admin-head p{color:#8189a0;margin:0}.back-link{color:#626fce;font-weight:800;text-decoration:none}.tabs{display:flex;gap:8px;margin:15px 0}.tabs button{border:0;border-radius:12px;padding:11px 15px;background:#e9ecf8;color:#66708d;font-weight:800}.tabs button.active{background:#626fe0;color:#fff}.editor-card,.list-card{background:white;border:1px solid #e8ecf5;border-radius:20px;padding:22px;margin:15px 0;box-shadow:0 8px 25px #25346608}.editor-card h2,.list-card h2{font-size:18px;color:#303d68;margin:0 0 16px}.form-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:12px}.editor-card>label,.form-grid label{display:block;margin:12px 0;color:#68728c;font-weight:800;font-size:12px}.editor-card input:not([type=radio]):not([type=checkbox]),.editor-card select,.editor-card textarea{display:block;width:100%;border:1px solid #e1e5ef;border-radius:11px;padding:11px;margin-top:6px;color:#384466;background:white;font:inherit;font-size:14px}.option-editor{display:grid;grid-template-columns:1fr 1fr;gap:9px}.option-row{display:flex;align-items:center;gap:8px}.option-row>span{font-weight:900;color:#7380d5}.option-row input[type=text]{flex:1}.primary-button{border:0;border-radius:12px;padding:12px 18px;background:linear-gradient(135deg,#6675e7,#8c64d6);color:white;font-weight:900}.list-heading{display:flex;align-items:center;justify-content:space-between}.list-heading span,.picker-heading span{color:#8991a6;font-size:12px}.question-row{padding:17px 0;border-top:1px solid #edf0f6}.item-meta{font-size:11px;color:#7680d4;font-weight:900;margin-bottom:6px}.question-row>strong,.exam-row strong{color:#344064;line-height:1.5}.option-list{display:grid;grid-template-columns:1fr 1fr;gap:7px;margin-top:11px}.option-list span{padding:8px;border-radius:9px;background:#f7f8fc;color:#707991;font-size:13px}.option-list span.correct{background:#eaf8f1;color:#27895f;font-weight:800}.explanation,.muted{font-size:12px;color:#858da1;margin-top:8px}.text-button{border:0;background:none;color:#be5a60;font-weight:800;font-size:12px;margin-top:8px;padding:0}.exam-row{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:15px 0;border-top:1px solid #edf0f6}.publish-button{border:0;border-radius:10px;padding:9px 12px;color:#6372d7;background:#eff1ff;font-weight:800;font-size:12px}.publish-button.published{background:#fff0e7;color:#cb7651}.question-picker{max-height:290px;overflow:auto;border:1px solid #e8ecf5;border-radius:13px;padding:12px;margin:14px 0}.picker-heading{display:flex;justify-content:space-between;margin-bottom:8px;color:#596480;font-size:13px}.picker-row{display:flex;gap:9px;align-items:start;padding:9px 3px;border-top:1px solid #f0f1f7;color:#66708a;font-size:13px}.picker-row input{margin-top:3px;accent-color:#6875df}.empty{text-align:center;color:#8a91a5;padding:18px}.notice{padding:12px 15px;border-radius:12px;margin:12px 0}.notice.error{background:#fff0f0;color:#ae4d54}.notice.success{background:#ebf8f1;color:#36865f}
@media(max-width:620px){.admin-head{align-items:start;gap:10px;flex-direction:column}.form-grid,.option-editor,.option-list{grid-template-columns:1fr}.editor-card,.list-card{padding:16px}.exam-row{align-items:start;flex-direction:column}}
.excel-import-card{padding:21px;background:linear-gradient(125deg,#fff,#f8f7fc);border:1px solid #e6e3ed;border-radius:18px;margin:15px 0;box-shadow:0 8px 25px #25346608}.excel-copy{display:flex;align-items:center;gap:13px}.excel-icon{display:grid;place-items:center;flex:0 0 48px;height:48px;border-radius:14px;background:#edf7f1;font-size:24px}.source-label{color:#6975d1;font-size:10px;font-weight:900;letter-spacing:.11em}.excel-copy h2{margin:4px 0;color:#303d68;font-size:18px}.excel-copy p{margin:0;color:#858da2;font-size:12px}.excel-actions{display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-top:17px}.source-button,.template-button,.file-picker{min-height:39px;border:0;border-radius:10px;padding:10px 14px;font-weight:900;font-size:12px;cursor:pointer}.source-button{background:#6674df;color:#fff}.template-button{border:1px solid #ded8e8;background:#fff;color:#66428e}.source-button:disabled,.template-button:disabled{opacity:.55;cursor:wait}.file-picker{position:relative;display:inline-flex;align-items:center;max-width:100%;overflow:hidden;border:1px dashed #c9c3d2;background:#fff;color:#656179}.file-picker input{position:absolute;inset:0;width:100%;height:100%;opacity:0;cursor:pointer;pointer-events:auto}.source-note{margin:12px 0 0;color:#9299ac;font-size:11px;line-height:1.6}
.lesson-row{padding:16px 0;border-top:1px solid #edf0f6}.lesson-row>strong{color:#344064}.lesson-row p{margin:7px 0;color:#858da1;font-size:12px}
</style>
