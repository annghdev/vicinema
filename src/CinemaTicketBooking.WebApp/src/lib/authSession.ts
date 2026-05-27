import type { AuthTokenResponse } from "../types/Auth"

export type StoredAuthSession = {
  accessToken: string
  accessTokenExpiresAtUtc: string
  accountId: string
  customerId: string | null
  refreshToken: string | null
}

export type StoredAuthProfile = {
  displayName: string
  email: string | null
  avatarUrl: string | null
  phoneNumber?: string | null
}

export type StoredAuthState = {
  session: StoredAuthSession
  profile: StoredAuthProfile
}

export type PersistAuthProfileInput = {
  displayName?: string | null
  email?: string | null
  avatarUrl?: string | null
  customerId?: string | null
  phoneNumber?: string | null
}

export const AUTH_PROFILE_KEY = "ctb.auth.profile"
const AUTH_LOGGED_IN_HINT_KEY = "ctb.auth.logged-in"
export const AUTH_STATE_CHANGED_EVENT = "ctb:auth-state-changed"

// =============================================
// In-memory session storage (XSS-safe)
// =============================================

/**
 * Access token and session data live here — never in localStorage.
 * Lost on page reload; re-hydrated via HttpOnly cookie refresh.
 */
let inMemorySession: StoredAuthSession | null = null

// =============================================
// Migration: remove legacy localStorage session key
// =============================================

const LEGACY_SESSION_KEY = "ctb.auth.session"
if (window.localStorage.getItem(LEGACY_SESSION_KEY)) {
  window.localStorage.removeItem(LEGACY_SESSION_KEY)
  // Preserve login hint so hydration will restore via cookie
  window.localStorage.setItem(AUTH_LOGGED_IN_HINT_KEY, "1")
}

// =============================================
// JWT helpers
// =============================================

function decodeBase64Url(input: string): string | null {
  try {
    const normalized = input.replace(/-/g, "+").replace(/_/g, "/")
    const pad = normalized.length % 4 === 0 ? "" : "=".repeat(4 - (normalized.length % 4))
    return window.atob(normalized + pad)
  } catch {
    return null
  }
}

function parseJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split(".")
  if (parts.length < 2) {
    return null
  }
  const decoded = decodeBase64Url(parts[1] ?? "")
  if (!decoded) {
    return null
  }
  try {
    return JSON.parse(decoded) as Record<string, unknown>
  } catch {
    return null
  }
}

function getClaim(payload: Record<string, unknown> | null, ...keys: string[]): string | null {
  if (!payload) {
    return null
  }
  for (const key of keys) {
    const raw = payload[key]
    if (typeof raw === "string" && raw.trim().length > 0) {
      return raw.trim()
    }
  }
  return null
}

function deriveProfile(tokens: AuthTokenResponse, input?: PersistAuthProfileInput): StoredAuthProfile {
  const jwtPayload = parseJwtPayload(tokens.accessToken)
  const cachedProfile = readCachedProfile()

  const displayName =
    input?.displayName?.trim() ||
    cachedProfile?.displayName ||
    getClaim(jwtPayload, "name", "given_name", "unique_name") ||
    input?.email?.trim() ||
    getClaim(jwtPayload, "email") ||
    "Khách hàng"

  const email = input?.email?.trim() || cachedProfile?.email || getClaim(jwtPayload, "email")
  const avatarUrl = input?.avatarUrl?.trim() || cachedProfile?.avatarUrl || getClaim(jwtPayload, "picture")
  const phoneNumber = input?.phoneNumber?.trim() || cachedProfile?.phoneNumber

  return {
    displayName,
    email: email ?? null,
    avatarUrl: avatarUrl ?? null,
    phoneNumber: phoneNumber ?? null,
  }
}

function deriveCustomerId(accessToken: string): string | null {
  const jwtPayload = parseJwtPayload(accessToken)
  return getClaim(jwtPayload, "customer_id", "customerId")
}

function safeParseJson<T>(raw: string | null): T | null {
  if (!raw) {
    return null
  }
  try {
    return JSON.parse(raw) as T
  } catch {
    return null
  }
}

function notifyAuthStateChanged() {
  window.dispatchEvent(new Event(AUTH_STATE_CHANGED_EVENT))
}

// =============================================
// Public API
// =============================================

/**
 * Persists auth state: session data goes to in-memory variable,
 * profile data goes to localStorage (non-sensitive display info),
 * and a logged-in hint flag is set in localStorage for reload detection.
 */
export function persistAuthState(tokens: AuthTokenResponse, profileInput?: PersistAuthProfileInput): StoredAuthState {
  const session: StoredAuthSession = {
    accessToken: tokens.accessToken,
    accessTokenExpiresAtUtc: tokens.accessTokenExpiresAtUtc,
    accountId: tokens.accountId,
    customerId: profileInput?.customerId?.trim() || deriveCustomerId(tokens.accessToken),
    refreshToken: tokens.refreshToken,
  }
  const profile = deriveProfile(tokens, profileInput)

  // 1. Session → in-memory only (XSS-safe)
  inMemorySession = session

  // 2. Profile → localStorage (non-sensitive display data)
  window.localStorage.setItem(AUTH_PROFILE_KEY, JSON.stringify(profile))

  // 3. Login hint → localStorage (tells us to try refresh on reload)
  window.localStorage.setItem(AUTH_LOGGED_IN_HINT_KEY, "1")

  notifyAuthStateChanged()

  return { session, profile }
}

/**
 * Reads current auth state from in-memory session + localStorage profile.
 * Returns null if the in-memory session is empty (e.g. after page reload
 * before hydration completes).
 */
export function readAuthState(): StoredAuthState | null {
  if (!inMemorySession) {
    return null
  }
  const normalizedSession: StoredAuthSession = {
    ...inMemorySession,
    customerId: inMemorySession.customerId ?? deriveCustomerId(inMemorySession.accessToken),
  }
  // Update in-memory with normalized version
  inMemorySession = normalizedSession

  const profile = safeParseJson<StoredAuthProfile>(window.localStorage.getItem(AUTH_PROFILE_KEY))
  if (profile) {
    return { session: normalizedSession, profile }
  }
  const reconstructedProfile = deriveProfile(
    {
      accessToken: normalizedSession.accessToken,
      accessTokenExpiresAtUtc: normalizedSession.accessTokenExpiresAtUtc,
      accountId: normalizedSession.accountId,
      refreshToken: normalizedSession.refreshToken,
    },
    undefined,
  )
  window.localStorage.setItem(AUTH_PROFILE_KEY, JSON.stringify(reconstructedProfile))
  return { session: normalizedSession, profile: reconstructedProfile }
}

/**
 * Clears all auth state: in-memory session, localStorage profile, and login hint.
 */
export function clearAuthState() {
  inMemorySession = null
  window.localStorage.removeItem(AUTH_PROFILE_KEY)
  window.localStorage.removeItem(AUTH_LOGGED_IN_HINT_KEY)
  notifyAuthStateChanged()
}

/**
 * Checks if the user previously logged in (hint flag in localStorage).
 * Used by AuthProvider to decide whether to attempt token refresh on mount.
 * This flag does NOT contain any token — just a boolean hint.
 */
export function hasRefreshCookieHint(): boolean {
  return window.localStorage.getItem(AUTH_LOGGED_IN_HINT_KEY) === "1"
}

/**
 * Reads cached profile from localStorage (for optimistic UI before hydration).
 */
export function readCachedProfile(): StoredAuthProfile | null {
  return safeParseJson<StoredAuthProfile>(window.localStorage.getItem(AUTH_PROFILE_KEY))
}
