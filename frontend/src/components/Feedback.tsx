import type { ReactNode } from 'react'
import type { ApiError } from '../api/client.ts'
import { Icon } from './Icon.tsx'

// Veri okunamadığında içeriğin yerine geçer.
export function LoadError({ error, onRetry }: { error: ApiError; onRetry: () => void }) {
  return (
    <div className="notice notice-error" role="alert">
      <Icon name="warning" />
      <p>{error.message}</p>
      <button type="button" className="button button-quiet" onClick={onRetry}>
        Tekrar dene
      </button>
    </div>
  )
}

// Formun içinde: sunucunun açıkça reddettiği istek.
export function FormError({ children }: { children: ReactNode }) {
  return (
    <div className="notice notice-error" role="alert">
      <Icon name="warning" />
      <p>{children}</p>
    </div>
  )
}

// İsteğin sonucu bilinmiyor; aynı pencereden tekrar gönderilmesine izin verilmez.
export function Uncertain({ action, onClose }: { action: string; onClose: () => void }) {
  return (
    <>
      <div className="notice notice-warning" role="alert">
        <Icon name="warning" />
        <p>
          <strong>Sonuç belirsiz.</strong> Sunucudan cevap alınamadı; işlem yapılmış olabilir. Tekrar denemeden önce
          kontrol et.
        </p>
      </div>
      <div className="dialog-actions">
        <button type="button" className="button button-primary" onClick={onClose} autoFocus>
          {action}
        </button>
      </div>
    </>
  )
}

export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <svg className="empty-art" viewBox="0 0 120 80" aria-hidden="true">
        <path d="M10 70a50 50 0 0 1 100 0M26 70a34 34 0 0 1 68 0M42 70a18 18 0 0 1 36 0" />
        <path className="empty-star" d="M60 8c.8 6 4 9.200 10 10-6 .8-9.200 4-10 10-.8-6-4-9.200-10-10 6-.8 9.200-4 10-10z" />
      </svg>
      <h3>{title}</h3>
      {children}
    </div>
  )
}

export function SkeletonRows({ count }: { count: number }) {
  return (
    <div className="skeleton-rows" aria-hidden="true">
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="skeleton" />
      ))}
    </div>
  )
}
