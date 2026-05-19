import { Link, useLocation } from "react-router-dom"
import type { ShowTimeDto } from "../types/ShowTime"
import { format } from "date-fns"

export function ShowtimeButton({ showtime }: { showtime: ShowTimeDto }) {
  const location = useLocation()
  const returnUrl = encodeURIComponent(location.pathname + location.search)
  const startTimeStr = format(new Date(showtime.startAt), "HH:mm")
  const endTimeStr = format(new Date(showtime.endAt), "HH:mm")
  const availability = showtime.availableTicketCount > 10 ? "Còn vé" : showtime.availableTicketCount > 0 ? "Sắp hết vé" : "Hết vé"
  
  return (
    <Link
      to={`/showtimes/${showtime.id}/seats?returnUrl=${returnUrl}`}
      className={`group flex min-w-[145px] flex-col gap-1 rounded-xl border border-outline-variant/20 bg-surface-container-high p-3.5 transition-all duration-300 hover:border-primary/50 hover:bg-surface-container-highest hover:shadow-lg hover:scale-[1.02] ${showtime.availableTicketCount === 0 ? "pointer-events-none opacity-50 grayscale" : "active:scale-[0.98]"}`}
    >
      {/* dòng đầu: thời gian bắt đầu - thời gian kết thúc */}
      <div className="flex items-center justify-between">
        <span className="font-headline text-base font-black tracking-tight text-white group-hover:text-primary transition-colors">
          {startTimeStr} - {endTimeStr}
        </span>
        <span className="material-symbols-outlined text-[16px] text-on-surface-variant transition-transform group-hover:translate-x-0.5 opacity-60">
          chevron_right
        </span>
      </div>

      {/* dòng 2: phòng chiếu | format */}
      <div className="flex items-center gap-1.5 text-xs font-semibold text-on-surface-variant/90">
        <span className="uppercase font-black text-amber-500">{showtime.screenCode}</span>
        <span className="opacity-40">|</span>
        <span className="text-[10px] font-black uppercase tracking-widest text-secondary">
          {showtime.format || "2D"}
        </span>
      </div>

      {/* dòng 3: tình trạng vé */}
      <div className={`mt-0.5 text-[10px] font-black uppercase tracking-wider ${showtime.availableTicketCount > 10 ? "text-primary" : showtime.availableTicketCount > 0 ? "text-secondary" : "text-slate-500"}`}>
        {availability}
      </div>
    </Link>
  )
}
