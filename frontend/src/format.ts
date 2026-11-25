import type { Currency } from './api/types.ts'

export const MAX_AMOUNT = 1_000_000

const SYMBOLS: Record<Currency, string> = { TRY: 'TL', USD: '$', EUR: '€' }

const decimals = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const dateTime = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
const date = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })

export const formatMoney = (amount: number, currency: Currency) => `${decimals.format(amount)} ${SYMBOLS[currency]}`

// Tutar kutusuna yazılacak biçim: 1234.56 -> "1.234,56"
export const formatAmount = (amount: number) => decimals.format(amount)

export const formatDateTime = (iso: string) => dateTime.format(new Date(iso))

export const formatDate = (iso: string) => date.format(new Date(iso))

export const formatIban = (iban: string) => iban.replace(/(.{4})(?=.)/g, '$1 ')

// Türkçe kurallarla: "ışıl" -> "I", "ipek" -> "İ"
export const initials = (...names: string[]) =>
  names.map((name) => (Array.from(name.trim())[0] ?? '').toLocaleUpperCase('tr')).join('')

// "1.250,50" -> 1250.5. Nokta yalnızca doğru yerdeyse binlik ayırıcıdır; "10.5" gibi belirsiz yazımlar null döner.
export function parseAmount(text: string): number | null {
  const trimmed = text.trim()
  if (!/^(?:[1-9]\d{0,2}(?:\.\d{3})+|\d+)(?:,\d{1,2})?$/.test(trimmed)) return null
  return Number(trimmed.replaceAll('.', '').replace(',', '.'))
}
