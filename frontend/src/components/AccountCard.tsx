import { useEffect, useRef } from 'react'
import type { Account } from '../api/types.ts'
import { formatIban, formatMoney } from '../format.ts'

interface Props {
  account: Account
  selected: boolean
  onSelect: () => void
}

export function AccountCard({ account, selected, onSelect }: Props) {
  const ref = useRef<HTMLButtonElement>(null)
  const closed = account.status === 'Closed'

  // Telefonda kartlar yana kayar; seçili kart görünür kalsın.
  useEffect(() => {
    const card = ref.current
    const list = card?.closest<HTMLElement>('.card-list')
    if (selected && card && list) {
      list.scrollTo({ left: card.offsetLeft - (list.clientWidth - card.clientWidth) / 2 })
    }
  }, [selected])

  return (
    <button
      ref={ref}
      type="button"
      data-testid="account-card"
      className={`account-card ${closed ? 'tone-closed' : `tone-${account.currency}`}`}
      aria-pressed={selected}
      onClick={onSelect}
    >
      <svg className="card-art" viewBox="0 0 200 120" preserveAspectRatio="xMaxYMid slice" aria-hidden="true">
        <circle cx="178" cy="18" r="40" />
        <circle cx="178" cy="18" r="66" />
        <circle cx="178" cy="18" r="94" />
      </svg>
      <span className="card-top">
        <span className="card-currency">{account.currency}</span>
        <span className="card-status">{closed ? 'Kapalı' : 'Açık'}</span>
      </span>
      <span className="card-balance">{formatMoney(account.balance, account.currency)}</span>
      <span className="card-iban">{formatIban(account.iban)}</span>
    </button>
  )
}
