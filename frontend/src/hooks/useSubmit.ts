import { useRef, useState } from 'react'
import { ApiError } from '../api/client.ts'

// Veri değiştiren istekler için: iş bitmeden ikinci kez göndermez ve hatayı forma taşır.
export function useSubmit() {
  const inFlight = useRef(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  async function submit(send: () => Promise<void>, onFailed?: () => void) {
    if (inFlight.current) return // State güncellenmeden gelen ikinci tıklamayı da durdurur.
    inFlight.current = true
    setBusy(true)
    setError(null)

    try {
      await send()
    } catch (failure) {
      setError(failure instanceof ApiError ? failure : new ApiError('Beklenmeyen bir hata oluştu.', 0, null))
      onFailed?.()
    } finally {
      inFlight.current = false
      setBusy(false)
    }
  }

  return { busy, error, submit }
}
