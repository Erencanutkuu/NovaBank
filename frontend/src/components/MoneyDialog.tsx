import { useRef, useState } from 'react'
import { deposit, withdraw } from '../api/novabank.ts'
import type { Account } from '../api/types.ts'
import { formatAmount, formatMoney, MAX_AMOUNT, parseAmount } from '../format.ts'
import { useSubmit } from '../hooks/useSubmit.ts'
import { Dialog } from './Dialog.tsx'
import { FormError, Uncertain } from './Feedback.tsx'
import { Field } from './Field.tsx'

const QUICK_AMOUNTS = ['100', '250', '500', '1.000']

interface Props {
  kind: 'deposit' | 'withdraw'
  account: Account
  onClose: () => void
  onDone: () => void
  onFailed: () => void
}

export function MoneyDialog({ kind, account, onClose, onDone, onFailed }: Props) {
  const [text, setText] = useState('')
  const [localError, setLocalError] = useState<string>()
  const input = useRef<HTMLInputElement>(null)
  const { busy, error, submit } = useSubmit()

  const action = kind === 'deposit' ? 'Para yatır' : 'Para çek'
  const amount = parseAmount(text)
  const serverFieldError = error?.fields.amount?.[0]

  function change(value: string) {
    setText(value)
    setLocalError(undefined)
  }

  function pick(value: string) {
    change(value)
    input.current?.focus()
  }

  function send() {
    const problem = findProblem(amount)
    setLocalError(problem)
    if (problem !== undefined || amount === null) {
      input.current?.focus()
      return
    }

    void submit(async () => {
      await (kind === 'deposit' ? deposit : withdraw)(account.id, amount)
      onDone()
    }, onFailed)
  }

  return (
    <Dialog title={action} busy={busy} onClose={onClose}>
      {error?.uncertain ? (
        <Uncertain action="Hareketleri kontrol et" onClose={onClose} />
      ) : (
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault()
            send()
          }}
        >
          {error && !serverFieldError && !localError && <FormError>{error.message}</FormError>}
          <Field
            className="amount-field"
            label="Tutar"
            inputRef={input}
            inputMode="decimal"
            autoComplete="off"
            data-autofocus
            placeholder="0,00"
            suffix={account.currency}
            value={text}
            readOnly={busy}
            error={localError ?? serverFieldError}
            hint={<span data-testid="amount-preview">{describe(kind, account, amount)}</span>}
            onChange={(event) => change(event.target.value)}
          />
          <div className="chips" role="group" aria-label="Hızlı seçim">
            {QUICK_AMOUNTS.map((value) => (
              <button key={value} type="button" className="chip" disabled={busy} onClick={() => pick(value)}>
                {value}
              </button>
            ))}
            {kind === 'withdraw' && account.balance > 0 && (
              <button
                type="button"
                className="chip"
                disabled={busy}
                onClick={() => pick(formatAmount(Math.min(account.balance, MAX_AMOUNT)))}
              >
                Tümü
              </button>
            )}
          </div>
          <div className="dialog-actions">
            <button type="button" className="button button-quiet" disabled={busy} onClick={onClose}>
              Vazgeç
            </button>
            <button type="submit" className="button button-primary" disabled={busy}>
              {action}
            </button>
          </div>
        </form>
      )}
    </Dialog>
  )
}

function findProblem(amount: number | null): string | undefined {
  if (amount === null) return 'Tutarı 1.250,50 biçiminde yaz.'
  if (amount <= 0 || amount > MAX_AMOUNT) return "Tutar 0'dan büyük, en çok 1.000.000 olmalıdır."
  return undefined
}

// Yazılan tutarın nasıl anlaşıldığı gönderilmeden önce görünür.
function describe(kind: Props['kind'], account: Account, amount: number | null): string {
  if (amount === null || findProblem(amount) !== undefined) {
    return `Bakiye: ${formatMoney(account.balance, account.currency)}`
  }

  return `${formatMoney(amount, account.currency)} ${kind === 'deposit' ? 'yatırılacak' : 'çekilecek'}`
}
