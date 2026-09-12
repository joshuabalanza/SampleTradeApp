# TradeOps Sample Project
**By:** Joshua Kim C. Balanza  
**Stack:** .NET 9 (C#), PostgreSQL, React, System.Threading.Channels, xUnit

---

## 1. Project Overview & Architectural Highlights
In a trading environment, systems must guarantee **correctness, idempotency, strict auditability, and fault tolerance**.

This project provides an end-to-end solution:
1. **API Ingestion Layer (`TradeOps.Api`):**
   - Implements idempotent request handling via client-generated `IdempotencyKey`. Repeated network retries return the existing trade with `200 OK` without creating duplicate orders.
2. **High-Throughput In-Memory Pipeline (`TradeOps.Core`):**
   - Utilizes `System.Threading.Channels` with bounded capacity and backpressure (`BoundedChannelFullMode.Wait`).
   - `TradeExecutionWorker` consumes trades asynchronously without blocking API request threads.
3. **Reconciliation Engine:**
   - Compares internal executions against external broker execution reports.
   - Evaluates Symbol, Side, Quantity, and checks Price Drift against a **0.5% slippage tolerance**.
   - Automatically transitions state: within tolerance -> `Reconciled`; outside tolerance -> `Discrepancy` with detailed reason.
4. **PostgreSQL Production Schema (`sql/schema.sql`):**
   - Unique index on `idempotency_key`.
   - PL/pgSQL database trigger (`fn_audit_trade_status_change`) writing state transitions into an append-only `trade_audit_logs` table with `JSONB` payloads.
   - Composite indexing for fast back-office query filtering.
5. **Back-Office Operations Portal (`index.html`):**
   - React UI enabling Operations and Finance teams to monitor the live feed, inspect discrepancies, view audit trails, and perform manual reconciliation overrides.

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
dotnet run --project TradeOps.Api/TradeOps.Api.csproj
```

### Viewing the Back-Office React Dashboard
Open `index.html` directly in your browser:
```bash
open index.html
```
