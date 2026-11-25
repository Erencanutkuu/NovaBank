import { Link, NavLink, Outlet } from 'react-router'
import { Star } from './Icon.tsx'

export function Layout() {
  return (
    <div className="shell">
      <header className="topbar">
        <div className="container topbar-inner">
          <Link to="/" className="brand">
            <Star />
            NovaBank
          </Link>
          <nav aria-label="Ana menü">
            <NavLink to="/" end className="nav-link">
              Müşteriler
            </NavLink>
          </nav>
        </div>
      </header>
      <Outlet />
      <footer className="container footer">NovaBank bir öğrenme projesidir; gerçek para ve gerçek IBAN içermez.</footer>
    </div>
  )
}
