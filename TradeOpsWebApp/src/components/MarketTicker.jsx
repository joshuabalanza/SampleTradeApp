import { useEffect, useState } from 'react';
import { SYMBOLS } from '../constants.js';

const BASE_PRICES = {
  EURUSD: 1.0850,
  GBPUSD: 1.2650,
  USDJPY: 150.20,
  AUDUSD: 0.6620,
  USDCAD: 1.3580,
  NZDUSD: 0.6050,
};

function randomWalk(price) {
  const drift = (Math.random() - 0.5) * price * 0.0006;
  return Math.max(0.0001, price + drift);
}

// Simulated market data — this project has no live price feed integration.
export default function MarketTicker() {
  const [prices, setPrices] = useState(BASE_PRICES);

  useEffect(() => {
    const interval = setInterval(() => {
      setPrices((prev) => {
        const next = { ...prev };
        for (const symbol of SYMBOLS) {
          next[symbol] = randomWalk(prev[symbol]);
        }
        return next;
      });
    }, 1800);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="ticker">
      {SYMBOLS.map((symbol) => {
        const price = prices[symbol];
        const up = price >= BASE_PRICES[symbol];
        return (
          <div className="ticker-item" key={symbol}>
            <span className="ticker-symbol">{symbol}</span>
            <span className={up ? 'ticker-up' : 'ticker-down'}>
              {price.toFixed(symbol.includes('JPY') ? 2 : 4)}
            </span>
          </div>
        );
      })}
    </div>
  );
}
