import { TRADE_STATUS_LABEL } from '../constants.js';

export default function StatusBadge({ status }) {
  const label = TRADE_STATUS_LABEL[status] ?? 'Unknown';
  return <span className={`badge badge-${label}`}>{label}</span>;
}
