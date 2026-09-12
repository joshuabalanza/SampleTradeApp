import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { tradesApi } from '../api/tradesApi.js';
import { SYMBOLS } from '../constants.js';
import { useAuth } from '../auth/AuthContext.jsx';

export default function NewOrderPage() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({
    accountId: user?.accountId ?? '',
    symbol: 'EURUSD',
    side: 'Buy',
    quantity: 100000,
    price: 1.0850,
  });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [confirmation, setConfirmation] = useState(null);

  const updateField = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setConfirmation(null);

    if (Number(form.quantity) <= 0 || Number(form.price) <= 0) {
      setError('Quantity and Price must be strictly positive.');
      return;
    }

    const idempotencyKey = `ORD-${Date.now()}-${crypto.randomUUID().slice(0, 8)}-${form.symbol}`;
    const payload = {
      idempotencyKey,
      accountId: form.accountId,
      symbol: form.symbol,
      side: form.side === 'Buy' ? 0 : 1,
      quantity: Number(form.quantity),
      price: Number(form.price),
    };

    setSubmitting(true);
    try {
      const trade = await tradesApi.ingest(payload);
      setConfirmation(trade);
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>New Order Ticket</h1>
          <p className="muted">Submitted orders are idempotent, queued, and auto-executed by the trade engine.</p>
        </div>
      </div>

      <div className="grid-2">
        <div className="card">
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Account ID</label>
              <input value={form.accountId} onChange={updateField('accountId')} required />
            </div>

            <div className="form-group">
              <label>Symbol</label>
              <select value={form.symbol} onChange={updateField('symbol')}>
                {SYMBOLS.map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>

            <div className="form-group">
              <label>Side</label>
              <div className="side-toggle">
                <button
                  type="button"
                  className={`side-btn side-btn-buy ${form.side === 'Buy' ? 'active' : ''}`}
                  onClick={() => setForm({ ...form, side: 'Buy' })}
                >
                  Buy
                </button>
                <button
                  type="button"
                  className={`side-btn side-btn-sell ${form.side === 'Sell' ? 'active' : ''}`}
                  onClick={() => setForm({ ...form, side: 'Sell' })}
                >
                  Sell
                </button>
              </div>
            </div>

            <div className="form-group">
              <label>Quantity</label>
              <input type="number" min="0" step="1" value={form.quantity} onChange={updateField('quantity')} required />
            </div>

            <div className="form-group">
              <label>Price</label>
              <input type="number" min="0" step="0.0001" value={form.price} onChange={updateField('price')} required />
            </div>

            {error && <div className="alert alert-error">{error}</div>}

            <button type="submit" className="btn-primary" style={{ width: '100%' }} disabled={submitting}>
              {submitting ? 'Submitting…' : `Submit ${form.side} Order`}
            </button>
          </form>
        </div>

        <div className="card">
          <h3>Order Preview</h3>
          <dl className="preview-list">
            <dt>Account</dt><dd>{form.accountId || '—'}</dd>
            <dt>Symbol</dt><dd>{form.symbol}</dd>
            <dt>Side</dt><dd className={form.side === 'Buy' ? 'side-buy' : 'side-sell'}>{form.side}</dd>
            <dt>Quantity</dt><dd>{Number(form.quantity || 0).toLocaleString()}</dd>
            <dt>Price</dt><dd>{Number(form.price || 0).toFixed(4)}</dd>
            <dt>Notional</dt><dd>{(Number(form.quantity || 0) * Number(form.price || 0)).toLocaleString(undefined, { maximumFractionDigits: 2 })}</dd>
          </dl>

          {confirmation && (
            <div className="alert alert-success">
              <p><strong>Order accepted.</strong> Trade ID: {confirmation.tradeId}</p>
              <button type="button" className="btn-ghost" onClick={() => navigate(`/trades/${confirmation.tradeId}`)}>
                View trade →
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
