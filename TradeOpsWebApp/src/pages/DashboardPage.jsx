import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { tradesApi } from '../api/tradesApi.js';
import { ORDER_SIDE_LABEL, TRADE_STATUS_LABEL } from '../constants.js';
import StatusBadge from '../components/StatusBadge.jsx';
import { useAuth } from '../auth/AuthContext.jsx';

const STATUS_ORDER = [0, 1, 2, 3, 4];

export default function DashboardPage() {
  const { user } = useAuth();
  const [trades, setTrades] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const data = await tradesApi.list();
        if (!cancelled) {
          setTrades(data);
          setError('');
        }
      } catch (err) {
        if (!cancelled) setError(err.message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };
    load();
    const interval = setInterval(load, 4000);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  const counts = STATUS_ORDER.reduce((acc, status) => {
    acc[status] = trades.filter((t) => t.status === status).length;
    return acc;
  }, {});

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Welcome back, {user?.displayName?.split(' ')[0]}</h1>
          <p className="muted">Real-time view of trade ingestion, execution &amp; reconciliation.</p>
        </div>
        <Link to="/orders/new" className="btn-primary">+ New Order</Link>
      </div>

      {error && <div className="alert alert-error">{error} — showing mock/offline data.</div>}

      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-value">{trades.length}</div>
          <div className="stat-label">Total Trades</div>
        </div>
        {STATUS_ORDER.map((status) => (
          <div className="stat-card" key={status}>
            <div className="stat-value">{counts[status] ?? 0}</div>
            <div className="stat-label">{TRADE_STATUS_LABEL[status]}</div>
          </div>
        ))}
      </div>

      <div className="card">
        <h3>Recent Trades</h3>
        {loading ? (
          <p className="muted">Loading…</p>
        ) : trades.length === 0 ? (
          <p className="muted">No trades yet. Submit your first order to get started.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Account</th>
                <th>Symbol</th>
                <th>Side</th>
                <th>Qty</th>
                <th>Price</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {trades.slice(0, 6).map((t) => (
                <tr key={t.tradeId}>
                  <td>
                    <Link to={`/trades/${t.tradeId}`}>{t.accountId}</Link>
                  </td>
                  <td><strong>{t.symbol}</strong></td>
                  <td className={t.side === 0 ? 'side-buy' : 'side-sell'}>{ORDER_SIDE_LABEL[t.side]}</td>
                  <td>{Number(t.quantity).toLocaleString()}</td>
                  <td>{Number(t.price).toFixed(4)}</td>
                  <td><StatusBadge status={t.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
