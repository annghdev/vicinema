import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { getActiveSlides } from "../apis/slideApi"
import { type Slide } from "../types/Slide"
import { MovieTrailerModal } from "../components/MovieTrailerModal"

function Home() {
  const [slides, setSlides] = useState<Slide[]>([])
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0)
  const [isLoadingSlides, setIsLoadingSlides] = useState(true)
  const [trailerData, setTrailerData] = useState<{ isOpen: boolean; url: string; name: string }>({
    isOpen: false,
    url: "",
    name: "",
  })

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
    if (slides.length <= 1) return

    const interval = setInterval(() => {
      setCurrentSlideIndex((prev) => (prev + 1) % slides.length)
    }, 5000)

    return () => clearInterval(interval)
  }, [slides.length])

  const currentSlide = slides[currentSlideIndex]

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
            {[
              {
                title: "BÓNG MA SỐ",
                genre: "HÀNH ĐỘNG / SCI-FI",
                tag: "IMAX 2D",
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuB441TQ4zaDFJaVwak7wkoqPlYKOO9HCCAbnxwIsXxRjDLSBMppuUIKptbYkswlfezxO0m0MGLvEv3Pnin31QegTK_QqhEUISfxgtNf6cD_pAAvfyAZpafp-QUBqUIJ-TTZgGUFeWFsVcbD9sv96Oid1VIAKbWbwFkGxoZH_dxcf8YZO2IwVZt6YZTKbR1_o_S2yhsJHo4qf5yPUfF0lBuM990VFQdIRASdo5Pa0wBE9N_I-v3gw3m-JBFGHMhz1yXZ6AsqC-SQeG8",
              },
              {
                title: "THIÊN HÀ RỰC CHÁY",
                genre: "PHIÊU LƯU / KỲ ẢO",
                tag: "3D SURROUND",
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuBQu8WkkrvsAmbKuM3Hy78ae7t6EUjRQcOrSme3BjDUnqMrJC0Evtw-2pDoRE-sJSqJZ2PlTQm8b9FzzvLA-_azF0cHrcT8sirpwIWad3LeHnB7_IM4vTU-QeTVwH5DY5kd8-DkFCqsZAs2ZWrJDk31TXiXWAatcntjQFatbEwSWe_Dd6pswk8-9P9gx04R3UEa8E_1VCEoUXMQ8gQhJpfn-jQ0KXI6AXr_SL3cui8Yuj2qc3iIvpQtsYxgR-SljKv_XNObklvOpCE",
              },
              {
                title: "GIAI ĐIỆU CUỐI",
                genre: "DRAMA / TÂM LÝ",
                tag: "HORIZON EXCLUSIVE",
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuBWeUkVRXpMKvJY4QjySG_Ma03jITEA8D7EVwlKw3ob7wa0YyfUl-5MLS_0r_WKQDo-3HN8Ll-BNtDzYekGBYOM58BVeLR65DL-FhG2lfDEA2BiWAd3cmKvoSaGX25669p__lXz5FQYi3BuijjBTmWbKvT8R4XoEYLRD8UZUxAwy0rEYTbc6YyGUWEIh2U-M0BigGZyxRahJVBV-IjdLqK3INFFVU1iEnBGOTAFwsntOopbT7q86yxHRecRuxEgFMsOyYJB-Yj7byI",
              },
              {
                title: "TRÍ TUỆ NHÂN TẠO",
                genre: "THRILLER / KINH DỊ",
                tag: "PREMIUM",
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuCfZN0pSVYsdGxwrIHVKnehXbLkaXStGazsbOdCyCY-mvW3bqr15nB_lsHKPs2M23YWF8k1FvvHe2qm7edn-WUhW5GtQDU40euLrsQeqZHCHG8PmIago2hr7KmPOJDnOaZNfdwX1531aDD5gkPF2m01IxUZP5e54wqRJ9NFwZjysVffF8lh7ZyD7aNje1M4Bqxj9RHBM6XQ74K7BBnQPDXOObb7DooZcD-uGv6prNMF1GRVs8P-ja83GLQZ-XLn-F3vmuAxlis-Voc",
              },
            ].map((movie) => (
              <div
                key={movie.title}
                className="group relative aspect-[2/3] overflow-hidden border border-outline-variant/10 bg-surface-container"
              >
                <img
                  className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-110"
                  alt={movie.title}
                  src={movie.image}
                />
                <div className="absolute right-4 top-4 z-20">
                  <span className="bg-surface-variant/60 px-3 py-1 font-headline text-xs font-bold uppercase tracking-widest text-secondary backdrop-blur-md">
                    {movie.tag}
                  </span>
                </div>
                <div className="absolute inset-0 bg-gradient-to-t from-background via-transparent to-transparent opacity-90 transition-opacity group-hover:opacity-100" />
                <div className="absolute bottom-0 w-full p-8">
                  <p className="mb-2 text-xs font-bold tracking-widest text-secondary">{movie.genre}</p>
                  <h3 className="mb-4 font-headline text-2xl font-black leading-none text-white">{movie.title}</h3>
                  <Link
                    to="/movies/echoes-of-eternity/showtimes"
                    className="block w-full translate-y-12 bg-secondary py-3 text-center font-black text-on-secondary opacity-0 transition-all duration-300 group-hover:translate-y-0 group-hover:opacity-100"
                  >
                    ĐẶT VÉ
                  </Link>
                </div>
              </div>
            ))}
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
            {[
              {
                date: "15.12.2024",
                title: "SAO HOẢ: NGÀY CUỐI",
                description: "Nỗ lực sinh tồn cuối cùng của nhân loại trên hành tinh đỏ đầy khắc nghiệt.",
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuAFLlZMs4cmfZw9seSCEaSKYcRsOZ0VF-iWKDEAtJfPQmbX99dd-pqQUh_pKhFbMhpGsW-TfqraOLgHR8_EZXgM7UdymXibECEEY9n2CvXSoTazsXYtSzJqHNm-q_8mWYOeNV4T3gT8R9vNZuFoif8LvaSF4BteX4myOVvkHKMGTUzf12SJU1nFEM4pG6tNBDk7yqlKFdrDidYYP4sLZlCN8Nr31eR2rQlnM4Ilhe56SYUOrF_VuVBFlN0y280MGvVCavG_f6DkT7k",
              },
              {
                date: "20.12.2024",
                title: "THÀNH PHỐ KHÔNG NGỦ",
                description: "Cuộc truy đuổi nghẹt thở trong thế giới ngầm của những kẻ đứng ngoài vòng pháp luật.",
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuChPcnpDT9D5r8ayBNHiBcGvHtN4mixoGqViVDxjNJwWgPgaa2y9NqNMo_w9DEz_8nzPq1RYg56OBGH2v3ALleNTx2zh57HTMkG2AIPLCPTcx4s8R9_838hIejqfUn0sMwfrTPgBIGUyTdwospGUe5l1YnvEil2hMYszoGxM9AjMdNf7Uf7NVlMKF-Or8ytthqliP-92N1VfngE8TFrGRU8XOlyB3BpYP9ZxO5KvhlU1VGw7pvqqZZ3KjMKbec9p0a_E39v0Wx7WWY",
              },
              {
                date: "01.01.2025",
                title: "ĐỈNH NÚI VÔ CỰC",
                description: "Hành trình khám phá bí ẩn vĩnh hằng ẩn sau những rặng núi tuyết trắng.",
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuCV3ne4VZCCEpHkgfwlLsyeQ5VtMl4wIPJ-Okss_UpyfPVL3YegJk7tx0iQRx1IQis3VsB5S8M036_RL3ljIF985F_OQqJK5loeaMNpkp8Y4zHSkoN48cBdor55pf-smuslXHiRW7BiDxfgy4Jnp2r5g3WUKm-aDQejnjFMf6GkvzwPIvRA2kDHkeJmTkmPLhg3JkeVfdg5w_0p1R-RWebdvsYTk4MofdqwP4c0CGzW04ANaXQUn_EPAvW9jx0I-FOu6e0W7vOVRpk",
              },
            ].map((movie) => (
              <div key={movie.title} className="group flex gap-6">
                <div className="aspect-[3/4] w-1/3 flex-shrink-0 overflow-hidden">
                  <img className="h-full w-full object-cover transition-transform group-hover:scale-105" alt={movie.title} src={movie.image} />
                </div>
                <div className="flex flex-col justify-center">
                  <span className="mb-1 font-headline text-sm font-black tracking-widest text-primary">{movie.date}</span>
                  <h4 className="mb-3 font-headline text-xl font-bold text-white transition-colors group-hover:text-secondary">{movie.title}</h4>
                  <p className="mb-5 text-sm text-slate-500">{movie.description}</p>
                  <button type="button" className="flex items-center gap-2 font-headline text-sm font-bold text-white transition-all hover:text-secondary">
                    <span className="material-symbols-outlined text-sm">notifications</span> NHẮC TÔI
                  </button>
                </div>
              </div>
            ))}
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
                    placeholder="Tìm theo tên hoặc khu vực..."
                    className="w-full rounded-t-lg border-b-2 border-outline-variant bg-surface-container-highest py-3 pl-12 pr-4 text-on-background placeholder:text-on-surface-variant/50 outline-none transition-colors focus:border-secondary focus:ring-0"
                  />
                </div>
                <button
                  type="button"
                  className="flex items-center justify-center gap-2 whitespace-nowrap rounded-lg border border-outline-variant/40 bg-transparent px-6 py-3 font-semibold text-primary transition-colors hover:bg-primary/10"
                >
                  <span className="material-symbols-outlined text-sm">filter_list</span>
                  Bộ lọc
                </button>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-2 xl:grid-cols-3">
            {[
              {
                name: "Horizon Grand Theater",
                distance: "2.4 km",
                address: "1010 Cyber Avenue, Neon District",
                tags: ["Dolby Atmos", "Ghế Recliner", "Dine-in"],
                badges: ["IMAX", "4DX"],
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuDOBuHcOdupL2kGWw-i_CXftcqV5dvdbTL9-5Us9Ke_oSnW9dKNkdjLoYZTsbtGiubCWkw2HU9fin3DcLXhKs-AowvykIol8n53YMZK9XYYyWt3alqQA_RmRUFmEeiF7I_hKAXIa1HKSbHJehuUIO_wO6fnwO8JOrOqlWCqBQt67manCCvfVFOksjm6JRvll2lQqnaK7eh-sV8_YyukBjSzrv_1D0W9CmTV3hI2Nza3O1L2cWOv_uTOcrjuH_I0RKQr6oMnLwjrj_8",
              },
              {
                name: "Auteur Multiplex",
                distance: "5.1 km",
                address: "444 Director's Cut Blvd, Uptown",
                tags: ["Laser Projection", "Bar"],
                badges: ["IMAX"],
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuBBzFzp9TSxT8fj-Bj_t96w6z9tw1fPoN7gQR3FMYoppXjf2CxpuUqHy9lgZlwJ-l0V-5KBP0fS75_WT3jlhr0OCZrLZeusR2rH98VlnyETNkQP9iC4wcUsN4wpC4HyD3JtekJIicRQwfR98sTzwcj_i6ax-MqNmpJ3RZ9q6IjNbS4qkRbMDxpmM3r-1g8QrOXmHJ9Qd6ZlIHPGsEUgmN_JFJO9tT5B96kT4FSvGPlJU-jm6hnlf_oyKo2EtupABhjMEV0UWP4nLGs",
              },
              {
                name: "Starlight Boutique",
                distance: "8.7 km",
                address: "77 Indie Lane, Arts District",
                tags: ["Phim Indie", "Cafe", "Recliners"],
                badges: ["VIP"],
                image:
                  "https://lh3.googleusercontent.com/aida-public/AB6AXuAZUTYUJ2nfo0THwyAMUon0MOUbrLbXJG2hCT1Fsly9-4aRvOmP1UiJPXtzY9wR7Fi_kVIb7TIK35fzNVs_FjkiMeYyKC20lE7GP_BnDtciM847VH45BBdNzIpP4yyeYVTgGyuQArjdU2EYWfh5hq_QMz8ba6lLKHFp7DtRJiIcTjoqS8plIygMS31iO2Tb8jCDwbPDgatcyY4hiqFOeF0g19SMVqscy3nhBRbvT-DOmSyrdYZ62YbT3Q12yCvMZsk6Dn0Du1p3B1w",
              },
            ].map((cinema) => (
              <article
                key={cinema.name}
                className="group relative flex flex-col overflow-hidden rounded-xl border border-outline-variant/20 bg-surface-container-low shadow-[0_10px_30px_rgba(0,0,0,0.3)] transition-all duration-500 hover:-translate-y-2 hover:shadow-[0_20px_40px_rgba(0,0,0,0.5)]"
              >
                <div className="relative h-56 w-full overflow-hidden bg-surface-container-highest">
                  <img
                    alt={cinema.name}
                    src={cinema.image}
                    className="h-full w-full object-cover opacity-80 transition-all duration-700 group-hover:scale-105 group-hover:opacity-100"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-surface-container-low to-transparent" />
                  <div className="absolute right-4 top-4 flex gap-2">
                    {cinema.badges.map((badge) => (
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
                      {cinema.distance}
                    </span>
                  </div>

                  <p className="flex items-start gap-2 text-sm text-on-surface-variant">
                    <span className="material-symbols-outlined shrink-0 text-[18px] opacity-70">map</span>
                    {cinema.address}
                  </p>

                  <div className="mt-2 flex flex-wrap gap-2">
                    {cinema.tags.map((tag) => (
                      <span key={tag} className="rounded border border-primary/20 bg-primary/5 px-2 py-1 text-xs text-primary">
                        {tag}
                      </span>
                    ))}
                  </div>

                  <div className="mt-4 flex gap-4 border-t border-outline-variant/20 pt-4">
                    <button type="button" className="flex-1 rounded border border-outline-variant/40 py-2 text-center font-semibold text-primary transition-colors hover:bg-primary/10">
                      Chi tiết
                    </button>
                    <Link
                      to="/movies/echoes-of-eternity/showtimes"
                      className="flex flex-1 items-center justify-center gap-2 rounded bg-gradient-to-r from-primary to-primary-container py-2 font-bold text-on-primary transition-all hover:shadow-[0_0_12px_rgba(0,244,254,0.5)]"
                    >
                      Lịch chiếu <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
                    </Link>
                  </div>
                </div>
              </article>
            ))}
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
