const BASE_URL =
    import.meta.env.VITE_API_BASE_URL ||
    "/api"

async function parseResponse(res) {
    if (res.status === 204) {
        return null
    }

    const contentType = res.headers.get("content-type") || ""

    if (contentType.includes("application/json")) {
        return res.json()
    }

    const text = await res.text()
    return text ? { message: text } : null
}

export async function apiRequest(endpoint, method = "GET", body = null) {
    const token = localStorage.getItem("token")

    const res = await fetch(BASE_URL + endpoint, {
        method,
        headers: {
            "Content-Type": "application/json",
            ...(token && { Authorization: `Bearer ${token}` })
        },
        body: body ? JSON.stringify(body) : null
    })

    const payload = await parseResponse(res)

    if (!res.ok) {
        throw new Error(
            payload?.message ||
            payload?.Message ||
            payload?.title ||
            "API error"
        )
    }

    return payload
}
