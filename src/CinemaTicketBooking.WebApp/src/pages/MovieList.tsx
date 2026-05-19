import { useEffect, useMemo, useState } from "react"
import { Link, useLocation } from "react-router-dom"
import { getUpcomingAndNowShowingMovies } from "../apis/movieApi"
import { getShowTimes } from "../apis/showtimeApi"
import type { MovieDto } from "../types/Movie"
import type { ShowTimeDto } from "../types/ShowTime"
import { MovieTrailerModal } from "../components/MovieTrailerModal"
import { useLoading } from "../contexts/LoadingContext"

type MovieWithShowTimes = {
  movie: MovieDto
  showtimes: ShowTimeDto[]
}

function formatDuration(duration: number) {
  const hours = Math.floor(duration / 60)
  const mins = duration % 60
  return `${hours}h ${mins.toString().padStart(2, "0")}m`
}

function formatDateLabel(dateInput: string) {
  const date = new Date(dateInput)
  if (Number.isNaN(date.getTime())) {
    return dateInput
  }

  return new Intl.DateTimeFormat("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" }).format(date)
}

function genreLabel(genre: MovieDto["genre"]) {
  if (genre === "SciFi") {
    return "Sci-Fi"
  }
  return genre
}

function formatLabel(format: string | number) {
  const f = (format || "0").toString()
  if (f === "0" || f === "TwoD") return "2D"
  if (f === "1" || f === "ThreeD") return "3D"
  if (f === "2" || f === "IMAX") return "IMAX"
  return f
}

function MovieList() {
  const location = useLocation()
  const returnUrl = encodeURIComponent(location.pathname + location.search)
  const [movies, setMovies] = useState<MovieDto[]>([])
  const [showtimes, setShowtimes] = useState<ShowTimeDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const { showLoading, hideLoading } = useLoading()

  const [trailerModal, setTrailerModal] = useState<{ isOpen: boolean; movieName: string; trailerUrl: string | null }>({
    isOpen: false,
    movieName: "",
    trailerUrl: null,
  })

  const openTrailer = (movieName: string, trailerUrl: string | null) => {
    setTrailerModal({ isOpen: true, movieName, trailerUrl })
  }

  useEffect(() => {
    async function loadData() {
      try {
        showLoading("Đang tải danh sách phim...")
        const [movieData, showtimeData] = await Promise.all([getUpcomingAndNowShowingMovies(), getShowTimes()])
        const activeShowtimes = showtimeData
          .filter((item) => item.status === "Upcoming" || item.status === "Showing")
          .sort((a, b) => new Date(a.startAt).getTime() - new Date(b.startAt).getTime())
        setMovies(movieData)
        setShowtimes(activeShowtimes)
        setError(null)
      } catch {
        setError("Không tải được dữ liệu phim/lịch chiếu từ backend.")
      } finally {
        hideLoading()
      }
    }

    void loadData()
  }, [showLoading, hideLoading])

  const showtimesByMovieId = useMemo(() => {
    const today = new Date().toISOString().split("T")[0]
    return showtimes.reduce<Record<string, ShowTimeDto[]>>((acc, item) => {
      // Filter for today's showtimes for the "Quick Pick" section
      const itemDate = new Date(item.startAt).toISOString().split("T")[0]
      if (itemDate !== today) return acc

      if (!acc[item.movieId]) {
        acc[item.movieId] = []
      }
      acc[item.movieId].push(item)
      return acc
    }, {})
  }, [showtimes])

  const moviesWithShowtimes = useMemo<MovieWithShowTimes[]>(() => {
    return movies.map((movie) => ({
      movie,
      showtimes: showtimesByMovieId[movie.id] ?? [],
    }))
  }, [movies, showtimesByMovieId])

  const hotMovies = useMemo(() => {
    return [...moviesWithShowtimes]
      .sort((a, b) => {
        if (b.showtimes.length !== a.showtimes.length) {
          return b.showtimes.length - a.showtimes.length
        }
        const aAvailable = a.showtimes.reduce((sum, item) => sum + item.availableTicketCount, 0)
        const bAvailable = b.showtimes.reduce((sum, item) => sum + item.availableTicketCount, 0)
        return bAvailable - aAvailable
      })
      .slice(0, 6)
  }, [moviesWithShowtimes])

  const nowShowingMovies = useMemo(() => {
    return moviesWithShowtimes.filter((item) => item.movie.status === "NowShowing")
  }, [moviesWithShowtimes])

  const upcomingMovies = useMemo(() => {
    return moviesWithShowtimes.filter((item) => item.movie.status === "Upcoming")
  }, [moviesWithShowtimes])

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
            alt="cinema background"
            className="h-full w-full object-cover opacity-20"
          />
          <div className="absolute inset-0 bg-gradient-to-r from-background via-background/85 to-background/40" />
        </div>
        <div className="relative z-10 mx-auto w-full max-w-screen-2xl px-8">
          <span className="mb-4 inline-flex items-center gap-2 border border-secondary/20 bg-secondary/10 px-3 py-1 text-sm font-semibold tracking-[0.14em] text-secondary">
            <span className="material-symbols-outlined text-base">movie</span>
            MOVIE LIBRARY
          </span>
          <h1 className="font-headline text-5xl font-black tracking-tight text-on-background md:text-7xl">
            PHIM HOT, ĐANG CHIẾU
            <br />
            <span className="text-primary drop-shadow-[0_0_15px_rgba(97,180,254,0.35)]">VÀ SẮP KHỞI CHIẾU</span>
          </h1>
          <p className="mt-4 max-w-3xl text-lg text-on-surface-variant">
            Dữ liệu phim và suất chiếu đang được lấy trực tiếp từ backend để hiển thị trạng thái phát hành theo thời gian thực.
          </p>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto w-full max-w-screen-2xl px-8">
          <div className="mb-6 border-l-4 border-secondary pl-5">
            <h2 className="font-headline text-4xl font-black tracking-tight text-on-background">PHIM HOT</h2>
            <p className="text-on-surface-variant">Những siêu phẩm được quan tâm nhiều nhất</p>
          </div>

          <div className="flex gap-6 overflow-x-auto pb-4 custom-scrollbar">
            {hotMovies.map(({ movie }) => {
              return (
                <article
                  key={movie.id}
                  className="group relative h-[460px] min-w-[280px] overflow-hidden rounded-xl border border-outline-variant/20 bg-surface-container-low md:min-w-[320px]"
                >
                  <img src={movie.thumbnailUrl} alt={movie.name} className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-105" />
                  <div className="absolute inset-0 bg-gradient-to-t from-background via-background/35 to-transparent" />
                  <div className="absolute bottom-0 w-full p-6">
                    <p className="text-xs font-semibold tracking-[0.16em] text-secondary">{genreLabel(movie.genre)}</p>
                    <Link to={`/movies/${movie.id}/showtimes`} className="block hover:text-primary transition-colors cursor-pointer">
                      <h3 className="mt-2 font-headline text-3xl font-black leading-tight text-on-background group-hover:text-primary transition-colors">{movie.name}</h3>
                    </Link>
                    <div className="mt-3 flex items-center gap-3 text-sm text-on-surface-variant">
                      <span>{formatDuration(movie.duration)}</span>
                      <span>•</span>
                      <span>{movie.studio}</span>
                    </div>
                    <div className="mt-5 flex gap-2">
                      <Link
                        to={`/movies/${movie.id}/showtimes`}
                        className="inline-flex items-center gap-2 rounded bg-gradient-to-r from-primary to-primary-container px-5 py-2.5 text-sm font-bold text-on-primary transition-all hover:shadow-[0_0_14px_rgba(0,244,254,0.45)]"
                      >
                        Lịch chiếu
                        <span className="material-symbols-outlined text-base">calendar_today</span>
                      </Link>
                      {movie.officialTrailerUrl && (
                        <button
                          type="button"
                          onClick={() => openTrailer(movie.name, movie.officialTrailerUrl)}
                          className="inline-flex items-center gap-2 rounded bg-surface-variant/40 border border-outline-variant/30 px-5 py-2.5 text-sm font-bold text-on-surface-variant transition-all hover:bg-surface-variant/60"
                        >
                          <span className="material-symbols-outlined text-base">play_circle</span>
                          Trailer
                        </button>
                      )}
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        </div>
      </section>

      <section className="bg-surface py-16">
        <div className="mx-auto w-full max-w-screen-2xl px-8">
          <div className="mb-8 border-l-4 border-primary pl-5">
            <h2 className="font-headline text-4xl font-black tracking-tight text-on-background">ĐANG CHIẾU</h2>
            <p className="text-on-surface-variant">Các phim đang có suất chiếu tại rạp</p>
          </div>

          <div className="space-y-6">
            {nowShowingMovies.map(({ movie, showtimes: movieShowtimes }) => (
              <article
                key={movie.id}
                className="group relative rounded-xl border border-outline-variant/20 bg-surface-container-low p-4 shadow-[0_10px_28px_rgba(0,0,0,0.25)] md:p-6"
              >
                {/* Actions at top right */}
                <div className="absolute right-4 top-4 z-10 flex gap-2 md:right-6 md:top-6">
                  {movie.officialTrailerUrl && (
                    <button
                      type="button"
                      onClick={() => openTrailer(movie.name, movie.officialTrailerUrl)}
                      className="flex h-10 items-center gap-2 rounded-md border border-outline-variant/30 bg-surface-variant/40 px-4 text-xs font-bold text-on-surface-variant transition-all hover:bg-surface-variant/60"
                    >
                      <span className="material-symbols-outlined text-lg">play_circle</span>
                      TRAILER
                    </button>
                  )}
                  <Link
                    to={`/movies/${movie.id}/showtimes`}
                    className="flex h-10 items-center gap-2 rounded-md border border-primary/25 bg-primary/10 px-4 text-xs font-bold text-primary transition-all hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-lg">calendar_today</span>
                    LỊCH CHIẾU
                  </Link>
                </div>

                <div className="flex flex-col gap-6 md:flex-row">
                  <div className="h-64 w-full shrink-0 overflow-hidden rounded-lg bg-surface-container-highest md:h-72 md:w-52">
                    <img src={movie.thumbnailUrl} alt={movie.name} className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105" />
                  </div>

                  <div className="flex min-w-0 flex-1 flex-col pt-8 md:pt-0">
                    <div className="flex flex-wrap items-center gap-3 pr-40"> {/* pr-40 to avoid overlap with buttons */}
                      <Link to={`/movies/${movie.id}/showtimes`} className="hover:text-primary transition-colors cursor-pointer">
                        <h3 className="font-headline text-3xl font-black tracking-tight text-on-background group-hover:text-primary transition-colors">{movie.name}</h3>
                      </Link>
                    </div>

                    <p className="mt-2 text-sm text-on-surface-variant">
                      {genreLabel(movie.genre)} • {formatDuration(movie.duration)} • Đạo diễn: {movie.director}
                    </p>
                    <p className="mt-4 max-w-3xl line-clamp-3 leading-relaxed text-on-surface-variant">{movie.description}</p>

                    <div className="mt-6 grid grid-cols-1 gap-4 text-sm text-on-surface-variant sm:grid-cols-3">
                      <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-4 py-3">
                        <span className="text-[10px] uppercase font-bold tracking-widest text-secondary opacity-80">Studio</span>
                        <p className="mt-1 font-medium text-on-background">{movie.studio}</p>
                      </div>
                      <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-4 py-3">
                        <span className="text-[10px] uppercase font-bold tracking-widest text-secondary opacity-80">Khởi chiếu</span>
                        <p className="mt-1 font-medium text-on-background">{formatDateLabel(movie.createdAt)}</p>
                      </div>
                      <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-4 py-3">
                        <span className="text-[10px] uppercase font-bold tracking-widest text-secondary opacity-80">Suất đang mở</span>
                        <p className="mt-1 font-medium text-on-background">{movieShowtimes.length}</p>
                      </div>
                    </div>

                    <div className="mt-6">
                      <p className="mb-3 text-[10px] font-bold uppercase tracking-widest text-on-surface-variant opacity-70">Suất chiếu hôm nay</p>
                      <div className="flex flex-wrap gap-3">
                        {movieShowtimes.slice(0, 8).map((showtime) => {
                          const startTime = new Date(showtime.startAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit", hour12: false })
                          const endTime = new Date(showtime.endAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit", hour12: false })
                          return (
                            <Link
                              key={showtime.id}
                              to={`/showtimes/${showtime.id}/seats?returnUrl=${returnUrl}`}
                              className="group flex flex-col items-center justify-center min-w-[180px] rounded-lg border border-outline-variant/30 bg-surface-container/80 py-2.5 px-4 transition-all duration-300 hover:border-primary/50 hover:bg-surface-container-highest shadow-[0_2px_8px_rgba(0,0,0,0.15)] hover:shadow-[0_4px_16px_rgba(0,244,254,0.15)] hover:scale-[1.02]"
                            >
                              <div className="flex items-center gap-1.5 text-xs font-bold text-on-background">
                                <span className="font-headline text-sm font-black text-secondary group-hover:text-secondary transition-colors">
                                  {startTime} - {endTime}
                                </span>
                                <span className="text-[10px] text-on-surface-variant opacity-60">|</span>
                                <span className="text-[10px] font-black tracking-tight text-secondary uppercase group-hover:text-primary transition-colors">
                                  {formatLabel(showtime.format)}
                                </span>
                              </div>
                              <div className="mt-1 flex items-center gap-1.5 text-[10px] font-semibold text-on-surface-variant/80">
                                <span className="uppercase font-black text-amber-500">{showtime.screenCode}</span>
                                <span className="opacity-50">|</span>
                                <span className="truncate max-w-[90px]" title={showtime.cinemaName}>{showtime.cinemaName}</span>
                              </div>
                            </Link>
                          )
                        })}
                        {movieShowtimes.length === 0 && (
                          <span className="rounded-lg border border-outline-variant/30 bg-surface-container/50 px-4 py-2 text-sm text-on-surface-variant italic">
                            Hôm nay đã hết suất chiếu
                          </span>
                        )}
                        {movieShowtimes.length > 8 && (
                          <Link
                            to={`/movies/${movie.id}/showtimes`}
                            className="flex h-[56px] items-center justify-center rounded-lg border border-outline-variant/20 bg-surface-variant/20 px-4 text-xs font-bold text-on-surface-variant hover:bg-surface-variant/40 transition-all hover:scale-[1.02]"
                          >
                            +{movieShowtimes.length - 8} suất
                          </Link>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto w-full max-w-screen-2xl px-8">
          <div className="mb-8 border-l-4 border-secondary pl-5">
            <h2 className="font-headline text-4xl font-black tracking-tight text-on-background">SẮP KHỞI CHIẾU</h2>
            <p className="text-on-surface-variant">Đón chờ những siêu phẩm sắp ra mắt</p>
          </div>

          <div className="space-y-6">
            {upcomingMovies.map(({ movie, showtimes: movieShowtimes }) => (
              <article
                key={movie.id}
                className="group flex flex-col gap-6 overflow-hidden rounded-xl border border-outline-variant/20 bg-surface-container-low p-4 transition-all duration-300 hover:border-secondary/40 md:flex-row md:items-stretch md:p-6"
              >
                <div className="h-56 w-full shrink-0 overflow-hidden rounded-lg bg-surface-container-highest md:h-auto md:w-64">
                  <img src={movie.thumbnailUrl} alt={movie.name} className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="rounded border border-secondary/20 bg-secondary/10 px-2 py-1 text-xs font-bold tracking-widest text-secondary">
                      COMING SOON
                    </span>
                    <span className="rounded border border-outline-variant/30 bg-surface-container/70 px-2 py-1 text-xs font-semibold text-on-surface-variant">
                      {genreLabel(movie.genre)}
                    </span>
                  </div>
                  <Link to={`/movies/${movie.id}/showtimes`} className="hover:text-secondary transition-colors cursor-pointer">
                    <h3 className="mt-4 font-headline text-3xl font-black tracking-tight text-on-background group-hover:text-secondary transition-colors">{movie.name}</h3>
                  </Link>
                  <p className="mt-2 text-sm text-on-surface-variant">
                    {formatDuration(movie.duration)} • {movie.studio} • Đạo diễn: {movie.director}
                  </p>
                  <p className="mt-4 max-w-4xl line-clamp-2 leading-relaxed text-on-surface-variant">{movie.description}</p>

                  <div className="mt-6 flex flex-wrap items-center gap-4">
                    <div className="rounded-lg border border-outline-variant/20 bg-surface-container/70 px-4 py-3 text-sm">
                      <p className="text-[10px] uppercase font-bold tracking-widest text-secondary opacity-80">Dự kiến khởi chiếu</p>
                      <p className="mt-1 font-semibold text-on-background">
                        {movieShowtimes[0] ? formatDateLabel(movieShowtimes[0].date) : formatDateLabel(movie.createdAt)}
                      </p>
                    </div>
                    {movie.officialTrailerUrl && (
                      <button
                        type="button"
                        onClick={() => openTrailer(movie.name, movie.officialTrailerUrl)}
                        className="inline-flex items-center gap-2 rounded-lg border border-outline-variant/30 bg-surface-variant/40 px-6 py-3 text-sm font-bold text-on-surface-variant transition-all hover:bg-surface-variant/60"
                      >
                        <span className="material-symbols-outlined text-lg">play_circle</span>
                        XEM TRAILER
                      </button>
                    )}
                    {movieShowtimes[0] ? (
                      <Link
                        to={`/movies/${movie.id}/showtimes`}
                        className="inline-flex items-center gap-2 rounded-lg bg-secondary px-6 py-3 text-sm font-bold text-on-secondary transition-all hover:shadow-[0_0_15px_rgba(0,244,254,0.4)]"
                      >
                        <span className="material-symbols-outlined text-lg">event_available</span>
                        XEM LỊCH SỚM
                      </Link>
                    ) : (
                      <button
                        type="button"
                        className="inline-flex cursor-not-allowed items-center gap-2 rounded-lg border border-outline-variant/30 bg-surface-variant/30 px-6 py-3 text-sm font-bold text-on-surface-variant opacity-50"
                      >
                        <span className="material-symbols-outlined text-lg">notifications</span>
                        NHẮC TÔI
                      </button>
                    )}
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <MovieTrailerModal
        isOpen={trailerModal.isOpen}
        onClose={() => setTrailerModal({ ...trailerModal, isOpen: false })}
        movieName={trailerModal.movieName}
        trailerUrl={trailerModal.trailerUrl}
      />
    </main>
  )
}

export default MovieList
