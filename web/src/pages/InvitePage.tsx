import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { api, ApiError, formatDate, type Invite } from '../api'

export default function InvitePage() {
  const { token = '' } = useParams()
  const [invite, setInvite] = useState<Invite | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [revealed, setRevealed] = useState(false)

  useEffect(() => {
    api
      .getInvite(token)
      .then(setInvite)
      .catch((err) =>
        setError(err instanceof ApiError && err.status === 404 ? "This invite link isn't valid. Check you copied all of it." : (err as Error).message),
      )
  }, [token])

  if (error) return <div className="card center"><p className="error">{error}</p></div>
  if (!invite) return <p className="muted center">Loading…</p>

  return (
    <div className="card center">
      <div className="hero-emoji">🎅</div>
      <p className="muted">Hi {invite.yourName}! You're part of</p>
      <h1>{invite.exchangeName}</h1>
      <p>📅 {formatDate(invite.date)}</p>
      {invite.description && <p className="description">{invite.description}</p>}
      <p className="muted small">{invite.participantCount} people taking part</p>

      <div className="reveal">
        {invite.recipientName === null ? (
          <p>Names haven't been drawn yet. Check back on this link soon!</p>
        ) : revealed ? (
          <>
            <p className="muted">You're buying a gift for</p>
            <p className="recipient">{invite.recipientName}</p>
            <p className="muted small">Shh… keep it a secret! 🤫</p>
          </>
        ) : (
          <button className="big" onClick={() => setRevealed(true)}>
            🎁 Reveal who I'm buying for
          </button>
        )}
      </div>
    </div>
  )
}
