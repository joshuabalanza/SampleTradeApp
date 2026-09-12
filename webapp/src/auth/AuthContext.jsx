import { createContext, useContext, useEffect, useMemo, useState } from 'react';

const SESSION_KEY = 'tradeops.session';

// Mock credential store for the demo — no real backend authentication exists.
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
    login(username, password) {
      const match = DEMO_USERS.find(
        (u) => u.username === username.trim().toLowerCase() && u.password === password
      );
      if (!match) {
        throw new Error('Invalid username or password.');
      }
      const { password: _password, ...publicUser } = match;
      setUser(publicUser);
      return publicUser;
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
