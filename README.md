# TradeOps Sample Project

**By:** Joshua Kim C. Balanza  
**Stack:** .NET 9 (C#), EF Core, PostgreSQL, React (Vite), System.Threading.Channels, xUnit

---

## 1. Project Overview & Architectural Highlights

In a trading environment, systems must guarantee **correctness, idempotency, strict auditability, and fault tolerance**.

This project provides an end-to-end solution:

1. **API Ingestion Layer (`TradeOps.Api`):**
   - Implements idempotent request handling via client-generated `IdempotencyKey`. Repeated network retries return the existing trade with `200 OK` without creating duplicate orders.
   - Dependency injection wires `ITradeRepository` to EF Core (Postgres) when a connection string is configured, or an in-memory repository otherwise.
2. **Data Access Layer (`TradeOps.Core/Data`):**
   - `TradeOpsDbContext` (EF Core + Npgsql provider) maps to the existing `trades` / `trade_audit_logs` tables, including native Postgres enum types (`order_side`, `trade_status`).
3. **High-Throughput In-Memory Pipeline (`TradeOps.Core`):**
   - Utilizes `System.Threading.Channels` with bounded capacity and backpressure (`BoundedChannelFullMode.Wait`).
   - `TradeExecutionWorker` consumes trades asynchronously without blocking API request threads.
4. **Reconciliation Engine:**
   - Compares internal executions against external broker execution reports.
   - Evaluates Symbol, Side, Quantity, and checks Price Drift against a **0.5% slippage tolerance**.
   - Automatically transitions state: within tolerance -> `Reconciled`; outside tolerance -> `Discrepancy` with detailed reason.
5. **PostgreSQL Production Schema (`sql/schema.sql`):**
   - Unique index on `idempotency_key`.
   - PL/pgSQL database trigger (`fn_audit_trade_status_change`) writing state transitions into an append-only `trade_audit_logs` table with `JSONB` payloads.
   - Composite indexing for fast back-office query filtering.
6. **TradeOps Terminal (`webapp/`):**
   - Full React (Vite) single-page app simulating a trading desk: mock login, live dashboard, trade blotter with filters, an order ticket, and a trade detail view with audit trail + manual broker reconciliation.

---

## 2. Quickstart Instructions on macOS (VS Code)

### Running Automated Unit Tests

Open VS Code terminal (`Cmd + ` `) and run:

```bash
dotnet test
```

**Test Coverage:**

- Idempotency replay verification (prevents duplicate trades).
- Slippage discrepancy detection (> 0.5% drift).
- Healthy trade reconciliation matching broker reports.

### Running the API

```bash
docker compose up -d postgres   # starts PostgreSQL with sql/schema.sql applied
dotnet run --project TradeOps.Api/TradeOps.Api.csproj
```

By default the API listens on `http://localhost:5025` (see `TradeOps.Api/Properties/launchSettings.json`).

### Running the TradeOps Terminal (React front-end)

The full trading UI lives in `webapp/` (Vite + React + React Router).

```bash
cd webapp
npm install
npm run dev
```

Open the printed local URL (typically `http://localhost:5173`). Sign in with one of the demo accounts shown on the login screen (e.g. `trader1` / `demo123`).

The app calls the API at the URL configured in `webapp/.env` (`VITE_API_BASE_URL`, defaults to `http://localhost:5025/api`) — copy `webapp/.env.example` if you need to override it. CORS is already open (`AllowAnyOrigin`) on the API for local development.

**Flow covered end-to-end:** Login → Dashboard (live stats & market ticker) → New Order ticket (idempotent submission) → automatic background execution → Trade Detail (audit trail) → manual broker reconciliation.

The legacy single-file prototype (`index.html`, CDN React + Babel) is still available for a quick, dependency-free demo of the same API.
