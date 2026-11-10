<template>
  <QuizLayout :subject="subject">
    <section class="tower-hero" :class="subjectTheme">
      <RouterLink :to="subjectPath" class="back-link">← Quay lại môn học</RouterLink>
      <span class="eyebrow">HÀNH TRÌNH CHINH PHỤC</span>
      <h1>Leo tháp {{ subject }}</h1>
      <p>Chinh phục 1000 tầng, tích lũy sao và mở khóa thử thách khó dần.</p>
      <div class="tower-summary"><span>🗼 <strong>{{ completedLevels }}/1000</strong> tầng hoàn thành</span><span>⭐ <strong>{{ totalStars }}/{{ levels.length * 5 }}</strong> sao</span></div>
      <div class="journey-progress"><div><span>TIẾN ĐỘ HÀNH TRÌNH</span><strong>{{ completedLevels }}/1000 tầng</strong></div><div class="hero-progress"><span :style="{ width: `${completedLevels / 10}%` }"></span></div></div>
      <p class="rules-note">Khối lớp, số câu và tỷ lệ câu Nâng cao tăng theo bậc. Hai tầng liền kề không trùng câu hỏi. Đạt 90% được 5 sao; mỗi bậc 10% thấp hơn giảm 1 sao. Cần 5 sao để mở tầng kế. Mỗi câu có 15 giây, chấm ngay khi chọn hoặc hết giờ; không đổi đáp án hay quay lại câu đã qua.</p>
      <p v-if="currentLevel?.requiredAdvancedQuestions != null" class="rules-note">Tầng {{ currentLevel.level }} · Lớp {{ currentLevel.questionGrade }} · {{ currentLevel.requiredNormalQuestions }} câu Thường + {{ currentLevel.requiredAdvancedQuestions }} câu Nâng cao</p>
      <button class="start-button" :disabled="startDisabled" @click="error ? loadProgress() : currentLevel && enterLevel(currentLevel)">{{ startButtonLabel }}</button>
      <p v-if="currentLevel && !loading && !error && !canPlayLevel(currentLevel)" class="start-hint">{{ currentLevel.unavailableReason || `Tầng ${currentLevel.level} cần ${currentLevel.requiredQuestions} câu ${subject} lớp ${currentLevel.questionGrade}, hiện có ${currentLevel.questionCount}.` }}</p>
    </section>

    <section class="journey-card">
      <div v-if="loading" class="state-card">Đang tải tiến trình leo tháp…</div>
      <div v-else-if="error" class="state-card error">{{ error }} <button @click="loadProgress">Thử lại</button></div>
      <div v-else class="journey-content">
        <nav v-if="isAdmin" class="stage-gallery" aria-label="Chọn chặng">
          <button v-for="number in 10" :key="number" class="stage-card" :class="[`stage-color-${number}`, { selected: number === stage, reached: number <= furthestStage }]" @click="stage = number">
            <span class="stage-illustration">{{ stageIcons[number - 1] }}</span><span class="stage-card-copy"><strong>Chặng {{ String(number).padStart(2, '0') }}</strong><small>{{ stageNames[number - 1] }}</small><i><span :style="{ width: `${stageCompletion(number)}%` }"></span></i></span><span class="stage-card-count">{{ getStageCompleted(number) }}/100</span>
          </button>
        </nav>
        <nav v-else class="player-stage-nav" aria-label="Điều hướng chặng">
          <button class="stage-arrow" :disabled="stage <= 1" aria-label="Chặng trước" @click="stage--">‹</button>
          <span class="player-stage-index">CHẶNG {{ String(stage).padStart(2, '0') }}</span>
          <button class="stage-arrow" :disabled="stage >= furthestStage" aria-label="Chặng tiếp theo" @click="stage++">›</button>
        </nav>
        <header class="stage-heading"><div class="stage-art">{{ stageIcons[stage - 1] }}</div><div class="stage-copy"><span class="stage-kicker">CHẶNG {{ String(stage).padStart(2, '0') }} · TẦNG {{ stageStart }}–{{ stageEnd }}</span><h2>{{ stageNames[stage - 1] }}</h2><p>{{ stageDescriptions[stage - 1] }} · Câu hỏi lớp {{ stageGrade }}</p></div><div class="stage-progress"><strong>{{ stageCompleted }}/100</strong><span>tầng xong</span></div></header>
        <div class="stage-levels" aria-label="Danh sách tầng trong chặng">
          <button v-for="level in stageLevels" :key="level.level" class="trail-stop" :class="{ locked: !level.isUnlocked, completed: level.isCompleted, current: currentLevel?.level === level.level, playable: level.isUnlocked && canPlayLevel(level) }" :disabled="!level.isUnlocked || !canPlayLevel(level)" :title="`Tầng ${level.level} · ${level.isUnlocked ? `${level.stars}/5 sao` : 'Đang khóa'}`" @click="enterLevel(level)">
            <span class="stop-number">{{ level.level }}</span><span v-if="level.isCompleted" class="level-icon">✓</span><span v-else-if="!level.isUnlocked" class="level-icon">🔒</span><span v-else-if="currentLevel?.level === level.level" class="level-icon">▶</span><span v-else class="level-icon">🧭</span><span v-if="level.stars" class="stop-stars">★{{ level.stars }}</span><span v-if="currentLevel?.level === level.level" class="you-are-here">TIẾP TỤC</span>
          </button>
        </div>
        <footer class="map-legend"><span><i class="legend-current"></i> Tầng hiện tại</span><span><i class="legend-done">✓</i> Đã vượt qua</span><span><i class="legend-locked">🔒</i> Chưa mở khóa</span></footer>
      </div>
    </section>
  </QuizLayout>
</template>

<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'
import { guestRequestConfig } from '../../utils/guestIdentity'
import { getStoredUserProfile, isPrivilegedProfile } from '../../utils/accessControl'

const route = useRoute()
const router = useRouter()
const subjectSlugs = { 'Toán': 'toan', 'Tiếng Anh': 'tieng-anh', 'Tiếng Việt': 'tieng-viet' }
const subject = computed(() => Object.hasOwn(subjectSlugs, route.query.subject) ? route.query.subject : 'Toán')
const subjectPath = computed(() => `/subjects/${subjectSlugs[subject.value]}`)
const subjectTheme = computed(() => ({ 'Toán': 'subject-math', 'Tiếng Anh': 'subject-english', 'Tiếng Việt': 'subject-vietnamese' })[subject.value])
const isAdmin = computed(() => isPrivilegedProfile(getStoredUserProfile()))
const levels = ref([])
const loading = ref(false)
const error = ref('')
const stage = ref(1)
const stageSize = 100
const stageStart = computed(() => (stage.value - 1) * stageSize + 1)
const stageEnd = computed(() => Math.min(stage.value * stageSize, levels.value.length))
const stageLevels = computed(() => levels.value.slice(stageStart.value - 1, stageEnd.value))
const stageCompleted = computed(() => stageLevels.value.filter(item => item.isCompleted).length)
const stageGrade = computed(() => stageLevels.value[0]?.questionGrade || 5)
const furthestStage = computed(() => Math.max(1, Math.ceil((currentLevel.value?.level || 1) / stageSize)))
const stageNames = ['Chân núi khởi đầu', 'Rừng tri thức', 'Suối thử thách', 'Thung lũng khám phá', 'Vách đá kiên trì', 'Cầu mây học tập', 'Đỉnh gió lớn', 'Thành trì trí tuệ', 'Đường lên mây', 'Đỉnh cao chinh phục']
const stageDescriptions = ['Làm quen và tích lũy những ngôi sao đầu tiên', 'Băng qua khu rừng câu hỏi mới', 'Giữ nhịp học tập qua dòng suối kiến thức', 'Khám phá những thử thách ngày càng khó', 'Vững vàng chinh phục những vách đá cao', 'Bước tiếp trên cây cầu tri thức', 'Vượt gió lớn bằng sự tập trung', 'Mở cổng thành bằng thành tích của em', 'Chạm gần hơn tới bầu trời tri thức', 'Chặng cuối cùng để trở thành nhà chinh phục']
const stageIcons = ['🌱', '🌲', '💧', '🌄', '🧗', '☁️', '🌬️', '🏯', '🌤️', '🏔️']
const totalStars = computed(() => levels.value.reduce((sum, item) => sum + item.stars, 0))
const completedLevels = computed(() => levels.value.filter(item => item.isCompleted).length)
const currentLevel = computed(() => levels.value.find(item => item.isUnlocked && !item.isCompleted) || levels.value.at(-1))
const canPlayLevel = level => level.canStart ?? (level.questionCount >= level.requiredQuestions)
const startDisabled = computed(() => loading.value || (!error.value && (!currentLevel.value || !canPlayLevel(currentLevel.value))))
const startButtonLabel = computed(() => {
  if (loading.value) return 'Đang tải tháp…'
  if (error.value) return 'Thử tải lại tiến trình'
  if (!currentLevel.value) return 'Chưa có tầng khả dụng'
  if (!canPlayLevel(currentLevel.value)) return 'Cần thêm câu hỏi'
  return `▶ Bắt đầu · Tầng ${currentLevel.value.level}`
})
const getStageCompleted = number => levels.value.slice((number - 1) * stageSize, number * stageSize).filter(item => item.isCompleted).length
const stageCompletion = number => getStageCompleted(number)

watch(stage, async () => {
  await nextTick()
  const activeStop = document.querySelector('.trail-stop.current')
  activeStop?.scrollIntoView({ behavior: 'smooth', block: 'center' })
})

const loadProgress = async () => {
  loading.value = true
  error.value = ''
  try {
    const response = await httpClient.get('/quizzes/tower/progress', {
      params: { subject: subject.value },
      ...guestRequestConfig()
    })
    levels.value = (response.data.levels ?? response.data.Levels ?? []).map(item => ({
      level: item.level ?? item.Level,
      questionGrade: item.questionGrade ?? item.QuestionGrade,
      requiredQuestions: item.requiredQuestions ?? item.RequiredQuestions ?? 5,
      stars: item.stars ?? item.Stars ?? 0,
      isUnlocked: item.isUnlocked ?? item.IsUnlocked,
      isCompleted: item.isCompleted ?? item.IsCompleted,
      questionCount: item.questionCount ?? item.QuestionCount ?? 0,
      requiredNormalQuestions: item.requiredNormalQuestions ?? item.RequiredNormalQuestions,
      requiredAdvancedQuestions: item.requiredAdvancedQuestions ?? item.RequiredAdvancedQuestions,
      canStart: item.canStart ?? item.CanStart,
      unavailableReason: item.unavailableReason ?? item.UnavailableReason
    }))
    stage.value = Math.ceil((currentLevel.value?.level || 1) / stageSize)
  } catch (err) {
    error.value = err?.response?.data?.message || 'Chưa tải được tiến trình tháp. Em thử lại nhé.'
  } finally {
    loading.value = false
  }
}

const enterLevel = level => {
  if (!level.isUnlocked || !canPlayLevel(level)) return
  router.push(`/tower-session/${subjectSlugs[subject.value]}/${level.level}`)
}

watch(subject, loadProgress, { immediate: true })
</script>

<style scoped>
.tower-hero {
  --hero-ink: var(--subject-ink);
  --hero-muted: var(--subject-muted);
  --hero-accent: var(--subject-primary);
  --hero-tint: var(--subject-tint);
  --hero-border: var(--subject-line);
  position: relative;
  isolation: isolate;
  overflow: hidden;
  padding: 32px 36px;
  border: 1px solid var(--hero-border);
  border-radius: 26px;
  background: linear-gradient(110deg, #fffdfd 0%, var(--subject-soft) 40%, var(--hero-tint) 100%);
  color: var(--hero-ink);
  box-shadow: 0 12px 34px color-mix(in srgb,var(--subject-primary) 4.7%,transparent), inset 0 1px 0 #fff;
}
.tower-hero.subject-math { --hero-art: url('/images/tower-math.svg'); }
.tower-hero.subject-english { --hero-art: url('/images/tower-english.svg'); }
.tower-hero.subject-vietnamese { --hero-art: url('/images/tower-vietnamese.svg'); }
.tower-hero::before {
  content: '';
  position: absolute;
  z-index: -2;
  inset: 0 0 0 35%;
  background-image: var(--hero-art);
  background-size: cover;
  background-position: right center;
  opacity: .2;
  mix-blend-mode: multiply;
  mask-image: linear-gradient(90deg, transparent, #000 65%);
  pointer-events: none;
}
.tower-hero::after { position: absolute; z-index: -1; right: 7%; bottom: -170px; width: 340px; height: 260px; border: 1px solid #ffffffb3; border-radius: 50%; box-shadow: 0 0 0 25px #ffffff30, 0 0 0 52px #ffffff24; content: ''; pointer-events: none; }
.back-link { display: inline-flex; align-items: center; padding: 7px 11px; border: 1px solid var(--hero-border); border-radius: 10px; background: #ffffff9c; color: var(--hero-accent); text-decoration: none; font-size: 13px; font-weight: 750; transition: background .18s; }
.back-link:hover { background: #fff; color: var(--hero-ink); }
.back-link:focus-visible { outline: 3px solid var(--hero-accent); outline-offset: 3px; }
.eyebrow { display: block; margin-top: 20px; color: var(--hero-accent); font-size: 11px; font-weight: 750; letter-spacing: .2em; }
.tower-hero h1 { margin: 8px 0 10px; color: var(--hero-ink); font-size: clamp(28px,4vw,40px); font-weight: 700; letter-spacing: -.035em; }
.tower-hero > p { max-width: 560px; margin: 0; color: var(--hero-muted); font-size: 14px; line-height: 1.7; }
.tower-summary { display: flex; flex-wrap: wrap; gap: 9px; margin-top: 22px; }
.tower-summary span { padding: 9px 13px; border: 1px solid var(--hero-border); border-radius: 12px; background: #ffffffbf; color: var(--hero-muted); font-size: 13px; box-shadow: 0 3px 8px color-mix(in srgb,var(--subject-primary) 1.6%,transparent); display: inline-flex; align-items: center; gap: 7px; }
.tower-summary strong { color: var(--hero-ink); }
.journey-progress { max-width: 480px; margin-top: 20px; }
.journey-progress > div:first-child { display: flex; justify-content: space-between; gap: 12px; color: var(--hero-muted); font-size: 10px; font-weight: 750; letter-spacing: .1em; }
.journey-progress strong { color: var(--hero-accent); letter-spacing: 0; }
.hero-progress { height: 7px; margin-top: 9px; overflow: hidden; border: 1px solid #ffffffa8; border-radius: 99px; background: var(--hero-border); }
.hero-progress span { display: block; height: 100%; border-radius: inherit; background: linear-gradient(90deg, var(--hero-accent), color-mix(in srgb, var(--hero-accent) 45%, #fff)); transition: width .7s ease; }
.tower-hero .rules-note { max-width: 580px; margin-top: 15px; color: var(--hero-muted); font-size: 12px; line-height: 1.85; }
.start-button { margin-top: 18px; min-height: 46px; padding: 12px 20px; border: 1px solid var(--subject-strong); border-radius: 12px; background: linear-gradient(120deg,var(--subject-strong),var(--subject-strong)); box-shadow: 0 6px 15px color-mix(in srgb,var(--subject-primary) 12.5%,transparent), inset 0 1px 0 #fff8; color: #fff; font: inherit; font-size: 14px; font-weight: 750; cursor: pointer; transition: transform .2s, box-shadow .2s; }
.start-button:hover:not(:disabled) { transform: translateY(-2px); box-shadow: 0 9px 20px color-mix(in srgb,var(--subject-primary) 18.8%,transparent); }
.start-button:focus-visible,.stage-card:focus-visible,.trail-stop:focus-visible { outline: 3px solid var(--subject-primary); outline-offset: 3px; }
.start-button:disabled { opacity: .58; cursor: not-allowed; }
.tower-hero .start-hint { max-width: 520px; margin-top: 12px; padding: 9px 12px; border: 1px solid var(--subject-line); border-radius: 10px; background: var(--subject-soft); color: var(--subject-primary); font-size: 12px; line-height: 1.6; }
.journey-card{margin-top:20px;padding:22px;border:1px solid #e5e9e4;border-radius:24px;background:#fff;box-shadow:0 12px 35px color-mix(in srgb,var(--subject-primary) 6.3%,transparent)}.journey-content{display:flex;flex-direction:column;gap:17px}
.stage-gallery{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:12px}.stage-card{--stage-accent:var(--subject-primary);--stage-tint:var(--subject-tint);--stage-wash:var(--subject-soft);position:relative;isolation:isolate;display:grid;grid-template-columns:48px minmax(0,1fr);grid-template-rows:auto auto;align-items:center;column-gap:11px;min-height:88px;overflow:hidden;padding:12px;border:1px solid color-mix(in srgb,var(--stage-accent) 15%,#fff);border-radius:19px;background:linear-gradient(135deg,#fff 10%,var(--stage-wash));text-align:left;cursor:pointer;box-shadow:0 6px 17px color-mix(in srgb,var(--stage-accent) 7%,transparent);transition:transform .2s,border-color .2s,box-shadow .2s,filter .2s}.stage-card:hover{transform:translateY(-3px);border-color:color-mix(in srgb,var(--stage-accent) 48%,#fff);box-shadow:0 13px 26px color-mix(in srgb,var(--stage-accent) 16%,transparent)}.stage-card:hover:after{transform:scale(1.14) translate(-4px,-3px)}.stage-card.selected{border-color:var(--stage-accent);box-shadow:inset 0 0 0 1.5px var(--stage-accent),0 9px 22px color-mix(in srgb,var(--stage-accent) 18%,transparent)}.stage-card.reached:not(.selected){border-color:color-mix(in srgb,var(--stage-accent) 27%,#fff)}.player-stage-nav{display:flex;align-items:center;justify-content:center;gap:18px;padding:5px 0 2px}.stage-arrow{display:grid;width:38px;height:38px;place-items:center;border:1px solid var(--subject-line);border-radius:50%;background:#f7faf7;color:var(--subject-primary);font-size:27px;line-height:1;cursor:pointer;transition:background .15s,transform .15s}.stage-arrow:hover:not(:disabled){transform:scale(1.06);background:var(--subject-tint)}.stage-arrow:disabled{color:var(--subject-muted);cursor:not-allowed}.player-stage-index{color:var(--subject-primary);font-size:11px;font-weight:750;letter-spacing:.14em}
.stage-illustration{position:relative;z-index:1;grid-row:1/3;display:grid;width:46px;height:46px;place-items:center;border:1px solid #ffffffc9;border-radius:16px;background:linear-gradient(145deg,#fff,var(--stage-tint));font-size:27px;box-shadow:0 5px 12px color-mix(in srgb,var(--stage-accent) 13%,transparent)}.stage-card-copy{position:relative;z-index:1;display:flex;min-width:0;flex-direction:column;gap:5px}.stage-card-copy strong{overflow:hidden;padding-right:38px;color:var(--subject-ink);font-size:13px;font-weight:750;letter-spacing:-.015em;text-overflow:ellipsis;white-space:nowrap}.stage-card-copy small{overflow:hidden;color:var(--subject-primary);font-size:11px;font-weight:650;text-overflow:ellipsis;white-space:nowrap}.stage-card-copy i{height:5px;width:100%;overflow:hidden;border-radius:6px;background:color-mix(in srgb,var(--stage-accent) 12%,#fff)}.stage-card-copy i span{display:block;height:100%;border-radius:inherit;background:linear-gradient(90deg,var(--stage-accent),color-mix(in srgb,var(--stage-accent) 55%,#fff));transition:width .5s}.stage-card-count{position:absolute;z-index:2;right:9px;top:8px;padding:4px 7px;border:1px solid color-mix(in srgb,var(--stage-accent) 12%,#fff);border-radius:99px;background:#ffffffdf;color:var(--subject-primary);font-size:10px;font-weight:750;box-shadow:0 2px 6px color-mix(in srgb,var(--subject-primary) 3.9%,transparent)}.stage-card.reached .stage-card-count{border-color:color-mix(in srgb,var(--stage-accent) 16%,#fff);background:var(--stage-tint);color:var(--stage-accent)}
.stage-heading{display:flex;min-height:88px;align-items:center;gap:14px;padding:14px 17px;border:1px solid var(--subject-line);border-radius:17px;background:var(--subject-soft)}.stage-art{display:grid;width:54px;height:54px;flex:none;place-items:center;border-radius:16px;background:var(--subject-tint);box-shadow:none;font-size:28px;color: var(--subject-primary)}.stage-copy{min-width:0;flex:1}.stage-kicker{color:var(--subject-primary);font-size:10px;font-weight:750;letter-spacing:.12em}.stage-copy h2{margin:3px 0;color:var(--subject-ink);font-size:20px;letter-spacing:-.02em}.stage-copy p{margin:0;color:var(--subject-primary);font-size:12px;line-height: 1.7}.stage-progress{display:flex;flex-direction:column;align-items:flex-end;color:var(--subject-primary);font-size:11px}.stage-progress strong{color:var(--subject-primary);font-size:21px;letter-spacing:-.04em}
.stage-levels{position:relative;height:var(--trail-height);min-height:72px;overflow:hidden;border:1px solid var(--subject-line);border-radius:20px;background:var(--subject-soft);gap: 12px;padding: 24px}.landscape-art{position:absolute;z-index:0;inset:0;width:100%;height:100%;pointer-events:none}.scene-sun{fill:var(--subject-line);opacity:.72;filter:drop-shadow(0 0 14px color-mix(in srgb,var(--subject-primary) 53.3%,transparent))}.scene-cloud path{fill:#ffffffc9;stroke:var(--subject-line);stroke-width:5}.scene-cloud circle{fill:var(--subject-primary)}.mountain-far{fill:var(--subject-line);opacity:.56}.mountain-near{fill:var(--subject-line);opacity:.56}.mountain-snow{fill:var(--subject-soft);opacity:.88}.mountain-trees{fill:var(--subject-primary);opacity:.7}.river-bank{fill:none;stroke:var(--subject-line);stroke-width:76;opacity:.36;stroke-linecap:round}.river-water{fill:none;stroke:var(--subject-primary);stroke-width:44;opacity:.4;stroke-linecap:round}.lesson-path{position:absolute;inset:0;width:100%;height:100%;overflow:visible;pointer-events:none}.lesson-path path{fill:none;stroke-linecap:round;stroke-linejoin:round}.lesson-path-base{stroke:#ffffffd9;stroke-width:62;filter:drop-shadow(0 3px 5px color-mix(in srgb,var(--subject-primary) 9%,transparent))}.lesson-path-progress{stroke:var(--subject-primary);stroke-width:30;stroke-dasharray:0 100;transition:stroke-dasharray .8s ease}
.trail-stop{position:absolute;z-index:2;display:grid;width:44px;height:44px;transform:translate(-50%,-50%);place-items:center;border:3px solid #fff;border-radius:15px;background:#e1e6e1;box-shadow:none;color:var(--subject-primary);font-size:11px;font-weight:750;cursor:not-allowed;transition:transform .18s,box-shadow .18s;min-height: 72px}.trail-stop.playable{cursor:pointer}.trail-stop.playable:hover{transform:translate(-50%,-50%) scale(1.09);box-shadow:0 4px 0 var(--subject-primary),0 8px 15px color-mix(in srgb,var(--subject-primary) 18%,transparent)}.trail-stop.locked{background:#fff;color:var(--subject-muted);border-color: var(--subject-line)}.trail-stop.completed{background:var(--subject-strong);box-shadow:0 4px 0 var(--subject-primary),0 7px 13px color-mix(in srgb,var(--subject-primary) 16.9%,transparent);color:#fff}.trail-stop.current{width:58px;height:58px;border:2px solid var(--subject-primary);border-radius:19px;background:var(--subject-tint);box-shadow:0 0 0 5px color-mix(in srgb,var(--subject-primary) 16.9%,transparent),0 5px 0 var(--subject-primary),0 9px 15px color-mix(in srgb,var(--subject-primary) 29%,transparent);color:var(--subject-strong);font-size:13px;animation:pulse 2.2s ease-in-out infinite}.stop-number{order:2;margin-top:-2px;padding:2px 5px;border-radius:7px;background:#ffffffd9;color:var(--subject-primary);font-size:10px;line-height:1;font-weight:750}.level-icon{order:1;display:grid;width:30px;height:28px;place-items:center;border-radius:10px;background:#ffffffd9;box-shadow:0 2px 5px color-mix(in srgb,var(--subject-primary) 6.3%,transparent);font-size:21px;line-height:1}.trail-stop.current .level-icon{background:#fff}.trail-stop.completed .level-icon{background:var(--subject-tint)}.trail-stop.locked .level-icon{background:#f5f6f4}.trail-stop.current .stop-number{color:var(--subject-ink)}.trail-stop.completed .stop-number{color:var(--subject-ink)}.stop-stars{position:absolute;right:-11px;bottom:-9px;padding:2px 5px;border:2px solid white;border-radius:9px;background:#fff5d8;color:#956d20;font-size:10px;white-space:nowrap}.you-are-here{position:absolute;top:-24px;left:50%;padding:4px 7px;transform:translateX(-50%);border-radius:7px;background:var(--subject-deep);color:#fff;font-size:10px;letter-spacing:.05em;white-space:nowrap}.path-decoration,.path-finish{filter:drop-shadow(0 2px 3px color-mix(in srgb,var(--subject-primary) 12.5%,transparent))}.map-legend{display:flex;justify-content:center;flex-wrap:wrap;gap:18px;padding-top:1px;color:var(--subject-primary);font-size:11px}.map-legend span{display:flex;align-items:center;gap:6px}.map-legend i{display:inline-grid;width:15px;height:15px;place-items:center;border-radius:50%;font-size:10px;font-style:normal}.legend-current{background:var(--subject-primary);box-shadow:0 0 0 3px color-mix(in srgb,var(--subject-primary) 12.5%,transparent)}.legend-done{background:#e7b84f;color:#fff}.legend-locked{background:#e1e6e1;font-size:10px!important}
.state-card{padding:38px 15px;text-align:center;color:var(--subject-primary)}.state-card.error{color:#a84747}.state-card button{margin-left:7px;border:0;background:none;color:var(--subject-ink);font-weight:700;cursor:pointer}
/* A clear floor selector replaces the long, decorative trail map. */
.stage-levels{display:grid;grid-template-columns:repeat(10,minmax(0,1fr));gap:12px;height:auto;min-height:0;padding:24px;border:1px solid #e6eae4;border-radius:18px;background:var(--subject-soft)}
.trail-stop{position:relative;inset:auto;display:flex;width:100%;height:auto;min-height:72px;transform:none;flex-direction:column;gap:5px;place-items:initial;padding:9px 5px 7px;border:1px solid #e0e6e0;border-radius:13px;background:#fff;box-shadow:none;color:var(--subject-primary);cursor:not-allowed;transition:transform .16s,border-color .16s,background .16s,box-shadow .16s}
.trail-stop.playable:hover{transform:translateY(-2px);border-color:var(--subject-primary);background:#fbfffc;box-shadow:0 4px 0 var(--subject-primary),0 7px 12px color-mix(in srgb,var(--subject-primary) 7.1%,transparent)}
.trail-stop.locked{background:#fff;color:var(--subject-muted);border-color: var(--subject-line)}.trail-stop.completed{border-color:var(--subject-line);background:var(--subject-tint);box-shadow:0 2px 0 var(--subject-primary);color:var(--subject-primary)}.trail-stop.current{width:100%;height:auto;min-height:74px;transform:none;border:2px solid var(--subject-primary);border-radius:14px;background:var(--subject-tint);box-shadow:0 0 0 3px color-mix(in srgb,var(--subject-primary) 9.4%,transparent),0 3px 0 var(--subject-primary);color:var(--subject-strong);font-size:11px;animation:none}.trail-stop.current:hover{transform:translateY(-2px)}
.stop-number{order:0;margin:0;padding:0;background:none;color:inherit;font-size:15px;line-height:1;font-weight:750}.level-icon{order:0;width:auto;height:20px;background:none;box-shadow:none;color:inherit;font-family:inherit;font-size:15px;line-height:1}.trail-stop.current .level-icon,.trail-stop.completed .level-icon,.trail-stop.locked .level-icon{background:none}.trail-stop.current .stop-number{color:var(--subject-ink)}.trail-stop.completed .stop-number{color:var(--subject-primary)}
.stop-stars{position:static;align-self:center;margin-top:auto;padding:0;border:0;background:none;color:#a47823;font-size:10px;line-height:1}.you-are-here{top:-10px;left:50%;padding:3px 6px;border:2px solid #f4f7f2;border-radius:99px;background:var(--subject-deep);font-size:10px;letter-spacing:.04em}
.stage-levels~.map-legend{padding:5px 0 2px}
@keyframes pulse{0%,100%{box-shadow:0 0 0 4px color-mix(in srgb,var(--subject-primary) 12.5%,transparent),0 5px 14px color-mix(in srgb,var(--subject-primary) 26.7%,transparent)}50%{box-shadow:0 0 0 8px color-mix(in srgb,var(--subject-primary) 6.3%,transparent),0 5px 18px color-mix(in srgb,var(--subject-primary) 33.3%,transparent)}}@keyframes float{0%,100%{transform:translateY(0)}50%{transform:translateY(-3px)}}
@media(max-width:900px){.stage-gallery{grid-template-columns:repeat(5,minmax(104px,1fr));overflow-x:auto;padding:2px 1px 6px;scrollbar-color:var(--subject-on-dark) transparent;scrollbar-width:thin}}
@media(max-width:700px){.tower-hero{padding:22px 20px}.hero-trophy{right:18px;top:45px;font-size:52px}.tower-hero h1{max-width:72%;font-size:28px}.tower-hero>p{max-width:88%;font-size:13px}.journey-card{padding:12px;border-radius:19px}.journey-content{gap:12px}.stage-gallery{grid-template-columns:repeat(5,112px);gap:7px}.stage-card{min-height:74px;padding:8px;grid-template-columns:36px minmax(0,1fr);column-gap:7px;border-radius:15px}.stage-illustration{width:35px;height:35px;border-radius:12px;font-size:21px}.stage-card-copy strong{padding-right:32px;font-size:10px}.stage-card-copy small{font-size:10px}.stage-card-count{right:6px;top:5px;padding:3px 5px;font-size:10px}.stage-heading{gap:10px;padding:11px;background: var(--subject-soft)}.stage-art{width:44px;height:44px;border-radius:13px;font-size:28px;background: var(--subject-tint);color: var(--subject-primary);box-shadow: none}.stage-copy h2{font-size:16px}.stage-copy p{font-size:12px;line-height:1.7}.stage-kicker{font-size:10px}.stage-progress{font-size:10px}.stage-progress strong{font-size:17px}.stage-levels{grid-template-columns:repeat(5,minmax(0,1fr));gap:12px;padding:24px;background: var(--subject-soft)}.trail-stop{min-height:72px;border-radius:11px;box-shadow: none}.trail-stop.current{min-height:64px;border-radius:12px;background: var(--subject-tint);color: var(--subject-strong);border: 2px solid var(--subject-primary)}.stop-number{font-size:14px}.stop-stars{font-size:10px}.map-legend{gap:8px 13px}.tower-summary{gap:7px}.tower-summary span{font-size:11px;display: inline-flex;align-items: center;gap: 7px}.journey-progress{max-width:100%}}
@media(prefers-reduced-motion:reduce){.hero-trophy,.trail-stop.current{animation:none}.start-button,.stage-card,.trail-stop,.hero-progress span,.stage-card-copy i span,.lesson-path-progress{transition:none}}
</style>
