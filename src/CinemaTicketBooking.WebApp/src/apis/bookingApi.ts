import { httpClient } from "./httpClient"
import { type PaymentRedirectBehavior } from "../types/Payment"
import {
  type CreateBookingRequest,
  type CreateBookingResponse,
  type CreateBookingResponseRaw,
  type RetryPaymentRequest,
  type BookingDetailsDto,
  type BookingHistoryItemDto,
  type GetBookingHistoryRequest,
  type PreviewPricingRequest,
  type PreviewPricingResponse,
  normalizeResponse,
} from "../types/Booking"
import { type PagedResult } from "../types/Common"

export async function createBooking(body: CreateBookingRequest): Promise<CreateBookingResponse> {
  const response = await httpClient.post<CreateBookingResponseRaw>("/api/bookings", {
    showTimeId: body.showTimeId,
    customerSessionId: body.customerSessionId,
    customerName: body.customerName,
    customerEmail: body.customerEmail,
    customerPhoneNumber: body.customerPhoneNumber,
    selectedTicketIds: body.selectedTicketIds,
    concessions: body.concessions,
    paymentMethod: body.paymentMethod,
    returnUrl: body.returnUrl,
    ipAddress: body.ipAddress,
    couponCode: body.couponCode,
  })
  return normalizeResponse(response.data)
}

export async function retryPayment(bookingId: string, body: RetryPaymentRequest): Promise<CreateBookingResponse> {
  const response = await httpClient.post<CreateBookingResponseRaw>(`/api/bookings/${bookingId}/retry-payment`, {
    customerSessionId: body.customerSessionId,
    paymentMethod: body.paymentMethod,
    returnUrl: body.returnUrl,
    ipAddress: body.ipAddress,
    replacePendingPayment: body.replacePendingPayment ?? false,
  })
  return normalizeResponse(response.data)
}

export async function cancelBooking(bookingId: string): Promise<void> {
  await httpClient.put(`/api/bookings/${bookingId}/cancel`)
}

export async function getBookingById(bookingId: string, customerSessionId?: string): Promise<BookingDetailsDto> {
  const response = await httpClient.get<BookingDetailsDto>(`/api/bookings/${bookingId}`, {
    params: { customerSessionId },
  })
  return response.data
}

export async function getBookingHistory(
  customerId: string,
  request: GetBookingHistoryRequest = {},
): Promise<PagedResult<BookingHistoryItemDto>> {
  const response = await httpClient.get<PagedResult<BookingHistoryItemDto>>(`/api/bookings/history/${customerId}`, {
    params: {
      customerId,
      pageNumber: request.pageNumber ?? 1,
      pageSize: request.pageSize ?? 10,
      date: request.date,
    },
  })
  return response.data
}

export async function previewPricing(body: PreviewPricingRequest): Promise<PreviewPricingResponse> {
  const response = await httpClient.post<PreviewPricingResponse>("/api/bookings/preview-pricing", {
    showTimeId: body.showTimeId,
    customerSessionId: body.customerSessionId,
    customerName: body.customerName,
    customerEmail: body.customerEmail,
    customerPhoneNumber: body.customerPhoneNumber,
    selectedTicketIds: body.selectedTicketIds,
    concessions: body.concessions,
    couponCode: body.couponCode,
  })
  return response.data
}
