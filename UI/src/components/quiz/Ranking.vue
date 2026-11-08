<template>
  <QuizLayout>
    <section class="tower-hero">
      <RouterLink :to="subjectPath" class="back-link">← Quay lại môn học</RouterLink>
      <div class="hero-trophy">🏔️</div>
      <span class="eyebrow">HÀNH TRÌNH CHINH PHỤC</span>
      <h1>Leo tháp {{ subject }}</h1>
      <p>Chinh phục 1000 tầng, tích lũy sao và mở khóa thử thách khó dần.</p>
      <div class="tower-summary"><span>🗼 <strong>{{ completedLevels }}/1000</strong> tầng hoàn thành</span><span>⭐ <strong>{{ totalStars }}/{{ levels.length * 5 }}</strong> sao</span></div>
      <div class="journey-progress"><div><span>TIẾN ĐỘ HÀNH TRÌNH</span><strong>{{ completedLevels }}/1000 tầng</strong></div><div class="hero-progress"><span :style="{ width: `${completedLevels / 10}%` }"></span></div></div>
      <p class="rules-note">Mỗi tầng tăng dần độ khó từ lớp 5 đến lớp 12. Đạt 90% được 5 sao; mỗi bậc 10% thấp hơn giảm 1 sao. Cần 5 sao để mở tầng kế.</p>
      <button class="start-button" :disabled="startDisabled" @click="error ? loadProgress() : currentLevel && enterLevel(currentLevel)">{{ startButtonLabel }}</button>
      <p v-if="currentLevel && !loading && !error && currentLevel.questionCount < currentLevel.requiredQuestions" class="start-hint">Tầng {{ currentLevel.level }} cần {{ currentLevel.requiredQuestions }} câu {{ subject }} lớp {{ currentLevel.questionGrade }}, hiện có {{ currentLevel.questionCount }}. Hãy import câu hỏi ở trang quản trị.</p>
    </section>

    <section class="journey-card">
      <div v-if="loading" class="state-card">Đang tải tiến trình leo tháp…</div>
      <div v-else-if="error" class="state-card error">{{ error }} <button @click="loadProgress">Thử lại</button></div>
      <div v-else class="journey-content">
        <div class="stage-gallery">
          <button v-for="number in 10" :key="number" class="stage-card" :class="[`stage-color-${number}`, { selected: number === stage, reached: number <= furthestStage }]" @click="stage = number">
            <span class="stage-illustration">{{ stageIcons[number - 1] }}</span><span class="stage-card-copy"><strong>Chặng {{ String(number).padStart(2, '0') }}</strong><small>{{ stageNames[number - 1] }}</small><i><span :style="{ width: `${stageCompletion(number)}%` }"></span></i></span><span class="stage-card-count">{{ getStageCompleted(number) }}/100</span>
          </button>
        </div>
        <header class="stage-heading"><div class="stage-art">{{ stageIcons[stage - 1] }}</div><div class="stage-copy"><span class="stage-kicker">CHẶNG {{ String(stage).padStart(2, '0') }} · TẦNG {{ stageStart }}–{{ stageEnd }}</span><h2>{{ stageNames[stage - 1] }}</h2><p>{{ stageDescriptions[stage - 1] }} · Câu hỏi lớp {{ stageGrade }}</p></div><div class="stage-progress"><strong>{{ stageCompleted }}/100</strong><span>tầng xong</span></div></header>
        <div class="stage-levels" :style="{ '--trail-height': `${stageLevels.length * levelPitch}px` }">
          <svg class="landscape-art" :viewBox="`0 0 1000 ${Math.max(stageLevels.length * levelPitch, levelPitch)}`" preserveAspectRatio="none" aria-hidden="true">
            <path class="river-bank" :d="landscapeRiverPath" />
            <path class="river-water" :d="landscapeRiverPath" />
            <g v-for="scene in landscapeScenes" :key="scene.index" :transform="`translate(0 ${scene.y}) scale(1 .75)`" class="mountain-scene">
              <circle class="scene-sun" :cx="scene.index % 2 ? 170 : 830" cy="92" r="42" />
              <g class="scene-cloud" :transform="`translate(${scene.index % 2 ? 640 : 150} 165)`"><path d="M15 42c-23 0-24-32 0-34 5-30 49-33 58-5 28-6 37 31 9 39z"/><circle cx="20" cy="51" r="3"/><circle cx="61" cy="51" r="3"/></g>
              <path class="mountain-far" d="M-30 650 90 420 180 535 275 360 410 650Z M610 650 735 405 830 525 925 345 1040 650Z" />
              <path class="mountain-near" d="M-40 700 70 530 190 650 305 470 440 700Z M570 700 705 500 830 650 945 450 1050 700Z" />
              <path class="mountain-snow" d="m58 480 32-60 34 65-30-17-16 25z m188-87 59-33 30 44-38-20-24 32z m518 43 35-31 31 43-34-18-19 28z m145-35 17-56 19 62-17-18z" />
              <g class="mountain-trees"><path d="m24 675 22-52 22 52zm38 0 18-43 18 43zm815 0 22-52 22 52zm39 0 18-43 18 43z" /></g>
            </g>
          </svg>
          <svg class="lesson-path" :viewBox="`0 0 1000 ${Math.max(stageLevels.length * levelPitch, levelPitch)}`" preserveAspectRatio="none" aria-hidden="true">
            <path class="lesson-path-base" pathLength="100" :d="lessonPath" />
            <path class="lesson-path-progress" pathLength="100" :style="{ strokeDasharray: `${stageProgress} 100` }" :d="lessonPath" />
          </svg>
          <button v-for="(level, index) in stageLevels" :key="level.level" class="trail-stop" :style="{ left: `${nodePosition(index)}%`, top: `${(stageLevels.length - index - 1) * levelPitch + levelPitch / 2}px` }" :class="{ locked: !level.isUnlocked, completed: level.isCompleted, current: currentLevel?.level === level.level, playable: level.isUnlocked && level.questionCount >= level.requiredQuestions }" :disabled="!level.isUnlocked || level.questionCount < level.requiredQuestions" :title="`Tầng ${level.level} · ${level.isUnlocked ? `${level.stars}/5 sao` : 'Đang khóa'}`" @click="enterLevel(level)">
            <span class="stop-number">{{ level.level }}</span><span class="level-icon">{{ level.isCompleted ? '🏆' : !level.isUnlocked ? '🔒' : currentLevel?.level === level.level ? '✨' : levelIcons[(level.level - 1) % levelIcons.length] }}</span><span v-if="level.stars" class="stop-stars">★{{ level.stars }}</span><span v-if="currentLevel?.level === level.level" class="you-are-here">BẮT ĐẦU</span>
          </button>
          <div class="path-decoration cloud-left">☁️</div><div class="path-decoration cloud-right">☁️</div><div class="path-finish">🏆</div>
        </div>
        <footer class="map-legend"><span><i class="legend-current"></i> Tầng hiện tại</span><span><i class="legend-done">✓</i> Đã vượt qua</span><span><i class="legend-locked">🔒</i> Chưa mở khóa</span></footer>
      </div>
    </section>
  </QuizLayout>
</template>

<script setup>
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'
import { guestRequestConfig } from '../../utils/guestIdentity'

const route = useRoute()
const router = useRouter()
const subjectSlugs = { 'Toán': 'toan', 'Tiếng Anh': 'tieng-anh', 'Tiếng Việt': 'tieng-viet' }
const subject = computed(() => Object.hasOwn(subjectSlugs, route.query.subject) ? route.query.subject : 'Toán')
const subjectPath = computed(() => `/subjects/${subjectSlugs[subject.value]}`)
const levels = ref([])
const loading = ref(false)
const error = ref('')
const stage = ref(1)
const stageSize = 100
const levelPitch = 54
const stageStart = computed(() => (stage.value - 1) * stageSize + 1)
const stageEnd = computed(() => Math.min(stage.value * stageSize, levels.value.length))
const stageLevels = computed(() => levels.value.slice(stageStart.value - 1, stageEnd.value))
const stageCompleted = computed(() => stageLevels.value.filter(item => item.isCompleted).length)
const stageProgress = computed(() => stageLevels.value.length ? stageCompleted.value / stageLevels.value.length * 100 : 0)
const nodePosition = index => 50 + 30 * Math.sin(index * 0.82)
const lessonPath = computed(() => stageLevels.value.map((_, index) => {
  const x = nodePosition(index) * 10
  const y = (stageLevels.value.length - index - 1) * levelPitch + levelPitch / 2
  if (index === 0) return `M ${x} ${y}`
  const previousX = nodePosition(index - 1) * 10
  const previousY = (stageLevels.value.length - index) * levelPitch - levelPitch / 2
  const middleY = (previousY + y) / 2
  return `C ${previousX} ${middleY}, ${x} ${middleY}, ${x} ${y}`
}).join(' '))
const landscapeSceneHeight = computed(() => levelPitch * 10)
const landscapeScenes = computed(() => Array.from({ length: Math.ceil(stageLevels.value.length / 10) }, (_, index) => ({ index, y: index * landscapeSceneHeight.value })))
const landscapeRiverPath = computed(() => {
  const sections = Math.max(1, Math.ceil(stageLevels.value.length / 10))
  const bends = [145, 820, 250, 760, 180, 875, 330, 690, 210, 800, 145]
  const sceneHeight = landscapeSceneHeight.value
  let path = `M ${bends[0]} 0`
  for (let index = 0; index < sections; index++) {
    const startX = bends[index % bends.length]
    const endX = bends[(index + 1) % bends.length]
    const startY = index * sceneHeight
    const endY = (index + 1) * sceneHeight
    path += ` C ${startX} ${startY + sceneHeight / 3}, ${endX} ${endY - sceneHeight / 3}, ${endX} ${endY}`
  }
  return path
})
const stageGrade = computed(() => stageLevels.value[0]?.questionGrade || 5)
const furthestStage = computed(() => Math.max(1, Math.ceil((currentLevel.value?.level || 1) / stageSize)))
const stageNames = ['Chân núi khởi đầu', 'Rừng tri thức', 'Suối thử thách', 'Thung lũng khám phá', 'Vách đá kiên trì', 'Cầu mây học tập', 'Đỉnh gió lớn', 'Thành trì trí tuệ', 'Đường lên mây', 'Đỉnh cao chinh phục']
const stageDescriptions = ['Làm quen và tích lũy những ngôi sao đầu tiên', 'Băng qua khu rừng câu hỏi mới', 'Giữ nhịp học tập qua dòng suối kiến thức', 'Khám phá những thử thách ngày càng khó', 'Vững vàng chinh phục những vách đá cao', 'Bước tiếp trên cây cầu tri thức', 'Vượt gió lớn bằng sự tập trung', 'Mở cổng thành bằng thành tích của em', 'Chạm gần hơn tới bầu trời tri thức', 'Chặng cuối cùng để trở thành nhà chinh phục']
const stageIcons = ['🌱', '🌲', '💧', '🌄', '🧗', '☁️', '🌬️', '🏯', '🌤️', '🏔️']
const levelIcons = ['🌸', '🏮', '🧧', '🍊', '🐉']
const totalStars = computed(() => levels.value.reduce((sum, item) => sum + item.stars, 0))
const completedLevels = computed(() => levels.value.filter(item => item.isCompleted).length)
const currentLevel = computed(() => levels.value.find(item => item.isUnlocked && !item.isCompleted) || levels.value.at(-1))
const startDisabled = computed(() => loading.value || (!error.value && (!currentLevel.value || currentLevel.value.questionCount < currentLevel.value.requiredQuestions)))
const startButtonLabel = computed(() => {
  if (loading.value) return 'Đang tải tháp…'
  if (error.value) return 'Thử tải lại tiến trình'
  if (!currentLevel.value) return 'Chưa có tầng khả dụng'
  if (currentLevel.value.questionCount < currentLevel.value.requiredQuestions) return 'Cần thêm câu hỏi'
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
      questionCount: item.questionCount ?? item.QuestionCount ?? 0
    }))
    stage.value = Math.ceil((currentLevel.value?.level || 1) / stageSize)
  } catch (err) {
    error.value = err?.response?.data?.message || 'Chưa tải được tiến trình tháp. Em thử lại nhé.'
  } finally {
    loading.value = false
  }
}

const enterLevel = level => {
  if (!level.isUnlocked || level.questionCount < level.requiredQuestions) return
  router.push(`/tower-session/${subjectSlugs[subject.value]}/${level.level}`)
}

onMounted(loadProgress)
</script>

<style scoped>
.tower-hero{position:relative;overflow:hidden;padding:25px 30px 23px;border:1px solid #e8e9f5;border-radius:26px;background:radial-gradient(ellipse at 90% 0%,#fff0c8 0,transparent 35%),linear-gradient(120deg,#f5f5ff,#fff 60%,#f0f8ff)}
.back-link{color:#6874cf;text-decoration:none;font-size:13px;font-weight:800}
.hero-trophy{position:absolute;right:6%;top:28px;font-size:86px;filter:drop-shadow(0 12px 12px #7482a333);animation:float 4s ease-in-out infinite}
.eyebrow{display:block;margin-top:17px;color:#8d78cc;font-size:10px;font-weight:900;letter-spacing:.16em}
.tower-hero h1{margin:5px 0;color:#303c68;font-size:32px}
.tower-hero>p{margin:0;color:#8189a0}
.tower-summary{display:flex;gap:10px;margin-top:17px}
.tower-summary span{padding:9px 12px;border:1px solid #e9e8f2;border-radius:11px;background:#ffffffc9;color:#727b94;font-size:11px}
.tower-summary strong{color:#4c5574}
.journey-progress{max-width:430px;margin-top:14px}
.journey-progress>div:first-child{display:flex;justify-content:space-between;color:#8c91a5;font-size:9px;font-weight:900;letter-spacing:.08em}
.journey-progress strong{color:#6672cd;letter-spacing:0}
.hero-progress{height:8px;margin-top:7px;overflow:hidden;border-radius:8px;background:#e9eaf2}
.hero-progress span{display:block;height:100%;border-radius:8px;background:linear-gradient(90deg,#f0bf58,#9d7be1);transition:width .7s ease}
.tower-hero .rules-note{margin-top:12px;font-size:11px}
.start-button{margin-top:11px;padding:12px 20px;border:0;border-radius:13px;background:linear-gradient(120deg,#f0b84d,#e88c50);box-shadow:0 7px 16px #c7863833;color:#fff;font-weight:900;cursor:pointer;transition:transform .2s,box-shadow .2s}
.start-button:hover:not(:disabled){transform:translateY(-2px);box-shadow:0 10px 20px #c7863844}
.start-button:disabled{opacity:.55;cursor:not-allowed}.tower-hero .start-hint{max-width:520px;margin-top:9px;color:#a36942;font-size:11px;line-height:1.5}
.journey-card{margin-top:18px;padding:22px 24px;border:1px solid #e7eaf3;border-radius:23px;background:linear-gradient(180deg,#fff,#fbfcff);box-shadow:0 12px 32px #2534660c}
.journey-content{display:flex;flex-direction:column;gap:18px}
.stage-gallery{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:11px}
.stage-card{position:relative;display:flex;min-height:132px;flex-direction:column;align-items:center;justify-content:center;gap:6px;overflow:hidden;padding:12px;border:1px solid #e8eaf2;border-radius:18px;text-align:center;cursor:pointer;transition:transform .2s,box-shadow .2s,border-color .2s}
.stage-card:after{position:absolute;right:-18px;top:-24px;width:75px;height:75px;border-radius:50%;background:#ffffff70;content:""}
.stage-card:hover{transform:translateY(-3px);box-shadow:0 9px 18px #3d4d7914}
.stage-card.selected{border:2px solid #8580dc;box-shadow:0 0 0 4px #8580dc18,0 9px 20px #3d4d7915}
.stage-card.reached .stage-card-count{background:#edf8f1;color:#419675}
.stage-color-1{background:linear-gradient(145deg,#fff8e7,#f9eed2)}.stage-color-2{background:linear-gradient(145deg,#f0faed,#e4f3df)}.stage-color-3{background:linear-gradient(145deg,#edf9ff,#dff2fa)}.stage-color-4{background:linear-gradient(145deg,#fff3e9,#fae7d8)}.stage-color-5{background:linear-gradient(145deg,#f7f0ff,#ebe3f9)}.stage-color-6{background:linear-gradient(145deg,#eff8ff,#e1effb)}.stage-color-7{background:linear-gradient(145deg,#fff4ed,#f7e4d9)}.stage-color-8{background:linear-gradient(145deg,#fff9e7,#f2edcf)}.stage-color-9{background:linear-gradient(145deg,#f1f5ff,#e4eafa)}.stage-color-10{background:linear-gradient(145deg,#fff3d9,#f7e6b8)}
.stage-illustration{z-index:1;display:grid;width:68px;height:66px;place-items:center;border-radius:17px;background:#ffffffa8;box-shadow:inset 0 -5px 0 #596a9a0b;font-size:44px;filter:drop-shadow(0 5px 4px #53608220)}
.stage-card-copy{z-index:1;display:flex;width:100%;flex-direction:column;align-items:center;gap:4px}
.stage-card-copy strong{color:#3b466c;font-size:11px}.stage-card-copy small{min-height:0;color:#7c8498;font-size:9px;line-height:1.25}
.stage-card-copy i{height:5px;width:100%;overflow:hidden;border-radius:5px;background:#ffffffc9}.stage-card-copy i span{display:block;height:100%;border-radius:inherit;background:linear-gradient(90deg,#ecc05c,#8c80d7);transition:width .5s}
.stage-card-count{position:absolute;z-index:2;right:8px;top:8px;padding:3px 6px;border-radius:8px;background:#ffffffc9;color:#8c91a0;font-size:8px;font-weight:900}
.stage-heading{display:flex;min-height:96px;align-items:center;gap:15px;padding:15px 18px;border:1px solid #eee8d8;border-radius:18px;background:linear-gradient(110deg,#fff8e9,#fff 65%,#f3f1ff);box-shadow:0 5px 14px #5662880c}
.stage-art{display:grid;width:58px;height:58px;flex:none;place-items:center;border-radius:18px;background:#ffffffbd;box-shadow:0 5px 14px #665b2910;font-size:37px}
.stage-copy{min-width:0;flex:1}.stage-kicker{color:#bd8b34;font-size:9px;font-weight:900;letter-spacing:.12em}.stage-copy h2{margin:3px 0;color:#34406b;font-size:21px}.stage-copy p{margin:0;color:#858ba0;font-size:11px}
.stage-progress{display:flex;flex-direction:column;align-items:flex-end;color:#8a91a3;font-size:10px}.stage-progress strong{color:#6875d3;font-size:20px}
.stage-levels{position:relative;height:var(--trail-height);min-height:72px;overflow:hidden;border:1px solid #efdcca;border-radius:24px;background:radial-gradient(ellipse at 8% 10%,#e7f3fa 0,transparent 28%),radial-gradient(ellipse at 92% 38%,#e6f2e6 0,transparent 26%),linear-gradient(180deg,#f3f8fc,#f4f8f0 52%,#f6f4e9)}
.landscape-art{position:absolute;z-index:0;inset:0;width:100%;height:100%;pointer-events:none}.scene-sun{fill:#ffe8a3;opacity:.8;filter:drop-shadow(0 0 14px #ffe39a99)}.scene-cloud path{fill:#ffffffd9;stroke:#d2e3ee;stroke-width:5}.scene-cloud circle{fill:#8292a0}.mountain-far{fill:#c9dce8;opacity:.58}.mountain-near{fill:#b9d3c0;opacity:.6}.mountain-snow{fill:#f7fbfc;opacity:.9}.mountain-trees{fill:#73977d;opacity:.7}.river-bank{fill:none;stroke:#b5d8d9;stroke-width:76;opacity:.34;stroke-linecap:round}.river-water{fill:none;stroke:#86c7d4;stroke-width:44;opacity:.42;stroke-linecap:round}.lesson-path{position:absolute;inset:0;width:100%;height:100%;overflow:visible;pointer-events:none}.lesson-path path{fill:none;stroke-linecap:round;stroke-linejoin:round}.lesson-path-base{stroke:#fff;stroke-width:62;filter:drop-shadow(0 4px 5px #60709716)}.lesson-path-progress{stroke:#f0bd51;stroke-width:32;stroke-dasharray:0 100;transition:stroke-dasharray .8s ease}
.trail-stop{position:absolute;z-index:2;display:grid;width:44px;height:44px;transform:translate(-50%,-50%);place-items:center;border:4px solid #fff;border-radius:17px;background:linear-gradient(145deg,#d9dce8,#eef0f5);box-shadow:0 4px 0 #cdd1de,0 7px 12px #58628120;color:#8b92a3;font-size:10px;font-weight:900;cursor:not-allowed;transition:transform .2s,box-shadow .2s}
.trail-stop.playable{cursor:pointer}.trail-stop.playable:hover{transform:translate(-50%,-50%) scale(1.08)}.trail-stop.locked{background:linear-gradient(145deg,#e9ebf1,#f5f6f9);color:#aeb3c0}
.trail-stop.completed{background:linear-gradient(145deg,#f6ca62,#eea943);box-shadow:0 5px 0 #d69736,0 8px 15px #95662a24;color:#fff}
.trail-stop.current{width:58px;height:58px;border:5px solid #fff;border-radius:22px;background:linear-gradient(145deg,#7188ef,#a86bd5);box-shadow:0 0 0 6px #837ce82a,0 6px 0 #725fc3,0 10px 16px #5d52a848;color:#fff;font-size:12px;animation:pulse 1.8s ease-in-out infinite}
.stop-number{order:2;margin-top:-2px;padding:2px 6px;border-radius:8px;background:#ffffffc9;color:#64708b;font-size:9px;line-height:1;font-weight:900}.level-icon{order:1;display:grid;width:31px;height:29px;place-items:center;border-radius:11px;background:#ffffffbd;box-shadow:0 2px 5px #554a6310;font-size:22px;line-height:1}.trail-stop.current .level-icon{background:#fff;color:#8069d0;animation:icon-sparkle 1.5s ease-in-out infinite}.trail-stop.completed .level-icon{background:#fff5d5}.trail-stop.locked .level-icon{background:#f5f5f7}.trail-stop.current .stop-number{color:#6254af}.trail-stop.completed .stop-number{color:#926522}.stop-stars{position:absolute;right:-11px;bottom:-9px;padding:2px 5px;border:2px solid white;border-radius:9px;background:#fff4d2;color:#bd851e;font-size:9px;white-space:nowrap}
.you-are-here{position:absolute;top:-24px;left:50%;padding:4px 7px;transform:translateX(-50%);border-radius:7px;background:#6e74d8;color:#fff;font-size:8px;letter-spacing:.05em;white-space:nowrap}
.map-legend{display:flex;justify-content:center;gap:22px;padding-top:3px;color:#838a9e;font-size:10px}.map-legend span{display:flex;align-items:center;gap:6px}.map-legend i{display:inline-grid;width:15px;height:15px;place-items:center;border-radius:50%;font-size:9px;font-style:normal}.legend-current{background:#8b78dd;box-shadow:0 0 0 3px #8b78dd22}.legend-done{background:#edb74e;color:#fff}.legend-locked{background:#e4e6ec;font-size:8px!important}
.state-card{padding:36px 15px;text-align:center;color:#7f889f}.state-card.error{color:#b34c55}.state-card button{margin-left:7px;border:0;background:none;color:#6571d0;font-weight:800}
@keyframes icon-sparkle{0%,100%{scale:1}50%{scale:1.14}}@keyframes pulse{0%,100%{box-shadow:0 0 0 4px #8b82e122,0 5px 14px #706bd644}50%{box-shadow:0 0 0 9px #8b82e10c,0 5px 18px #706bd666}}@keyframes float{0%,100%{transform:translateY(0)}50%{transform:translateY(-8px)}}
@media(max-width:700px){.tower-hero{padding:20px}.hero-trophy{right:18px;top:44px;font-size:54px}.tower-hero h1{max-width:70%;font-size:27px}.journey-card{padding:12px}.stage-gallery{grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}.stage-card{min-height:105px;padding:9px}.stage-illustration{width:48px;height:48px;font-size:33px}.stage-card-copy strong{font-size:10px}.stage-card-copy small{font-size:8px}.stage-heading{gap:10px;padding:11px}.stage-art{width:45px;height:45px;border-radius:14px;font-size:28px}.stage-copy h2{font-size:17px}.stage-copy p{font-size:9px}.stage-progress strong{font-size:17px}.stage-levels{height:var(--trail-height)}.trail-stop{width:40px;height:40px;border-radius:16px;font-size:9px}.trail-stop.current{width:52px;height:52px;border-radius:20px}.lesson-path-base{stroke-width:58}.lesson-path-progress{stroke-width:28}.river-bank{stroke-width:64}.river-water{stroke-width:36}.map-legend{flex-wrap:wrap;gap:8px 14px}.tower-summary{flex-wrap:wrap}}
</style>
