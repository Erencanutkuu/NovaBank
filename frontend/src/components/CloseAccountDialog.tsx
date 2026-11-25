import { closeAccount } from '../api/novabank.ts'
import type { Account } from '../api/types.ts'
import { formatIban, formatMoney } from '../format.ts'
import { useSubmit } from '../hooks/useSubmit.ts'
import { Dialog } from './Dialog.tsx'
import { FormError, Uncertain } from './Feedback.tsx'

interface Props {
  account: Account
  onClose: () => void
  onClosed: () => void
  onFailed: () => void
  onWithdraw: () => void
}

export function CloseAccountDialog({ account, onClose, onClosed, onFailed, onWithdraw }: Props) {
  const { busy, error, submit } = useSubmit()

  function close() {
    void submit(async () => {
      await closeAccount(account.id)
      onClosed()
    }, onFailed)
  }

  function renderBody() {
    if (error?.uncertain) {
      return <Uncertain action="Hesabı kontrol et" onClose={onClose} />
    }

    // Asıl kontrol sunucudadır; burada yalnızca sonucu belli isteği göndermeden yol gösterilir.
    if (account.balance > 0) {
      return (
        <>
          <FormError>
            Hesapta {formatMoney(account.balance, account.currency)} var. Kapatmak için önce bakiyeyi sıfırla.
          </FormError>
          <div className="dialog-actions">
            <button type="button" className="button button-quiet" onClick={onClose}>
              Vazgeç
            </button>
            <button type="button" className="button button-primary" onClick={onWithdraw} autoFocus data-autofocus>
              Para çek
            </button>
          </div>
        </>
      )
    }

    if (error) {
      return (
        <>
          <FormError>{error.message}</FormError>
          <div className="dialog-actions">
            <button type="button" className="button button-primary" onClick={onClose} autoFocus>
              Tamam
            </button>
          </div>
        </>
      )
    }

    return (
      <>
        <p className="dialog-text">
          <strong>{formatIban(account.iban)}</strong> numaralı {account.currency} hesabı kapatılacak. Bu işlem geri
          alınamaz; hareketler görünmeye devam eder.
        </p>
        <div className="dialog-actions">
          <button type="button" className="button button-quiet" disabled={busy} onClick={onClose} data-autofocus>
            Vazgeç
          </button>
          <button type="button" className="button button-danger" disabled={busy} onClick={close}>
            Hesabı kapat
          </button>
        </div>
      </>
    )
  }

  return (
    <Dialog title="Hesabı kapat" busy={busy} onClose={onClose}>
      {renderBody()}
    </Dialog>
  )
}
