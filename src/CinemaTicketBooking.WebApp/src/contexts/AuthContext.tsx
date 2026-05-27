import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type PropsWithChildren } from "react"
import { getCurrentAuthProfile } from "../apis/authApi"
import { performTokenRefresh } from "../apis/httpClient"
import { type AuthTokenResponse } from "../types/Auth"
import {
  AUTH_STATE_CHANGED_EVENT,
  clearAuthState,
  hasRefreshCookieHint,
  persistAuthState,
  readAuthState,
  readCachedProfile,
  type PersistAuthProfileInput,
  type StoredAuthState,
} from "../lib/authSession"

type AuthContextValue = {
  authState: StoredAuthState | null
  isAuthenticated: boolean
  isHydrating: boolean
  isResolvingProfile: boolean
  displayName: string | null
  email: string | null
  avatarUrl: string | null
  phoneNumber: string | null
  customerId: string | null
  setAuthFromTokens: (tokens: AuthTokenResponse, profileInput?: PersistAuthProfileInput) => Promise<void>
  refreshProfile: () => Promise<void>
  clearAuth: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: PropsWithChildren) {
  // In-memory session is empty on mount after reload — start as null
  const [authState, setAuthState] = useState<StoredAuthState | null>(() => readAuthState())
  const [isHydrating, setIsHydrating] = useState(false)
  const [isResolvingProfile, setIsResolvingProfile] = useState(false)
  const profileSyncTokenRef = useRef<string | null>(null)
  const hydrationAttemptedRef = useRef(false)

  // =============================================
  // Hydration: on mount, use HttpOnly cookie to restore in-memory session
  // =============================================

  useEffect(() => {
    // Only hydrate once, and only if we have a login hint but no in-memory session
    if (hydrationAttemptedRef.current) return
    if (readAuthState()) return // already have in-memory session (e.g. SPA navigation)
    if (!hasRefreshCookieHint()) return // user was never logged in

    hydrationAttemptedRef.current = true
    setIsHydrating(true)

    void performTokenRefresh().then((newToken) => {
      if (newToken) {
        // performTokenRefresh already called persistAuthState → in-memory is populated
        setAuthState(readAuthState())
      }
      setIsHydrating(false)
    })
  }, [])

  const refreshProfile = useCallback(async () => {
    const current = readAuthState()
    if (!current) {
      return
    }

    setIsResolvingProfile(true)
    try {
      const profile = await getCurrentAuthProfile()
      const nextState = persistAuthState(
        {
          accessToken: current.session.accessToken,
          accessTokenExpiresAtUtc: current.session.accessTokenExpiresAtUtc,
          accountId: current.session.accountId,
          refreshToken: current.session.refreshToken,
        },
        {
          displayName: profile.displayName,
          email: profile.email,
          avatarUrl: profile.avatarUrl,
          customerId: profile.customerId,
          phoneNumber: profile.phoneNumber,
        },
      )
      profileSyncTokenRef.current = nextState.session.accessToken
      setAuthState(nextState)
    } catch (err: unknown) {
      // If the server returned 401 after the httpClient interceptor already
      // attempted a token refresh, the session is truly expired → force logout.
      if (
        err &&
        typeof err === "object" &&
        "response" in err &&
        (err as { response?: { status?: number } }).response?.status === 401
      ) {
        clearAuthState()
        setAuthState(null)
        return
      }
      // Keep existing local profile when profile endpoint is unavailable for other reasons.
    } finally {
      setIsResolvingProfile(false)
    }
  }, [])

  // =============================================
  // Sync auth state across in-memory changes (same tab)
  // =============================================

  useEffect(() => {
    const syncAuthState = () => setAuthState(readAuthState())

    window.addEventListener(AUTH_STATE_CHANGED_EVENT, syncAuthState)
    return () => {
      window.removeEventListener(AUTH_STATE_CHANGED_EVENT, syncAuthState)
    }
  }, [])

  const setAuthFromTokens = useCallback(async (tokens: AuthTokenResponse, profileInput?: PersistAuthProfileInput) => {
    const nextState = persistAuthState(tokens, profileInput)
    profileSyncTokenRef.current = null
    setAuthState(nextState)
    await refreshProfile()
  }, [refreshProfile])

  useEffect(() => {
    if (!authState) {
      profileSyncTokenRef.current = null
      return
    }

    const shouldHydrate =
      !authState.session.customerId ||
      authState.profile.displayName.includes("@") ||
      authState.profile.displayName.trim().length === 0

    if (!shouldHydrate) {
      return
    }

    if (profileSyncTokenRef.current === authState.session.accessToken) {
      return
    }

    void refreshProfile()
  }, [authState, refreshProfile])

  const clearAuth = useCallback(() => {
    clearAuthState()
    setAuthState(null)
  }, [])

  // Use cached profile for optimistic display during hydration
  const cachedProfile = isHydrating ? readCachedProfile() : null

  const value = useMemo<AuthContextValue>(
    () => ({
      authState,
      isAuthenticated: Boolean(authState) || isHydrating,
      isHydrating,
      isResolvingProfile,
      displayName: authState?.profile.displayName ?? cachedProfile?.displayName ?? null,
      email: authState?.profile.email ?? cachedProfile?.email ?? null,
      avatarUrl: authState?.profile.avatarUrl ?? cachedProfile?.avatarUrl ?? null,
      phoneNumber: authState?.profile.phoneNumber ?? cachedProfile?.phoneNumber ?? null,
      customerId: authState?.session.customerId ?? null,
      setAuthFromTokens,
      refreshProfile,
      clearAuth,
    }),
    [authState, cachedProfile, clearAuth, isHydrating, isResolvingProfile, refreshProfile, setAuthFromTokens],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider")
  }
  return context
}
