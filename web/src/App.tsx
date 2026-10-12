import { Route, Routes } from 'react-router-dom'
import AdminPage from './pages/AdminPage'
import ExchangePage from './pages/ExchangePage'
import HomePage from './pages/HomePage'
import InvitePage from './pages/InvitePage'

export default function App() {
  return (
    <main className="container">
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/admin" element={<AdminPage />} />
        <Route path="/admin/exchanges/:id" element={<ExchangePage />} />
        <Route path="/invite/:token" element={<InvitePage />} />
        <Route path="*" element={<HomePage />} />
      </Routes>
    </main>
  )
}
