const TIMEOUT_MS = 15_000
const UNREACHABLE = 'Sunucuya ulaşılamıyor. API çalışıyor mu, kontrol et.'

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    // Backend'in hata kodu; cevap backend'den gelmediyse boştur.
    readonly code: string | null,
    readonly fields: Record<string, string[]> = {},
  ) {
    super(message)
  }

  // Sunucu isteği açıkça reddetmediyse işlemin yapılıp yapılmadığı bilinemez.
  get uncertain(): boolean {
    return this.code === null || this.status >= 500
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST'
  body?: unknown
  signal?: AbortSignal
}

// Başarılı cevabın "data" alanını döner; diğer her durumda ApiError fırlatır. İsteği asla kendiliğinden tekrarlamaz.
export async function request<T>(path: string, { method = 'GET', body, signal }: RequestOptions = {}): Promise<T> {
  const timeout = AbortSignal.timeout(TIMEOUT_MS)

  let response: Response
  let json: unknown
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: signal ? AbortSignal.any([signal, timeout]) : timeout,
    })
    json = await response.json()
  } catch (error) {
    if (signal?.aborted) throw error
    throw new ApiError(UNREACHABLE, 0, null)
  }

  if (response.ok && isObject(json) && 'data' in json) {
    return json.data as T
  }

  if (!response.ok && isObject(json) && typeof json.errorCode === 'string') {
    const title = typeof json.title === 'string' ? json.title : 'İşlem yapılamadı.'
    const fields = isObject(json.errors) ? (json.errors as Record<string, string[]>) : {}
    throw new ApiError(title, response.status, json.errorCode, fields)
  }

  throw new ApiError(UNREACHABLE, response.status, null)
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}
