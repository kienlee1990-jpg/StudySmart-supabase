<template>
  <QuizLayout :subject="result?.Subject">
    <section v-if="result" class="result-card" :class="{ 'tower-complete': result.Mode === 'Tower' }">
      <template v-if="result.Mode === 'Tower' && result.Tower">
        <header class="completion-header">
          <div class="completion-emblem" aria-hidden="true">
            <svg viewBox="0 0 48 48" fill="none"><path d="M15 9h18v12a9 9 0 0 1-18 0V9Z" fill="currentColor" opacity=".2"/><path d="M15 9h18v12a9 9 0 0 1-18 0V9Zm9 21v9m-7 1h14M15 13H9v5a7 7 0 0 0 7 7m17-12h6v5a7 7 0 0 1-7 7" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/><path d="m24 13 1.8 3.7 4.2.6-3 2.9.7 4.1-3.7-2-3.7 2 .7-4.1-3-2.9 4.2-.6L24 13Z" fill="currentColor"/></svg>
          </div>
          <span class="eyebrow">{{ result.Subject }} <span aria-hidden="true">·</span> KẾT QUẢ LEO THÁP</span>
          <h1>Hoàn thành tầng {{ result.Tower.Level }}</h1>
          <p>{{ result.Tower.AttemptStars >= result.Tower.MaxStars ? 'Tuyệt vời! Em đã chinh phục tầng này.' : result.Tower.AttemptStars > 0 ? 'Thêm một bước tiến trên hành trình chinh phục tháp!' : 'Mỗi lần thử là một bước tiến. Cùng cố gắng ở lượt sau nhé!' }}</p>
        </header>
        <div class="completion-summary">
          <div class="completion-score">
            <div class="score-circle" :style="{ '--score-progress': `${Math.max(0, Math.min(100, Number(result.Score) || 0))}%` }"><strong>{{ result.Score }}</strong><span>/ 100 điểm</span></div>
            <p class="score-caption">Trả lời đúng <strong>{{ result.CorrectCount }} / {{ result.TotalQuestions }}</strong> câu</p>
          </div>
          <div class="tower-achievement">
            <div class="achievement-heading"><span>KỶ LỤC TẦNG NÀY</span><span v-if="result.Tower.EarnedStar" class="record-badge">✦ Kỷ lục mới</span></div>
            <div class="achievement-stars" role="img" :aria-label="`Kỷ lục ${result.Tower.Stars} trên ${result.Tower.MaxStars} sao`"><span v-for="star in result.Tower.MaxStars" :key="star" :class="{ earned: star <= result.Tower.Stars }" aria-hidden="true">★</span></div>
            <div class="attempt-stars"><span>Lượt này</span><strong>{{ result.Tower.AttemptStars }} / {{ result.Tower.MaxStars }} sao</strong></div>
            <div class="achievement-track" aria-hidden="true"><span :style="{ width: `${result.Tower.Stars / result.Tower.MaxStars * 100}%` }"></span></div>
            <p class="unlock-note"><span aria-hidden="true">{{ result.Tower.IsCompleted ? '✦' : '↗' }}</span>{{ result.Tower.CurrentLevel != null && result.Tower.AttemptStars < result.Tower.MaxStars && result.Tower.IsCompleted ? 'Cần đạt 90% ở lượt này để vượt tầng và tăng rank.' : result.Tower.IsCompleted ? (result.Tower.Level < 1000 ? 'Đã mở khóa tầng tiếp theo!' : 'Em đã chinh phục toàn bộ 1000 tầng!') : `Còn ${result.Tower.MaxStars - result.Tower.Stars} sao để mở tầng tiếp theo` }}</p>
          </div>
        </div>
      </template>
      <template v-else>
        <div class="confetti">🎉</div>
        <span class="eyebrow">{{ `${result.Subject} · LỚP ${result.Grade}` }}</span>
        <h1>{{ result.Title }}</h1>
        <div class="score-circle"><strong>{{ result.Score }}</strong><span>điểm</span></div>
        <p class="score-caption">Em trả lời đúng <strong>{{ result.CorrectCount }}/{{ result.TotalQuestions }}</strong> câu. Cố gắng phát huy nhé!</p>
      </template>
      <p v-if="result.Mode === 'Tower' && result.Tower?.CurrentLevel" class="rank-outcome" :class="{ demoted: result.Tower.Demoted }" role="status">{{ result.Tower.Demoted ? `Em chưa qua tầng 3 lần liên tiếp. Rank đã lùi về tầng ${result.Tower.CurrentLevel}. Cùng chinh phục lại nhé!` : result.Tower.CountsForRank ? (result.Tower.Passed ? `Rank hiện tại: tầng ${result.Tower.CurrentLevel}. Đã xóa chuỗi trượt.` : `Chưa qua tầng: ${result.Tower.ConsecutiveFailures}/3 lần liên tiếp. Rank hiện tại: tầng ${result.Tower.CurrentLevel}.`) : `Lượt chơi lại này không ảnh hưởng rank. Rank hiện tại: tầng ${result.Tower.CurrentLevel}.` }}</p>
      <div class="result-actions"><template v-if="result.Mode === 'Tower'"><RouterLink v-if="unlockedNextLevel(result)" :to="nextTowerPath(result)" class="primary-button">🎉 Tiếp tục · Tầng {{ result.Tower.Level + 1 }}</RouterLink><RouterLink :to="{ path: '/ranking', query: { subject: result.Subject } }" :class="unlockedNextLevel(result) ? 'secondary-button' : 'primary-button'">Quay lại tháp</RouterLink></template><RouterLink v-else to="/" class="primary-button">Chọn bài khác</RouterLink><RouterLink v-if="authenticated && result.Mode !== 'Tower'" to="/" class="secondary-button">Xem thêm bài luyện tập</RouterLink></div>
      <div v-if="result.Mode !== 'Tower'" class="review-list"><h2>Cùng xem lại bài nhé</h2><article v-for="(answer, index) in result.Answers" :key="answer.Id || answer.id" class="review-question">
        <div class="review-title"><span>Câu {{ index + 1 }} · {{ answer.IsCorrect ?? answer.isCorrect ? 'Chính xác' : 'Ôn lại nhé' }}</span><span>{{ answer.IsCorrect ?? answer.isCorrect ? '✅' : '💡' }}</span></div>
        <strong>{{ answer.Content ?? answer.content }}</strong>
        <p v-if="answer.CorrectOption ?? answer.correctOption" class="correct-answer">Đáp án đúng: {{ answer.CorrectOption ?? answer.correctOption }}</p>
        <p v-if="answer.Explanation ?? answer.explanation" class="explanation">{{ answer.Explanation ?? answer.explanation }}</p>
      </article></div>
    </section>
    <section v-else class="result-card empty-result"><div class="confetti">📚</div><h1>Chưa tìm thấy kết quả</h1><p>Không tìm thấy kết quả hoặc bạn không có quyền xem lượt làm bài này.</p><RouterLink to="/" class="primary-button">Về trang chọn bài</RouterLink></section>
    <dialog v-if="result?.Mode === 'Tower' && result.Tower?.Demoted" ref="demotionDialog" class="demotion-dialog" role="alertdialog" aria-labelledby="demotion-title" aria-describedby="demotion-description">
      <div class="demotion-body">
        <span class="demotion-symbol" aria-hidden="true">↓</span>
        <span class="demotion-eyebrow">THAY ĐỔI THỨ HẠNG</span>
        <h2 id="demotion-title">Bạn bị giáng 1 hạng xuống tầng {{ result.Tower.CurrentLevel }}!</h2>
        <div class="demotion-level"><span>HẠNG HIỆN TẠI</span><strong>Tầng {{ result.Tower.CurrentLevel }}</strong></div>
        <p id="demotion-description">Bạn chưa vượt qua tầng cao nhất 3 lần liên tiếp. Chuỗi trượt đã được đặt lại; cùng chinh phục lại nhé!</p>
        <button type="button" autofocus @click="demotionDialog.close()">Đã hiểu</button>
      </div>
    </dialog>
  </QuizLayout>
</template>

<script setup>
import { nextTick, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'
import { guestRequestConfig } from '../../utils/guestIdentity'

const props = defineProps({ id: { type: String, required: true } })
const result = ref(null)
const demotionDialog = ref(null)
const authenticated = Boolean(localStorage.getItem('token'))
const subjectSlugs = { 'Toán': 'toan', 'Tiếng Anh': 'tieng-anh', 'Tiếng Việt': 'tieng-viet' }
const unlockedNextLevel = value => value?.Mode === 'Tower' && (value.Tower?.CurrentLevel != null ? value.Tower?.CountsForRank && value.Tower?.Passed && value.Tower.CurrentLevel > value.Tower.Level : value.Tower?.EarnedStar && value.Tower?.IsCompleted) && value.Tower.Level < 1000
const nextTowerPath = value => `/tower-session/${subjectSlugs[value.Subject]}/${value.Tower.Level + 1}`
const normalizeResult = data => data && ({
  ...data,
  Title: data.title ?? data.Title,
  Subject: data.subject ?? data.Subject,
  Mode: data.mode ?? data.Mode,
  Tower: (data.tower ?? data.Tower) && {
    CurrentLevel: (data.tower ?? data.Tower).currentLevel ?? (data.tower ?? data.Tower).CurrentLevel,
    ConsecutiveFailures: (data.tower ?? data.Tower).consecutiveFailures ?? (data.tower ?? data.Tower).ConsecutiveFailures ?? 0,
    Demoted: (data.tower ?? data.Tower).demoted ?? (data.tower ?? data.Tower).Demoted ?? false,
    CountsForRank: (data.tower ?? data.Tower).countsForRank ?? (data.tower ?? data.Tower).CountsForRank ?? false,
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
  if (result.value?.Mode === 'Tower' && result.value.Tower?.Demoted) {
    await nextTick()
    demotionDialog.value?.showModal()
  }
})
</script>

<style scoped>
.demotion-dialog{width:min(450px,calc(100vw - 32px));max-height:calc(100dvh - 32px);margin:auto;padding:0;overflow:auto;border:1px solid #efd8a9;border-radius:26px;background:#fff;box-shadow:0 28px 85px #30204033;color:var(--subject-ink);font-family:"Be Vietnam Pro","Segoe UI",sans-serif}
.demotion-dialog::backdrop{background:#261d3e66;backdrop-filter:blur(6px)}
.demotion-body{padding:30px 28px 26px;text-align:center;background:radial-gradient(ellipse at 50% 0,#fff0c9,transparent 65%),#fff}
.demotion-symbol{display:grid;place-items:center;width:64px;height:64px;margin:0 auto 17px;border:1px solid #efcc83;border-radius:21px;background:linear-gradient(145deg,#fff9e9,#ffe4a2);color:#aa6d16;font-size:38px;font-weight:700;box-shadow:0 8px 20px #ac76151a}
.demotion-eyebrow{color:#97702e;font-size:10px;font-weight:750;letter-spacing:.12em}
.demotion-dialog h2{margin:10px 0 18px;font-size:23px;font-weight:750;line-height:1.5;letter-spacing:-.025em;color:var(--subject-ink)}
.demotion-level{display:grid;gap:4px;padding:15px;border:1px solid #f0daaf;border-radius:15px;background:#fffaf0}.demotion-level span{color:#927449;font-size:10px;font-weight:700;letter-spacing:.08em}.demotion-level strong{color:#a96b14;font-size:30px;line-height:1.4}
.demotion-dialog p{margin:17px 0 23px;color:var(--subject-muted);font-size:13px;line-height:1.8}
.demotion-dialog button{width:100%;min-height:46px;padding:12px;border:0;border-radius:12px;background:var(--subject-strong);color:#fff;font:inherit;font-size:14px;font-weight:700;cursor:pointer}.demotion-dialog button:hover{background:var(--subject-primary)}.demotion-dialog button:focus-visible{outline:3px solid var(--subject-primary);outline-offset:3px}

.rank-outcome{padding:13px 16px;border:1px solid var(--subject-line);border-radius:12px;background:var(--subject-soft);color:var(--subject-ink);font-size:13px;line-height:1.7;text-align:left}.rank-outcome.demoted{border-color:#efd6a2;background:#fff8e9;color:#805519}

.result-card{max-width:820px;margin:10px auto;background:#fff;border:1px solid var(--subject-line);border-radius:28px;padding:34px;box-shadow:0 15px 40px color-mix(in srgb,var(--subject-primary) 4.3%,transparent);text-align:center}.confetti{font-size:52px}.eyebrow{font-size:12px;letter-spacing:.13em;color:var(--subject-muted);font-weight:750}.result-card h1{font-size:28px;color:var(--subject-ink);margin:8px 0 18px}.score-circle{width:136px;height:136px;margin:16px auto;border:9px solid var(--subject-line);border-radius:50%;display:flex;flex-direction:column;justify-content:center;background:var(--subject-soft);color:var(--subject-primary)}.score-circle strong{font-size:36px;line-height:1}.score-circle span{font-weight:700;font-size:14px;margin-top:5px}.score-caption{color:var(--subject-muted)}.result-actions{display:flex;justify-content:center;gap:12px;margin:20px}.primary-button,.secondary-button{padding:12px 18px;border-radius:13px;text-decoration:none;font-weight:700}.primary-button{color:#fff;background:linear-gradient(135deg,var(--subject-strong),var(--subject-strong))}.secondary-button{color:var(--subject-muted);background:var(--subject-soft)}.review-list{text-align:left;margin-top:32px}.review-list h2{font-size:20px;color:var(--subject-ink);margin-bottom:15px}.review-question{padding:17px;border:1px solid var(--subject-line);border-radius:15px;margin:10px 0}.review-title{display:flex;justify-content:space-between;font-size:12px;color:var(--subject-muted);font-weight:750;margin-bottom:9px}.review-question>strong{color:var(--subject-ink);line-height:1.5}.correct-answer{margin:11px 0 0;color:#2c9a70;font-weight:700}.explanation{margin:5px 0 0;color:var(--subject-muted);font-size:14px}.empty-result{padding:60px 25px}.empty-result p{color:var(--subject-muted)}
@media(max-width:600px){.result-card{padding:23px 15px}.result-card h1{font-size:23px}.result-actions{flex-direction:column}}

.tower-complete {
  --result-purple: var(--subject-primary);
  --result-ink: var(--subject-ink);
  position: relative;
  isolation: isolate;
  width: 100%;
  max-width: 740px;
  margin: 28px auto;
  padding: 36px 38px 28px;
  overflow: hidden;
  border: 1px solid var(--subject-line);
  border-radius: 28px;
  background: radial-gradient(ellipse at 50% 0, var(--subject-tint) 0, transparent 58%), #fff;
  box-shadow: 0 20px 60px color-mix(in srgb,var(--subject-primary) 6.3%,transparent), 0 3px 12px color-mix(in srgb,var(--subject-primary) 2.4%,transparent);
}
.tower-complete::before {
  content: '';
  position: absolute;
  inset: 0 15% auto;
  height: 3px;
  background: linear-gradient(90deg, transparent, var(--subject-line), var(--subject-line), transparent);
}
.completion-header { display: flex; flex-direction: column; align-items: center; }
.completion-emblem {
  display: grid;
  place-items: center;
  width: 72px;
  height: 72px;
  margin-bottom: 18px;
  border: 1px solid #e9d3a3;
  border-radius: 23px;
  color: #ac7523;
  background: linear-gradient(145deg, #fff9e9, #f8e4b6);
  box-shadow: 0 7px 20px #c89a3c18, inset 0 1px 0 #fff;
  transform: rotate(-5deg);
}
.completion-emblem svg { width: 46px; height: 46px; transform: rotate(5deg); }
.tower-complete .eyebrow { color: var(--subject-primary); font-size: 11px; letter-spacing: .13em; line-height: 1.8; }
.tower-complete .eyebrow span { padding: 0 6px; color: var(--subject-muted); }
.tower-complete h1 { margin: 8px 0 9px; color: var(--result-ink); font-size: clamp(25px, 3vw, 32px); font-weight: 700; letter-spacing: -.04em; }
.completion-header > p { max-width: 460px; margin: 0; color: var(--subject-primary); font-size: 14px; line-height: 1.7; }
.completion-summary { display: grid; grid-template-columns: .9fr 1.1fr; align-items: center; gap: 28px; margin: 30px 0 26px; }
.completion-score { min-width: 0; }
.tower-complete .score-circle {
  position: relative;
  isolation: isolate;
  width: 154px;
  height: 154px;
  margin: 0 auto 14px;
  border: 0;
  background: conic-gradient(from -90deg, var(--subject-primary) var(--score-progress), var(--subject-tint) 0);
  color: var(--result-purple);
}
.tower-complete .score-circle::before { content: ''; position: absolute; z-index: -1; inset: 9px; border: 4px solid #fff; border-radius: inherit; background: linear-gradient(145deg, #fff, var(--subject-soft)); }
.tower-complete .score-circle strong { font-size: 44px; font-weight: 750; letter-spacing: -.04em; font-variant-numeric: tabular-nums; }
.tower-complete .score-circle span { margin-top: 6px; color: var(--subject-muted); font-size: 12px; font-weight: 700; }
.tower-complete .score-caption { margin: 0; color: var(--subject-muted); font-size: 13px; line-height: 1.7; }
.tower-complete .score-caption strong { color: var(--subject-primary); white-space: nowrap; }
.tower-achievement { min-width: 0; padding: 21px 22px 18px; border: 1px solid #ecdfc5; border-radius: 20px; background: linear-gradient(145deg, #fffcf5, #fff8eb); text-align: left; }
.achievement-heading { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 8px; color: #967b4d; font-size: 10px; font-weight: 750; letter-spacing: .09em; }
.record-badge { padding: 4px 7px; border-radius: 6px; background: #f2e1b8; color: #85601f; font-size: 10px; letter-spacing: 0; }
.achievement-stars { display: flex; justify-content: space-between; gap: 4px; margin: 12px 0 13px; color: #e2dccf; font-size: clamp(25px, 3vw, 33px); line-height: 1.2; }
.achievement-stars .earned { color: #eeb43f; text-shadow: 0 3px 8px #cf941d26; }
.attempt-stars { display: flex; justify-content: space-between; gap: 10px; color: #988361; font-size: 13px; }
.attempt-stars strong { color: #806035; font-weight: 700; }
.achievement-track { height: 5px; margin: 13px 0 11px; overflow: hidden; border-radius: 10px; background: #ede4d3; }
.achievement-track span { display: block; height: 100%; border-radius: inherit; background: linear-gradient(90deg, #d4a04a, #f0c66d); }
.unlock-note { display: flex; align-items: baseline; gap: 6px; margin: 0; color: #9a8157; font-size: 11px; line-height: 1.6; }
.unlock-note > span { color: #bf8d38; font-weight: 750; }
.tower-complete .result-actions { flex-wrap: wrap; gap: 10px; margin: 0; padding-top: 23px; border-top: 1px solid var(--subject-line); }
.tower-complete .primary-button, .tower-complete .secondary-button { display: inline-flex; align-items: center; justify-content: center; min-height: 46px; min-width: 185px; padding: 12px 22px; border: 1px solid transparent; border-radius: 12px; font-size: 14px; line-height: 1.5; transition: transform .18s ease, box-shadow .18s ease, background .18s ease; }
.tower-complete .primary-button { background: linear-gradient(120deg, var(--subject-primary), var(--subject-primary)); box-shadow: 0 6px 16px color-mix(in srgb,var(--subject-primary) 13.3%,transparent), inset 0 1px 0 #ffffff20; }
.tower-complete .secondary-button { border-color: var(--subject-line); background: var(--subject-soft); color: var(--subject-primary); }
.tower-complete .primary-button:hover { transform: translateY(-2px); box-shadow: 0 9px 22px color-mix(in srgb,var(--subject-primary) 20%,transparent); }
.tower-complete .secondary-button:hover { background: var(--subject-tint); transform: translateY(-2px); }
.tower-complete .result-actions a:focus-visible { outline: 3px solid var(--subject-line); outline-offset: 4px; }
@media (max-width: 600px) {
  .tower-complete { max-width: 460px; margin: 16px auto; padding: 25px 20px 22px; border-radius: 22px; }
  .completion-emblem { width: 60px; height: 60px; margin-bottom: 14px; border-radius: 19px; }
  .completion-emblem svg { width: 39px; height: 39px; }
  .completion-header > p { max-width: 300px; font-size: 13px; }
  .completion-summary { grid-template-columns: 1fr; gap: 22px; margin: 24px 0; }
  .tower-complete .score-circle { width: 138px; height: 138px; }
  .tower-complete .score-circle strong { font-size: 39px; }
  .tower-achievement { padding: 18px 20px; }
  .achievement-stars { justify-content: center; gap: 13px; font-size: 32px; }
  .tower-complete .result-actions { padding-top: 20px; }
  .tower-complete .result-actions a { width: 100%; min-width: 0; }
}
@media (prefers-reduced-motion: reduce) {
  .tower-complete .result-actions a { transition: none; }
  .tower-complete .result-actions a:hover { transform: none; }
}
</style>
