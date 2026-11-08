import axios from "axios"

const BASE_URL =
    import.meta.env.VITE_API_BASE_URL ||
    "/api"

const httpClient = axios.create({
    baseURL: BASE_URL
})

httpClient.interceptors.request.use((config) => {
    const token = localStorage.getItem("token")

    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }

    return config
})

export default httpClient
