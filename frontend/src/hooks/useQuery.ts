import { useCallback, useEffect, useRef, useState } from 'react'
import { ApiError } from '../api/client.ts'

interface State<T> {
  key: string
  data?: T
  error?: ApiError
}

// Veriyi okur. key değişince eski istek iptal edilir ve eski cevap bir daha gösterilmez.
export function useQuery<T>(key: string | null, load: (signal: AbortSignal) => Promise<T>) {
  const [state, setState] = useState<State<T>>()
  const [version, setVersion] = useState(0)
  const latestLoad = useRef(load)
  useEffect(() => {
    latestLoad.current = load
  })

  useEffect(() => {
    if (key === null) return
    const controller = new AbortController()

    latestLoad.current(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) setState({ key, data })
      },
      (error: unknown) => {
        if (controller.signal.aborted) return
        const apiError = error instanceof ApiError ? error : new ApiError('Beklenmeyen bir hata oluştu.', 0, null)
        // Yenileme başarısızsa eldeki veri ekranda kalır.
        setState((previous) => ({ key, data: previous?.key === key ? previous.data : undefined, error: apiError }))
      },
    )

    return () => controller.abort()
  }, [key, version])

  const reload = useCallback(() => setVersion((current) => current + 1), [])
  const current = state?.key === key ? state : undefined

  return { data: current?.data, error: current?.error, reload }
}
