import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router'
import { Layout } from './components/Layout.tsx'
import { ToastProvider } from './components/Toast.tsx'
import { CustomerPage } from './pages/CustomerPage.tsx'
import { CustomersPage } from './pages/CustomersPage.tsx'
import { NotFound } from './pages/NotFound.tsx'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <ToastProvider>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<CustomersPage />} />
            <Route path="customers/:id" element={<CustomerPage />} />
            <Route path="*" element={<NotFound title="Sayfa bulunamadı" />} />
          </Route>
        </Routes>
      </ToastProvider>
    </BrowserRouter>
  </StrictMode>,
)
