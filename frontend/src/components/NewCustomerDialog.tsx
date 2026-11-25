import { useState } from 'react'
import { createCustomer } from '../api/novabank.ts'
import type { Customer } from '../api/types.ts'
import { useSubmit } from '../hooks/useSubmit.ts'
import { Dialog } from './Dialog.tsx'
import { FormError, Uncertain } from './Feedback.tsx'
import { Field } from './Field.tsx'

interface Props {
  onClose: () => void
  onCreated: (customer: Customer) => void
  onUncertain: () => void
}

export function NewCustomerDialog({ onClose, onCreated, onUncertain }: Props) {
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '' })
  const { busy, error, submit } = useSubmit()
  const hasFieldErrors = error !== null && Object.keys(error.fields).length > 0

  const bind = (name: keyof typeof form) => ({
    value: form[name],
    error: error?.fields[name]?.[0],
    disabled: busy,
    autoComplete: 'off',
    onChange: (event: { target: { value: string } }) => setForm({ ...form, [name]: event.target.value }),
  })

  return (
    <Dialog title="Yeni müşteri" busy={busy} onClose={onClose}>
      {error?.uncertain ? (
        <Uncertain action="Listeyi kontrol et" onClose={onClose} />
      ) : (
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault()
            void submit(async () => onCreated(await createCustomer(form)), onUncertain)
          }}
        >
          {error && !hasFieldErrors && <FormError>{error.message}</FormError>}
          <div className="field-pair">
            <Field label="Ad" data-autofocus {...bind('firstName')} />
            <Field label="Soyad" {...bind('lastName')} />
          </div>
          <Field label="E-posta" type="email" {...bind('email')} />
          <div className="dialog-actions">
            <button type="button" className="button button-quiet" disabled={busy} onClick={onClose}>
              Vazgeç
            </button>
            <button type="submit" className="button button-primary" disabled={busy}>
              Müşteri ekle
            </button>
          </div>
        </form>
      )}
    </Dialog>
  )
}
