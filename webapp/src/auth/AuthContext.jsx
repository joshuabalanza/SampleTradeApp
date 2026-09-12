import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { authApi } from '../api/authApi.js';

const SESSION_KEY = 'tradeops.session';

// Pre-seeded demo credentials for quick-fill testing
export const DEMO_USERS = [
  {
    username: 'trader1',
    password: 'demo123',
    displayName: 'Joshua Kim Balanza',
    role: 'Trader',
    accountId: 'ACC-JOSHUA-101',
  },
  {
    username: 'ops1',
    password: 'demo123',
    displayName: 'Back-Office Operations',
    role: 'Operations',
    accountId: 'ACC-OPS-001',
  },
];

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const stored = sessionStorage.getItem(SESSION_KEY);
    return stored ? JSON.parse(stored) : null;
  });

  useEffect(() => {
    if (user) {
      sessionStorage.setItem(SESSION_KEY, JSON.stringify(user));
    } else {
      sessionStorage.removeItem(SESSION_KEY);
    }
  }, [user]);

  const value = useMemo(() => ({
    user,
    async login(username, password) {
      try {
        const response = await authApi.login(username, password);
        setUser(response.user);
        return response.user;
      } catch (err) {
        // Fallback to demo users if network/offline
        if (err.message?.includes('Failed to fetch') || err.message?.includes('NetworkError')) {
          const match = DEMO_USERS.find(
            (u) => u.username === username.trim().toLowerCase() && u.password === password
          );
          if (match) {
            const { password: _password, ...publicUser } = match;
            setUser(publicUser);
            return publicUser;
          }
        }
        throw err;
      }
    },
    async register(payload) {
      try {
        const response = await authApi.register(payload);
        setUser(response.user);
        return response.user;
      } catch (err) {
        // Fallback to local session if network/offline
        if (err.message?.includes('Failed to fetch') || err.message?.includes('NetworkError')) {
          const newUser = {
            userId: crypto.randomUUID(),
            username: payload.username,
            displayName: payload.displayName || payload.username,
            role: payload.role || 'Trader',
            accountId: payload.accountId || `ACC-${payload.username.toUpperCase()}`,
            createdAtUtc: new Date().toISOString(),
          };
          setUser(newUser);
          return newUser;
        }
        throw err;
      }
    },
    logout() {
      setUser(null);
    },
  }), [user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
