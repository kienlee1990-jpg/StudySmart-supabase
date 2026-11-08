<template>
  <BaseLayout>
    <div class="page-shell">
      <div class="page-header">
        <div class="gov-banner">
          <div class="gov-emblem">
            <i class="bi bi-key-fill"></i>
          </div>
          <div>
            <div class="gov-title">CẤP LẠI MẬT KHẨU</div>
          </div>
        </div>
      </div>

      <div class="card shadow-sm border-0 mb-3">
        <div class="card-body">
          <div class="filter-grid">
            <div>
              <label class="form-label">Từ khóa</label>
              <input
                v-model.trim="searchTerm"
                type="text"
                class="form-control"
                placeholder="Tên đăng nhập, email, tên hiển thị..."
              />
            </div>
            <div>
              <label class="form-label">Trạng thái</label>
              <select v-model="statusFilter" class="form-select">
                <option value="ALL">Tất cả</option>
                <option value="ACTIVE">Đang hoạt động</option>
                <option value="INACTIVE">Ngừng hoạt động</option>
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
          <ColumnVisibilityTools table-id="CapLaiMatKhauPage-table" />
          <table id="CapLaiMatKhauPage-table" class="table table-hover align-middle mb-0 managed-table">
            <thead>
              <tr>
                <th>Tên đăng nhập</th>
                <th>Tên hiển thị</th>
                <th>Email</th>
                <th>Trạng thái</th>
                <th class="text-end">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in filteredUsers" :key="item.id">
                <td>{{ item.userName || "-" }}</td>
                <td>{{ item.fullName || "-" }}</td>
                <td>{{ item.email || "-" }}</td>
                <td>
                  <span class="badge rounded-pill" :class="item.isActive ? 'text-bg-success' : 'text-bg-secondary'">
                    {{ item.isActive ? "Đang hoạt động" : "Ngừng hoạt động" }}
                  </span>
                </td>
                <td class="text-end">
                  <button type="button" class="btn btn-sm btn-primary" @click="openResetModal(item)">
                    Cấp lại mật khẩu
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-else class="alert alert-light border shadow-sm">
        Không tìm thấy người dùng phù hợp.
      </div>

      <div v-if="showModal" class="modal fade show d-block">
        <div class="modal-dialog modal-dialog-centered modal-lg">
          <div class="modal-content shadow-lg border-0 rounded-4">
            <div class="modal-header bg-primary text-white rounded-top-4">
              <div>
                <h5 class="modal-title mb-1">Cấp lại mật khẩu</h5>
                <div class="small opacity-75">{{ selectedUser?.email || "-" }}</div>
              </div>
              <button class="btn-close btn-close-white" @click="closeModal"></button>
            </div>
            <div class="modal-body px-4 py-3">
              <div class="row g-3">
                <div class="col-md-6">
                  <label class="form-label">Tên đăng nhập</label>
                  <input :value="selectedUser?.userName || '-'" type="text" class="form-control" readonly />
                </div>
                <div class="col-md-6">
                  <label class="form-label">Email</label>
                  <input :value="selectedUser?.email || '-'" type="text" class="form-control" readonly />
                </div>
                <div class="col-md-6">
                  <label class="form-label">Tên hiển thị</label>
                  <input :value="selectedUser?.fullName || '-'" type="text" class="form-control" readonly />
                </div>
                <div class="col-12">
                  <label class="form-label">Token reset</label>
                  <div class="token-row">
                    <input v-model="resetForm.token" type="text" class="form-control" readonly />
                    <button
                      type="button"
                      class="btn btn-outline-primary"
                      :disabled="submitting || !selectedUser?.id"
                      @click="requestResetToken"
                    >
                      {{ resetForm.token ? "Lấy lại token" : "Lấy token reset" }}
                    </button>
                  </div>
                </div>
                <div class="col-12">
                  <label class="form-label">Mật khẩu mới</label>
                  <input
                    v-model="resetForm.newPassword"
                    type="password"
                    class="form-control"
                    placeholder="Nhập mật khẩu mới"
                    autocomplete="new-password"
                  />
                </div>
              </div>
            </div>
            <div class="modal-footer border-0 px-4 pb-4">
              <button class="btn btn-outline-secondary rounded-pill px-4" @click="closeModal">Hủy</button>
              <button class="btn btn-primary rounded-pill px-4" :disabled="submitting" @click="submitResetPassword">
                {{ submitting ? "Đang xử lý..." : "Đặt lại mật khẩu" }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </BaseLayout>
</template>

<script setup>
  import { computed, onMounted, ref } from 'vue'
  import BaseLayout from '../BaseLayout.vue'
  import ColumnVisibilityTools from '../shared/ColumnVisibilityTools.vue'
  import httpClient from '../../services/httpClient'

  const users = ref([])
  const loading = ref(false)
  const submitting = ref(false)
  const searchTerm = ref('')
  const statusFilter = ref('ALL')
  const showModal = ref(false)
  const selectedUser = ref(null)
  const resetForm = ref({
    token: '',
    newPassword: ''
  })

  const filteredUsers = computed(() => {
    const keyword = String(searchTerm.value || '').trim().toLowerCase()

    return users.value.filter(item => {
      const matchKeyword =
        !keyword ||
        [
          item.userName,
          item.fullName,
          item.email
        ].some(value => String(value || '').toLowerCase().includes(keyword))

      const matchStatus =
        statusFilter.value === 'ALL' ||
        (statusFilter.value === 'ACTIVE' && item.isActive) ||
        (statusFilter.value === 'INACTIVE' && !item.isActive)

      return matchKeyword && matchStatus
    })
  })

  const getErrorMessage = (error, fallback) =>
    error.response?.data?.message ||
    error.response?.data?.Message ||
    error.response?.data?.title ||
    error.message ||
    fallback

  const fetchUsers = async () => {
    loading.value = true
    try {
      const res = await httpClient.get('/users')
      users.value = Array.isArray(res.data) ? res.data : []
    } catch (error) {
      console.error(error)
      alert(getErrorMessage(error, 'Không tải được danh sách người dùng.'))
    } finally {
      loading.value = false
    }
  }

  const openResetModal = (user) => {
    selectedUser.value = user
    resetForm.value = {
      token: '',
      newPassword: ''
    }
    showModal.value = true
  }

  const closeModal = () => {
    showModal.value = false
    selectedUser.value = null
    resetForm.value = {
      token: '',
      newPassword: ''
    }
  }

  const requestResetToken = async () => {
    if (!selectedUser.value?.id) {
      alert('Không xác định được tài khoản.')
      return
    }

    try {
      submitting.value = true
      const { data: result } = await httpClient.post(`/users/${selectedUser.value.id}/password-reset-tokens`)
      resetForm.value.token = result?.Token || result?.token || ''
      if (!resetForm.value.token) {
        throw new Error('API không trả về token reset.')
      }
    } catch (error) {
      console.error(error)
      alert(getErrorMessage(error, 'Không lấy được token reset.'))
    } finally {
      submitting.value = false
    }
  }

  const submitResetPassword = async () => {
    if (!selectedUser.value?.id) {
      alert('Không xác định được tài khoản.')
      return
    }

    if (!resetForm.value.token) {
      alert('Bạn cần lấy token reset trước khi đặt lại mật khẩu.')
      return
    }

    if (!resetForm.value.newPassword) {
      alert('Vui lòng nhập mật khẩu mới.')
      return
    }

    try {
      submitting.value = true
      await httpClient.put(`/users/${selectedUser.value.id}/password`, {
        token: resetForm.value.token,
        newPassword: resetForm.value.newPassword
      })
      alert('Đặt lại mật khẩu thành công.')
      closeModal()
    } catch (error) {
      console.error(error)
      alert(getErrorMessage(error, 'Đặt lại mật khẩu thất bại.'))
    } finally {
      submitting.value = false
    }
  }

  onMounted(fetchUsers)
</script>

<style scoped>
  .page-shell {
    padding: 20px;
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
    margin-bottom: 18px;
  }

  .gov-banner {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 20px 24px;
    border-radius: 20px;
    background: linear-gradient(135deg, #ffffff 0%, #f4f9ff 100%);
    box-shadow: 0 10px 30px rgba(13, 110, 253, 0.08);
    border: 1px solid rgba(13, 110, 253, 0.08);
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
    box-shadow: 0 10px 24px rgba(13, 110, 253, 0.24);
  }

  .gov-title {
    font-size: 1.3rem;
    font-weight: 800;
    color: #1f2d3d;
    margin-bottom: 4px;
  }

  .filter-grid {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 14px;
  }

  .loading-state {
    display: flex;
    align-items: center;
    gap: 10px;
    color: #475569;
    padding: 18px 0;
  }

  .modal {
    background: rgba(15, 23, 42, 0.45);
  }

  .token-row {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 10px;
  }

  @media (max-width: 992px) {
    .filter-grid {
      grid-template-columns: 1fr;
    }

    .table {
      min-width: 960px;
    }

    .token-row {
      grid-template-columns: 1fr;
    }
  }
</style>
