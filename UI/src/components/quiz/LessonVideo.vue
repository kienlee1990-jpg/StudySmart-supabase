<template>
  <QuizLayout>
    <div class="video-page">
      <RouterLink :to="catalogPath" class="back-link">← Danh sách bài học</RouterLink>
      <div v-if="loading" class="state-card">Đang tải bài học…</div>
      <div v-else-if="error" class="state-card error">{{ error }}</div>
      <template v-else-if="lesson">
        <header class="video-heading"><div><span class="eyebrow">{{ subject.toUpperCase() }} · LỚP {{ grade }} · {{ lesson.topic || 'BÀI HỌC' }}</span><h1>{{ lesson.title }}</h1><p v-if="lesson.summary">{{ lesson.summary }}</p></div><span class="book-icon">📚</span></header>
        <section v-if="videoUrl" class="player-card">
          <div v-if="directVideoUrl" class="player-frame"><video :src="directVideoUrl" controls playsinline preload="metadata">Trình duyệt chưa hỗ trợ phát video.</video></div>
          <div v-else-if="embedUrl" class="player-frame"><iframe :src="embedUrl" :title="lesson.title" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen referrerpolicy="strict-origin-when-cross-origin"></iframe></div>
          <div v-else class="external-player"><span>↗</span><div><strong>Video được lưu bằng liên kết ngoài</strong><p>Nền tảng này không hỗ trợ phát nhúng. Mở liên kết để xem video.</p></div></div>
          <div class="player-caption"><span>▶ VIDEO BÀI GIẢNG</span><a :href="videoUrl" target="_blank" rel="noopener noreferrer">Mở video ↗</a></div>
        </section>
        <section v-else class="no-video"><span>🎬</span><div><strong>Bài này chưa có video đính kèm</strong><p>Em vẫn có thể xem nội dung bài học bên dưới.</p></div></section>
        <article class="lesson-content-card"><div class="content-title"><span>✦</span><h2>Nội dung bài học</h2></div><div class="lesson-content">{{ lesson.content }}</div></article>
      </template>
    </div>
  </QuizLayout>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'

const route = useRoute()
const subjectBySlug = { toan: 'Toán', 'tieng-anh': 'Tiếng Anh', 'tieng-viet': 'Tiếng Việt' }
const subject = computed(() => subjectBySlug[route.params.subjectSlug] || 'Môn học')
const grade = computed(() => Math.min(12, Math.max(4, Number(route.params.grade) || 4)))
const catalogPath = computed(() => `/learning/${route.params.subjectSlug}/${grade.value}`)
const lesson = ref(null)
const loading = ref(true)
const error = ref('')
const videoUrl = computed(() => lesson.value?.videoUrl ?? lesson.value?.VideoUrl ?? '')
const directVideoUrl = computed(() => /\.(mp4|webm|ogg)(?:$|[?#])/i.test(videoUrl.value) ? videoUrl.value : '')
const embedUrl = computed(() => {
  const value = videoUrl.value
  if (!value || directVideoUrl.value) return ''
  try {
    const url = new URL(value)
    if (!['http:', 'https:'].includes(url.protocol)) return ''
    const host = url.hostname.replace(/^www\./, '').toLowerCase()
    if (host === 'youtu.be') return `https://www.youtube-nocookie.com/embed/${url.pathname.split('/').filter(Boolean)[0]}`
    if (['youtube.com', 'm.youtube.com', 'youtube-nocookie.com'].includes(host)) {
      const id = url.searchParams.get('v') || url.pathname.match(/\/(?:embed|shorts|live)\/([^/?]+)/)?.[1]
      return id ? `https://www.youtube-nocookie.com/embed/${id}` : ''
    }
    if (host === 'vimeo.com') return `https://player.vimeo.com/video/${url.pathname.split('/').filter(Boolean).at(-1)}`
    if (host === 'drive.google.com') {
      const id = url.pathname.match(/\/file\/d\/([^/]+)/)?.[1]
      if (id) return `https://drive.google.com/file/d/${id}/preview`
    }
    return url.href
  } catch { return '' }
})

const loadLesson = async () => {
  loading.value = true
  error.value = ''
  try {
    const response = await httpClient.get('/quizzes/lessons', { params: { grade: grade.value, subject: subject.value } })
    const lessons = (response.data || []).map(item => ({ ...item, id: item.id ?? item.Id, title: item.title ?? item.Title, topic: item.topic ?? item.Topic, summary: item.summary ?? item.Summary, content: item.content ?? item.Content, videoUrl: item.videoUrl ?? item.VideoUrl }))
    lesson.value = lessons.find(item => String(item.id) === String(route.params.id)) || null
    if (!lesson.value) error.value = 'Không tìm thấy bài học này.'
  } catch {
    error.value = 'Chưa tải được bài học. Em thử lại nhé.'
  } finally {
    loading.value = false
  }
}
watch([() => route.params.id, subject, grade], loadLesson, { immediate: true })
</script>

<style scoped>
.video-page{max-width:1050px;margin:0 auto}.back-link{display:inline-block;margin-bottom:16px;color:var(--subject-primary);text-decoration:none;font-size:13px;font-weight:750}.back-link:hover{color:var(--subject-primary)}.video-heading{display:flex;align-items:center;justify-content:space-between;gap:20px;margin-bottom:17px;padding:24px 27px;border:1px solid var(--subject-line);border-radius:20px;background:linear-gradient(115deg,var(--subject-soft),#fff 70%);box-shadow:0 8px 25px color-mix(in srgb,var(--subject-primary) 3.5%,transparent)}.eyebrow{color:var(--subject-primary);font-size:10px;font-weight:750;letter-spacing:.13em}.video-heading h1{margin:7px 0;color:var(--subject-ink);font-size:clamp(23px,3vw,32px);letter-spacing:-.025em}.video-heading p{margin:0;color:var(--subject-primary);font-size:13px;line-height:1.6}.book-icon{display:grid;flex:0 0 60px;height:60px;place-items:center;border-radius:18px;background:#fff;font-size:31px;box-shadow:0 7px 18px color-mix(in srgb,var(--subject-primary) 7.1%,transparent)}.player-card,.lesson-content-card,.no-video,.state-card{overflow:hidden;margin-bottom:17px;border:1px solid var(--subject-line);border-radius:20px;background:#fff;box-shadow:0 9px 26px color-mix(in srgb,var(--subject-primary) 3.9%,transparent)}.player-frame{position:relative;width:100%;aspect-ratio:16/9;background:radial-gradient(circle at 50% 40%,var(--subject-deep),var(--subject-deep))}.player-frame iframe,.player-frame video{position:absolute;inset:0;width:100%;height:100%;border:0}.player-frame video{background:var(--subject-deep);object-fit:contain}.external-player{display:flex;min-height:180px;align-items:center;justify-content:center;gap:14px;padding:24px;background:linear-gradient(125deg,var(--subject-soft),var(--subject-soft));text-align:left}.external-player>span{display:grid;width:48px;height:48px;place-items:center;border-radius:15px;background:var(--subject-strong);color:#fff;font-size:24px}.external-player strong{color:var(--subject-ink);font-size:14px}.external-player p{margin:5px 0 0;color:var(--subject-primary);font-size:12px}.player-caption{display:flex;align-items:center;justify-content:space-between;padding:12px 16px;color:var(--subject-primary);font-size:10px;font-weight:750;letter-spacing:.09em}.player-caption a{color:var(--subject-primary);text-decoration:none;font-size:12px;letter-spacing:0}.player-caption a:hover{text-decoration:underline}.no-video{display:flex;align-items:center;gap:13px;padding:19px 21px;background:linear-gradient(110deg,var(--subject-soft),#fff)}.no-video>span{display:grid;flex:0 0 43px;height:43px;place-items:center;border-radius:13px;background:var(--subject-tint);font-size:22px}.no-video strong{color:var(--subject-primary);font-size:14px}.no-video p{margin:4px 0 0;color:var(--subject-muted);font-size:12px}.lesson-content-card{padding:21px 23px}.content-title{display:flex;align-items:center;gap:9px;padding-bottom:13px;border-bottom:1px solid #f0edf3}.content-title>span{color:var(--subject-muted);font-size:20px}.content-title h2{margin:0;color:var(--subject-ink);font-size:17px}.lesson-content{padding-top:15px;color:var(--subject-primary);font-size:14px;line-height:1.85;white-space:pre-line}.state-card{padding:45px 20px;color:var(--subject-primary);text-align:center}.state-card.error{color:#ae5050}
@media(max-width:560px){.video-heading{padding:18px}.book-icon{flex-basis:45px;height:45px;border-radius:14px;font-size:24px}.lesson-content-card{padding:17px}.player-caption{padding:10px 12px}}
</style>
