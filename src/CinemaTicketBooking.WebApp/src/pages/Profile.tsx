import { isAxiosError } from "axios"
import { useEffect, useMemo, useState } from "react"
import { getBookingHistory } from "../apis/bookingApi"
import { changePassword } from "../apis/authApi"
import { getCustomerLoyalty, getActiveTiers } from "../apis/loyaltyApi"
import { getMyCoupons } from "../apis/couponApi"
import { type BookingHistoryItemDto } from "../types/Booking"
import { type CustomerCouponDto } from "../types/Coupon"
import { type CustomerLoyaltyDto, type LoyaltyTierDto, TIER_LABELS, TIER_COLORS, TIER_NUMBER_MAP, TIER_ICONS } from "../types/Loyalty"
import { useAuth } from "../contexts/AuthContext"
import { useToast } from "../contexts/ToastContext"

const PAGE_SIZE = 10

// Formatters
function formatCurrency(amount: number) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(amount)
}

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date)
}

function normalizeBookingStatus(status: number | string): { label: string; className: string } {
  const raw = typeof status === "string" ? status : String(status)
  const key = raw.toLowerCase()

  if (key === "1" || key === "pending") {
    return { label: "Chờ thanh toán", className: "bg-amber-500/10 text-amber-300 border-amber-300/30" }
  }
  if (key === "2" || key === "confirmed") {
    return { label: "Đã xác nhận", className: "bg-emerald-500/10 text-emerald-300 border-emerald-300/30" }
  }
  if (key === "3" || key === "checkedin" || key === "checked_in") {
    return { label: "Đã check-in", className: "bg-sky-500/10 text-sky-300 border-sky-300/30" }
  }
  if (key === "4" || key === "cancelled" || key === "canceled") {
    return { label: "Đã hủy", className: "bg-rose-500/10 text-rose-300 border-rose-300/30" }
  }

  return {
    label: typeof status === "string" ? status : `Trạng thái ${status}`,
    className: "bg-surface-container-high text-on-surface border-outline-variant/30",
  }
}

// Available genres for selection
const AVAILABLE_GENRES = [
  "Hành động",
  "Hài hước",
  "Kinh dị",
  "Tình cảm",
  "Khoa học viễn tưởng",
  "Hoạt hình",
  "Kịch tính",
  "Phiêu lưu",
  "Tâm lý",
]

function Profile() {
  const { isAuthenticated, isResolvingProfile, customerId, displayName, email, phoneNumber, avatarUrl } = useAuth()
  const { success, error: toastError } = useToast()

  // Tabs navigation
  const [activeTab, setActiveTab] = useState<"info" | "benefits" | "password" | "history">("info")

  // Loyalty data from API
  const [loyaltyLoading, setLoyaltyLoading] = useState(true)
  const [customerLoyalty, setCustomerLoyalty] = useState<CustomerLoyaltyDto | null>(null)
  const [tiers, setTiers] = useState<LoyaltyTierDto[]>([])

  // Additional faked information persistent states
  const [dob, setDob] = useState("")
  const [gender, setGender] = useState("")
  const [address, setAddress] = useState("")
  const [favoriteGenres, setFavoriteGenres] = useState<string[]>([])
  const [phoneVal, setPhoneVal] = useState("")

  // Load faked information from localStorage on component mount
  useEffect(() => {
    if (customerId) {
      const storedInfo = localStorage.getItem(`profile_info_${customerId}`)
      if (storedInfo) {
        try {
          const parsed = JSON.parse(storedInfo)
          setDob(parsed.dob || "")
          setGender(parsed.gender || "")
          setAddress(parsed.address || "")
          setFavoriteGenres(parsed.favoriteGenres || [])
          setPhoneVal(parsed.phoneVal || phoneNumber || "")
        } catch {
          // ignore
        }
      } else {
        setPhoneVal(phoneNumber || "")
      }
    }
  }, [customerId, phoneNumber])

  // Load loyalty data from API
  useEffect(() => {
    if (!isAuthenticated || !customerId) return

    let disposed = false
    async function loadLoyalty() {
      setLoyaltyLoading(true)
      try {
        const [myLoyalty, activeTiers] = await Promise.all([
          getCustomerLoyalty(),
          getActiveTiers(),
        ])
        if (disposed) return
        setCustomerLoyalty(myLoyalty)
        setTiers(activeTiers)
      } catch {
        // Loyalty fetch failure is non-critical
      } finally {
        if (!disposed) setLoyaltyLoading(false)
      }
    }
    void loadLoyalty()
    return () => { disposed = true }
  }, [isAuthenticated, customerId])

  // Compute progress percentage and next tier info
  const progressPercent = useMemo(() => {
    if (!customerLoyalty || !customerLoyalty.pointsToNextTier) return 100
    const currentMin = tiers.find(t => t.tier === customerLoyalty.currentTier)?.minPoints ?? 0
    const nextMin = customerLoyalty.pointsToNextTier + customerLoyalty.accumulatedPoints
    const range = nextMin - currentMin
    if (range <= 0) return 100
    const progress = ((customerLoyalty.accumulatedPoints - currentMin) / range) * 100
    return Math.min(100, Math.max(0, progress))
  }, [customerLoyalty, tiers])

  // Save additional faked information
  const handleSaveAdditionalInfo = () => {
    if (!customerId) return
    const info = { dob, gender, address, favoriteGenres, phoneVal }
    localStorage.setItem(`profile_info_${customerId}`, JSON.stringify(info))
    success("Thông tin cá nhân đã được cập nhật thành công!")
  }

  // Handle genre click
  const toggleGenre = (genre: string) => {
    setFavoriteGenres((prev) =>
      prev.includes(genre) ? prev.filter((g) => g !== genre) : [...prev, genre]
    )
  }

  // Change password states
  const [currentPassword, setCurrentPassword] = useState("")
  const [newPassword, setNewPassword] = useState("")
  const [confirmPassword, setConfirmPassword] = useState("")
  const [changingPass, setChangingPass] = useState(false)

  const handleUpdatePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!currentPassword.trim() || !newPassword.trim() || !confirmPassword.trim()) {
      toastError("Vui lòng điền đầy đủ các trường mật khẩu.")
      return
    }
    if (newPassword !== confirmPassword) {
      toastError("Mật khẩu mới và xác nhận mật khẩu không khớp.")
      return
    }
    if (newPassword.length < 6) {
      toastError("Mật khẩu mới phải có tối thiểu 6 ký tự.")
      return
    }

    setChangingPass(true)
    try {
      await changePassword({ currentPassword, newPassword })
      success("Đổi mật khẩu thành công!")
      setCurrentPassword("")
      setNewPassword("")
      setConfirmPassword("")
    } catch (err) {
      if (isAxiosError(err) && err.response?.data) {
        const payload = err.response.data as { detail?: string; title?: string; errors?: Record<string, string[]> }
        let msg = payload.detail ?? payload.title ?? "Không đổi được mật khẩu."
        if (payload.errors) {
          const firstErrorList = Object.values(payload.errors)[0]
          if (firstErrorList && firstErrorList.length > 0) {
            msg = firstErrorList[0]
          }
        }
        toastError(msg)
      } else {
        toastError("Không thể đổi mật khẩu. Vui lòng kiểm tra lại mật khẩu hiện tại.")
      }
    } finally {
      setChangingPass(false)
    }
  }

  // Booking history states
  const [pageNumber, setPageNumber] = useState(1)
  const [historyLoading, setHistoryLoading] = useState(true)
  const [historyError, setHistoryError] = useState<string | null>(null)
  const [bookings, setBookings] = useState<BookingHistoryItemDto[]>([])
  const [totalPages, setTotalPages] = useState(1)
  const [totalItems, setTotalItems] = useState(0)

  // Coupon vault states
  const [coupons, setCoupons] = useState<CustomerCouponDto[]>([])
  const [couponsLoading, setCouponsLoading] = useState(false)

  useEffect(() => {
    if (activeTab !== "history" || !isAuthenticated || !customerId) {
      return
    }

    let disposed = false
    async function loadBookingHistory() {
      setHistoryLoading(true)
      setHistoryError(null)
      try {
        const response = await getBookingHistory(customerId!, { pageNumber, pageSize: PAGE_SIZE })
        if (disposed) return
        setBookings(response.items)
        setTotalPages(Math.max(1, response.totalPages))
        setTotalItems(response.totalItems)
      } catch (error) {
        if (disposed) return
        if (isAxiosError(error) && error.response?.data) {
          const payload = error.response.data as { detail?: string; title?: string; message?: string }
          const msg = payload.detail ?? payload.title ?? payload.message ?? "Không tải được lịch sử đặt vé."
          setHistoryError(msg)
          toastError(msg)
        } else {
          setHistoryError("Không tải được lịch sử đặt vé.")
          toastError("Không tải được lịch sử đặt vé.")
        }
      } finally {
        if (!disposed) {
          setHistoryLoading(false)
        }
      }
    }

    void loadBookingHistory()
    return () => {
      disposed = true
    }
  }, [customerId, isAuthenticated, pageNumber, activeTab, toastError])

  // Coupon vault loading
  useEffect(() => {
    if (activeTab !== "coupons" || !isAuthenticated || !customerId) return

    let disposed = false
    async function loadCoupons() {
      setCouponsLoading(true)
      try {
        const data = await getMyCoupons()
        if (!disposed) setCoupons(data)
      } catch {
        // non-critical
      } finally {
        if (!disposed) setCouponsLoading(false)
      }
    }
    void loadCoupons()
    return () => { disposed = true }
  }, [activeTab, isAuthenticated, customerId])

  const pageInfoLabel = useMemo(() => `Trang ${pageNumber}/${Math.max(1, totalPages)}`, [pageNumber, totalPages])

  if (!isAuthenticated) {
    return (
      <main className="mx-auto min-h-[60vh] w-full max-w-screen-xl px-8 pb-12 pt-28">
        <div className="rounded-2xl border border-outline-variant/20 bg-surface-container-low p-8 text-center shadow-2xl">
          <h1 className="font-headline text-3xl font-bold">Quản lý tài khoản</h1>
          <p className="mt-3 text-on-surface-variant">Bạn cần đăng nhập để quản lý trang cá nhân cá nhân.</p>
        </div>
      </main>
    )
  }

  if (!customerId && isResolvingProfile) {
    return (
      <main className="mx-auto min-h-[60vh] w-full max-w-screen-xl px-8 pb-12 pt-28">
        <div className="rounded-2xl border border-outline-variant/20 bg-surface-container-low p-8 text-center shadow-2xl">
          <h1 className="font-headline text-3xl font-bold">Quản lý tài khoản</h1>
          <p className="mt-3 text-on-surface-variant">Đang đồng bộ thông tin tài khoản...</p>
        </div>
      </main>
    )
  }

  return (
    <main className="mx-auto min-h-screen w-full max-w-screen-2xl px-6 pb-20 pt-28 md:px-8">
      {/* Title with Profile Overview and Progress Bar */}
      <section className="mb-10 rounded-3xl border border-outline-variant/20 bg-surface-container-low p-6 shadow-2xl backdrop-blur-xl md:p-8 flex flex-col gap-6 md:gap-8">

        {/* Profile Overview Banner */}
        <div className="flex flex-col items-center gap-6 md:flex-row text-center md:text-left">
          <div className="relative">
            <img
              src={avatarUrl && avatarUrl.trim() !== "" ? avatarUrl : `https://api.dicebear.com/7.x/adventurer/svg?seed=${encodeURIComponent(displayName || "User")}`}
              alt={displayName ?? "Avatar"}
              className="h-24 w-24 rounded-full border-2 border-primary/30 object-cover shadow-lg shadow-black/30 md:h-28 md:w-28"
            />
            <div className="absolute bottom-0 right-0 flex h-8 w-8 items-center justify-center rounded-full bg-primary p-1 text-on-primary shadow-lg md:h-9 md:w-9 md:p-1.5">
              <span className="material-symbols-outlined text-base font-bold md:text-lg">verified_user</span>
            </div>
          </div>
          <div className="flex-1">
            <h1 className="font-headline text-3xl font-black text-on-surface md:text-4xl">
              <span className="text-secondary drop-shadow-[0_0_8px_rgba(97,180,254,0.4)]">{displayName}</span>
            </h1>
            <p className="text-sm text-on-surface-variant mt-1">
              {loyaltyLoading
                ? "Đang tải thông tin hội viên..."
                : customerLoyalty && customerLoyalty.currentTier
                  ? `Khách hàng thành viên • ${TIER_LABELS[customerLoyalty.currentTier]}`
                  : "Khách hàng thành viên"}
            </p>

            <div className="mt-4 flex flex-wrap gap-4 justify-center md:justify-start text-sm">
              <div className="flex items-center gap-2 rounded-xl bg-background/50 border border-outline-variant/10 px-4 py-2">
                <span className="material-symbols-outlined text-primary text-lg">mail</span>
                <span className="text-on-surface font-semibold">{email || "—"}</span>
              </div>
              <div className="flex items-center gap-2 rounded-xl bg-background/50 border border-outline-variant/10 px-4 py-2">
                <span className="material-symbols-outlined text-primary text-lg">phone</span>
                <span className="text-on-surface font-semibold">{phoneVal || phoneNumber || "—"}</span>
              </div>
            </div>
          </div>
        </div>

        {/* Progress Bar below Overview - Loyalty (API-driven) */}
        {customerLoyalty && !loyaltyLoading && (
          <div className="border-t border-outline-variant/15 pt-6">
            <div className="space-y-4">
              <div className="flex items-center justify-between text-sm">
                <div className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-secondary">insights</span>
                  <span className="font-headline font-bold text-on-surface">Tích lũy cấp bậc hội viên</span>
                </div>
                <span className="font-mono font-bold text-secondary text-base">
                  {customerLoyalty.accumulatedPoints.toLocaleString("vi-VN")}
                  {customerLoyalty.pointsToNextTier != null
                    ? ` / ${(customerLoyalty.accumulatedPoints + customerLoyalty.pointsToNextTier).toLocaleString("vi-VN")} PTS`
                    : " PTS"}
                </span>
              </div>

              {/* Progress Track */}
              <div className="relative h-4 w-full overflow-hidden rounded-full border border-outline-variant/30 bg-background/60 shadow-[inset_0_2px_4px_rgba(0,0,0,0.6)]">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-primary via-secondary to-secondary shadow-[0_0_12px_rgba(0,244,254,0.6)] transition-all duration-1000 ease-out"
                  style={{ width: `${progressPercent}%` }}
                />
              </div>

              {/* Left & Right Bounds showing discount */}
              <div className="flex items-center justify-between text-xs md:text-sm font-semibold">
                <div className="flex flex-col">
                  <span className="text-on-surface-variant">{TIER_LABELS[customerLoyalty.currentTier]}</span>
                  <span className="text-primary font-bold">
                    {customerLoyalty.ticketDiscountPercent > 0
                      ? `Giảm ${customerLoyalty.ticketDiscountPercent}% vé`
                      : "Giá vé gốc"}
                  </span>
                </div>
                {customerLoyalty.nextTier && (
                  <div className="flex flex-col text-right">
                    <span className="text-on-surface-variant">{TIER_LABELS[customerLoyalty.nextTier]}</span>
                    <span className="text-secondary font-bold">
                      {(() => {
                        const next = tiers.find(t => t.tier === customerLoyalty.nextTier)
                        return next && next.ticketDiscountPercent > 0
                          ? `Giảm ${next.ticketDiscountPercent}% vé`
                          : "Phúc lợi cao hơn"
                      })()}
                    </span>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

      </section>

      {/* Grid Layout: Sidebar on Left, Content on Right */}
      <div className="grid gap-8 lg:grid-cols-4">

        {/* Sidebar navigation */}
        <aside className="lg:col-span-1">
          <div className="sticky top-28 flex flex-row overflow-x-auto rounded-2xl border border-outline-variant/20 bg-surface-container-low p-2 shadow-xl lg:flex-col lg:overflow-x-visible lg:p-3">
            {[
              { id: "info", label: "Thông tin cá nhân", icon: "person" },
              { id: "benefits", label: "Phúc lợi hội viên", icon: "workspace_premium" },
              { id: "coupons", label: "Kho mã giảm giá", icon: "redeem" },
              { id: "password", label: "Đổi mật khẩu", icon: "lock" },
              { id: "history", label: "Lịch sử đặt vé", icon: "confirmation_number" },
            ].map((tab) => {
              const active = activeTab === tab.id
              return (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setActiveTab(tab.id as typeof activeTab)}
                  className={`flex items-center gap-3 whitespace-nowrap rounded-xl px-5 py-4 text-sm font-bold transition-all duration-300 lg:w-full active:scale-95 ${active
                    ? "bg-primary/10 text-primary shadow-[inset_0_0_12px_rgba(97,180,254,0.15)] border border-primary/20"
                    : "text-slate-400 hover:bg-white/5 border border-transparent"
                    }`}
                >
                  <span className="material-symbols-outlined text-xl">{tab.icon}</span>
                  <span>{tab.label}</span>
                </button>
              )
            })}
          </div>
        </aside>

        {/* Dynamic Content Panel */}
        <section className="lg:col-span-3">
          <div className="rounded-3xl border border-outline-variant/20 bg-surface-container-low p-6 shadow-2xl backdrop-blur-xl md:p-10">

            {/* -------------------- TAB 1: THÔNG TIN CÁ NHÂN -------------------- */}
            {activeTab === "info" && (
              /* Personal Information Form */
              <article className="animate-fadeIn">
                <div className="mb-8">
                  <h3 className="font-headline text-2xl font-black text-on-surface flex items-center gap-2">
                    <span className="material-symbols-outlined text-secondary text-2xl">person</span>
                    <span>Thông tin chi tiết</span>
                  </h3>
                  <p className="mt-2 text-sm text-on-surface-variant">
                    Cập nhật ngày sinh, giới tính, địa chỉ liên hệ và thể loại phim ưa thích của bạn.
                  </p>
                </div>

                <div className="grid gap-6 md:grid-cols-2">

                  {/* Email (Readonly) */}
                  <div className="space-y-2">
                    <label htmlFor="email_field" className="text-sm font-bold text-on-surface-variant">Email</label>
                    <input
                      type="email"
                      id="email_field"
                      value={email || ""}
                      readOnly
                      className="w-full rounded-xl border border-outline-variant/30 bg-background/50 px-4 py-3 text-sm text-on-surface-variant cursor-not-allowed opacity-70 focus:outline-none"
                    />
                  </div>

                  {/* Phone Number */}
                  <div className="space-y-2">
                    <label htmlFor="phone_field" className="text-sm font-bold text-on-surface-variant">Số điện thoại</label>
                    <input
                      type="text"
                      id="phone_field"
                      placeholder="Nhập số điện thoại của bạn"
                      value={phoneVal}
                      onChange={(e) => setPhoneVal(e.target.value)}
                      className="w-full rounded-xl border border-outline-variant/30 bg-background px-4 py-3 text-sm text-on-surface transition-all focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  {/* DOB */}
                  <div className="space-y-2">
                    <label htmlFor="dob" className="text-sm font-bold text-on-surface-variant">Ngày sinh</label>
                    <input
                      type="date"
                      id="dob"
                      value={dob}
                      onChange={(e) => setDob(e.target.value)}
                      className="w-full rounded-xl border border-outline-variant/30 bg-background px-4 py-3 text-sm text-on-surface transition-all focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  {/* Gender */}
                  <div className="space-y-2">
                    <label className="text-sm font-bold text-on-surface-variant">Giới tính</label>
                    <div className="flex gap-4 pt-1">
                      {[
                        { id: "male", label: "Nam" },
                        { id: "female", label: "Nữ" },
                        { id: "other", label: "Khác" },
                      ].map((g) => (
                        <label key={g.id} className="flex cursor-pointer items-center gap-2 text-sm text-on-surface">
                          <input
                            type="radio"
                            name="gender"
                            value={g.id}
                            checked={gender === g.id}
                            onChange={() => setGender(g.id)}
                            className="accent-primary h-4 w-4"
                          />
                          <span>{g.label}</span>
                        </label>
                      ))}
                    </div>
                  </div>

                  {/* Address */}
                  <div className="space-y-2 md:col-span-2">
                    <label htmlFor="address" className="text-sm font-bold text-on-surface-variant">Địa chỉ</label>
                    <input
                      type="text"
                      id="address"
                      placeholder="Nhập địa chỉ của bạn"
                      value={address}
                      onChange={(e) => setAddress(e.target.value)}
                      className="w-full rounded-xl border border-outline-variant/30 bg-background px-4 py-3 text-sm text-on-surface transition-all focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  {/* Favorite Genres multiselect */}
                  <div className="space-y-3 md:col-span-2">
                    <label className="text-sm font-bold text-on-surface-variant">Thể loại phim yêu thích</label>
                    <div className="flex flex-wrap gap-2 pt-1">
                      {AVAILABLE_GENRES.map((genre) => {
                        const selected = favoriteGenres.includes(genre)
                        return (
                          <button
                            key={genre}
                            type="button"
                            onClick={() => toggleGenre(genre)}
                            className={`rounded-full px-4 py-2 text-xs font-semibold tracking-wide transition-all ${selected
                              ? "bg-secondary/15 text-secondary border border-secondary/40 shadow-[0_0_8px_rgba(0,244,254,0.2)]"
                              : "bg-background border border-outline-variant/30 text-on-surface-variant hover:text-on-surface hover:border-outline"
                              }`}
                          >
                            {genre}
                          </button>
                        )
                      })}
                    </div>
                  </div>

                </div>

                <div className="mt-8 flex justify-end">
                  <button
                    type="button"
                    onClick={handleSaveAdditionalInfo}
                    className="rounded-full bg-gradient-to-r from-primary to-primary-container px-8 py-3.5 text-sm font-bold text-on-primary shadow-lg shadow-primary/20 transition-transform active:scale-95 hover:scale-[1.02]"
                  >
                    Cập nhật thông tin
                  </button>
                </div>
              </article>
            )}

            {/* -------------------- TAB 2: PHÚC LỢI HỘI VIÊN -------------------- */}
            {activeTab === "benefits" && (
              <div className="space-y-8 animate-fadeIn">
                <div>
                  <h3 className="font-headline text-2xl font-black text-on-surface flex items-center gap-2">
                    <span className="material-symbols-outlined text-secondary text-2xl">workspace_premium</span>
                    <span>Phân hạng & Phúc lợi Hội viên</span>
                  </h3>
                  <p className="mt-2 text-sm text-on-surface-variant">
                    Dưới đây là chi tiết quyền lợi của từng cấp bậc tài khoản dựa trên điểm tích lũy đạt được.
                  </p>
                </div>

                {loyaltyLoading ? (
                  <div className="flex min-h-[200px] items-center justify-center">
                    <span className="inline-block h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                  </div>
                ) : (
                  <div className="grid gap-6 sm:grid-cols-2">
                    {[...tiers].sort((a, b) => {
                      const tierOrder = { Bronze: 0, Silver: 1, Gold: 2, Platinum: 3, Diamond: 4, Ruby: 5 }
                      const keyA = typeof a.tier === "number" ? (TIER_NUMBER_MAP[a.tier] ?? "Bronze") : a.tier
                      const keyB = typeof b.tier === "number" ? (TIER_NUMBER_MAP[b.tier] ?? "Bronze") : b.tier
                      return (tierOrder[keyA] ?? 0) - (tierOrder[keyB] ?? 0)
                    }).map((tier) => {
                      const tierKey = typeof tier.tier === "number" ? (TIER_NUMBER_MAP[tier.tier] ?? "Bronze") : tier.tier
                      const colors = TIER_COLORS[tierKey] ?? TIER_COLORS.Bronze
                      const isMyTier = customerLoyalty?.currentTier === tierKey
                      return (
                        <article
                          key={tier.id}
                          className={`relative overflow-hidden rounded-2xl border p-6 backdrop-blur-md bg-gradient-to-br transition-all hover:scale-[1.01] ${colors.gradient
                            } ${isMyTier ? "ring-2 ring-primary ring-offset-2 ring-offset-background" : ""}`}
                        >
                          {isMyTier && (
                            <div className="absolute right-4 top-4 rounded-full bg-primary/20 px-3 py-1 text-[10px] font-black uppercase tracking-wider text-primary border border-primary/30">
                              Hạng của bạn
                            </div>
                          )}

                          <div className="flex items-center gap-3">
                            <span className={`material-symbols-outlined text-3xl ${colors.textColor}`}>
                              {TIER_ICONS[tierKey]}
                            </span>
                            <div>
                              <h4 className="font-headline text-lg font-black text-on-surface">{TIER_LABELS[tierKey]}</h4>
                              <span className="text-xs font-bold text-on-surface-variant font-mono">
                                {tier.minPoints.toLocaleString("vi-VN")}
                                {tier.maxPoints != null ? ` - ${tier.maxPoints.toLocaleString("vi-VN")} PTS` : "+ PTS"}
                              </span>
                            </div>
                          </div>

                          <div className="mt-4 border-t border-outline-variant/15 pt-4">
                            <div className={`text-sm font-bold ${colors.textColor} mb-3`}>
                              {tier.ticketDiscountPercent > 0
                                ? `Giảm ${tier.ticketDiscountPercent}% vé`
                                : "Giá vé gốc"}
                              {tier.concessionDiscountPercent > 0 && ` • Giảm ${tier.concessionDiscountPercent}% bắp nước`}
                            </div>
                            {tier.description && (
                              <p className="text-xs text-on-surface-variant">{tier.description}</p>
                            )}
                          </div>
                        </article>
                      )
                    })}
                  </div>
                )}
              </div>
            )}

            {/* -------------------- TAB 3: KHO MÃ GIẢM GIÁ -------------------- */}
            {activeTab === "coupons" && (
              <div className="space-y-8 animate-fadeIn">
                <div>
                  <h3 className="font-headline text-2xl font-black text-on-surface flex items-center gap-2">
                    <span className="material-symbols-outlined text-secondary text-2xl">redeem</span>
                    <span>Kho mã giảm giá</span>
                  </h3>
                  <p className="mt-2 text-sm text-on-surface-variant">
                    Các mã giảm giá dành riêng cho bạn. Mỗi mã chỉ được sử dụng một lần.
                  </p>
                </div>

                {couponsLoading ? (
                  <div className="flex min-h-[200px] items-center justify-center">
                    <span className="inline-block h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                  </div>
                ) : coupons.length === 0 ? (
                  <div className="rounded-2xl border border-outline-variant/20 bg-background/30 p-12 text-center text-on-surface-variant">
                    <span className="material-symbols-outlined text-4xl mb-3">card_giftcard</span>
                    <p>Bạn chưa có mã giảm giá nào.</p>
                  </div>
                ) : (
                  <div className="grid gap-4 md:grid-cols-2">
                    {coupons.map((c) => (
                      <article
                        key={c.id}
                        className={`rounded-2xl border p-5 transition-all ${
                          c.isUsed
                            ? "border-outline-variant/10 bg-surface-container-high/20 opacity-60"
                            : "border-secondary/20 bg-surface-container-high/40 hover:scale-[1.01]"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-3 mb-3">
                          <div>
                            <code className="rounded-lg bg-white/10 px-3 py-1 font-mono text-sm font-bold text-secondary tracking-wider">
                              {c.couponCode}
                            </code>
                            <span className={`ml-2 rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-widest ${
                              c.isUsed
                                ? "bg-rose-500/10 text-rose-300"
                                : "bg-emerald-500/10 text-emerald-300"
                            }`}>
                              {c.isUsed ? "Đã dùng" : "Còn hiệu lực"}
                            </span>
                          </div>
                          <span className="material-symbols-outlined text-2xl text-secondary">redeem</span>
                        </div>

                        <div className="text-2xl font-bold text-secondary mb-2">
                          {c.discountType === "Fixed"
                            ? `${c.discountValue.toLocaleString("vi-VN")}đ`
                            : `${c.discountValue}%`}
                          {c.maxDiscountAmount && c.discountType === "Percentage" && (
                            <span className="text-xs font-normal text-on-surface-variant ml-1">
                              (tối đa {c.maxDiscountAmount.toLocaleString("vi-VN")}đ)
                            </span>
                          )}
                        </div>

                        <div className="flex justify-between text-xs text-on-surface-variant">
                          <span>
                            Phạm vi: {c.scope === "All" ? "Toàn bộ" : c.scope === "Tickets" ? "Vé" : "Bắp nước"}
                          </span>
                          <span>
                            HSD: {new Date(c.expiresAt).toLocaleDateString("vi-VN")}
                          </span>
                        </div>

                        {!c.isUsed && (
                          <button
                            type="button"
                            onClick={() => {
                              navigator.clipboard.writeText(c.couponCode).catch(() => {})
                            }}
                            className="mt-3 w-full rounded-lg border border-secondary/30 py-2 text-xs font-bold text-secondary transition-colors hover:bg-secondary/10"
                          >
                            Sao chép mã
                          </button>
                        )}
                      </article>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* -------------------- TAB 4: ĐỔI MẬT KHẨU -------------------- */}
            {activeTab === "password" && (
              <div className="space-y-8 animate-fadeIn">
                <div>
                  <h3 className="font-headline text-2xl font-black text-on-surface flex items-center gap-2">
                    <span className="material-symbols-outlined text-secondary text-2xl">lock_reset</span>
                    <span>Đổi mật khẩu</span>
                  </h3>
                  <p className="mt-2 text-sm text-on-surface-variant">
                    Bảo mật tài khoản bằng cách thay đổi mật khẩu định kỳ. Nhập mật khẩu cũ và mới của bạn dưới đây.
                  </p>
                </div>

                <form onSubmit={handleUpdatePassword} className="max-w-lg space-y-6">

                  {/* Current Password */}
                  <div className="space-y-2">
                    <label htmlFor="curr_pass" className="text-sm font-bold text-on-surface-variant">
                      Mật khẩu hiện tại
                    </label>
                    <input
                      type="password"
                      id="curr_pass"
                      placeholder="••••••••"
                      value={currentPassword}
                      onChange={(e) => setCurrentPassword(e.target.value)}
                      required
                      className="w-full rounded-xl border border-outline-variant/30 bg-background px-4 py-3 text-sm text-on-surface transition-all focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  {/* New Password */}
                  <div className="space-y-2">
                    <label htmlFor="new_pass" className="text-sm font-bold text-on-surface-variant">
                      Mật khẩu mới
                    </label>
                    <input
                      type="password"
                      id="new_pass"
                      placeholder="••••••••"
                      value={newPassword}
                      onChange={(e) => setNewPassword(e.target.value)}
                      required
                      className="w-full rounded-xl border border-outline-variant/30 bg-background px-4 py-3 text-sm text-on-surface transition-all focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  {/* Confirm New Password */}
                  <div className="space-y-2">
                    <label htmlFor="confirm_pass" className="text-sm font-bold text-on-surface-variant">
                      Xác nhận mật khẩu mới
                    </label>
                    <input
                      type="password"
                      id="confirm_pass"
                      placeholder="••••••••"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      required
                      className="w-full rounded-xl border border-outline-variant/30 bg-background px-4 py-3 text-sm text-on-surface transition-all focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={changingPass}
                    className="w-full rounded-full bg-gradient-to-r from-primary to-primary-container py-4 text-sm font-bold text-on-primary shadow-lg shadow-primary/20 transition-transform active:scale-95 hover:scale-[1.01] disabled:opacity-60 disabled:pointer-events-none"
                  >
                    {changingPass ? "Đang xử lý..." : "Cập nhật mật khẩu"}
                  </button>

                </form>
              </div>
            )}

            {/* -------------------- TAB 4: LỊCH SỬ ĐẶT VÉ -------------------- */}
            {activeTab === "history" && (
              <div className="space-y-8 animate-fadeIn">
                <div className="flex flex-wrap items-end justify-between gap-4">
                  <div>
                    <h3 className="font-headline text-2xl font-black text-on-surface flex items-center gap-2">
                      <span className="material-symbols-outlined text-secondary text-2xl">confirmation_number</span>
                      <span>Lịch sử đặt vé</span>
                    </h3>
                    <p className="mt-2 text-sm text-on-surface-variant">
                      Dưới đây là danh sách các giao dịch đặt vé xem phim của bạn tại Vicinema.
                    </p>
                  </div>
                  <span className="rounded-full border border-outline-variant/30 px-4 py-2 text-xs font-mono font-bold tracking-widest text-on-surface-variant bg-background/55">
                    Tổng {totalItems} đơn
                  </span>
                </div>

                {historyError && (
                  <div className="rounded-xl border border-red-400/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
                    {historyError}
                  </div>
                )}

                {historyLoading ? (
                  <div className="flex min-h-[280px] items-center justify-center">
                    <span className="inline-block h-10 w-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                  </div>
                ) : bookings.length === 0 ? (
                  <div className="rounded-2xl border border-outline-variant/20 bg-background/30 p-12 text-center text-on-surface-variant">
                    Bạn chưa thực hiện giao dịch đặt vé nào.
                  </div>
                ) : (
                  <div className="space-y-4">
                    {bookings.map((booking) => {
                      const status = normalizeBookingStatus(booking.status)
                      return (
                        <article
                          key={booking.bookingId}
                          className="rounded-2xl border border-outline-variant/15 bg-surface-container-high/40 p-5 shadow-lg transition-transform duration-300 hover:scale-[1.005]"
                        >
                          <div className="flex flex-wrap items-start justify-between gap-3 border-b border-outline-variant/15 pb-4">
                            <div>
                              <p className="text-[10px] uppercase tracking-widest text-on-surface-variant font-bold">Mã đơn</p>
                              <p className="font-mono text-sm font-black text-primary">{booking.bookingId}</p>
                            </div>
                            <span className={`rounded-full border px-3.5 py-1 text-xs font-bold ${status.className}`}>
                              {status.label}
                            </span>
                          </div>

                          <div className="mt-4 grid gap-4 text-sm md:grid-cols-2 lg:grid-cols-4">
                            <div>
                              <p className="text-[11px] text-on-surface-variant font-bold">Phim</p>
                              <p className="font-semibold text-on-surface mt-0.5">{booking.showTimeInfo.movie}</p>
                            </div>
                            <div>
                              <p className="text-[11px] text-on-surface-variant font-bold">Phòng chiếu & Rạp</p>
                              <p className="font-semibold text-on-surface mt-0.5">{booking.showTimeInfo.screen}</p>
                            </div>
                            <div>
                              <p className="text-[11px] text-on-surface-variant font-bold">Suất chiếu</p>
                              <p className="font-semibold text-on-surface mt-0.5">{formatDateTime(booking.showTimeInfo.startAt)}</p>
                            </div>
                            <div>
                              <p className="text-[11px] text-on-surface-variant font-bold">Thành tiền</p>
                              <p className="font-bold text-secondary text-base mt-0.5">{formatCurrency(booking.finalAmount)}</p>
                            </div>
                          </div>

                          <div className="mt-4 flex items-center justify-between border-t border-outline-variant/15 pt-3 text-[11px] text-on-surface-variant font-medium">
                            <span>Đặt lúc: {formatDateTime(booking.createdAt)}</span>
                          </div>
                        </article>
                      )
                    })}

                    {/* Pagination */}
                    <section className="mt-8 flex items-center justify-end gap-3 border-t border-outline-variant/15 pt-6">
                      <button
                        type="button"
                        onClick={() => setPageNumber((prev) => Math.max(1, prev - 1))}
                        disabled={historyLoading || pageNumber <= 1}
                        className="rounded-full border border-outline-variant/30 px-5 py-2 text-xs font-bold text-on-surface hover:bg-white/5 disabled:cursor-not-allowed disabled:opacity-50 transition-colors"
                      >
                        Trang trước
                      </button>
                      <span className="text-xs font-bold text-on-surface-variant font-mono">{pageInfoLabel}</span>
                      <button
                        type="button"
                        onClick={() => setPageNumber((prev) => Math.min(totalPages, prev + 1))}
                        disabled={historyLoading || pageNumber >= totalPages}
                        className="rounded-full border border-outline-variant/30 px-5 py-2 text-xs font-bold text-on-surface hover:bg-white/5 disabled:cursor-not-allowed disabled:opacity-50 transition-colors"
                      >
                        Trang sau
                      </button>
                    </section>
                  </div>
                )}
              </div>
            )}

          </div>
        </section>

      </div>
    </main>
  )
}

export default Profile
