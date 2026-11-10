<template>
  <QuizLayout :subject="quiz?.subject">
    <div v-if="loading" class="message">Đang chuẩn bị bài cho em…</div>
    <div v-else-if="error" class="message error">{{ error }} <RouterLink to="/">Về trang chọn bài</RouterLink></div>
    <template v-else-if="quiz">
      <section v-if="!attempt" class="intro-card" :class="{ 'intro-exam': quiz.mode === 'ExamRoom' }">
        <RouterLink to="/" class="back-link">← Chọn bài khác</RouterLink>
        <div class="intro-icon">{{ quiz.subject === 'Toán' ? '🔢' : quiz.subject === 'Tiếng Anh' ? '🌍' : '📖' }}</div>
        <span class="eyebrow">{{ quiz.mode === 'Tower' ? `LEO THÁP · LEVEL ${quiz.towerLevel}` : `${modeLabel} · LỚP ${quiz.grade}` }}</span>
        <h1>{{ quiz.title }}</h1>
        <p>{{ quiz.description }}</p>
        <div class="facts"><span>📚 {{ quiz.subject }}</span><span>📝 {{ questionCount }} câu hỏi</span><span v-if="isTower">15 giây / câu</span><span v-else-if="quiz.durationMinutes">⏱ {{ quiz.durationMinutes }} phút</span><span v-else>Không giới hạn thời gian</span></div>
        <label v-if="!authenticated && quiz.mode !== 'Tower'" class="guest-name">Tên của em (không bắt buộc)<input v-model="guestName" maxlength="100" placeholder="Ví dụ: Minh Anh" /></label>
        <button class="begin-button" :disabled="starting" @click="begin">{{ starting ? 'Đang vào bài…' : 'Bắt đầu làm bài' }} <span>→</span></button>
      </section>

      <section v-else-if="!result" class="quiz-work" :class="{ 'immersive-exam': quiz.mode === 'Tower' || quiz.mode === 'ExamRoom' }">
        <template v-if="quiz.mode === 'Tower' || quiz.mode === 'ExamRoom'">
          <header class="tower-toolbar">
            <button class="tower-tool-button tower-exit" type="button" aria-label="Thoát bài thi" title="Thoát bài thi" @click="leaveSession">×</button>
            <div class="tower-session-name"><span>{{ quiz.mode === 'Tower' ? 'LEO THÁP' : modeLabel }}</span><strong>{{ quiz.mode === 'Tower' ? `${quiz.subject} · Tầng ${quiz.towerLevel}` : `${quiz.subject} · Lớp ${quiz.grade}` }}</strong><span v-if="isTower" class="immersive-timer" :class="{ urgent: secondsLeft <= 5 && !towerLocked }" role="timer">⏱ {{ secondsLeft }} giây</span><span v-else-if="quiz.durationMinutes > 0" class="immersive-timer" :class="{ urgent: secondsLeft < 60 }">⏱ {{ clock }}</span></div>
            <div class="tower-page-count">{{ activeQuestionIndex + 1 }}<span>/</span>{{ quiz.questions.length }}</div>
            <div class="tower-toolbar-actions">
              <button v-if="!isTower" class="tower-tool-button" type="button" :aria-expanded="questionMenuOpen" aria-label="Mở danh sách câu hỏi" title="Danh sách câu hỏi" @click="questionMenuOpen = !questionMenuOpen">☷</button>
              <button class="tower-tool-button" type="button" :aria-pressed="isFullscreen" :aria-label="isFullscreen ? 'Thoát toàn màn hình' : 'Toàn màn hình'" title="Toàn màn hình" @click="toggleFullscreen">{{ isFullscreen ? '↙' : '⛶' }}</button>
            </div>
            <div v-if="!isTower && questionMenuOpen" class="tower-question-menu">
              <div class="tower-question-menu-head"><strong>Câu hỏi</strong><span>{{ answeredCount }}/{{ quiz.questions.length }} đã chọn</span></div>
              <div class="tower-question-menu-grid">
                <button v-for="(question, index) in quiz.questions" :key="question.id" type="button" :class="{ current: index === activeQuestionIndex, answered: answered[question.id] }" :aria-label="`Câu ${index + 1}${answered[question.id] ? ', đã chọn đáp án' : ''}`" @click="goToQuestion(index)">{{ index + 1 }}</button>
              </div>
            </div>
          </header>

          <main v-if="activeQuestion" class="tower-exam-content">
            <article class="tower-question-panel">
              <span class="tower-prompt-hint">EM HÃY CHỌN ĐÁP ÁN ĐÚNG NHẤT</span>
              <h1>{{ activeQuestion.content }}</h1>
              <div class="tower-question-progress" aria-hidden="true"><span :style="{ width: `${((activeQuestionIndex + 1) / quiz.questions.length) * 100}%` }"></span></div>
            </article>

            <div class="tower-answer-grid">
              <label v-for="(option, optionIndex) in activeQuestion.options" :key="option.id" class="tower-answer-card" :class="[answerAccents[optionIndex], { chosen: answered[activeQuestion.id] === option.id, locked: isTower && towerLocked, correct: isTower && towerFeedback?.correctOptionId === option.id, incorrect: isTower && towerFeedback && answered[activeQuestion.id] === option.id && !towerFeedback.isCorrect }]">
                <input :checked="answered[activeQuestion.id] === option.id" :disabled="isTower && towerLocked" type="radio" :name="`question-${activeQuestion.id}`" :value="option.id" @change="chooseActiveAnswer(option.id)" />
                <span class="tower-answer-letter">{{ letters[optionIndex] }}</span>
                <span class="tower-answer-text">{{ option.content }}</span>
                <span v-if="answered[activeQuestion.id] === option.id" class="tower-selected-mark" aria-hidden="true">{{ isTower && towerFeedback && !towerFeedback.isCorrect ? '✕' : '✓' }}</span>
              </label>
            </div>

            <div v-if="isTower && towerLocked" class="tower-feedback" :class="{ correct: towerFeedback?.isCorrect, incorrect: towerFeedback && !towerFeedback.isCorrect }" role="status" aria-live="polite">
              <template v-if="towerFeedback"><strong>{{ towerFeedback.isCorrect ? 'Chính xác! 🌟' : towerTimedOut ? 'Hết giờ! Câu này tính là sai.' : 'Chưa chính xác.' }}</strong><span>Đáp án đúng: {{ towerFeedback.correctOption }}</span></template>
              <span v-else-if="!towerAnswerError">Đang chấm đáp án…</span>
              <template v-else><span>{{ towerAnswerError }}</span><button type="button" @click="gradeTowerAnswer">Thử lại</button></template>
            </div>
            <p v-if="submitError" class="tower-feedback incorrect" role="alert">{{ submitError }}</p>
            <div class="tower-bottom-bar">
              <span class="tower-answered-count"><strong>{{ isTower ? Object.keys(feedback).length : answeredCount }}</strong> / {{ quiz.questions.length }} {{ isTower ? 'câu đã chấm' : 'câu đã chọn' }}</span>
              <div class="tower-navigation">
                <button v-if="!isTower" class="tower-nav-button previous" type="button" :disabled="activeQuestionIndex === 0" @click="goToQuestion(activeQuestionIndex - 1)">← <span>Câu trước</span></button>
                <button v-if="activeQuestionIndex < quiz.questions.length - 1" class="tower-nav-button next" type="button" :disabled="isTower && !towerFeedback" @click="goToQuestion(activeQuestionIndex + 1)">Câu tiếp theo <span>→</span></button>
                <button v-else class="tower-submit-button" type="button" :disabled="submitting || (isTower && !towerFeedback)" @click="submit">{{ submitting ? 'Đang chấm bài…' : 'Nộp bài' }} <span>✓</span></button>
              </div>
            </div>
          </main>
        </template>

        <template v-else>
          <div class="work-header"><div><span class="eyebrow">{{ `${modeLabel} · ${quiz.subject.toUpperCase()} · LỚP ${quiz.grade}` }}</span><h1>{{ quiz.title }}</h1></div><div v-if="quiz.durationMinutes > 0" class="timer" :class="{ urgent: secondsLeft < 60 }">⏱ {{ clock }}</div></div>
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
        </template>
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
  level: { type: String, default: '' },
  topics: { type: [Array, String], default: () => [] }
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
const submitError = ref('')
const guestName = ref('')
const secondsLeft = ref(0)
const result = ref(null)
const activeQuestionIndex = ref(0)
const questionMenuOpen = ref(false)
const isFullscreen = ref(false)
const letters = ['A', 'B', 'C', 'D', 'E', 'F']
const answerAccents = ['accent-blue', 'accent-teal', 'accent-amber', 'accent-rose']
const isTower = computed(() => quiz.value?.mode === 'Tower')
const towerLocked = ref(false)
const towerTimedOut = ref(false)
const towerAnswerError = ref('')
const towerGrading = ref(false)
const towerFeedback = computed(() => feedback.value[activeQuestion.value?.id] ?? null)
const authenticated = Boolean(localStorage.getItem('token'))
let timerId
let timerDeadline = 0

const answeredCount = computed(() => Object.values(answered.value).filter(Boolean).length)
const activeQuestion = computed(() => quiz.value?.questions?.[activeQuestionIndex.value] ?? null)
const clock = computed(() => `${String(Math.floor(secondsLeft.value / 60)).padStart(2, '0')}:${String(secondsLeft.value % 60).padStart(2, '0')}`)
const modeLabel = computed(() => ({ Study: 'HỌC TẬP', Test: 'LÀM BÀI KIỂM TRA', ExamRoom: 'BÀI THI 60 PHÚT' }[quiz.value?.mode] || 'BÀI TẬP'))
const questionCount = computed(() => quiz.value?.questions?.length || (isTower.value ? 5 + Math.floor((Number(props.level) - 1) * 15 / 999) : Number(props.duration) === 60 ? 40 : 10))
const goToQuestion = index => {
  if (!quiz.value?.questions?.length) return
  if (isTower.value && (index !== activeQuestionIndex.value + 1 || !towerFeedback.value || index >= quiz.value.questions.length)) return
  activeQuestionIndex.value = Math.max(0, Math.min(index, quiz.value.questions.length - 1))
  questionMenuOpen.value = false
  if (isTower.value) startTowerCountdown()
}

const toggleFullscreen = async () => {
  try {
    if (document.fullscreenElement) await document.exitFullscreen()
    else await document.documentElement.requestFullscreen()
  } catch {
    isFullscreen.value = Boolean(document.fullscreenElement)
  }
}

const syncFullscreen = () => { isFullscreen.value = Boolean(document.fullscreenElement) }
const startCountdown = durationMinutes => {
  if (timerId) clearInterval(timerId)
  timerDeadline = Date.now() + Number(durationMinutes) * 60_000
  const updateCountdown = () => {
    secondsLeft.value = Math.max(0, Math.ceil((timerDeadline - Date.now()) / 1000))
    if (secondsLeft.value === 0) {
      if (timerId) clearInterval(timerId)
      timerId = undefined
      submit()
    }
  }
  updateCountdown()
  if (secondsLeft.value > 0) timerId = setInterval(updateCountdown, 250)
}
const startTowerCountdown = () => {
  if (timerId) clearInterval(timerId)
  towerLocked.value = false
  towerTimedOut.value = false
  towerAnswerError.value = ''
  timerDeadline = Date.now() + 15_000
  secondsLeft.value = 15
  const tick = () => {
    secondsLeft.value = Math.max(0, Math.ceil((timerDeadline - Date.now()) / 1000))
    if (secondsLeft.value === 0) finishTowerQuestion(null)
  }
  timerId = setInterval(tick, 100)
}
const chooseActiveAnswer = optionId => {
  if (isTower.value) return finishTowerQuestion(optionId)
  answered.value[activeQuestion.value.id] = optionId
  checkStudyAnswer(activeQuestion.value, optionId)
}
const finishTowerQuestion = optionId => {
  if (towerLocked.value || !activeQuestion.value) return
  // Check the deadline here too, in case a background tab delayed the timer.
  towerTimedOut.value = optionId == null || Date.now() >= timerDeadline
  answered.value[activeQuestion.value.id] = towerTimedOut.value ? null : optionId
  towerLocked.value = true
  if (timerId) clearInterval(timerId)
  timerId = undefined
  if (towerTimedOut.value) secondsLeft.value = 0
  gradeTowerAnswer()
}
const gradeTowerAnswer = async () => {
  if (towerGrading.value || towerFeedback.value) return
  towerGrading.value = true
  towerAnswerError.value = ''
  const questionId = activeQuestion.value.id
  try {
    const response = await httpClient.post(`/quizzes/attempts/${attempt.value.id}/answer`, {
      questionId, optionId: answered.value[questionId]
    }, guestRequestConfig())
    feedback.value[questionId] = response.data
  } catch {
    towerAnswerError.value = 'Chưa chấm được đáp án. Em thử lại nhé.'
  } finally {
    towerGrading.value = false
  }
}
const leaveSession = async () => {
  if (document.fullscreenElement) await document.exitFullscreen().catch(() => {})
  if (quiz.value?.mode === 'Tower') await router.push({ path: '/ranking', query: { subject: quiz.value.subject } })
  else await router.push('/')
}

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
      title: isTower ? `Leo tháp · Level ${props.level} · ${subject}` : `${props.mode === 'Test' ? 'Kiểm tra' : 'Bài thi'} ${props.duration} phút · ${subject}`,
      description: isTower ? 'Số câu Nâng cao tăng theo bậc; hai tầng liền kề không trùng câu hỏi. Mỗi câu có 15 giây, chấm ngay khi chọn hoặc hết giờ. Không được đổi đáp án hay quay lại câu đã qua.' : 'Hệ thống sẽ chọn ngẫu nhiên câu hỏi từ ngân hàng ngay khi em bắt đầu.',
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
  const isImmersiveExam = quiz.value && ['Tower', 'ExamRoom'].includes(quiz.value.mode)
  if (isImmersiveExam) document.body.classList.add('immersive-exam-active')
  if (isImmersiveExam && !document.fullscreenElement) {
    try {
      const fullscreenRequest = document.documentElement.requestFullscreen?.()
      fullscreenRequest?.catch(() => {})
    } catch {}
  }
  starting.value = true
  error.value = ''
  try {
    if (!props.id) {
      const response = props.mode === 'Tower'
        ? await httpClient.post('/quizzes/tower/sessions', { subject: quiz.value.subject, level: Number(props.level) }, guestRequestConfig())
        : await httpClient.post('/quizzes/sessions', {
          subject: quiz.value.subject, grade: Number(props.grade), mode: props.mode,
          durationMinutes: Number(props.duration), guestName: guestName.value,
          topics: (Array.isArray(props.topics) ? props.topics : props.topics ? [props.topics] : [])
            .map(topic => topic === '__uncategorized__' ? null : topic)
        }, guestRequestConfig())
      quiz.value = response.data.quiz
      attempt.value = response.data.attempt
    } else {
      const response = await httpClient.post(`/quizzes/${props.id}/attempts`, { guestName: guestName.value }, guestRequestConfig())
      attempt.value = response.data
    }
    activeQuestionIndex.value = 0
    if (['Tower', 'ExamRoom'].includes(quiz.value?.mode)) document.body.classList.add('immersive-exam-active')
    if (isTower.value) startTowerCountdown()
    else if (quiz.value.durationMinutes > 0) startCountdown(quiz.value.durationMinutes)
  } catch (err) {
    if (['Tower', 'ExamRoom'].includes(quiz.value?.mode) && !attempt.value) document.body.classList.remove('immersive-exam-active')
    error.value = err?.response?.data?.message || 'Chưa bắt đầu được bài làm. Em thử tải lại trang nhé.'
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
  if (isTower.value && (activeQuestionIndex.value !== quiz.value.questions.length - 1 || !towerFeedback.value)) return
  submitting.value = true
  submitError.value = ''
  if (timerId) clearInterval(timerId)
  timerId = undefined
  try {
    const answers = quiz.value.questions.map(question => ({ questionId: question.id, optionId: answered.value[question.id] || null }))
    const response = await httpClient.post(`/quizzes/attempts/${attempt.value.id}/submit`, { answers }, guestRequestConfig())
    result.value = response.data
    sessionStorage.setItem(`quiz-result-${attempt.value.id}`, JSON.stringify(response.data))
    await router.replace(`/quiz-result/${attempt.value.id}`)
  } catch (err) {
    const message = err?.response?.data?.message || 'Chưa nộp được bài. Em thử lại nhé.'
    if (isTower.value) submitError.value = message
    else error.value = message
    submitting.value = false
  }
}

onMounted(() => {
  document.addEventListener('fullscreenchange', syncFullscreen)
  loadQuiz()
})
onUnmounted(() => {
  if (timerId) clearInterval(timerId)
  document.removeEventListener('fullscreenchange', syncFullscreen)
  document.body.classList.remove('immersive-exam-active')
  if (document.fullscreenElement) document.exitFullscreen().catch(() => {})
})
</script>

<style scoped>
.eyebrow{font-size:12px;letter-spacing:.13em;font-weight:750;color:var(--subject-muted)}.intro-card{position:relative;isolation:isolate;overflow:hidden;max-width:820px;margin:30px auto;padding:clamp(28px,5vw,54px);border:1px solid var(--subject-line);border-radius:30px;background:radial-gradient(circle at 92% 4%,color-mix(in srgb,var(--subject-tint) 43.9%,transparent),transparent 27%),radial-gradient(circle at 8% 100%,color-mix(in srgb,var(--subject-tint) 43.9%,transparent),transparent 30%),linear-gradient(145deg,#fff 8%,#fcfaff 58%,var(--subject-soft));text-align:center;box-shadow:0 24px 65px color-mix(in srgb,var(--subject-primary) 8.6%,transparent)}.intro-card>*{position:relative;z-index:1}.back-link{display:block;width:max-content;text-align:left;color:var(--subject-muted);text-decoration:none;font-weight:700}.intro-card .back-link{padding:9px 13px;border:1px solid var(--subject-line);border-radius:12px;background:#ffffffb8}.intro-card .back-link:hover{border-color:var(--subject-line);background:#fff}.intro-icon{display:grid;width:86px;height:86px;place-items:center;margin:23px auto 18px;border:1px solid #ffffffc9;border-radius:26px;background:linear-gradient(145deg,var(--subject-soft),#fff);font-size:47px;box-shadow:0 12px 28px color-mix(in srgb,var(--subject-primary) 11%,transparent)}.intro-card.intro-exam{background:radial-gradient(circle at 90% 4%,color-mix(in srgb,var(--subject-tint) 53.3%,transparent),transparent 28%),radial-gradient(circle at 4% 100%,color-mix(in srgb,var(--subject-tint) 50.2%,transparent),transparent 32%),linear-gradient(145deg,#fff 4%,var(--subject-soft) 58%,var(--subject-soft))}.intro-card .eyebrow{display:inline-flex;align-items:center;justify-content:center;padding:8px 12px;border:1px solid var(--subject-line);border-radius:99px;background:var(--subject-soft);color:var(--subject-primary)}.intro-card h1,.work-header h1{color:var(--subject-ink);font-size:clamp(30px,4vw,42px);letter-spacing:-.04em;margin:13px 0}.intro-card p{max-width:620px;margin:0 auto;color:var(--subject-primary);font-size:16px;line-height:1.65}.facts{display:flex;justify-content:center;flex-wrap:wrap;gap:10px;margin:25px 0}.facts span{border:1px solid var(--subject-line);background:#ffffffc9;padding:11px 15px;border-radius:14px;color:var(--subject-primary);font-weight:700;font-size:14px;box-shadow:0 4px 12px color-mix(in srgb,var(--subject-primary) 3.1%,transparent)}.guest-name{display:block;max-width:380px;margin:18px auto;text-align:left;color:var(--subject-primary);font-weight:700;font-size:14px}.guest-name input{width:100%;display:block;margin-top:7px;border:1px solid var(--subject-line);border-radius:12px;padding:12px}.begin-button{border:0;border-radius:15px;padding:15px 23px;background:linear-gradient(120deg,var(--subject-strong),var(--subject-strong) 60%,var(--subject-strong));color:#fff;font-weight:750;font-size:15px;box-shadow:0 12px 26px color-mix(in srgb,var(--subject-primary) 20.8%,transparent);transition:transform .16s,box-shadow .16s}.begin-button:hover{transform:translateY(-2px);box-shadow:0 16px 32px color-mix(in srgb,var(--subject-primary) 26.7%,transparent)}.begin-button span{font-size:18px;margin-left:12px}.begin-button:disabled{opacity:.6}.quiz-work{max-width:820px;margin:auto}.work-header{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}.work-header h1{font-size:25px}.timer{white-space:nowrap;background:var(--subject-tint);border-radius:14px;padding:12px 15px;color:var(--subject-primary);font-weight:750;font-size:18px}.timer.urgent{background:#ffe4e5;color:#d33d4d}.progress-track{height:8px;border-radius:10px;background:var(--subject-tint);margin-bottom:19px;overflow:hidden}.progress-track div{height:100%;background:linear-gradient(90deg,var(--subject-line),var(--subject-primary));transition:width .2s}.question-card{background:#fff;border:1px solid var(--subject-line);border-radius:20px;padding:22px;margin:14px 0;box-shadow:0 8px 24px color-mix(in srgb,var(--subject-primary) 3.1%,transparent)}.question-label{display:flex;justify-content:space-between;color:var(--subject-muted);font-size:12px;font-weight:750;letter-spacing:.08em}.question-card h2{font-size:19px;line-height:1.5;color:var(--subject-ink);margin:12px 0 17px}.answer-option{display:flex;align-items:center;gap:12px;padding:12px;border:1px solid var(--subject-line);border-radius:13px;margin:9px 0;cursor:pointer;color:var(--subject-primary)}.answer-option.chosen{border-color:var(--subject-line);background:var(--subject-soft);color:var(--subject-primary)}.answer-option input{accent-color:var(--subject-muted)}.option-letter{display:grid;place-items:center;width:29px;height:29px;border-radius:9px;background:var(--subject-soft);font-size:13px;font-weight:750}.submit-row{display:flex;align-items:center;justify-content:space-between;padding:15px 2px;color:var(--subject-muted);font-weight:700}.message{padding:30px;text-align:center;color:var(--subject-muted);background:white;border-radius:18px}.message.error{color:#b64b54}.message a{display:block;margin-top:10px;color:var(--subject-muted)}
@media(max-width:620px){.intro-card{padding:22px 16px}.intro-card h1{font-size:25px}.work-header{align-items:start;gap:10px}.timer{font-size:14px;padding:9px}.work-header h1{font-size:20px}.question-card{padding:17px}.submit-row{gap:12px;font-size:13px}}
.study-feedback{display:flex;flex-direction:column;gap:5px;margin-top:14px;padding:13px 15px;border-radius:12px;font-size:14px}.study-feedback p{margin:0;line-height:1.5}.study-feedback.correct{background:#eaf8f1;color:#26835f}.study-feedback.incorrect{background:#fff6e5;color:#a66c20}
:global(body.immersive-exam-active){overflow:hidden;background:var(--subject-deep)}
:global(body.immersive-exam-active .quiz-app){min-height:100dvh;background:transparent}
:global(body.immersive-exam-active .notice-strip),:global(body.immersive-exam-active .site-header),:global(body.immersive-exam-active .quiz-footer),:global(body.immersive-exam-active .contact-footer),:global(body.immersive-exam-active .ai-widget){display:none!important}
:global(body.immersive-exam-active .quiz-main){width:100%!important;max-width:none!important;min-height:100dvh;margin:0!important;padding:0!important}
.immersive-exam{position:relative;isolation:isolate;display:flex;flex-direction:column;gap:14px;flex:1;width:100%;max-width:none;height:100dvh;min-height:100dvh;margin:0;padding:12px clamp(12px,1.5vw,28px) 16px;overflow:auto;color:var(--subject-ink);background:linear-gradient(135deg,var(--subject-soft),#fff 55%,var(--subject-tint))}
.immersive-exam::before{content:"";position:fixed;z-index:-1;inset:0;pointer-events:none;opacity:.15;background-image:radial-gradient(circle at 20% 30%,transparent 0 80px,color-mix(in srgb,var(--subject-line) 12.5%,transparent) 82px,transparent 84px),radial-gradient(circle at 72% 70%,transparent 0 120px,color-mix(in srgb,var(--subject-line) 12.5%,transparent) 122px,transparent 124px);background-size:420px 320px,560px 430px}
.tower-toolbar{position:relative;z-index:4;display:grid;grid-template-columns:auto minmax(100px,1fr) auto auto;align-items:center;gap:14px;min-height:48px}.tower-session-name{display:flex;flex-direction:column;gap:2px;color:var(--subject-muted);font-size:11px;letter-spacing:.14em;font-weight:700}.tower-session-name strong{color:var(--subject-ink);font-size:14px;letter-spacing:0}.immersive-timer{color:var(--subject-primary);font-size:13px;letter-spacing:0;font-weight:750;font-variant-numeric:tabular-nums;background:var(--subject-tint);border-radius:8px;padding:4px 8px;width:fit-content}.immersive-timer.urgent{color:#b83e4d;background:#fff0f0}.tower-tool-button{display:grid;place-items:center;width:46px;height:46px;border:1px solid var(--subject-line);border-radius:13px;background:#fff;color:var(--subject-primary);text-decoration:none;font:inherit;font-size:24px;line-height:1;cursor:pointer;transition:background .16s ease,transform .16s ease}.tower-tool-button:hover{background:var(--subject-tint);transform:translateY(-1px)}.tower-exit{font-size:31px}.tower-page-count{display:flex;align-items:center;gap:5px;padding:8px 13px;border-radius:13px;background:var(--subject-tint);color:var(--subject-strong);font-size:14px;font-weight:750;font-variant-numeric:tabular-nums}.tower-page-count span{color:var(--subject-muted)}.tower-toolbar-actions{display:flex;gap:8px}
.tower-question-menu{position:absolute;top:54px;right:0;width:min(340px,calc(100vw - 24px));padding:16px;border:1px solid var(--subject-line);border-radius:18px;background:var(--subject-soft);color:var(--subject-ink);box-shadow:0 18px 50px color-mix(in srgb,var(--subject-primary) 40%,transparent)}.tower-question-menu-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;font-size:14px}.tower-question-menu-head span{color:var(--subject-primary);font-size:12px}.tower-question-menu-grid{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:8px}.tower-question-menu-grid button{height:38px;border:1px solid var(--subject-line);border-radius:10px;background:#fff;color:var(--subject-primary);font-weight:700;cursor:pointer}.tower-question-menu-grid button.answered{border-color:var(--subject-primary);background:var(--subject-tint);color:var(--subject-ink)}.tower-question-menu-grid button.current{box-shadow:0 0 0 2px var(--subject-primary) inset;border-color:var(--subject-primary);color:var(--subject-primary)}
.tower-exam-content{flex:1;min-height:0;display:grid;grid-template-rows:minmax(260px,1fr) auto auto;gap:14px}.tower-question-panel{position:relative;display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:260px;padding:38px clamp(22px,7vw,120px) 48px;border:1px solid var(--subject-line);border-radius:24px;background:var(--subject-soft);box-shadow:0 5px 18px #30254f05;color:var(--subject-ink);text-align:center}.tower-prompt-hint{margin-bottom:22px;color:var(--subject-primary);font-size:clamp(10px,.8vw,13px);font-weight:750;letter-spacing:.16em}.tower-question-panel h1{max-width:1100px;margin:0;color:var(--subject-ink);font-size:clamp(24px,2.15vw,39px);line-height:1.42;font-weight:650;white-space:pre-wrap}.tower-question-progress{position:absolute;bottom:0;left:0;right:0;height:5px;overflow:hidden;border-radius:0 0 24px 24px;background:var(--subject-tint)}.tower-question-progress span{display:block;height:100%;border-radius:inherit;background:linear-gradient(90deg,var(--subject-primary),var(--subject-primary));transition:width .25s ease}
.tower-answer-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px}.tower-answer-card{position:relative;isolation:isolate;overflow:hidden;display:flex;align-items:center;justify-content:center;min-height:clamp(150px,27vh,260px);padding:30px 22px;border:4px solid var(--answer-accent);border-radius:20px;background:var(--answer-bg);color:var(--subject-ink);text-align:center;cursor:pointer;box-shadow:0 0 0 1px #ffffff99 inset,0 10px 24px color-mix(in srgb,var(--subject-primary) 12%,transparent);transition:transform .17s ease,box-shadow .17s ease,filter .17s ease,background .2s ease,border-color .2s ease;border-width:4px}.tower-answer-card:hover{z-index:2;--answer-accent:var(--subject-muted);--answer-glow:color-mix(in srgb,var(--subject-primary) 60%,transparent);--answer-bg:linear-gradient(145deg,var(--subject-tint),var(--subject-line) 48%,var(--subject-line));transform:translateY(-6px) scale(1.02);filter:saturate(1.08);box-shadow:0 0 0 2px #fff inset,0 16px 30px color-mix(in srgb,var(--subject-primary) 20%,transparent),0 0 24px var(--answer-glow)}.tower-answer-card:hover::after{animation-duration:1.15s;opacity:1}.tower-answer-card:active{transform:translateY(-2px) scale(.99)}.tower-answer-card.accent-blue,.tower-answer-card.accent-teal,.tower-answer-card.accent-amber,.tower-answer-card.accent-rose{--answer-accent:var(--subject-primary);--answer-bg:linear-gradient(145deg,#fff,var(--subject-tint));--answer-glow:color-mix(in srgb,var(--subject-primary) 12%,transparent)}.tower-answer-card.accent-blue:hover,.tower-answer-card.accent-teal:hover,.tower-answer-card.accent-amber:hover,.tower-answer-card.accent-rose:hover{--answer-accent:var(--subject-strong);--answer-bg:linear-gradient(145deg,var(--subject-soft),var(--subject-tint));--answer-glow:color-mix(in srgb,var(--subject-primary) 20%,transparent)}.tower-answer-card.chosen{filter:saturate(1.18);box-shadow:0 0 0 3px #fff9 inset,0 0 0 3px var(--answer-accent),0 0 30px var(--answer-glow),0 15px 32px color-mix(in srgb,var(--subject-primary) 33.3%,transparent);transform:translateY(-3px) scale(1.01)}@keyframes answer-shimmer{0%,15%{transform:translateX(-65%) rotate(8deg);opacity:0}30%{opacity:.8}60%,100%{transform:translateX(65%) rotate(8deg);opacity:0}}.tower-answer-card input{position:absolute;width:1px;height:1px;opacity:0;pointer-events:none}.tower-answer-card:focus-within{z-index:2;outline:3px solid #fff;outline-offset:4px;box-shadow:0 0 0 7px var(--subject-primary),0 18px 38px color-mix(in srgb,var(--subject-primary) 40%,transparent),0 0 34px var(--answer-glow)}.tower-answer-letter{z-index:1;position:absolute;top:10px;left:11px;display:grid;place-items:center;width:29px;height:29px;border-radius:9px;background:color-mix(in srgb,var(--answer-accent) 13%,white);color:var(--answer-accent);font-size:13px;font-weight:750}.tower-answer-text{position:relative;z-index:1;max-width:100%;font-size:clamp(17px,1.35vw,24px);line-height:1.45;font-weight:600;overflow-wrap:anywhere}.tower-selected-mark{z-index:1;position:absolute;top:10px;right:12px;display:grid;place-items:center;width:27px;height:27px;border-radius:50%;background:var(--answer-accent);color:#fff;font-size:15px;font-weight:750}
.tower-bottom-bar{display:flex;justify-content:space-between;align-items:center;gap:16px;min-height:54px;padding:0 2px}.tower-answered-count{color:var(--subject-muted);font-size:14px}.tower-answered-count strong{color:var(--subject-ink)}.tower-navigation{display:flex;align-items:center;gap:9px}.tower-nav-button,.tower-submit-button{min-height:44px;padding:0 16px;border:1px solid var(--subject-line);border-radius:12px;background:#fff;color:var(--subject-primary);font:inherit;font-size:14px;font-weight:700;cursor:pointer;transition:background .16s ease,transform .16s ease}.tower-nav-button:hover:not(:disabled){background:#ffffff2e;transform:translateY(-1px)}.tower-nav-button:disabled{opacity:.4;cursor:not-allowed}.tower-nav-button.next{border-color:var(--subject-strong);background:var(--subject-strong);color:#fff}.tower-submit-button{border:1px solid var(--subject-strong);background:linear-gradient(120deg,var(--subject-strong),var(--subject-strong));color:#fff;box-shadow:0 9px 22px color-mix(in srgb,var(--subject-primary) 33.3%,transparent)}.tower-submit-button:disabled{opacity:.65;cursor:wait}.tower-navigation button span{margin-left:5px}
@media(max-width:900px){.tower-answer-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.tower-answer-card{min-height:clamp(135px,21vh,205px);box-shadow:0 0 0 1px #ffffff99 inset,0 10px 24px color-mix(in srgb,var(--subject-primary) 12%,transparent);border-width:4px}}
@media(max-width:600px){.immersive-exam{gap:10px;padding:8px 10px max(12px,env(safe-area-inset-bottom));background:linear-gradient(135deg,var(--subject-soft),#fff 55%,var(--subject-tint));color:var(--subject-ink)}.tower-toolbar{grid-template-columns:auto 1fr auto;gap:8px;min-height:42px}.tower-session-name{grid-column:1;grid-row:1;font-size:10px;color:var(--subject-muted)}.tower-session-name strong{font-size:12px;color:var(--subject-ink)}.immersive-timer{font-size:11px;color:var(--subject-primary);background:var(--subject-tint);border-radius:8px;padding:4px 8px;width:fit-content}.tower-toolbar-actions{grid-column:3;grid-row:1;gap:6px}.tower-tool-button{width:40px;height:40px;border-radius:11px;font-size:21px;background:#fff;color:var(--subject-primary);border:1px solid var(--subject-line)}.tower-exit{font-size:28px}.tower-page-count{grid-column:2;grid-row:1;justify-self:center;padding:7px 11px;font-size:13px;background:var(--subject-tint);color:var(--subject-strong)}.tower-question-menu{top:48px}.tower-exam-content{grid-template-rows:minmax(230px,36vh) auto auto;gap:10px}.tower-question-panel{min-height:230px;padding:28px 18px 34px;border-radius:18px;box-shadow:0 5px 18px #30254f05}.tower-prompt-hint{margin-bottom:14px;font-size:10px}.tower-question-panel h1{font-size:clamp(21px,6vw,30px)}.tower-question-progress{border-radius:0 0 18px 18px}.tower-answer-grid{gap:9px}.tower-answer-card{min-height:clamp(115px,18vh,170px);padding:27px 12px 16px;border-width:4px;border-radius:16px;box-shadow:0 0 0 1px #ffffff99 inset,0 10px 24px color-mix(in srgb,var(--subject-primary) 12%,transparent)}.tower-answer-letter{top:7px;left:7px;width:25px;height:25px}.tower-answer-text{font-size:clamp(15px,4vw,19px)}.tower-selected-mark{top:7px;right:7px;width:23px;height:23px;font-size:14px}.tower-bottom-bar{align-items:flex-start;flex-direction:column;gap:9px;padding:0 1px}.tower-answered-count{font-size:12px;color:var(--subject-muted)}.tower-navigation{width:100%;justify-content:space-between}.tower-nav-button,.tower-submit-button{min-height:42px;padding:0 12px;font-size:13px;background:#fff;color:var(--subject-primary);border:1px solid var(--subject-line)}}
@media(max-width:370px){.tower-answer-grid{grid-template-columns:1fr}.tower-answer-card{min-height:105px;box-shadow:0 0 0 1px #ffffff99 inset,0 10px 24px color-mix(in srgb,var(--subject-primary) 12%,transparent);border-width:4px}.tower-exam-content{grid-template-rows:minmax(200px,32vh) auto auto}}
@media(prefers-reduced-motion:reduce){.tower-answer-card,.tower-tool-button,.tower-nav-button{transition:none}.tower-answer-card:hover{transform:translateY(-6px) scale(1.02);box-shadow:0 0 0 2px #fff inset,0 16px 30px color-mix(in srgb,var(--subject-primary) 20%,transparent),0 0 24px var(--answer-glow);filter:saturate(1.08)}}
.tower-exam-content:has(.tower-feedback){grid-template-rows:minmax(260px,1fr) auto auto auto}
.tower-feedback{display:flex;flex-wrap:wrap;align-items:center;gap:8px 18px;padding:14px 18px;border-radius:14px;background:var(--subject-soft);color:var(--subject-ink)}.tower-feedback.correct{background:#dcfce7;color:#166534}.tower-feedback.incorrect{background:#fee2e2;color:#991b1b}
.tower-answer-card.locked{pointer-events:none;transform:none;cursor:default}.tower-answer-card.correct{--answer-accent:#22c55e;--answer-bg:#dcfce7;--answer-glow:#22c55e55}.tower-answer-card.incorrect{--answer-accent:#ef4444;--answer-bg:#fee2e2;--answer-glow:#ef444455}.tower-answer-card.locked::after{animation:none}
.tower-submit-button:disabled{cursor:not-allowed}
.tower-answer-card.locked.correct{--answer-accent:#22c55e;--answer-bg:#dcfce7;--answer-glow:#22c55e22}
.tower-answer-card.locked.incorrect{--answer-accent:#ef4444;--answer-bg:#fee2e2;--answer-glow:#ef444422}
</style>
