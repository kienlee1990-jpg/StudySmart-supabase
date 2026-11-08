<template>
  <QuizLayout>
    <section v-if="result" class="result-card">
      <div class="confetti">🎉</div>
      <span class="eyebrow">{{ result.Mode === 'Tower' ? `${result.Subject} · LEO THÁP · LEVEL ${result.Tower?.Level}` : `${result.Subject} · LỚP ${result.Grade}` }}</span>
      <h1>{{ result.Title }}</h1>
      <div class="score-circle"><strong>{{ result.Score }}</strong><span>điểm</span></div>
      <p class="score-caption">Em trả lời đúng <strong>{{ result.CorrectCount }}/{{ result.TotalQuestions }}</strong> câu. Cố gắng phát huy nhé!</p>
      <div v-if="result.Tower" class="tower-result"><strong>{{ result.Tower.AttemptStars > 0 ? `Lượt này đạt ${result.Tower.AttemptStars} sao${result.Tower.EarnedStar ? ' · Kỷ lục mới!' : ''}` : 'Lượt này dưới 50%, chưa nhận được sao.' }}</strong><div><span v-for="star in result.Tower.MaxStars" :key="star" :class="{ earned: star <= result.Tower.Stars }">★</span></div><small>Điểm lượt này: {{ result.Score }}% · Kỷ lục: {{ result.Tower.Stars }}/{{ result.Tower.MaxStars }} sao · {{ result.Tower.IsCompleted ? 'Đã mở khóa tầng tiếp theo!' : `Còn ${result.Tower.MaxStars - result.Tower.Stars} sao để mở tầng tiếp theo.` }}</small></div>
      <div class="result-actions"><template v-if="result.Mode === 'Tower'"><RouterLink v-if="unlockedNextLevel(result)" :to="nextTowerPath(result)" class="primary-button">🎉 Tiếp tục · Tầng {{ result.Tower.Level + 1 }}</RouterLink><RouterLink :to="{ path: '/ranking', query: { subject: result.Subject } }" :class="unlockedNextLevel(result) ? 'secondary-button' : 'primary-button'">Quay lại tháp</RouterLink></template><RouterLink v-else to="/" class="primary-button">Chọn bài khác</RouterLink><RouterLink v-if="authenticated && result.Mode !== 'Tower'" to="/" class="secondary-button">Xem thêm bài luyện tập</RouterLink></div>
      <div class="review-list"><h2>Cùng xem lại bài nhé</h2><article v-for="(answer, index) in result.Answers" :key="answer.Id || answer.id" class="review-question">
        <div class="review-title"><span>Câu {{ index + 1 }} · {{ answer.IsCorrect ?? answer.isCorrect ? 'Chính xác' : 'Ôn lại nhé' }}</span><span>{{ answer.IsCorrect ?? answer.isCorrect ? '✅' : '💡' }}</span></div>
        <strong>{{ answer.Content ?? answer.content }}</strong>
        <p v-if="answer.CorrectOption ?? answer.correctOption" class="correct-answer">Đáp án đúng: {{ answer.CorrectOption ?? answer.correctOption }}</p>
        <p v-if="answer.Explanation ?? answer.explanation" class="explanation">{{ answer.Explanation ?? answer.explanation }}</p>
      </article></div>
    </section>
    <section v-else class="result-card empty-result"><div class="confetti">📚</div><h1>Chưa tìm thấy kết quả</h1><p>Không tìm thấy kết quả hoặc bạn không có quyền xem lượt làm bài này.</p><RouterLink to="/" class="primary-button">Về trang chọn bài</RouterLink></section>
  </QuizLayout>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'
import { guestRequestConfig } from '../../utils/guestIdentity'

const props = defineProps({ id: { type: String, required: true } })
const result = ref(null)
const authenticated = Boolean(localStorage.getItem('token'))
const subjectSlugs = { 'Toán': 'toan', 'Tiếng Anh': 'tieng-anh', 'Tiếng Việt': 'tieng-viet' }
const unlockedNextLevel = value => value?.Mode === 'Tower' && value.Tower?.EarnedStar && value.Tower?.IsCompleted && value.Tower.Level < 1000
const nextTowerPath = value => `/tower-session/${subjectSlugs[value.Subject]}/${value.Tower.Level + 1}`
const normalizeResult = data => data && ({
  ...data,
  Title: data.title ?? data.Title,
  Subject: data.subject ?? data.Subject,
  Mode: data.mode ?? data.Mode,
  Tower: (data.tower ?? data.Tower) && {
    Level: (data.tower ?? data.Tower).level ?? (data.tower ?? data.Tower).Level,
    Stars: (data.tower ?? data.Tower).stars ?? (data.tower ?? data.Tower).Stars,
    EarnedStar: (data.tower ?? data.Tower).earnedStar ?? (data.tower ?? data.Tower).EarnedStar,
    AttemptStars: (data.tower ?? data.Tower).attemptStars ?? (data.tower ?? data.Tower).AttemptStars ?? ((data.tower ?? data.Tower).earnedStar ?? (data.tower ?? data.Tower).EarnedStar ? 1 : 0),
    Passed: (data.tower ?? data.Tower).passed ?? (data.tower ?? data.Tower).Passed,
    IsCompleted: (data.tower ?? data.Tower).isCompleted ?? (data.tower ?? data.Tower).IsCompleted,
    MaxStars: (data.tower ?? data.Tower).maxStars ?? (data.tower ?? data.Tower).MaxStars ?? 5
  },
  Grade: data.grade ?? data.Grade,
  Score: data.score ?? data.Score,
  CorrectCount: data.correctCount ?? data.CorrectCount,
  TotalQuestions: data.totalQuestions ?? data.TotalQuestions,
  Answers: (data.answers ?? data.Answers ?? []).map(answer => ({
    ...answer,
    Id: answer.id ?? answer.Id,
    Content: answer.content ?? answer.Content,
    IsCorrect: answer.isCorrect ?? answer.IsCorrect,
    CorrectOption: answer.correctOption ?? answer.CorrectOption,
    Explanation: answer.explanation ?? answer.Explanation
  }))
})
onMounted(async () => {
  try { result.value = normalizeResult(JSON.parse(sessionStorage.getItem(`quiz-result-${props.id}`) || 'null')) } catch { result.value = null }
  if (!result.value) {
    try { result.value = normalizeResult((await httpClient.get(`/quizzes/attempts/${props.id}/result`, guestRequestConfig())).data) } catch { result.value = null }
  }
})
</script>

<style scoped>
.result-card{max-width:820px;margin:10px auto;background:#fff;border:1px solid #e8ecf5;border-radius:28px;padding:34px;box-shadow:0 15px 40px #2534660b;text-align:center}.confetti{font-size:52px}.eyebrow{font-size:11px;letter-spacing:.13em;color:#7780d9;font-weight:900}.result-card h1{font-size:28px;color:#2d3b68;margin:8px 0 18px}.score-circle{width:136px;height:136px;margin:16px auto;border:9px solid #e4f6ee;border-radius:50%;display:flex;flex-direction:column;justify-content:center;background:#f8fffb;color:#36a77c}.score-circle strong{font-size:36px;line-height:1}.score-circle span{font-weight:800;font-size:13px;margin-top:5px}.score-caption{color:#7a839a}.result-actions{display:flex;justify-content:center;gap:12px;margin:20px}.primary-button,.secondary-button{padding:12px 18px;border-radius:13px;text-decoration:none;font-weight:800}.primary-button{color:#fff;background:linear-gradient(135deg,#6477ea,#8a64da)}.secondary-button{color:#6370cf;background:#f0f2ff}.tower-result{display:flex;flex-direction:column;align-items:center;gap:7px;max-width:380px;margin:18px auto;padding:15px 18px;border:1px solid #f0e4c9;border-radius:15px;background:#fffaf0;color:#775d2d}.tower-result>div{display:flex;gap:5px;color:#d9dce5;font-size:25px}.tower-result>div .earned{color:#f0ae31;text-shadow:0 2px 4px #f0ae3140}.tower-result small{color:#8b806c;font-size:11px}.review-list{text-align:left;margin-top:32px}.review-list h2{font-size:20px;color:#303d68;margin-bottom:15px}.review-question{padding:17px;border:1px solid #e8ecf5;border-radius:15px;margin:10px 0}.review-title{display:flex;justify-content:space-between;font-size:11px;color:#6675d0;font-weight:900;margin-bottom:9px}.review-question>strong{color:#334064;line-height:1.5}.correct-answer{margin:11px 0 0;color:#2c9a70;font-weight:800}.explanation{margin:5px 0 0;color:#788198;font-size:13px}.empty-result{padding:60px 25px}.empty-result p{color:#7e879d}
@media(max-width:600px){.result-card{padding:23px 15px}.result-card h1{font-size:23px}.result-actions{flex-direction:column}}
</style>
