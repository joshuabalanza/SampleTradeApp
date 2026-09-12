import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { tradesApi } from '../api/tradesApi.js';
import { ORDER_SIDE_LABEL } from '../constants.js';
import StatusBadge from '../components/StatusBadge.jsx';

export default function TradeDetailPage() {
  const { tradeId } = useParams();
  const [trade, setTrade] = useState(null);
  const [logs, setLogs] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const [report, setReport] = useState({ executedPrice: '', executionTimeUtc: '' });
  const [reconciling, setReconciling] = useState(false);
  const [reconcileResult, setReconcileResult] = useState(null);

  const load = async () => {
    try {
      const [tradeData, logData] = await Promise.all([
        tradesApi.getById(tradeId),
        tradesApi.getAuditLogs(tradeId),
      ]);
      setTrade(tradeData);
      setLogs(logData);
      setError('');
      setReport((r) => ({
        executedPrice: r.executedPrice || tradeData.price,
        executionTimeUtc: new Date().toISOString(),
      }));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    const interval = setInterval(load, 3000);
    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tradeId]);

  const handleReconcile = async (e) => {
    e.preventDefault();
    setReconciling(true);
    setReconcileResult(null);
    try {
      const result = await tradesApi.reconcile(tradeId, {
        externalTradeId: `BROKER-${Date.now()}`,
        symbol: trade.symbol,
        side: trade.side,
        quantity: trade.quantity,
        executedPrice: Number(report.executedPrice),
        executionTimeUtc: report.executionTimeUtc,
      });
      setReconcileResult(result);
      await load();
    } catch (err) {
      setError(err.message);
    } finally {
      setReconciling(false);
    }
  };

  if (loading) return <p className="muted">Loading trade…</p>;
  if (error && !trade) return <div className="alert alert-error">{error}</div>;
  if (!trade) return <p className="muted">Trade not found.</p>;

  const canReconcile = trade.status === 1 || trade.status === 3; // Executed or Discrepancy

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Trade {trade.tradeId.slice(0, 8)}</h1>
          <p className="muted"><Link to="/blotter">← Back to Blotter</Link></p>
        </div>
        <StatusBadge status={trade.status} />
      </div>

      <div className="grid-2">
        <div className="card">
          <h3>Trade Summary</h3>
          <dl className="preview-list">
            <dt>Account</dt><dd>{trade.accountId}</dd>
            <dt>Symbol</dt><dd><strong>{trade.symbol}</strong></dd>
            <dt>Side</dt><dd className={trade.side === 0 ? 'side-buy' : 'side-sell'}>{ORDER_SIDE_LABEL[trade.side]}</dd>
            <dt>Quantity</dt><dd>{Number(trade.quantity).toLocaleString()}</dd>
            <dt>Price</dt><dd>{Number(trade.price).toFixed(4)}</dd>
            <dt>Idempotency Key</dt><dd className="mono">{trade.idempotencyKey}</dd>
            <dt>Created (UTC)</dt><dd>{new Date(trade.createdAtUtc).toLocaleString()}</dd>
            {trade.discrepancyReason && (
              <>
                <dt>Discrepancy</dt>
                <dd className="reason-text">{trade.discrepancyReason}</dd>
              </>
            )}
          </dl>
        </div>

        <div className="card">
          <h3>Broker Reconciliation</h3>
          {!canReconcile ? (
            <p className="muted">
              {trade.status === 0
                ? 'Waiting for the execution engine to fill this order…'
                : 'This trade has already been reconciled.'}
            </p>
          ) : (
            <form onSubmit={handleReconcile}>
              <p className="muted">Simulate the broker execution report used to reconcile this trade (0.5% slippage tolerance).</p>
              <div className="form-group">
                <label>Broker Executed Price</label>
                <input
                  type="number"
                  step="0.0001"
                  value={report.executedPrice}
                  onChange={(e) => setReport({ ...report, executedPrice: e.target.value })}
                  required
                />
              </div>
              <button type="submit" className="btn-primary" disabled={reconciling}>
                {reconciling ? 'Reconciling…' : 'Submit Broker Report'}
              </button>
            </form>
          )}

          {error && <div className="alert alert-error">{error}</div>}
          {reconcileResult && (
            <div className={`alert ${reconcileResult.reconciled ? 'alert-success' : 'alert-error'}`}>
              {reconcileResult.reconciled ? 'Trade reconciled successfully.' : 'Discrepancy detected — see trade summary.'}
            </div>
          )}
        </div>
      </div>

      <div className="card">
        <h3>Audit Trail</h3>
        {logs.length === 0 ? (
          <p className="muted">No audit events yet.</p>
        ) : (
          <div className="audit-box">
            {logs.map((log) => (
              <div className="audit-entry" key={log.id}>
                <span className="muted">[{new Date(log.timestampUtc).toLocaleString()}]</span>{' '}
                <strong>{log.action}</strong>: {log.details}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
