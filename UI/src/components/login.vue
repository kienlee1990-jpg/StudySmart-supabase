<template>
    <section class="login-page" :class="theme" @mousemove="handleMouseMove">

        <!-- ðŸ’Ž GLOW -->
        <div class="glow" :style="glowStyle"></div>

        <!-- LEFT PANEL -->
        <div class="left-panel">
            <div class="overlay"></div>
            <div class="content">
                <div class="study-illustration" aria-hidden="true">📚✦</div>
                <div class="hero-copy">
                    <h2>Học vui mỗi ngày</h2>
                    <p>
                        Cùng khám phá Toán, Tiếng Anh và Tiếng Việt dành cho lớp 5–12.
                    </p>
                </div>
            </div>
        </div>

        <!-- RIGHT PANEL -->
        <div class="right-panel">
            <div class="glass-card" :class="{ fadeOut: isSuccess }">

                <!-- ðŸŒ™ THEME TOGGLE -->
                <div class="theme-toggle" @click="toggleTheme">
                    {{ theme === 'dark' ? '🌙' : '☀️' }}
                </div>

                <h3 class="mb-4">Đăng nhập</h3>

                <form @submit.prevent="handleLogin">

                    <!-- USERNAME -->
                    <div class="mb-3">
                        <input v-model="username" class="form-control" placeholder="Email hoặc tên đăng nhập" />
                        <small class="text-danger">{{ usernameError }}</small>
                    </div>

                    <!-- PASSWORD -->
                    <div class="mb-3 position-relative">
                        <input :type="showPassword ? 'text' : 'password'" v-model="password" class="form-control"
                            placeholder="Mật khẩu" />
                        <span class="toggle-password" @click="showPassword = !showPassword">
                            <i :class="showPassword ? 'fa fa-eye-slash' : 'fa fa-eye'"></i>
                        </span>
                        <small class="text-danger">{{ passwordError }}</small>
                    </div>

                    <!-- ERROR MESSAGE -->
                    <div v-if="error" class="alert alert-danger py-2">
                        {{ error }}
                    </div>

                    <!-- LOGIN BUTTON -->
                    <button class="btn-login" type="submit" :disabled="loading">
                        <span v-if="loading" class="spinner"></span>
                        {{ loading ? "Đang đăng nhập..." : "Đăng nhập" }}
                    </button>

                    <button type="button" class="forgot-link" :disabled="forgotLoading" @click="handleForgotPasswordClick">
                        {{ forgotLoading ? "Đang gửi link..." : "Quên mật khẩu?" }}
                    </button>
                    <p class="register-prompt">Chưa có tài khoản học sinh? <RouterLink to="/register">Tạo tài khoản</RouterLink></p>

                    <div v-if="forgotMessage && !showForgotModal" class="alert alert-success py-2 mt-3">
                        {{ forgotMessage }}
                    </div>

                </form>
            </div>
        </div>

        <div v-if="showForgotModal" class="password-modal">
            <div class="password-dialog">
                <button type="button" class="modal-close" @click="closeForgotPassword">×</button>
                <h4>Quên mật khẩu</h4>
                <p>Nhập email tài khoản, hệ thống sẽ gửi liên kết đặt lại mật khẩu về email đó.</p>

                <input
                    v-model.trim="forgotEmail"
                    type="email"
                    class="form-control"
                    placeholder="Email tài khoản"
                    @keyup.enter="submitForgotPassword"
                />

                <div v-if="forgotError" class="alert alert-danger py-2">{{ forgotError }}</div>
                <div v-if="forgotMessage" class="alert alert-success py-2">{{ forgotMessage }}</div>

                <button type="button" class="btn-login" :disabled="forgotLoading" @click="submitForgotPassword">
                    {{ forgotLoading ? "Đang gửi..." : "Gửi yêu cầu" }}
                </button>
            </div>
        </div>

    </section>
</template>

<script setup>
    import { ref, onMounted, computed } from "vue"
    import { RouterLink, useRouter } from "vue-router"
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
    const x = ref(0)
    const y = ref(0)
    const handleMouseMove = (e) => {
        x.value = e.clientX
        y.value = e.clientY
    }
    const glowStyle = computed(() => ({
        background: `radial-gradient(circle at ${x.value}px ${y.value}px, rgba(99,102,241,0.25), transparent 40%)`
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
    html,
    body {
        margin: 0;
        height: 100%;
    }

    /* PAGE */
    .login-page {
        display: flex;
        min-height: 100vh;
        width: 70%;
        position: absolute;
        left: 15%;
        overflow: hidden;
    }

    /* ðŸ’Ž GLOW */
    .glow {
        position: absolute;
        inset: 0;
        pointer-events: none;
        transition: 0.1s;
    }

    /* LEFT PANEL */
    .left-panel {
        flex: 1;
        background:
            radial-gradient(circle at top, rgba(255, 215, 120, 0.18), transparent 36%),
            linear-gradient(160deg, #08214a 0%, #0b3d7a 52%, #061731 100%);
        position: relative;
        display: flex;
        align-items: center;
        justify-content: center;
        color: white;
    }

    .content {
        z-index: 2;
        text-align: center;
        width: min(100%, 520px);
        padding: 40px;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 28px;
    }

    .overlay {
        position: absolute;
        inset: 0;
        background:
            linear-gradient(180deg, rgba(5, 16, 38, 0.15), rgba(5, 16, 38, 0.35)),
            radial-gradient(circle at 20% 20%, rgba(255, 232, 163, 0.16), transparent 22%);
    }

    .study-illustration {
        width: min(100%, 420px);
        min-height: 230px;
        display: grid;
        place-items: center;
        font-size: clamp(90px, 12vw, 160px);
        filter: drop-shadow(0 24px 40px rgba(0, 0, 0, 0.22));
    }

    .hero-copy {
        max-width: 420px;
    }

    .hero-badge {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 8px 16px;
        border-radius: 999px;
        background: rgba(255, 214, 102, 0.18);
        border: 1px solid rgba(255, 214, 102, 0.38);
        color: #fbe39c;
        font-size: 0.82rem;
        font-weight: 700;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        margin-bottom: 18px;
    }

    .hero-copy h2 {
        margin: 0 0 14px;
        font-size: 2rem;
        font-weight: 800;
        line-height: 1.25;
        color: #ffffff;
    }

    .hero-copy p {
        margin: 0;
        color: rgba(255, 255, 255, 0.84);
        font-size: 1rem;
        line-height: 1.7;
    }

    /* RIGHT PANEL */
    .right-panel {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    /* CARD */
    .glass-card {
        width: 100%;
        max-width: 360px;
        padding: 40px;
        border-radius: 20px;
        backdrop-filter: blur(12px);
        transition: 0.4s;
        position: relative;
    }

    /* LIGHT THEME */
    .light .right-panel {
        background: linear-gradient(135deg, #f8fafc, #e2e8f0);
    }

    .light .glass-card {
        background: rgba(255, 255, 255, 0.7);
        color: #1e293b;
    }

    /* DARK THEME */
    .dark .right-panel {
        background: #0f172a;
    }

    .dark .glass-card {
        background: rgba(255, 255, 255, 0.05);
        color: white;
    }

    /* INPUT */
    .form-control {
        width: 100%;
        padding: 12px;
        border-radius: 10px;
        border: none;
        margin-bottom: 10px;
    }

    /* BUTTON */
    .btn-login {
        width: 100%;
        padding: 12px;
        border-radius: 10px;
        background: linear-gradient(135deg, #4f46e5, #6366f1);
        color: white;
        border: none;
    }

    .forgot-link {
        width: 100%;
        margin-top: 14px;
        border: none;
        background: transparent;
        color: #2563eb;
        font-weight: 700;
    }

    .register-prompt {
        margin: 10px 0 0;
        text-align: center;
        color: #667085;
        font-size: 13px;
    }

    .register-prompt a {
        color: #4f46e5;
        font-weight: 800;
        text-decoration: none;
    }

    .password-modal {
        position: fixed;
        inset: 0;
        z-index: 20;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 20px;
        background: rgba(15, 23, 42, .5);
    }

    .password-dialog {
        width: 100%;
        max-width: 420px;
        position: relative;
        padding: 30px;
        border-radius: 18px;
        background: #fff;
        box-shadow: 0 24px 70px rgba(15, 23, 42, .24);
    }

    .password-dialog h4 {
        margin-bottom: 10px;
        color: #0f172a;
        font-weight: 800;
    }

    .password-dialog p {
        margin-bottom: 18px;
        color: #64748b;
        line-height: 1.6;
    }

    .modal-close {
        position: absolute;
        top: 12px;
        right: 14px;
        width: 32px;
        height: 32px;
        border: none;
        border-radius: 999px;
        background: #f1f5f9;
        color: #0f172a;
        font-size: 20px;
        line-height: 1;
    }

    /* TOGGLE PASSWORD */
    .toggle-password {
        position: absolute;
        right: 12px;
        top: 50%;
        transform: translateY(-50%);
        cursor: pointer;
        color: #64748b;
    }

    /* SPINNER */
    .spinner {
        width: 16px;
        height: 16px;
        border: 2px solid white;
        border-top: 2px solid transparent;
        border-radius: 50%;
        display: inline-block;
        animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
        to {
            transform: rotate(360deg);
        }
    }

    /* FADE ANIMATION */
    .fadeOut {
        opacity: 0;
        transform: scale(0.95);
    }

    /* THEME TOGGLE */
    .theme-toggle {
        position: absolute;
        top: 15px;
        right: 20px;
        cursor: pointer;
    }

    /* RESPONSIVE */
    @media (max-width: 768px) {
        .left-panel {
            display: none;
        }

        .login-page {
            width: 100%;
            left: 0;
        }
    }
</style>
