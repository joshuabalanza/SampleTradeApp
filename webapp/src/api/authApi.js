const API_BASE = `${import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5025/api'}/auth`;

async function handle(res) {
  if (!res.ok) {
    let message = `Request failed with status ${res.status}`;
    try {
      const body = await res.json();
      message = body.error || body.message || message;
    } catch {
      // response had no JSON body
    }
    throw new Error(message);
  }
  return res.json();
}

export const authApi = {
  async register(payload) {
    const res = await fetch(`${API_BASE}/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handle(res);
  },

  async login(username, password) {
    const res = await fetch(`${API_BASE}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });
    return handle(res);
  },
};
