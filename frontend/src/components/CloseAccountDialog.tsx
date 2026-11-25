import { closeAccount } from '../api/novabank.ts'
import type { Account } from '../api/types.ts'
import { formatIban } from '../format.ts'
import { useSubmit } from '../hooks/useSubmit.ts'
import { Dialog } from './Dialog.tsx'
import { FormError, Uncertain } from './Feedback.tsx'

interface Props {
  account: Account
  onClose: () => void
  onClosed: () => void
  onFailed: () => void
}

export function CloseAccountDialog({ account, onClose, onClosed, onFailed }: Props) {
  const { busy, error, submit } = useSubmit()

  return (
    <Dialog title="Hesabı kapat" busy={busy} onClose={onClose}>
      {error?.uncertain ? (
        <Uncertain action="Hesabı kontrol et" onClose={onClose} />
      ) : (
        <>
          {error && <FormError>{error.message}</FormError>}
          <p className="dialog-text">
            <strong>{formatIban(account.iban)}</strong> numaralı {account.currency} hesabı kapatılacak. Bu işlem geri
            alınamaz; hareketler görünmeye devam eder.
          </p>
          <div className="dialog-actions">
            <button type="button" className="button button-quiet" disabled={busy} onClick={onClose} data-autofocus>
              Vazgeç
            </button>
            <button
              type="button"
              className="button button-danger"
              disabled={busy}
              onClick={() =>
                void submit(async () => {
                  await closeAccount(account.id)
                  onClosed()
                }, onFailed)
              }
            >
              Hesabı kapat
            </button>
          </div>
        </>
      )}
    </Dialog>
  )
}
