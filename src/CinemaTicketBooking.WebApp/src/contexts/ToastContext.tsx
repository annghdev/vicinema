import React, { createContext, useContext, useState, useCallback, useEffect } from "react"
import * as Toast from "@radix-ui/react-toast"
import { registerGlobalErrorHandler } from "../apis/httpClient"

type ToastType = "success" | "error" | "warning" | "info"

type ToastItem = {
  id: string
  message: string
  title?: string
  type?: ToastType
}

type ToastContextType = {
  showToast: (message: string, options?: { title?: string; type?: ToastType }) => void
  success: (message: string, title?: string) => void
  error: (message: string, title?: string) => void
  warning: (message: string, title?: string) => void
  info: (message: string, title?: string) => void
}

const ToastContext = createContext<ToastContextType | undefined>(undefined)

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error("useToast must be used within a ToastProvider")
  }
  return context
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  const showToast = useCallback((message: string, options?: { title?: string; type?: ToastType }) => {
    setToasts((prev) => {
      // 1. Prevent showing duplicate active toasts with the exact same message and type
      const isDuplicate = prev.some(
        (t) => t.message === message && t.type === options?.type
      )
      if (isDuplicate) {
        return prev
      }
      
      const id = Math.random().toString(36).substring(2, 9)
      return [...prev, { id, message, ...options }]
    })
  }, [])

  useEffect(() => {
    registerGlobalErrorHandler((message) => {
      showToast(message, { type: "error", title: "Lỗi kết nối" })
    })
  }, [showToast])

  const success = useCallback((message: string, title = "Thành công") => showToast(message, { title, type: "success" }), [showToast])
  const error = useCallback((message: string, title = "Lỗi") => showToast(message, { title, type: "error" }), [showToast])
  const warning = useCallback((message: string, title = "Cảnh báo") => showToast(message, { title, type: "warning" }), [showToast])
  const info = useCallback((message: string, title = "Thông báo") => showToast(message, { title, type: "info" }), [showToast])

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  return (
    <ToastContext.Provider value={{ showToast, success, error, warning, info }}>
      <Toast.Provider swipeDirection="right">
        {children}
        
        {toasts.map((toast) => {
          const typeClass =
            toast.type === "success"
              ? "border-green-500/30 bg-green-900/90 text-green-200"
              : toast.type === "error"
                ? "border-red-500/30 bg-red-900/90 text-red-200"
                : toast.type === "warning"
                  ? "border-amber-500/30 bg-amber-950/90 text-amber-200"
                  : "border-blue-500/30 bg-blue-900/90 text-blue-200"

          const icon =
            toast.type === "success"
              ? "check_circle"
              : toast.type === "error"
                ? "error"
                : toast.type === "warning"
                  ? "warning"
                  : "info"

          return (
            <Toast.Root
              key={toast.id}
              duration={5000}
              onOpenChange={(open) => {
                if (!open) {
                  setTimeout(() => removeToast(toast.id), 300)
                }
              }}
              className={`toast-root flex flex-col gap-1 rounded-xl border p-4 shadow-2xl backdrop-blur-xl pointer-events-auto w-full max-w-sm ${typeClass}`}
            >
              <div className="flex items-start gap-3">
                <span className="material-symbols-outlined mt-0.5 shrink-0">{icon}</span>
                <div className="min-w-0 flex-1">
                  {toast.title && (
                    <Toast.Title className="font-headline text-sm font-semibold">
                      {toast.title}
                    </Toast.Title>
                  )}
                  <Toast.Description className="mt-1 text-sm leading-relaxed opacity-90 break-words">
                    {toast.message}
                  </Toast.Description>
                </div>
                <Toast.Close className="ml-auto hover:opacity-75 transition-opacity shrink-0">
                  <span className="material-symbols-outlined text-base">close</span>
                </Toast.Close>
              </div>
            </Toast.Root>
          )
        })}

        <Toast.Viewport className="fixed top-4 right-4 z-[100] flex flex-col p-4 gap-2 w-full max-w-sm m-0 list-none outline-none pointer-events-none items-end" />
      </Toast.Provider>
    </ToastContext.Provider>
  )
}
