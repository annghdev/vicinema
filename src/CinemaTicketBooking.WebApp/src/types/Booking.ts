import { type PaymentRedirectBehavior } from "./Payment"

export type CreateBookingRequest = {
  showTimeId: string
  customerSessionId: string
  customerName: string
  customerEmail: string
  customerPhoneNumber: string
  selectedTicketIds: string[]
  concessions: { concessionId: string; quantity: number }[]
  paymentMethod: string
  returnUrl: string
  ipAddress: string
  couponCode?: string
}

export type CreateBookingResponse = {
  bookingId: string
  paymentExpiresAt: string
  originAmount: number
  finalAmount: number
  paymentStatus: string
  paymentUrl: string | null
  redirectBehavior: PaymentRedirectBehavior | null
  paymentTransactionId: string | null
  gatewayTransactionId: string | null
}

export type CreateBookingResponseRaw = {
  bookingId: string
  paymentExpiresAt: string
  originAmount: number
  finalAmount: number
  paymentStatus: string
  paymentUrl: string | null
  redirectBehavior: PaymentRedirectBehavior | null
  paymentTransactionId: string | null
  gatewayTransactionId: string | null
}

export function normalizeResponse(r: CreateBookingResponseRaw): CreateBookingResponse {
  return {
    bookingId: r.bookingId,
    paymentExpiresAt: r.paymentExpiresAt,
    originAmount: r.originAmount,
    finalAmount: r.finalAmount,
    paymentStatus: r.paymentStatus,
    paymentUrl: r.paymentUrl,
    redirectBehavior: r.redirectBehavior,
    paymentTransactionId: r.paymentTransactionId,
    gatewayTransactionId: r.gatewayTransactionId,
  }
}

export type RetryPaymentRequest = {
  customerSessionId: string
  paymentMethod: string
  returnUrl: string
  ipAddress: string
  replacePendingPayment?: boolean
}

export type PreviewPricingRequest = {
  showTimeId: string
  customerSessionId: string
  customerName: string
  customerEmail: string
  customerPhoneNumber: string
  selectedTicketIds: string[]
  concessions: { concessionId: string; quantity: number }[]
  couponCode?: string
}

export type PreviewPricingResponse = {
  originAmount: number
  ticketDiscount: number
  concessionDiscount: number
  totalDiscount: number
  finalAmount: number
  isRegisteredCustomer: boolean
  loyaltyTierName: string | null
  loyaltyTierDescription: string | null
  ticketDiscountPercent: number | null
  concessionDiscountPercent: number | null
  couponDiscountAmount: number
  couponCode: string | null
  couponDescription: string | null
}
