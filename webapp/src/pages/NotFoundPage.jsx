import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="login-page">
      <div className="login-card" style={{ textAlign: 'center' }}>
        <h1>404</h1>
        <p className="muted">This page does not exist.</p>
        <Link className="btn-primary" to="/">Back to Dashboard</Link>
      </div>
    </div>
  );
}
