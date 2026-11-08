<template>
  <QuizLayout>
    <div v-if="loading" class="message">Đang chuẩn bị bài cho em…</div>
    <div v-else-if="error" class="message error">{{ error }} <RouterLink to="/">Về trang chọn bài</RouterLink></div>
    <template v-else-if="quiz">
      <section v-if="!attempt" class="intro-card">
        <RouterLink to="/" class="back-link">← Chọn bài khác</RouterLink>
        <div class="intro-icon">{{ icon }}</div>
        <span class="eyebrow">{{ quiz.mode === 'Tower' ? `LEO THÁP · LEVEL ${quiz.towerLevel}` : `${modeLabel} · LỚP ${quiz.grade}` }}</span>
        <h1>{{ quiz.title }}</h1>
        <p>{{ quiz.description }}</p>
        <div class="facts"><span>📚 {{ quiz.subject }}</span><span>📝 {{ questionCount }} câu hỏi</span><span v-if="quiz.durationMinutes">⏱ {{ quiz.durationMinutes }} phút</span><span v-else>🌿 Không giới hạn thời gian</span></div>
        <label v-if="!authenticated && quiz.mode !== 'Tower'" class="guest-name">Tên của em (không bắt buộc)<input v-model="guestName" maxlength="100" placeholder="Ví dụ: Minh Anh" /></label>
        <button class="begin-button" :disabled="starting" @click="begin">{{ starting ? 'Đang vào bài…' : 'Bắt đầu làm bài' }} <span>→</span></button>
      </section>

      <section v-else-if="!result" class="quiz-work">
        <div class="work-header"><div><span class="eyebrow">{{ quiz.mode === 'Tower' ? `LEO THÁP · LEVEL ${quiz.towerLevel}` : `${modeLabel} · ${quiz.subject.toUpperCase()} · LỚP ${quiz.grade}` }}</span><h1>{{ quiz.title }}</h1></div><div v-if="quiz.durationMinutes > 0" class="timer" :class="{ urgent: secondsLeft < 60 }">⏱ {{ clock }}</div></div>
        <div class="progress-track"><div :style="{ width: `${((answeredCount / quiz.questions.length) * 100) || 0}%` }"></div></div>
        <article v-for="(question, index) in quiz.questions" :key="question.id" class="question-card">
          <div class="question-label"><span>CÂU {{ index + 1 }}</span><span>{{ answered[question.id] ? 'Đã chọn' : 'Chưa chọn' }}</span></div>
          <h2>{{ question.content }}</h2>
          <label v-for="(option, optionIndex) in question.options" :key="option.id" class="answer-option" :class="{ chosen: answered[question.id] === option.id }">
            <input v-model="answered[question.id]" type="radio" :name="`question-${question.id}`" :value="option.id" @change="checkStudyAnswer(question, option.id)" />
            <span class="option-letter">{{ letters[optionIndex] }}</span><span>{{ option.content }}</span>
          </label>
          <div v-if="feedback[question.id]" class="study-feedback" :class="{ correct: feedback[question.id].isCorrect, incorrect: !feedback[question.id].isCorrect }">
            <strong>{{ feedback[question.id].isCorrect ? 'Đúng rồi! 🌟' : 'Mình cùng xem lại nhé.' }}</strong>
            <span v-if="!feedback[question.id].isCorrect">Đáp án đúng: {{ feedback[question.id].correctOption }}</span>
            <p v-if="feedback[question.id].explanation">{{ feedback[question.id].explanation }}</p>
          </div>
        </article>
        <div class="submit-row"><span>{{ answeredCount }} / {{ quiz.questions.length }} câu đã trả lời</span><button class="begin-button" :disabled="submitting" @click="submit">{{ submitting ? 'Đang chấm bài…' : 'Nộp bài' }} <span>✓</span></button></div>
      </section>
    </template>
  </QuizLayout>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'
import { guestRequestConfig } from '../../utils/guestIdentity'

const props = defineProps({
  id: { type: String, default: '' },
  subjectSlug: { type: String, default: '' },
  mode: { type: String, default: 'Test' },
  grade: { type: String, default: '5' },
  duration: { type: String, default: '15' },
  level: { type: String, default: '' }
})
const router = useRouter()
const quiz = ref(null)
const attempt = ref(null)
const answered = ref({})
const feedback = ref({})
const loading = ref(true)
const starting = ref(false)
const submitting = ref(false)
const error = ref('')
const guestName = ref('')
const secondsLeft = ref(0)
const result = ref(null)
const letters = ['A', 'B', 'C', 'D', 'E', 'F']
const authenticated = Boolean(localStorage.getItem('token'))
let timerId

const answeredCount = computed(() => Object.values(answered.value).filter(Boolean).length)
const clock = computed(() => `${String(Math.floor(secondsLeft.value / 60)).padStart(2, '0')}:${String(secondsLeft.value % 60).padStart(2, '0')}`)
const icon = computed(() => quiz.value?.subject === 'Toán' ? '🔢' : quiz.value?.subject === 'Tiếng Anh' ? '🌍' : '📖')
const modeLabel = computed(() => ({ Study: 'HỌC TẬP', Test: 'LÀM BÀI KIỂM TRA', ExamRoom: 'CHẾ ĐỘ PHÒNG THI' }[quiz.value?.mode] || 'BÀI TẬP'))
const questionCount = computed(() => quiz.value?.questions?.length || (Number(props.duration) === 60 ? 40 : 10))

const loadQuiz = async () => {
  if (!props.id) {
    const subjects = { toan: 'Toán', 'tieng-anh': 'Tiếng Anh', 'tieng-viet': 'Tiếng Việt' }
    const subject = subjects[props.subjectSlug]
    const isTower = props.mode === 'Tower'
    if (!subject || (isTower ? !(Number(props.level) >= 1 && Number(props.level) <= 1000) : !['Test', 'ExamRoom'].includes(props.mode) || ![15, 60].includes(Number(props.duration)))) {
      error.value = 'Thông tin bài làm không hợp lệ.'
      loading.value = false
      return
    }
    quiz.value = {
      title: isTower ? `Leo tháp · Level ${props.level} · ${subject}` : `${props.mode === 'Test' ? 'Kiểm tra' : 'Thi thử'} ${props.duration} phút · ${subject}`,
      description: isTower ? 'Câu hỏi được chọn ngẫu nhiên theo độ khó của tầng.' : 'Hệ thống sẽ chọn ngẫu nhiên câu hỏi từ ngân hàng ngay khi em bắt đầu.',
      grade: isTower ? 5 : Number(props.grade), subject, mode: props.mode,
      durationMinutes: isTower ? 0 : Number(props.duration), towerLevel: isTower ? Number(props.level) : null, questions: []
    }
    loading.value = false
    return
  }
  try {
    const response = await httpClient.get(`/quizzes/${props.id}`)
    quiz.value = response.data
  } catch {
    error.value = 'Không tìm thấy bài hoặc bài chưa được mở.'
  } finally {
    loading.value = false
  }
}

const begin = async () => {
  starting.value = true
  error.value = ''
  try {
    if (!props.id) {
      const response = props.mode === 'Tower'
        ? await httpClient.post('/quizzes/tower/sessions', { subject: quiz.value.subject, level: Number(props.level) }, guestRequestConfig())
        : await httpClient.post('/quizzes/sessions', {
          subject: quiz.value.subject, grade: Number(props.grade), mode: props.mode,
          durationMinutes: Number(props.duration), guestName: guestName.value
        }, guestRequestConfig())
      quiz.value = response.data.quiz
      attempt.value = response.data.attempt
    } else {
      const response = await httpClient.post(`/quizzes/${props.id}/attempts`, { guestName: guestName.value }, guestRequestConfig())
      attempt.value = response.data
    }
    if (quiz.value.durationMinutes > 0) {
      secondsLeft.value = Number(quiz.value.durationMinutes) * 60
      timerId = setInterval(() => {
        secondsLeft.value = Math.max(0, secondsLeft.value - 1)
        if (secondsLeft.value === 0) submit()
      }, 1000)
    }
  } catch {
    error.value = 'Chưa bắt đầu được bài làm. Em thử tải lại trang nhé.'
  } finally {
    starting.value = false
  }
}

const checkStudyAnswer = async (question, optionId) => {
  if (quiz.value?.mode !== 'Study' || !attempt.value) return
  feedback.value[question.id] = null
  try {
    const response = await httpClient.post(`/quizzes/attempts/${attempt.value.id}/answer`, { questionId: question.id, optionId }, guestRequestConfig())
    feedback.value[question.id] = response.data
  } catch {
    error.value = 'Chưa kiểm tra được đáp án. Em vẫn có thể tiếp tục làm bài.'
  }
}

const submit = async () => {
  if (!attempt.value || submitting.value || result.value) return
  submitting.value = true
  if (timerId) clearInterval(timerId)
  try {
    const answers = quiz.value.questions.map(question => ({ questionId: question.id, optionId: answered.value[question.id] || null }))
    const response = await httpClient.post(`/quizzes/attempts/${attempt.value.id}/submit`, { answers }, guestRequestConfig())
    result.value = response.data
    sessionStorage.setItem(`quiz-result-${attempt.value.id}`, JSON.stringify(response.data))
    await router.replace(`/quiz-result/${attempt.value.id}`)
  } catch (err) {
    error.value = err?.response?.data?.message || 'Chưa nộp được bài. Em thử lại nhé.'
    submitting.value = false
  }
}

onMounted(loadQuiz)
onUnmounted(() => { if (timerId) clearInterval(timerId) })
</script>

<style scoped>
.eyebrow{font-size:11px;letter-spacing:.13em;font-weight:900;color:#7780d9}.intro-card{max-width:720px;margin:22px auto;background:#fff;border:1px solid #e8ecf5;border-radius:28px;padding:36px;text-align:center;box-shadow:0 15px 40px #2534660b}.back-link{display:block;text-align:left;color:#6573cf;text-decoration:none;font-weight:700}.intro-icon{font-size:52px;margin:18px auto 10px}.intro-card h1,.work-header h1{color:#293966;font-size:30px;margin:8px 0}.intro-card p{color:#8189a1}.facts{display:flex;justify-content:center;flex-wrap:wrap;gap:10px;margin:22px 0}.facts span{background:#f4f6fc;padding:10px 13px;border-radius:12px;color:#68728f;font-weight:700;font-size:13px}.guest-name{display:block;max-width:380px;margin:18px auto;text-align:left;color:#69738e;font-weight:700;font-size:13px}.guest-name input{width:100%;display:block;margin-top:7px;border:1px solid #e0e5f0;border-radius:12px;padding:12px}.begin-button{border:0;border-radius:14px;padding:13px 20px;background:linear-gradient(135deg,#6477ea,#8a64da);color:#fff;font-weight:900;box-shadow:0 10px 22px #7474db30}.begin-button span{font-size:18px;margin-left:12px}.begin-button:disabled{opacity:.6}.quiz-work{max-width:820px;margin:auto}.work-header{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}.work-header h1{font-size:25px}.timer{white-space:nowrap;background:#fff1ce;border-radius:14px;padding:12px 15px;color:#a56e15;font-weight:900;font-size:18px}.timer.urgent{background:#ffe4e5;color:#d33d4d}.progress-track{height:8px;border-radius:10px;background:#e6e9f3;margin-bottom:19px;overflow:hidden}.progress-track div{height:100%;background:linear-gradient(90deg,#7c83ea,#53c5a0);transition:width .2s}.question-card{background:#fff;border:1px solid #e7ebf4;border-radius:20px;padding:22px;margin:14px 0;box-shadow:0 8px 24px #25346608}.question-label{display:flex;justify-content:space-between;color:#8a91a8;font-size:11px;font-weight:900;letter-spacing:.08em}.question-card h2{font-size:19px;line-height:1.5;color:#2c3a65;margin:12px 0 17px}.answer-option{display:flex;align-items:center;gap:12px;padding:12px;border:1px solid #e9ecf4;border-radius:13px;margin:9px 0;cursor:pointer;color:#57617e}.answer-option.chosen{border-color:#8389e8;background:#f4f4ff;color:#343d75}.answer-option input{accent-color:#7077df}.option-letter{display:grid;place-items:center;width:29px;height:29px;border-radius:9px;background:#f1f2f9;font-size:12px;font-weight:900}.submit-row{display:flex;align-items:center;justify-content:space-between;padding:15px 2px;color:#8189a0;font-weight:700}.message{padding:30px;text-align:center;color:#74809b;background:white;border-radius:18px}.message.error{color:#b64b54}.message a{display:block;margin-top:10px;color:#606fda}
@media(max-width:620px){.intro-card{padding:22px 16px}.intro-card h1{font-size:25px}.work-header{align-items:start;gap:10px}.timer{font-size:14px;padding:9px}.work-header h1{font-size:20px}.question-card{padding:17px}.submit-row{gap:12px;font-size:12px}}
.study-feedback{display:flex;flex-direction:column;gap:5px;margin-top:14px;padding:13px 15px;border-radius:12px;font-size:13px}.study-feedback p{margin:0;line-height:1.5}.study-feedback.correct{background:#eaf8f1;color:#26835f}.study-feedback.incorrect{background:#fff6e5;color:#a66c20}
</style>
