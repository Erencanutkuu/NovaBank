import { useState } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { getCustomer, listAccounts } from '../api/novabank.ts'
import type { Account } from '../api/types.ts'
import { AccountCard } from '../components/AccountCard.tsx'
import { AccountDetail } from '../components/AccountDetail.tsx'
import { Empty, LoadError, SkeletonRows } from '../components/Feedback.tsx'
import { Icon } from '../components/Icon.tsx'
import { OpenAccountDialog } from '../components/OpenAccountDialog.tsx'
import { useToast } from '../components/Toast.tsx'
import { formatDate, initials } from '../format.ts'
import { useQuery } from '../hooks/useQuery.ts'
import { useTitle } from '../hooks/useTitle.ts'
import { NotFound } from './NotFound.tsx'

const GUID = /^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i
const CUSTOMER_NOT_FOUND = '7232'

export function CustomerPage() {
  const { id = '' } = useParams()

  // key: başka müşteriye geçince ekranın bütün durumu sıfırlanır.
  return GUID.test(id) ? <CustomerView key={id} id={id} /> : <NotFound title="Müşteri bulunamadı." />
}

function CustomerView({ id }: { id: string }) {
  const showToast = useToast()
  const [opening, setOpening] = useState(false)
  const customer = useQuery(`customer:${id}`, (signal) => getCustomer(id, signal))
  const accounts = useQuery(`accounts:${id}`, (signal) => listAccounts(id, signal))

  // Seçili hesap adreste durur; sayfa yenilenince kaybolmaz.
  const [params, setParams] = useSearchParams()
  const selected = pickAccount(accounts.data ?? [], params.get('account'))
  const select = (accountId: string) => setParams({ account: accountId }, { replace: true })

  const name = customer.data ? `${customer.data.firstName} ${customer.data.lastName}` : 'Müşteri'
  useTitle(name)

  if (customer.error?.code === CUSTOMER_NOT_FOUND) {
    return <NotFound title={customer.error.message} />
  }

  return (
    <main>
      <div className="hero hero-tall">
        <div className="container hero-inner">
          {customer.data ? (
            <div className="hero-person">
              <span className="avatar avatar-large" aria-hidden="true">
                {initials(customer.data.firstName, customer.data.lastName)}
              </span>
              <div className="hero-text">
                <h1>{name}</h1>
                <p>
                  {customer.data.email} · {formatDate(customer.data.createdAt)} tarihinden beri
                </p>
              </div>
            </div>
          ) : (
            <div className="hero-text">
              <h1>{customer.error ? 'Müşteri açılamadı' : 'Yükleniyor…'}</h1>
            </div>
          )}
          <button type="button" className="button button-accent" disabled={!accounts.data} onClick={() => setOpening(true)}>
            <Icon name="plus" />
            Hesap aç
          </button>
        </div>
      </div>

      <div className="container overlap">
        {(customer.error || accounts.error) && (
          <div className="panel">
            <LoadError
              error={(customer.error ?? accounts.error)!}
              onRetry={() => {
                customer.reload()
                accounts.reload()
              }}
            />
          </div>
        )}
        {!accounts.data && !accounts.error && (
          <div className="panel">
            <SkeletonRows count={3} />
          </div>
        )}
        {accounts.data?.length === 0 && (
          <div className="panel">
            <Empty title="Henüz hesap yok">
              <p>TRY, USD veya EUR hesabı açarak başla.</p>
              <button type="button" className="button button-primary" onClick={() => setOpening(true)}>
                Hesap aç
              </button>
            </Empty>
          </div>
        )}
        {accounts.data && accounts.data.length > 0 && (
          <div className="card-list" role="group" aria-label="Hesaplar">
            {accounts.data.map((account) => (
              <AccountCard
                key={account.id}
                account={account}
                selected={account.id === selected?.id}
                onSelect={() => select(account.id)}
              />
            ))}
          </div>
        )}
        {selected && (
          <AccountDetail
            key={selected.id}
            account={selected}
            onChanged={() => {
              select(selected.id) // Kapanan hesap ekranda kalsın, seçim başka hesaba atlamasın.
              accounts.reload()
            }}
          />
        )}
      </div>

      {opening && (
        <OpenAccountDialog
          customerId={id}
          onClose={() => setOpening(false)}
          onFailed={accounts.reload}
          onOpened={(account) => {
            setOpening(false)
            showToast('Hesap açıldı.')
            select(account.id)
            accounts.reload()
          }}
        />
      )}
    </main>
  )
}

// Adresteki hesap; yoksa ilk açık hesap; o da yoksa ilk hesap.
function pickAccount(accounts: Account[], id: string | null): Account | undefined {
  return (
    accounts.find((account) => account.id === id) ??
    accounts.find((account) => account.status === 'Open') ??
    accounts[0]
  )
}
