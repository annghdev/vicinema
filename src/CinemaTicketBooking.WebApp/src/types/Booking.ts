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

export type BookingDetailsDto = {
  bookingId: string
  showTimeId: string
  showTimeInfo: { screen: string; movie: string; startAt: string; endAt: string }
  originalAmount: number
  discountAmount: number
  finalAmount: number
  checkinQrCode: string
  status: number | string
  createdAt: string
  tickets: { seatCode: string; price: number }[]
  ticketIds: string[]
  concessions: { name: string; imageUrl: string; price: number; quantity: number; amount: number }[]
}

export type BookingHistoryItemDto = {
  bookingId: string
  showTimeInfo: { screen: string; movie: string; startAt: string; endAt: string }
  finalAmount: number
  createdAt: string
  status: number | string
}

export type GetBookingHistoryRequest = {
  pageNumber?: number
  pageSize?: number
  date?: string
}
