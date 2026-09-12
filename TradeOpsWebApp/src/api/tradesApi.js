const API_BASE = `${import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5025/api'}/trades`;

async function handle(res) {
  if (!res.ok) {
    let message = `Request failed with status ${res.status}`;
    try {
      const body = await res.json();
      message = body.error || body.title || message;
    } catch {
      // response had no JSON body
    }
    throw new Error(message);
  }
  return res.status === 204 ? null : res.json();
}

export const tradesApi = {
  async list() {
    const res = await fetch(API_BASE);
    return handle(res);
  },

  async getById(tradeId) {
    const res = await fetch(`${API_BASE}/${tradeId}`);
    return handle(res);
  },

  async getAuditLogs(tradeId) {
    const res = await fetch(`${API_BASE}/${tradeId}/audit-logs`);
    return handle(res);
  },

  async ingest(payload) {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handle(res);
  },

  async reconcile(tradeId, brokerReport) {
    const res = await fetch(`${API_BASE}/${tradeId}/reconcile`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(brokerReport),
    });
    return handle(res);
  },
};
