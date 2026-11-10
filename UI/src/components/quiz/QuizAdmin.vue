<template>
  <QuizLayout>
    <section class="admin-head"><div><span class="eyebrow">KHU VỰC GIÁO VIÊN</span><h1>Quản lý nội dung học tập</h1><p>Ngân hàng câu hỏi, điểm số và bài giảng được lưu trên Supabase.</p></div><RouterLink to="/" class="back-link">← Về trang học sinh</RouterLink></section>
    <div v-if="error" class="notice error">{{ error }}</div><div v-if="message" class="notice success">{{ message }}</div>
    <div class="tabs"><button :class="{ active: tab === 'questions' }" @click="tab = 'questions'">Ngân hàng câu hỏi</button><button :class="{ active: tab === 'lessons' }" @click="tab = 'lessons'">Bài giảng và lý thuyết</button></div>

    <template v-if="tab === 'questions'">
      <section class="excel-import-card">
        <div class="excel-copy"><span class="excel-icon">📊</span><div><span class="source-label">NHẬP HÀNG LOẠT</span><h2>Import câu hỏi từ Excel</h2><p>Nhập dữ liệu trong sheet CauHoi; sheet HuongDan ghi rõ cột bắt buộc, loại câu hỏi và giới hạn import.</p></div></div>
        <div class="excel-actions"><button class="template-button" :disabled="downloadingTemplate" @click="downloadTemplate">{{ downloadingTemplate ? 'Đang tải mẫu…' : '↓ Tải file Excel mẫu' }}</button><label class="file-picker">{{ importingExcel ? 'Đang nhập…' : (selectedExcelName || 'Chọn file Excel .xlsx') }}<input ref="excelFile" type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" @change="selectExcelFile" /></label><button class="source-button" :disabled="importingExcel || !selectedExcel" @click="importExcel">{{ importingExcel ? 'Đang nhập…' : 'Import câu hỏi' }}</button></div>
        <p class="source-note">Bắt buộc: Lớp, Môn, Nội dung, Đáp án A–D và Đáp án đúng. Chủ đề không bắt buộc; Loại câu hỏi để trống sẽ mặc định là Thường. Tối đa 500 câu, 5 MB mỗi lần.</p>
      </section>
      <section class="editor-card"><h2>Thêm câu hỏi mới</h2><div class="form-grid"><label>Khối<select v-model.number="question.grade"><option v-for="item in grades" :key="item" :value="item">Lớp {{ item }}</option></select></label><label>Môn<select v-model="question.subject"><option v-for="item in subjects" :key="item">{{ item }}</option></select></label><label>Loại câu hỏi<select v-model="question.difficulty"><option v-for="item in questionDifficulties" :key="item">{{ item }}</option></select></label><label>Chủ đề (không bắt buộc)<input v-model.trim="question.topic" list="question-topic-options" maxlength="200" placeholder="Ví dụ: Số thập phân · So sánh số" /><datalist id="question-topic-options"><option v-for="topic in questionTopicOptions" :key="topic" :value="topic" /></datalist><small class="field-hint">Có thể chọn chủ đề đã dùng hoặc nhập chủ đề mới.</small></label></div>
        <label>Nội dung câu hỏi<textarea v-model.trim="question.content" rows="3" maxlength="4000" placeholder="Nhập câu hỏi…"></textarea></label>
        <div class="option-editor"><label v-for="(option, index) in question.options" :key="index" class="option-row"><input type="radio" name="correct-option" :value="index" v-model.number="question.correctIndex" /><span>{{ letters[index] }}</span><input v-model.trim="option.content" :placeholder="`Nội dung đáp án ${letters[index]}`" maxlength="1000" /></label></div>
        <label>Giải thích đáp án (không bắt buộc)<textarea v-model.trim="question.explanation" rows="2" maxlength="2000" placeholder="Giải thích ngắn gọn để học sinh ôn lại"></textarea></label>
        <button class="primary-button" :disabled="saving" @click="createQuestion">{{ saving ? 'Đang lưu…' : 'Lưu câu hỏi' }}</button>
      </section>
      <section class="list-card"><div class="list-heading"><h2>Ngân hàng câu hỏi</h2><div class="list-actions"><span>{{ filteredQuestions.length }} / {{ questions.length }} câu</span><button class="danger-button" :disabled="deletingAllQuestions" @click="deleteAllQuestions">{{ deletingAllQuestions ? 'Đang xóa…' : 'Xóa toàn bộ dữ liệu câu hỏi' }}</button></div></div><p class="archive-note">Xóa vĩnh viễn câu hỏi, lựa chọn, liên kết đề thi và lịch sử câu trả lời. Đề thi và điểm tổng vẫn còn nhưng mất nội dung câu hỏi.</p><div class="bank-filters"><label>Lớp<select v-model.number="filterGrade"><option v-for="filter in gradeFilters" :key="filter" :value="filter">{{ filter === 0 ? 'Tất cả lớp' : `Lớp ${filter}` }} ({{ gradeCount(filter) }})</option></select></label><label>Môn học<select v-model="filterSubject"><option v-for="filter in subjectFilters" :key="filter" :value="filter">{{ filter === 'Tất cả' ? 'Tất cả môn' : filter }} ({{ subjectCount(filter) }})</option></select></label><label>Loại câu hỏi<select v-model="filterDifficulty"><option v-for="filter in questionFilters" :key="filter" :value="filter">{{ filter }} ({{ difficultyCount(filter) }})</option></select></label><label>Chủ đề<select v-model="filterTopic"><option value="Tất cả">Tất cả chủ đề ({{ questions.length }})</option><option value="__no_topic__">Chưa phân chủ đề ({{ topicCount('__no_topic__') }})</option><option v-for="topic in questionTopicOptions" :key="topic" :value="topic">{{ topic }} ({{ topicCount(topic) }})</option></select></label></div><div v-if="!filteredQuestions.length" class="empty">{{ questions.length ? 'Không có câu hỏi khớp với các bộ lọc này.' : 'Chưa có câu hỏi.' }}</div><article v-for="item in filteredQuestions" :key="item.id" class="question-row"><div class="question-meta-line"><div class="item-meta">Lớp {{ item.grade }} · {{ item.subject }}<span class="topic-tag">{{ item.topic || 'Chưa phân chủ đề' }}</span></div><label class="difficulty-picker" :class="{ advanced: item.difficulty === 'Nâng cao' }"><span>{{ item.difficulty || 'Thường' }}</span><select :value="item.difficulty || 'Thường'" :aria-label="`Đổi loại câu hỏi: ${item.content}`" @change="setQuestionDifficulty(item, $event.target.value)"><option v-for="difficulty in questionDifficulties" :key="difficulty">{{ difficulty }}</option></select></label></div><strong>{{ item.content }}</strong><div class="option-list"><span v-for="(option, index) in item.options" :key="option.id" :class="{ correct: option.isCorrect }">{{ letters[index] }}. {{ option.content }}</span></div><div class="topic-assignment"><label :for="`topic-${item.id}`">Chủ đề</label><input :id="`topic-${item.id}`" v-model.trim="topicDraft[item.id]" list="question-topic-options" maxlength="200" placeholder="Chọn hoặc nhập chủ đề" /><button type="button" :disabled="savingTopicId === item.id" @click="setQuestionTopic(item)">{{ savingTopicId === item.id ? 'Đang lưu…' : 'Lưu chủ đề' }}</button></div><div v-if="item.explanation" class="explanation">{{ item.explanation }}</div><button class="text-button" @click="deleteQuestion(item)">{{ item.isActive ? 'Xóa / lưu trữ' : 'Đã lưu trữ' }}</button></article></section>
    </template>

    <template v-else>
      <section class="editor-card"><h2>{{ editingLessonId ? 'Chỉnh sửa bài giảng hoặc lý thuyết' : 'Thêm bài giảng hoặc lý thuyết' }}</h2><div class="form-grid"><label>Tên bài<input v-model.trim="lesson.title" maxlength="200" placeholder="Ví dụ: Bài 1 · Ôn tập số tự nhiên" /></label><label>Chủ đề<select v-model="topicChoice" @change="chooseTopic"><option value="">Chọn chủ đề</option><optgroup v-for="group in topicOptionGroups" :key="group.grade" :label="`Chủ đề đã có · Lớp ${group.grade}`"><option v-for="topic in group.topics" :key="`${group.grade}-${topic}`" :value="topic">{{ topic }}</option></optgroup><option :value="newTopicOption">＋ Thêm chủ đề mới…</option></select><small class="field-hint">Chủ đề được lấy từ bài giảng đã lưu và nhãn trong câu hỏi cùng lớp, môn.</small><input v-if="isAddingTopic" v-model.trim="lesson.topic" maxlength="200" placeholder="Nhập tên chủ đề mới" aria-label="Tên chủ đề mới" /><small v-if="isAddingTopic" class="field-hint">Chủ đề mới sẽ được lưu và xuất hiện trong danh sách cho cùng lớp, môn.</small></label><label>Khối<select v-model.number="lesson.grade"><option v-for="item in lessonGrades" :key="item" :value="item">Lớp {{ item }}</option></select></label><label>Môn<select v-model="lesson.subject"><option v-for="item in subjects" :key="item">{{ item }}</option></select></label><label>Liên kết video (không bắt buộc)<input v-model.trim="lesson.videoUrl" type="url" inputmode="url" maxlength="2000" placeholder="https://www.youtube.com/watch?v=…" /><small class="field-hint">Dán link chia sẻ YouTube, Vimeo, Google Drive hoặc link video MP4. Nếu thiếu https://, hệ thống sẽ tự bổ sung.</small></label></div>
        <label>Mô tả ngắn<textarea v-model.trim="lesson.summary" rows="2" maxlength="1000" placeholder="Giới thiệu nội dung bài học"></textarea></label>
        <label>Nội dung bài học<textarea v-model.trim="lesson.content" rows="9" maxlength="20000" placeholder="Nhập lý thuyết, ví dụ và ghi chú. Có thể xuống dòng để chia mục."></textarea></label>
        <div class="lesson-form-actions"><button class="primary-button" :disabled="saving" @click="createLesson">{{ saving ? 'Đang lưu…' : (editingLessonId ? 'Cập nhật bài học' : 'Đăng bài học') }}</button><button v-if="editingLessonId" class="cancel-edit-button" :disabled="saving" @click="cancelEditLesson">Hủy chỉnh sửa</button></div>
      </section>
      <section class="list-card"><div class="list-heading"><h2>Bài học đã đăng</h2><span>{{ lessons.length }} bài</span></div><div v-if="dataLoadError" class="empty error-empty">Không tải được danh sách bài học. Hãy kiểm tra API hoặc tải lại trang.</div><div v-else-if="!lessons.length" class="empty">Chưa có bài học.</div><article v-for="item in lessons" :key="item.id" class="lesson-row"><div class="item-meta">Lớp {{ item.grade }} · {{ item.subject }}</div><strong>{{ item.title }}</strong><p>{{ item.topic || 'Chưa xếp chủ đề' }} · {{ item.summary }}</p><div class="lesson-admin-links"><a v-if="item.videoUrl" class="saved-video-link" :href="item.videoUrl" target="_blank" rel="noopener noreferrer">▶ {{ item.videoUrl }} ↗</a><span v-else class="missing-video-link">Chưa gắn liên kết video</span></div><div class="lesson-admin-actions"><button class="text-button" @click="editLesson(item)">Sửa bài học / link</button><button class="text-button" @click="deleteLesson(item)">Xóa bài học</button></div></article></section>
    </template>
  </QuizLayout>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'

const subjects = ['Toán', 'Tiếng Anh', 'Tiếng Việt']
const questionDifficulties = ['Thường', 'Nâng cao']
const questionFilters = ['Tất cả', ...questionDifficulties]
const questionTopicOptions = computed(() => [...new Set(questions.value.map(item => item.topic?.trim()).filter(Boolean))].sort((a, b) => a.localeCompare(b, 'vi', { numeric: true })))
const subjectFilters = ['Tất cả', ...subjects]
const grades = Array.from({ length: 8 }, (_, index) => index + 5)
const lessonGrades = Array.from({ length: 9 }, (_, index) => index + 4)
const gradeFilters = [0, ...grades]
const letters = ['A', 'B', 'C', 'D', 'E', 'F']
const tab = ref('questions')
const questions = ref([])
const filterDifficulty = ref('Tất cả')
const filterTopic = ref('Tất cả')
const filterSubject = ref('Tất cả')
const filterGrade = ref(0)
const lessons = ref([])
const dataLoadError = ref(false)
const editingLessonId = ref(null)
const saving = ref(false)
const savingTopicId = ref(null)
const topicDraft = reactive({})
const error = ref('')
const message = ref('')
const excelFile = ref(null)
const selectedExcel = ref(null)
const selectedExcelName = ref('')
const importingExcel = ref(false)
const downloadingTemplate = ref(false)
const deletingAllQuestions = ref(false)
const question = reactive({ grade: 5, subject: 'Toán', difficulty: 'Thường', topic: '', content: '', explanation: '', correctIndex: 0, options: [{ content: '' }, { content: '' }, { content: '' }, { content: '' }] })
const lesson = reactive({ title: '', topic: '', summary: '', content: '', videoUrl: '', grade: 4, subject: 'Toán' })
const newTopicOption = '__add_new_topic__'
const topicChoice = ref('')
const isAddingTopic = computed(() => topicChoice.value === newTopicOption)
const topicOptionGroups = computed(() => {
  const groups = new Map()
  const addTopic = (grade, value) => {
    const topic = value?.trim().replace(/\s+/g, ' ')
    if (!topic) return
    const classNumber = Number(grade) || Number(lesson.grade)
    if (!groups.has(classNumber)) groups.set(classNumber, new Map())
    groups.get(classNumber).set(topic.toLocaleLowerCase('vi'), topic)
  }
  for (const item of lessons.value.filter(item => (item.subject ?? item.Subject) === lesson.subject)) {
    const grade = item.grade ?? item.Grade
    addTopic(grade, item.topic ?? item.Topic)
    const title = item.title ?? item.Title ?? ''
    const inferred = title.match(/^\s*(Chủ đề\s*\d+\s*[:：—-]\s*.+?)(?:\s*(?:[|·]|-\s*)Bài\s*\d+\b.*)?\s*$/i)?.[1]
    addTopic(grade, inferred)
  }
  for (const item of questions.value.filter(item => (item.subject ?? item.Subject) === lesson.subject)) {
    const grade = item.grade ?? item.Grade
    addTopic(grade, item.topic ?? item.Topic)
    const content = item.content ?? item.Content ?? ''
    addTopic(grade, content.match(/^\s*\[([^\]]+)\]\s*/)?.[1])
  }
  addTopic(lesson.grade, lesson.topic)
  return [...groups.entries()]
    .sort(([gradeA], [gradeB]) => gradeA - gradeB)
    .map(([grade, topics]) => ({ grade, topics: [...topics.values()].sort((a, b) => a.localeCompare(b, 'vi', { numeric: true })) }))
})
const chooseTopic = () => {
  lesson.topic = topicChoice.value === newTopicOption ? '' : topicChoice.value
}
const filteredQuestions = computed(() => questions.value.filter(item =>
  (filterGrade.value === 0 || item.grade === filterGrade.value) &&
  (filterSubject.value === 'Tất cả' || item.subject === filterSubject.value) &&
  (filterDifficulty.value === 'Tất cả' || (item.difficulty || 'Thường') === filterDifficulty.value) &&
  (filterTopic.value === 'Tất cả' || (filterTopic.value === '__no_topic__' ? !item.topic : item.topic === filterTopic.value))))
const gradeCount = filter => questions.value.filter(item =>
  (filter === 0 || item.grade === filter) &&
  (filterSubject.value === 'Tất cả' || item.subject === filterSubject.value) &&
  (filterDifficulty.value === 'Tất cả' || (item.difficulty || 'Thường') === filterDifficulty.value)).length
const subjectCount = filter => questions.value.filter(item =>
  (filterGrade.value === 0 || item.grade === filterGrade.value) &&
  (filter === 'Tất cả' || item.subject === filter) &&
  (filterDifficulty.value === 'Tất cả' || (item.difficulty || 'Thường') === filterDifficulty.value)).length
const difficultyCount = filter => questions.value.filter(item =>
  (filterGrade.value === 0 || item.grade === filterGrade.value) &&
  (filterSubject.value === 'Tất cả' || item.subject === filterSubject.value) &&
  (filter === 'Tất cả' || (item.difficulty || 'Thường') === filter)).length
const topicCount = filter => questions.value.filter(item =>
  (filterGrade.value === 0 || item.grade === filterGrade.value) &&
  (filterSubject.value === 'Tất cả' || item.subject === filterSubject.value) &&
  (filterDifficulty.value === 'Tất cả' || (item.difficulty || 'Thường') === filterDifficulty.value) &&
  (filter === '__no_topic__' ? !item.topic : item.topic === filter)).length

const loadData = async () => {
  dataLoadError.value = false
  try {
    const [questionResult, lessonResult] = await Promise.all([httpClient.get('/quiz-admin/questions'), httpClient.get('/quiz-admin/lessons')])
    questions.value = questionResult.data || []
    Object.keys(topicDraft).forEach(id => delete topicDraft[id])
    Object.assign(topicDraft, Object.fromEntries(questions.value.map(item => [item.id, item.topic || ''])))
    lessons.value = (lessonResult.data || []).map(item => ({ ...item, id: item.id ?? item.Id, title: item.title ?? item.Title, topic: item.topic ?? item.Topic, summary: item.summary ?? item.Summary, content: item.content ?? item.Content, videoUrl: item.videoUrl ?? item.VideoUrl, grade: item.grade ?? item.Grade, subject: item.subject ?? item.Subject }))
    error.value = ''
    return lessons.value
  } catch (err) {
    dataLoadError.value = true
    const status = err?.response?.status
    const detail = responseErrorText(err)
    error.value = status === 401 ? 'Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.' : status === 403 ? 'Tài khoản chưa được cấp quyền quản lý nội dung.' : detail ? `Không tải được dữ liệu quản trị: ${detail}` : 'Không tải được dữ liệu quản trị. Kiểm tra API và cơ sở dữ liệu.'
    return null
  }
}
const responseErrorText = err => {
  const body = err?.response?.data || {}
  if (body.message || body.Message || body.title || body.Title) return body.message || body.Message || body.title || body.Title
  const errors = body.errors || body.Errors
  if (errors && typeof errors === 'object') return Object.values(errors).flat().filter(Boolean).join(' ')
  return ''
}
const apiUpdateHint = err => {
  const serverMessage = responseErrorText(err)
  if (err?.response?.status === 404 && serverMessage === 'API route not found.') return 'API đang chạy phiên bản cũ nên chưa có route cập nhật bài giảng. Hãy khởi động lại API rồi lưu lại liên kết.'
  if (err?.response?.status === 404) return 'Không tìm thấy bài giảng để cập nhật. Hãy tải lại danh sách rồi thử lại.'
  if (err?.response?.status === 400) return serverMessage || 'Dữ liệu chưa hợp lệ. Hãy kiểm tra URL video, lớp, môn học và nội dung bài.'
  if (!err?.response) return 'Không kết nối được API. Hãy kiểm tra API .NET tại cổng 5080.'
  if (err.response.status >= 500) return serverMessage || 'API không lưu được dữ liệu. Hãy kiểm tra migration cơ sở dữ liệu và log API.'
  return serverMessage || `Không lưu được bài học (HTTP ${err.response.status}).`
}
const normalizeVideoUrl = value => {
  const text = value.trim()
  if (!text) return ''
  const normalized = /^https?:\/\//i.test(text) ? text : `https://${text}`
  const parsed = new URL(normalized)
  if (!['http:', 'https:'].includes(parsed.protocol)) throw new Error('Liên kết video phải bắt đầu bằng http:// hoặc https://.')
  return parsed.href
}
const resetLessonForm = () => {
  editingLessonId.value = null
  Object.assign(lesson, { title: '', topic: '', summary: '', content: '', videoUrl: '', grade: 4, subject: 'Toán' })
  topicChoice.value = ''
}
const editLesson = item => {
  error.value = ''; message.value = ''
  editingLessonId.value = item.id
  Object.assign(lesson, { title: item.title || '', topic: item.topic || '', summary: item.summary || '', content: item.content || '', videoUrl: item.videoUrl || '', grade: item.grade || 4, subject: item.subject || 'Toán' })
  topicChoice.value = item.topic ? item.topic : ''
  document.querySelector('.editor-card')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}
const cancelEditLesson = () => { resetLessonForm(); error.value = ''; message.value = '' }
const createLesson = async () => {
  error.value = ''; message.value = ''
  if (!lesson.title || !lesson.content) { error.value = 'Hãy nhập tên và nội dung bài học.'; return }
  let videoUrl
  try { videoUrl = normalizeVideoUrl(lesson.videoUrl) }
  catch (err) { error.value = err.message || 'Liên kết video chưa đúng định dạng.'; return }
  saving.value = true
  try {
    const payload = { ...lesson, videoUrl: videoUrl || null }
    const wasEditing = Boolean(editingLessonId.value)
    const response = wasEditing
      ? await httpClient.put(`/quiz-admin/lessons/${editingLessonId.value}`, payload)
      : await httpClient.post('/quiz-admin/lessons', payload)
    if (!wasEditing) editingLessonId.value = response.data?.id ?? response.data?.Id ?? null
    const loadedLessons = await loadData()
    if (!loadedLessons) throw new Error('Đã gửi yêu cầu lưu nhưng chưa kiểm tra được dữ liệu từ API. Hãy khởi động lại API rồi tải lại trang.')
    const savedLesson = loadedLessons?.find(item => editingLessonId.value && String(item.id) === String(editingLessonId.value))
    if (videoUrl && savedLesson && savedLesson.videoUrl !== videoUrl) {
      throw new Error('API chưa lưu được trường URL. Khởi động lại API để cập nhật migration, sau đó bấm Cập nhật bài học để lưu lại link.')
    }
    resetLessonForm()
    message.value = wasEditing ? 'Đã cập nhật bài học và liên kết video.' : 'Đã đăng bài học.'
  } catch (err) { error.value = err?.response ? apiUpdateHint(err) : (err.message || 'Chưa lưu được bài học.') }
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
    const response = await httpClient.post('/quiz-admin/questions/import-excel', form, { timeout: 120000 })
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
    await httpClient.post('/quiz-admin/questions', { grade: question.grade, subject: question.subject, difficulty: question.difficulty, topic: question.topic || null, content: question.content, explanation: question.explanation, options: question.options.map((item, index) => ({ content: item.content, isCorrect: index === question.correctIndex })) })
    question.topic = ''; question.content = ''; question.explanation = ''; question.options.forEach(item => { item.content = '' }); question.correctIndex = 0
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
const deleteAllQuestions = async () => {
  if (deletingAllQuestions.value) return
  if (!confirm('CẢNH BÁO: Xóa vĩnh viễn toàn bộ dữ liệu câu hỏi trong database? Thao tác này sẽ xóa câu hỏi, lựa chọn, liên kết đề thi và lịch sử câu trả lời; đề thi và điểm tổng vẫn còn nhưng mất nội dung câu hỏi. Không thể hoàn tác.')) return
  deletingAllQuestions.value = true
  error.value = ''; message.value = ''
  try {
    const response = await httpClient.delete('/quiz-admin/questions')
    message.value = response.data.message
    await loadData()
  } catch (err) {
    error.value = err?.response?.data?.message || 'Không xóa được toàn bộ câu hỏi.'
  } finally {
    deletingAllQuestions.value = false
  }
}
const setQuestionTopic = async item => {
  const topic = topicDraft[item.id]?.trim() || null
  const previousTopic = item.topic || null
  if (topic === previousTopic) return
  savingTopicId.value = item.id
  try {
    await httpClient.patch(`/quiz-admin/questions/${item.id}/topic`, { topic })
    item.topic = topic
    topicDraft[item.id] = topic || ''
    message.value = topic ? `Đã gán chủ đề “${topic}” cho câu hỏi.` : 'Đã bỏ phân loại chủ đề cho câu hỏi.'
    error.value = ''
  } catch (err) {
    error.value = err?.response?.data?.message || 'Không lưu được chủ đề cho câu hỏi.'
  } finally {
    savingTopicId.value = null
  }
}
const setQuestionDifficulty = async (item, difficulty) => {
  const previousDifficulty = item.difficulty || 'Thường'
  if (previousDifficulty === difficulty) return
  try {
    await httpClient.patch(`/quiz-admin/questions/${item.id}/difficulty`, { difficulty })
    item.difficulty = difficulty
    message.value = `Đã chuyển câu hỏi sang loại ${difficulty}.`
    error.value = ''
  } catch (err) {
    error.value = err?.response?.data?.message || 'Không đổi được loại câu hỏi.'
  }
}
onMounted(loadData)
</script>

<style scoped>
.eyebrow{color:#747bd4;font-size:11px;font-weight:900;letter-spacing:.13em}.admin-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:22px}.admin-head h1{color:#2a3865;margin:6px 0}.admin-head p{color:#8189a0;margin:0}.back-link{color:#626fce;font-weight:800;text-decoration:none}.tabs{display:flex;gap:8px;margin:15px 0}.tabs button{border:0;border-radius:12px;padding:11px 15px;background:#e9ecf8;color:#66708d;font-weight:800}.tabs button.active{background:#626fe0;color:#fff}.editor-card,.list-card{background:white;border:1px solid #e8ecf5;border-radius:20px;padding:22px;margin:15px 0;box-shadow:0 8px 25px #25346608}.editor-card h2,.list-card h2{font-size:18px;color:#303d68;margin:0 0 16px}.form-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:12px}.editor-card>label,.form-grid label{display:block;margin:12px 0;color:#68728c;font-weight:800;font-size:12px}.editor-card input:not([type=radio]):not([type=checkbox]),.editor-card select,.editor-card textarea{display:block;width:100%;border:1px solid #e1e5ef;border-radius:11px;padding:11px;margin-top:6px;color:#384466;background:white;font:inherit;font-size:14px}.option-editor{display:grid;grid-template-columns:1fr 1fr;gap:9px}.option-row{display:flex;align-items:center;gap:8px}.option-row>span{font-weight:900;color:#7380d5}.option-row input[type=text]{flex:1}.primary-button{border:0;border-radius:12px;padding:12px 18px;background:linear-gradient(135deg,#6675e7,#8c64d6);color:white;font-weight:900}.list-heading{display:flex;align-items:center;justify-content:space-between}.list-heading span,.picker-heading span{color:#8991a6;font-size:12px}.question-row{padding:17px 0;border-top:1px solid #edf0f6}.item-meta{font-size:11px;color:#7680d4;font-weight:900;margin-bottom:6px}.question-row>strong,.exam-row strong{color:#344064;line-height:1.5}.option-list{display:grid;grid-template-columns:1fr 1fr;gap:7px;margin-top:11px}.option-list span{padding:8px;border-radius:9px;background:#f7f8fc;color:#707991;font-size:13px}.option-list span.correct{background:#eaf8f1;color:#27895f;font-weight:800}.explanation,.muted{font-size:12px;color:#858da1;margin-top:8px}.text-button{border:0;background:none;color:#be5a60;font-weight:800;font-size:12px;margin-top:8px;padding:0}.exam-row{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:15px 0;border-top:1px solid #edf0f6}.publish-button{border:0;border-radius:10px;padding:9px 12px;color:#6372d7;background:#eff1ff;font-weight:800;font-size:12px}.publish-button.published{background:#fff0e7;color:#cb7651}.question-picker{max-height:290px;overflow:auto;border:1px solid #e8ecf5;border-radius:13px;padding:12px;margin:14px 0}.picker-heading{display:flex;justify-content:space-between;margin-bottom:8px;color:#596480;font-size:13px}.picker-row{display:flex;gap:9px;align-items:start;padding:9px 3px;border-top:1px solid #f0f1f7;color:#66708a;font-size:13px}.picker-row input{margin-top:3px;accent-color:#6875df}.error-empty{color:#a84d58!important}.empty{text-align:center;color:#8a91a5;padding:18px}.notice{padding:12px 15px;border-radius:12px;margin:12px 0}.notice.error{background:#fff0f0;color:#ae4d54}.notice.success{background:#ebf8f1;color:#36865f}
@media(max-width:620px){.admin-head{align-items:start;gap:10px;flex-direction:column}.form-grid,.option-editor,.option-list{grid-template-columns:1fr}.editor-card,.list-card{padding:16px}.exam-row{align-items:start;flex-direction:column}}
.excel-import-card{padding:21px;background:linear-gradient(125deg,#fff,#f8f7fc);border:1px solid #e6e3ed;border-radius:18px;margin:15px 0;box-shadow:0 8px 25px #25346608}.excel-copy{display:flex;align-items:center;gap:13px}.excel-icon{display:grid;place-items:center;flex:0 0 48px;height:48px;border-radius:14px;background:#edf7f1;font-size:24px}.source-label{color:#6975d1;font-size:10px;font-weight:900;letter-spacing:.11em}.excel-copy h2{margin:4px 0;color:#303d68;font-size:18px}.excel-copy p{margin:0;color:#858da2;font-size:12px}.excel-actions{display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-top:17px}.source-button,.template-button,.file-picker{min-height:39px;border:0;border-radius:10px;padding:10px 14px;font-weight:900;font-size:12px;cursor:pointer}.source-button{background:#6674df;color:#fff}.template-button{border:1px solid #ded8e8;background:#fff;color:#66428e}.source-button:disabled,.template-button:disabled{opacity:.55;cursor:wait}.file-picker{position:relative;display:inline-flex;align-items:center;max-width:100%;overflow:hidden;border:1px dashed #c9c3d2;background:#fff;color:#656179}.file-picker input{position:absolute;inset:0;width:100%;height:100%;opacity:0;cursor:pointer;pointer-events:auto}.source-note{margin:12px 0 0;color:#9299ac;font-size:11px;line-height:1.6}
.field-hint{display:block;margin-top:6px;color:#7d859c;font-size:11px;line-height:1.45}.lesson-form-actions,.lesson-admin-actions{display:flex;align-items:center;flex-wrap:wrap;gap:9px}.cancel-edit-button{padding:12px 15px;border:1px solid #deddea;border-radius:11px;background:#fff;color:#62627a;font-weight:800;cursor:pointer}.lesson-admin-links{margin:7px 0}.saved-video-link{display:inline-block;max-width:100%;overflow-wrap:anywhere;color:#5c4693;font-size:12px;font-weight:700;text-decoration:none}.saved-video-link:hover{text-decoration:underline}.missing-video-link{color:#9a7180;font-size:11px}.lesson-row{padding:16px 0;border-top:1px solid #edf0f6}.lesson-row>strong{color:#344064}.lesson-row p{margin:7px 0;color:#858da1;font-size:12px}
.bank-filters{display:grid;grid-template-columns:repeat(4,minmax(150px,1fr));gap:12px;padding:13px 0;border-top:1px solid #f0f2f6}.bank-filters label{display:grid;gap:6px;color:#748095;font-size:11px;font-weight:800}.bank-filters select{width:100%;min-height:40px;padding:9px 12px;border:1px solid #dfe5ed;border-radius:10px;background:#fff;color:#394766;font:inherit;font-size:13px;cursor:pointer}.question-meta-line{display:flex;align-items:center;justify-content:space-between;gap:12px}.question-meta-line .item-meta{display:flex;align-items:center;flex-wrap:wrap;gap:7px;margin:0}.topic-tag{padding:4px 8px;border:1px solid #e4d9f1;border-radius:99px;background:#f8f3fd;color:#75549a;font-size:10px;font-weight:800}.topic-assignment{display:flex;align-items:center;gap:8px;margin-top:12px}.topic-assignment label{color:#748095;font-size:11px;font-weight:900}.topic-assignment input{min-width:180px;max-width:320px;min-height:34px;padding:7px 10px;border:1px solid #dfe5ed;border-radius:9px;color:#394766;font:inherit;font-size:12px}.topic-assignment button{min-height:34px;padding:7px 11px;border:0;border-radius:9px;background:#f0eafb;color:#674295;font-size:11px;font-weight:900;cursor:pointer}.topic-assignment button:disabled{opacity:.6;cursor:wait}.difficulty-picker{position:relative;display:inline-flex;align-items:center;gap:7px;padding:5px 9px;border:1px solid #dce9df;border-radius:999px;background:#f0f7f1;color:#3d7856;font-size:10px;font-weight:900}.difficulty-picker.advanced{border-color:#f0dfb7;background:#fff8e8;color:#a06e1c}.difficulty-picker select{position:absolute;inset:0;width:100%;height:100%;opacity:0;cursor:pointer}
.list-actions{display:flex;align-items:center;gap:12px}.danger-button{border:1px solid #f1c5c8;border-radius:10px;padding:9px 12px;background:#fff3f3;color:#b54b55;font-size:12px;font-weight:900;cursor:pointer}.danger-button:disabled{opacity:.5;cursor:not-allowed}.archive-note{margin:8px 0 2px;color:#a14b51;font-size:11px;line-height:1.5}
@media(max-width:620px){.bank-filters{grid-template-columns:1fr;gap:9px}}
@media(max-width:620px){.list-actions{align-items:flex-end;flex-direction:column;gap:8px}.list-heading{align-items:flex-start}}
</style>
