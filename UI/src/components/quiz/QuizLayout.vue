<template>
  <div class="quiz-app">
    <div class="notice-strip"><div class="site-width"><span>✦</span> Góc học tập trực tuyến dành cho học sinh lớp 5–12 <RouterLink to="/ranking">Khám phá bảng xếp hạng <b>→</b></RouterLink></div></div>
    <header class="site-header">
      <div class="site-width header-main">
        <RouterLink to="/" class="quiz-brand"><span class="brand-mark">✦</span><span class="brand-copy"><strong>HỌC VUI</strong><small>HỌC TỐT MỖI NGÀY</small></span></RouterLink>
        <nav class="desktop-nav" aria-label="Điều hướng chính">
          <RouterLink to="/">Trang chủ</RouterLink>
          <RouterLink to="/">Môn học</RouterLink>
          <button class="nav-chat" @click="openTutor">AI gia sư</button>
          <RouterLink to="/ranking">Leo tháp</RouterLink>
          <RouterLink v-if="canManageQuizContent" to="/quiz-admin">Quản trị nội dung</RouterLink>
          <RouterLink v-if="canManageUsers" to="/users">Quản lý tài khoản</RouterLink>
        </nav>
        <div class="account-actions">
          <RouterLink v-if="!isAuthenticated" to="/register" class="register-link">Tạo tài khoản</RouterLink>
          <RouterLink v-if="!isAuthenticated" to="/login" class="account-button">Đăng nhập</RouterLink>
          <button v-else class="account-button" @click="logout">Đăng xuất</button>
          <button class="menu-toggle" :aria-expanded="menuOpen" aria-label="Mở menu" @click="menuOpen = !menuOpen">{{ menuOpen ? '×' : '☰' }}</button>
        </div>
      </div>
      <nav v-if="menuOpen" class="mobile-nav" aria-label="Điều hướng di động">
        <RouterLink to="/" @click="menuOpen = false">Trang chủ · Môn học</RouterLink><button class="nav-chat" @click="openTutor">AI gia sư</button><RouterLink to="/ranking" @click="menuOpen = false">Leo tháp</RouterLink><RouterLink v-if="canManageQuizContent" to="/quiz-admin" @click="menuOpen = false">Quản trị nội dung</RouterLink><RouterLink v-if="canManageUsers" to="/users" @click="menuOpen = false">Quản lý tài khoản</RouterLink>
      </nav>
    </header>
    <main class="quiz-main"><slot /></main>
    <footer class="quiz-footer"><div class="footer-inner"><RouterLink to="/" class="footer-brand">✦ Học vui</RouterLink><span>Học tập · Kiểm tra · Phòng thi · AI gia sư · Leo tháp</span><small>Dành cho học sinh lớp 5 đến lớp 12</small></div></footer>
    <AiTutor />
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AiTutor from './AiTutor.vue'
import { useAuth } from '../../composables/useAuth'
import { canAccessPermission, getStoredUserPermissions, getStoredUserProfile } from '../../utils/accessControl'

const auth = useAuth()
const menuOpen = ref(false)
const isAuthenticated = computed(() => auth.isAuthenticated())
const canManageQuizContent = computed(() => canAccessPermission(getStoredUserPermissions(), 'ManageQuizContent', getStoredUserProfile()))
const canManageUsers = computed(() => canAccessPermission(getStoredUserPermissions(), 'ManageUsers', getStoredUserProfile()))
const logout = () => auth.logout()
const openTutor = () => { menuOpen.value = false; window.dispatchEvent(new Event('open-ai-tutor')) }
</script>

<style scoped>
.quiz-app{--ink:#28264d;--muted:#73758c;--purple:#6d32a8;--orange:#f4a340;--line:#e8e7ee;min-height:100vh;background:#f6f6f8;color:var(--ink);font-family:"Be Vietnam Pro","Segoe UI",sans-serif}.site-width{width:min(1180px,92vw);margin:auto}.notice-strip{height:36px;background:#272544;color:#fff;font-size:11px}.notice-strip .site-width{height:100%;display:flex;align-items:center;gap:8px}.notice-strip>div>span{color:#ffc55f}.notice-strip a{margin-left:auto;color:#ffcf84;text-decoration:none;font-weight:800}.notice-strip a b{font-size:15px;margin-left:4px}.site-header{position:sticky;top:0;z-index:10;background:#fff;box-shadow:0 3px 13px #211c3310}.header-main{height:76px;display:flex;align-items:center;gap:40px}.quiz-brand{display:flex;align-items:center;gap:10px;min-width:180px;text-decoration:none;color:var(--ink)}.brand-mark{display:grid;place-items:center;width:42px;height:42px;border-radius:13px;background:#fff0d8;color:#e38c27;font-size:26px}.brand-copy strong,.brand-copy small{display:block}.brand-copy strong{font-size:18px;letter-spacing:.04em;color:#3c296b}.brand-copy small{margin-top:2px;font-size:8px;letter-spacing:.13em;color:#88849b;font-weight:800}.desktop-nav{display:flex;align-items:center;gap:30px;flex:1}.desktop-nav a,.mobile-nav a,.nav-chat{font-size:13px;font-weight:800;color:#5b5874;text-decoration:none}.nav-chat{padding:0;border:0;background:transparent;font-family:inherit;cursor:pointer}.mobile-nav .nav-chat{padding:11px 0;border-bottom:1px solid #f0eff3;text-align:left}.desktop-nav a:hover,.desktop-nav a.router-link-active,.nav-chat:hover{color:#713caa}.account-actions{display:flex;align-items:center;gap:14px}.register-link{color:#6e45a1;text-decoration:none;font-size:12px;font-weight:800}.account-button{border:0;border-radius:9px;padding:10px 16px;background:#713ca6;color:#fff;text-decoration:none;font-size:12px;font-weight:900}.menu-toggle{display:none;border:0;background:#f4eff9;color:#65349b;border-radius:9px;width:38px;height:38px;font-size:20px}.mobile-nav{display:none}.quiz-main{width:min(1180px,92vw);margin:27px auto 56px;min-height:calc(100vh - 190px)}.quiz-footer{background:#fff;border-top:1px solid var(--line);padding:22px max(4vw,18px);color:#858398;font-size:11px}.footer-inner{width:min(1180px,92vw);margin:auto;display:flex;align-items:center;gap:20px}.footer-brand{font-size:14px;font-weight:900;color:#493171;text-decoration:none}.footer-inner small{margin-left:auto;color:#88869a!important}
@media(max-width:760px){.notice-strip{height:auto;min-height:34px;padding:6px 0}.notice-strip .site-width{font-size:10px}.notice-strip a{white-space:nowrap}.header-main{height:66px;gap:10px}.quiz-brand{min-width:0}.desktop-nav,.register-link{display:none}.account-actions{margin-left:auto}.menu-toggle{display:block}.mobile-nav{display:grid;gap:0;padding:4px 4vw 12px;border-top:1px solid var(--line)}.mobile-nav a{padding:11px 0;border-bottom:1px solid #f0eff3}.quiz-main{margin-top:18px}.footer-inner{flex-wrap:wrap;gap:8px 16px}.footer-inner small{margin-left:0;width:100%}}
</style>
