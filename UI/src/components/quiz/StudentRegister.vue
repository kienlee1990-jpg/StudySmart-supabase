<template>
  <QuizLayout>
    <section class="register-card">
      <div class="register-icon">🎒</div><span class="eyebrow">GÓC HỌC TẬP CỦA EM</span>
      <h1>Tạo tài khoản học sinh</h1><p>Lưu điểm số và xem lại những bài em đã làm.</p>
      <form @submit.prevent="submit">
        <label>Họ và tên<input v-model.trim="form.fullName" required maxlength="200" placeholder="Tên của em" /></label>
        <label>Email<input v-model.trim="form.email" required type="email" maxlength="256" placeholder="email@example.com" /></label>
        <label>Mật khẩu<input v-model="form.password" required type="password" minlength="6" maxlength="100" placeholder="Ít nhất 6 ký tự" /></label>
        <div v-if="error" class="error-message">{{ error }}</div><div v-if="success" class="success-message">{{ success }}</div>
        <button :disabled="loading">{{ loading ? 'Đang tạo tài khoản…' : 'Tạo tài khoản' }}</button>
      </form>
      <div class="register-links"><span>Đã có tài khoản?</span> <RouterLink to="/login">Đăng nhập</RouterLink><span class="divider">·</span><RouterLink to="/">Làm khách</RouterLink></div>
    </section>
  </QuizLayout>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import httpClient from '../../services/httpClient'

const router = useRouter()
const form = reactive({ fullName: '', email: '', password: '' })
const loading = ref(false)
const error = ref('')
const success = ref('')
const submit = async () => {
  loading.value = true; error.value = ''; success.value = ''
  try {
    await httpClient.post('/auth/register-student', form)
    success.value = 'Tạo tài khoản thành công. Mời em đăng nhập.'
    setTimeout(() => router.push('/login'), 900)
  } catch (err) {
    error.value = err?.response?.data?.message || 'Tạo tài khoản chưa thành công. Em kiểm tra thông tin rồi thử lại.'
  } finally { loading.value = false }
}
</script>

<style scoped>
.register-card{width:min(480px,100%);margin:16px auto;background:#fff;border:1px solid #e7ebf4;border-radius:25px;padding:30px;box-shadow:0 15px 40px #2534660b;text-align:center}.register-icon{font-size:46px}.eyebrow{font-size:11px;letter-spacing:.13em;color:#7780d9;font-weight:900}.register-card h1{font-size:25px;color:#2e3c68;margin:9px 0}.register-card>p{color:#7d869d;margin-bottom:23px}.register-card form{text-align:left}.register-card label{display:block;margin:13px 0;color:#66718d;font-weight:800;font-size:12px}.register-card input{width:100%;display:block;margin-top:6px;padding:12px;border:1px solid #e2e6f0;border-radius:12px;font-size:14px}.register-card button{width:100%;border:0;padding:13px;border-radius:13px;background:linear-gradient(135deg,#6376e8,#8865dc);color:white;font-weight:900;margin-top:8px}.register-links{margin-top:19px;font-size:13px;color:#828aa0}.register-links a{color:#5d70d8;font-weight:800;text-decoration:none;margin-left:5px}.divider{margin:0 5px}.error-message,.success-message{padding:10px;border-radius:10px;font-size:13px}.error-message{background:#fff0f0;color:#b04d54}.success-message{background:#eaf8f0;color:#32895e}
</style>
