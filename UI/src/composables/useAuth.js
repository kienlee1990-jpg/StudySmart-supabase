import { ref } from "vue"
import { useRouter } from "vue-router"
import { getMe as getMeApi, loginApi, logoutApi } from "../services/authService"

export function useAuth() {
    const router = useRouter()
    const loading = ref(false)
    const error = ref("")
    const user = ref(null)

    const normalizeUser = (data) => ({
        id: data?.userId || data?.id || "",
        userId: data?.userId || data?.id || "",
        userName: data?.userName || "",
        email: data?.email || "",
        fullName: data?.fullName || "",
        roles: Array.isArray(data?.roles) ? data.roles : [],
        permissions: Array.isArray(data?.permissions) ? data.permissions : [],
        rolePermissions: Array.isArray(data?.rolePermissions) ? data.rolePermissions : []
    })

    const syncUserStorage = (profile) => {
        user.value = profile
        localStorage.setItem("user", JSON.stringify(profile))
        localStorage.setItem("userId", profile.userId || "")
        localStorage.setItem("fullName", profile.fullName || "")
        localStorage.setItem("email", profile.email || "")
    }

    const clearSession = () => {
        localStorage.removeItem("token")
        localStorage.removeItem("refreshToken")
        localStorage.removeItem("user")
        localStorage.removeItem("userId")
        localStorage.removeItem("fullName")
        localStorage.removeItem("email")
        user.value = null
    }

    const login = async (username, password) => {
        error.value = ""
        loading.value = true

        try {
            const res = await loginApi({ email: username, password })
            const token = res.accessToken || res.token
            const refreshToken = res.refreshToken || ""

            if (!token) {
                throw new Error("Không nhận được token")
            }

            localStorage.setItem("token", token)
            if (refreshToken) {
                localStorage.setItem("refreshToken", refreshToken)
            }

            const profile = normalizeUser(await getMeApi())
            syncUserStorage(profile)

            router.push("/")
            return profile
        } catch (err) {
            clearSession()
            error.value = err.message || "Đăng nhập thất bại"
            return null
        } finally {
            loading.value = false
        }
    }

    const logout = async () => {
        const refreshToken = localStorage.getItem("refreshToken")

        try {
            if (refreshToken) {
                await logoutApi(refreshToken)
            }
        } catch (err) {
            console.warn("Logout API failed:", err?.message || err)
        } finally {
            clearSession()
            router.push("/login")
        }
    }

    const isAuthenticated = () => !!localStorage.getItem("token")

    const getMe = async () => {
        try {
            const profile = normalizeUser(await getMeApi())
            syncUserStorage(profile)
            return profile
        } catch (err) {
            console.log(err.message)
            clearSession()
            router.push("/login")
            return null
        }
    }

    return { login, logout, getMe, user, isAuthenticated, loading, error }
}
