import { Link } from 'react-router-dom'

export default function HomePage() {
  return (
    <div className="card center">
      <div className="hero-emoji">🎁</div>
      <h1>Secret Santa</h1>
      <p className="muted">
        Got an invite link? Open it to find out who you're buying for.
      </p>
      <p>
        <Link to="/admin">Organiser login</Link>
      </p>
    </div>
  )
}
