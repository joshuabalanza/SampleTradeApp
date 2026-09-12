import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth, DEMO_USERS } from '../auth/AuthContext.jsx';

export default function LoginPage() {
  const { login, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const redirectTo = location.state?.from?.pathname ?? '/';

  const [mode, setMode] = useState('login'); // 'login' | 'register'

  // Login form state
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  // Register form state
  const [regUsername, setRegUsername] = useState('');
  const [regPassword, setRegPassword] = useState('');
  const [regDisplayName, setRegDisplayName] = useState('');
  const [regRole, setRegRole] = useState('Trader');
  const [regAccountId, setRegAccountId] = useState('');

  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      await login(username, password);
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError(err.message || 'Login failed.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      await register({
        username: regUsername,
        password: regPassword,
        displayName: regDisplayName,
        role: regRole,
        accountId: regAccountId,
      });
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError(err.message || 'Registration failed.');
    } finally {
      setSubmitting(false);
    }
  };

  const quickLogin = (demoUser) => {
    setMode('login');
    setUsername(demoUser.username);
    setPassword(demoUser.password);
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="brand" style={{ marginBottom: 20 }}>
          <span className="brand-mark">BM</span>
          <div>
            <div className="brand-name">TradeOps Markets</div>
            <div className="brand-sub">TradeOps Terminal</div>
          </div>
        </div>

        <div className="auth-tabs" style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
          <button
            type="button"
            className={`btn-tab ${mode === 'login' ? 'active' : ''}`}
            onClick={() => { setMode('login'); setError(''); }}
            style={{
              flex: 1,
              padding: '8px',
              borderRadius: '6px',
              border: '1px solid var(--border)',
              background: mode === 'login' ? '#1c2b3f' : 'transparent',
              color: mode === 'login' ? 'var(--accent)' : 'var(--text-muted)',
              fontWeight: 600,
            }}
          >
            Sign In
          </button>
          <button
            type="button"
            className={`btn-tab ${mode === 'register' ? 'active' : ''}`}
            onClick={() => { setMode('register'); setError(''); }}
            style={{
              flex: 1,
              padding: '8px',
              borderRadius: '6px',
              border: '1px solid var(--border)',
              background: mode === 'register' ? '#1c2b3f' : 'transparent',
              color: mode === 'register' ? 'var(--accent)' : 'var(--text-muted)',
              fontWeight: 600,
            }}
          >
            Register
          </button>
        </div>

        {mode === 'login' ? (
          <>
            <h1 className="login-title">Sign in</h1>
            <p className="login-subtitle">Access the trade ingestion, execution &amp; reconciliation desk.</p>

            <form onSubmit={handleLogin}>
              <div className="form-group">
                <label htmlFor="username">Username</label>
                <input
                  id="username"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="trader1"
                  autoFocus
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="password">Password</label>
                <input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  required
                />
              </div>

              {error && <div className="alert alert-error">{error}</div>}

              <button type="submit" className="btn-primary" style={{ width: '100%' }} disabled={submitting}>
                {submitting ? 'Signing in…' : 'Sign in'}
              </button>
            </form>

            <div className="demo-accounts">
              <span>Demo accounts:</span>
              {DEMO_USERS.map((u) => (
                <button type="button" key={u.username} className="btn-chip" onClick={() => quickLogin(u)}>
                  {u.username} ({u.role})
                </button>
              ))}
            </div>
          </>
        ) : (
          <>
            <h1 className="login-title">Create Account</h1>
            <p className="login-subtitle">Register a new user account in PostgreSQL database.</p>

            <form onSubmit={handleRegister}>
              <div className="form-group">
                <label htmlFor="regUsername">Username *</label>
                <input
                  id="regUsername"
                  value={regUsername}
                  onChange={(e) => setRegUsername(e.target.value)}
                  placeholder="e.g. joshua"
                  autoFocus
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="regPassword">Password * (min 6 chars)</label>
                <input
                  id="regPassword"
                  type="password"
                  value={regPassword}
                  onChange={(e) => setRegPassword(e.target.value)}
                  placeholder="••••••••"
                  minLength={6}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="regDisplayName">Full Name / Display Name</label>
                <input
                  id="regDisplayName"
                  value={regDisplayName}
                  onChange={(e) => setRegDisplayName(e.target.value)}
                  placeholder="e.g. Joshua Balanza"
                />
              </div>
              <div className="form-group">
                <label htmlFor="regRole">Role</label>
                <select id="regRole" value={regRole} onChange={(e) => setRegRole(e.target.value)}>
                  <option value="Trader">Trader</option>
                  <option value="Operations">Operations</option>
                </select>
              </div>
              <div className="form-group">
                <label htmlFor="regAccountId">Account ID (Optional)</label>
                <input
                  id="regAccountId"
                  value={regAccountId}
                  onChange={(e) => setRegAccountId(e.target.value)}
                  placeholder="e.g. ACC-JOSHUA-202"
                />
              </div>

              {error && <div className="alert alert-error">{error}</div>}

              <button type="submit" className="btn-primary" style={{ width: '100%' }} disabled={submitting}>
                {submitting ? 'Creating account…' : 'Register Account'}
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  );
}
