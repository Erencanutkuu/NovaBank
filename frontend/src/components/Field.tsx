import { useId, type InputHTMLAttributes, type ReactNode, type Ref } from 'react'

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
  hint?: ReactNode
  suffix?: string
  inputRef?: Ref<HTMLInputElement>
}

// Etiket, hata ve ipucu ekran okuyucu için de alana bağlıdır.
export function Field({ label, error, hint, suffix, inputRef, className, ...input }: Props) {
  const id = useId()
  const describedBy = [hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ')

  return (
    <div className={`field ${className ?? ''}`}>
      <label htmlFor={id}>{label}</label>
      <div className={`field-box ${error ? 'is-invalid' : ''}`}>
        <input
          {...input}
          ref={inputRef}
          id={id}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy || undefined}
        />
        {suffix && (
          <span className="field-suffix" aria-hidden="true">
            {suffix}
          </span>
        )}
      </div>
      {hint && (
        <p id={`${id}-hint`} className="field-hint">
          {hint}
        </p>
      )}
      {error && (
        <p id={`${id}-error`} className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}
