const footerLinks = ["Điều khoản", "Quyền riêng tư", "Tuyển dụng", "Hỗ trợ"]

function Footer() {
  return (
    <footer className="w-full border-t border-[#45484f]/20 bg-[#0b0e14] px-8 py-12">
      <div className="mx-auto flex w-full max-w-screen-2xl flex-col items-center justify-between gap-6 md:flex-row">
        <div className="font-headline text-lg font-bold uppercase tracking-widest text-slate-200">
          VICINEMA
        </div>

        <div className="flex gap-8 text-xs uppercase tracking-widest text-slate-500 font-['Manrope']">
          {footerLinks.map((item) => (
            <a key={item} href="#" className="transition-colors hover:text-white">
              {item}
            </a>
          ))}
        </div>

        <div className="text-[10px] font-medium tracking-[0.2em] text-slate-500">
          © 2026 VICINEMA. ALL RIGHTS RESERVED.
        </div>
      </div>
    </footer>
  )
}

export default Footer
