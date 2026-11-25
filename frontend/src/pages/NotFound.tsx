import { Link } from 'react-router'
import { useTitle } from '../hooks/useTitle.ts'

export function NotFound({ title }: { title: string }) {
  useTitle(title)

  return (
    <main>
      <div className="hero">
        <div className="container hero-inner">
          <div className="hero-text">
            <h1>{title}</h1>
            <p>Adres hatalı olabilir ya da kayıt hiç oluşturulmamış olabilir.</p>
          </div>
          <Link to="/" className="button button-accent">
            Müşterilere dön
          </Link>
        </div>
      </div>
    </main>
  )
}
