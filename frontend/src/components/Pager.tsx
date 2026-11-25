import { Icon } from './Icon.tsx'

interface Props {
  page: number
  pageSize: number
  totalCount: number
  onChange: (page: number) => void
}

export function Pager({ page, pageSize, totalCount, onChange }: Props) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (pages === 1 && page === 1) return null

  return (
    <nav className="pager" aria-label="Sayfalar">
      <button type="button" className="icon-button" aria-label="Önceki sayfa" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        <Icon name="left" />
      </button>
      <span>
        {page} / {pages}
      </span>
      <button type="button" className="icon-button" aria-label="Sonraki sayfa" disabled={page >= pages} onClick={() => onChange(page + 1)}>
        <Icon name="right" />
      </button>
    </nav>
  )
}
