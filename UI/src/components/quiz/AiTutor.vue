<template>
  <div class="ai-widget">
    <Transition name="chat-pop">
      <section v-if="open" class="chat-panel" aria-label="Trò chuyện với Gia sư AI">
        <header class="chat-toolbar">
          <div class="tutor-avatar">✦</div>
          <div class="tutor-status"><strong>Gia sư Học vui</strong><span><i></i> Sẵn sàng giúp em học</span></div>
          <button class="icon-button" aria-label="Làm mới cuộc trò chuyện" title="Làm mới" @click="resetChat">↻</button>
          <button class="icon-button close-button" aria-label="Đóng chatbot" @click="open = false">×</button>
        </header>
        <div class="chat-filters"><label>Lớp<select v-model.number="grade"><option v-for="item in grades" :key="item" :value="item">Lớp {{ item }}</option></select></label><label>Môn<select v-model="subject"><option v-for="item in subjects" :key="item">{{ item }}</option></select></label></div>
        <div ref="thread" class="chat-thread" aria-live="polite">
          <article v-for="message in messages" :key="message.id" class="chat-message" :class="message.role"><div v-if="message.role === 'assistant'" class="message-avatar">✦</div><div class="message-bubble"><span v-if="message.role === 'assistant'" class="message-name">Gia sư Học vui</span><p v-if="message.role === 'user'">{{ message.content }}</p><div v-else class="formatted-content" v-html="formatMessage(message.content)"></div></div></article>
          <article v-if="loading" class="chat-message assistant"><div class="message-avatar">✦</div><div class="message-bubble"><span class="message-name">Gia sư Học vui</span><p class="typing"><i></i><i></i><i></i></p></div></article>
          <div v-if="error" class="chat-error" role="alert">{{ error }}</div>
        </div>
        <div class="suggestions"><span>Gợi ý hỏi bài</span><button v-for="prompt in suggestions" :key="prompt" :disabled="loading" @click="draft = prompt">{{ prompt }}</button></div>
        <form class="composer" @submit.prevent="sendMessage()"><textarea v-model="draft" rows="2" maxlength="2000" placeholder="Gõ câu hỏi của em…" :disabled="loading" @keydown.enter.exact.prevent="sendFromKeyboard"></textarea><button type="submit" aria-label="Gửi câu hỏi" :disabled="loading || !draft.trim()">{{ loading ? '…' : '➤' }}</button></form>
        <p class="privacy-note">Đừng gửi thông tin riêng tư nhé.</p>
      </section>
    </Transition>
    <button class="chat-launcher" :class="{ active: open }" :aria-expanded="open" aria-label="Mở Gia sư AI" @click="open = !open"><span v-if="!open">✦</span><span v-else>×</span><i v-if="!open">AI gia sư</i></button>
  </div>
</template>

<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import httpClient from '../../services/httpClient'

const route = useRoute()
const subjects = ['Toán', 'Tiếng Anh', 'Tiếng Việt']
const grades = Array.from({ length: 8 }, (_, index) => index + 5)
const subjectBySlug = { toan: 'Toán', 'tieng-anh': 'Tiếng Anh', 'tieng-viet': 'Tiếng Việt' }
const sessionKey = 'hoc-vui-ai-chat-session'
const readSession = () => {
  try {
    const stored = JSON.parse(sessionStorage.getItem(sessionKey) || 'null')
    if (!stored || !Array.isArray(stored.messages)) return null
    const messages = stored.messages.filter(message =>
      message && ['user', 'assistant'].includes(message.role) && typeof message.content === 'string'
    ).slice(-100)
    return { ...stored, messages }
  } catch { return null }
}
const restoredSession = readSession()
const routeGrade = Number(route.params.grade || route.query.grade)
const isValidGrade = value => Number.isInteger(Number(value)) && Number(value) >= 5 && Number(value) <= 12
const grade = ref(isValidGrade(restoredSession?.grade) ? Number(restoredSession.grade) : isValidGrade(routeGrade) ? routeGrade : 5)
const subject = ref(subjects.includes(restoredSession?.subject) ? restoredSession.subject : subjectBySlug[route.params.subjectSlug] || (subjects.includes(route.query.subject) ? route.query.subject : 'Toán'))
const open = ref(restoredSession?.open === true)
const welcomeMessage = () => ({ id: `welcome-${Date.now()}`, role: 'assistant', content: `Chào em! Cô/thầy có thể giúp em học ${subject.value} lớp ${grade.value}. Em đang muốn tìm hiểu điều gì?` })
const messages = ref(restoredSession?.messages.length ? restoredSession.messages : [welcomeMessage()])
const draft = ref(typeof restoredSession?.draft === 'string' ? restoredSession.draft : '')
const loading = ref(false)
const error = ref('')
const thread = ref(null)
const suggestions = computed(() => subject.value === 'Toán'
  ? ['Giải thích phân số nhé', 'Gợi ý cách giải bài này', 'Cho em một câu đố Toán']
  : subject.value === 'Tiếng Anh'
    ? ['Giải thích thì hiện tại đơn', 'Từ vựng về trường học', 'Cho em luyện tiếng Anh']
    : ['Giải thích chủ ngữ, vị ngữ', 'Gợi ý viết bài văn', 'Đố em một câu Tiếng Việt'])

const escapeHtml = value => value.replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]))
const readMathGroup = (text, start) => {
  if (text[start] !== '{') return null
  let depth = 0
  for (let index = start; index < text.length; index++) {
    if (text[index] === '{') depth++
    if (text[index] === '}' && --depth === 0) return { value: text.slice(start + 1, index), end: index + 1 }
  }
  return null
}
const renderMath = source => {
  let output = ''
  for (let index = 0; index < source.length;) {
    if (source.startsWith('\\frac', index)) {
      const numerator = readMathGroup(source, index + 5)
      const denominator = numerator && readMathGroup(source, numerator.end)
      if (numerator && denominator) {
        output += `<span class="math-fraction"><span>${renderMath(numerator.value)}</span><span>${renderMath(denominator.value)}</span></span>`
        index = denominator.end
        continue
      }
    }
    if (source[index] === '\\') {
      const command = source.slice(index + 1).match(/^[a-zA-Z]+/)?.[0]
      const symbols = { times: '×', cdot: '·', div: '÷', pm: '±', leq: '≤', geq: '≥', neq: '≠', pi: 'π' }
      if (command && symbols[command]) {
        output += symbols[command]
        index += command.length + 1
        continue
      }
    }
    output += source[index] === '{' || source[index] === '}' ? '' : source[index]
    index++
  }
  return output
}
const formatMessage = value => {
  const formatted = renderMath(escapeHtml(value).replace(/\$/g, ''))
  return formatted.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/^(?:\*|- )\s?/gm, '• ')
    .replace(/\n/g, '<br>')
}

watch(() => [route.params.subjectSlug, route.params.grade, route.query.subject, route.query.grade], () => {
  subject.value = subjectBySlug[route.params.subjectSlug] || (subjects.includes(route.query.subject) ? route.query.subject : subject.value)
  const nextGrade = Number(route.params.grade || route.query.grade)
  if (isValidGrade(nextGrade)) grade.value = nextGrade
})
watch([messages, loading], async () => {
  await nextTick()
  if (thread.value) thread.value.scrollTop = thread.value.scrollHeight
}, { deep: true })
watch([messages, grade, subject, open, draft], () => {
  try {
    sessionStorage.setItem(sessionKey, JSON.stringify({
      messages: messages.value.slice(-100), grade: grade.value, subject: subject.value,
      open: open.value, draft: draft.value
    }))
  } catch { /* Session storage may be unavailable or full. */ }
}, { deep: true })
const openChat = () => { open.value = true }
onMounted(() => window.addEventListener('open-ai-tutor', openChat))
onUnmounted(() => window.removeEventListener('open-ai-tutor', openChat))
const resetChat = () => {
  error.value = ''; draft.value = ''; messages.value = [welcomeMessage()]
}
const sendFromKeyboard = event => {
  if (loading.value) return
  const content = event.currentTarget.value.trim()
  event.currentTarget.value = ''
  draft.value = ''
  if (content) void sendMessage(content)
}
const sendMessage = async (submittedText = draft.value) => {
  const content = String(submittedText ?? '').trim()
  if (!content || loading.value) return
  error.value = ''; loading.value = true; draft.value = ''
  const userMessage = { id: `user-${Date.now()}`, role: 'user', content }
  messages.value.push(userMessage)
  const history = messages.value.slice(-8).map(({ role, content: text }) => ({ role, content: text }))
  while (history.length > 1 && history.reduce((sum, item) => sum + item.content.length, 0) > 6000) history.shift()
  try {
    const response = await httpClient.post('/ai/chat', { grade: grade.value, subject: subject.value, messages: history }, { timeout: 50000 })
    messages.value.push({ id: `assistant-${Date.now()}`, role: 'assistant', content: response.data.answer })
  } catch (err) {
    messages.value = messages.value.filter(message => message.id !== userMessage.id)
    error.value = err?.response?.data?.message || 'Chưa gửi được câu hỏi. Em thử lại nhé.'
  } finally { loading.value = false }
}
</script>

<style scoped>
.ai-widget{position:fixed;right:24px;bottom:22px;z-index:50;font-family:"Be Vietnam Pro","Segoe UI",sans-serif}.chat-launcher{position:relative;display:flex;align-items:center;justify-content:center;gap:9px;width:62px;height:62px;border:0;border-radius:50%;background:linear-gradient(145deg,var(--subject-strong),var(--subject-strong));color:#fff;font-size:28px;box-shadow:0 8px 25px color-mix(in srgb,var(--subject-primary) 29%,transparent);cursor:pointer}.chat-launcher i{position:absolute;right:68px;padding:8px 12px;border-radius:18px;background:var(--subject-deep);color:#fff;font-size:12px;font-style:normal;font-weight:700;white-space:nowrap;box-shadow:0 4px 12px color-mix(in srgb,var(--subject-primary) 12.5%,transparent)}.chat-launcher.active{background:var(--subject-deep);font-size:32px}.chat-panel{position:absolute;right:0;bottom:76px;display:flex;flex-direction:column;width:min(390px,calc(100vw - 28px));height:min(590px,calc(100vh - 115px));overflow:hidden;border:1px solid var(--subject-line);border-radius:18px;background:#fff;box-shadow:0 18px 60px color-mix(in srgb,var(--subject-primary) 25.1%,transparent)}.chat-toolbar{display:flex;align-items:center;gap:10px;padding:13px 14px;border-bottom:1px solid #efedf2;background:#fff}.tutor-avatar,.message-avatar{display:grid;place-items:center;flex:none;width:38px;height:38px;border-radius:13px;background:var(--subject-soft);color:var(--subject-primary);font-size:22px}.tutor-status{display:flex;flex:1;flex-direction:column;gap:4px}.tutor-status strong{color:var(--subject-ink);font-size:13px}.tutor-status span{color:var(--subject-muted);font-size:10px}.tutor-status i{display:inline-block;width:7px;height:7px;margin-right:4px;border-radius:50%;background:var(--subject-primary)}.icon-button{width:30px;height:30px;border:0;border-radius:9px;background:#f6f3f8;color:var(--subject-primary);font-size:19px;cursor:pointer}.close-button{font-size:23px}.chat-filters{display:flex;gap:9px;padding:8px 14px;border-bottom:1px solid #f1eef4}.chat-filters label{flex:1;color:var(--subject-primary);font-size:10px;font-weight:700}.chat-filters select{display:block;width:100%;margin-top:4px;padding:6px 8px;border:1px solid var(--subject-line);border-radius:8px;background:#fff;color:var(--subject-primary);font-size:11px}.chat-thread{flex:1;min-height:0;overflow-y:auto;padding:15px 12px;background:linear-gradient(180deg,#fcfbfd,#fff)}.chat-message{display:flex;align-items:flex-end;gap:7px;margin:0 0 11px}.chat-message.user{justify-content:flex-end}.message-avatar{width:25px;height:25px;border-radius:9px;font-size:14px}.message-bubble{max-width:84%;padding:9px 11px;border:1px solid var(--subject-line);border-radius:13px 13px 13px 4px;background:#fff;color:var(--subject-primary);box-shadow:0 3px 10px color-mix(in srgb,var(--subject-primary) 3.1%,transparent)}.chat-message.user .message-bubble{border-color:var(--subject-primary);border-radius:13px 13px 4px 13px;background:var(--subject-strong);color:white}.message-name{display:block;margin-bottom:4px;color:var(--subject-primary);font-size:10px;font-weight:750}.message-bubble p{margin:0;font-size:12px;line-height:1.65;white-space:pre-wrap;overflow-wrap:anywhere}.chat-message.user .message-bubble p{color:white}.typing{display:flex;gap:4px;align-items:center;height:15px}.typing i{width:5px;height:5px;border-radius:50%;background:var(--subject-primary);animation:blink 1s infinite alternate}.typing i:nth-child(2){animation-delay:.2s}.typing i:nth-child(3){animation-delay:.4s}@keyframes blink{to{opacity:.25;transform:translateY(-3px)}}.formatted-content{font-size:12px;line-height:1.65;white-space:normal;overflow-wrap:anywhere}.formatted-content :deep(strong){font-weight:700}.formatted-content :deep(.math-inline){display:inline-block;margin:1px 2px;font-family:"Cambria Math",Cambria,serif;font-size:1.08em}.formatted-content :deep(.math-fraction){display:inline-grid;grid-template-rows:auto auto;vertical-align:middle;text-align:center;line-height:1.15;margin:0 2px}.formatted-content :deep(.math-fraction) > span:first-child{padding:0 3px 2px}.formatted-content :deep(.math-fraction) > span:last-child{padding:2px 3px 0;border-top:1px solid currentColor}.chat-error{padding:8px 10px;border-radius:8px;background:#fff0f0;color:#a6444c;font-size:11px}.suggestions{display:flex;gap:5px;overflow-x:auto;padding:8px 10px;border-top:1px solid #f0edf3}.suggestions>span{display:none}.suggestions button{flex:none;border:1px solid var(--subject-line);border-radius:15px;padding:6px 8px;background:#fff;color:var(--subject-primary);font-size:10px;cursor:pointer}.suggestions button:disabled{opacity:.55}.composer{display:flex;align-items:end;gap:7px;padding:9px 10px 4px;background:#fff}.composer textarea{flex:1;resize:none;min-height:43px;max-height:100px;padding:9px 10px;border:1px solid var(--subject-line);border-radius:10px;color:var(--subject-ink);font:inherit;font-size:12px;line-height:1.5}.composer button{flex:none;width:40px;height:40px;border:0;border-radius:10px;background:var(--subject-strong);color:#fff;font-size:18px;font-weight:750;cursor:pointer}.composer button:disabled{opacity:.55;cursor:wait}.privacy-note{margin:0;padding:3px 12px 8px;color:var(--subject-muted);font-size:10px}.chat-pop-enter-active,.chat-pop-leave-active{transition:opacity .16s ease,transform .16s ease}.chat-pop-enter-from,.chat-pop-leave-to{opacity:0;transform:translateY(9px) scale(.98)}
@media(max-width:600px){.ai-widget{right:14px;bottom:14px}.chat-launcher{width:55px;height:55px}.chat-panel{bottom:66px;height:min(560px,calc(100dvh - 100px))}}
</style>
