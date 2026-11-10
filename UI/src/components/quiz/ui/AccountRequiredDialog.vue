<template>
  <dialog ref="dialog" class="account-dialog" :data-subject="subjectTheme" aria-labelledby="account-required-title" @click="closeOnBackdrop">
    <section class="account-dialog-body">
      <button class="dialog-close" type="button" aria-label="Đóng thông báo" @click="close">×</button>
      <span class="dialog-lock" aria-hidden="true">🔒</span>
      <h2 id="account-required-title">Đăng nhập để trải nghiệm tính năng này!</h2>
      <p>Cùng thử sức và chinh phục những thử thách mới nhé.</p>
      <div class="dialog-actions">
        <button type="button" class="dialog-later" autofocus @click="close">Để sau</button>
        <button type="button" class="dialog-login" @click="login">Đăng nhập <span aria-hidden="true">→</span></button>
      </div>
    </section>
  </dialog>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getSubjectTheme } from '../../../utils/subjectTheme'

const props = defineProps({ subject: { type: String, default: '' } })
const subjectTheme = computed(() => getSubjectTheme(props.subject))
const router = useRouter()
const dialog = ref(null)
let returnPath = '/'
const open = path => { returnPath = path; dialog.value?.showModal() }
const close = () => dialog.value?.close()
const closeOnBackdrop = event => { if (event.target === event.currentTarget) close() }
const login = () => { close(); router.push({ path: '/login', query: { redirect: returnPath } }) }
defineExpose({ open })
</script>

<style scoped>
.account-dialog{width:min(430px,calc(100vw - 32px));max-height:calc(100dvh - 40px);margin:auto;padding:0;overflow:auto;border:1px solid var(--subject-line);border-radius:26px;background:#fff;color:var(--subject-ink);box-shadow:0 24px 80px #251c4b33;font-family:"Be Vietnam Pro","Segoe UI",sans-serif}
.account-dialog::backdrop{background:#261d3e55;backdrop-filter:blur(5px)}
.account-dialog-body{position:relative;padding:34px 28px 26px;text-align:center;background:radial-gradient(ellipse at 50% 0,var(--subject-tint),transparent 65%),#fff}
.dialog-close{position:absolute;top:12px;right:12px;width:30px;height:30px;border:1px solid var(--subject-line);border-radius:10px;background:#fff;color:var(--subject-muted);font-size:22px;cursor:pointer}
.dialog-lock{display:grid;place-items:center;width:68px;height:68px;margin:0 auto 20px;border:1px solid #efd18a;border-radius:22px;background:linear-gradient(145deg,#fffdf2,#ffedb7);box-shadow:0 8px 22px #aa79181a;font-size:34px}
.account-dialog h2{margin:0;color:var(--subject-ink);font-size:23px;font-weight:750;line-height:1.5;letter-spacing:-.025em}
.account-dialog p{margin:12px 0 25px;color:var(--subject-muted);font-size:13px;line-height:1.7}
.dialog-actions{display:flex;gap:10px}.dialog-actions button{flex:1;min-height:44px;padding:10px 14px;border:1px solid var(--subject-line);border-radius:12px;font:inherit;font-size:13px;font-weight:700;cursor:pointer;transition:background .18s,box-shadow .18s}
.dialog-later{background:#fff;color:var(--subject-muted)}.dialog-later:hover{background:var(--subject-soft)}.dialog-actions .dialog-login{border-color:var(--subject-strong);background:var(--subject-strong);color:#fff}.dialog-login span{margin-left:10px}.dialog-login:hover{box-shadow:0 5px 15px color-mix(in srgb,var(--subject-primary) 25%,transparent)}
.account-dialog button:focus-visible{outline:3px solid var(--subject-primary);outline-offset:3px}
</style>
