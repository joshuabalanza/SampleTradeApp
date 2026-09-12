import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { useAuth } from '../auth/AuthContext.jsx';
import { tradesApi } from '../api/tradesApi.js';
import MarketTicker from './MarketTicker.jsx';

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [apiOnline, setApiOnline] = useState(null);

  useEffect(() => {
    let cancelled = false;
    const checkHealth = async () => {
      try {
        await tradesApi.list();
        if (!cancelled) setApiOnline(true);
      } catch {
        if (!cancelled) setApiOnline(false);
      }
    };
    checkHealth();
    const interval = setInterval(checkHealth, 5000);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">BM</span>
          <div>
            <div className="brand-name">Blueberry Markets</div>
            <div className="brand-sub">TradeOps Terminal</div>
          </div>
        </div>
        <nav className="nav">
          <NavLink to="/" end className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
            Dashboard
          </NavLink>
          <NavLink to="/blotter" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
            Trade Blotter
          </NavLink>
          <NavLink to="/orders/new" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
            New Order
          </NavLink>
        </nav>
        <div className="sidebar-footer">
          <span className={`dot ${apiOnline ? 'dot-online' : 'dot-offline'}`} />
          {apiOnline === null ? 'Checking API…' : apiOnline ? 'API Connected' : 'API Offline (mock mode)'}
        </div>
      </aside>

      <div className="main-area">
        <header className="topbar">
          <MarketTicker />
          <div className="user-menu">
            <div className="user-info">
              <div className="user-name">{user?.displayName}</div>
              <div className="user-role">{user?.role} · {user?.accountId}</div>
            </div>
            <button type="button" className="btn-ghost" onClick={handleLogout}>
              Log out
            </button>
          </div>
        </header>

        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
