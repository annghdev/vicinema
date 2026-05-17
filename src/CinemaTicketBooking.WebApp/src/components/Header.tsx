import { useEffect, useMemo, useRef, useState } from "react"
import { NavLink, useNavigate } from "react-router-dom"
import { logout } from "../apis/authApi"
import { useAuth } from "../contexts/AuthContext"
import AuthModal from "./AuthModal"

import logo from "../assets/logo.png"

const navItems = [
  { label: "Trang chủ", to: "/" },
  { label: "Thư viện phim", to: "/movies" },
  { label: "Lịch chiếu", to: "/showtimes" },
  { label: "Sự kiện", to: "/promos" },
  { label: "Hội viên", to: "/member" },
]

function Header() {
  const navigate = useNavigate()
  const { authState, displayName, avatarUrl, clearAuth } = useAuth()
  const [isAuthModalOpen, setIsAuthModalOpen] = useState(false)
  const [isAccountMenuOpen, setIsAccountMenuOpen] = useState(false)
  const [isLoggingOut, setIsLoggingOut] = useState(false)
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const mobileMenuRef = useRef<HTMLDivElement | null>(null)
  const mobileMenuBtnRef = useRef<HTMLButtonElement | null>(null)
  const accountMenuRef = useRef<HTMLDivElement | null>(null)


  useEffect(() => {
    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node
      if (
        !accountMenuRef.current?.contains(target) &&
        !mobileMenuRef.current?.contains(target) &&
        !mobileMenuBtnRef.current?.contains(target)
      ) {
        setIsAccountMenuOpen(false)
        setIsMobileMenuOpen(false)
      }
    }

    window.addEventListener("pointerdown", onPointerDown)
    return () => {
      window.removeEventListener("pointerdown", onPointerDown)
    }
  }, [])

  useEffect(() => {
    if (!authState) {
      setIsAccountMenuOpen(false)
    }
  }, [authState])

  const navigateFromMenu = (path: string) => {
    setIsAccountMenuOpen(false)
    setIsMobileMenuOpen(false)
    navigate(path)
  }

  const onLogout = async () => {
    if (isLoggingOut) {
      return
    }
    setIsLoggingOut(true)
    try {
      await logout()
    } catch {
      // Keep local logout flow regardless of API response.
    } finally {
      clearAuth()
      setIsAccountMenuOpen(false)
      setIsMobileMenuOpen(false)
      setIsLoggingOut(false)
      navigate("/")
    }
  }

  return (
    <>
      <header className="fixed top-0 z-50 w-full border-b border-outline-variant/20 bg-background/95 shadow-lg shadow-black/20 backdrop-blur-md">
        <div className="mx-auto flex w-full max-w-screen-2xl items-center justify-between px-6 py-4 md:px-8">
          <div className="flex items-center gap-4">
            <button
              ref={mobileMenuBtnRef}
              type="button"
              className="material-symbols-outlined flex h-10 w-10 items-center justify-center rounded-lg text-slate-400 transition-all hover:bg-white/5 md:hidden"
              onClick={() => setIsMobileMenuOpen((prev) => !prev)}
            >
              {isMobileMenuOpen ? "close" : "menu"}
            </button>
            <div
              className="flex cursor-pointer items-center gap-2 transition-transform active:scale-95"
              onClick={() => navigate("/")}
            >
              <img src={logo} alt="Vicienema" className="h-8 w-auto sm:h-10" />
              <span className="font-headline text-xl font-black tracking-tighter text-[#61b4fe] sm:text-2xl">
                VICINEMA
              </span>
            </div>
          </div>

          <nav className="hidden items-center space-x-8 font-headline font-bold tracking-tight md:flex">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  isActive
                    ? "active:scale-95 border-b-2 border-[#00f4fe] pb-1 text-[#00f4fe] drop-shadow-[0_0_8px_rgba(0,244,254,0.6)] transition-all duration-300 ease-out"
                    : "px-2 py-1 text-slate-400 transition-all hover:text-[#00f4fe] hover:drop-shadow-[0_0_5px_rgba(0,244,254,0.4)] active:scale-95"
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex items-center gap-2 sm:gap-4">
            <button
              type="button"
              aria-label="Tìm kiếm"
              className="material-symbols-outlined text-slate-400 transition-all hover:text-[#00f4fe]"
            >
              search
            </button>
            {authState ? (
              <div className="relative" ref={accountMenuRef}>
                <button
                  type="button"
                  aria-label="Mở menu tài khoản"
                  onClick={() => setIsAccountMenuOpen((prev) => !prev)}
                  className="flex items-center gap-2 rounded-full border border-outline-variant/30 bg-surface-container-highest px-2 py-1 transition-colors hover:border-secondary/50"
                >
                  <img
                    src={avatarUrl && avatarUrl.trim() !== "" ? avatarUrl : `https://api.dicebear.com/7.x/adventurer/svg?seed=${encodeURIComponent(displayName || "User")}`}
                    alt={displayName ?? "Avatar"}
                    className="h-7 w-7 rounded-full object-cover sm:h-8 sm:w-8"
                  />
                  <span className="hidden max-w-[160px] truncate text-sm text-on-surface md:block">{displayName}</span>
                  <span className="material-symbols-outlined text-lg text-on-surface-variant">keyboard_arrow_down</span>
                </button>
                {isAccountMenuOpen && (
                  <div className="absolute right-0 top-12 z-50 w-56 overflow-hidden rounded-lg border border-outline-variant/20 bg-surface-container-highest shadow-xl">
                    <button
                      type="button"
                      onClick={() => navigateFromMenu("/booking-history")}
                      className="flex w-full items-center gap-2 px-4 py-3 text-left text-sm text-on-surface transition-colors hover:bg-surface-container-high"
                    >
                      <span className="material-symbols-outlined text-base">confirmation_number</span>
                      Lịch sử đặt vé
                    </button>
                    <button
                      type="button"
                      onClick={() => navigateFromMenu("/member?section=profile")}
                      className="flex w-full items-center gap-2 px-4 py-3 text-left text-sm text-on-surface transition-colors hover:bg-surface-container-high"
                    >
                      <span className="material-symbols-outlined text-base">badge</span>
                      Thông tin cá nhân
                    </button>
                    <button
                      type="button"
                      onClick={() => void onLogout()}
                      disabled={isLoggingOut}
                      className="flex w-full items-center gap-2 border-t border-outline-variant/20 px-4 py-3 text-left text-sm text-red-300 transition-colors hover:bg-red-500/10 disabled:opacity-60"
                    >
                      <span className="material-symbols-outlined text-base">logout</span>
                      {isLoggingOut ? "Đang đăng xuất..." : "Đăng xuất"}
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <button
                type="button"
                onClick={() => setIsAuthModalOpen(true)}
                className="flex items-center gap-2 rounded-full bg-gradient-to-r from-primary to-primary-container px-4 py-1.5 text-xs font-bold uppercase tracking-widest text-on-primary shadow-[0_0_15px_rgba(97,180,254,0.3)] transition-all hover:scale-105 hover:shadow-[0_0_20px_rgba(0,244,254,0.5)] active:scale-95 sm:px-6 sm:py-2 sm:text-sm"
              >
                <span className="material-symbols-outlined text-lg sm:text-xl">account_circle</span>
                <span>Đăng nhập</span>
              </button>
            )}
          </div>
        </div>

        {/* Mobile Navigation Panel */}
        <div
          ref={mobileMenuRef}
          className={`absolute left-0 top-[calc(100%-1px)] w-full border-b border-outline-variant/20 bg-background/95 shadow-xl backdrop-blur-md transition-all duration-300 md:hidden ${isMobileMenuOpen ? "translate-y-0 opacity-100" : "-translate-y-4 opacity-0 pointer-events-none"
            }`}
        >
          <nav className="flex flex-col p-4">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={() => setIsMobileMenuOpen(false)}
                className={({ isActive }) =>
                  `flex items-center gap-3 rounded-xl px-4 py-3 font-headline font-bold transition-all active:scale-[0.98] ${isActive ? "bg-primary/10 text-[#00f4fe]" : "text-slate-400 hover:bg-white/5"
                  }`
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>


      <AuthModal open={isAuthModalOpen} onOpenChange={setIsAuthModalOpen} />
    </>
  )
}

export default Header
