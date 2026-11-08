<template>
    <div class="app-shell">
        <aside :class="['sidebar', { collapsed: isCollapsed }]">
            <div class="brand">
                <button class="icon-btn" @click="toggleSidebar" aria-label="Thu gọn menu">
                    <span class="brand-mark">✦</span>
                </button>
                <div v-if="!isCollapsed" class="brand-text">
                    <h1>HỌC VUI<br />MỖI NGÀY</h1>
                </div>
            </div>

            <nav class="nav-section">
                <p v-if="!isCollapsed" class="section-label">Không gian quản trị</p>

                <RouterLink v-if="hasPermission('ManageQuizContent')" to="/quiz-admin" class="nav-item" active-class="active">
                    <span class="nav-left">
                        <i class="bi bi-journal-check"></i>
                        <span v-if="!isCollapsed">Nội dung học tập</span>
                    </span>
                </RouterLink>

                <RouterLink v-if="hasPermission('ManageUsers')" to="/users" class="nav-item" active-class="active">
                    <span class="nav-left">
                        <i class="bi bi-people"></i>
                        <span v-if="!isCollapsed">Học sinh và tài khoản</span>
                    </span>
                </RouterLink>

                <RouterLink
                    v-if="hasAdminRole && hasPermission('ResetUserPasswords')"
                    to="/cap-lai-mat-khau"
                    class="nav-item"
                    active-class="active"
                >
                    <span class="nav-left">
                        <i class="bi bi-key"></i>
                        <span v-if="!isCollapsed">Cấp lại mật khẩu</span>
                    </span>
                </RouterLink>

                <RouterLink v-if="hasPermission('ManagePermissions')" to="/permissions" class="nav-item" active-class="active">
                    <span class="nav-left">
                        <i class="bi bi-shield-lock"></i>
                        <span v-if="!isCollapsed">Phân quyền quản trị</span>
                    </span>
                </RouterLink>

                <RouterLink v-if="hasPermission('ViewSystemLogs')" to="/nhat-ky-he-thong" class="nav-item" active-class="active">
                    <span class="nav-left">
                        <i class="bi bi-clock-history"></i>
                        <span v-if="!isCollapsed">Hoạt động hệ thống</span>
                    </span>
                </RouterLink>
            </nav>
        </aside>

        <div class="content-shell">
            <header class="topbar">
                <div class="topbar-left">
                    <div class="vn-header">
                        <span class="header-mark">✦</span>
                        <div>
                            <h2>KHU VỰC QUẢN TRỊ</h2>
                            <p>Quản lý nội dung và tài khoản · {{ currentDateTime }}</p>
                        </div>
                    </div>
                </div>

                <div class="topbar-right">
                    <RouterLink to="/" class="student-link"><i class="bi bi-mortarboard"></i> Trang học sinh</RouterLink>
                    <div class="context-badge">
                        <span class="label">Vai trò</span>
                        <strong>{{ userRoleLabel }}</strong>
                    </div>

                    <div class="user-menu-wrapper">
                        <button type="button" class="user-menu" @click="toggleDropdown">
                            <i class="bi bi-person-circle"></i>
                            <div class="user-meta">
                                <strong :title="currentUserDisplayName">{{ currentUserDisplayName }}</strong>
                                <span>{{ user?.email || "---" }}</span>
                            </div>
                            <i class="bi bi-chevron-down"></i>
                        </button>

                        <div v-if="dropdownOpen" class="user-dropdown-menu">
                            <button type="button" class="dropdown-item danger" @click.stop="logout">
                                Đăng xuất
                            </button>
                        </div>
                    </div>
                </div>
            </header>

            <main class="main-content">
                <slot />
            </main>
        </div>
    </div>
</template>

<script setup>
    import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
    import { RouterLink, useRouter } from 'vue-router'
    import { useAuth } from '../composables/useAuth'
    import { canAccessPermission, hasRole, resolveEffectivePermissions } from '../utils/accessControl'

    const router = useRouter()
    const { getMe, logout: authLogout, user } = useAuth()

    const dropdownOpen = ref(false)
    const savedCollapsed = localStorage.getItem('sidebar_collapsed')
    const isCollapsed = ref(savedCollapsed === null ? false : savedCollapsed === 'true')
    const now = ref(new Date())
    let timer = null

    watch(isCollapsed, (value) => {
        localStorage.setItem('sidebar_collapsed', String(value))
    })

    const userPermissions = computed(() => resolveEffectivePermissions(user.value))
    const hasPermission = (permission) => canAccessPermission(userPermissions.value, permission, user.value)
    const hasAdminRole = computed(() => hasRole(user.value, 'Admin'))

    const userRoleLabel = computed(() => {
        if (!Array.isArray(user.value?.roles) || user.value.roles.length === 0) {
            return 'Người dùng'
        }

        return user.value.roles.join(', ')
    })

    const currentUserDisplayName = computed(() => {
        if (!user.value) return '---'
        return user.value.fullName || user.value.userName || user.value.email || '---'
    })

    const currentDateTime = computed(() => {
        return now.value.toLocaleString('vi-VN', {
            weekday: 'long',
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
        })
    })

    const toggleSidebar = () => {
        isCollapsed.value = !isCollapsed.value
    }

    const toggleDropdown = (e) => {
        e.stopPropagation()
        dropdownOpen.value = !dropdownOpen.value
    }

    const handleClickOutside = () => {
        dropdownOpen.value = false
    }

    const logout = async () => {
        await authLogout()
    }

    onMounted(async () => {
        timer = setInterval(() => {
            now.value = new Date()
        }, 1000)

        try {
            await getMe()
            if (!user.value) {
                router.push('/login')
            }
        } catch {
            router.push('/login')
        }

        document.addEventListener('click', handleClickOutside)
    })

    onUnmounted(() => {
        if (timer) clearInterval(timer)
        document.removeEventListener('click', handleClickOutside)
    })
</script>

<style scoped>
    * {
        box-sizing: border-box;
    }

    .app-shell {
        min-height: 100vh;
        display: flex;
        background: #f6f8fb;
        color: #1a263d;
    }

    .sidebar {
        width: 280px;
        background: linear-gradient(180deg, #0b4a29 0%, #0d5f33 52%, #0f7340 100%);
        color: #fff;
        padding: 20px 14px;
        transition: width 0.25s ease;
        overflow-y: auto;
        position: sticky;
        top: 0;
        height: 100vh;
    }

    .sidebar.collapsed {
        width: 84px;
    }

    .brand {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 4px 8px 18px;
        border-bottom: 1px solid rgba(230, 193, 106, 0.22);
        margin-bottom: 18px;
    }

    .brand-text h1 {
        margin: 0;
        font-size: 16px;
        font-weight: 800;
        line-height: 1.35;
    }

    .icon-btn {
        width: 64px;
        height: 64px;
        border: none;
        border-radius: 18px;
        background: rgba(255, 255, 255, 0.04);
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0;
        overflow: hidden;
        flex-shrink: 0;
    }

    .brand-logo {
        width: 100%;
        height: 100%;
        object-fit: contain;
        object-position: center;
        border-radius: 18px;
        display: block;
    }

    .section-label {
        margin: 18px 10px 10px;
        font-size: 11px;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: rgba(243, 221, 166, 0.74);
    }

    .nav-section {
        margin-bottom: 8px;
    }

    .nav-item {
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        text-decoration: none;
        border: none;
        background: transparent;
        color: #f8fbff;
        padding: 12px 14px;
        border-radius: 14px;
        cursor: pointer;
        font-size: 14px;
        margin-bottom: 4px;
        min-height: 48px;
    }

    .nav-item:hover,
    .nav-item.active {
        background: linear-gradient(90deg, rgba(216, 173, 82, 0.22), rgba(255, 255, 255, 0.08));
        box-shadow: inset 0 0 0 1px rgba(232, 200, 125, 0.18);
    }

    .nav-left {
        display: flex;
        align-items: center;
        gap: 12px;
        min-width: 0;
    }

    .content-shell {
        flex: 1;
        display: flex;
        flex-direction: column;
        min-width: 0;
    }

    .topbar {
        min-height: 76px;
        background: linear-gradient(90deg, rgba(255, 248, 230, 0.96), rgba(247, 239, 212, 0.96));
        border-bottom: 1px solid rgba(200, 155, 60, 0.24);
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 12px 24px;
        gap: 20px;
        box-shadow: 0 10px 20px rgba(8, 21, 47, 0.05);
    }

    .topbar-right {
        display: flex;
        align-items: center;
        gap: 12px;
        min-width: 0;
    }

    .context-badge {
        min-width: 150px;
        padding: 10px 14px;
        background: rgba(255, 255, 255, 0.88);
        border: 1px solid rgba(200, 155, 60, 0.18);
        border-radius: 14px;
        box-shadow: 0 10px 18px rgba(8, 21, 47, 0.06);
    }

    .context-badge .label {
        display: block;
        font-size: 11px;
        color: #7b6d4d;
        margin-bottom: 4px;
    }

    .context-badge strong {
        display: block;
        font-size: 13px;
        color: #122d61;
        max-width: 190px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .user-menu-wrapper {
        position: relative;
    }

    .user-menu {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 10px 14px;
        border-radius: 16px;
        border: 1px solid rgba(200, 155, 60, 0.2);
        background: rgba(255, 255, 255, 0.9);
        cursor: pointer;
        box-shadow: 0 10px 22px rgba(8, 21, 47, 0.08);
        min-width: 220px;
        max-width: 320px;
    }

    .user-menu i.bi-person-circle {
        font-size: 28px;
        color: #c89b3c;
    }

    .user-meta {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        min-width: 0;
        flex: 1;
    }

    .user-meta strong,
    .user-meta span {
        display: block;
        max-width: 100%;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .user-meta strong {
        font-size: 13px;
        color: #102753;
    }

    .user-meta span {
        font-size: 12px;
        color: #6c7287;
    }

    .user-dropdown-menu {
        position: absolute;
        right: 0;
        top: calc(100% + 8px);
        min-width: 220px;
        background: #fff;
        border: 1px solid rgba(200, 155, 60, 0.18);
        border-radius: 16px;
        box-shadow: 0 16px 40px rgba(15, 23, 42, 0.12);
        overflow: hidden;
        z-index: 30;
    }

    .dropdown-item {
        width: 100%;
        display: block;
        text-align: left;
        padding: 12px 14px;
        border: none;
        background: transparent;
        color: #122348;
        cursor: pointer;
        font-size: 14px;
    }

    .dropdown-item:hover {
        background: rgba(216, 173, 82, 0.12);
    }

    .dropdown-item.danger {
        color: #dc2626;
    }

    .main-content {
        flex: 1;
        padding: 24px;
        overflow: auto;
    }

    .vn-header {
        display: flex;
        align-items: center;
        gap: 10px;
    }

    .vn-flag {
        width: 48px;
        height: 48px;
        object-fit: cover;
        border-radius: 50%;
        box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
    }

    .vn-header h2 {
        margin: 0;
        font-size: 18px;
        font-weight: 800;
        color: #0f2759;
    }

    .vn-header p {
        margin: 0;
        font-size: 12px;
        color: #7b6d4d;
    }

    @media (max-width: 1200px) {
        .context-badge {
            display: none;
        }
    }

    @media (max-width: 992px) {
        .sidebar {
            position: fixed;
            z-index: 40;
            left: 0;
            top: 0;
        }

        .content-shell {
            margin-left: 84px;
        }

        .sidebar:not(.collapsed)+.content-shell {
            margin-left: 280px;
        }
    }

    @media (max-width: 768px) {
        .topbar {
            height: auto;
            align-items: flex-start;
            flex-direction: column;
            padding: 16px;
        }

        .topbar-right {
            width: 100%;
            justify-content: space-between;
        }

        .main-content {
            padding: 16px;
        }
    }

    /* Match the student site's soft indigo visual system while keeping the
       existing admin pages and their Bootstrap-based content intact. */
    .app-shell {
        background: #f5f7ff;
        color: #29365e;
        font-family: "Be Vietnam Pro", "Segoe UI", sans-serif;
    }

    .sidebar {
        width: 264px;
        background: #fff;
        color: #596581;
        border-right: 1px solid #e8ecf5;
        padding: 18px 13px;
        box-shadow: 8px 0 26px rgba(37, 52, 102, .035);
    }

    .sidebar.collapsed { width: 78px; }

    .brand {
        gap: 11px;
        border-bottom: 1px solid #edf0f7;
        padding: 4px 7px 17px;
    }

    .brand-text h1 {
        color: #2d3b6a;
        font-size: 13px;
        letter-spacing: .04em;
    }

    .icon-btn {
        width: 44px;
        height: 44px;
        border-radius: 15px;
        background: #fff3d5;
    }

    .brand-mark {
        color: #eda632;
        font-size: 31px;
        line-height: 1;
    }

    .section-label {
        color: #959cb0;
        font-size: 10px;
        letter-spacing: .1em;
    }

    .nav-item {
        color: #66718c;
        border-radius: 12px;
        font-size: 13px;
        min-height: 45px;
        padding: 11px 12px;
    }

    .nav-item i { color: #8289db; font-size: 17px; }

    .nav-item:hover,
    .nav-item.active {
        background: #f0f1ff;
        color: #5362ca;
        box-shadow: inset 0 0 0 1px #e4e5ff;
    }

    .topbar {
        min-height: 76px;
        background: rgba(255,255,255,.94);
        border-bottom: 1px solid #e8ecf5;
        box-shadow: 0 7px 22px rgba(37,52,102,.035);
        padding: 12px 27px;
    }

    .header-mark {
        display: grid;
        place-items: center;
        width: 42px;
        height: 42px;
        border-radius: 14px;
        background: #fff3d5;
        color: #eda632;
        font-size: 25px;
    }

    .vn-header h2 { color: #303d68; font-size: 15px; letter-spacing: .02em; }
    .vn-header p { color: #9299ad; font-size: 11px; margin-top: 4px; }

    .student-link {
        display: inline-flex;
        align-items: center;
        gap: 7px;
        padding: 10px 13px;
        border-radius: 12px;
        background: #f0f1ff;
        color: #626fd0;
        text-decoration: none;
        font-size: 12px;
        font-weight: 800;
        white-space: nowrap;
    }

    .context-badge {
        background: #f8f9ff;
        border: 1px solid #e8ebf7;
        border-radius: 12px;
        box-shadow: none;
    }

    .context-badge .label { color: #8b93aa; }
    .context-badge strong { color: #5362ca; }

    .user-menu {
        border-color: #e8ecf5;
        border-radius: 13px;
        box-shadow: none;
    }

    .user-menu i.bi-person-circle { color: #7780d9; }
    .user-meta strong { color: #354064; }
    .user-dropdown-menu { border-color: #e8ecf5; }
    .dropdown-item:hover { background: #f4f5ff; }

    .main-content {
        padding: 26px clamp(16px, 3vw, 38px);
        overflow-x: hidden;
    }

    @media (max-width: 992px) {
        .sidebar, .sidebar.collapsed { width: 78px; padding-inline: 9px; }
        .brand { justify-content: center; padding-inline: 0; }
        .brand-text, .section-label, .nav-item .nav-left > span { display: none !important; }
        .nav-item { justify-content: center; padding: 10px; }
        .nav-left { justify-content: center; }
        .content-shell, .sidebar:not(.collapsed) + .content-shell { margin-left: 78px; }
    }

    @media (max-width: 600px) {
        .topbar { flex-direction: row; align-items: center; padding: 11px 13px; gap: 9px; }
        .topbar-right { width: auto; gap: 7px; }
        .vn-header { gap: 8px; }
        .header-mark { width: 34px; height: 34px; font-size: 20px; }
        .vn-header h2 { font-size: 11px; }
        .vn-header p { display: none; }
        .student-link { padding: 9px; font-size: 0; }
        .student-link i { font-size: 17px; }
        .user-menu { min-width: 0; padding: 8px; }
        .user-menu .user-meta, .user-menu > .bi-chevron-down { display: none; }
        .user-menu i.bi-person-circle { font-size: 22px; }
        .main-content { padding: 15px 12px; }
    }
</style>
