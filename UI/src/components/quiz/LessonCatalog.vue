<template>
  <QuizLayout>
    <section class="catalog-hero" :class="subjectClass">
      <RouterLink :to="`/subjects/${subjectSlug}`" class="back-link">← Quay lại môn học</RouterLink>
      <div class="hero-art" aria-hidden="true">{{ subjectIcon }}</div>
      <span class="eyebrow">GÓC HỌC TẬP · LỚP {{ grade }}</span>
      <h1>{{ subject }} lớp {{ grade }}</h1>
      <p>Chọn chủ đề em muốn học. Mỗi bài có nội dung và video hướng dẫn để em ôn tập theo nhịp riêng.</p>
      <div class="hero-tags"><span>📚 {{ lessons.length }} bài học</span><span>🧭 {{ groups.length }} chủ đề</span></div>
    </section>

    <section class="catalog-body">
      <header class="catalog-heading"><div><span class="eyebrow">CHƯƠNG TRÌNH HỌC</span><h2>Danh sách bài học</h2></div><span class="sort-note">Sắp xếp theo chủ đề</span></header>
      <div v-if="loading" class="state-card">Đang tải danh sách bài học…</div>
      <div v-else-if="error" class="state-card error">{{ error }} <button @click="loadLessons">Thử lại</button></div>
      <div v-else-if="!groups.length" class="empty-state"><span>📖</span><h3>Chưa có bài học cho lớp {{ grade }}</h3><p>Giáo viên sẽ sớm bổ sung bài giảng và video hướng dẫn.</p></div>
      <div v-else class="topic-grid">
        <section v-for="(group, groupIndex) in groups" :key="group.name" class="topic-card" :class="`topic-color-${groupIndex % 4}`">
          <header class="topic-heading"><span class="topic-number">{{ String(groupIndex + 1).padStart(2, '0') }}</span><div><span class="topic-label">CHỦ ĐỀ {{ groupIndex + 1 }}</span><h3>{{ group.name }}</h3></div><span class="topic-count">{{ group.lessons.length }} bài</span></header>
          <div class="lesson-rows">
            <RouterLink v-for="(lesson, lessonIndex) in group.lessons" :key="lesson.id" :to="videoPath(lesson)" class="lesson-row">
              <span class="lesson-number">{{ lessonNumber(lesson.title, lessonIndex + 1) }}</span>
              <span class="lesson-copy"><strong>{{ cleanTitle(lesson.title) }}</strong><small>{{ lesson.summary || 'Bài học và hướng dẫn ôn tập' }}</small></span>
              <span class="lesson-action" :class="{ video: lesson.videoUrl }">{{ lesson.videoUrl ? '▶' : '→' }}</span>
            </RouterLink>
          </div>
        </section>
      </div>
    </section>
  </QuizLayout>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'

const route = useRoute()
const subjectBySlug = { toan: 'Toán', 'tieng-anh': 'Tiếng Anh', 'tieng-viet': 'Tiếng Việt' }
const subjectSlug = computed(() => route.params.subjectSlug)
const subject = computed(() => subjectBySlug[subjectSlug.value] || 'Môn học')
const subjectClass = computed(() => subjectSlug.value)
const subjectIcon = computed(() => subjectSlug.value === 'toan' ? '📐' : subjectSlug.value === 'tieng-anh' ? '🌍' : '📚')
const grade = computed(() => Math.min(12, Math.max(4, Number(route.params.grade) || 4)))
const lessons = ref([])
const loading = ref(true)
const error = ref('')
const topicFor = lesson => lesson.topic?.trim() || lesson.title.match(/^(Chủ đề\s*\d+\s*[:：—-]?[^·|]*)[·|]*/i)?.[1]?.trim() || 'Bài học khác'
const groups = computed(() => {
  const grouped = new Map()
  for (const lesson of [...lessons.value].sort((a, b) => a.title.localeCompare(b.title, 'vi', { numeric: true }))) {
    const topic = topicFor(lesson)
    if (!grouped.has(topic)) grouped.set(topic, [])
    grouped.get(topic).push(lesson)
  }
  return [...grouped].map(([name, items]) => ({ name, lessons: items }))
})
const cleanTitle = value => value.replace(/^Chủ đề\s*\d+\s*[:：—-]?\s*/i, '').trim()
const lessonNumber = (title, fallback) => title.match(/\bBài\s*(\d+)\b/i)?.[1]?.padStart(2, '0') || String(fallback).padStart(2, '0')
const videoPath = lesson => ({ name: 'LessonVideo', params: { subjectSlug: subjectSlug.value, grade: grade.value, id: lesson.id } })

const loadLessons = async () => {
  loading.value = true
  error.value = ''
  try {
    const response = await httpClient.get('/quizzes/lessons', { params: { grade: grade.value, subject: subject.value } })
    lessons.value = response.data || []
  } catch {
    error.value = 'Chưa tải được danh sách bài học. Em thử lại nhé.'
  } finally {
    loading.value = false
  }
}
watch([subject, grade], loadLessons, { immediate: true })
</script>

<style scoped>
.catalog-hero {
  --hero-ink: var(--subject-ink);
  --hero-muted: var(--subject-muted);
  --hero-accent: var(--subject-primary);
  --hero-tint: var(--subject-tint);
  --hero-border: var(--subject-line);
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: 250px;
  padding: 32px 36px;
  border: 1px solid var(--hero-border);
  border-radius: 26px;
  background: linear-gradient(110deg, #fffdfd 0%, var(--subject-soft) 40%, var(--hero-tint) 100%);
  color: var(--hero-ink);
  box-shadow: 0 12px 34px color-mix(in srgb,var(--subject-primary) 4.7%,transparent), inset 0 1px 0 #fff;
}
.catalog-hero::after { position: absolute; z-index: -1; right: 4%; bottom: -125px; width: 340px; height: 260px; border: 1px solid #ffffffb3; border-radius: 50%; box-shadow: 0 0 0 25px #ffffff30, 0 0 0 55px #ffffff24; content: ''; pointer-events: none; }
.back-link { display: inline-flex; align-items: center; padding: 7px 11px; border: 1px solid var(--hero-border); border-radius: 10px; background: #ffffff9c; color: var(--hero-accent); text-decoration: none; font-size: 13px; font-weight: 750; transition: background .18s; }
.back-link:hover { background: #fff; color: var(--hero-ink); }
.back-link:focus-visible { outline: 3px solid var(--hero-accent); outline-offset: 3px; }
.eyebrow { display: block; margin-top: 20px; color: var(--hero-accent, var(--subject-primary)); font-size: 11px; font-weight: 750; letter-spacing: .16em; }
.catalog-hero h1 { position: relative; z-index: 1; margin: 8px 0 10px; color: var(--hero-ink); font-size: clamp(30px,4vw,44px); font-weight: 700; letter-spacing: -.04em; }
.catalog-hero p { position: relative; z-index: 1; max-width: 560px; margin: 0; color: var(--hero-muted); font-size: 14px; line-height: 1.7; }
.hero-art { position: absolute; right: 10%; top: 50%; transform: translateY(-43%) rotate(-5deg); font-size: clamp(75px,11vw,128px); filter: drop-shadow(0 12px 18px color-mix(in srgb,var(--subject-primary) 14%,transparent)); opacity: .9; pointer-events: none; color: var(--subject-primary); }
.hero-tags { position: relative; z-index: 1; display: flex; flex-wrap: wrap; gap: 9px; margin-top: 22px; }
.hero-tags span { padding: 9px 13px; border: 1px solid var(--hero-border); border-radius: 12px; background: #ffffffbf; color: var(--hero-ink); font-size: 12px; font-weight: 700; box-shadow: 0 3px 8px color-mix(in srgb,var(--subject-primary) 1.6%,transparent); display: inline-flex; align-items: center; gap: 7px; }
.catalog-body{margin-top:26px}.catalog-heading{display:flex;align-items:end;justify-content:space-between;margin-bottom:15px}.catalog-heading .eyebrow{margin:0;color:var(--subject-primary)}.catalog-heading h2{margin:5px 0 0;color:var(--subject-ink);font-size:24px}.sort-note{color:var(--subject-muted);font-size:12px}.topic-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.topic-card{overflow:hidden;border:1px solid var(--subject-line);border-radius:19px;background:#fff;box-shadow:0 8px 25px color-mix(in srgb,var(--subject-primary) 3.9%,transparent)}.topic-heading{position:relative;isolation:isolate;overflow:hidden;display:flex;align-items:center;gap:12px;padding:20px;background:var(--subject-soft);border-bottom:1px solid var(--subject-line)}.topic-number{position:relative;z-index:1;display:grid;flex:0 0 42px;height:42px;place-items:center;border:1px solid #ffffffa8;border-radius:14px;background:var(--subject-tint);color:var(--subject-strong);font-size:13px;font-weight:750;box-shadow:none;border-color: var(--subject-line)}.topic-label{position:relative;z-index:1;display:inline-flex;align-items:center;width:max-content;padding:0;border:0;border-radius:99px;background:transparent;color:var(--subject-primary);font-size:10px;font-weight:750;letter-spacing:.12em;text-transform:uppercase}.topic-heading h3{position:relative;z-index:1;margin:5px 0 0;color:var(--subject-ink);font-size:19px;font-weight:750;line-height:1.45;letter-spacing:-.035em;text-wrap:pretty;text-shadow:0 1px #ffffff80}.topic-heading>div{position:relative;z-index:1;min-width:0}.topic-heading h3::selection{background:var(--subject-tint);color:var(--subject-ink)}.topic-count{position:relative;z-index:1;margin-left:auto;padding:7px 10px;border:1px solid #ffffffd9;border-radius:99px;background:#ffffffdc;color:var(--subject-primary);font-size:11px;font-weight:750;white-space:nowrap;box-shadow:0 3px 10px color-mix(in srgb,var(--subject-primary) 7.1%,transparent)}.lesson-rows{padding:5px 14px 12px}.lesson-row{display:flex;align-items:center;gap:12px;padding:15px 5px;border-bottom:1px solid #f0edf3;color:inherit;text-decoration:none;transition:background .15s,transform .15s}.lesson-row:last-child{border-bottom:0}.lesson-row:hover{transform:translateX(3px);background:#faf7fd}.lesson-number{display:grid;flex:0 0 34px;height:34px;place-items:center;border-radius:11px;background:var(--subject-soft);color:var(--subject-primary);font-size:11px;font-weight:750}.lesson-copy{display:flex;min-width:0;flex:1;flex-direction:column;gap:4px}.lesson-copy strong{color:var(--subject-ink);font-size:14px;line-height:1.4;font-weight: 600}.lesson-copy small{overflow:hidden;color:var(--subject-muted);font-size:12px;text-overflow:ellipsis;white-space:nowrap}.lesson-action{display:grid;flex:0 0 32px;height:32px;place-items:center;border-radius:50%;background:var(--subject-soft);color:var(--subject-primary);font-size:14px;font-weight:750}.lesson-action.video{background:var(--subject-soft);color:var(--subject-muted)}.state-card,.empty-state{padding:42px 20px;border:1px solid var(--subject-line);border-radius:18px;background:#fff;color:var(--subject-primary);text-align:center}.state-card.error{color:#b84e54}.state-card button{margin-left:8px;border:0;background:none;color:var(--subject-primary);font-weight:750;cursor:pointer}.empty-state>span{font-size:34px}.empty-state h3{margin:10px 0 5px;color:var(--subject-ink)}.empty-state p{margin:0;color:var(--subject-muted);font-size:13px}
@media(max-width:800px){.topic-grid{grid-template-columns:1fr}.hero-art{right:6%;font-size:84px;color: var(--subject-primary);opacity: .9;filter: drop-shadow(0 12px 18px color-mix(in srgb,var(--subject-primary) 14%,transparent))}.catalog-hero p{max-width:70%}}
@media(max-width:560px){.catalog-hero{min-height:0;padding:21px 19px;border-radius:20px}.catalog-hero h1{max-width:75%;font-size:29px}.catalog-hero p{max-width:75%;font-size:13px}.hero-art{right:1%;top:43%;font-size:65px;opacity:.9;color: var(--subject-primary);filter: drop-shadow(0 12px 18px color-mix(in srgb,var(--subject-primary) 14%,transparent))}.catalog-heading{align-items:start;gap:7px;flex-direction:column}.catalog-heading h2{font-size:21px}.topic-heading{padding:20px;background: var(--subject-soft)}.topic-heading h3{font-size:19px;line-height: 1.45}.lesson-rows{padding:4px 10px 9px}.lesson-row{gap:9px;padding:15px 5px}.lesson-copy strong{font-size:14px;font-weight: 600}}
</style>
