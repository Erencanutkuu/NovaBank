// Backend'in JSON gövdeleri.

export type Currency = 'TRY' | 'USD' | 'EUR'

export interface Customer {
  id: string
  firstName: string
  lastName: string
  email: string
  createdAt: string
}

export interface Account {
  id: string
  customerId: string
  iban: string
  currency: Currency
  balance: number
  status: 'Open' | 'Closed'
  openedAt: string
  closedAt: string | null
}

export interface Transaction {
  id: string
  accountId: string
  type: 'Deposit' | 'Withdrawal'
  amount: number
  balanceAfter: number
  createdAt: string
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}
