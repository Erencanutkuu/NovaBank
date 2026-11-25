import type { Currency, Transaction } from '../api/types.ts'
import { formatDateTime, formatMoney } from '../format.ts'
import { Icon } from './Icon.tsx'

export function TransactionList({ items, currency }: { items: Transaction[]; currency: Currency }) {
  return (
    <table className="transactions">
      <thead>
        <tr>
          <th scope="col">İşlem</th>
          <th scope="col" className="number">
            Tutar
          </th>
          <th scope="col" className="number">
            Kalan bakiye
          </th>
        </tr>
      </thead>
      <tbody>
        {items.map((item) => {
          const deposit = item.type === 'Deposit'
          return (
            <tr key={item.id} data-testid="transaction" className={deposit ? 'is-in' : 'is-out'}>
              <td>
                <span className="transaction-what">
                  <span className="transaction-icon">
                    <Icon name={deposit ? 'down' : 'up'} />
                  </span>
                  <span>
                    <strong>{deposit ? 'Para yatırma' : 'Para çekme'}</strong>
                    <time dateTime={item.createdAt}>{formatDateTime(item.createdAt)}</time>
                  </span>
                </span>
              </td>
              <td className="number transaction-amount">
                {deposit ? '+' : '−'}
                {formatMoney(item.amount, currency)}
              </td>
              <td className="number transaction-balance">{formatMoney(item.balanceAfter, currency)}</td>
            </tr>
          )
        })}
      </tbody>
    </table>
  )
}
