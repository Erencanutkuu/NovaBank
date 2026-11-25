import { useId, useLayoutEffect, useRef, type ReactNode } from 'react'
import { Icon } from './Icon.tsx'

interface Props {
  title: string
  busy?: boolean // İşlem sürerken pencere kapatılamaz.
  onClose: () => void
  children: ReactNode
}

// Tarayıcının kendi <dialog> öğesi: odağı içeride tutar, Escape'i bilir, kapanınca odağı geri verir.
export function Dialog({ title, busy = false, onClose, children }: Props) {
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()

  useLayoutEffect(() => {
    const dialog = ref.current!
    if (!dialog.open) dialog.showModal()
    // showModal odağı ilk düğmeye (kapat) verir; asıl alana biz taşırız.
    dialog.querySelector<HTMLElement>('[data-autofocus]')?.focus()
    return () => dialog.close()
  }, [])

  return (
    <dialog
      ref={ref}
      className="dialog"
      aria-labelledby={titleId}
      onCancel={(event) => {
        if (busy) event.preventDefault()
      }}
      onClose={() => {
        const dialog = ref.current
        if (!dialog || dialog.open) return
        // Chrome art arda Escape'te iptali engelletmez; iş sürüyorsa pencereyi geri aç.
        if (busy) dialog.showModal()
        else onClose()
      }}
      onMouseDown={(event) => {
        if (event.target === ref.current && !busy) onClose() // Pencerenin dışına tıklandı.
      }}
    >
      <div className="dialog-body">
        <header className="dialog-header">
          <h2 id={titleId}>{title}</h2>
          <button type="button" className="icon-button" aria-label="Pencereyi kapat" disabled={busy} onClick={onClose}>
            <Icon name="close" />
          </button>
        </header>
        {children}
      </div>
    </dialog>
  )
}
