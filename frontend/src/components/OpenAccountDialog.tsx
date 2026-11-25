import { useState } from 'react'
import { openAccount } from '../api/novabank.ts'
import type { Account, Currency } from '../api/types.ts'
import { useSubmit } from '../hooks/useSubmit.ts'
import { Dialog } from './Dialog.tsx'
import { FormError, Uncertain } from './Feedback.tsx'

const OPTIONS: { currency: Currency; symbol: string; name: string }[] = [
  { currency: 'TRY', symbol: '₺', name: 'Türk lirası' },
  { currency: 'USD', symbol: '$', name: 'ABD doları' },
  { currency: 'EUR', symbol: '€', name: 'Euro' },
]

interface Props {
  customerId: string
  onClose: () => void
  onOpened: (account: Account) => void
  onFailed: () => void
}

export function OpenAccountDialog({ customerId, onClose, onOpened, onFailed }: Props) {
  const [currency, setCurrency] = useState<Currency>('TRY')
  const { busy, error, submit } = useSubmit()

  return (
    <Dialog title="Yeni hesap" busy={busy} onClose={onClose}>
      {error?.uncertain ? (
        <Uncertain action="Hesapları kontrol et" onClose={onClose} />
      ) : (
        <form
          onSubmit={(event) => {
            event.preventDefault()
            void submit(async () => onOpened(await openAccount(customerId, currency)), onFailed)
          }}
        >
          {error && <FormError>{error.message}</FormError>}
          <fieldset className="currency-options" disabled={busy}>
            <legend>Para birimi</legend>
            {OPTIONS.map((option) => (
              <label key={option.currency} className={`currency-option tone-${option.currency}`}>
                <input
                  type="radio"
                  name="currency"
                  value={option.currency}
                  checked={currency === option.currency}
                  onChange={() => setCurrency(option.currency)}
                />
                <span className="currency-symbol" aria-hidden="true">
                  {option.symbol}
                </span>
                <span className="currency-label">
                  <strong>{option.currency}</strong>
                  {option.name}
                </span>
              </label>
            ))}
          </fieldset>
          <p className="dialog-note">Para birimi sonradan değiştirilemez. IBAN otomatik verilir.</p>
          <div className="dialog-actions">
            <button type="button" className="button button-quiet" disabled={busy} onClick={onClose}>
              Vazgeç
            </button>
            <button type="submit" className="button button-primary" disabled={busy}>
              Hesap aç
            </button>
          </div>
        </form>
      )}
    </Dialog>
  )
}
