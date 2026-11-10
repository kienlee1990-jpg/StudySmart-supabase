import { createRouter, createWebHistory } from "vue-router"

import Login from "../components/login.vue"
import ResetPassword from "../components/ResetPassword.vue"
import CapLaiMatKhauPage from "../components/views/CapLaiMatKhauPage.vue"
import NhatKyHeThongPage from "../components/views/NhatKyHeThongPage.vue"
import Permissions from "../components/views/Permissions.vue"
import Users from "../components/views/Users.vue"
import SubjectSelect from "../components/quiz/SubjectSelect.vue"
import SubjectHome from "../components/quiz/SubjectHome.vue"
import LessonCatalog from "../components/quiz/LessonCatalog.vue"
import LessonVideo from "../components/quiz/LessonVideo.vue"
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

import { hasAccountSession, requiresAccountMode } from "../utils/accountSession"

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
    { path: "/learning/:subjectSlug/:grade", name: "LessonCatalog", component: LessonCatalog },
    { path: "/learning/:subjectSlug/:grade/video/:id", name: "LessonVideo", component: LessonVideo },
    { path: "/ranking", name: "Ranking", component: Ranking, meta: { requiresAuth: true } },
    { path: "/tower-session/:subjectSlug/:level", name: "TowerSession", component: QuizPlay, meta: { requiresAuth: true }, props: route => ({ ...route.params, mode: "Tower" }) },
    { path: "/quiz-session/:subjectSlug/:mode/:grade/:duration", name: "QuizSession", component: QuizPlay, props: route => ({ ...route.params, topics: route.query.topics }) },
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
    routes,
    scrollBehavior(to, from, savedPosition) {
        if (savedPosition) return savedPosition
        if (to.hash) return {
            el: to.hash,
            top: 100,
            behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth'
        }
        return { top: 0 }
    }
})

const clearStoredSession = () => {
    localStorage.removeItem("token")
    localStorage.removeItem("refreshToken")
    localStorage.removeItem("user")
}

const getFirstAuthorizedPath = (permissions, profile) => {
    const firstRoute = routes.find(route => {
        if (!route?.path || route.path === "/login" || route.path === "/") return false
        if (!route?.meta?.permission) return false
        return canAccessPermission(permissions, route.meta.permission, profile)
    })

    return firstRoute?.path || "/login"
}

router.beforeEach((to) => {
    const token = hasAccountSession()
    const permissions = getStoredUserPermissions()
    const profile = getStoredUserProfile()

    if ((to.meta.requiresAuth || requiresAccountMode(to.params.mode)) && !token) {
        return { path: "/login", query: { redirect: to.fullPath } }
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
