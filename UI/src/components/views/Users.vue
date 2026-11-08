<template>
  <BaseLayout>
    <div class="page-shell">
      <div class="page-header">
        <div class="gov-banner">
          <div class="gov-emblem">
            <i class="bi bi-people-fill"></i>
          </div>
          <div>
            <div class="gov-title">NGƯỜI DÙNG VÀ VAI TRÒ</div>
          </div>
        </div>
        <div class="header-actions">
          <RouterLink v-if="canManagePermissions" to="/permissions" class="btn btn-outline-primary">Mở màn phân quyền
            chi tiết</RouterLink>
          <button @click="openRoleManager" class="btn btn-outline-success">Quản lý vai trò</button>
          <button @click="openAdd" class="btn btn-primary">+ Thêm người dùng</button>
        </div>
      </div>

      <div class="alert alert-info border-0 shadow-sm">
        <strong>Màn này chỉ dùng để gán vai trò.</strong>
        Quyền chi tiết theo user và theo role được cấu hình tại màn Phân quyền.
      </div>

      <div class="summary-grid">
        <div class="summary-card">
          <span class="summary-label">Tổng người dùng</span>
          <strong class="summary-value">{{ users.length }}</strong>
        </div>
        <div class="summary-card">
          <span class="summary-label">Đang hoạt động</span>
          <strong class="summary-value text-success">{{ activeUsersCount }}</strong>
        </div>
        <div class="summary-card">
          <span class="summary-label">Ngừng hoạt động</span>
          <strong class="summary-value text-danger">{{ inactiveUsersCount }}</strong>
        </div>
        <div class="summary-card">
          <span class="summary-label">Có vai trò</span>
          <strong class="summary-value text-primary">{{ usersWithRolesCount }}</strong>
        </div>
      </div>

      <div class="card shadow-sm border-0 mb-3">
        <div class="card-body">
          <div class="filter-grid">
            <div>
              <label class="form-label">Từ khóa</label>
              <input v-model.trim="searchTerm" type="text" class="form-control"
                placeholder="Nhập họ tên, username, email hoặc số điện thoại" />
            </div>
            <div>
              <label class="form-label">Trạng thái</label>
              <select v-model="statusFilter" class="form-select">
                <option value="ALL">Tất cả</option>
                <option value="ACTIVE">Đang hoạt động</option>
                <option value="INACTIVE">Ngừng hoạt động</option>
              </select>
            </div>
            <div>
              <label class="form-label">Vai trò</label>
              <select v-model="roleFilter" class="form-select">
                <option value="ALL">Tất cả vai trò</option>
                <option value="NONE">Chưa có vai trò</option>
                <option v-for="role in availableRoles" :key="role" :value="role">{{ role }}</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <div v-if="loading" class="loading-state">
        <div class="spinner-border text-primary"></div>
        <span>Đang tải danh sách người dùng...</span>
      </div>

      <div v-else-if="filteredUsers.length" class="card shadow-sm border-0">
        <div class="card-body p-0">
          <ColumnVisibilityTools table-id="Users-table" />
          <table id="Users-table" class="table table-hover align-middle mb-0 permission-table managed-table">
            <thead>
              <tr>
                <th>Người dùng</th>
                <th>Email</th>
                <th>Vai trò</th>
                <th>Quyền chi tiết</th>
                <th class="text-center">Trạng thái</th>
                <th class="text-end">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in filteredUsers" :key="item.id">
                <td class="user-cell">
                  <strong class="d-block">{{ item.userName || item.email || "Chưa đặt tên đăng nhập" }}</strong>
                </td>
                <td>
                  <strong class="d-block">{{ item.email || "-" }}</strong>
                  <span class="muted-line">{{ item.phoneNumber || item.address || "Chưa cập nhật liên hệ" }}</span>
                </td>
                <td>
                  <div class="chip-wrap">
                    <span v-for="role in item.roles || []" :key="role" class="badge role-chip">{{ role }}</span>
                    <span v-if="!(item.roles || []).length" class="text-muted">Chưa có vai trò</span>
                  </div>
                </td>
                <td>
                  <div class="chip-wrap">
                    <span v-for="permission in item.permissions || []" :key="permission"
                      class="badge permission-chip">{{ getPermissionLabel(permission) }}</span>
                    <span v-if="!(item.permissions || []).length" class="text-muted">Không có quyền trực tiếp</span>
                  </div>
                </td>
                <td class="text-center">
                  <template v-if="!isProtectedUser(item)">
                    <button type="button" class="status-toggle"
                      :class="item.isActive ? 'status-active' : 'status-inactive'" :disabled="item._loading"
                      @click="confirmToggle(item)">
                      <span v-if="item._loading" class="spinner-border spinner-border-sm"></span>
                      <span v-else>{{ item.isActive ? "Đang hoạt động" : "Ngừng hoạt động" }}</span>
                    </button>
                  </template>
                  <span v-else class="badge text-bg-warning">Tài khoản hệ thống</span>
                </td>
                <td class="text-end">
                  <div class="action-stack">
                    <button @click="openEdit(item)" class="btn btn-sm btn-outline-warning">Sửa</button>
                    <button v-if="!isProtectedUser(item)" @click="openRole(item)" class="btn btn-sm btn-primary">
                      Gán vai trò
                    </button>
                    <span v-else class="text-muted small">Không chỉnh vai trò</span>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-else class="alert alert-light border shadow-sm">
        Không tìm thấy người dùng phù hợp với bộ lọc hiện tại.
      </div>

      <div v-if="showForm" class="modal fade show d-block" tabindex="-1">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content shadow-lg border-0 rounded-4">
            <div class="modal-header bg-primary text-white rounded-top-4">
              <h5 class="modal-title">{{ isEdit ? "Cập nhật người dùng" : "Thêm người dùng mới" }}</h5>
              <button class="btn-close btn-close-white" @click="closeForm"></button>
            </div>
            <div class="modal-body px-4 py-3" autocomplete="off">
              <template v-if="!isEdit">
                <div class="row g-3">
                  <div class="col-md-6">
                    <label class="form-label">Tên đăng nhập</label>
                    <input v-model="form.userName" placeholder="Nhập tên đăng nhập" class="form-control shadow-sm"
                      autocomplete="off" autocapitalize="none" />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Tên hiển thị</label>
                    <input v-model="form.fullName" placeholder="Nhập tên hiển thị" class="form-control shadow-sm"
                      autocomplete="off" />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Email</label>
                    <input v-model="form.email" placeholder="Nhập email" class="form-control shadow-sm"
                      autocomplete="off" autocapitalize="none" />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Mật khẩu</label>
                    <input type="password" v-model="form.password" placeholder="Nhập mật khẩu"
                      class="form-control shadow-sm" autocomplete="new-password" />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Số điện thoại</label>
                    <input v-model="form.phoneNumber" placeholder="Nhập số điện thoại" class="form-control shadow-sm"
                      autocomplete="off" />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Địa chỉ</label>
                    <input v-model="form.address" placeholder="Nhập địa chỉ" class="form-control shadow-sm"
                      autocomplete="off" />
                  </div>
                </div>
              </template>
              <template v-else>
                <div class="mb-3">
                  <label class="form-label">Tên hiển thị</label>
                  <input v-model="form.fullName" placeholder="Nhập họ tên" class="form-control shadow-sm" />
                </div>
                <div class="mb-3">
                  <label class="form-label">Địa chỉ</label>
                  <input v-model="form.address" placeholder="Nhập địa chỉ" class="form-control shadow-sm" />
                </div>
                <div>
                  <label class="form-label">Số điện thoại</label>
                  <input v-model="form.phoneNumber" placeholder="Nhập số điện thoại" class="form-control shadow-sm" />
                </div>
              </template>
            </div>
            <div class="modal-footer border-0 px-4 pb-4">
              <button class="btn btn-outline-secondary rounded-pill px-4" @click="closeForm">Hủy</button>
              <button class="btn btn-primary rounded-pill px-4 shadow-sm" @click="saveUser">Lưu</button>
            </div>
          </div>
        </div>
      </div>

      <div v-if="showRoleForm" class="modal fade show d-block">
        <div class="modal-dialog modal-dialog-centered modal-lg">
          <div class="modal-content shadow-lg border-0 rounded-4">
            <div class="modal-header bg-success text-white rounded-top-4">
              <div>
                <h5 class="modal-title mb-1">Gán vai trò</h5>
                <div class="small opacity-75">{{ currentUserName }}</div>
              </div>
              <button class="btn-close btn-close-white" @click="showRoleForm = false"></button>
            </div>
            <div class="modal-body px-4 py-3">
              <div v-if="roleLoading" class="text-center py-4">
                <div class="spinner-border text-primary"></div>
              </div>
              <div v-else>
                <div class="role-modal-toolbar">
                  <div class="alert alert-light border mb-0 flex-grow-1">
                    Chọn vai trò cần gán cho người dùng này. Nếu cần phân quyền chi tiết hơn, mở màn Phân quyền để gán
                    trực tiếp theo user hoặc role.
                  </div>
                  <div class="d-flex gap-2">
                    <button type="button" class="btn btn-outline-success btn-sm" @click="selectAllRoles">Chọn tất
                      cả</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm" @click="clearRoles">Bỏ hết</button>
                  </div>
                </div>

                <div class="role-grid mt-3">
                  <label v-for="role in availableRoles" :key="role" class="role-option">
                    <input type="checkbox" :value="role" v-model="selectedRoles" />
                    <span>
                      <strong class="d-block">{{ role }}</strong>
                      <small class="text-muted">{{ selectedRoles.includes(role) ? "Đã chọn" : "Chưa chọn" }}</small>
                    </span>
                  </label>
                </div>
              </div>
            </div>
            <div class="modal-footer border-0 px-4 pb-4">
              <button class="btn btn-outline-secondary rounded-pill px-4" @click="showRoleForm = false">Hủy</button>
              <button class="btn btn-success rounded-pill px-4 shadow-sm" @click="saveRole">Lưu vai trò</button>
            </div>
          </div>
        </div>
      </div>

      <div v-if="showRoleManager" class="modal fade show d-block">
        <div class="modal-dialog modal-dialog-centered modal-xl">
          <div class="modal-content shadow-lg border-0 rounded-4">
            <div class="modal-header bg-primary text-white rounded-top-4">
              <div>
                <h5 class="modal-title mb-1">Quản lý vai trò</h5>
                <div class="small opacity-75">Thêm, sửa tên và xóa vai trò ngay trên màn người dùng</div>
              </div>
              <button class="btn-close btn-close-white" @click="closeRoleManager"></button>
            </div>
            <div class="modal-body px-4 py-3">
              <div class="role-manager-layout">
                <div class="role-manager-list">
                  <div class="role-manager-list-head">
                    <h6 class="mb-0">Danh sách vai trò</h6>
                    <button type="button" class="btn btn-sm btn-outline-primary" @click="startAddRole">+ Thêm vai
                      trò</button>
                  </div>

                  <div v-if="roleCatalogLoading" class="loading-state py-3">
                    <div class="spinner-border text-primary"></div>
                    <span>Đang tải vai trò...</span>
                  </div>

                  <div v-else class="role-manager-items">
                    <button v-for="role in managedRoles" :key="role.id" type="button" class="role-manager-item"
                      :class="{ active: selectedManagedRoleId === role.id }" @click="selectManagedRole(role)">
                      <div>
                        <strong class="d-block">{{ role.roleName }}</strong>
                        <span class="muted-line">{{ role.userCount }} người dùng</span>
                      </div>
                      <span class="badge role-count-badge">{{ role.permissions?.length || 0 }} quyền</span>
                    </button>
                  </div>
                </div>

                <div class="role-manager-form">
                  <div class="role-form-head">
                    <div>
                      <h6 class="mb-1">{{ roleForm.mode === 'EDIT' ? 'Sửa vai trò' : 'Thêm vai trò mới' }}</h6>
                      <div class="text-muted small">
                        {{ roleForm.mode === 'EDIT'
                        ? 'Đổi tên vai trò sẽ giữ lại quyền và người dùng đang gán vai trò đó.'
                        : 'Tạo vai trò mới để dùng cho gán vai trò và phân quyền chi tiết.' }}
                      </div>
                    </div>
                  </div>

                  <div class="mb-3">
                    <label class="form-label">Tên vai trò</label>
                    <input v-model.trim="roleForm.roleName" type="text" class="form-control"
                      placeholder="Nhập tên vai trò" />
                  </div>

                  <div v-if="roleForm.mode === 'EDIT' && selectedManagedRole" class="role-form-meta">
                    <div class="meta-inline-card">
                      <span class="meta-inline-label">Vai trò cũ</span>
                      <strong>{{ roleForm.originalName }}</strong>
                    </div>
                    <div class="meta-inline-card">
                      <span class="meta-inline-label">Số người dùng</span>
                      <strong>{{ selectedManagedRole.userCount }}</strong>
                    </div>
                    <div class="meta-inline-card">
                      <span class="meta-inline-label">Số quyền</span>
                      <strong>{{ selectedManagedRole.permissions?.length || 0 }}</strong>
                    </div>
                  </div>

                  <div class="role-form-actions">
                    <button v-if="roleForm.mode === 'EDIT'" type="button" class="btn btn-outline-danger"
                      :disabled="roleCatalogLoading" @click="deleteManagedRole">
                      Xóa vai trò
                    </button>
                    <div class="ms-auto d-flex gap-2">
                      <button type="button" class="btn btn-outline-secondary" @click="closeRoleManager">Đóng</button>
                      <button type="button" class="btn btn-primary" :disabled="roleCatalogLoading"
                        @click="saveManagedRole">
                        {{ roleForm.mode === 'EDIT' ? 'Cập nhật vai trò' : 'Tạo vai trò' }}
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </BaseLayout>
</template>

<script setup>
  import { computed, onMounted, ref } from 'vue'
  import { RouterLink } from 'vue-router'
  import BaseLayout from '../BaseLayout.vue'
  import ColumnVisibilityTools from '../shared/ColumnVisibilityTools.vue'
  import httpClient from '../../services/httpClient'
  import { canAccessPermission, getStoredUserPermissions, getStoredUserProfile } from '../../utils/accessControl'

  const users = ref([])
  const loading = ref(false)
  const showForm = ref(false)
  const showRoleForm = ref(false)
  const roleLoading = ref(false)
  const isEdit = ref(false)
  const searchTerm = ref('')
  const statusFilter = ref('ALL')
  const roleFilter = ref('ALL')
  const roles = ref([])
  const selectedRoles = ref([])
  const currentUserId = ref(null)
  const currentUserName = ref('')
  const showRoleManager = ref(false)
  const roleCatalogLoading = ref(false)
  const selectedManagedRoleId = ref('')
  const roleForm = ref({ mode: 'ADD', originalName: '', roleName: '' })
  const form = ref({ id: null, email: '', password: '', userName: '', fullName: '', phoneNumber: '', address: '', isActive: true })
  const protectedUserId = 'd12c48e3-825f-4799-b6f1-a55857131796'
  const canManagePermissions = computed(() => canAccessPermission(getStoredUserPermissions(), 'ManagePermissions', getStoredUserProfile()))

  const permissionLabelMap = {
    ManageUsers: 'Quản trị người dùng',
    ResetUserPasswords: 'Cấp lại mật khẩu',
    ManagePermissions: 'Quản trị phân quyền',
    ViewSystemLogs: 'Xem nhật ký hệ thống',
    ManageQuizContent: 'Quản lý nội dung thi'
  }

  const isProtectedUser = (user) => user?.id === protectedUserId
  const getPermissionLabel = (permission) => permissionLabelMap[permission] || permission

  const availableRoles = computed(() =>
    [...roles.value]
      .map(item => item.roleName)
      .filter(Boolean)
      .sort((left, right) => left.localeCompare(right, 'vi'))
  )

  const managedRoles = computed(() =>
    [...roles.value]
      .sort((left, right) => left.roleName.localeCompare(right.roleName, 'vi'))
  )

  const selectedManagedRole = computed(() => managedRoles.value.find(item => item.id === selectedManagedRoleId.value) || null)
  const normalizeRole = (item) => ({
    id: item?.id || item?.Id || item?.roleId || '',
    roleName: item?.roleName || item?.RoleName || '',
    permissions: Array.isArray(item?.permissions) ? item.permissions : Array.isArray(item?.Permissions) ? item.Permissions : [],
    userCount: Number(item?.userCount ?? item?.UserCount ?? 0)
  })

  const activeUsersCount = computed(() => users.value.filter(item => item.isActive).length)
  const inactiveUsersCount = computed(() => users.value.filter(item => !item.isActive).length)
  const usersWithRolesCount = computed(() => users.value.filter(item => (item.roles || []).length > 0).length)

  const filteredUsers = computed(() => {
    const keyword = searchTerm.value.trim().toLowerCase()

    return users.value.filter(item => {
      const matchesKeyword = !keyword || [
        item.fullName,
        item.userName,
        item.email,
        item.phoneNumber,
        item.address
      ].some(value => String(value || '').toLowerCase().includes(keyword))

      const matchesStatus =
        statusFilter.value === 'ALL' ||
        (statusFilter.value === 'ACTIVE' && item.isActive) ||
        (statusFilter.value === 'INACTIVE' && !item.isActive)

      const userRoles = item.roles || []
      const matchesRole =
        roleFilter.value === 'ALL' ||
        (roleFilter.value === 'NONE' && !userRoles.length) ||
        userRoles.includes(roleFilter.value)

      return matchesKeyword && matchesStatus && matchesRole
    })
  })

  const fetchUsers = async () => {
    loading.value = true
    try {
      const res = await httpClient.get('/users')
      users.value = (Array.isArray(res.data) ? res.data : [])
        .map(item => ({ ...item, _loading: false }))
        .sort((a, b) => (a.id === protectedUserId ? -1 : b.id === protectedUserId ? 1 : 0))
    } catch (err) {
      console.error(err)
      alert('Không tải được danh sách người dùng.')
    } finally {
      loading.value = false
    }
  }

  const fetchRoles = async () => {
    try {
      roleCatalogLoading.value = true
      const res = await httpClient.get('/roles')
      roles.value = Array.isArray(res.data) ? res.data.map(normalizeRole) : []
    } catch (err) {
      console.error(err)
      roles.value = []
    } finally {
      roleCatalogLoading.value = false
    }
  }

  const openAdd = () => {
    isEdit.value = false
    form.value = { id: null, email: '', password: '', userName: '', fullName: '', phoneNumber: '', address: '', isActive: true }
    showForm.value = true
  }

  const openEdit = (user) => {
    isEdit.value = true
    form.value = {
      id: user.id,
      fullName: user.fullName || '',
      address: user.address || '',
      phoneNumber: user.phoneNumber || '',
      isActive: user.isActive
    }
    showForm.value = true
  }

  const saveUser = async () => {
    try {
      if (isEdit.value) {
        await httpClient.put(`/users/${form.value.id}`, {
          fullName: form.value.fullName,
          address: form.value.address,
          phoneNumber: form.value.phoneNumber,
          isActive: form.value.isActive
        })
      } else {
        await httpClient.post('/users', {
          email: form.value.email,
          password: form.value.password,
          userName: form.value.userName,
          fullName: form.value.fullName,
          phoneNumber: form.value.phoneNumber,
          address: form.value.address
        })
      }
      showForm.value = false
      await fetchUsers()
    } catch (err) {
      console.error(err)
      const apiMessage =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        err?.response?.data?.error ||
        err?.response?.data?.title ||
        (Array.isArray(err?.response?.data?.errors)
          ? err.response.data.errors.join(', ')
          : null)

      alert(apiMessage || 'Lưu người dùng thất bại.')
    }
  }

  const confirmToggle = (user) => {
    const ok = confirm(`Bạn có chắc muốn ${user.isActive ? 'ngừng' : 'kích hoạt'} người dùng này không?`)
    if (!ok) return
    toggleActive(user)
  }

  const toggleActive = async (user) => {
    try {
      user._loading = true
      await httpClient.put(`/users/${user.id}`, {
        fullName: user.fullName,
        address: user.address,
        phoneNumber: user.phoneNumber,
        isActive: !user.isActive
      })
      user.isActive = !user.isActive
    } catch (err) {
      console.error(err)
      alert('Đổi trạng thái thất bại.')
    } finally {
      user._loading = false
    }
  }

  const openRole = (user) => {
    currentUserId.value = user.id
    currentUserName.value = user.fullName || user.email || user.userName || ''
    selectedRoles.value = [...(user.roles || [])]
    showRoleForm.value = true
  }

  const resetRoleForm = () => {
    roleForm.value = { mode: 'ADD', originalName: '', roleName: '' }
  }

  const openRoleManager = async () => {
    showRoleManager.value = true
    resetRoleForm()
    if (!roles.value.length) {
      await fetchRoles()
    }
    if (managedRoles.value.length) {
      selectManagedRole(managedRoles.value[0])
    }
  }

  const closeRoleManager = () => {
    showRoleManager.value = false
    selectedManagedRoleId.value = ''
    resetRoleForm()
  }

  const startAddRole = () => {
    selectedManagedRoleId.value = ''
    resetRoleForm()
  }

  const selectManagedRole = (role) => {
    selectedManagedRoleId.value = role.id
    roleForm.value = {
      mode: 'EDIT',
      originalName: role.roleName,
      roleName: role.roleName
    }
  }

  const getManagedRoleByName = (roleName) => managedRoles.value.find(item => item.roleName === roleName) || null

  const saveManagedRole = async () => {
    const nextRoleName = roleForm.value.roleName.trim()
    if (!nextRoleName) {
      alert('Vui lòng nhập tên vai trò.')
      return
    }

    try {
      roleCatalogLoading.value = true

      if (roleForm.value.mode === 'ADD') {
        await httpClient.post('/roles', { roleName: nextRoleName })
        await fetchRoles()
        const createdRole = getManagedRoleByName(nextRoleName)
        if (createdRole) {
          selectManagedRole(createdRole)
        } else {
          startAddRole()
        }
        alert('Tạo vai trò thành công.')
        return
      }

      const oldRoleId = selectedManagedRole.value?.id
      const oldRoleName = roleForm.value.originalName.trim()
      if (!oldRoleId || !oldRoleName) {
        alert('Không xác định được vai trò cần sửa.')
        return
      }

      if (oldRoleName === nextRoleName) {
        alert('Tên vai trò không thay đổi.')
        return
      }

      await httpClient.put(`/roles/${oldRoleId}`, { roleName: nextRoleName })
      await Promise.all([fetchUsers(), fetchRoles()])
      const refreshedRole = getManagedRoleByName(nextRoleName)
      if (refreshedRole) {
        selectManagedRole(refreshedRole)
      }
      alert('Đổi tên vai trò thành công.')
    } catch (err) {
      console.error(err)
      alert(err.response?.data?.message || err.response?.data?.title || 'Lưu vai trò thất bại.')
    } finally {
      roleCatalogLoading.value = false
    }
  }

  const deleteManagedRole = async () => {
    const role = selectedManagedRole.value
    if (!role?.roleName) return

    const ok = confirm(`Bạn có chắc muốn xóa vai trò "${role.roleName}" không?`)
    if (!ok) return

    try {
      roleCatalogLoading.value = true
      const affectedUsers = users.value.filter(user => (user.roles || []).includes(role.roleName))
      for (const user of affectedUsers) {
        await httpClient.delete(`/users/${user.id}/roles/${encodeURIComponent(role.roleName)}`)
      }

      await httpClient.delete(`/roles/${role.id}`)
      await Promise.all([fetchUsers(), fetchRoles()])
      selectedManagedRoleId.value = ''
      resetRoleForm()
      alert('Xóa vai trò thành công.')
    } catch (err) {
      console.error(err)
      alert(err.response?.data?.message || err.response?.data?.title || 'Xóa vai trò thất bại.')
    } finally {
      roleCatalogLoading.value = false
    }
  }

  const selectAllRoles = () => {
    selectedRoles.value = [...availableRoles.value]
  }

  const clearRoles = () => {
    selectedRoles.value = []
  }

  const saveRole = async () => {
    if (!currentUserId.value) return
    try {
      roleLoading.value = true
      const user = users.value.find(item => item.id === currentUserId.value)
      const currentRoles = user?.roles || []
      const rolesToAdd = selectedRoles.value.filter(role => !currentRoles.includes(role))
      const rolesToRemove = currentRoles.filter(role => !selectedRoles.value.includes(role))

      for (const roleName of rolesToAdd) {
        await httpClient.put(`/users/${currentUserId.value}/roles/${encodeURIComponent(roleName)}`)
      }
      for (const roleName of rolesToRemove) {
        await httpClient.delete(`/users/${currentUserId.value}/roles/${encodeURIComponent(roleName)}`)
      }

      showRoleForm.value = false
      await fetchUsers()
      alert('Cập nhật vai trò thành công.')
    } catch (err) {
      console.error(err)
      alert('Cập nhật vai trò thất bại.')
    } finally {
      roleLoading.value = false
    }
  }

  const closeForm = () => {
    showForm.value = false
  }

  onMounted(async () => {
    await Promise.all([fetchUsers(), fetchRoles()])
  })
</script>

<style scoped>
  .page-shell {
    padding: 20px
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
    margin-bottom: 18px
  }

  .header-actions {
    display: flex;
    gap: 12px;
    flex-wrap: wrap
  }

  .gov-banner {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 20px 24px;
    border-radius: 20px;
    background: linear-gradient(135deg, #ffffff 0%, #f4f9ff 100%);
    box-shadow: 0 10px 30px rgba(13, 110, 253, .08);
    border: 1px solid rgba(13, 110, 253, .08)
  }

  .gov-emblem {
    width: 64px;
    height: 64px;
    border-radius: 18px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #0d6efd, #4ea1ff);
    color: #fff;
    font-size: 28px;
    box-shadow: 0 10px 24px rgba(13, 110, 253, .24)
  }

  .gov-title {
    font-size: 1.3rem;
    font-weight: 800;
    color: #1f2d3d;
    margin-bottom: 4px
  }

  .gov-sub {
    font-size: .95rem;
    color: #4f6b88;
    opacity: 1
  }

  .summary-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 14px;
    margin-bottom: 18px
  }

  .summary-card {
    padding: 16px 18px;
    border-radius: 16px;
    background: #fff;
    border: 1px solid #e9ecef;
    box-shadow: 0 10px 20px rgba(15, 23, 42, .05)
  }

  .summary-label {
    display: block;
    font-size: 12px;
    color: #64748b;
    text-transform: uppercase;
    font-weight: 700;
    margin-bottom: 6px
  }

  .summary-value {
    font-size: 28px;
    line-height: 1
  }

  .filter-grid {
    display: grid;
    grid-template-columns: 2fr 1fr 1fr;
    gap: 14px
  }

  .loading-state {
    display: flex;
    align-items: center;
    gap: 10px;
    color: #475569;
    padding: 18px 0
  }

  .permission-table thead th {
    background: #f8fafc;
    border-bottom: 1px solid #dbe4ef;
    color: #0f172a;
    font-size: 13px;
    text-transform: uppercase;
    letter-spacing: .03em
  }

  .permission-table td,
  .permission-table th {
    padding: 16px;
    border-color: #eef2f7
  }

  .user-cell strong {
    font-size: 15px
  }

  .muted-line {
    display: block;
    font-size: 12px;
    color: #64748b;
    margin-top: 3px
  }

  .chip-wrap {
    display: flex;
    flex-wrap: wrap;
    gap: 6px
  }

  .role-chip {
    background: #dbeafe;
    color: #1d4ed8
  }

  .permission-chip {
    background: #f8fafc;
    color: #334155;
    border: 1px solid #cbd5e1
  }

  .status-toggle {
    border: none;
    border-radius: 999px;
    padding: 8px 14px;
    font-size: 13px;
    font-weight: 600;
    min-width: 140px
  }

  .status-active {
    background: #dcfce7;
    color: #166534
  }

  .status-inactive {
    background: #fee2e2;
    color: #991b1b
  }

  .action-stack {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    flex-wrap: wrap
  }

  .modal {
    background: rgba(15, 23, 42, .45)
  }

  .role-modal-toolbar {
    display: flex;
    gap: 12px;
    align-items: flex-start;
    flex-wrap: wrap
  }

  .role-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 12px
  }

  .role-manager-layout {
    display: grid;
    grid-template-columns: minmax(260px, 320px) 1fr;
    gap: 18px
  }

  .role-manager-list,
  .role-manager-form {
    border: 1px solid #e2e8f0;
    border-radius: 18px;
    background: #fff;
    padding: 18px
  }

  .role-manager-list-head,
  .role-form-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 14px
  }

  .role-manager-items {
    display: flex;
    flex-direction: column;
    gap: 10px;
    max-height: 420px;
    overflow: auto
  }

  .role-manager-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    width: 100%;
    text-align: left;
    padding: 14px 16px;
    border: 1px solid #dbe4ef;
    border-radius: 14px;
    background: #f8fbff
  }

  .role-manager-item.active {
    border-color: #2563eb;
    background: linear-gradient(135deg, #eff6ff, #ffffff);
    box-shadow: 0 8px 20px rgba(37, 99, 235, .12)
  }

  .role-count-badge {
    background: #dbeafe;
    color: #1d4ed8;
    border: 1px solid #bfdbfe
  }

  .role-form-meta {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
    gap: 12px;
    margin-bottom: 16px
  }

  .meta-inline-card {
    padding: 14px 16px;
    border-radius: 14px;
    background: #f8fafc;
    border: 1px solid #e2e8f0
  }

  .meta-inline-label {
    display: block;
    font-size: 12px;
    text-transform: uppercase;
    color: #64748b;
    font-weight: 700;
    margin-bottom: 6px
  }

  .role-form-actions {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
    margin-top: 18px
  }

  .role-option {
    display: flex;
    gap: 10px;
    align-items: flex-start;
    padding: 14px 16px;
    border: 1px solid #e2e8f0;
    border-radius: 14px;
    background: #fff;
    cursor: pointer
  }

  .role-option:hover {
    border-color: #86efac;
    background: #f0fdf4
  }

  @media (max-width: 992px) {
    .filter-grid {
      grid-template-columns: 1fr
    }

    .role-manager-layout {
      grid-template-columns: 1fr
    }

    .permission-table {
      min-width: 900px
    }
  }
</style>
