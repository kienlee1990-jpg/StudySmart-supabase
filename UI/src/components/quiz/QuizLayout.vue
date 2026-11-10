<template>
  <div class="quiz-app" :data-subject="subjectTheme">
    <div class="notice-strip"><div class="site-width"><span>✦</span> Góc học tập trực tuyến dành cho học sinh lớp 5–12</div></div>
    <header class="site-header">
      <div class="site-width header-main">
        <RouterLink to="/" class="quiz-brand"><span class="brand-mark">✦</span><span class="brand-copy"><strong>HỌC VUI</strong><small>HỌC TỐT MỖI NGÀY</small></span></RouterLink>
        <nav class="desktop-nav" aria-label="Điều hướng chính">
          <RouterLink to="/" :class="{ 'nav-current': route.path === '/' && !route.hash }">Trang chủ</RouterLink>
          <RouterLink to="/#chon-mon" :class="{ 'nav-current': route.hash === '#chon-mon' || Boolean(route.params.subjectSlug) }">Môn học</RouterLink>
          <RouterLink v-if="canManageQuizContent" to="/quiz-admin">Quản trị nội dung</RouterLink>
          <RouterLink v-if="canManageUsers" to="/users">Quản lý tài khoản</RouterLink>
        </nav>
        <div class="account-actions">
          <span v-if="!isAuthenticated" class="guest-badge">Khách</span>
          <AppearancePicker />
          <RouterLink v-if="!isAuthenticated" to="/register" class="register-link">Tạo tài khoản</RouterLink>
          <RouterLink v-if="!isAuthenticated" to="/login" class="account-button">Đăng nhập</RouterLink>
          <button v-else class="account-button" @click="logout">Đăng xuất</button>
          <button class="menu-toggle" :aria-expanded="menuOpen" aria-label="Mở menu" @click="menuOpen = !menuOpen">{{ menuOpen ? '×' : '☰' }}</button>
        </div>
      </div>
      <nav v-if="menuOpen" class="mobile-nav" aria-label="Điều hướng di động">
        <RouterLink to="/" @click="menuOpen = false">Trang chủ · Môn học</RouterLink><RouterLink v-if="canManageQuizContent" to="/quiz-admin" @click="menuOpen = false">Quản trị nội dung</RouterLink><RouterLink v-if="canManageUsers" to="/users" @click="menuOpen = false">Quản lý tài khoản</RouterLink>
      </nav>
    </header>
    <main class="quiz-main"><slot /></main>
    <ContactFooter />
    <AiTutor />
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { getSubjectTheme } from '../../utils/subjectTheme'
import { RouterLink } from 'vue-router'
import AiTutor from './AiTutor.vue'
import ContactFooter from './ContactFooter.vue'
import AppearancePicker from './ui/AppearancePicker.vue'
import { useAuth } from '../../composables/useAuth'
import { canAccessPermission, getStoredUserPermissions, getStoredUserProfile } from '../../utils/accessControl'

const props = defineProps({ subject: { type: String, default: '' } })
const route = useRoute()
const subjectTheme = computed(() => getSubjectTheme(props.subject || route.params.subjectSlug || route.query.subject))
const auth = useAuth()
const menuOpen = ref(false)
const isAuthenticated = computed(() => auth.isAuthenticated())
const canManageQuizContent = computed(() => isAuthenticated.value && canAccessPermission(getStoredUserPermissions(), 'ManageQuizContent', getStoredUserProfile()))
const canManageUsers = computed(() => isAuthenticated.value && canAccessPermission(getStoredUserPermissions(), 'ManageUsers', getStoredUserProfile()))
const logout = () => auth.logout()
</script>

<style scoped>
.guest-badge{padding:6px 9px;border:1px solid var(--subject-line);border-radius:20px;background:var(--subject-soft);color:var(--subject-ink);font-size:11px;font-weight:700}
@media(max-width:420px){.guest-badge{padding:4px;font-size:10px}.account-actions{gap:5px}}
.quiz-app{--ink:var(--subject-ink);--muted:var(--subject-muted);--purple:var(--subject-strong);--orange:var(--subject-muted);--line:var(--subject-line);min-height:100vh;background:#f6f6f8;color:var(--ink);font-family:"Be Vietnam Pro","Segoe UI",sans-serif}.site-width{width:min(1180px,92vw);margin:auto}.notice-strip{height:32px;background:var(--subject-soft);color:var(--subject-muted);font-size:11px;border-bottom:1px solid var(--subject-line)}.notice-strip .site-width{height:100%;display:flex;align-items:center;gap:8px}.notice-strip>div>span{color:var(--subject-muted)}.site-header{position:sticky;top:0;z-index:10;background:#fff;box-shadow:none;border-bottom:1px solid var(--subject-line)}.header-main{height:80px;display:flex;align-items:center;gap:40px}.quiz-brand{display:flex;align-items:center;gap:10px;min-width:180px;text-decoration:none;color:var(--ink)}.brand-mark{display:grid;place-items:center;width:40px;height:40px;border-radius:13px;background:var(--subject-tint);color:var(--subject-primary);font-size:24px}.brand-copy strong,.brand-copy small{display:block}.brand-copy strong{font-size:18px;letter-spacing:.04em;color:var(--subject-ink)}.brand-copy small{margin-top:2px;font-size:9px;letter-spacing:.09em;color:var(--subject-muted);font-weight:700}.desktop-nav{display:flex;align-items:center;gap:30px;flex:1}.desktop-nav a,.mobile-nav a{font-size:13px;font-weight:600;color:var(--subject-muted);text-decoration:none}.desktop-nav a:hover,.desktop-nav a.nav-current{color:var(--subject-strong)}.account-actions{display:flex;align-items:center;gap:14px}.register-link{color:var(--subject-primary);text-decoration:none;font-size:13px;font-weight:700}.account-button{border:0;border-radius:11px;padding:10px 17px;background:var(--subject-strong);color:#fff;text-decoration:none;font-size:12px;font-weight:650;min-height:40px}.menu-toggle{display:none;border:0;background:var(--subject-soft);color:var(--subject-primary);border-radius:9px;width:38px;height:38px;font-size:20px}.mobile-nav{display:none}.quiz-main{width:min(1180px,92vw);margin:28px auto 48px;min-height:calc(100vh - 190px)}
@media(max-width:760px){.notice-strip{height:32px;min-height:34px;padding:6px 0;background:var(--subject-soft);font-size:11px;color:var(--subject-muted);border-bottom:1px solid var(--subject-line)}.notice-strip .site-width{font-size:11px}.header-main{height:80px;gap:10px}.quiz-brand{min-width:0}.desktop-nav,.register-link{display:none}.account-actions{margin-left:auto}.menu-toggle{display:block}.mobile-nav{display:grid;gap:0;padding:4px 4vw 12px;border-top:1px solid var(--line)}.mobile-nav a{padding:11px 0;border-bottom:1px solid #f0eff3}.quiz-main{margin-top:18px;margin:28px auto 48px}}
@media(max-width:760px){.account-actions{gap:8px}.header-main{gap:8px}.brand-mark{width:34px;height:34px}.brand-copy strong{font-size:16px}.brand-copy small{font-size:8px}.quiz-brand{gap:7px}.account-button{padding:10px 12px;font-size:11px}}
@media(min-width:761px) and (max-width:1100px){.header-main{gap:22px}.desktop-nav{gap:18px}.quiz-brand{min-width:160px}}
</style>
