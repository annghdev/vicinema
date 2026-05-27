import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios"
import { readAuthState, persistAuthState, clearAuthState, hasRefreshCookieHint } from "../lib/authSession"

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ""

export const httpClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 100000,
})

// =============================================
// Global error handler
// =============================================

type GlobalErrorHandler = (msg: string) => void
let onGlobalError: GlobalErrorHandler | null = null

export function registerGlobalErrorHandler(handler: GlobalErrorHandler) {
  onGlobalError = handler
}

// =============================================
// Token refresh state (prevent concurrent refreshes)
// =============================================

let isRefreshing = false
let pendingQueue: {
  resolve: (token: string | null) => void
  reject: (err: unknown) => void
}[] = []

function processPendingQueue(token: string | null, error: unknown = null) {
  for (const p of pendingQueue) {
    if (error) {
      p.reject(error)
    } else {
      p.resolve(token)
    }
  }
  pendingQueue = []
}

/** Minimum remaining seconds before proactively refreshing the access token. */
const PROACTIVE_REFRESH_THRESHOLD_S = 60

function isAccessTokenExpiringSoon(): boolean {
  const state = readAuthState()
  if (!state?.session.accessTokenExpiresAtUtc) return false
  const expiresAt = new Date(state.session.accessTokenExpiresAtUtc).getTime()
  const now = Date.now()
  return expiresAt - now < PROACTIVE_REFRESH_THRESHOLD_S * 1000
}

// =============================================
// Token refresh helper (queue-aware singleton)
// =============================================

/**
 * Calls POST /api/auth/refresh with HttpOnly cookie credentials.
 * Uses an internal mutex so concurrent callers (proactive refresh,
 * 401-retry) are serialized — only one HTTP request is made, and
 * all waiters receive the same result.
 */
export async function performTokenRefresh(): Promise<string | null> {
  // If already refreshing, queue this caller and wait for the result
  if (isRefreshing) {
    return new Promise<string | null>((resolve, reject) => {
      pendingQueue.push({ resolve, reject })
    })
  }

  isRefreshing = true
  try {
    const response = await axios.post(
      `${apiBaseUrl}/api/auth/refresh`,
      undefined,
      { withCredentials: true, timeout: 10000 }
    )

    const data = response.data as {
      accessToken: string
      accessTokenExpiresAtUtc: string
      accountId: string
      refreshToken: string | null
    }

    // Persist new tokens (in-memory session + localStorage profile)
    persistAuthState(data)
    processPendingQueue(data.accessToken)

    return data.accessToken
  } catch (err) {
    processPendingQueue(null, err)
    // Refresh token invalid or expired — clear session
    clearAuthState()
    return null
  } finally {
    isRefreshing = false
  }
}

// =============================================
// Request interceptor — attach Bearer token + proactive refresh
// =============================================

httpClient.interceptors.request.use(async (config) => {
  // Skip refresh endpoint to avoid infinite loop
  if (config.url?.includes("/api/auth/refresh") || config.url?.includes("/api/auth/login")) {
    const accessToken = readAuthState()?.session.accessToken
    if (accessToken) {
      config.headers = config.headers ?? {}
      if (!config.headers.Authorization) {
        config.headers.Authorization = `Bearer ${accessToken}`
      }
    }
    return config
  }

  // Proactively refresh if token is about to expire (serialized via queue)
  if (readAuthState() && isAccessTokenExpiringSoon()) {
    try {
      const newToken = await performTokenRefresh()
      if (newToken) {
        config.headers = config.headers ?? {}
        config.headers.Authorization = `Bearer ${newToken}`
        return config
      }
    } catch {
      // Fall through to use existing token
    }
  }

  const accessToken = readAuthState()?.session.accessToken
  if (accessToken) {
    config.headers = config.headers ?? {}
    if (!config.headers.Authorization) {
      config.headers.Authorization = `Bearer ${accessToken}`
    }
  }
  return config
})

// =============================================
// Response interceptor — handle 401 with token refresh
// =============================================

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retried?: boolean }

    // 1. Handle 401 — attempt token refresh (serialized via queue inside performTokenRefresh)
    //    Check readAuthState() (in-memory) OR hasRefreshCookieHint() (localStorage flag)
    //    to handle the case where page just reloaded and in-memory is empty but cookie exists.
    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retried &&
      !originalRequest.url?.includes("/api/auth/refresh") &&
      !originalRequest.url?.includes("/api/auth/login") &&
      (readAuthState() || hasRefreshCookieHint())
    ) {
      originalRequest._retried = true

      try {
        const newToken = await performTokenRefresh()

        if (newToken) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`
          return httpClient(originalRequest)
        }
      } catch (refreshError) {
        clearAuthState()
        return Promise.reject(refreshError)
      }
    }

    // 2. Handle network errors
    if (!error.response) {
      if (onGlobalError) {
        onGlobalError("Không thể kết nối đến máy chủ. Vui lòng kiểm tra đường truyền.")
      }
    } else if (error.response.status >= 500) {
      if (onGlobalError) {
        onGlobalError("Lỗi hệ thống. Vui lòng thử lại sau.")
      }
    }

    return Promise.reject(error)
  }
)
