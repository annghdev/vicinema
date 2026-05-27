import { httpClient } from "./httpClient"
import {
  type AuthTokenResponse,
  type LoginRequest,
  type RegisterRequest,
  type ForgotPasswordRequest,
  type AuthProfileResponse,
  type ChangePasswordRequest,
} from "../types/Auth"


export async function login(body: LoginRequest): Promise<AuthTokenResponse> {
  const response = await httpClient.post<AuthTokenResponse>("/api/auth/login", body, {
    withCredentials: true,
  })
  return response.data
}

export async function register(body: RegisterRequest): Promise<AuthTokenResponse> {
  const response = await httpClient.post<AuthTokenResponse>("/api/auth/register", body, {
    withCredentials: true,
  })
  return response.data
}

export async function forgotPassword(body: ForgotPasswordRequest): Promise<void> {
  await httpClient.post("/api/auth/forgot-password", body, {
    withCredentials: true,
  })
}




export async function logout(): Promise<void> {
  await httpClient.post("/api/auth/logout", undefined, {
    withCredentials: true,
  })
}

export async function getCurrentAuthProfile(): Promise<AuthProfileResponse> {
  const response = await httpClient.get<AuthProfileResponse>("/api/auth/me", {
    withCredentials: true,
  })
  return response.data
}

export async function changePassword(body: ChangePasswordRequest): Promise<void> {
  await httpClient.post("/api/auth/change-password", body, {
    withCredentials: true,
  })
}

