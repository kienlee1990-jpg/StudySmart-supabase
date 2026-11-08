import { apiRequest } from "./api"

export const loginApi = (data) => {
    return apiRequest("/auth/login", "POST", data)
}

export const getMe = () => {
    return apiRequest("/auth/me", "GET")
}

export const logoutApi = (refreshToken) => {
    return apiRequest("/auth/logout", "DELETE", { refreshToken })
}

export const requestPasswordResetApi = (data) => {
    return apiRequest("/auth/forgot-password", "POST", data)
}

export const resetPasswordApi = (data) => {
    return apiRequest("/auth/reset-password", "POST", data)
}
