import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { CUSTOMERS_PAGE_SIZE, listCustomers } from '../api/novabank.ts'
import { Empty, LoadError, SkeletonRows } from '../components/Feedback.tsx'
import { Icon } from '../components/Icon.tsx'
import { NewCustomerDialog } from '../components/NewCustomerDialog.tsx'
import { Pager } from '../components/Pager.tsx'
import { useToast } from '../components/Toast.tsx'
import { formatDate, initials } from '../format.ts'
import { useQuery } from '../hooks/useQuery.ts'
import { useTitle } from '../hooks/useTitle.ts'

export function CustomersPage() {
  useTitle('Müşteriler')
  const navigate = useNavigate()
  const showToast = useToast()
  const [adding, setAdding] = useState(false)

  // Sayfa numarası adreste durur; geri tuşu aynı sayfaya döner.
  const [params, setParams] = useSearchParams()
  const page = readPage(params.get('page'))
  const customers = useQuery(`customers:${page}`, (signal) => listCustomers(page, signal))

  return (
    <main>
      <div className="hero">
        <div className="container hero-inner">
          <div className="hero-text">
            <h1>Müşteriler</h1>
            <p>
              {customers.data ? `${customers.data.totalCount} müşteri` : 'Yükleniyor…'}
            </p>
          </div>
          <button type="button" className="button button-accent" onClick={() => setAdding(true)}>
            <Icon name="plus" />
            Müşteri ekle
          </button>
        </div>
      </div>

      <div className="container overlap">
        <section className="panel" aria-label="Müşteri listesi">
          {customers.error && <LoadError error={customers.error} onRetry={customers.reload} />}
          {!customers.data && !customers.error && <SkeletonRows count={5} />}
          {customers.data?.items.length === 0 && (
            <Empty title={page === 1 ? 'Henüz müşteri yok' : 'Bu sayfada müşteri yok'}>
              <p>{page === 1 ? 'İlk müşteriyi ekleyerek başla.' : 'Önceki sayfalara göz at.'}</p>
            </Empty>
          )}
          {customers.data && customers.data.items.length > 0 && (
            <ul className="customer-list">
              {customers.data.items.map((customer) => (
                <li key={customer.id}>
                  <Link to={`/customers/${customer.id}`} className="customer-row">
                    <span className="avatar" aria-hidden="true">
                      {initials(customer.firstName, customer.lastName)}
                    </span>
                    <span className="customer-name">
                      <strong>
                        {customer.firstName} {customer.lastName}
                      </strong>
                      <span>{customer.email}</span>
                    </span>
                    <span className="customer-date">{formatDate(customer.createdAt)}</span>
                    <Icon name="right" />
                  </Link>
                </li>
              ))}
            </ul>
          )}
          {customers.data && (
            <Pager
              page={page}
              pageSize={CUSTOMERS_PAGE_SIZE}
              totalCount={customers.data.totalCount}
              onChange={(next) => setParams(next === 1 ? {} : { page: String(next) })}
            />
          )}
        </section>
      </div>

      {adding && (
        <NewCustomerDialog
          onClose={() => setAdding(false)}
          onUncertain={customers.reload}
          onCreated={(customer) => {
            setAdding(false)
            showToast('Müşteri oluşturuldu.')
            void navigate(`/customers/${customer.id}`)
          }}
        />
      )}
    </main>
  )
}

function readPage(value: string | null): number {
  const page = Number(value ?? 1)
  return Number.isInteger(page) && page >= 1 && page <= 1_000_000 ? page : 1
}
