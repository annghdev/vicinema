import { useCallback, useEffect, useRef, useState } from "react"
import { Link, useNavigate, useParams } from "react-router-dom"
import { getPromotionById } from "../apis/promotionApi"
import type { PromotionProgramDetailDto } from "../types/Promotion"
import { useLoading } from "../contexts/LoadingContext"
import { useToast } from "../contexts/ToastContext"

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  })
}

function discountDescription(dto: PromotionProgramDetailDto) {
  const { discountType, discountForm, discountValue, maxDiscountAmount, maxDiscountPercentage } = dto
  const val = discountValue.toLocaleString("vi-VN")
  let desc = ""
  if (discountType === "Percentage") {
    desc = `Giảm ${val}%`
    if (maxDiscountAmount != null) desc += ` (tối đa ${maxDiscountAmount.toLocaleString("vi-VN")}₫)`
    else if (maxDiscountPercentage != null) desc += ` (tối đa ${maxDiscountPercentage}%)`
  } else if (discountType === "Fixed") {
    desc = `Giảm ${val}₫`
  } else if (discountType === "BuyXGetY") {
    desc = `Mua ${val} sản phẩm, tặng sản phẩm tương ứng`
  } else {
    desc = `${discountType} ${val}`
  }
  if (discountForm === "GiftItem") desc += " (tặng kèm)"
  return desc
}

function conditionLabel(conditionType: string, value: string | null, secondaryValue: string | null) {
  switch (conditionType) {
    case "MinTicketQuantity":
      return `Số lượng vé tối thiểu: ${value ?? "—"}`
    case "MinOrderAmount":
      return `Giá trị đơn hàng tối thiểu: ${Number(value ?? 0).toLocaleString("vi-VN")}₫`
    case "SpecificMovie":
      return `Chỉ áp dụng cho phim cụ thể`
    case "SpecificShowtime":
      return `Chỉ áp dụng cho suất chiếu cụ thể`
    case "SpecificDayOfWeek":
      return `Chỉ áp dụng thứ: ${value ?? "—"}`
    case "SpecificDateRange":
      return `Áp dụng từ ${value ?? "—"} đến ${secondaryValue ?? "—"}`
    default:
      return `${conditionType}: ${value ?? "—"}${secondaryValue ? ` / ${secondaryValue}` : ""}`
  }
}

function PromotionDetail() {
  const { promotionId } = useParams<{ promotionId: string }>()
  const navigate = useNavigate()
  const { showLoading, hideLoading } = useLoading()
  const { error: toastError } = useToast()
  const [promotion, setPromotion] = useState<PromotionProgramDetailDto | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const cancelledRef = useRef(false)

  const load = useCallback(async () => {
    if (!promotionId) return
    cancelledRef.current = false
    try {
      showLoading("Đang tải thông tin khuyến mãi...")
      setLoading(true)
      setLoadError(null)
      const data = await getPromotionById(promotionId)
      if (cancelledRef.current) return
      setPromotion(data)
    } catch {
      if (!cancelledRef.current) {
        toastError("Không thể tải thông tin khuyến mãi.")
        setLoadError("Không tải được dữ liệu khuyến mãi từ backend.")
      }
    } finally {
      if (!cancelledRef.current) {
        setLoading(false)
        hideLoading()
      }
    }
  }, [promotionId, showLoading, hideLoading, toastError])

  useEffect(() => {
    void load()
    return () => { cancelledRef.current = true }
  }, [load])

  if (!promotionId) {
    navigate("/promos", { replace: true })
    return null
  }

  if (loadError) {
    return (
      <main className="min-h-screen bg-background pb-20 pt-24 md:pt-28">
        <div className="mx-auto w-full max-w-screen-2xl px-8 py-16">
          <div className="rounded-xl border border-red-400/30 bg-red-500/10 p-8 text-center text-red-200">{loadError}</div>
        </div>
      </main>
    )
  }

  if (loading || !promotion) {
    return null
  }

  return (
    <main className="min-h-screen bg-background pb-20 pt-24 md:pt-28">
      <div className="mx-auto w-full max-w-screen-2xl px-8 py-8">
        <Link
          to="/promos"
          className="mb-8 inline-flex items-center gap-2 text-sm font-semibold text-on-surface-variant hover:text-primary transition-colors"
        >
          <span className="material-symbols-outlined text-lg">arrow_back</span>
          Quay lại danh sách khuyến mãi
        </Link>

        <div className="grid grid-cols-1 gap-10 lg:grid-cols-5">
          <div className="lg:col-span-2">
            <div className="sticky top-28 overflow-hidden rounded-xl border border-outline-variant/20 bg-surface-container-low">
              {promotion.posterImage ? (
                <img
                  src={promotion.posterImage}
                  alt={promotion.name}
                  className="w-full object-cover"
                />
              ) : (
                <div className="flex h-80 items-center justify-center">
                  <span className="material-symbols-outlined text-8xl text-on-surface-variant/20">sell</span>
                </div>
              )}
            </div>
          </div>

          <div className="lg:col-span-3">
            <h1 className="font-headline text-4xl font-black tracking-tight text-on-background md:text-5xl">
              {promotion.name}
            </h1>
            {promotion.description && (
              <p className="mt-4 max-w-3xl text-lg leading-relaxed text-on-surface-variant">
                {promotion.description}
              </p>
            )}

            <div className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-5 py-4">
                <p className="text-[10px] font-bold uppercase tracking-widest text-secondary opacity-80">Thời gian</p>
                <p className="mt-1.5 font-semibold text-on-background">
                  {formatDate(promotion.startDate)} - {formatDate(promotion.endDate)}
                </p>
              </div>
              <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-5 py-4">
                <p className="text-[10px] font-bold uppercase tracking-widest text-secondary opacity-80">Giảm giá</p>
                <p className="mt-1.5 font-semibold text-on-background">{discountDescription(promotion)}</p>
              </div>
              {promotion.maxUsagePerCustomer != null && (
                <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-5 py-4">
                  <p className="text-[10px] font-bold uppercase tracking-widest text-secondary opacity-80">Giới hạn</p>
                  <p className="mt-1.5 font-semibold text-on-background">
                    Tối đa {promotion.maxUsagePerCustomer} lần / khách hàng
                  </p>
                </div>
              )}
              <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-5 py-4">
                <p className="text-[10px] font-bold uppercase tracking-widest text-secondary opacity-80">Trạng thái</p>
                <p className="mt-1.5 font-semibold text-on-background">
                  <span className={`inline-flex items-center gap-1.5 ${promotion.isActive ? "text-green-400" : "text-rose-400"}`}>
                    <span className={`inline-block h-2 w-2 rounded-full ${promotion.isActive ? "bg-green-400" : "bg-rose-400"}`} />
                    {promotion.isActive ? "Đang diễn ra" : "Đã kết thúc"}
                  </span>
                </p>
              </div>
            </div>

            {promotion.conditions.length > 0 && (
              <div className="mt-8 rounded-lg border border-outline-variant/20 bg-surface-container/70 px-5 py-5">
                <h2 className="font-headline text-xl font-bold text-on-background">Điều kiện áp dụng</h2>
                <ul className="mt-4 space-y-3">
                  {promotion.conditions.map((cond, idx) => (
                    <li key={cond.id ?? idx} className="flex items-start gap-3">
                      <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-secondary/20 text-[11px] font-bold text-secondary">
                        {idx + 1}
                      </span>
                      <span className="text-sm text-on-surface-variant">
                        {conditionLabel(cond.conditionType, cond.value, cond.secondaryValue)}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {promotion.freeConcessionItems.length > 0 && (
              <div className="mt-8 rounded-lg border border-outline-variant/20 bg-surface-container/70 px-5 py-5">
                <h2 className="font-headline text-xl font-bold text-on-background">Quà tặng kèm</h2>
                <ul className="mt-4 space-y-3">
                  {promotion.freeConcessionItems.map((item, idx) => (
                    <li key={item.id ?? idx} className="flex items-center gap-3">
                      <span className="material-symbols-outlined text-xl text-primary">card_giftcard</span>
                      <span className="text-sm text-on-surface-variant">
                        {item.quantity} món (kèm theo)
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </div>
      </div>
    </main>
  )
}

export default PromotionDetail
