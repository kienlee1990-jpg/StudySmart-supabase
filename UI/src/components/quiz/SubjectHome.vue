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
      <div class="section-title"><div><span class="eyebrow">{{ modeTitle(activeMode) }} · LỚP {{ grade }}</span><h2>Chọn thời lượng bài làm</h2></div></div>
      <p class="random-note">Hệ thống rút ngẫu nhiên câu hỏi khác nhau từ ngân hàng. Hiện có <strong>{{ bankQuestionCount }}</strong> câu cho môn {{ subject }}, lớp {{ grade }}.</p>
      <div class="duration-grid"><RouterLink v-for="duration in durations" :key="duration.minutes" :to="sessionPath(activeMode, duration.minutes)" class="duration-card" :class="{ unavailable: bankQuestionCount < duration.questions }" :aria-disabled="bankQuestionCount < duration.questions" @click="bankQuestionCount < duration.questions && $event.preventDefault()"><span class="duration-icon">{{ duration.icon }}</span><strong>{{ duration.minutes }} phút</strong><span>{{ duration.questions }} câu hỏi ngẫu nhiên</span><small>{{ activeMode === 'Test' ? 'Làm bài kiểm tra có tính giờ' : 'Thi thử trong phòng thi có tính giờ' }}</small><small v-if="bankQuestionCount < duration.questions" class="need-questions">Cần {{ duration.questions }} câu; ngân hàng hiện có {{ bankQuestionCount }}.</small><b v-else>Bắt đầu →</b></RouterLink></div>
    </section>
  </QuizLayout>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'

const route = useRoute()
const subjectMap = { toan: 'Toán', 'tieng-anh': 'Tiếng Anh', 'tieng-viet': 'Tiếng Việt' }
const subject = computed(() => subjectMap[route.params.subjectSlug] || 'Môn học')
const subjectClass = computed(() => route.params.subjectSlug || '')
const icon = computed(() => route.params.subjectSlug === 'toan' ? '🔢' : route.params.subjectSlug === 'tieng-anh' ? '🌍' : '📖')
const grades = Array.from({ length: 8 }, (_, index) => index + 5)
const requestedGrade = Number(route.query.grade)
const grade = ref(grades.includes(requestedGrade) ? requestedGrade : 5)
const activeMode = ref('')
const lessons = ref([])
const selectedLesson = ref(null)
const loading = ref(false)
const error = ref('')
const bankQuestionCount = ref(0)
const modes = [
  { id: 'Study', title: 'Học tập', icon: '🌱', kind: 'study', description: 'Đọc bài giảng, lý thuyết và ví dụ theo từng môn.' },
  { id: 'Test', title: 'Kiểm tra', icon: '📝', kind: 'test', description: 'Bài 15 hoặc 60 phút, câu hỏi được rút ngẫu nhiên.' },
  { id: 'ExamRoom', title: 'Chế độ thi', icon: '⏱️', kind: 'exam', description: 'Thi có tính giờ với bộ câu hỏi ngẫu nhiên.' }
]
const durations = [{ minutes: 15, questions: 10, icon: '⚡' }, { minutes: 60, questions: 40, icon: '🎯' }]
const modeTitle = id => ({ Study: 'Học tập', Test: 'Kiểm tra', ExamRoom: 'Chế độ thi' }[id] || id)
const sessionPath = (mode, duration) => `/quiz-session/${route.params.subjectSlug}/${mode}/${grade.value}/${duration}`
const selectMode = id => { activeMode.value = id; error.value = ''; selectedLesson.value = null; if (id === 'Study') loadLessons(); else loadQuestionCount() }
const loadQuestionCount = async () => {
  try {
    const response = await httpClient.get('/quizzes/question-count', { params: { grade: grade.value, subject: subject.value } })
    bankQuestionCount.value = response.data.availableCount || 0
  } catch { bankQuestionCount.value = 0 }
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
watch(grade, () => { if (activeMode.value === 'Study') loadLessons(); else if (activeMode.value) loadQuestionCount(); selectedLesson.value = null })
</script>

<style scoped>
.subject-hero{position:relative;padding:25px 32px 30px;background:linear-gradient(120deg,#eaf6ef,#f3f2ff);border-radius:24px}.subject-hero.tieng-anh{background:linear-gradient(120deg,#e9f0ff,#f4f1ff)}.subject-hero.tieng-viet{background:linear-gradient(120deg,#fff1e6,#fff8ed)}.back-link{color:#6874cf;text-decoration:none;font-size:13px;font-weight:800}.subject-icon{float:right;font-size:54px}.eyebrow{display:block;color:#7480d7;font-size:10px;letter-spacing:.13em;font-weight:900;margin-top:14px}.subject-hero h1{font-size:37px;color:#2d3a67;margin:3px 0}.subject-hero p{color:#7a8299;margin:0}.mode-section,.quiz-section{margin-top:29px}.section-title{display:flex;align-items:end;justify-content:space-between;margin-bottom:15px}.section-title .eyebrow{margin:0}.section-title h2{font-size:23px;color:#303d68;margin:5px 0 0}.section-title label{font-size:11px;color:#77809a;font-weight:800}.section-title select{display:block;margin-top:5px;padding:9px 12px;border:1px solid #e1e6f0;border-radius:10px;background:#fff;color:#39466a}.mode-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.mode-card{display:flex;flex-direction:column;align-items:flex-start;min-height:200px;text-align:left;padding:17px;border:1px solid #e7eaf4;border-radius:18px;background:#fff;color:#2e3a65;text-decoration:none;cursor:pointer;box-shadow:0 7px 18px #25346608;transition:.15s}.mode-card:hover,.mode-card.selected{transform:translateY(-3px);border-color:#8589e6;box-shadow:0 12px 24px #5d68cf18}.mode-icon{font-size:28px;margin-bottom:12px}.mode-card strong{font-size:15px}.mode-card small{margin-top:8px;color:#8189a0;line-height:1.5;font-size:11px}.mode-action{margin-top:auto;padding-top:12px;color:#6975d2;font-size:11px;font-weight:900}.mode-card.ranking{background:linear-gradient(135deg,#fff8e9,#fff);border-color:#f4e8c9}.mode-card.ranking .mode-action{color:#c48928}.quiz-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:13px}.quiz-card{display:flex;flex-direction:column;min-height:205px;padding:17px;background:#fff;border:1px solid #e7eaf3;border-radius:17px}.quiz-card-top{display:flex;justify-content:space-between;color:#7d86a0;font-size:11px;font-weight:800}.quiz-card h3{font-size:17px;color:#344064;margin:18px 0 7px}.quiz-card p{font-size:12px;line-height:1.5;color:#838ba0}.start-button{display:flex;justify-content:space-between;align-items:center;margin-top:auto;padding:10px 12px;border-radius:11px;background:#6674e3;color:white;text-decoration:none;font-size:12px;font-weight:900}.start-button b{font-size:18px}.empty-card{padding:24px;border:1px dashed #dce1ee;border-radius:16px;text-align:center;background:#fff;color:#7d879f}.empty-card button{border:0;background:none;color:#626fd5;font-weight:800}.loading-label{font-size:12px;color:#8790a5}.duration-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.duration-card{display:flex;flex-direction:column;align-items:flex-start;min-height:210px;padding:22px;border:1px solid #e6e9f4;border-radius:20px;background:#fff;box-shadow:0 9px 25px #25346608;text-decoration:none;transition:.15s}.duration-card:hover{transform:translateY(-3px);border-color:#878be8;box-shadow:0 14px 28px #5967cf16}.duration-card.unavailable{opacity:.62;cursor:not-allowed;box-shadow:none}.duration-icon{font-size:28px}.duration-card strong{margin-top:12px;color:#303d68;font-size:22px}.duration-card span:not(.duration-icon){margin-top:4px;color:#6572d0;font-weight:900}.duration-card small{margin-top:7px;color:#858da1}.duration-card .need-questions{color:#b65b5b}.duration-card b{margin-top:auto;padding-top:15px;color:#6875d6;font-size:12px}.random-note,.bank-note{color:#828ba1;font-size:13px;line-height:1.6}.lesson-layout{display:grid;grid-template-columns:minmax(220px,.8fr) minmax(0,1.7fr);gap:16px;align-items:start}.lesson-list{display:grid;gap:9px}.lesson-choice{display:flex;flex-direction:column;gap:6px;padding:15px;text-align:left;border:1px solid #e8ecf5;border-radius:15px;background:#fff;cursor:pointer}.lesson-choice.active{border-color:#8389e8;background:#f7f7ff}.lesson-grade{color:#7b83d4;font-size:9px;letter-spacing:.08em;font-weight:900}.lesson-choice strong{color:#344064;font-size:13px}.lesson-choice small{color:#838ba0;line-height:1.45}.lesson-article{padding:23px;background:#fff;border:1px solid #e8ecf5;border-radius:19px;box-shadow:0 8px 24px #25346608}.lesson-article h2{margin:7px 0;color:#303d68;font-size:23px}.lesson-summary{color:#828ba0;font-size:13px}.lesson-content{margin-top:17px;color:#505b76;line-height:1.8;font-size:14px;white-space:pre-line}
@media(max-width:900px){.mode-grid{grid-template-columns:repeat(2,1fr)}.quiz-grid{grid-template-columns:repeat(2,1fr)}}@media(max-width:600px){.subject-hero{padding:20px}.subject-hero h1{font-size:30px}.mode-grid,.quiz-grid,.duration-grid,.lesson-layout{grid-template-columns:1fr}.mode-card{min-height:165px}.section-title{align-items:start;gap:10px;flex-direction:column}.lesson-article{padding:18px}}
</style>
