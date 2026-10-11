import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { adminPassword, api, ApiError, formatDate, type ExchangeSummary } from '../api'
import AdminLogin from './AdminLogin'

export default function AdminPage() {
  const [loggedIn, setLoggedIn] = useState(() => adminPassword.get() !== null)
  const [exchanges, setExchanges] = useState<ExchangeSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setExchanges(await api.listExchanges())
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        adminPassword.clear()
        setLoggedIn(false)
      } else {
        setError((err as Error).message)
      }
    }
  }, [])

  useEffect(() => {
    if (loggedIn) load()
  }, [loggedIn, load])

  if (!loggedIn) return <AdminLogin onLogin={() => setLoggedIn(true)} />

  function logout() {
    adminPassword.clear()
    setLoggedIn(false)
    setExchanges(null)
  }

  return (
    <>
      <header className="page-header">
        <h1>🎄 Gift exchanges</h1>
        <button className="secondary" onClick={logout}>
          Log out
        </button>
      </header>

      {error && <p className="error">{error}</p>}

      <section className="card">
        <h2>Your exchanges</h2>
        {exchanges === null ? (
          <p className="muted">Loading…</p>
        ) : exchanges.length === 0 ? (
          <p className="muted">No exchanges yet. Create one below.</p>
        ) : (
          <ul className="list">
            {exchanges.map((x) => (
              <li key={x.id}>
                <Link to={`/admin/exchanges/${x.id}`} className="list-link">
                  <span>
                    <strong>{x.name}</strong>
                    <br />
                    <span className="muted small">
                      {formatDate(x.date)} · {x.participantCount} people
                    </span>
                  </span>
                  <span className={`badge ${x.isDrawn ? 'badge-done' : ''}`}>{x.isDrawn ? 'Drawn' : 'Not drawn'}</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>

      <CreateExchangeForm />
    </>
  )
}

function CreateExchangeForm() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [date, setDate] = useState('')
  const [description, setDescription] = useState('')
  const [people, setPeople] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const participants = people
        .split('\n')
        .map((p) => p.trim())
        .filter(Boolean)
      const created = await api.createExchange({ name, date, description, participants })
      navigate(`/admin/exchanges/${created.id}`)
    } catch (err) {
      setError((err as Error).message)
      setBusy(false)
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>New exchange</h2>
      <label>
        Name
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Family Christmas 2026" required />
      </label>
      <label>
        Date
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
      </label>
      <label>
        Description
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={3}
          placeholder="£30 budget. Gifts opened after Christmas lunch!"
        />
      </label>
      <label>
        People (one per line)
        <textarea value={people} onChange={(e) => setPeople(e.target.value)} rows={6} placeholder={'Alice\nBob\nCharlie'} />
      </label>
      {error && <p className="error">{error}</p>}
      <button type="submit" disabled={busy}>
        {busy ? 'Creating…' : 'Create exchange'}
      </button>
    </form>
  )
}
