import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { tradesApi } from '../api/tradesApi.js';
import { ORDER_SIDE_LABEL, TRADE_STATUS_LABEL } from '../constants.js';
import StatusBadge from '../components/StatusBadge.jsx';

export default function BlotterPage() {
  const [trades, setTrades] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('all');
  const [search, setSearch] = useState('');

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

  const filtered = useMemo(() => {
    return trades.filter((t) => {
      if (statusFilter !== 'all' && String(t.status) !== statusFilter) return false;
      if (search) {
        const term = search.toLowerCase();
        return (
          t.symbol.toLowerCase().includes(term) ||
          t.accountId.toLowerCase().includes(term) ||
          t.idempotencyKey.toLowerCase().includes(term)
        );
      }
      return true;
    });
  }, [trades, statusFilter, search]);

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Trade Blotter</h1>
          <p className="muted">Full ingestion, execution and reconciliation feed.</p>
        </div>
        <Link to="/orders/new" className="btn-primary">+ New Order</Link>
      </div>

      <div className="card">
        <div className="toolbar">
          <input
            className="search-input"
            placeholder="Search by symbol, account, or order key…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="all">All statuses</option>
            {Object.entries(TRADE_STATUS_LABEL).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {loading ? (
          <p className="muted">Loading…</p>
        ) : filtered.length === 0 ? (
          <p className="muted">No trades match the current filters.</p>
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
                <th>Created (UTC)</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((t) => (
                <tr key={t.tradeId} className="row-clickable">
                  <td><Link to={`/trades/${t.tradeId}`}>{t.accountId}</Link></td>
                  <td><strong>{t.symbol}</strong></td>
                  <td className={t.side === 0 ? 'side-buy' : 'side-sell'}>{ORDER_SIDE_LABEL[t.side]}</td>
                  <td>{Number(t.quantity).toLocaleString()}</td>
                  <td>{Number(t.price).toFixed(4)}</td>
                  <td>
                    <StatusBadge status={t.status} />
                    {t.discrepancyReason && <div className="reason-text">{t.discrepancyReason}</div>}
                  </td>
                  <td className="muted">{new Date(t.createdAtUtc).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
