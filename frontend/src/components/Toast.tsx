import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { Icon } from './Icon.tsx'

const ToastContext = createContext<(message: string) => void>(() => {})

export const useToast = () => useContext(ToastContext)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<{ id: number; message: string } | null>(null)
  const show = useCallback((message: string) => setToast((old) => ({ id: (old?.id ?? 0) + 1, message })), [])

  useEffect(() => {
    if (!toast) return
    const timer = setTimeout(() => setToast(null), 5000)
    return () => clearTimeout(timer)
  }, [toast])

  return (
    <ToastContext value={show}>
      {children}
      {/* Kutu hep sayfada durur; ekran okuyucu içine yazılanı duyurur. */}
      <div className="toast-area" role="status">
        {toast && (
          <div className="toast" key={toast.id}>
            <Icon name="check" />
            {toast.message}
          </div>
        )}
      </div>
    </ToastContext>
  )
}
