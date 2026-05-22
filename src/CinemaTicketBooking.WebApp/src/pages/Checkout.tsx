import { isAxiosError } from "axios"
import type { HubConnection } from "@microsoft/signalr"
import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom"
import { cancelBooking, createBooking, previewPricing, retryPayment } from "../apis/bookingApi"
import { getConcessions } from "../apis/concessionApi"
import { connectPaymentHub } from "../apis/paymentRealtime"
import { getMyCoupons, verifyCoupon } from "../apis/couponApi"
import { PaymentQRModal } from "../components/PaymentQRModal"
import { getAvailableGateways, getFakePaymentSuccess } from "../apis/paymentApi"
import { getShowTimeById } from "../apis/showtimeApi"
import { clearCheckoutDraft, loadCheckoutDraft, type CheckoutDraft } from "../lib/checkoutDraft"
import { getOrCreateCustomerSessionId } from "../lib/customerSessionId"
import { useAuth } from "../contexts/AuthContext"
import { useToast } from "../contexts/ToastContext"
import type { ShowTimeDetailDto, ShowTimeTicketDto } from "../types/ShowTime"
import type { CreateBookingResponse, PreviewPricingResponse } from "../types/Booking"
import type { ConcessionDto } from "../types/Concession"
import type { CustomerCouponDto } from "../types/Coupon"
import { isRedirectBehavior, type PaymentConfirmedRealtimeEvent, type PaymentGatewayOptionDto, type VerifyPaymentResponse } from "../types/Payment"

type CheckoutLocationState = {
  showtimeId?: string
  selectedTicketIds?: string[]
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(value)
}

function formatDateTimeLabel(dateInput: string) {
  const date = new Date(dateInput)
  if (Number.isNaN(date.getTime())) {
    return dateInput
  }
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date)
}

function resolvePaymentIcon(icon: string | null | undefined): string | null {
  if (!icon || !icon.trim()) {
    return null
  }
  const trimmed = icon.trim()
  if (trimmed.startsWith("<svg") || trimmed.includes("xmlns=")) {
    return `data:image/svg+xml;utf8,${encodeURIComponent(trimmed)}`
  }
  return trimmed
}

function parseGatewayIdFromPaymentUrl(paymentUrl: string | null | undefined, fallback: string | null | undefined): string | null {
  if (fallback) {
    return fallback
  }
  if (!paymentUrl) {
    return null
  }
  const m = paymentUrl.match(/\/pay\/([^/?#]+)\/?$/)
  return m?.[1] ?? null
}

function resolveApiOrigin(): string {
  const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined
  if (!configuredBaseUrl) {
    return window.location.origin
  }
  try {
    return new URL(configuredBaseUrl, window.location.origin).origin
  } catch {
    return window.location.origin
  }
}

function Checkout() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const location = useLocation()
  const { success: toastSuccess, error: toastError } = useToast()

  const showtimeId = searchParams.get("showtimeId")
  const locState = location.state as CheckoutLocationState | null

  const { displayName, email, phoneNumber } = useAuth()

  const [customerName, setCustomerName] = useState(() => displayName ?? "")
  const [customerEmail, setCustomerEmail] = useState(() => email ?? "")
  const [customerPhone, setCustomerPhone] = useState(() => phoneNumber ?? "")

  useEffect(() => {
    if (displayName) setCustomerName((prev) => prev || displayName)
    if (email) setCustomerEmail((prev) => prev || email)
    if (phoneNumber && phoneNumber !== "—") setCustomerPhone((prev) => prev || phoneNumber)
  }, [displayName, email, phoneNumber])

  const [showtime, setShowtime] = useState<ShowTimeDetailDto | null>(null)
  const [selectedTicketIds, setSelectedTicketIds] = useState<string[]>([])

  const [concessions, setConcessions] = useState<ConcessionDto[]>([])
  const [concessionQty, setConcessionQty] = useState<Record<string, number>>({})

  const [gateways, setGateways] = useState<PaymentGatewayOptionDto[]>([])
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState<string | null>(null)

  const [loadError, setLoadError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [completingFake, setCompletingFake] = useState(false)
  const [switchingGateway, setSwitchingGateway] = useState(false)
  const [cancellingPayment, setCancellingPayment] = useState(false)
  const [modalError, setModalError] = useState<string | null>(null)
  const [selectedSwitchGateway, setSelectedSwitchGateway] = useState<string | null>(null)
  const [postBooking, setPostBooking] = useState<CreateBookingResponse | null>(null)
  const [paymentModalOpen, setPaymentModalOpen] = useState(false)

  // ═══════════════════════════════════════════════════════════
  // Coupon state
  // ═══════════════════════════════════════════════════════════
  const [availableCoupons, setAvailableCoupons] = useState<CustomerCouponDto[]>([])
  const [couponCodeInput, setCouponCodeInput] = useState("")
  const [verifyingCoupon, setVerifyingCoupon] = useState(false)
  const [appliedCoupon, setAppliedCoupon] = useState<string | null>(null)
  const [couponError, setCouponError] = useState<string | null>(null)
  const [selectedPersonalCoupon, setSelectedPersonalCoupon] = useState<string | null>(null)

  const backToSeatSelectionPath = showtimeId ? `/showtimes/${showtimeId}/seats` : "/showtimes"

  useEffect(() => {
    if (!showtimeId) {
      setLoadError("Thiếu mã suất chiếu.")
      setLoading(false)
      return
    }

    const resolvedShowtimeId = showtimeId

    const fromState = locState?.selectedTicketIds
    const fromStore = loadCheckoutDraft(resolvedShowtimeId)
    const draft: CheckoutDraft | null =
      fromState && fromState.length > 0 ? { selectedTicketIds: fromState } : fromStore

    if (!draft) {
      setLoadError("Không tìm thấy ghế đã chọn. Vui lòng chọn lại.")
      setLoading(false)
      return
    }
    setSelectedTicketIds(draft.selectedTicketIds)

    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        setLoadError(null)
        const [st, list, gws] = await Promise.all([
          getShowTimeById(resolvedShowtimeId),
          getConcessions(),
          getAvailableGateways(),
        ])
        if (cancelled) {
          return
        }
        setShowtime(st)
        setConcessions(list.filter((c) => c.isAvailable))
        setGateways(gws)
        if (gws.length > 0) {
          setSelectedPaymentMethod((m) => m ?? gws[0]!.method)
        }
      } catch {
        if (!cancelled) {
          setLoadError("Không tải được dữ liệu thanh toán. Vui lòng thử lại.")
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [showtimeId, locState])

  // Load customer's available coupons
  useEffect(() => {
    if (!showtimeId) return
    let cancelled = false
    async function loadCoupons() {
      try {
        const coupons = await getMyCoupons()
        if (!cancelled) {
          setAvailableCoupons(coupons.filter((c) => !c.isUsed))
        }
      } catch {
        // non-critical
      }
    }
    void loadCoupons()
    return () => { cancelled = true }
  }, [showtimeId])

  const isPostPayQrFlow = useMemo(
    () => postBooking && isRedirectBehavior(postBooking.redirectBehavior) !== "Redirect",
    [postBooking],
  )

  useEffect(() => {
    if (!selectedPaymentMethod) {
      setSelectedSwitchGateway(null)
      return
    }
    setSelectedSwitchGateway(selectedPaymentMethod)
  }, [selectedPaymentMethod, postBooking?.bookingId])

  useEffect(() => {
    if (isPostPayQrFlow) {
      setModalError(null)
      setPaymentModalOpen(true)
    }
  }, [isPostPayQrFlow, postBooking?.bookingId])

  const selectedTickets: ShowTimeTicketDto[] = useMemo(() => {
    if (!showtime) {
      return []
    }
    const set = new Set(selectedTicketIds)
    return showtime.tickets.filter((t) => set.has(t.id))
  }, [showtime, selectedTicketIds])

  const ticketsSubtotal = useMemo(() => selectedTickets.reduce((s, t) => s + t.price, 0), [selectedTickets])

  const concessionLines = useMemo(() => {
    return concessions
      .map((c) => {
        const q = concessionQty[c.id] ?? 0
        if (q <= 0) {
          return null
        }
        return { concession: c, quantity: q, lineTotal: c.price * q }
      })
      .filter((x): x is { concession: ConcessionDto; quantity: number; lineTotal: number } => x != null)
  }, [concessions, concessionQty])

  const concessionSubtotal = useMemo(
    () => concessionLines.reduce((s, l) => s + l.lineTotal, 0),
    [concessionLines],
  )

  const totalAmount = ticketsSubtotal + concessionSubtotal

  // ═══════════════════════════════════════════════════════════
  // Preview pricing — debounced call to server for discount display
  // ═══════════════════════════════════════════════════════════
  const [previewData, setPreviewData] = useState<PreviewPricingResponse | null>(null)
  const previewTimerRef = useRef<ReturnType<typeof setTimeout>>()

  useEffect(() => {
    if (previewTimerRef.current) clearTimeout(previewTimerRef.current)
    setPreviewData(null)

    if (!showtimeId || selectedTicketIds.length === 0) {
      return
    }

    previewTimerRef.current = setTimeout(() => {
      const concessionsList = Object.entries(concessionQty)
        .filter(([, qty]) => qty > 0)
        .map(([id, qty]) => ({ concessionId: id, quantity: qty }))

      previewPricing({
        showTimeId: showtimeId,
        customerSessionId: getOrCreateCustomerSessionId(),
        customerName: customerName.trim() || "Guest",
        customerEmail: customerEmail.trim() || "guest@example.com",
        customerPhoneNumber: customerPhone.trim() || "0000000000",
        selectedTicketIds: selectedTicketIds,
        concessions: concessionsList,
      })
        .then(setPreviewData)
        .catch(() => setPreviewData(null))
    }, 600)

    return () => {
      if (previewTimerRef.current) clearTimeout(previewTimerRef.current)
    }
  }, [showtimeId, selectedTicketIds, concessionQty, customerName, customerEmail, customerPhone])

  // Re-trigger preview when coupon changes
  useEffect(() => {
    if (previewTimerRef.current) clearTimeout(previewTimerRef.current)
    setPreviewData(null)

    if (!showtimeId || selectedTicketIds.length === 0) {
      return
    }

    const effectiveCouponCode = selectedPersonalCoupon || appliedCoupon || undefined

    previewTimerRef.current = setTimeout(() => {
      const concessionsList = Object.entries(concessionQty)
        .filter(([, qty]) => qty > 0)
        .map(([id, qty]) => ({ concessionId: id, quantity: qty }))

      previewPricing({
        showTimeId: showtimeId,
        customerSessionId: getOrCreateCustomerSessionId(),
        customerName: customerName.trim() || "Guest",
        customerEmail: customerEmail.trim() || "guest@example.com",
        customerPhoneNumber: customerPhone.trim() || "0000000000",
        selectedTicketIds: selectedTicketIds,
        concessions: concessionsList,
        couponCode: effectiveCouponCode,
      })
        .then(setPreviewData)
        .catch(() => setPreviewData(null))
    }, 600)

    return () => {
      if (previewTimerRef.current) clearTimeout(previewTimerRef.current)
    }
  }, [showtimeId, selectedTicketIds, concessionQty, customerName, customerEmail, customerPhone, selectedPersonalCoupon, appliedCoupon])

  // Derive the amounts to display (preview values if available, otherwise origin).
  const displayOriginAmount = previewData?.originAmount ?? totalAmount
  const displayFinalAmount = previewData?.finalAmount ?? totalAmount
  const displayTotalDiscount = previewData?.totalDiscount ?? 0
  const hasDiscount = previewData?.isRegisteredCustomer === true && displayTotalDiscount > 0
  const hasCouponDiscount = (previewData?.couponDiscountAmount ?? 0) > 0

  const setQty = useCallback((concessionId: string, next: number) => {
    setConcessionQty((prev) => {
      const p = { ...prev }
      if (next <= 0) {
        delete p[concessionId]
      } else {
        p[concessionId] = next
      }
      return p
    })
  }, [])

  const onSubmitPayment = async () => {
    if (!showtimeId || !showtime || selectedTickets.length === 0) {
      return
    }
    if (!customerName.trim() || !customerEmail.trim() || !customerPhone.trim()) {
      const msg = "Vui lòng nhập đủ họ tên, email và số điện thoại."
      setSubmitError(msg)
      toastError(msg)
      return
    }
    if (!selectedPaymentMethod) {
      const msg = "Chưa chọn phương thức thanh toán."
      setSubmitError(msg)
      toastError(msg)
      return
    }

    setSubmitError(null)
    setModalError(null)
    setSubmitting(true)
    setPostBooking(null)
    try {
      const selectedGateway = selectedPaymentMethod.toLowerCase()
      const returnUrl = selectedGateway === "momo"
        ? `${resolveApiOrigin()}/api/payments/momo-return`
        : selectedGateway === "vnpay"
          ? `${resolveApiOrigin()}/api/payments/vnpay-return`
          : `${window.location.origin}/payment-result`
      const res = await createBooking({
        showTimeId: showtimeId,
        customerSessionId: getOrCreateCustomerSessionId(),
        customerName: customerName.trim(),
        customerEmail: customerEmail.trim(),
        customerPhoneNumber: customerPhone.trim(),
        selectedTicketIds: selectedTickets.map((t) => t.id),
        concessions: concessionLines.map((l) => ({
          concessionId: l.concession.id,
          quantity: l.quantity,
        })),
        paymentMethod: selectedPaymentMethod,
        returnUrl,
        ipAddress: "127.0.0.1",
        couponCode: selectedPersonalCoupon || appliedCoupon || undefined,
      })

      if (showtimeId) {
        clearCheckoutDraft(showtimeId)
      }

      const behavior = isRedirectBehavior(res.redirectBehavior)
      if (behavior === "Redirect" && res.paymentUrl) {
        window.location.assign(res.paymentUrl)
        return
      }

      setPostBooking(res)
    } catch (e) {
      if (isAxiosError(e) && e.response?.data) {
        const d = e.response.data as { title?: string; detail?: string; message?: string }
        const msg = d.detail ?? d.title ?? d.message
        if (msg) {
          setSubmitError(String(msg))
          toastError(String(msg))
        } else {
          setSubmitError("Không thể xử lý thanh toán. Vui lòng thử lại.")
          toastError("Không thể xử lý thanh toán. Vui lòng thử lại.")
        }
      } else {
        setSubmitError("Không thể xử lý thanh toán. Vui lòng thử lại.")
        toastError("Không thể xử lý thanh toán. Vui lòng thử lại.")
      }
    } finally {
      setSubmitting(false)
    }
  }

  const navigateToSuccess = useCallback((bookingId: string, checkinQrCode?: string | null, gatewayTxnRef?: string | null) => {
    setPaymentModalOpen(false)
    toastSuccess("Đặt vé thành công!")
    navigate({
      pathname: "/payment-result",
      search: `?status=success&bookingId=${encodeURIComponent(bookingId)}${gatewayTxnRef ? `&txnRef=${encodeURIComponent(gatewayTxnRef)}` : ""}`,
    }, checkinQrCode ? {
      state: {
        checkinQrCode,
        bookingId,
        movieName: showtime?.movieName,
        screenCode: showtime?.screenCode,
        startAt: showtime?.startAt,
        cinemaName: showtime?.cinemaName,
        seatsLabel: selectedTickets.map((t) => t.code).join(", "),
        finalAmount: postBooking?.finalAmount,
        concessions: concessionLines.map((l) => ({
          name: l.concession.name,
          quantity: l.quantity,
          amount: l.lineTotal,
        })),
      },
    } : undefined)
  }, [navigate, showtime?.movieName, showtime?.screenCode, showtime?.startAt, showtime?.cinemaName, selectedTickets, postBooking?.finalAmount, concessionLines])

  const onPaymentConfirmedRealtime = useCallback((event: PaymentConfirmedRealtimeEvent) => {
    setModalError(null)
    navigateToSuccess(event.bookingId, event.checkinQrCode, event.gatewayTransactionId)
  }, [navigateToSuccess])

  const onCancelPendingPayment = async () => {
    if (!postBooking) {
      return
    }
    setModalError(null)
    setCancellingPayment(true)
    try {
      await cancelBooking(postBooking.bookingId)
      setPaymentModalOpen(false)
      setPostBooking(null)
      navigate(backToSeatSelectionPath)
    } catch (e) {
      if (isAxiosError(e) && e.response?.data) {
        const d = e.response.data as { title?: string; detail?: string; message?: string }
        setModalError(d.detail ?? d.title ?? d.message ?? "Không thể hủy thanh toán.")
      } else {
        setModalError("Không thể hủy thanh toán.")
      }
    } finally {
      setCancellingPayment(false)
    }
  }

  const onSwitchGateway = async () => {
    if (!postBooking || !selectedSwitchGateway) {
      return
    }
    setModalError(null)
    setSwitchingGateway(true)
    try {
      const selectedGateway = selectedSwitchGateway.toLowerCase()
      const returnUrl = selectedGateway === "momo"
        ? `${resolveApiOrigin()}/api/payments/momo-return`
        : selectedGateway === "vnpay"
          ? `${resolveApiOrigin()}/api/payments/vnpay-return`
          : `${window.location.origin}/payment-result`
      const retry = await retryPayment(postBooking.bookingId, {
        customerSessionId: getOrCreateCustomerSessionId(),
        paymentMethod: selectedSwitchGateway,
        returnUrl,
        ipAddress: "127.0.0.1",
        replacePendingPayment: true,
      })

      setSelectedPaymentMethod(selectedSwitchGateway)
      setPostBooking(retry)

      const behavior = isRedirectBehavior(retry.redirectBehavior)
      if (behavior === "Redirect" && retry.paymentUrl) {
        window.location.assign(retry.paymentUrl)
        return
      }
      setPaymentModalOpen(true)
    } catch (e) {
      if (isAxiosError(e) && e.response?.data) {
        const d = e.response.data as { title?: string; detail?: string; message?: string }
        setModalError(d.detail ?? d.title ?? d.message ?? "Không thể đổi gateway.")
      } else {
        setModalError("Không thể đổi gateway.")
      }
    } finally {
      setSwitchingGateway(false)
    }
  }

  const onCompleteFakeCallback = async () => {
    if (!postBooking) {
      return
    }
    const gatewayId = parseGatewayIdFromPaymentUrl(postBooking.paymentUrl, postBooking.gatewayTransactionId)
    if (!gatewayId) {
      setSubmitError("Thiếu mã giao dịch cổng thanh toán — không thể xác nhận.")
      return
    }
    setSubmitError(null)
    setModalError(null)
    setCompletingFake(true)
    try {
      const data: VerifyPaymentResponse = await getFakePaymentSuccess(postBooking.bookingId, gatewayId)
      if (!data.isSuccess) {
        setModalError(data.errorMessage ?? "Thanh toán chưa được xác nhận.")
        return
      }
      navigateToSuccess(data.bookingId, data.checkinQrCode, gatewayId)
    } catch (e) {
      if (isAxiosError(e) && e.response?.data) {
        const d = e.response.data as VerifyPaymentResponse | { title?: string; detail?: string }
        const maybeV = d as VerifyPaymentResponse
        if (typeof maybeV.errorMessage === "string" && maybeV.errorMessage) {
          setModalError(maybeV.errorMessage)
        } else {
          const t = d as { title?: string; detail?: string }
          setModalError(t.detail ?? t.title ?? "Xác nhận thanh toán thất bại.")
        }
      } else {
        setModalError("Xác nhận thanh toán thất bại.")
      }
    } finally {
      setCompletingFake(false)
    }
  }

  useEffect(() => {
    if (!postBooking || !isPostPayQrFlow) {
      return
    }
    let connection: HubConnection | null = null
    let disposed = false

    void (async () => {
      try {
        connection = await connectPaymentHub(postBooking.bookingId, (payload) => {
          if (!disposed) {
            onPaymentConfirmedRealtime(payload)
          }
        })
      } catch {
        // Keep QR flow usable even when realtime channel is unavailable.
      }
    })()

    return () => {
      disposed = true
      if (connection) {
        void connection.stop()
      }
    }
  }, [postBooking?.bookingId, isPostPayQrFlow, onPaymentConfirmedRealtime])

  if (!showtimeId) {
    return (
      <main className="mx-auto w-full max-w-screen-lg px-8 pb-12 pt-28">
        <p className="text-on-surface-variant">Thiếu mã suất chiếu.</p>
        <Link to="/showtimes" className="mt-4 inline-block text-secondary hover:underline">
          Lịch chiếu
        </Link>
      </main>
    )
  }

  if (loadError) {
    return (
      <main className="mx-auto w-full max-w-screen-lg px-8 pb-12 pt-28">
        <p className="text-on-surface-variant">{loadError}</p>
        <Link to={backToSeatSelectionPath} className="mt-4 inline-block text-secondary hover:underline">
          Quay lại đặt ghế
        </Link>
      </main>
    )
  }

  if (loading || !showtime) {
    return (
      <main className="mx-auto flex min-h-[50vh] w-full max-w-screen-lg items-center justify-center px-8 pt-28">
        <div className="flex flex-col items-center gap-3 text-on-surface-variant">
          <span
            className="inline-block h-10 w-10 animate-spin rounded-full border-2 border-secondary border-t-transparent"
            aria-hidden
          />
          <p>Đang tải thanh toán…</p>
        </div>
      </main>
    )
  }

  if (selectedTickets.length === 0) {
    return (
      <main className="mx-auto w-full max-w-screen-lg px-8 pb-12 pt-28">
        <p className="text-on-surface-variant">Không còn ghế hợp lệ. Vui lòng chọn lại.</p>
        <Link to={backToSeatSelectionPath} className="mt-4 inline-block text-secondary hover:underline">
          Quay lại đặt ghế
        </Link>
      </main>
    )
  }

  const seatList = selectedTickets.map((t) => t.code).join(", ")
  const switchableGateways = gateways.filter((gateway) => gateway.method !== selectedPaymentMethod)

  return (
    <>
    <main className="mx-auto grid w-full max-w-screen-2xl grid-cols-1 gap-8 px-8 pb-12 pt-24 md:pt-28 lg:grid-cols-12 lg:gap-12">
      <section className="flex flex-col gap-10 lg:col-span-8">
        <div>
          <Link
            to={backToSeatSelectionPath}
            className="group mb-4 flex items-center text-sm text-on-surface-variant transition-colors hover:text-secondary"
          >
            <span className="material-symbols-outlined mr-1 text-lg transition-transform group-hover:-translate-x-1">arrow_back</span>
            Quay lại đặt ghế
          </Link>
          <h1 className="mb-2 font-headline text-4xl font-bold tracking-tight md:text-5xl">Thanh toán</h1>
          <p className="text-on-surface-variant">Hoàn tất đặt vé để có trải nghiệm điện ảnh khó quên.</p>
        </div>

        <section className="rounded-xl border border-outline-variant/10 bg-surface-container-low p-6 md:p-8">
          <h2 className="mb-4 font-headline text-xl font-semibold">Thông tin liên hệ</h2>
          <div className="grid gap-4 sm:grid-cols-1">
            <label className="block text-sm text-on-surface-variant">
              Họ tên
              <input
                value={customerName}
                onChange={(e) => setCustomerName(e.target.value)}
                className="mt-1 w-full rounded-lg border border-outline-variant/30 bg-surface-container-highest px-3 py-2 text-on-background"
                autoComplete="name"
                placeholder="Nguyễn Văn A"
              />
            </label>
            <label className="block text-sm text-on-surface-variant">
              Email
              <input
                value={customerEmail}
                onChange={(e) => setCustomerEmail(e.target.value)}
                className="mt-1 w-full rounded-lg border border-outline-variant/30 bg-surface-container-highest px-3 py-2 text-on-background"
                autoComplete="email"
                type="email"
                placeholder="you@example.com"
              />
            </label>
            <label className="block text-sm text-on-surface-variant">
              Số điện thoại
              <input
                value={customerPhone}
                onChange={(e) => setCustomerPhone(e.target.value)}
                className="mt-1 w-full rounded-lg border border-outline-variant/30 bg-surface-container-highest px-3 py-2 text-on-background"
                autoComplete="tel"
                inputMode="tel"
                placeholder="09xxxxxxxx"
              />
            </label>
          </div>
        </section>

        <section className="rounded-xl bg-surface-container-low p-6 md:p-8">
          <h2 className="mb-6 flex items-center gap-3 font-headline text-2xl font-semibold">
            <span className="material-symbols-outlined text-secondary">fastfood</span>
            Đồ ăn &amp; Nước uống
          </h2>
          {concessions.length === 0 ? (
            <p className="text-sm text-on-surface-variant">Hiện không có sản phẩm nào.</p>
          ) : (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              {concessions.map((item) => {
                const q = concessionQty[item.id] ?? 0
                return (
                  <div
                    key={item.id}
                    className="flex flex-col gap-3 rounded-lg border border-outline-variant/20 bg-surface-container-highest p-4 sm:flex-row"
                  >
                    <div className="h-24 w-full shrink-0 overflow-hidden rounded-md bg-surface-dim sm:h-20 sm:w-20">
                      {item.imageUrl ? (
                        <img src={item.imageUrl} alt="" className="h-full w-full object-cover" />
                      ) : null}
                    </div>
                    <div className="min-w-0 flex-1">
                      <h3 className="font-headline text-lg font-medium">{item.name}</h3>
                      <p className="mb-3 text-sm font-bold text-secondary">{formatCurrency(item.price)}</p>
                      <div className="flex items-center gap-2">
                        <button
                          type="button"
                          disabled={q <= 0}
                          onClick={() => setQty(item.id, q - 1)}
                          className="rounded border border-outline-variant/30 px-2.5 py-1 text-sm font-bold text-on-surface disabled:opacity-30"
                        >
                          −
                        </button>
                        <span className="min-w-[2ch] text-center text-sm font-semibold">{q}</span>
                        <button
                          type="button"
                          onClick={() => setQty(item.id, q + 1)}
                          className="rounded border border-secondary/40 bg-secondary/10 px-2.5 py-1 text-sm font-bold text-secondary"
                        >
                          +
                        </button>
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </section>

        {/* ═══════════════════════════════════════════════════════
            Coupon Section
        ═══════════════════════════════════════════════════════ */}
        <section className="rounded-xl bg-surface-container-low p-6 md:p-8">
          <h2 className="mb-4 flex items-center gap-3 font-headline text-2xl font-semibold">
            <span className="material-symbols-outlined text-secondary">redeem</span>
            Mã giảm giá
          </h2>

          {/* Personal coupon dropdown */}
          {availableCoupons.length > 0 && (
            <div className="mb-4">
              <label className="mb-2 block text-sm font-medium text-on-surface-variant">
                Chọn mã ưu đãi của bạn
              </label>
              <select
                value={selectedPersonalCoupon ?? ""}
                onChange={(e) => {
                  const val = e.target.value
                  setSelectedPersonalCoupon(val || null)
                  setCouponError(null)
                  if (val) {
                    setAppliedCoupon(null)
                    setCouponCodeInput("")
                  }
                }}
                className="w-full rounded-lg border border-outline-variant/30 bg-surface-container-highest px-3 py-2.5 text-sm text-on-background"
              >
                <option value="">— Không dùng mã —</option>
                {availableCoupons.map((c) => (
                  <option key={c.id} value={c.couponCode}>
                    {c.couponCode} — {c.discountType === "Fixed" ? `${c.discountValue.toLocaleString("vi-VN")}đ` : `${c.discountValue}%`}
                    {c.scope !== "All" && ` (${c.scope === "Tickets" ? "vé" : "bắp nước"})`}
                    {` - HSD: ${new Date(c.expiresAt).toLocaleDateString("vi-VN")}`}
                  </option>
                ))}
              </select>
            </div>
          )}

          {/* Public coupon code input */}
          <div>
            <label className="mb-2 block text-sm font-medium text-on-surface-variant">
              Nhập mã giảm giá chung
            </label>
            <div className="flex gap-2">
              <input
                value={couponCodeInput}
                onChange={(e) => {
                  setCouponCodeInput(e.target.value.toUpperCase())
                  setCouponError(null)
                }}
                placeholder="Ví dụ: KOL50K"
                className="flex-1 rounded-lg border border-outline-variant/30 bg-surface-container-highest px-3 py-2.5 text-sm text-on-background uppercase tracking-wider"
              />
              <button
                type="button"
                disabled={!couponCodeInput.trim() || verifyingCoupon}
                onClick={async () => {
                  if (!couponCodeInput.trim()) return
                  setVerifyingCoupon(true)
                  setCouponError(null)
                  try {
                    const result = await verifyCoupon(couponCodeInput.trim())
                    if (result.isValid) {
                      setAppliedCoupon(couponCodeInput.trim())
                      setSelectedPersonalCoupon(null)
                      setCouponError(null)
                    } else {
                      setAppliedCoupon(null)
                      setCouponError(result.errorMessage ?? "Mã không hợp lệ")
                    }
                  } catch {
                    setCouponError("Không thể kiểm tra mã. Vui lòng thử lại.")
                  } finally {
                    setVerifyingCoupon(false)
                  }
                }}
                className="rounded-lg bg-secondary/20 px-4 py-2.5 text-sm font-semibold text-secondary transition-colors hover:bg-secondary/30 disabled:opacity-50"
              >
                {verifyingCoupon ? (
                  <span className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-secondary border-t-transparent" />
                ) : (
                  "Áp dụng"
                )}
              </button>
            </div>
            {appliedCoupon && (
              <div className="mt-2 flex items-center gap-2 rounded-lg bg-emerald-500/10 px-3 py-2 text-sm text-emerald-300">
                <span className="material-symbols-outlined text-base">check_circle</span>
                Đã áp dụng mã <strong>{appliedCoupon}</strong>
                {previewData?.couponDiscountAmount ? ` (giảm ${formatCurrency(previewData.couponDiscountAmount)})` : ""}
                <button
                  type="button"
                  onClick={() => {
                    setAppliedCoupon(null)
                    setCouponCodeInput("")
                    setCouponError(null)
                  }}
                  className="ml-auto text-xs text-red-300 hover:text-red-200"
                >
                  Hủy
                </button>
              </div>
            )}
            {couponError && (
              <p className="mt-2 text-sm text-red-300">{couponError}</p>
            )}
          </div>
        </section>

        <section className="rounded-xl bg-surface-container-low p-6 md:p-8">
          <h2 className="mb-2 flex items-center gap-3 font-headline text-2xl font-semibold">
            <span className="material-symbols-outlined text-secondary">payment</span>
            Phương thức thanh toán
          </h2>
          {gateways.length === 0 ? (
            <p className="text-sm text-amber-200/80">Chưa cấu hình phương thức thanh toán.</p>
          ) : (
            <div className="mb-2 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {gateways.map((g) => {
                const selected = selectedPaymentMethod === g.method
                const iconDataUrl = resolvePaymentIcon(g.icon)
                return (
                  <button
                    key={g.method}
                    type="button"
                    onClick={() => setSelectedPaymentMethod(g.method)}
                    className={`flex h-16 items-center gap-4 rounded-xl border px-4 transition-all ${
                      selected
                        ? "border-secondary bg-secondary/5 shadow-[0_0_15px_rgba(0,244,254,0.15)]"
                        : "border-outline-variant/10 bg-surface-container-highest hover:border-outline-variant/30 hover:bg-surface-container-highest/80"
                    }`}
                  >
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-white p-1">
                      {iconDataUrl ? (
                        <img
                          src={iconDataUrl}
                          alt={g.displayName || g.method}
                          className="h-full w-full object-contain"
                        />
                      ) : (
                        <span className="material-symbols-outlined text-xl text-secondary">account_balance</span>
                      )}
                    </div>
                    <div className="min-w-0 flex-1 overflow-hidden">
                      <h3 className="truncate font-headline text-sm font-semibold">{g.displayName || g.method}</h3>
                      <p className="truncate text-[9px] uppercase tracking-wider text-on-surface-variant/70">{g.method}</p>
                    </div>
                  </button>
                )
              })}
            </div>
          )}
        </section>
      </section>

      <aside className="lg:col-span-4">
        <div className="rounded-sm border border-outline-variant/10 bg-surface-container-highest p-6 shadow-[0_20px_40px_rgba(0,0,0,0.4)] lg:sticky lg:top-28">
          <h3 className="mb-1 font-headline text-2xl font-bold">{showtime.movieName}</h3>
          <p className="mb-1 text-sm text-on-surface-variant">
            {formatDateTimeLabel(showtime.startAt)} · {showtime.screenCode}
          </p>
          <p className="mb-1 text-sm text-on-surface-variant">{showtime.cinemaName}</p>
          <div className="mb-6 text-sm">
            <p>
              Ghế: <span className="font-bold text-secondary">{seatList}</span>
            </p>
            <p className="text-on-surface-variant">
              {selectedTickets.length} vé
            </p>
          </div>
          <div className="space-y-2 border-t border-outline-variant/20 pt-4 text-sm">
            <div className="flex justify-between">
              <span className="text-on-surface-variant">Vé</span>
              <span>{formatCurrency(ticketsSubtotal)}</span>
            </div>
            {concessionLines.map((l) => (
              <div key={l.concession.id} className="flex justify-between">
                <span className="text-on-surface-variant">
                  {l.concession.name} (×{l.quantity})
                </span>
                <span>{formatCurrency(l.lineTotal)}</span>
              </div>
            ))}
          </div>
          {hasDiscount && previewData && (
            <div className="mt-3 space-y-1 border-t border-secondary/20 pt-3 text-sm">
              <div className="flex items-center justify-between text-secondary">
                <span className="flex items-center gap-1.5 font-medium">
                  <span className="material-symbols-outlined text-base">loyalty</span>
                  Giảm giá thành viên
                </span>
                <span className="font-semibold text-red-400">
                  −{formatCurrency(displayTotalDiscount)}
                </span>
              </div>
              {previewData.loyaltyTierName && (
                <div className="flex items-center justify-between text-[11px] text-on-surface-variant/70">
                  <span>
                    {previewData.loyaltyTierName}
                    {previewData.ticketDiscountPercent != null && previewData.ticketDiscountPercent > 0
                      ? ` – giảm ${previewData.ticketDiscountPercent}% vé`
                      : ""}
                    {previewData.concessionDiscountPercent != null && previewData.concessionDiscountPercent > 0
                      ? ` + ${previewData.concessionDiscountPercent}% đồ uống`
                      : ""}
                  </span>
                  {previewData.loyaltyTierDescription && (
                    <span title={previewData.loyaltyTierDescription} className="cursor-help underline decoration-dotted">
                      ⓘ
                    </span>
                  )}
                </div>
              )}
            </div>
          )}
          {hasCouponDiscount && previewData && (
            <div className="mt-2 space-y-1 border-t border-primary/20 pt-2 text-sm">
              <div className="flex items-center justify-between text-primary">
                <span className="flex items-center gap-1.5 font-medium">
                  <span className="material-symbols-outlined text-base">redeem</span>
                  Mã giảm giá {previewData.couponCode}
                </span>
                <span className="font-semibold text-red-400">
                  −{formatCurrency(previewData.couponDiscountAmount)}
                </span>
              </div>
              {previewData.couponDescription && (
                <p className="text-[11px] text-on-surface-variant/70">{previewData.couponDescription}</p>
              )}
            </div>
          )}
          {submitError && <div className="mt-4 rounded border border-red-400/30 bg-red-500/10 px-3 py-2 text-sm text-red-200">{submitError}</div>}
          <div className="mt-6 flex items-end justify-between border-t border-outline-variant/20 pt-4">
            <span className="font-headline text-lg text-on-surface-variant">Tổng cộng</span>
            <span className="font-headline text-3xl font-bold text-secondary">
              {formatCurrency(displayFinalAmount)}
              {hasDiscount && (
                <span className="ml-2 text-xs font-normal text-on-surface-variant/60 line-through">
                  {formatCurrency(displayOriginAmount)}
                </span>
              )}
            </span>
          </div>
          <button
            type="button"
            disabled={submitting || gateways.length === 0 || !selectedPaymentMethod || !!postBooking}
            onClick={() => {
              void onSubmitPayment()
            }}
            className="mt-6 flex w-full items-center justify-center gap-2 bg-gradient-to-br from-primary to-primary-container py-4 font-headline font-bold text-on-primary transition-all hover:shadow-[0_0_20px_rgba(0,244,254,0.6)] disabled:cursor-not-allowed disabled:opacity-50"
          >
            {submitting ? (
              <span
                className="inline-block h-5 w-5 shrink-0 animate-spin rounded-full border-2 border-on-primary border-t-transparent"
                aria-hidden
              />
            ) : (
              <span className="material-symbols-outlined">lock</span>
            )}
            {postBooking ? "Đã tạo đơn" : `Thanh toán ${formatCurrency(displayFinalAmount)}`}
          </button>
        </div>
      </aside>
    </main>
    <PaymentQRModal
      open={Boolean(paymentModalOpen && isPostPayQrFlow && postBooking)}
      onOpenChange={setPaymentModalOpen}
      paymentUrl={postBooking?.paymentUrl ?? null}
      displayMethod={selectedPaymentMethod ?? "—"}
      paymentExpiresAt={postBooking?.paymentExpiresAt ?? null}
      isNoneOrFake={
        selectedPaymentMethod?.toLowerCase() === "none" || Boolean(postBooking?.paymentUrl?.includes("fake-payment"))
      }
      onConfirmNonePayment={() => {
        void onCompleteFakeCallback()
      }}
      completingCallback={completingFake}
      errorText={modalError ?? submitError}
      onCancelPayment={() => {
        void onCancelPendingPayment()
      }}
      cancellingPayment={cancellingPayment}
      switchableGateways={switchableGateways.map((x) => ({
        method: x.method,
        displayName: x.displayName || x.method,
        icon: x.icon,
      }))}
      selectedSwitchGateway={selectedSwitchGateway}
      onSelectSwitchGateway={setSelectedSwitchGateway}
      onSwitchGateway={() => {
        void onSwitchGateway()
      }}
      switchingGateway={switchingGateway}
    />
    </>
  )
}

export default Checkout
