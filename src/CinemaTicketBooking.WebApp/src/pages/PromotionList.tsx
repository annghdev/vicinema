import { useEffect, useMemo, useState } from "react"
import { Link } from "react-router-dom"
import { getActivePromotions } from "../apis/promotionApi"
import type { PromotionProgramDto } from "../types/Promotion"
import { useLoading } from "../contexts/LoadingContext"

function discountLabel(dto: PromotionProgramDto) {
  const { discountType, discountForm, discountValue } = dto
  const val = discountValue.toLocaleString("vi-VN")
  if (discountType === "Percentage") {
    const suffix = discountForm === "MaxDiscount" ? " (tối đa)" : ""
    return `Giảm ${val}%${suffix}`
  }
  if (discountType === "Fixed") return `Giảm ${val}₫`
  if (discountType === "BuyXGetY") return `Mua ${val} tặng`
  if (discountForm === "GiftItem") return "Tặng kèm"
  return `${discountType} ${val}`
}

function formatDateRange(start: string, end: string) {
  const fmt: Intl.DateTimeFormatOptions = { day: "2-digit", month: "2-digit", year: "numeric" }
  const s = new Date(start).toLocaleDateString("vi-VN", fmt)
  const e = new Date(end).toLocaleDateString("vi-VN", fmt)
  return `${s} - ${e}`
}

function daysLeft(end: string) {
  const diff = new Date(end).getTime() - Date.now()
  if (diff <= 0) return 0
  return Math.ceil(diff / 86400000)
}

function PromotionList() {
  const [promotions, setPromotions] = useState<PromotionProgramDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const { showLoading, hideLoading } = useLoading()

  useEffect(() => {
    async function load() {
      try {
        showLoading("Đang tải danh sách khuyến mãi...")
        const data = await getActivePromotions()
        setPromotions(data)
        setError(null)
      } catch {
        setError("Không tải được danh sách khuyến mãi.")
      } finally {
        hideLoading()
      }
    }
    void load()
  }, [showLoading, hideLoading])

  const sorted = useMemo(() => {
    return [...promotions].sort(
      (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
    )
  }, [promotions])

  if (error) {
    return (
      <main className="min-h-screen bg-background pb-20 pt-24 md:pt-28">
        <div className="mx-auto w-full max-w-screen-2xl px-8 py-16">
          <div className="rounded-xl border border-red-400/30 bg-red-500/10 p-8 text-center text-red-200">{error}</div>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-background pb-20 pt-24 md:pt-28">
      <section className="relative overflow-hidden border-b border-outline-variant/10 bg-surface-container-low/40 py-20">
        <div className="absolute inset-0">
          <img
            src="https://lh3.googleusercontent.com/aida-public/AB6AXuAXFfKSlBIjWgJAXE2TSgP4bQr2F4Uw4bWNSF8ujCRXUkNKzCbCAR3zDdnq_7PRBiOtaIu_yZ3tVPyZZSf_t7yhF7McQ8FCetR3i0d-2hrlWjvCaFpVDa_kuGMPNnbe5ZUmE0izxS2Gbjhqn4pbytGwVCheCOx6NCMTpdH7fT5dxlHbTmWa0GXHAMoPOzbDNwuQBhFil2L4OgKBlOMZu1MH9-AeBpKh0sl7fvz92gPhzC44s_kANtNWI1myHc6X0X-as7Rsc3VHcow"
            alt=""
            className="h-full w-full object-cover opacity-20"
          />
          <div className="absolute inset-0 bg-gradient-to-r from-background via-background/85 to-background/40" />
        </div>
        <div className="relative z-10 mx-auto w-full max-w-screen-2xl px-8">
          <span className="mb-4 inline-flex items-center gap-2 border border-secondary/20 bg-secondary/10 px-3 py-1 text-sm font-semibold tracking-[0.14em] text-secondary">
            <span className="material-symbols-outlined text-base">sell</span>
            KHUYẾN MÃI
          </span>
          <h1 className="font-headline text-5xl font-black tracking-tight text-on-background md:text-7xl">
            CHƯƠNG TRÌNH
            <br />
            <span className="text-primary drop-shadow-[0_0_15px_rgba(97,180,254,0.35)]">KHUYẾN MÃI & ƯU ĐÃI</span>
          </h1>
          <p className="mt-4 max-w-3xl text-lg text-on-surface-variant">
            Khám phá các chương trình khuyến mãi đang diễn ra tại rạp. Áp dụng khi đặt vé online hoặc tại quầy.
          </p>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto w-full max-w-screen-2xl px-8">
          {sorted.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-24 text-center">
              <span className="material-symbols-outlined text-6xl text-on-surface-variant/30">sell</span>
              <h3 className="mt-4 font-headline text-2xl font-bold text-on-background">Chưa có khuyến mãi</h3>
              <p className="mt-2 text-on-surface-variant">Hiện tại chưa có chương trình khuyến mãi nào đang diễn ra.</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {sorted.map((promo) => {
                const remaining = daysLeft(promo.endDate)
                return (
                  <Link
                    key={promo.id}
                    to={`/promos/${promo.id}`}
                    className="group flex flex-col overflow-hidden rounded-xl border border-outline-variant/20 bg-surface-container-low transition-all duration-300 hover:border-secondary/40 hover:shadow-[0_8px_28px_rgba(0,0,0,0.35)]"
                  >
                    <div className="relative h-48 overflow-hidden bg-surface-container-highest">
                      {promo.posterImage ? (
                        <img
                          src={promo.posterImage}
                          alt={promo.name}
                          className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
                        />
                      ) : (
                        <div className="flex h-full items-center justify-center">
                          <span className="material-symbols-outlined text-6xl text-on-surface-variant/20">sell</span>
                        </div>
                      )}
                      {remaining > 0 && remaining <= 7 && (
                        <div className="absolute right-2 top-2 rounded-full bg-rose-500/90 px-3 py-1 text-[11px] font-bold text-white shadow-lg">
                          Còn {remaining} ngày
                        </div>
                      )}
                    </div>
                    <div className="flex flex-1 flex-col p-5">
                      <h3 className="font-headline text-lg font-bold text-on-background group-hover:text-primary transition-colors line-clamp-2">
                        {promo.name}
                      </h3>
                      {promo.description && (
                        <p className="mt-2 flex-1 text-sm leading-relaxed text-on-surface-variant line-clamp-2">
                          {promo.description}
                        </p>
                      )}
                      <div className="mt-4 flex flex-wrap items-center gap-3 text-xs text-on-surface-variant">
                        <span className="inline-flex items-center gap-1 rounded-full border border-secondary/20 bg-secondary/10 px-2.5 py-1 font-semibold text-secondary">
                          {discountLabel(promo)}
                        </span>
                        {promo.conditionCount > 0 && (
                          <span className="inline-flex items-center gap-1">
                            <span className="material-symbols-outlined text-sm">info</span>
                            {promo.conditionCount} điều kiện
                          </span>
                        )}
                      </div>
                      <div className="mt-3 flex items-center gap-1 text-[11px] text-on-surface-variant/60">
                        <span className="material-symbols-outlined text-[14px]">schedule</span>
                        <span>{formatDateRange(promo.startDate, promo.endDate)}</span>
                      </div>
                    </div>
                  </Link>
                )
              })}
            </div>
          )}
        </div>
      </section>
    </main>
  )
}

export default PromotionList
