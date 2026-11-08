<template>
  <QuizLayout>
    <section class="welcome-banner">
      <div class="welcome-copy">
        <span class="eyebrow">GÓC HỌC TẬP LỚP 5–12</span>
        <h1>Mỗi ngày một chút,<br /><span>tiến bộ thật nhiều!</span></h1>
        <p>Chọn môn học, chọn thử thách và bắt đầu hành trình chinh phục kiến thức nhé.</p>
        <div class="welcome-actions"><a href="#chon-bai" class="primary-button">Chọn bài ngay <span>→</span></a><RouterLink v-if="canManageQuiz" to="/quiz-admin" class="admin-link">Soạn câu hỏi</RouterLink></div>
      </div>
      <div class="welcome-art" aria-hidden="true"><div class="sun"></div><div class="book">📚</div><div class="spark spark-one">✦</div><div class="spark spark-two">✧</div><div class="art-note">Học vui<br />nhớ lâu!</div></div>
    </section>

    <section id="chon-bai" class="pick-section">
      <div class="section-heading"><div><span class="eyebrow">SẴN SÀNG CHƯA?</span><h2>Chọn bài học của em</h2></div><span class="tip-pill">🌈 Học theo cách của em</span></div>
      <div class="filters">
        <label>Khối lớp<select v-model="grade"><option value="">Tất cả</option><option v-for="item in grades" :key="item" :value="String(item)">Lớp {{ item }}</option></select></label>
        <label>Môn học<select v-model="subject"><option value="">Tất cả môn</option><option v-for="item in subjects" :key="item" :value="item">{{ item }}</option></select></label>
        <label>Hình thức<select v-model="mode"><option value="">Tất cả</option><option value="Practice">Luyện tập</option><option value="Exam">Thi thử</option></select></label>
      </div>

      <div v-if="loading" class="state-card">Đang tìm những bài học hay cho em…</div>
      <div v-else-if="error" class="state-card error">{{ error }} <button @click="loadQuizzes">Thử lại</button></div>
      <div v-else-if="quizzes.length" class="quiz-grid">
        <article v-for="quiz in quizzes" :key="quiz.id" class="quiz-card" :class="subjectClass(quiz.subject)">
          <div class="card-top"><span class="subject-icon">{{ subjectIcon(quiz.subject) }}</span><span class="grade-chip">Lớp {{ quiz.grade }}</span></div>
          <span class="subject-name">{{ quiz.subject }}</span>
          <h3>{{ quiz.title }}</h3>
          <p>{{ quiz.description || 'Sẵn sàng khám phá và thử sức với những câu hỏi thú vị!' }}</p>
          <div class="card-meta"><span>📝 {{ quiz.questionCount }} câu</span><span v-if="quiz.mode === 'Exam'">⏱ {{ quiz.durationMinutes }} phút</span><span v-else>✨ Không giới hạn giờ</span></div>
          <RouterLink :to="`/quiz/${quiz.id}`" class="start-button">{{ quiz.mode === 'Exam' ? 'Vào phòng thi' : 'Bắt đầu luyện' }} <span>→</span></RouterLink>
        </article>
      </div>
      <div v-else class="state-card">Chưa có bài phù hợp. Thử thay đổi bộ lọc nhé!</div>
      <div class="guest-note">💡 Em có thể làm bài ngay không cần tài khoản. <RouterLink to="/register">Tạo tài khoản</RouterLink> để lưu lịch sử điểm và xem lại kết quả.</div>
    </section>
    <section v-if="authenticated && attempts.length" class="history-section">
      <div class="section-heading"><div><span class="eyebrow">HÀNH TRÌNH CỦA EM</span><h2>Bài làm gần đây</h2></div></div>
      <article v-for="attempt in attempts" :key="attempt.id" class="history-card"><div><strong>{{ attempt.title }}</strong><span>{{ attempt.subject }} · Lớp {{ attempt.grade }} · {{ new Date(attempt.submittedAt).toLocaleDateString('vi-VN') }}</span></div><b>{{ attempt.score }} điểm</b><RouterLink :to="`/quiz-result/${attempt.id}`">Xem lại</RouterLink></article>
    </section>
  </QuizLayout>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'
import { canAccessPermission, getStoredUserPermissions, getStoredUserProfile } from '../../utils/accessControl'

const subjects = ['Toán', 'Tiếng Anh', 'Tiếng Việt']
const quizzes = ref([])
const attempts = ref([])
const loading = ref(false)
const error = ref('')
const grades = Array.from({ length: 8 }, (_, index) => index + 5)
const grade = ref('')
const subject = ref('')
const mode = ref('')
const canManageQuiz = computed(() => canAccessPermission(getStoredUserPermissions(), 'ManageQuizContent', getStoredUserProfile()))
const authenticated = Boolean(localStorage.getItem('token'))

const loadQuizzes = async () => {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (grade.value) params.grade = grade.value
    if (subject.value) params.subject = subject.value
    if (mode.value) params.mode = mode.value
    const response = await httpClient.get('/quizzes', { params })
    quizzes.value = Array.isArray(response.data) ? response.data : []
  } catch {
    error.value = 'Chưa tải được danh sách bài. Em kiểm tra API rồi thử lại nhé.'
  } finally {
    loading.value = false
  }
}

const subjectIcon = value => value === 'Toán' ? '🔢' : value === 'Tiếng Anh' ? '🌍' : '📖'
const subjectClass = value => value === 'Toán' ? 'math' : value === 'Tiếng Anh' ? 'english' : 'vietnamese'
const loadHistory = async () => {
  if (!authenticated) return
  try { const response = await httpClient.get('/quizzes/my-attempts'); attempts.value = response.data || [] } catch { attempts.value = [] }
}
watch([grade, subject, mode], loadQuizzes)
onMounted(() => { loadQuizzes(); loadHistory() })
</script>

<style scoped>
.welcome-banner{min-height:340px;background:linear-gradient(115deg,#eff3ff 0%,#f2f1ff 60%,#fff4e8 100%);border-radius:30px;padding:46px 6%;display:flex;align-items:center;justify-content:space-between;overflow:hidden;position:relative}.welcome-copy{max-width:560px;position:relative;z-index:1}.eyebrow{font-size:11px;letter-spacing:.14em;font-weight:900;color:#6e73d9}.welcome-banner h1{font-size:clamp(36px,5vw,58px);line-height:1.13;letter-spacing:-.04em;margin:13px 0;color:#253667}.welcome-banner h1 span{color:#ef9c48}.welcome-banner p{font-size:16px;line-height:1.7;color:#69728f;max-width:480px}.welcome-actions{display:flex;align-items:center;gap:24px;margin-top:23px}.primary-button,.start-button{display:inline-flex;align-items:center;justify-content:space-between;gap:24px;padding:13px 19px;border-radius:14px;color:white;text-decoration:none;font-weight:800;background:linear-gradient(135deg,#6674ed,#8c65dc);box-shadow:0 10px 20px #7074db33}.primary-button span,.start-button span{font-size:20px}.admin-link{color:#606dc4;font-weight:700}.welcome-art{width:300px;height:250px;position:relative;display:grid;place-items:center;margin-right:3%}.sun{position:absolute;width:210px;height:210px;border-radius:50%;background:radial-gradient(circle,#ffe2a4,#ffd4ac);opacity:.8}.book{position:relative;font-size:128px;filter:drop-shadow(0 16px 12px #916c4c2b);transform:rotate(-8deg)}.spark{position:absolute;color:#e99d3c;font-size:36px}.spark-one{top:12px;right:26px}.spark-two{left:22px;bottom:29px}.art-note{position:absolute;right:-12px;bottom:10px;transform:rotate(8deg);padding:12px 17px;background:#fff;border-radius:14px;box-shadow:0 7px 20px #4146641a;color:#6973d7;font-weight:900;text-align:center}.pick-section{padding-top:45px}.section-heading{display:flex;align-items:end;justify-content:space-between;margin-bottom:20px}.section-heading h2{font-size:27px;color:#273664;margin:7px 0 0}.tip-pill{background:#fff;padding:10px 14px;border-radius:20px;color:#7e6b9e;font-size:13px;font-weight:700}.filters{display:grid;grid-template-columns:repeat(3,1fr);gap:14px;margin-bottom:22px}.filters label{font-size:12px;font-weight:800;color:#747e9d}.filters select{display:block;width:100%;margin-top:7px;border:1px solid #e2e7f1;border-radius:12px;padding:12px;background:white;color:#344169}.quiz-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:17px}.quiz-card{background:#fff;border:1px solid #e9ecf5;border-radius:22px;padding:20px;box-shadow:0 8px 24px #2534660a;display:flex;flex-direction:column;min-height:300px}.card-top{display:flex;justify-content:space-between;align-items:center}.subject-icon{display:grid;place-items:center;width:48px;height:48px;border-radius:16px;background:#f1efff;font-size:25px}.grade-chip{padding:7px 10px;border-radius:14px;background:#f3f4fb;color:#637099;font-size:12px;font-weight:800}.subject-name{font-size:12px;font-weight:800;margin-top:17px;color:#7472d5}.quiz-card h3{font-size:18px;margin:7px 0;color:#27335b}.quiz-card p{font-size:13px;line-height:1.55;color:#7a8299;margin:0 0 14px}.card-meta{display:flex;justify-content:space-between;gap:5px;margin-top:auto;padding-top:12px;font-size:11px;color:#69748f}.start-button{margin-top:15px;padding:11px 14px;border-radius:12px}.math .subject-icon{background:#e9f7f0}.math .subject-name{color:#40a77b}.math .start-button{background:linear-gradient(135deg,#40b986,#5c9e6b)}.english .subject-icon{background:#eaf1ff}.english .subject-name{color:#5580d6}.english .start-button{background:linear-gradient(135deg,#568de8,#7265dc)}.vietnamese .subject-icon{background:#fff0e7}.vietnamese .subject-name{color:#dc8757}.vietnamese .start-button{background:linear-gradient(135deg,#ef9b51,#e77670)}.state-card{padding:25px;text-align:center;background:#fff;border:1px solid #e8ecf5;border-radius:18px;color:#77819b}.state-card.error{color:#b84e54}.state-card button{margin-left:8px;border:0;background:none;color:#546fe2;font-weight:bold}.guest-note{text-align:center;margin:22px 0;color:#828ba4;font-size:13px}.guest-note a{color:#5b6fe0;font-weight:800}.guest-note{font-weight:500}
@media(max-width:760px){.welcome-banner{padding:31px 7%;min-height:300px}.welcome-art{position:absolute;right:-55px;bottom:-25px;opacity:.27}.welcome-copy{max-width:100%}.filters{grid-template-columns:1fr}.quiz-grid{grid-template-columns:1fr}.section-heading{align-items:start;gap:10px;flex-direction:column}.tip-pill{font-size:11px}}
.history-section{margin:28px 0}.history-card{display:flex;align-items:center;gap:16px;padding:15px 17px;margin:9px 0;border:1px solid #e7ebf4;border-radius:15px;background:#fff}.history-card div{flex:1}.history-card strong,.history-card span{display:block}.history-card strong{color:#364268}.history-card span{margin-top:4px;color:#858da2;font-size:12px}.history-card b{white-space:nowrap;color:#3ba77e}.history-card>a{white-space:nowrap;color:#6472d7;font-weight:800;text-decoration:none;font-size:13px}
@media(max-width:600px){.history-card{gap:8px;flex-wrap:wrap}.history-card div{flex-basis:100%}}
</style>
