export type CouponTemplateDto = {
  id: string
  code: string
  type: string
  discountType: string
  discountValue: number
  maxDiscountAmount: number | null
  scope: string
  maxUsageCount: number
  maxUsagePerUser: number
  totalUsedCount: number
  validFrom: string
  validTo: string
  isActive: boolean
  description: string
  createdAt: string
}

export type CustomerCouponDto = {
  id: string
  customerId: string
  couponCode: string
  discountType: string
  discountValue: number
  maxDiscountAmount: number | null
  scope: string
  usageCount: number
  maxUsage: number
  isUsed: boolean
  issuedAt: string
  usedAt: string | null
  expiresAt: string
}

export type CouponValidationResultDto = {
  isValid: boolean
  errorMessage: string | null
  couponCode: string | null
  discountType: string | null
  discountValue: number | null
  maxDiscountAmount: number | null
  scope: string | null
  description: string | null
}