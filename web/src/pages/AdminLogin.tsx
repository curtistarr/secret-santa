import { useState, type FormEvent } from 'react'
import { adminPassword, api } from '../api'

export default function AdminLogin({ onLogin }: { onLogin: () => void }) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await api.login(password)
      adminPassword.set(password)
      onLogin()
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="card narrow" onSubmit={submit}>
      <h1>Organiser login</h1>
      <label>
        Admin password
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoFocus required />
      </label>
      {error && <p className="error">{error}</p>}
      <button type="submit" disabled={busy}>
        {busy ? 'Checking…' : 'Log in'}
      </button>
    </form>
  )
}
