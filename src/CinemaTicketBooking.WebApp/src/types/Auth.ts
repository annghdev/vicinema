export type AuthTokenResponse = {
  accessToken: string
  accessTokenExpiresAtUtc: string
  accountId: string
  refreshToken: string | null
}

export type LoginRequest = {
  email: string
  password: string
}

export type RegisterRequest = {
  email: string
  password: string
  name: string
  phoneNumber: string
  sessionId?: string | null
}

export type ForgotPasswordRequest = {
  email: string
}

export type AuthProfileResponse = {
  accountId: string
  customerId: string | null
  displayName: string
  email: string | null
  avatarUrl: string | null
  phoneNumber?: string | null
}

export type ChangePasswordRequest = {
  currentPassword: string
  newPassword: string
}

