import { ref } from 'vue'

const revision = ref(0)
export const notifySessionChanged = () => { revision.value++ }
if (typeof window !== 'undefined') window.addEventListener('storage', notifySessionChanged)

// This only controls presentation; the API validates the signed token.
export const hasAccountSession = () => {
  revision.value
  try {
    const token = localStorage.getItem('token')
    if (!token) return false
    const parts = token.split('.')
    if (parts.length !== 3) return false
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')))
    return Number.isFinite(payload.exp) && payload.exp * 1000 > Date.now()
  } catch { return false }
}

export const requiresAccountMode = mode => mode === 'Tower' || mode === 'ExamRoom'
export const safeReturnPath = path => typeof path === 'string' && path.startsWith('/') && !path.startsWith('//') && !path.includes('\\') && !path.startsWith('/login') ? path : '/'
