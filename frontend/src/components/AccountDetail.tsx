import { useState } from 'react'
import { listTransactions, TRANSACTIONS_PAGE_SIZE } from '../api/novabank.ts'
import type { Account } from '../api/types.ts'
import { formatDate, formatIban, formatMoney } from '../format.ts'
import { useQuery } from '../hooks/useQuery.ts'
import { CloseAccountDialog } from './CloseAccountDialog.tsx'
import { Empty, LoadError, SkeletonRows } from './Feedback.tsx'
import { Icon } from './Icon.tsx'
import { MoneyDialog } from './MoneyDialog.tsx'
import { Pager } from './Pager.tsx'
import { useToast } from './Toast.tsx'
import { TransactionList } from './TransactionList.tsx'

interface Props {
  account: Account
  onChanged: () => void // Hesap değişti ya da değişmiş olabilir: hesapları yeniden oku.
}

export function AccountDetail({ account, onChanged }: Props) {
  const [dialog, setDialog] = useState<'deposit' | 'withdraw' | 'close' | null>(null)
  const [page, setPage] = useState(1)
  const showToast = useToast()
  const transactions = useQuery(`transactions:${account.id}:${page}`, (signal) =>
    listTransactions(account.id, page, signal),
  )

  const closed = account.status === 'Closed'

  function refresh() {
    setPage(1)
    transactions.reload()
    onChanged()
  }

  function finish(message: string) {
    setDialog(null)
    showToast(message)
    refresh()
  }

  async function copyIban() {
    try {
      await navigator.clipboard.writeText(account.iban)
      showToast('IBAN kopyalandı.')
    } catch {
      showToast('IBAN kopyalanamadı.')
    }
  }

  return (
    <section className="panel detail" aria-label="Seçili hesap">
      <div className="detail-head">
        <div className="detail-summary">
          <h2>
            {account.currency} hesabı
            <span className={`badge ${closed ? 'badge-closed' : 'badge-open'}`}>{closed ? 'Kapalı' : 'Açık'}</span>
          </h2>
          <p className="detail-balance" data-testid="balance">
            {formatMoney(account.balance, account.currency)}
          </p>
          <p className="detail-iban">
            <span>{formatIban(account.iban)}</span>
            <button type="button" className="icon-button" aria-label="IBAN'ı kopyala" onClick={() => void copyIban()}>
              <Icon name="copy" />
            </button>
          </p>
          <p className="detail-date">
            {formatDate(account.openedAt)} tarihinde açıldı
            {account.closedAt && `, ${formatDate(account.closedAt)} tarihinde kapandı`}
          </p>
        </div>
        <div className="detail-actions">
          <button type="button" className="button button-primary" disabled={closed} onClick={() => setDialog('deposit')}>
            <Icon name="down" />
            Para yatır
          </button>
          <button type="button" className="button button-outline" disabled={closed} onClick={() => setDialog('withdraw')}>
            <Icon name="up" />
            Para çek
          </button>
          <button type="button" className="button button-quiet" disabled={closed} onClick={() => setDialog('close')}>
            <Icon name="lock" />
            Hesabı kapat
          </button>
        </div>
      </div>

      <h3 className="section-title">Hareketler</h3>
      {transactions.error && <LoadError error={transactions.error} onRetry={transactions.reload} />}
      {!transactions.data && !transactions.error && <SkeletonRows count={4} />}
      {transactions.data?.totalCount === 0 && (
        <Empty title="Henüz hareket yok">
          <p>{closed ? 'Bu hesap hareket görmeden kapatılmış.' : 'Para yatırınca burada görünecek.'}</p>
        </Empty>
      )}
      {transactions.data && transactions.data.items.length > 0 && (
        <TransactionList items={transactions.data.items} currency={account.currency} />
      )}
      {transactions.data && (
        <Pager
          page={page}
          pageSize={TRANSACTIONS_PAGE_SIZE}
          totalCount={transactions.data.totalCount}
          onChange={setPage}
        />
      )}

      {(dialog === 'deposit' || dialog === 'withdraw') && (
        <MoneyDialog
          key={dialog}
          kind={dialog}
          account={account}
          onClose={() => setDialog(null)}
          onDone={() => finish(dialog === 'deposit' ? 'Para yatırıldı.' : 'Para çekildi.')}
          onFailed={refresh}
        />
      )}
      {dialog === 'close' && (
        <CloseAccountDialog
          account={account}
          onClose={() => setDialog(null)}
          onClosed={() => finish('Hesap kapatıldı.')}
          onFailed={refresh}
          onWithdraw={() => setDialog('withdraw')}
        />
      )}
    </section>
  )
}
