<template>
  <section class="login-page" :class="theme" @mousemove="handleMouseMove">
    <div class="glow" :style="glowStyle" aria-hidden="true"></div>
    <div class="ambient ambient-lilac" aria-hidden="true"></div><div class="ambient ambient-peach" aria-hidden="true"></div>
    <div class="login-shell">
      <div class="left-panel">
        <RouterLink to="/" class="login-brand"><span>✦</span><div><strong>HỌC VUI</strong><small>HỌC TỐT MỖI NGÀY</small></div></RouterLink>
        <div class="content">
          <div class="study-scene" aria-hidden="true"><div class="scene-orbit orbit-one"></div><div class="scene-orbit orbit-two"></div><span class="scene-spark spark-one">✦</span><span class="scene-spark spark-two">✧</span><div class="study-illustration">📚</div><span class="scene-note">Mỗi ngày, thêm một điều hay ✨</span></div>
          <div class="hero-copy"><span class="hero-badge">KHÔNG GIAN HỌC TẬP CỦA EM</span><h2>Học vui mỗi ngày,<br /><em>tự tin thêm mỗi bước.</em></h2><p>Khám phá kiến thức, luyện tập và chinh phục những thử thách mới cùng Học vui.</p></div>
          <div class="subject-chips"><span data-subject="toan">📐 Toán</span><span data-subject="tieng-anh">🌍 Tiếng Anh</span><span data-subject="tieng-viet">📚 Tiếng Việt</span></div>
        </div>
        <span class="left-caption">Dành cho học sinh lớp 5–12 · Học theo nhịp của em</span>
      </div>
      <div class="right-panel">
        <div class="login-appearance"><AppearancePicker /></div>
        <RouterLink to="/" class="home-link">← Về trang chủ</RouterLink>
        <button class="theme-toggle" type="button" :aria-label="theme === 'dark' ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'" @click="toggleTheme">{{ theme === 'dark' ? '☀' : '☾' }}</button>
        <div class="glass-card" :class="{ fadeOut: isSuccess }">
          <div class="welcome-icon" aria-hidden="true">✦</div><span class="form-eyebrow">CHÀO MỪNG EM TRỞ LẠI</span><h1>Đăng nhập</h1><p class="form-description">Tiếp tục hành trình học tập của em hôm nay.</p>
          <form @submit.prevent="handleLogin" novalidate>
            <div class="field-group"><label for="login-username">Email hoặc tên đăng nhập</label><input id="login-username" v-model="username" class="form-control" autocomplete="username" placeholder="Nhập email hoặc tên đăng nhập" :aria-invalid="Boolean(usernameError)" :aria-describedby="usernameError ? 'username-error' : undefined" /><small v-if="usernameError" id="username-error" class="field-error" role="alert">{{ usernameError }}</small></div>
            <div class="field-group"><label for="login-password">Mật khẩu</label><div class="password-field"><input id="login-password" :type="showPassword ? 'text' : 'password'" v-model="password" class="form-control" autocomplete="current-password" placeholder="Nhập mật khẩu của em" :aria-invalid="Boolean(passwordError)" :aria-describedby="passwordError ? 'password-error' : undefined" /><button type="button" class="toggle-password" :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'" :aria-pressed="showPassword" @click="showPassword = !showPassword"><svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" stroke="currentColor" stroke-width="1.6"/><circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="1.6"/><path v-if="showPassword" d="m4 3 16 18" stroke="currentColor" stroke-width="1.8"/></svg></button></div><small v-if="passwordError" id="password-error" class="field-error" role="alert">{{ passwordError }}</small></div>
            <div class="forgot-row"><span>Học vui, tiến bộ mỗi ngày</span><button type="button" class="forgot-link" :disabled="forgotLoading" @click="handleForgotPasswordClick">{{ forgotLoading ? 'Đang gửi link…' : 'Quên mật khẩu?' }}</button></div>
            <div v-if="error" class="form-alert danger" role="alert">{{ error }}</div>
            <button class="btn-login" type="submit" :disabled="loading"><span v-if="loading" class="spinner"></span>{{ loading ? 'Đang đăng nhập…' : 'Đăng nhập' }}<span v-if="!loading" aria-hidden="true">→</span></button>
            <div v-if="forgotMessage && !showForgotModal" class="form-alert success" role="status">{{ forgotMessage }}</div>
          </form>
          <div class="form-divider"><span>MỘT HÀNH TRÌNH MỚI ĐANG CHỜ EM</span></div><p class="register-prompt">Chưa có tài khoản? <RouterLink to="/register">Tạo tài khoản <span aria-hidden="true">↗</span></RouterLink></p>
        </div>
        <span class="login-footnote">✧ Một chút chăm chỉ, thêm nhiều tự tin.</span>
      </div>
    </div>
    <div v-if="showForgotModal" class="password-modal" @click.self="closeForgotPassword" @keydown.esc="closeForgotPassword">
      <section class="password-dialog" role="dialog" aria-modal="true" aria-labelledby="forgot-title"><button type="button" class="modal-close" aria-label="Đóng cửa sổ" @click="closeForgotPassword">×</button><span class="welcome-icon" aria-hidden="true">✉</span><h2 id="forgot-title">Quên mật khẩu</h2><p>Nhập email tài khoản để nhận liên kết đặt lại mật khẩu.</p><label for="forgot-email">Email tài khoản</label><input id="forgot-email" v-model.trim="forgotEmail" type="email" class="form-control" autocomplete="email" placeholder="email@example.com" @keyup.enter="submitForgotPassword" /><div v-if="forgotError" class="form-alert danger" role="alert">{{ forgotError }}</div><div v-if="forgotMessage" class="form-alert success" role="status">{{ forgotMessage }}</div><button type="button" class="btn-login" :disabled="forgotLoading" @click="submitForgotPassword">{{ forgotLoading ? 'Đang gửi…' : 'Gửi yêu cầu' }}</button></section>
    </div>
  </section>
</template>

<script setup>
    import { ref, onMounted, computed } from "vue"
    import { RouterLink, useRouter } from "vue-router"
    import AppearancePicker from "./quiz/ui/AppearancePicker.vue"
    import { useAuth } from "../composables/useAuth"
    import { requestPasswordResetApi } from "../services/authService"

    const router = useRouter()
    const { login, loading, error } = useAuth()

    const username = ref("")
    const password = ref("")
    const usernameError = ref("")
    const passwordError = ref("")
    const showPassword = ref(false)
    const isSuccess = ref(false)
    const showForgotModal = ref(false)
    const forgotEmail = ref("")
    const forgotLoading = ref(false)
    const forgotError = ref("")
    const forgotMessage = ref("")

    // ðŸŒ™ THEME
    const theme = ref(localStorage.getItem("theme") || "light")
    const toggleTheme = () => {
        theme.value = theme.value === "light" ? "dark" : "light"
        localStorage.setItem("theme", theme.value)
    }

    // ðŸ”¥ AUTO REDIRECT IF TOKEN EXISTS
    onMounted(() => {
        const token = localStorage.getItem("token")
        if (token) router.push("/")
    })

    // ðŸ’Ž GLOW EFFECT
    const x = ref(500)
    const y = ref(300)
    const handleMouseMove = (e) => {
        x.value = e.clientX
        y.value = e.clientY
    }
    const glowStyle = computed(() => ({
        background: `radial-gradient(circle at ${x.value}px ${y.value}px, rgba(167,139,210,0.12), transparent 45%)`
    }))

    // ✅ HANDLE LOGIN
    const handleLogin = async () => {
        usernameError.value = ""
        passwordError.value = ""

        if (!username.value) {
            usernameError.value = "Vui lòng nhập email hoặc tên đăng nhập"
            return
        }
        if (!password.value) {
            passwordError.value = "Vui lòng nhập mật khẩu"
            return
        }

        const loggedUser = await login(username.value, password.value)

        if (loggedUser) {
            isSuccess.value = true
            // ❌ Không router.push ở đây, đã xử lý trong useAuth
        }
    }

    const isEmail = (value) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(value || '').trim())

    const handleForgotPasswordClick = async () => {
        forgotError.value = ""
        forgotMessage.value = ""

        if (isEmail(username.value)) {
            forgotEmail.value = username.value.trim()
            await submitForgotPassword()
            return
        }

        openForgotPassword()
    }

    const openForgotPassword = () => {
        forgotEmail.value = username.value.includes("@") ? username.value : ""
        forgotError.value = ""
        forgotMessage.value = ""
        showForgotModal.value = true
    }

    const closeForgotPassword = () => {
        showForgotModal.value = false
    }

    const submitForgotPassword = async () => {
        forgotError.value = ""
        forgotMessage.value = ""

        if (!forgotEmail.value) {
            forgotError.value = "Vui lòng nhập email."
            return
        }

        try {
            forgotLoading.value = true
            const result = await requestPasswordResetApi({ email: forgotEmail.value })
            forgotMessage.value = result?.message || "Nếu email tồn tại, liên kết đặt lại mật khẩu sẽ được gửi về email."
        } catch (err) {
            forgotError.value = err.message || "Gửi yêu cầu quên mật khẩu thất bại."
        } finally {
            forgotLoading.value = false
        }
    }
</script>
<style scoped>
:global(body:has(.login-page)){background:#f8f6fc}
:global(body:has(.login-page)::before){display:none}
:global(.container-fluid:has(> .login-page)){padding:0}
.login-page{--login-ink:var(--subject-ink);--login-muted:var(--subject-muted);--login-line:var(--subject-line);--login-surface:#fff;--login-soft:var(--subject-soft);position:relative;isolation:isolate;display:grid;place-items:center;min-height:100dvh;width:100%;padding:36px 4vw;overflow:hidden;background:radial-gradient(ellipse at 6% 10%,#eee3fb,transparent 45%),radial-gradient(ellipse at 95% 90%,#ffebdf,transparent 40%),#f8f6fc;color:var(--login-ink);font-family:"Be Vietnam Pro","Segoe UI",sans-serif}
.glow{position:absolute;inset:0;pointer-events:none;z-index:-1}
.ambient{position:absolute;z-index:-1;width:280px;height:280px;border-radius:50%;filter:blur(50px);opacity:.55;pointer-events:none;animation:ambient-drift 14s ease-in-out infinite alternate}.ambient-lilac{top:-110px;right:10%;background:#ddc6f4}.ambient-peach{bottom:-100px;left:10%;background:#fbe1ca;animation-delay:-7s}
.login-shell{display:grid;grid-template-columns:1fr 1fr;min-height:650px;width:min(1080px,100%);overflow:hidden;border:1px solid #ffffffd9;border-radius:30px;background:var(--login-surface);box-shadow:0 24px 80px #63418412,0 3px 14px #63418406}
.left-panel{position:relative;isolation:isolate;display:flex;flex-direction:column;padding:32px 34px 24px;overflow:hidden;background:radial-gradient(circle at 15% 95%,#ffeedd99,transparent 42%),linear-gradient(145deg,var(--subject-soft),var(--subject-tint) 65%,var(--subject-soft));border-right:1px solid var(--login-line)}
.left-panel::after{content:'';position:absolute;z-index:-1;right:-145px;bottom:-195px;width:420px;height:420px;border:1px solid #ffffffb3;border-radius:50%;box-shadow:0 0 0 28px #ffffff30,0 0 0 60px #ffffff20;pointer-events:none}
.login-brand{display:flex;align-items:center;gap:10px;width:fit-content;color:#63408b;text-decoration:none}.login-brand>span{display:grid;place-items:center;width:39px;height:39px;border:1px solid #ffffffc9;border-radius:13px;background:#ffffff9c;color:#956ac0;font-size:24px;box-shadow:0 4px 14px #8054b00c}.login-brand strong,.login-brand small{display:block}.login-brand strong{font-size:15px;font-weight:750;letter-spacing:.06em}.login-brand small{margin-top:3px;color:#937aa6!important;font-size:10px;font-weight:700;letter-spacing:.13em}
.content{display:flex;flex:1;flex-direction:column;align-items:center;justify-content:center;padding:15px 0 24px;text-align:center}
.study-scene{position:relative;display:grid;place-items:center;width:270px;height:235px;margin:2px auto 15px}.scene-orbit{position:absolute;border:1px solid #ffffffc9;border-radius:50%;pointer-events:none}.orbit-one{width:196px;height:196px;background:linear-gradient(145deg,#ffffffa8,#e5d5f688);box-shadow:0 0 0 16px #ffffff24}.orbit-two{width:240px;height:218px;transform:rotate(-24deg);border-style:dashed;border-color:#cdb3e766}.study-illustration{position:relative;width:200px;font-size:102px;line-height:1;filter:drop-shadow(0 16px 13px #8054b01c);transform:rotate(-8deg);animation:book-float 5s ease-in-out infinite}.scene-spark{position:absolute;color:#b79bcf;animation:sparkle 4s ease-in-out infinite}.spark-one{top:25px;right:21px;font-size:31px}.spark-two{bottom:45px;left:10px;font-size:26px;animation-delay:-2s}.scene-note{position:absolute;right:-1px;bottom:10px;padding:10px 12px;border:1px solid #fff;border-radius:12px;background:#fffffff0;box-shadow:0 7px 18px #8054b00c;color:#896799;font-size:10px;font-weight:750;transform:rotate(-5deg)}
.hero-badge{color:var(--subject-muted);font-size:10px;font-weight:750;letter-spacing:.16em}.hero-copy h2{margin:12px 0;color:var(--subject-ink);font-size:clamp(25px,2.6vw,32px);line-height:1.4;font-weight:700;letter-spacing:-.04em}.hero-copy em{color:var(--subject-primary);font-style:normal}.hero-copy p{max-width:325px;margin:0 auto;color:var(--subject-muted);font-size:13px;line-height:1.85}.subject-chips{display:flex;flex-wrap:wrap;justify-content:center;gap:8px;margin-top:22px}.subject-chips span{display:inline-flex;align-items:center;gap:6px;padding:7px 10px;border:1px solid var(--subject-line);border-radius:9px;background:#ffffffb3;color:var(--subject-ink);font-size:11px;font-weight:750;box-shadow:0 3px 9px #8054b005}.left-caption{text-align:center;color:#9a87aa;font-size:10px;line-height:1.7}
.right-panel{position:relative;display:flex;align-items:center;justify-content:center;padding:74px 46px 62px;background:linear-gradient(145deg,var(--login-surface),var(--login-soft))}.home-link{position:absolute;top:29px;left:32px;color:var(--login-muted);font-size:12px;font-weight:700;text-decoration:none}.home-link:hover{color:var(--subject-strong)}.theme-toggle{position:absolute;top:22px;right:28px;display:grid;place-items:center;width:34px;height:34px;border:1px solid var(--login-line);border-radius:11px;background:var(--login-surface);color:#9877b4;font-size:20px;cursor:pointer;transition:background .2s,transform .2s}.theme-toggle:hover{background:var(--login-soft);transform:rotate(-10deg)}
.glass-card{width:100%;max-width:360px;transition:opacity .35s,transform .35s}.welcome-icon{display:grid;place-items:center;width:46px;height:46px;margin-bottom:20px;border:1px solid var(--login-line);border-radius:15px;background:linear-gradient(145deg,var(--login-surface),#eee5fb);color:#9d77bd;font-size:25px;box-shadow:0 5px 15px #8054b009}.form-eyebrow{color:var(--subject-muted);font-size:10px;font-weight:750;letter-spacing:.15em}.glass-card h1{margin:8px 0 9px;color:var(--login-ink);font-size:31px;font-weight:700;letter-spacing:-.045em}.form-description{margin:0 0 26px;color:var(--login-muted);font-size:12px;line-height:1.7}.field-group{margin-bottom:17px}.field-group label,.password-dialog label{display:block;margin-bottom:8px;color:var(--login-ink);font-size:12px;font-weight:750}
.login-page .form-control{min-height:46px;padding:12px 14px;border:1px solid var(--login-line)!important;border-radius:12px!important;background:var(--login-surface)!important;color:var(--login-ink)!important;font:inherit;font-size:13px;box-shadow:0 2px 5px #8054b003;transition:border-color .2s,box-shadow .2s}.login-page .form-control::placeholder{color:#9987a7;font-size:12px}.login-page .form-control:focus{border-color:#b692d1!important;box-shadow:0 0 0 4px #b692d11c!important}.login-page .form-control[aria-invalid=true]{border-color:#d8949c!important}.login-page .field-error{display:block;margin-top:7px;color:#b74d5e!important;font-size:11px;line-height:1.5}.password-field{position:relative}.password-field .form-control{padding-right:48px}.toggle-password{position:absolute;top:6px;right:6px;display:grid;place-items:center;width:34px;height:34px;border:0;border-radius:8px;background:transparent;color:#a18bb2;cursor:pointer}.toggle-password:hover{background:var(--login-soft)}.toggle-password svg{width:18px;height:18px}
.forgot-row{display:flex;align-items:center;justify-content:space-between;gap:10px;margin:-1px 0 22px}.forgot-row>span{color:#8a789b;font-size:10px}.forgot-link{padding:0;border:0;background:transparent;color:var(--subject-strong);font-size:11px;font-weight:750;cursor:pointer}.forgot-link:hover{text-decoration:underline}.btn-login{display:flex;align-items:center;justify-content:center;gap:10px;width:100%;min-height:46px;padding:12px 17px;border:1px solid var(--subject-strong);border-radius:12px;background:linear-gradient(120deg,var(--subject-primary),var(--subject-strong));color:#fff;font-size:13px;font-weight:700;box-shadow:0 7px 18px #8054b024,inset 0 1px 0 #ffffff30;cursor:pointer;transition:transform .2s,box-shadow .2s}.btn-login>span:last-child:not(.spinner){margin-left:auto;font-size:19px}.btn-login:hover:not(:disabled){transform:translateY(-2px);box-shadow:0 10px 23px #8054b033}.btn-login:disabled{opacity:.65;cursor:wait}.form-divider{position:relative;margin:27px 0 17px;border-top:1px solid var(--login-line);text-align:center}.form-divider span{position:relative;top:-7px;padding:0 9px;background:var(--login-surface);color:#907a9f;font-size:10px;font-weight:750;letter-spacing:.06em}.register-prompt{margin:0;text-align:center;color:var(--login-muted);font-size:12px}.register-prompt a{margin-left:5px;color:var(--subject-strong);font-weight:700;text-decoration:none}.register-prompt a:hover{text-decoration:underline}.login-footnote{position:absolute;bottom:25px;left:20px;right:20px;color:#8d789c;text-align:center;font-size:10px}
.form-alert{margin:12px 0;padding:10px 12px;border:1px solid;border-radius:10px;font-size:12px;line-height:1.6}.form-alert.danger{border-color:#f0d2d7;background:#fff3f5;color:#a64457}.form-alert.success{border-color:#cde6d8;background:#f0faf4;color:#387453}.password-modal{position:fixed;inset:0;z-index:100;display:grid;place-items:center;padding:20px;background:#44306455;backdrop-filter:blur(8px)}.password-dialog{position:relative;width:min(420px,100%);padding:30px;border:1px solid var(--login-line);border-radius:23px;background:var(--login-surface);box-shadow:0 24px 80px #44306424}.password-dialog h2{margin:0 0 12px;color:var(--login-ink);font-size:23px;font-weight:700}.password-dialog p{margin:0 0 20px;color:var(--login-muted);font-size:13px;line-height:1.7}.password-dialog>.btn-login{margin-top:18px}.modal-close{position:absolute;top:15px;right:15px;width:30px;height:30px;border:1px solid var(--login-line);border-radius:10px;background:var(--login-soft);color:var(--login-muted);font-size:22px;cursor:pointer}.spinner{display:inline-block;width:15px;height:15px;border:2px solid #fff;border-top-color:transparent;border-radius:50%;animation:spin .8s linear infinite}.fadeOut{opacity:0;transform:translateY(-3px)}
.login-page a:focus-visible,.login-page button:focus-visible{outline:3px solid #c0a0d9;outline-offset:4px}
.dark{--login-ink:#eee5f6;--login-muted:#b7a6c9;--login-line:#4d3d63;--login-surface:#30263f;--login-soft:#352b45;background:radial-gradient(ellipse at 10% 10%,#4d3764,transparent 50%),#251e31}.dark .left-panel{background:linear-gradient(145deg,#453356,#392e4c);border-color:var(--login-line)}.dark .hero-copy h2{color:#ede3f6}.dark .hero-copy em{color:#c7a4e6}.dark .hero-copy p,.dark .left-caption{color:#b4a0c7}.dark .login-brand{color:#e0c8f2}.dark .subject-chips span{background:#ffffffd9}.dark .form-divider span{background:var(--login-surface)}.dark .register-prompt a,.dark .forgot-link{color:#c6a1e2}
@keyframes ambient-drift{to{transform:translate(35px,25px)}}@keyframes book-float{50%{transform:translateY(-3px) rotate(-4deg)}}@keyframes sparkle{50%{opacity:.45;transform:scale(.85)}}@keyframes spin{to{transform:rotate(360deg)}}
@media(max-width:900px){.login-page{padding:24px}.login-shell{min-height:610px}.left-panel{padding:27px 22px}.right-panel{padding:74px 28px 60px}.hero-copy h2{font-size:27px}.study-scene{transform:scale(.9);margin-top:-12px;margin-bottom:0}.hero-copy p{font-size:12px}.subject-chips{gap:5px}.subject-chips span{display:inline-flex;align-items:center;gap:6px;padding:6px 8px;font-size:10px}}
@media(max-width:680px){.login-page{padding:20px 14px;align-items:center}.login-shell{display:flex;flex-direction:column;max-width:440px;min-height:0;border-radius:24px}.left-panel{min-height:0;padding:18px 22px;border-right:0;border-bottom:1px solid var(--login-line)}.login-brand>span{width:33px;height:33px;border-radius:11px;font-size:21px}.login-brand strong{font-size:14px}.login-brand small{font-size:10px}.content,.left-caption{display:none}.right-panel{padding:75px 25px 57px}.glass-card{max-width:none}.home-link{top:25px;left:25px}.theme-toggle{top:19px;right:23px}.welcome-icon{width:41px;height:41px;margin-bottom:15px;border-radius:13px;font-size:23px}.glass-card h1{font-size:28px}.form-description{margin-bottom:23px}.forgot-row>span{font-size:10px}.form-divider span{font-size:10px}.password-dialog{padding:25px}.ambient{width:200px;height:200px}}
@media(prefers-reduced-motion:reduce){.ambient,.study-illustration,.scene-spark{animation:none}.glass-card,.btn-login,.theme-toggle{transition:none}.btn-login:hover:not(:disabled){transform:none}}
.login-appearance{position:absolute;top:21px;right:76px;z-index:5}
@media(max-width:680px){.login-appearance{top:18px;right:70px}}
</style>
