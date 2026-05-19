import * as Dialog from "@radix-ui/react-dialog"
import { ShowtimeButton } from "../components/ShowtimeButton"
import { useEffect, useState, useMemo } from "react"
import { Link } from "react-router-dom"
import { useToast } from "../contexts/ToastContext"
import { getShowTimes } from "../apis/showtimeApi"
import { getMovies } from "../apis/movieApi"
import { getCinemas } from "../apis/cinemaApi"
import type { ShowTimeDto } from "../types/ShowTime"
import type { MovieDto } from "../types/Movie"
import type { CinemaDto } from "../types/Cinema"
import { format, addDays, startOfDay } from "date-fns"
import { vi } from "date-fns/locale"

type GroupMode = "cinema" | "movie"

type MovieGroup = {
  id: string
  title: string
  genre: string
  duration: number
  poster: string
  showtimes: ShowTimeDto[]
}

type CinemaGroup = {
  id: string
  name: string
  address: string
  movies: MovieGroup[]
}

type MovieGroupWithCinemas = {
  id: string
  title: string
  genre: string
  duration: number
  poster: string
  cinemas: {
    id: string
    name: string
    address: string
    showtimes: ShowTimeDto[]
  }[]
}

import { useLoading } from "../contexts/LoadingContext"

function Showtimes() {
  const { error } = useToast()
  const [groupMode, setGroupMode] = useState<GroupMode>("cinema")
  const [isFilterOpen, setIsFilterOpen] = useState(false)
  const [timeRange, setTimeRange] = useState("all")
  const [selectedCinemaIds, setSelectedCinemaIds] = useState<string[]>([])
  const [selectedMovieIds, setSelectedMovieIds] = useState<string[]>([])
  const { showLoading, hideLoading } = useLoading()
  
  const [showtimes, setShowtimes] = useState<ShowTimeDto[]>([])
  const [movies, setMovies] = useState<MovieDto[]>([])
  const [cinemas, setCinemas] = useState<CinemaDto[]>([])
  
  const [selectedDate, setSelectedDate] = useState<Date>(startOfDay(new Date()))

  // Generate 7 days from today
  const dates = useMemo(() => {
    return Array.from({ length: 7 }).map((_, i) => {
      const d = addDays(startOfDay(new Date()), i)
      return {
        date: d,
        dayLabel: i === 0 ? "Hôm nay" : format(d, "eeee", { locale: vi }),
        dateLabel: format(d, "dd"),
        monthLabel: format(d, "MMM", { locale: vi }),
      }
    })
  }, [])

  useEffect(() => {
    const fetchData = async () => {
      showLoading("Đang tải lịch chiếu...")
      try {
        const [showtimesData, moviesData, cinemasData] = await Promise.all([
          getShowTimes({ date: format(selectedDate, "yyyy-MM-dd"), status: "Upcoming" }),
          getMovies(),
          getCinemas(),
          new Promise((resolve) => setTimeout(resolve, 500)), // Guarantee minimum 0.5s loading time for smooth UX
        ])
        setShowtimes(showtimesData)
        setMovies(moviesData)
        setCinemas(cinemasData)
      } catch (e) {
        console.error(e)
        error("Không thể tải dữ liệu lịch chiếu. Vui lòng thử lại sau.")
      } finally {
        hideLoading()
      }
    }

    fetchData()
  }, [selectedDate, error, showLoading, hideLoading])

  const toggleSelection = (value: string, current: string[], setter: (value: string[]) => void) => {
    if (current.includes(value)) {
      setter(current.filter((x) => x !== value))
    } else {
      setter([...current, value])
    }
  }

  const filteredShowtimes = useMemo(() => {
    return showtimes.filter((st) => {
      // Filter by Cinema
      if (selectedCinemaIds.length > 0 && !selectedCinemaIds.includes(st.cinemaId)) return false
      
      // Filter by Movie
      if (selectedMovieIds.length > 0 && !selectedMovieIds.includes(st.movieId)) return false
      
      // Filter by Time Range
      const hour = new Date(st.startAt).getHours()
      if (timeRange === "morning" && (hour < 8 || hour >= 12)) return false
      if (timeRange === "afternoon" && (hour < 12 || hour >= 18)) return false
      if (timeRange === "evening" && (hour < 18 || hour >= 24)) return false
      
      return true
    })
  }, [showtimes, selectedCinemaIds, selectedMovieIds, timeRange])

  const groupedByCinema = useMemo(() => {
    const groups: CinemaGroup[] = []
    
    filteredShowtimes.forEach((st) => {
      let cinemaGroup = groups.find((g) => g.id === st.cinemaId)
      if (!cinemaGroup) {
        cinemaGroup = {
          id: st.cinemaId,
          name: st.cinemaName,
          address: st.cinemaAddress,
          movies: [],
        }
        groups.push(cinemaGroup)
      }
      
      let movieGroup = cinemaGroup.movies.find((m) => m.id === st.movieId)
      if (!movieGroup) {
        movieGroup = {
          id: st.movieId,
          title: st.movieName,
          genre: st.movieGenre,
          duration: st.movieDuration,
          poster: st.movieThumbnailUrl,
          showtimes: [],
        }
        cinemaGroup.movies.push(movieGroup)
      }
      
      movieGroup.showtimes.push(st)
    })
    
    return groups
  }, [filteredShowtimes])

  const groupedByMovie = useMemo(() => {
    const groups: MovieGroupWithCinemas[] = []
    
    filteredShowtimes.forEach((st) => {
      let movieGroup = groups.find((g) => g.id === st.movieId)
      if (!movieGroup) {
        movieGroup = {
          id: st.movieId,
          title: st.movieName,
          genre: st.movieGenre,
          duration: st.movieDuration,
          poster: st.movieThumbnailUrl,
          cinemas: [],
        }
        groups.push(movieGroup)
      }
      
      let cinemaInMovie = movieGroup.cinemas.find((c) => c.id === st.cinemaId)
      if (!cinemaInMovie) {
        cinemaInMovie = {
          id: st.cinemaId,
          name: st.cinemaName,
          address: st.cinemaAddress,
          showtimes: [],
        }
        movieGroup.cinemas.push(cinemaInMovie)
      }
      
      cinemaInMovie.showtimes.push(st)
    })
    
    return groups
  }, [filteredShowtimes])

  return (
    <main className="min-h-screen bg-background pb-12 pt-24 md:pt-28">
      <div className="relative z-10 mx-auto w-full max-w-screen-2xl px-8">
        <section className="mb-12 flex flex-col gap-6 rounded-xl border border-outline-variant/20 bg-surface-variant/40 p-4 shadow-2xl backdrop-blur-xl md:flex-row md:items-center md:justify-between">
          <div className="flex-grow overflow-hidden">
            <h3 className="mb-2 text-sm text-on-surface-variant">Chọn ngày</h3>
            <div className="flex space-x-4 overflow-x-auto pb-2">
              {dates.map((item) => {
                const isActive = item.date.getTime() === selectedDate.getTime()
                return (
                  <button
                    key={item.date.toISOString()}
                    type="button"
                    onClick={() => setSelectedDate(item.date)}
                    className={
                      isActive
                        ? "flex h-[80px] min-w-[80px] flex-col items-center justify-center rounded-lg border border-secondary/50 bg-surface-container-highest text-secondary shadow-[0_0_8px_rgba(0,244,254,0.5)]"
                        : "flex h-[80px] min-w-[80px] flex-col items-center justify-center rounded-lg border border-outline-variant/30 bg-surface-container-low text-on-surface-variant transition-colors hover:bg-surface-container"
                    }
                  >
                    <span className="text-[10px] uppercase font-bold opacity-70">{item.dayLabel}</span>
                    <span className="font-headline text-2xl font-bold">{item.dateLabel}</span>
                    <span className="text-[10px] uppercase font-bold opacity-70">{item.monthLabel}</span>
                  </button>
                )
              })}
            </div>
          </div>

          <div className="flex min-w-[300px] flex-col gap-4">
            <div className="flex rounded-lg border border-outline-variant/30 bg-surface-container-low p-1">
              <button
                type="button"
                onClick={() => setGroupMode("cinema")}
                className={`flex-1 rounded px-4 py-2 text-sm font-semibold transition-colors ${groupMode === "cinema" ? "bg-surface-container-highest text-secondary" : "text-on-surface-variant"}`}
              >
                Group theo rạp
              </button>
              <button
                type="button"
                onClick={() => setGroupMode("movie")}
                className={`flex-1 rounded px-4 py-2 text-sm font-semibold transition-colors ${groupMode === "movie" ? "bg-surface-container-highest text-secondary" : "text-on-surface-variant"}`}
              >
                Group theo phim
              </button>
            </div>

            <button
              type="button"
              onClick={() => setIsFilterOpen(true)}
              className="flex items-center justify-center gap-2 rounded-lg border border-outline-variant/40 bg-surface-container-low py-3 font-semibold text-primary transition-colors hover:bg-primary/10"
            >
              <span className="material-symbols-outlined text-[18px]">filter_list</span>
              Bộ lọc { (selectedCinemaIds.length > 0 || selectedMovieIds.length > 0 || timeRange !== 'all') && <span className="flex h-2 w-2 rounded-full bg-secondary"></span> }
            </button>
          </div>
        </section>

        {filteredShowtimes.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-xl border border-outline-variant/20 bg-surface-container-low p-20 text-center shadow-2xl">
            <span className="material-symbols-outlined text-6xl text-on-surface-variant/40 mb-4">calendar_today</span>
            <h2 className="text-2xl font-bold text-on-background">Không tìm thấy lịch chiếu</h2>
            <p className="mt-2 text-on-surface-variant">Vui lòng chọn ngày khác hoặc thay đổi bộ lọc.</p>
            <button 
              onClick={() => {
                setSelectedCinemaIds([]);
                setSelectedMovieIds([]);
                setTimeRange('all');
              }}
              className="mt-6 text-secondary font-bold hover:underline"
            >
              Xóa tất cả bộ lọc
            </button>
          </div>
        ) : (
          <section className="space-y-10">
            {groupMode === "cinema"
              ? groupedByCinema.map((cinema) => (
                  <div key={cinema.id} className="rounded-xl border border-outline-variant/20 bg-surface-container-low p-6 shadow-[0_10px_30px_rgba(0,0,0,0.25)]">
                    <div className="mb-6 flex flex-col gap-2 border-b border-outline-variant/20 pb-4 md:flex-row md:items-end md:justify-between">
                      <div>
                        <h2 className="font-headline text-2xl font-bold tracking-tight text-on-background">{cinema.name}</h2>
                        <p className="mt-1 flex items-center gap-2 text-sm text-on-surface-variant">
                          <span className="material-symbols-outlined text-[18px] opacity-80">map</span>
                          {cinema.address}
                        </p>
                      </div>
                      <span className="flex w-fit items-center gap-1 rounded bg-secondary/10 px-2 py-1 text-sm font-bold text-secondary">
                        <span className="material-symbols-outlined text-[16px]">theaters</span>
                        {cinema.movies.length} phim
                      </span>
                    </div>

                    <div className="space-y-8">
                      {cinema.movies.map((movie) => (
                        <div key={`${cinema.id}-${movie.id}`} className="rounded-lg border border-outline-variant/15 bg-surface-container/60 p-4 md:p-5">
                          <div className="flex flex-col gap-4 sm:flex-row sm:items-stretch">
                            <div className="h-40 w-full shrink-0 overflow-hidden rounded-lg bg-surface-container-highest sm:h-auto sm:w-28 md:w-32">
                              <img src={movie.poster} alt={movie.title} className="h-full w-full object-cover" />
                            </div>
                            <div className="min-w-0 flex-1">
                              <Link to={`/movies/${movie.id}/showtimes`} className="hover:text-primary transition-colors cursor-pointer">
                                <h3 className="font-headline text-xl font-bold text-on-background hover:text-primary transition-colors">{movie.title}</h3>
                              </Link>
                              <p className="mt-1 text-sm text-on-surface-variant">
                                {movie.genre} · {movie.duration} phút
                              </p>
                              <div className="mt-4 flex flex-wrap gap-3">
                                {movie.showtimes.map((st) => (
                                  <ShowtimeButton key={st.id} showtime={st} />
                                ))}
                              </div>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ))
              : groupedByMovie.map((movie) => (
                  <div key={movie.id} className="rounded-xl border border-outline-variant/20 bg-surface-container-low p-6 shadow-[0_10px_30px_rgba(0,0,0,0.25)]">
                    <div className="mb-6 flex flex-col gap-4 border-b border-outline-variant/20 pb-4 sm:flex-row sm:items-start">
                      <div className="h-44 w-full shrink-0 overflow-hidden rounded-lg bg-surface-container-highest sm:h-36 sm:w-28 md:w-32">
                        <img src={movie.poster} alt={movie.title} className="h-full w-full object-cover" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <Link to={`/movies/${movie.id}/showtimes`} className="hover:text-primary transition-colors cursor-pointer">
                          <h2 className="font-headline text-2xl font-bold tracking-tight text-on-background hover:text-primary transition-colors">{movie.title}</h2>
                        </Link>
                        <p className="mt-1 text-sm text-on-surface-variant">
                          {movie.genre} · {movie.duration} phút
                        </p>
                        <p className="mt-2 text-xs text-on-surface-variant">
                          Đang chiếu tại {movie.cinemas.length} rạp
                        </p>
                      </div>
                    </div>

                    <div className="space-y-8">
                      {movie.cinemas.map((c) => (
                        <div key={`${movie.id}-${c.id}`} className="rounded-lg border border-outline-variant/15 bg-surface-container/60 p-4 md:p-5">
                          <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                            <div>
                              <h4 className="flex items-center gap-2 font-headline text-lg font-semibold text-primary">
                                <span className="material-symbols-outlined text-[20px]">location_city</span>
                                {c.name}
                              </h4>
                              <p className="mt-1 flex items-start gap-2 text-sm text-on-surface-variant">
                                <span className="material-symbols-outlined mt-0.5 shrink-0 text-[18px] opacity-70">map</span>
                                {c.address}
                              </p>
                            </div>
                          </div>
                          <div className="flex flex-wrap gap-3">
                            {c.showtimes.map((st) => (
                              <ShowtimeButton key={st.id} showtime={st} />
                            ))}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
          </section>
        )}
      </div>

      <Dialog.Root open={isFilterOpen} onOpenChange={setIsFilterOpen}>
        <Dialog.Portal>
          <Dialog.Overlay className="auth-modal-overlay fixed inset-0 z-[90] bg-black/70 backdrop-blur-sm" />
          <Dialog.Content className="auth-modal-content fixed left-1/2 top-1/2 z-[100] w-[95vw] max-w-2xl -translate-x-1/2 -translate-y-1/2 border border-outline-variant/20 bg-surface-container-low p-6 text-on-background shadow-2xl focus:outline-none md:p-8 max-h-[85vh] overflow-y-auto">
            <div className="mb-6 flex items-start justify-between">
              <div>
                <Dialog.Title className="font-headline text-2xl font-bold">Bộ lọc lịch chiếu</Dialog.Title>
                <Dialog.Description className="mt-1 text-sm text-on-surface-variant">Lọc theo rạp, phim đang chiếu và khoảng thời gian.</Dialog.Description>
              </div>
              <Dialog.Close className="text-on-surface-variant hover:text-secondary">
                <span className="material-symbols-outlined">close</span>
              </Dialog.Close>
            </div>

            <div className="grid gap-6 md:grid-cols-2">
              <div>
                <h4 className="mb-3 font-headline text-lg font-bold">Danh sách rạp</h4>
                <div className="space-y-2 max-h-48 overflow-y-auto pr-2">
                  {cinemas.map((cinema) => (
                    <label key={cinema.id} className="flex cursor-pointer items-center gap-3 rounded border border-outline-variant/20 px-3 py-2 hover:bg-surface-variant/40">
                      <input
                        type="checkbox"
                        checked={selectedCinemaIds.includes(cinema.id)}
                        onChange={() => toggleSelection(cinema.id, selectedCinemaIds, setSelectedCinemaIds)}
                        className="h-4 w-4 border-outline-variant bg-surface-container-low text-secondary focus:ring-secondary"
                      />
                      <span className="text-sm">{cinema.name}</span>
                    </label>
                  ))}
                </div>
              </div>

              <div>
                <h4 className="mb-3 font-headline text-lg font-bold">Phim đang chiếu</h4>
                <div className="space-y-2 max-h-48 overflow-y-auto pr-2">
                  {movies.map((m) => (
                    <label key={m.id} className="flex cursor-pointer items-center gap-3 rounded border border-outline-variant/20 px-3 py-2 hover:bg-surface-variant/40">
                      <input
                        type="checkbox"
                        checked={selectedMovieIds.includes(m.id)}
                        onChange={() => toggleSelection(m.id, selectedMovieIds, setSelectedMovieIds)}
                        className="h-4 w-4 border-outline-variant bg-surface-container-low text-secondary focus:ring-secondary"
                      />
                      <span className="text-sm">{m.name}</span>
                    </label>
                  ))}
                </div>
              </div>
            </div>

            <div className="mt-6">
              <h4 className="mb-3 font-headline text-lg font-bold">Khoảng thời gian</h4>
              <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
                {[
                  { id: "all", label: "Tất cả" },
                  { id: "morning", label: "Sáng (08-12)" },
                  { id: "afternoon", label: "Chiều (12-18)" },
                  { id: "evening", label: "Tối (18-24)" },
                ].map((range) => (
                  <button
                    key={range.id}
                    type="button"
                    onClick={() => setTimeRange(range.id)}
                    className={`rounded border px-3 py-2 text-xs font-semibold transition-colors ${timeRange === range.id ? "border-secondary bg-secondary/10 text-secondary" : "border-outline-variant/30 text-on-surface-variant hover:bg-surface-variant/40"}`}
                  >
                    {range.label}
                  </button>
                ))}
              </div>
            </div>

            <div className="mt-8 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => {
                  setSelectedCinemaIds([])
                  setSelectedMovieIds([])
                  setTimeRange("all")
                }}
                className="rounded border border-outline-variant/30 px-4 py-2 text-sm text-on-surface-variant transition-colors hover:bg-surface-variant/40"
              >
                Xóa lọc
              </button>
              <Dialog.Close asChild>
                <button
                  type="button"
                  className="rounded bg-gradient-to-r from-primary to-primary-container px-4 py-2 text-sm font-bold text-on-primary"
                >
                  Áp dụng
                </button>
              </Dialog.Close>
            </div>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </main>
  )
}

export default Showtimes
