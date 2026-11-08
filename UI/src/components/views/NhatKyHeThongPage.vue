<template>
  <BaseLayout>
    <div class="page-shell">
      <div class="page-header">
        <div class="gov-banner">
          <div class="gov-emblem">
            <i class="bi bi-clock-history"></i>
          </div>
          <div>
            <div class="gov-title">NHAT KY HE THONG</div>
          </div>
        </div>
        <div class="header-actions">
          <button type="button" class="btn btn-outline-secondary" @click="resetFilters">Dat lai</button>
          <button type="button" class="btn btn-primary" :disabled="loading" @click="fetchAuditLogs">
            <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
            Tai lai
          </button>
        </div>
      </div>

      <div class="summary-grid">
        <div class="summary-card">
          <span class="summary-label">Tong ban ghi</span>
          <strong class="summary-value">{{ totalItems }}</strong>
        </div>
        <div class="summary-card">
          <span class="summary-label">Trang hien tai</span>
          <strong class="summary-value text-primary">{{ page }}</strong>
        </div>
        <div class="summary-card">
          <span class="summary-label">Thanh cong</span>
          <strong class="summary-value text-success">{{ succeededCount }}</strong>
        </div>
        <div class="summary-card">
          <span class="summary-label">That bai</span>
          <strong class="summary-value text-danger">{{ failedCount }}</strong>
        </div>
      </div>

      <div class="card shadow-sm border-0 mb-3">
        <div class="card-body">
          <div class="filter-grid">
            <div>
              <label class="form-label">Hanh dong</label>
              <select v-model="filters.action" class="form-select" @change="goFirstPage">
                <option value="">Tat ca hanh dong</option>
                <option v-for="action in actionOptions" :key="action" :value="action">
                  {{ getActionLabel(action) }}
                </option>
              </select>
            </div>
            <div>
              <label class="form-label">Nguoi thao tac</label>
              <input
                v-model.trim="filters.actorUserId"
                type="text"
                class="form-control"
                placeholder="Nhap actor user id"
                @keyup.enter="goFirstPage"
              />
            </div>
            <div>
              <label class="form-label">Nguoi bi tac dong</label>
              <input
                v-model.trim="filters.targetUserId"
                type="text"
                class="form-control"
                placeholder="Nhap target user id"
                @keyup.enter="goFirstPage"
              />
            </div>
            <div>
              <label class="form-label">Tu ngay</label>
              <input v-model="filters.fromDate" type="datetime-local" class="form-control" @change="goFirstPage" />
            </div>
            <div>
              <label class="form-label">Den ngay</label>
              <input v-model="filters.toDate" type="datetime-local" class="form-control" @change="goFirstPage" />
            </div>
            <div>
              <label class="form-label">So dong</label>
              <select v-model.number="pageSize" class="form-select" @change="goFirstPage">
                <option :value="20">20</option>
                <option :value="50">50</option>
                <option :value="100">100</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <div v-if="errorMessage" class="alert alert-danger border-0 shadow-sm">
        {{ errorMessage }}
      </div>

      <div v-if="loading" class="loading-state">
        <div class="spinner-border text-primary"></div>
        <span>Dang tai nhat ky he thong...</span>
      </div>

      <div v-else-if="auditLogs.length" class="card shadow-sm border-0">
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0 audit-table">
              <thead>
                <tr>
                  <th>Thoi gian</th>
                  <th>Hanh dong</th>
                  <th>Trang thai</th>
                  <th>Nguoi thao tac</th>
                  <th>Doi tuong</th>
                  <th>IP</th>
                  <th class="text-end">Chi tiet</th>
                </tr>
              </thead>
              <tbody>
                <template v-for="item in auditLogs" :key="item.id">
                  <tr>
                    <td class="time-cell">
                      <strong>{{ formatDateTime(item.createdAt) }}</strong>
                    </td>
                    <td>
                      <span class="badge action-chip">{{ getActionLabel(item.action) }}</span>
                    </td>
                    <td>
                      <span class="badge" :class="item.succeeded ? 'success-chip' : 'danger-chip'">
                        {{ item.succeeded ? 'Thanh cong' : 'That bai' }}
                      </span>
                    </td>
                    <td>
                      <strong class="d-block">{{ item.actorUserName || '-' }}</strong>
                      <span class="muted-line">{{ item.actorUserId || '-' }}</span>
                    </td>
                    <td>
                      <strong class="d-block">{{ item.targetUserName || item.entityName || '-' }}</strong>
                      <span class="muted-line">{{ item.targetUserId || item.entityId || '-' }}</span>
                    </td>
                    <td>
                      <span class="ip-text">{{ item.ipAddress || '-' }}</span>
                    </td>
                    <td class="text-end">
                      <button type="button" class="btn btn-sm btn-outline-primary" @click="toggleDetails(item.id)">
                        {{ expandedLogId === item.id ? 'An' : 'Xem' }}
                      </button>
                    </td>
                  </tr>
                  <tr v-if="expandedLogId === item.id" class="detail-row">
                    <td colspan="7">
                      <div class="detail-panel">
                        <div>
                          <span class="detail-label">Details</span>
                          <strong>{{ item.details || '-' }}</strong>
                        </div>
                        <div>
                          <span class="detail-label">User agent</span>
                          <strong>{{ item.userAgent || '-' }}</strong>
                        </div>
                      </div>
                    </td>
                  </tr>
                </template>
              </tbody>
            </table>
          </div>

          <div class="table-footer">
            <div class="text-muted small">
              Hien thi {{ auditLogs.length }} / {{ totalItems }} ban ghi
            </div>
            <div class="pager">
              <button type="button" class="btn btn-outline-secondary btn-sm" :disabled="page <= 1" @click="changePage(page - 1)">
                Truoc
              </button>
              <span>Trang {{ page }} / {{ totalPages || 1 }}</span>
              <button
                type="button"
                class="btn btn-outline-secondary btn-sm"
                :disabled="page >= totalPages"
                @click="changePage(page + 1)"
              >
                Sau
              </button>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="alert alert-light border shadow-sm">
        Chua co ban ghi nhat ky phu hop.
      </div>
    </div>
  </BaseLayout>
</template>

<script setup>
  import { computed, onMounted, ref, watch } from 'vue'
  import BaseLayout from '../BaseLayout.vue'
  import httpClient from '../../services/httpClient'

  const actionOptions = [
    'LoginSucceeded',
    'LoginFailed',
    'LoginBlocked',
    'LoginLockedOut',
    'AccountLocked',
    'Logout',
    'RefreshTokenRotated',
    'RefreshTokenFailed',
    'RefreshTokenReuseDetected',
    'PasswordResetRequested',
    'PasswordResetCompleted',
    'PasswordResetFailed',
    'AdminPasswordResetTokenCreated',
    'AdminPasswordChanged',
    'UserCreated',
    'UserUpdated',
    'UserRoleAssigned',
    'UserRoleRemoved',
    'UserPermissionGranted',
    'UserPermissionDenied',
    'RoleCreated',
    'RoleUpdated',
    'RoleDeleted',
    'RolePermissionAssigned',
    'RolePermissionRemoved'
  ]

  const actionLabels = {
    LoginSucceeded: 'Dang nhap thanh cong',
    LoginFailed: 'Dang nhap that bai',
    LoginBlocked: 'Tai khoan bi chan',
    LoginLockedOut: 'Dang nhap khi bi khoa',
    AccountLocked: 'Khoa tai khoan tam thoi',
    Logout: 'Dang xuat',
    RefreshTokenRotated: 'Lam moi token',
    RefreshTokenFailed: 'Lam moi token that bai',
    RefreshTokenReuseDetected: 'Phat hien dung lai token',
    PasswordResetRequested: 'Yeu cau quen mat khau',
    PasswordResetCompleted: 'Doi mat khau thanh cong',
    PasswordResetFailed: 'Doi mat khau that bai',
    AdminPasswordResetTokenCreated: 'Admin tao token doi mat khau',
    AdminPasswordChanged: 'Admin doi mat khau',
    UserCreated: 'Tao nguoi dung',
    UserUpdated: 'Cap nhat nguoi dung',
    UserRoleAssigned: 'Gan vai tro user',
    UserRoleRemoved: 'Xoa vai tro user',
    UserPermissionGranted: 'Cap quyen user',
    UserPermissionDenied: 'Chan quyen user',
    RoleCreated: 'Tao vai tro',
    RoleUpdated: 'Cap nhat vai tro',
    RoleDeleted: 'Xoa vai tro',
    RolePermissionAssigned: 'Gan quyen vao vai tro',
    RolePermissionRemoved: 'Xoa quyen khoi vai tro'
  }

  const auditLogs = ref([])
  const loading = ref(false)
  const errorMessage = ref('')
  const page = ref(1)
  const pageSize = ref(50)
  const totalItems = ref(0)
  const totalPages = ref(0)
  const expandedLogId = ref(null)
  const filters = ref({
    action: '',
    actorUserId: '',
    targetUserId: '',
    fromDate: '',
    toDate: ''
  })

  const succeededCount = computed(() => auditLogs.value.filter(item => item.succeeded).length)
  const failedCount = computed(() => auditLogs.value.filter(item => !item.succeeded).length)

  const normalizeAuditLog = (item) => ({
    id: item?.id ?? item?.Id,
    action: item?.action ?? item?.Action ?? '',
    succeeded: Boolean(item?.succeeded ?? item?.Succeeded),
    actorUserId: item?.actorUserId ?? item?.ActorUserId ?? '',
    actorUserName: item?.actorUserName ?? item?.ActorUserName ?? '',
    targetUserId: item?.targetUserId ?? item?.TargetUserId ?? '',
    targetUserName: item?.targetUserName ?? item?.TargetUserName ?? '',
    entityName: item?.entityName ?? item?.EntityName ?? '',
    entityId: item?.entityId ?? item?.EntityId ?? '',
    details: item?.details ?? item?.Details ?? '',
    ipAddress: item?.ipAddress ?? item?.IpAddress ?? '',
    userAgent: item?.userAgent ?? item?.UserAgent ?? '',
    createdAt: item?.createdAt ?? item?.CreatedAt ?? null
  })

  const getActionLabel = (action) => actionLabels[action] || action || '-'

  const toUtcIso = (value) => {
    if (!value) return null
    const date = new Date(value)
    return Number.isNaN(date.getTime()) ? null : date.toISOString()
  }

  const fetchAuditLogs = async () => {
    loading.value = true
    errorMessage.value = ''

    try {
      const params = {
        page: page.value,
        pageSize: pageSize.value
      }

      if (filters.value.action) params.action = filters.value.action
      if (filters.value.actorUserId) params.actorUserId = filters.value.actorUserId
      if (filters.value.targetUserId) params.targetUserId = filters.value.targetUserId

      const fromUtc = toUtcIso(filters.value.fromDate)
      const toUtc = toUtcIso(filters.value.toDate)
      if (fromUtc) params.fromUtc = fromUtc
      if (toUtc) params.toUtc = toUtc

      const res = await httpClient.get('/audit-logs', { params })
      const payload = res.data || {}
      const items = payload.items || payload.Items || []

      auditLogs.value = Array.isArray(items) ? items.map(normalizeAuditLog) : []
      totalItems.value = Number(payload.totalItems ?? payload.TotalItems ?? 0)
      totalPages.value = Number(payload.totalPages ?? payload.TotalPages ?? 0)
      page.value = Number(payload.page ?? payload.Page ?? page.value)
      pageSize.value = Number(payload.pageSize ?? payload.PageSize ?? pageSize.value)
      expandedLogId.value = null
    } catch (err) {
      console.error(err)
      errorMessage.value =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        err?.response?.data?.title ||
        'Khong tai duoc nhat ky he thong.'
    } finally {
      loading.value = false
    }
  }

  const goFirstPage = () => {
    page.value = 1
    fetchAuditLogs()
  }

  const resetFilters = () => {
    filters.value = {
      action: '',
      actorUserId: '',
      targetUserId: '',
      fromDate: '',
      toDate: ''
    }
    page.value = 1
    fetchAuditLogs()
  }

  const changePage = (nextPage) => {
    if (nextPage < 1 || (totalPages.value && nextPage > totalPages.value)) return
    page.value = nextPage
    fetchAuditLogs()
  }

  const toggleDetails = (id) => {
    expandedLogId.value = expandedLogId.value === id ? null : id
  }

  const formatDateTime = (value) => {
    if (!value) return '-'
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return '-'

    return date.toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    })
  }

  watch(pageSize, () => {
    page.value = 1
  })

  onMounted(fetchAuditLogs)
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

  .header-actions {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
  }

  .gov-banner {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 20px 24px;
    border-radius: 20px;
    background: linear-gradient(135deg, #ffffff 0%, #f4f9ff 100%);
    box-shadow: 0 10px 30px rgba(13, 110, 253, .08);
    border: 1px solid rgba(13, 110, 253, .08);
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
    box-shadow: 0 10px 24px rgba(13, 110, 253, .24);
  }

  .gov-title {
    font-size: 1.3rem;
    font-weight: 800;
    color: #1f2d3d;
    margin-bottom: 4px;
  }

  .summary-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 14px;
    margin-bottom: 18px;
  }

  .summary-card {
    padding: 16px 18px;
    border-radius: 16px;
    background: #fff;
    border: 1px solid #e9ecef;
    box-shadow: 0 10px 20px rgba(15, 23, 42, .05);
  }

  .summary-label {
    display: block;
    font-size: 12px;
    color: #64748b;
    text-transform: uppercase;
    font-weight: 700;
    margin-bottom: 6px;
  }

  .summary-value {
    font-size: 28px;
    line-height: 1;
  }

  .filter-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(180px, 1fr));
    gap: 14px;
  }

  .loading-state {
    display: flex;
    align-items: center;
    gap: 10px;
    color: #475569;
    padding: 18px 0;
  }

  .audit-table {
    min-width: 1120px;
  }

  .audit-table thead th {
    background: #f8fafc;
    border-bottom: 1px solid #dbe4ef;
    color: #0f172a;
    font-size: 13px;
    text-transform: uppercase;
    letter-spacing: .03em;
  }

  .audit-table td,
  .audit-table th {
    padding: 16px;
    border-color: #eef2f7;
  }

  .time-cell strong {
    white-space: nowrap;
  }

  .muted-line {
    display: block;
    font-size: 12px;
    color: #64748b;
    margin-top: 3px;
    max-width: 240px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .ip-text {
    font-family: Consolas, "Courier New", monospace;
    font-size: 13px;
  }

  .action-chip {
    color: #1d4ed8;
    background: #dbeafe;
    border: 1px solid #bfdbfe;
  }

  .success-chip {
    color: #166534;
    background: #dcfce7;
    border: 1px solid #bbf7d0;
  }

  .danger-chip {
    color: #991b1b;
    background: #fee2e2;
    border: 1px solid #fecaca;
  }

  .detail-row td {
    background: #f8fafc;
  }

  .detail-panel {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
    padding: 14px;
    border-radius: 14px;
    background: #fff;
    border: 1px solid #e2e8f0;
  }

  .detail-label {
    display: block;
    color: #64748b;
    font-size: 12px;
    text-transform: uppercase;
    font-weight: 800;
    margin-bottom: 6px;
  }

  .detail-panel strong {
    display: block;
    color: #0f172a;
    font-size: 13px;
    overflow-wrap: anywhere;
  }

  .table-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
    padding: 16px;
    border-top: 1px solid #e2e8f0;
  }

  .pager {
    display: flex;
    align-items: center;
    gap: 10px;
    color: #475569;
    font-weight: 700;
  }

  @media (max-width: 992px) {
    .filter-grid,
    .detail-panel {
      grid-template-columns: 1fr;
    }
  }
</style>
