import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type PropsWithChildren } from "react"
import { getCurrentAuthProfile } from "../apis/authApi"
import { type AuthTokenResponse } from "../types/Auth"
import {
  AUTH_STATE_CHANGED_EVENT,
  clearAuthState,
  persistAuthState,
  readAuthState,
  type PersistAuthProfileInput,
  type StoredAuthState,
} from "../lib/authSession"

type AuthContextValue = {
  authState: StoredAuthState | null
  isAuthenticated: boolean
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
  const [authState, setAuthState] = useState<StoredAuthState | null>(() => readAuthState())
  const [isResolvingProfile, setIsResolvingProfile] = useState(false)
  const profileSyncTokenRef = useRef<string | null>(null)

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

  useEffect(() => {
    const syncAuthState = () => setAuthState(readAuthState())

    const onStorage = (event: StorageEvent) => {
      if (!event.key || event.key.startsWith("ctb.auth.")) {
        syncAuthState()
      }
    }

    window.addEventListener("storage", onStorage)
    window.addEventListener(AUTH_STATE_CHANGED_EVENT, syncAuthState)
    return () => {
      window.removeEventListener("storage", onStorage)
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

  const value = useMemo<AuthContextValue>(
    () => ({
      authState,
      isAuthenticated: Boolean(authState),
      isResolvingProfile,
      displayName: authState?.profile.displayName ?? null,
      email: authState?.profile.email ?? null,
      avatarUrl: authState?.profile.avatarUrl ?? null,
      phoneNumber: authState?.profile.phoneNumber ?? null,
      customerId: authState?.session.customerId ?? null,
      setAuthFromTokens,
      refreshProfile,
      clearAuth,
    }),
    [authState, clearAuth, isResolvingProfile, refreshProfile, setAuthFromTokens],
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
