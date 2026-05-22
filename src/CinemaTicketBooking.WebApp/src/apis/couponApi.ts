import { httpClient } from "./httpClient"
import { type CustomerCouponDto, type CouponValidationResultDto } from "../types/Coupon"

export async function getMyCoupons(): Promise<CustomerCouponDto[]> {
  const response = await httpClient.get<CustomerCouponDto[]>("/api/coupons/my-coupons")
  return response.data
}

export async function getPublicCoupons(): Promise<CustomerCouponDto[]> {
  const response = await httpClient.get<CustomerCouponDto[]>("/api/coupons/public")
  return response.data
}

export async function verifyCoupon(couponCode: string): Promise<CouponValidationResultDto> {
  const response = await httpClient.post<CouponValidationResultDto>("/api/coupons/verify", {
    couponCode,
  })
  return response.data
}