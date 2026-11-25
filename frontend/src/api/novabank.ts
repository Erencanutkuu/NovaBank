import { request } from './client.ts'
import type { Account, Currency, Customer, Paged, Transaction } from './types.ts'

export const CUSTOMERS_PAGE_SIZE = 20
export const TRANSACTIONS_PAGE_SIZE = 10

export const listCustomers = (page: number, signal?: AbortSignal) =>
  request<Paged<Customer>>(`/customers?page=${page}&pageSize=${CUSTOMERS_PAGE_SIZE}`, { signal })

export const getCustomer = (id: string, signal?: AbortSignal) => request<Customer>(`/customers/${id}`, { signal })

export const createCustomer = (body: { firstName: string; lastName: string; email: string }) =>
  request<Customer>('/customers', { method: 'POST', body })

export const listAccounts = (customerId: string, signal?: AbortSignal) =>
  request<Account[]>(`/customers/${customerId}/accounts`, { signal })

export const openAccount = (customerId: string, currency: Currency) =>
  request<Account>(`/customers/${customerId}/accounts`, { method: 'POST', body: { currency } })

export const closeAccount = (id: string) => request<Account>(`/accounts/${id}/close`, { method: 'POST' })

export const deposit = (id: string, amount: number) =>
  request<Transaction>(`/accounts/${id}/deposit`, { method: 'POST', body: { amount } })

export const withdraw = (id: string, amount: number) =>
  request<Transaction>(`/accounts/${id}/withdraw`, { method: 'POST', body: { amount } })

export const listTransactions = (accountId: string, page: number, signal?: AbortSignal) =>
  request<Paged<Transaction>>(
    `/accounts/${accountId}/transactions?page=${page}&pageSize=${TRANSACTIONS_PAGE_SIZE}`,
    { signal },
  )
