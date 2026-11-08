<template>
    <section class="reset-page">
        <div class="reset-card">
            <h3>Đặt lại mật khẩu</h3>
            <p class="reset-copy">
                Nhập mật khẩu mới cho tài khoản của bạn.
            </p>

            <form @submit.prevent="submitResetPassword">
                <div class="mb-3">
                    <label class="form-label">Mật khẩu mới</label>
                    <input
                        v-model="newPassword"
                        type="password"
                        class="form-control"
                        placeholder="Nhập mật khẩu mới"
                        autocomplete="new-password"
                    />
                </div>

                <div class="mb-3">
                    <label class="form-label">Nhập lại mật khẩu mới</label>
                    <input
                        v-model="confirmPassword"
                        type="password"
                        class="form-control"
                        placeholder="Nhập lại mật khẩu mới"
                        autocomplete="new-password"
                    />
                </div>

                <div v-if="error" class="alert alert-danger py-2">{{ error }}</div>
                <div v-if="successMessage" class="alert alert-success py-2">{{ successMessage }}</div>

                <button class="btn-reset" type="submit" :disabled="loading || !canSubmit">
                    {{ loading ? "Đang xử lý..." : "Đặt lại mật khẩu" }}
                </button>

                <RouterLink class="back-link" to="/login">Quay lại đăng nhập</RouterLink>
            </form>
        </div>
    </section>
</template>

<script setup>
    import { computed, onMounted, ref } from 'vue'
    import { RouterLink, useRoute, useRouter } from 'vue-router'
    import { resetPasswordApi } from '../services/authService'

    const route = useRoute()
    const router = useRouter()
    const email = ref('')
    const token = ref('')
    const newPassword = ref('')
    const confirmPassword = ref('')
    const loading = ref(false)
    const error = ref('')
    const successMessage = ref('')

    const canSubmit = computed(() => !!token.value && !!email.value && !!newPassword.value && !!confirmPassword.value)

    onMounted(() => {
        email.value = String(route.query.email || '')
        token.value = String(route.query.token || '')

        if (!email.value || !token.value) {
            error.value = 'Liên kết đặt lại mật khẩu không hợp lệ hoặc đã thiếu thông tin.'
        }
    })

    const submitResetPassword = async () => {
        error.value = ''
        successMessage.value = ''

        if (!email.value || !token.value) {
            error.value = 'Liên kết đặt lại mật khẩu không hợp lệ hoặc đã thiếu thông tin.'
            return
        }

        if (!newPassword.value) {
            error.value = 'Vui lòng nhập mật khẩu mới.'
            return
        }

        if (newPassword.value !== confirmPassword.value) {
            error.value = 'Mật khẩu nhập lại không khớp.'
            return
        }

        try {
            loading.value = true
            await resetPasswordApi({
                email: email.value,
                token: token.value,
                newPassword: newPassword.value
            })
            successMessage.value = 'Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.'
            setTimeout(() => router.push('/login'), 1200)
        } catch (err) {
            error.value = err.message || 'Đặt lại mật khẩu thất bại.'
        } finally {
            loading.value = false
        }
    }
</script>

<style scoped>
    .reset-page {
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        background: linear-gradient(135deg, #f8fafc, #e2e8f0);
        padding: 24px;
    }

    .reset-card {
        width: 100%;
        max-width: 420px;
        padding: 34px;
        border-radius: 20px;
        background: rgba(255, 255, 255, 0.82);
        box-shadow: 0 24px 70px rgba(15, 23, 42, 0.16);
    }

    .reset-card h3 {
        margin-bottom: 8px;
        color: #0f172a;
        font-weight: 800;
    }

    .reset-copy {
        margin-bottom: 22px;
        color: #64748b;
    }

    .form-control {
        min-height: 44px;
        border-radius: 10px;
    }

    .btn-reset {
        width: 100%;
        padding: 12px;
        border-radius: 10px;
        border: none;
        background: linear-gradient(135deg, #2563eb, #4f46e5);
        color: white;
        font-weight: 700;
    }

    .btn-reset:disabled {
        opacity: .7;
    }

    .back-link {
        display: block;
        margin-top: 16px;
        text-align: center;
        color: #2563eb;
        text-decoration: none;
        font-weight: 600;
    }
</style>
