import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { getActiveSlides } from "../apis/slideApi"
import { type Slide } from "../types/Slide"
import { MovieTrailerModal } from "../components/MovieTrailerModal"
import { getUpcomingAndNowShowingMovies } from "../apis/movieApi"
import { getCinemas } from "../apis/cinemaApi"
import type { MovieDto } from "../types/Movie"
import type { CinemaDto } from "../types/Cinema"

function formatDuration(duration: number) {
  const hours = Math.floor(duration / 60)
  const mins = duration % 60
  return `${hours}h ${mins.toString().padStart(2, "0")}m`
}

function genreLabel(genre: MovieDto["genre"]) {
  if (genre === "SciFi") {
    return "Sci-Fi"
  }
  return genre
}

function formatDateLabel(dateInput: string) {
  const date = new Date(dateInput)
  if (Number.isNaN(date.getTime())) {
    return dateInput
  }
  return new Intl.DateTimeFormat("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" }).format(date)
}

function getMockDistance(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) {
    hash = id.charCodeAt(i) + ((hash << 5) - hash)
  }
  const distance = 1.5 + (Math.abs(hash) % 80) / 10
  return `${distance.toFixed(1)} km`
}

const MOCK_CINEMA_DETAILS: Record<number, { tags: string[]; badges: string[] }> = {
  0: { tags: ["Dolby Atmos", "Ghế Recliner", "Dine-in"], badges: ["IMAX", "4DX"] },
  1: { tags: ["Laser Projection", "Bar", "Recliners"], badges: ["IMAX"] },
  2: { tags: ["Phim Indie", "Cafe", "Recliners"], badges: ["VIP"] },
  3: { tags: ["Dolby Atmos", "Recliners"], badges: ["4DX"] },
}

function getCinemaDetails(index: number) {
  return MOCK_CINEMA_DETAILS[index % 4]
}

function Home() {
  const [slides, setSlides] = useState<Slide[]>([])
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0)
  const [isLoadingSlides, setIsLoadingSlides] = useState(true)
  const [trailerData, setTrailerData] = useState<{ isOpen: boolean; url: string; name: string }>({
    isOpen: false,
    url: "",
    name: "",
  })

  const [movies, setMovies] = useState<MovieDto[]>([])
  const [cinemas, setCinemas] = useState<CinemaDto[]>([])
  const [isLoadingData, setIsLoadingData] = useState(true)
  const [cinemaSearchQuery, setCinemaSearchQuery] = useState("")

  useEffect(() => {
    const fetchSlides = async () => {
      try {
        const data = await getActiveSlides()
        setSlides(Array.isArray(data) ? data : [])
      } catch (error) {
        console.error("Failed to fetch slides:", error)
      } finally {
        setIsLoadingSlides(false)
      }
    }
    fetchSlides()
  }, [])

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [moviesData, cinemasData] = await Promise.all([
          getUpcomingAndNowShowingMovies(),
          getCinemas(),
        ])
        setMovies(moviesData)
        setCinemas(cinemasData)
      } catch (error) {
        console.error("Failed to fetch movies or cinemas:", error)
      } finally {
        setIsLoadingData(false)
      }
    }
    fetchData()
  }, [])

  useEffect(() => {
    if (slides.length <= 1) return

    const interval = setInterval(() => {
      setCurrentSlideIndex((prev) => (prev + 1) % slides.length)
    }, 5000)

    return () => clearInterval(interval)
  }, [slides.length])

  const currentSlide = slides[currentSlideIndex]

  const nowShowingMovies = movies.filter((m) => m.status === "NowShowing").slice(0, 4)
  const upcomingMovies = movies.filter((m) => m.status === "Upcoming").slice(0, 3)

  const filteredCinemas = cinemas
    .filter((c) =>
      c.name.toLowerCase().includes(cinemaSearchQuery.toLowerCase()) ||
      c.address.toLowerCase().includes(cinemaSearchQuery.toLowerCase())
    )
    .slice(0, 3)

  return (
    <main>
      <section className="relative flex min-h-[600px] h-[85vh] w-full items-center overflow-hidden bg-background md:h-screen">
        {isLoadingSlides ? (
          <div className="absolute inset-0 flex items-center justify-center bg-background/50 backdrop-blur-md">
            <div className="h-12 w-12 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          </div>
        ) : (
          (slides || []).map((slide, index) => (
            <div
              key={slide.id}
              className={`absolute inset-0 z-0 transition-all duration-1000 cubic-bezier(0.4, 0, 0.2, 1) ${index === currentSlideIndex
                ? "translate-y-0 opacity-100 z-10"
                : "translate-y-full opacity-0 z-0"
                }`}
            >
              <img
                className="h-full w-full object-cover brightness-[0.35] grayscale-[5%]"
                alt={slide.title}
                src={slide.imageUrl}
              />
              <div className="absolute inset-0 bg-gradient-to-r from-background via-background/60 to-transparent" />
              <div className="absolute bottom-0 inset-x-0 h-1/2 bg-gradient-to-t from-background to-transparent" />
            </div>
          ))
        )}

        <div className="relative z-10 mx-auto w-full max-w-screen-2xl px-6 md:px-8">
          <div className="max-w-3xl">
            {currentSlide && (
              <div key={currentSlide.id} className={`transition-all duration-700 ${isLoadingSlides ? "opacity-0" : "opacity-100 translate-y-0"}`}>
                <div className="flex items-center gap-3 mb-6 animate-slide-left">
                  <span className={`px-3 py-1 font-headline text-xs font-bold tracking-widest uppercase backdrop-blur-sm border ${(currentSlide.type === 'ShowingMovie' || currentSlide.type as unknown as number === 0) ? 'bg-primary/20 border-primary/40 text-primary' :
                    (currentSlide.type === 'UpcomingMovie' || currentSlide.type as unknown as number === 1) ? 'bg-secondary/20 border-secondary/40 text-secondary' :
                      'bg-amber-500/20 border-amber-500/40 text-amber-500'
                    }`}>
                    {(currentSlide.type === 'ShowingMovie' || currentSlide.type as unknown as number === 0) ? 'Đang chiếu' :
                      (currentSlide.type === 'UpcomingMovie' || currentSlide.type as unknown as number === 1) ? 'Sắp khởi chiếu' : 'Sự kiện khuyến mãi'}
                  </span>
                  {(currentSlide.type === 'ShowingMovie' || currentSlide.type as unknown as number === 0) && (
                    <span className="flex items-center gap-1 text-[10px] font-bold text-white/60 uppercase tracking-tighter">
                      <span className="material-symbols-outlined text-xs">local_fire_department</span>
                      Hot nhất tuần
                    </span>
                  )}
                </div>
                <h1 className="mb-6 font-headline text-5xl font-black leading-[1.1] tracking-tighter text-white sm:text-6xl md:text-8xl animate-slide-left anim-delay-150">
                  {(currentSlide.title || "").split(":")[0]} <br />
                  <span className="text-primary drop-shadow-[0_0_15px_rgba(97,180,254,0.4)]">
                    {(currentSlide.title || "").split(":")[1] || ""}
                  </span>
                </h1>
                <p className="mb-10 max-w-2xl text-base font-light leading-relaxed text-slate-400 sm:text-lg md:text-xl animate-slide-left anim-delay-300">
                  {currentSlide.description || "Trải nghiệm đỉnh cao của điện ảnh với hệ thống âm thanh vòm thế hệ mới và hình ảnh sắc nét đến từng chi tiết tại hệ thống rạp Absolute Cinema."}
                </p>
                <div className="flex flex-wrap gap-4 animate-slide-left anim-delay-450">
                  {(currentSlide.type === 'UpcomingMovie' || currentSlide.type as unknown as number === 1) ? (
                    <button
                      type="button"
                      className="flex items-center gap-2 bg-white/10 border border-white/20 px-6 py-3 font-headline font-bold tracking-wide text-white transition-all hover:bg-white/20 active:scale-95 sm:px-8 sm:py-4"
                    >
                      <span className="material-symbols-outlined">notifications</span>
                      <span>NHẮC TÔI KHI CÓ VÉ</span>
                    </button>
                  ) : (
                    <Link
                      to={currentSlide.targetUrl || "/movies"}
                      className="flex items-center gap-2 bg-gradient-to-br from-primary to-primary-container px-6 py-3 font-headline font-bold tracking-wide text-on-primary transition-all hover:shadow-[0_0_25px_rgba(97,180,254,0.6)] active:scale-95 sm:px-8 sm:py-4"
                    >
                      <span>{(currentSlide.type === 'Event' || currentSlide.type as unknown as number === 2) ? 'XEM CHI TIẾT' : 'ĐẶT VÉ NGAY'}</span>
                      <span className="material-symbols-outlined">{(currentSlide.type === 'Event' || currentSlide.type as unknown as number === 2) ? 'info' : 'confirmation_number'}</span>
                    </Link>
                  )}

                  {(currentSlide.type !== 'Event' && currentSlide.type as unknown as number !== 2) && currentSlide.videoUrl && (
                    <button
                      type="button"
                      onClick={() => setTrailerData({ isOpen: true, url: currentSlide.videoUrl!, name: currentSlide.title })}
                      className="flex items-center gap-2 border border-outline-variant/30 bg-surface-variant/40 px-6 py-3 font-headline font-bold tracking-wide text-white backdrop-blur-md transition-all hover:bg-surface-variant/60 active:scale-95 sm:px-8 sm:py-4"
                    >
                      <span className="material-symbols-outlined text-2xl">play_circle</span>
                      XEM TRAILER
                    </button>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Carousel Image Previews on the Right */}
        {!isLoadingSlides && (slides || []).length > 1 && (
          <div className="absolute right-6 md:right-10 top-[54%] z-20 hidden sm:flex -translate-y-1/2 flex-col items-end gap-3">
            {/* Up Button */}
            <button
              type="button"
              onClick={() => setCurrentSlideIndex((prev) => (prev - 1 + slides.length) % slides.length)}
              className="flex h-5 md:h-6 w-24 md:w-32 items-center justify-center rounded-md border border-white/5 bg-black/35 text-white backdrop-blur-sm opacity-35 hover:opacity-100 transition-all hover:bg-black/50 active:scale-[0.98]"
              aria-label="Previous slide"
            >
              <span className="material-symbols-outlined text-[22px] font-black leading-none">expand_less</span>
            </button>

            {/* Vertically Aligned Previews */}
            {(slides || []).map((slide, index) => {
              const isActive = index === currentSlideIndex
              return (
                <button
                  key={slide.id}
                  onClick={() => setCurrentSlideIndex(index)}
                  className={`group relative h-14 md:h-18 w-24 md:w-32 rounded-lg overflow-hidden border transition-all duration-500 ease-in-out ${isActive
                    ? "border-primary shadow-[0_0_15px_rgba(0,244,254,0.4)] opacity-100 scale-[1.04]"
                    : "border-outline-variant/30 opacity-40 scale-95 hover:opacity-75 hover:scale-98"
                    }`}
                  aria-label={`Go to slide ${index + 1}`}
                >
                  <img
                    src={slide.imageUrl}
                    alt={slide.title}
                    className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
                  />
                  {!isActive && <div className="absolute inset-0 bg-black/45 transition-opacity group-hover:opacity-20" />}
                  {isActive && (
                    <div className="absolute inset-0 border border-primary/30 animate-pulse pointer-events-none" />
                  )}
                </button>
              )
            })}

            {/* Down Button */}
            <button
              type="button"
              onClick={() => setCurrentSlideIndex((prev) => (prev + 1) % slides.length)}
              className="flex h-5 md:h-6 w-24 md:w-32 items-center justify-center rounded-md border border-white/5 bg-black/35 text-white backdrop-blur-sm opacity-35 hover:opacity-100 transition-all hover:bg-black/50 active:scale-[0.98]"
              aria-label="Next slide"
            >
              <span className="material-symbols-outlined text-[22px] font-black leading-none">expand_more</span>
            </button>
          </div>
        )}
      </section>

      <section className="bg-surface py-20 md:py-24">
        <div className="mx-auto max-w-screen-2xl px-6 md:px-8">
          <div className="mb-12 flex flex-col gap-6 border-l-4 border-secondary pl-6 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 className="font-headline text-3xl font-black uppercase tracking-tighter text-white sm:text-4xl">Phim Đang Chiếu</h2>
              <p className="font-medium text-slate-500">Những siêu phẩm điện ảnh không thể bỏ lỡ tuần này</p>
            </div>
            <Link className="group flex items-center gap-2 font-headline font-bold text-secondary transition-all hover:opacity-80" to="/movies">
              XEM TẤT CẢ
              <span className="material-symbols-outlined transition-transform group-hover:translate-x-1">arrow_forward</span>
            </Link>
          </div>

          <div className="grid grid-cols-1 gap-0 md:grid-cols-2 lg:grid-cols-4">
            {isLoadingData ? (
              Array.from({ length: 4 }).map((_, index) => (
                <div key={index} className="aspect-[2/3] w-full animate-pulse border border-outline-variant/10 bg-surface-container-low" />
              ))
            ) : nowShowingMovies.length === 0 ? (
              <div className="col-span-full py-12 text-center text-slate-500 font-medium">
                Hiện tại không có phim nào đang chiếu.
              </div>
            ) : (
              nowShowingMovies.map((movie) => (
                <div
                  key={movie.id}
                  className="group relative aspect-[2/3] overflow-hidden border border-outline-variant/10 bg-surface-container"
                >
                  <img
                    className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-110"
                    alt={movie.name}
                    src={movie.thumbnailUrl}
                  />
                  <div className="absolute right-4 top-4 z-20">
                    <span className="bg-surface-variant/60 px-3 py-1 font-headline text-xs font-bold uppercase tracking-widest text-secondary backdrop-blur-md">
                      {formatDuration(movie.duration)}
                    </span>
                  </div>
                  <div className="absolute inset-0 bg-gradient-to-t from-background via-transparent to-transparent opacity-90 transition-opacity group-hover:opacity-100" />
                  <div className="absolute bottom-0 w-full p-8">
                    <p className="mb-2 text-xs font-bold tracking-widest text-secondary">{genreLabel(movie.genre)}</p>
                    <h3 className="mb-4 font-headline text-2xl font-black leading-none text-white">{movie.name}</h3>
                    <Link
                      to={`/movies/${movie.id}/showtimes`}
                      className="block w-full translate-y-12 bg-secondary py-3 text-center font-black text-on-secondary opacity-0 transition-all duration-300 group-hover:translate-y-0 group-hover:opacity-100"
                    >
                      ĐẶT VÉ
                    </Link>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="bg-background py-24">
        <div className="mx-auto max-w-screen-2xl px-8">
          <div className="mb-16 flex flex-col items-center justify-between gap-4 md:flex-row">
            <h2 className="font-headline text-5xl font-black tracking-tighter text-white">SẮP KHỞI CHIẾU</h2>
            <div className="mx-8 hidden h-[2px] flex-grow bg-gradient-to-r from-secondary/50 to-transparent md:block" />
            <div className="flex gap-2">
              <button
                type="button"
                className="border border-outline-variant/30 p-2 text-outline transition-all hover:border-secondary hover:text-secondary"
              >
                <span className="material-symbols-outlined">chevron_left</span>
              </button>
              <button
                type="button"
                className="border border-outline-variant/30 p-2 text-outline transition-all hover:border-secondary hover:text-secondary"
              >
                <span className="material-symbols-outlined">chevron_right</span>
              </button>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            {isLoadingData ? (
              Array.from({ length: 3 }).map((_, index) => (
                <div key={index} className="flex gap-6 animate-pulse">
                  <div className="aspect-[3/4] w-1/3 flex-shrink-0 bg-surface-container-low" />
                  <div className="flex flex-grow flex-col gap-2 justify-center">
                    <div className="h-4 w-1/3 bg-surface-container-low rounded" />
                    <div className="h-6 w-2/3 bg-surface-container-low rounded" />
                    <div className="h-10 w-full bg-surface-container-low rounded" />
                  </div>
                </div>
              ))
            ) : upcomingMovies.length === 0 ? (
              <div className="col-span-full py-12 text-center text-slate-500 font-medium">
                Hiện tại không có phim nào sắp khởi chiếu.
              </div>
            ) : (
              upcomingMovies.map((movie) => (
                <div key={movie.id} className="group flex gap-6">
                  <div className="aspect-[3/4] w-1/3 flex-shrink-0 overflow-hidden">
                    <img className="h-full w-full object-cover transition-transform group-hover:scale-105" alt={movie.name} src={movie.thumbnailUrl} />
                  </div>
                  <div className="flex flex-col justify-center">
                    <span className="mb-1 font-headline text-sm font-black tracking-widest text-primary">{formatDateLabel(movie.createdAt)}</span>
                    <Link to={`/movies/${movie.id}/showtimes`}>
                      <h4 className="mb-3 font-headline text-xl font-bold text-white transition-colors group-hover:text-secondary">{movie.name}</h4>
                    </Link>
                    <p className="mb-5 text-sm text-slate-500 line-clamp-3">{movie.description}</p>
                    <button type="button" className="flex items-center gap-2 font-headline text-sm font-bold text-white transition-all hover:text-secondary">
                      <span className="material-symbols-outlined text-sm">notifications</span> NHẮC TÔI
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="bg-background py-24">
        <div className="mx-auto flex w-full max-w-screen-2xl flex-col gap-12 px-8">
          <div className="relative overflow-hidden rounded-xl border border-outline-variant/20 bg-surface-container-low p-8 shadow-[0_20px_40px_rgba(0,0,0,0.4)]">
            <div className="pointer-events-none absolute -left-24 -top-24 h-64 w-64 rounded-full bg-primary/20 blur-3xl" />
            <div className="relative z-10 flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
              <div>
                <h2 className="font-headline text-5xl font-bold tracking-tighter text-on-background md:text-6xl">
                  Khám Phá <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">Rạp Chiếu</span>
                </h2>
                <p className="mt-2 text-lg text-on-surface-variant">Tìm không gian điện ảnh đỉnh cao gần bạn nhất.</p>
              </div>
              <div className="flex w-full flex-col gap-4 sm:flex-row md:w-auto">
                <div className="relative w-full sm:w-72">
                  <span className="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-on-surface-variant">search</span>
                  <input
                    type="text"
                    value={cinemaSearchQuery}
                    onChange={(e) => setCinemaSearchQuery(e.target.value)}
                    placeholder="Tìm theo tên hoặc khu vực..."
                    className="w-full rounded-t-lg border-b-2 border-outline-variant bg-surface-container-highest py-3 pl-12 pr-4 text-on-background placeholder:text-on-surface-variant/50 outline-none transition-colors focus:border-secondary focus:ring-0"
                  />
                </div>
                <Link
                  to="/showtimes"
                  className="flex items-center justify-center gap-2 whitespace-nowrap rounded-lg border border-outline-variant/40 bg-transparent px-6 py-3 font-semibold text-primary transition-colors hover:bg-primary/10"
                >
                  <span className="material-symbols-outlined text-sm">filter_list</span>
                  Bộ lọc
                </Link>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-2 xl:grid-cols-3">
            {isLoadingData ? (
              Array.from({ length: 3 }).map((_, index) => (
                <div key={index} className="h-[400px] w-full animate-pulse rounded-xl bg-surface-container-low" />
              ))
            ) : filteredCinemas.length === 0 ? (
              <div className="col-span-full py-12 text-center text-slate-500 font-medium">
                Không tìm thấy rạp chiếu nào phù hợp.
              </div>
            ) : (
              filteredCinemas.map((cinema, index) => {
                const { tags, badges } = getCinemaDetails(index)
                const distance = getMockDistance(cinema.id)
                return (
                  <article
                    key={cinema.id}
                    className="group relative flex flex-col overflow-hidden rounded-xl border border-outline-variant/20 bg-surface-container-low shadow-[0_10px_30px_rgba(0,0,0,0.3)] transition-all duration-500 hover:-translate-y-2 hover:shadow-[0_20px_40px_rgba(0,0,0,0.5)]"
                  >
                    <div className="relative h-56 w-full overflow-hidden bg-surface-container-highest">
                      <img
                        alt={cinema.name}
                        src={cinema.thumbnailUrl}
                        className="h-full w-full object-cover opacity-80 transition-all duration-700 group-hover:scale-105 group-hover:opacity-100"
                      />
                      <div className="absolute inset-0 bg-gradient-to-t from-surface-container-low to-transparent" />
                      <div className="absolute right-4 top-4 flex gap-2">
                        {badges.map((badge) => (
                          <span
                            key={badge}
                            className="rounded-full border border-outline-variant/30 bg-surface-variant/60 px-3 py-1 text-xs font-semibold text-on-background shadow-lg backdrop-blur-md"
                          >
                            {badge}
                          </span>
                        ))}
                      </div>
                    </div>

                    <div className="relative z-10 -mt-6 flex flex-col gap-4 rounded-t-xl bg-surface-container-low/95 p-6 backdrop-blur-sm">
                      <div className="flex items-start justify-between gap-4">
                        <h3 className="font-headline text-2xl font-bold tracking-tight text-on-background transition-colors group-hover:text-secondary">
                          {cinema.name}
                        </h3>
                        <span className="flex items-center gap-1 rounded bg-secondary/10 px-2 py-1 font-bold text-secondary">
                          <span className="material-symbols-outlined text-[16px]">location_on</span>
                          {distance}
                        </span>
                      </div>

                      <p className="flex items-start gap-2 text-sm text-on-surface-variant">
                        <span className="material-symbols-outlined shrink-0 text-[18px] opacity-70">map</span>
                        {cinema.address}
                      </p>

                      <div className="mt-2 flex flex-wrap gap-2">
                        {tags.map((tag) => (
                          <span key={tag} className="rounded border border-primary/20 bg-primary/5 px-2 py-1 text-xs text-primary">
                            {tag}
                          </span>
                        ))}
                      </div>

                      <div className="mt-4 flex gap-4 border-t border-outline-variant/20 pt-4">
                        <Link
                          to="/showtimes"
                          className="flex-1 rounded border border-outline-variant/40 py-2 text-center font-semibold text-primary transition-colors hover:bg-primary/10"
                        >
                          Chi tiết
                        </Link>
                        <Link
                          to="/showtimes"
                          className="flex flex-1 items-center justify-center gap-2 rounded bg-gradient-to-r from-primary to-primary-container py-2 font-bold text-on-primary transition-all hover:shadow-[0_0_12px_rgba(0,244,254,0.5)]"
                        >
                          Lịch chiếu <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
                        </Link>
                      </div>
                    </div>
                  </article>
                )
              })
            )}
          </div>
        </div>
      </section>

      {/* <section className="mx-auto max-w-screen-2xl px-8 py-24">
        <div className="relative overflow-hidden border border-outline-variant/10 bg-surface-container-high p-12 md:p-24">
          <div className="pointer-events-none absolute right-0 top-0 h-full w-1/2 opacity-30">
            <div className="absolute inset-0 bg-gradient-to-l from-secondary/20 to-transparent" />
          </div>
          <div className="relative z-10 max-w-2xl">
            <h2 className="mb-6 font-headline text-4xl font-black tracking-tighter text-white md:text-5xl">
              TRỞ THÀNH HỘI VIÊN <span className="text-secondary">VIP</span>
            </h2>
            <p className="mb-10 text-lg leading-relaxed text-slate-400">
              Nhận ngay ưu đãi giảm giá 50% cho vé xem phim đầu tiên và tích điểm đổi quà không giới hạn.
            </p>
            <div className="flex flex-col gap-4 md:flex-row">
              <input
                type="tel"
                placeholder="Số điện thoại của bạn"
                className="flex-grow border-0 border-b-2 border-outline-variant bg-surface-container-low px-2 py-4 font-medium text-white placeholder:text-slate-600 focus:border-secondary focus:ring-0"
              />
              <button
                type="button"
                className="bg-secondary px-10 py-4 font-headline font-black text-on-secondary transition-all hover:shadow-[0_0_20px_rgba(0,244,254,0.4)]"
              >
                TƯ VẤN NGAY
              </button>
            </div>
          </div>
        </div>
      </section> */}

      <MovieTrailerModal
        isOpen={trailerData.isOpen}
        onClose={() => setTrailerData(prev => ({ ...prev, isOpen: false }))}
        movieName={trailerData.name}
        trailerUrl={trailerData.url}
      />
    </main>
  )
}

export default Home
