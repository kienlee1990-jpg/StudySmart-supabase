<template>
  <QuizLayout>
    <section class="subject-hero" :class="subjectClass"><RouterLink to="/" class="back-link">← Tất cả môn học</RouterLink><div class="subject-icon">{{ icon }}</div><span class="eyebrow">KHÔNG GIAN HỌC TẬP</span><h1>{{ subject }}</h1><p>Chọn cách học phù hợp với em hôm nay.</p></section>
    <section class="mode-section"><div class="section-title"><div><span class="eyebrow">BỐN CÁCH CHINH PHỤC KIẾN THỨC</span><h2>Em muốn làm gì?</h2></div><label>Khối lớp<select v-model.number="grade"><option v-for="item in grades" :key="item" :value="item">Lớp {{ item }}</option></select></label></div>
      <div class="mode-grid">
        <button v-for="mode in modes" :key="mode.id" class="mode-card" :class="[mode.kind, { selected: activeMode === mode.id }]" @click="selectMode(mode.id)"><span class="mode-icon">{{ mode.icon }}</span><strong>{{ mode.title }}</strong><small>{{ mode.description }}</small><span class="mode-action">{{ activeMode === mode.id ? 'Đang chọn' : 'Chọn chế độ' }} →</span></button>
        <RouterLink :to="{ path: '/ranking', query: { subject } }" class="mode-card ranking"><span class="mode-icon">🗼</span><strong>Leo tháp</strong><small>Vượt từng level, tích lũy sao và mở tầng tiếp theo.</small><span class="mode-action">Bắt đầu leo tháp →</span></RouterLink>
      </div>
    </section>

    <section v-if="activeMode === 'Study'" class="quiz-section">
      <div class="section-title"><div><span class="eyebrow">HỌC TẬP · LỚP {{ grade }}</span><h2>Bài giảng và lý thuyết</h2></div><span v-if="loading" class="loading-label">Đang tải bài…</span></div>
      <div v-if="error" class="empty-card">{{ error }} <button @click="loadLessons">Thử lại</button></div>
      <div v-else class="lesson-layout">
        <div class="lesson-list"><button v-for="lesson in lessons" :key="lesson.id" class="lesson-choice" :class="{ active: selectedLesson?.id === lesson.id }" @click="selectedLesson = lesson"><span class="lesson-grade">LỚP {{ lesson.grade }}</span><strong>{{ lesson.title }}</strong><small>{{ lesson.summary }}</small></button><div v-if="!loading && !lessons.length" class="empty-card">Chưa có bài học cho lớp {{ grade }}. Giáo viên có thể đăng bài trong khu vực quản trị.</div></div>
        <article v-if="selectedLesson" class="lesson-article"><span class="eyebrow">{{ subject.toUpperCase() }} · LỚP {{ selectedLesson.grade }}</span><h2>{{ selectedLesson.title }}</h2><p class="lesson-summary">{{ selectedLesson.summary }}</p><div class="lesson-content">{{ selectedLesson.content }}</div></article>
      </div>
    </section>

    <section v-if="activeMode === 'Test' || activeMode === 'ExamRoom'" class="quiz-section">
      <div class="section-title"><div><span class="eyebrow">{{ modeTitle(activeMode) }} · LỚP {{ grade }}</span><h2>Chọn chủ đề cho bài thi</h2></div></div>
      <p class="random-note">Hiện có <strong>{{ bankQuestionCount }}</strong> câu hỏi môn {{ subject }}, lớp {{ grade }}. Chọn một hoặc nhiều chủ đề để tạo đề chỉ từ nội dung đã chọn.</p>
      <div v-if="topicsLoading" class="empty-card">Đang tải chủ đề…</div>
      <div v-else-if="topicsError" class="empty-card">{{ topicsError }} <button @click="loadQuestionCount">Thử lại</button></div>
      <div v-else-if="!questionTopics.length" class="empty-card">Chưa có chủ đề trong ngân hàng câu hỏi môn {{ subject }}, lớp {{ grade }}.</div>
      <template v-else>
        <div v-if="activeMode === 'Test'" class="topic-picker"><div class="topic-picker-heading"><div><strong>Chọn chủ đề bằng checkbox</strong><small>Đã chọn {{ selectedTopicKeys.length }} / {{ questionTopics.length }} chủ đề · {{ selectedQuestionCount }} câu</small></div><div class="topic-picker-actions"><button type="button" @click="selectAllTopics">Chọn tất cả</button><button type="button" @click="selectedTopicKeys = []">Bỏ chọn</button></div></div><div class="topic-options"><label v-for="topic in questionTopics" :key="topic.key" class="topic-option" :class="{ selected: selectedTopicKeys.includes(topic.key), uncategorized: topic.uncategorized }"><input v-model="selectedTopicKeys" type="checkbox" :value="topic.key" /><span class="topic-option-copy"><strong>{{ topic.label }}</strong><small>{{ topic.uncategorized ? 'Câu hỏi không có chủ đề · ' : '' }}{{ topic.count }} câu</small></span><span class="topic-check">✓</span></label></div></div>
        <div class="duration-grid"><button v-for="duration in durationOptions" :key="duration.minutes" type="button" class="duration-card duration-button" :class="{ unavailable: activeMode === 'Test' ? !canStartDuration(duration.questions) : bankQuestionCount < duration.questions }" :disabled="activeMode === 'Test' ? !canStartDuration(duration.questions) : bankQuestionCount < duration.questions" @click="activeMode === 'Test' ? createTestWithTopics(duration) : openTopicModal(duration)"><span class="duration-icon">{{ duration.icon }}</span><strong>{{ duration.minutes }} phút</strong><span>{{ duration.questions }} câu hỏi ngẫu nhiên</span><small>{{ activeMode === 'Test' ? 'Kiểm tra theo chủ đề đã đánh dấu' : 'Thi thử trong phòng thi có tính giờ' }}</small><small v-if="activeMode === 'Test' && selectedQuestionCount < duration.questions" class="need-questions">Đã chọn {{ selectedQuestionCount }} câu; cần ít nhất {{ duration.questions }}.</small><small v-else-if="activeMode === 'ExamRoom' && bankQuestionCount < duration.questions" class="need-questions">Cần {{ duration.questions }} câu; hiện có {{ bankQuestionCount }}.</small><b v-else>{{ activeMode === 'Test' ? 'Bắt đầu kiểm tra →' : 'Chỉnh chủ đề →' }}</b></button></div>
      </template>

      <Teleport to="body"><div v-if="activeMode === 'ExamRoom' && showTopicModal" class="topic-modal-backdrop" :data-subject="getSubjectTheme(subject)" @click.self="closeTopicModal" @keydown.esc="closeTopicModal"><section class="topic-modal" role="dialog" aria-modal="true" aria-labelledby="topic-modal-title"><header class="topic-modal-header"><div><span class="topic-modal-icon">{{ icon }}</span><div><span class="eyebrow">{{ modeTitle(activeMode) }} · {{ subject.toUpperCase() }} · LỚP {{ grade }}</span><h2 id="topic-modal-title">Chọn chủ đề cho đề thi</h2><p>Đề {{ pendingDuration?.minutes }} phút gồm {{ pendingDuration?.questions }} câu, chỉ lấy từ các chủ đề đã đánh dấu.</p></div></div><button class="topic-modal-close" type="button" aria-label="Đóng cửa sổ" @click="closeTopicModal">×</button></header><div class="topic-modal-body"><div class="topic-picker-heading"><div><strong>Chủ đề có trong ngân hàng</strong><small>Đã chọn {{ selectedTopicKeys.length }} / {{ questionTopics.length }} · {{ selectedQuestionCount }} câu khả dụng</small></div><div class="topic-picker-actions"><button type="button" @click="selectAllTopics">Chọn tất cả</button><button type="button" @click="selectedTopicKeys = []">Bỏ chọn</button></div></div><div class="topic-options"><label v-for="topic in questionTopics" :key="topic.key" class="topic-option" :class="{ selected: selectedTopicKeys.includes(topic.key), uncategorized: topic.uncategorized }"><input v-model="selectedTopicKeys" type="checkbox" :value="topic.key" /><span class="topic-option-copy"><strong>{{ topic.label }}</strong><small>{{ topic.uncategorized ? 'Câu hỏi không có chủ đề · ' : '' }}{{ topic.count }} câu</small></span><span class="topic-check">✓</span></label></div></div><footer class="topic-modal-footer"><div><strong>{{ selectedQuestionCount }} câu đã chọn</strong><small v-if="!selectedTopicKeys.length" class="need-questions">Hãy chọn ít nhất một chủ đề.</small><small v-else-if="selectedQuestionCount < (pendingDuration?.questions || 0)" class="need-questions">Cần thêm {{ (pendingDuration?.questions || 0) - selectedQuestionCount }} câu. Hãy chọn thêm chủ đề.</small><small v-else>Đủ câu hỏi để tạo đề ngẫu nhiên.</small></div><div><button class="modal-cancel" type="button" @click="closeTopicModal">Hủy</button><button class="modal-create" type="button" :disabled="!pendingDuration || !canStartDuration(pendingDuration.questions)" @click="createSelectedExam">Tạo đề thi <span>→</span></button></div></footer></section></div></Teleport>
    </section>
  </QuizLayout>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import { getSubjectTheme } from '../../utils/subjectTheme'
import httpClient from '../../services/httpClient'

const route = useRoute()
const router = useRouter()
const subjectMap = { toan: 'Toán', 'tieng-anh': 'Tiếng Anh', 'tieng-viet': 'Tiếng Việt' }
const subject = computed(() => subjectMap[route.params.subjectSlug] || 'Môn học')
const subjectClass = computed(() => route.params.subjectSlug || '')
const icon = computed(() => route.params.subjectSlug === 'toan' ? '🔢' : route.params.subjectSlug === 'tieng-anh' ? '🌍' : '📖')
const grades = Array.from({ length: 9 }, (_, index) => index + 4)
const requestedGrade = Number(route.query.grade)
const grade = ref(grades.includes(requestedGrade) ? requestedGrade : 5)
const activeMode = ref('')
const lessons = ref([])
const selectedLesson = ref(null)
const loading = ref(false)
const error = ref('')
const bankQuestionCount = ref(0)
const questionTopics = ref([])
const selectedTopicKeys = ref([])
const topicsLoading = ref(false)
const topicsError = ref('')
const showTopicModal = ref(false)
const pendingDuration = ref(null)
const selectedQuestionCount = computed(() => questionTopics.value.filter(item => selectedTopicKeys.value.includes(item.key)).reduce((sum, item) => sum + item.count, 0))
const selectedTopicValues = computed(() => questionTopics.value.filter(item => selectedTopicKeys.value.includes(item.key)).map(item => item.topic))
const modes = [
  { id: 'Study', title: 'Học tập', icon: '🌱', kind: 'study', description: 'Đọc bài giảng, lý thuyết và ví dụ theo từng môn.' },
  { id: 'Test', title: 'Kiểm tra', icon: '📝', kind: 'test', description: 'Bài 15 hoặc 60 phút, câu hỏi được rút ngẫu nhiên.' },
  { id: 'ExamRoom', title: 'Chế độ thi', icon: '⏱️', kind: 'exam', description: 'Thi có tính giờ với bộ câu hỏi ngẫu nhiên.' }
]
const durations = [{ minutes: 15, questions: 10, icon: '⚡' }, { minutes: 60, questions: 40, icon: '🎯' }]
const durationOptions = computed(() => activeMode.value === 'ExamRoom' ? durations.filter(item => item.minutes === 60) : durations)
const modeTitle = id => ({ Study: 'Học tập', Test: 'Kiểm tra', ExamRoom: 'Chế độ thi' }[id] || id)
const sessionPath = (mode, duration) => ({ path: `/quiz-session/${route.params.subjectSlug}/${mode}/${grade.value}/${duration}`, query: { topics: selectedTopicValues.value.map(topic => topic ?? '__uncategorized__') } })
const selectAllTopics = () => { selectedTopicKeys.value = questionTopics.value.filter(item => item.count > 0).map(item => item.key) }
const canStartDuration = count => selectedTopicKeys.value.length > 0 && selectedQuestionCount.value >= count
const openTopicModal = duration => {
  if (topicsLoading.value || topicsError.value || bankQuestionCount.value < duration.questions) return
  pendingDuration.value = duration
  showTopicModal.value = true
}
const closeTopicModal = () => { showTopicModal.value = false; pendingDuration.value = null }
const createTestWithTopics = async duration => {
  if (!canStartDuration(duration.questions)) return
  await router.push(sessionPath('Test', duration.minutes))
}
const createSelectedExam = async () => {
  if (!pendingDuration.value || !canStartDuration(pendingDuration.value.questions)) return
  const path = sessionPath(activeMode.value, pendingDuration.value.minutes)
  closeTopicModal()
  await router.push(path)
}
const selectMode = async id => {
  closeTopicModal()
  error.value = ''
  selectedLesson.value = null
  if (id === 'Study') {
    router.push(`/learning/${route.params.subjectSlug}/${grade.value}`)
    return
  }
  activeMode.value = id
  await loadQuestionCount()
  if (id === 'ExamRoom' && !topicsError.value && questionTopics.value.length) {
    pendingDuration.value = durations.find(item => item.minutes === 60)
    showTopicModal.value = true
  }
}
const loadQuestionCount = async () => {
  topicsLoading.value = true
  topicsError.value = ''
  try {
    const [countResponse, topicResponse] = await Promise.all([
      httpClient.get('/quizzes/question-count', { params: { grade: grade.value, subject: subject.value } }),
      httpClient.get('/quizzes/question-topics', { params: { grade: grade.value, subject: subject.value } })
    ])
    bankQuestionCount.value = countResponse.data.availableCount || 0
    questionTopics.value = (topicResponse.data || []).map(item => {
      const topic = item.topic ?? item.Topic ?? null
      const count = item.count ?? item.Count ?? 0
      const uncategorized = topic === null || !String(topic).trim()
      return { topic: uncategorized ? null : String(topic).trim(), count, key: uncategorized ? 'topic-uncategorized' : `topic:${String(topic).trim()}`, label: uncategorized ? 'Câu hỏi chưa gán chủ đề' : String(topic).trim(), uncategorized }
    })
    if (!questionTopics.value.some(item => item.uncategorized)) {
      questionTopics.value.push({ topic: null, count: 0, key: 'topic-uncategorized', label: 'Câu hỏi chưa gán chủ đề', uncategorized: true })
    }
    selectAllTopics()
  } catch {
    bankQuestionCount.value = 0
    questionTopics.value = []
    selectedTopicKeys.value = []
    topicsError.value = 'Chưa tải được chủ đề từ ngân hàng câu hỏi.'
  } finally { topicsLoading.value = false }
}
const loadLessons = async () => {
  if (!subjectMap[route.params.subjectSlug]) return
  loading.value = true; error.value = ''
  try {
    const response = await httpClient.get('/quizzes/lessons', { params: { grade: grade.value, subject: subject.value } })
    lessons.value = response.data || []
    selectedLesson.value = lessons.value[0] || null
  } catch { error.value = 'Chưa tải được danh sách bài học. Em thử lại nhé.' }
  finally { loading.value = false }
}
watch(grade, () => { closeTopicModal(); if (activeMode.value === 'Study') loadLessons(); else if (activeMode.value) loadQuestionCount(); selectedLesson.value = null })
</script>

<style scoped>
.subject-hero{position:relative;padding:30px 32px;background:linear-gradient(110deg,#fff 0%,var(--subject-soft) 40%,var(--subject-tint) 100%);border-radius:24px;border:1px solid var(--subject-line)}.back-link{color:var(--subject-muted);text-decoration:none;font-size:14px;font-weight:700}.subject-icon{float:right;font-size:39px;display:grid;place-items:center;width:72px;height:72px;background:var(--subject-tint);color:var(--subject-primary);border-radius:21px}.eyebrow{display:block;color:var(--subject-muted);font-size:11px;letter-spacing:.13em;font-weight:750;margin-top:14px}.subject-hero h1{font-size:37px;color:var(--subject-ink);margin:3px 0}.subject-hero p{color:var(--subject-muted);margin:0}.mode-section,.quiz-section{margin-top:29px}.section-title{display:flex;align-items:end;justify-content:space-between;margin-bottom:15px}.section-title .eyebrow{margin:0}.section-title h2{font-size:23px;color:var(--subject-ink);margin:5px 0 0}.section-title label{font-size:12px;color:var(--subject-muted);font-weight:700}.section-title select{display:block;margin-top:5px;padding:9px 12px;border:1px solid var(--subject-line);border-radius:10px;background:#fff;color:var(--subject-ink)}.mode-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}.mode-card{--mode-accent:var(--subject-primary);--mode-ink:var(--subject-ink);position:relative;isolation:isolate;overflow:hidden;display:flex;flex-direction:column;align-items:flex-start;min-width:0;min-height:230px;padding:24px;border:1px solid var(--subject-line);border-radius:20px;background:linear-gradient(145deg,#fff 18%,var(--subject-soft));color:var(--mode-ink);text-align:left;text-decoration:none;cursor:pointer;box-shadow:0 3px 12px #30254f04;transition:transform .22s,border-color .22s,box-shadow .22s}.mode-card:hover,.mode-card:focus-visible,.mode-card.selected{transform:translateY(-5px);border-color:var(--subject-primary);box-shadow:0 18px 38px color-mix(in srgb,var(--subject-primary) 15%,transparent);outline:none}.mode-card:hover:before,.mode-card:focus-visible:before,.mode-card.selected:before{transform:scale(1.12) translate(-5px,5px)}.mode-card:hover:after,.mode-card:focus-visible:after,.mode-card.selected:after{transform:scaleX(1)}.mode-icon{display:grid;width:58px;height:58px;place-items:center;margin-bottom:22px;border:1px solid color-mix(in srgb,var(--mode-accent) 16%,#fff);border-radius:18px;background:linear-gradient(145deg,#fff,var(--subject-tint));font-size:30px;box-shadow:0 7px 18px color-mix(in srgb,var(--subject-primary) 11%,transparent);transition:transform .2s,box-shadow .2s}.mode-card:hover .mode-icon,.mode-card.selected .mode-icon{transform:translateY(-2px) rotate(-4deg);box-shadow:0 10px 22px color-mix(in srgb,var(--subject-primary) 20%,transparent)}.mode-card strong{color:var(--mode-ink);font-size:18px;font-weight:750;letter-spacing:-.025em}.mode-card small{max-width:34ch;margin-top:9px;color:var(--subject-muted);line-height:1.8;font-size:13px}.mode-action{display:inline-flex;align-items:center;gap:7px;margin-top:22px;padding:8px 11px;border:1px solid color-mix(in srgb,var(--mode-accent) 14%,#fff);border-radius:10px;background:var(--subject-soft);color:var(--mode-accent);font-size:12px;font-weight:750;transition:background .18s,color .18s,transform .18s;border-color:var(--subject-line)}.mode-card:hover .mode-action,.mode-card.selected .mode-action{background:var(--subject-strong);color:#fff;transform:translateX(2px)}.quiz-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:13px}.quiz-card{display:flex;flex-direction:column;min-height:205px;padding:17px;background:#fff;border:1px solid var(--subject-line);border-radius:17px}.quiz-card-top{display:flex;justify-content:space-between;color:var(--subject-muted);font-size:12px;font-weight:700}.quiz-card h3{font-size:17px;color:var(--subject-ink);margin:18px 0 7px}.quiz-card p{font-size:13px;line-height:1.5;color:var(--subject-muted)}.start-button{display:flex;justify-content:space-between;align-items:center;margin-top:auto;padding:10px 12px;border-radius:11px;background:var(--subject-strong);color:white;text-decoration:none;font-size:13px;font-weight:750}.start-button b{font-size:18px}.empty-card{padding:24px;border:1px dashed var(--subject-line);border-radius:16px;text-align:center;background:#fff;color:var(--subject-muted)}.empty-card button{border:0;background:none;color:var(--subject-muted);font-weight:700}.loading-label{font-size:13px;color:var(--subject-muted)}.duration-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.duration-grid:has(.duration-card:only-child){grid-template-columns:minmax(0,1fr)}.duration-card{display:flex;flex-direction:column;align-items:flex-start;min-height:210px;padding:22px;border:1px solid var(--subject-line);border-radius:20px;background:#fff;box-shadow:0 9px 25px color-mix(in srgb,var(--subject-primary) 3.1%,transparent);text-decoration:none;transition:.15s}.duration-button{width:100%;text-align:left;color:inherit;font:inherit;cursor:pointer}.duration-button:disabled{cursor:not-allowed}.duration-card:hover{transform:translateY(-3px);border-color:var(--subject-line);box-shadow:0 14px 28px color-mix(in srgb,var(--subject-primary) 8.6%,transparent)}.duration-card.unavailable{opacity:.62;cursor:not-allowed;box-shadow:none}.duration-icon{font-size:28px}.duration-card strong{margin-top:12px;color:var(--subject-ink);font-size:22px}.duration-card span:not(.duration-icon){margin-top:4px;color:var(--subject-muted);font-weight:750}.duration-card small{margin-top:7px;color:var(--subject-muted)}.duration-card .need-questions{color:#b65b5b}.duration-card b{margin-top:auto;padding-top:15px;color:var(--subject-muted);font-size:13px}.random-note,.bank-note{color:var(--subject-muted);font-size:14px;line-height:1.6}.topic-picker{margin:16px 0;padding:18px;border:1px solid var(--subject-line);border-radius:18px;background:linear-gradient(135deg,#fff,var(--subject-soft));box-shadow:0 8px 24px color-mix(in srgb,var(--subject-primary) 6.3%,transparent)}.topic-picker-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:13px}.topic-picker-heading>div:first-child{display:grid;gap:4px}.topic-picker-heading strong{color:var(--subject-ink);font-size:14px}.topic-picker-heading small{color:var(--subject-muted);font-size:12px}.topic-picker-actions{display:flex;gap:7px}.topic-picker-actions button{padding:7px 10px;border:1px solid var(--subject-line);border-radius:9px;background:#fff;color:var(--subject-primary);font-size:12px;font-weight:750;cursor:pointer}.topic-options{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:9px}.topic-option{display:flex;align-items:center;gap:9px;min-width:0;padding:11px 12px;border:1px solid var(--subject-line);border-radius:12px;background:#fff;cursor:pointer;transition:.16s}.topic-option:hover{border-color:var(--subject-primary);transform:translateY(-1px)}.topic-option.selected{border-color:var(--subject-primary);background:var(--subject-soft);box-shadow:0 4px 12px color-mix(in srgb,var(--subject-primary) 7.8%,transparent)}.topic-option input{accent-color:var(--subject-primary)}.topic-option-copy{display:grid;gap:3px;min-width:0;flex:1}.topic-option-copy strong{overflow:hidden;color:var(--subject-ink);font-size:13px;text-overflow:ellipsis;white-space:nowrap}.topic-option-copy small{color:var(--subject-muted);font-size:11px}.topic-check{display:grid;flex:0 0 20px;height:20px;place-items:center;border-radius:50%;background:var(--subject-tint);color:transparent;font-size:12px;font-weight:750}.topic-option.selected .topic-check{background:var(--subject-strong);color:#fff}.no-topic-options{padding:15px;border-radius:11px;background:#fff8eb;color:#997335;font-size:13px}.topic-modal-backdrop{position:fixed;z-index:2000;inset:0;display:grid;place-items:center;padding:20px;background:color-mix(in srgb,var(--subject-deep) 72.2%,transparent);backdrop-filter:blur(8px);animation:modal-fade-in .18s ease-out}.topic-modal{display:flex;width:min(740px,100%);max-height:min(86vh,820px);flex-direction:column;overflow:hidden;border:1px solid #ffffffa8;border-radius:24px;background:#fff;box-shadow:0 30px 100px color-mix(in srgb,var(--subject-primary) 50.2%,transparent);animation:modal-rise-in .22s ease-out}.topic-modal-header{display:flex;align-items:flex-start;justify-content:space-between;gap:18px;padding:23px 25px;background:linear-gradient(110deg,var(--subject-soft),var(--subject-tint));color:var(--subject-ink)}.topic-modal-header>div{display:flex;align-items:center;gap:15px}.topic-modal-icon{display:grid;flex:0 0 52px;height:52px;place-items:center;border:1px solid var(--subject-line);border-radius:17px;background:#fff;font-size:27px}.topic-modal-header .eyebrow{margin:0;color:var(--subject-primary)}.topic-modal-header h2{margin:5px 0;color:var(--subject-ink);font-size:23px;letter-spacing:-.03em}.topic-modal-header p{margin:0;color:var(--subject-muted);font-size:13px;line-height:1.5}.topic-modal-close{display:grid;flex:0 0 36px;height:36px;place-items:center;border:1px solid var(--subject-line);border-radius:11px;background:#fff;color:var(--subject-primary);font-size:25px;cursor:pointer}.topic-modal-close:hover{background:var(--subject-tint)}.topic-modal-body{overflow:auto;padding:20px 24px}.modal-duration-section{margin-bottom:18px}.modal-duration-section>strong{display:block;margin-bottom:9px;color:var(--subject-ink);font-size:13px}.modal-duration-options{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:9px}.modal-duration-options button{display:grid;grid-template-columns:auto 1fr;align-items:center;column-gap:9px;padding:11px 13px;border:1px solid var(--subject-line);border-radius:12px;background:#fff;text-align:left;cursor:pointer;transition:.15s}.modal-duration-options button:hover:not(:disabled){border-color:var(--subject-primary);transform:translateY(-1px)}.modal-duration-options button.active{border-color:var(--subject-primary);background:var(--subject-soft);box-shadow:0 4px 12px color-mix(in srgb,var(--subject-primary) 8.2%,transparent)}.modal-duration-options button>span{grid-row:span 2;font-size:21px}.modal-duration-options button b{color:var(--subject-ink);font-size:13px}.modal-duration-options button small{margin-top:3px;color:var(--subject-muted);font-size:11px}.modal-duration-options button:disabled{opacity:.45;cursor:not-allowed}.topic-modal .topic-picker-heading{align-items:center}.topic-modal .topic-options{grid-template-columns:repeat(2,minmax(0,1fr))}.topic-modal-footer{display:flex;align-items:center;justify-content:space-between;gap:14px;padding:16px 24px;border-top:1px solid var(--subject-line);background:#fcfbfe}.topic-modal-footer>div:first-child{display:grid;gap:4px}.topic-modal-footer strong{color:var(--subject-ink);font-size:14px}.topic-modal-footer small{color:var(--subject-muted);font-size:12px}.topic-modal-footer small.need-questions{color:#b65b5b}.topic-modal-footer>div:last-child{display:flex;gap:9px}.modal-cancel,.modal-create{min-height:42px;padding:0 15px;border-radius:11px;font-size:13px;font-weight:750;cursor:pointer}.modal-cancel{border:1px solid var(--subject-line);background:#fff;color:var(--subject-primary)}.modal-create{border:0;background:linear-gradient(135deg,var(--subject-strong),var(--subject-strong));color:#fff;box-shadow:0 8px 18px color-mix(in srgb,var(--subject-primary) 19.6%,transparent)}.modal-create span{margin-left:10px;font-size:17px}.modal-create:disabled{opacity:.48;box-shadow:none;cursor:not-allowed}@keyframes modal-fade-in{from{opacity:0}to{opacity:1}}@keyframes modal-rise-in{from{opacity:0;transform:translateY(12px) scale(.985)}to{opacity:1;transform:translateY(0) scale(1)}}.lesson-layout{display:grid;grid-template-columns:minmax(220px,.8fr) minmax(0,1.7fr);gap:16px;align-items:start}.lesson-list{display:grid;gap:9px}.lesson-choice{display:flex;flex-direction:column;gap:6px;padding:15px;text-align:left;border:1px solid var(--subject-line);border-radius:15px;background:#fff;cursor:pointer}.lesson-choice.active{border-color:var(--subject-line);background:var(--subject-soft)}.lesson-grade{color:var(--subject-muted);font-size:10px;letter-spacing:.08em;font-weight:750}.lesson-choice strong{color:var(--subject-ink);font-size:14px}.lesson-choice small{color:var(--subject-muted);line-height:1.45}.lesson-article{padding:23px;background:#fff;border:1px solid var(--subject-line);border-radius:19px;box-shadow:0 8px 24px color-mix(in srgb,var(--subject-primary) 3.1%,transparent)}.lesson-article h2{margin:7px 0;color:var(--subject-ink);font-size:23px}.lesson-summary{color:var(--subject-muted);font-size:14px}.lesson-content{margin-top:17px;color:var(--subject-primary);line-height:1.8;font-size:14px;white-space:pre-line}
@media(max-width:900px){.mode-grid{grid-template-columns:repeat(2,1fr)}.quiz-grid{grid-template-columns:repeat(2,1fr)}.topic-options{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:600px){.subject-hero{padding:30px 32px;background:linear-gradient(110deg,#fff 0%,var(--subject-soft) 40%,var(--subject-tint) 100%);border:1px solid var(--subject-line);border-radius:24px}.subject-hero h1{font-size:30px}.mode-grid,.quiz-grid,.duration-grid,.lesson-layout,.topic-options{grid-template-columns:1fr}.topic-picker{padding:14px}.topic-picker-heading{align-items:flex-start;flex-direction:column}.topic-modal-backdrop{padding:10px}.topic-modal{max-height:92vh;border-radius:19px}.topic-modal-header{padding:18px;background:linear-gradient(110deg,var(--subject-soft),var(--subject-tint));color:var(--subject-ink)}.topic-modal-header>div{align-items:flex-start;gap:10px}.topic-modal-icon{flex-basis:40px;height:40px;border-radius:13px;font-size:21px;background:#fff;border:1px solid var(--subject-line)}.topic-modal-header h2{font-size:18px;color:var(--subject-ink)}.topic-modal-body{padding:14px}.topic-modal .topic-picker-heading{gap:9px}.topic-modal .topic-options{grid-template-columns:1fr}.topic-modal-footer{align-items:stretch;flex-direction:column;padding:13px 15px}.topic-modal-footer>div:last-child{justify-content:flex-end}.mode-card{min-height:230px;padding:24px;background:linear-gradient(145deg,#fff 18%,var(--subject-soft));box-shadow:0 3px 12px #30254f04;border-radius:20px;transition:transform .22s,border-color .22s,box-shadow .22s}.section-title{align-items:start;gap:10px;flex-direction:column}.lesson-article{padding:18px}}.mode-card:before{content:"";position:absolute;z-index:-1;top:-60px;right:-38px;width:175px;height:175px;border:1px solid color-mix(in srgb,var(--subject-primary) 18%,transparent);border-radius:50%;box-shadow:0 0 0 18px color-mix(in srgb,var(--subject-primary) 5%,transparent),0 0 0 42px color-mix(in srgb,var(--subject-primary) 3%,transparent);transition:transform .28s;pointer-events:none}
@media(prefers-reduced-motion:reduce){.mode-card:hover,.mode-card:focus-visible,.mode-card.selected,.mode-card:hover .mode-icon,.mode-card.selected .mode-icon,.mode-card:hover:before,.mode-card:focus-visible:before,.mode-card.selected:before{transform:none}}
</style>
