import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { adminPassword, api, ApiError, formatDate, inviteUrl, type ExchangeDetail, type Participant } from '../api'

export default function ExchangePage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const [exchange, setExchange] = useState<ExchangeDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState(false)

  // Runs an API call, applying the returned exchange and surfacing errors.
  const run = useCallback(
    async (action: () => Promise<ExchangeDetail | void>) => {
      setError(null)
      try {
        const result = await action()
        if (result) setExchange(result)
        return true
      } catch (err) {
        if (err instanceof ApiError && err.status === 401) {
          adminPassword.clear()
          navigate('/admin')
        } else {
          setError((err as Error).message)
        }
        return false
      }
    },
    [navigate],
  )

  useEffect(() => {
    run(() => api.getExchange(id))
  }, [id, run])

  if (!exchange) {
    return (
      <>
        <Link to="/admin">← All exchanges</Link>
        {error ? <p className="error">{error}</p> : <p className="muted">Loading…</p>}
      </>
    )
  }

  const isDrawn = exchange.drawnAt !== null

  async function draw() {
    if (!confirm(`Draw names for ${exchange!.participants.length} people? Participants can't be changed afterwards unless you reset.`)) return
    await run(() => api.draw(id))
  }

  async function reset() {
    if (!confirm('Reset the draw? Everyone will get a new person once you draw again, so let them know if they have already looked.')) return
    await run(() => api.resetDraw(id))
  }

  async function remove() {
    if (!confirm(`Delete "${exchange!.name}" permanently?`)) return
    if (await run(() => api.deleteExchange(id))) navigate('/admin')
  }

  return (
    <>
      <Link to="/admin">← All exchanges</Link>

      {editing ? (
        <EditDetails exchange={exchange} onCancel={() => setEditing(false)} onSave={async (input) => {
          if (await run(() => api.updateExchange(id, input))) setEditing(false)
        }} />
      ) : (
        <section className="card">
          <header className="page-header">
            <h1>{exchange.name}</h1>
            <button className="secondary" onClick={() => setEditing(true)}>
              Edit
            </button>
          </header>
          <p className="muted">📅 {formatDate(exchange.date)}</p>
          {exchange.description && <p className="description">{exchange.description}</p>}
        </section>
      )}

      {error && <p className="error">{error}</p>}

      <section className="card">
        <header className="page-header">
          <h2>People ({exchange.participants.length})</h2>
          <span className={`badge ${isDrawn ? 'badge-done' : ''}`}>{isDrawn ? 'Names drawn' : 'Not drawn yet'}</span>
        </header>

        {isDrawn ? (
          <p className="muted small">
            Send each person their own link. Only they can see who they're buying for — this page never shows it.
          </p>
        ) : (
          <p className="muted small">Add everyone taking part, then draw names. Invite links work straight away and show results once names are drawn.</p>
        )}

        <ul className="list">
          {exchange.participants.map((p) => (
            <ParticipantRow
              key={p.id}
              participant={p}
              isDrawn={isDrawn}
              onRemove={() => run(() => api.removeParticipant(id, p.id))}
            />
          ))}
        </ul>

        {!isDrawn && <AddParticipant onAdd={(name) => run(() => api.addParticipant(id, name))} />}

        <div className="actions">
          {isDrawn ? (
            <button className="secondary" onClick={reset}>
              Reset draw
            </button>
          ) : (
            <button onClick={draw} disabled={exchange.participants.length < 2}>
              🎲 Draw names
            </button>
          )}
          <button className="danger" onClick={remove}>
            Delete exchange
          </button>
        </div>
      </section>
    </>
  )
}

function ParticipantRow({ participant, isDrawn, onRemove }: { participant: Participant; isDrawn: boolean; onRemove: () => void }) {
  const [copied, setCopied] = useState(false)
  const url = inviteUrl(participant.inviteToken)

  async function copy() {
    await navigator.clipboard.writeText(url)
    setCopied(true)
    setTimeout(() => setCopied(false), 1500)
  }

  return (
    <li className="participant">
      <div className="participant-main">
        <strong>{participant.name}</strong>
        {isDrawn && <span className="muted small">{participant.hasViewed ? ' ✓ has looked' : ' · not looked yet'}</span>}
        <div className="invite-link small">{url}</div>
      </div>
      <div className="participant-actions">
        <button className="secondary small-btn" onClick={copy}>
          {copied ? 'Copied!' : 'Copy link'}
        </button>
        {!isDrawn && (
          <button className="danger small-btn" onClick={onRemove} aria-label={`Remove ${participant.name}`}>
            ✕
          </button>
        )}
      </div>
    </li>
  )
}

function AddParticipant({ onAdd }: { onAdd: (name: string) => Promise<boolean> }) {
  const [name, setName] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (await onAdd(name)) setName('')
  }

  return (
    <form className="inline-form" onSubmit={submit}>
      <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Add a person" required />
      <button type="submit">Add</button>
    </form>
  )
}

function EditDetails({
  exchange,
  onSave,
  onCancel,
}: {
  exchange: ExchangeDetail
  onSave: (input: { name: string; date: string; description: string }) => void
  onCancel: () => void
}) {
  const [name, setName] = useState(exchange.name)
  const [date, setDate] = useState(exchange.date)
  const [description, setDescription] = useState(exchange.description)

  return (
    <form
      className="card"
      onSubmit={(e) => {
        e.preventDefault()
        onSave({ name, date, description })
      }}
    >
      <label>
        Name
        <input value={name} onChange={(e) => setName(e.target.value)} required />
      </label>
      <label>
        Date
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
      </label>
      <label>
        Description
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
      </label>
      <div className="actions">
        <button type="submit">Save</button>
        <button type="button" className="secondary" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  )
}
