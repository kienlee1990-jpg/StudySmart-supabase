import { createRouter, createWebHistory } from "vue-router"

import Login from "../components/login.vue"
import ResetPassword from "../components/ResetPassword.vue"
import CapLaiMatKhauPage from "../components/views/CapLaiMatKhauPage.vue"
import NhatKyHeThongPage from "../components/views/NhatKyHeThongPage.vue"
import Permissions from "../components/views/Permissions.vue"
import Users from "../components/views/Users.vue"
import SubjectSelect from "../components/quiz/SubjectSelect.vue"
import SubjectHome from "../components/quiz/SubjectHome.vue"
import Ranking from "../components/quiz/Ranking.vue"
import QuizPlay from "../components/quiz/QuizPlay.vue"
import QuizResult from "../components/quiz/QuizResult.vue"
import StudentRegister from "../components/quiz/StudentRegister.vue"
import QuizAdmin from "../components/quiz/QuizAdmin.vue"
import {
    canAccessPermission,
    getStoredUserPermissions,
    getStoredUserProfile,
    hasRole
} from "../utils/accessControl"

const routes = [
    {
        path: "/login",
        name: "Đăng nhập",
        component: Login
    },
    {
        path: "/reset-password",
        name: "ResetPassword",
        component: ResetPassword
    },
    { path: "/register", name: "StudentRegister", component: StudentRegister },
    { path: "/subjects/:subjectSlug", name: "SubjectHome", component: SubjectHome },
    { path: "/ranking", name: "Ranking", component: Ranking },
    { path: "/tower-session/:subjectSlug/:level", name: "TowerSession", component: QuizPlay, props: route => ({ ...route.params, mode: "Tower" }) },
    { path: "/quiz-session/:subjectSlug/:mode/:grade/:duration", name: "QuizSession", component: QuizPlay, props: true },
    { path: "/quiz/:id", name: "QuizPlay", component: QuizPlay, props: true },
    { path: "/quiz-result/:id", name: "QuizResult", component: QuizResult, props: true },
    { path: "/quiz-admin", name: "QuizAdmin", component: QuizAdmin, meta: { requiresAuth: true, permission: "ManageQuizContent" } },
    {
        path: "/users",
        name: "Users",
        component: Users,
        meta: { requiresAuth: true, permission: "ManageUsers" }
    },
    {
        path: "/cap-lai-mat-khau",
        name: "CapLaiMatKhau",
        component: CapLaiMatKhauPage,
        meta: { requiresAuth: true, permission: "ResetUserPasswords", adminOnly: true }
    },
    {
        path: "/permissions",
        name: "Permissions",
        component: Permissions,
        meta: { requiresAuth: true, permission: "ManagePermissions" }
    },
    {
        path: "/nhat-ky-he-thong",
        name: "NhatKyHeThong",
        component: NhatKyHeThongPage,
        meta: { requiresAuth: true, permission: "ViewSystemLogs" }
    },
    {
        path: "/",
        component: SubjectSelect
    },
    {
        path: "/:pathMatch(.*)*",
        redirect: "/"
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

const clearStoredSession = () => {
    localStorage.removeItem("token")
    localStorage.removeItem("refreshToken")
    localStorage.removeItem("user")
}

const getFirstAuthorizedPath = (permissions, profile) => {
    const firstRoute = routes.find(route => {
        if (!route?.path || route.path === "/login" || route.path === "/") return false
        if (!route?.meta?.requiresAuth) return false
        return canAccessPermission(permissions, route.meta.permission, profile)
    })

    return firstRoute?.path || "/login"
}

router.beforeEach((to) => {
    const token = localStorage.getItem("token")
    const permissions = getStoredUserPermissions()
    const profile = getStoredUserProfile()

    if (to.meta.requiresAuth && !token) {
        return { path: "/login" }
    }

    if (token && to.meta?.adminOnly && !hasRole(profile, "Admin")) {
        return { path: getFirstAuthorizedPath(permissions, profile) }
    }

    if (to.path === "/login" && token) {
        const firstAuthorizedPath = getFirstAuthorizedPath(permissions, profile)
        return { path: firstAuthorizedPath === "/login" ? "/" : firstAuthorizedPath }
    }

    if (token && to.meta.requiresAuth && !canAccessPermission(permissions, to.meta.permission, profile)) {
        const fallbackPath = getFirstAuthorizedPath(permissions, profile)
        if (fallbackPath === "/login") {
            clearStoredSession()
        }
        return { path: fallbackPath }
    }

    return true
})

export default router
