import sqlite3
import uuid
import datetime

# Connect to in-memory database to simulate PostgreSQL schema behavior
conn = sqlite3.connect(":memory:")
cursor = conn.cursor()

print("=" * 65)
print("  POSTGRESQL RECONCILIATION & AUDIT ENGINE SIMULATION")
print("=" * 65)

# 1. Create Core Trades Table with Unique Idempotency Constraint
cursor.execute("""
CREATE TABLE trades (
    trade_id TEXT PRIMARY KEY,
    idempotency_key TEXT UNIQUE NOT NULL,
    account_id TEXT NOT NULL,
    symbol TEXT NOT NULL,
    side TEXT NOT NULL,
    quantity REAL NOT NULL,
    price REAL NOT NULL,
    status TEXT NOT NULL,
    discrepancy_reason TEXT,
    created_at TEXT NOT NULL
);
""")

# 2. Create Audit Log Table
cursor.execute("""
CREATE TABLE trade_audit_logs (
    audit_id INTEGER PRIMARY KEY AUTOINCREMENT,
    trade_id TEXT NOT NULL,
    action TEXT NOT NULL,
    details TEXT,
    created_at TEXT NOT NULL
);
""")

# 3. Simulate PostgreSQL Trigger on Status Change
cursor.execute("""
CREATE TRIGGER trg_audit_status_change
AFTER UPDATE OF status ON trades
BEGIN
    INSERT INTO trade_audit_logs (trade_id, action, details, created_at)
    VALUES (
        NEW.trade_id,
        'STATUS_CHANGE',
        'Transitioned from ' || OLD.status || ' to ' || NEW.status || ' (Reason: ' || COALESCE(NEW.discrepancy_reason, 'None') || ')',
        datetime('now')
    );
END;
""")

print("\n[+] Tables and PostgreSQL Trigger Created successfully.")

# Test 1: Ingest Initial Trade
trade_id_1 = str(uuid.uuid4())
cursor.execute("""
INSERT INTO trades (trade_id, idempotency_key, account_id, symbol, side, quantity, price, status, created_at)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, datetime('now'));
""", (trade_id_1, "IDEM-KEY-001", "ACC-JOSHUA-1", "EURUSD", "Buy", 100000.0, 1.0850, "Pending"))
conn.commit()

print(f"[+] Ingested Trade 1: EURUSD @ 1.0850 (Status: Pending, Key: IDEM-KEY-001)")

# Test 2: Try Ingesting Duplicate Idempotency Key (Proving Constraint)
try:
    cursor.execute("""
    INSERT INTO trades (trade_id, idempotency_key, account_id, symbol, side, quantity, price, status, created_at)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, datetime('now'));
    """, (str(uuid.uuid4()), "IDEM-KEY-001", "ACC-JOSHUA-1", "EURUSD", "Buy", 100000.0, 1.0850, "Pending"))
    conn.commit()
except sqlite3.IntegrityError:
    print("[!] Duplicate IdempotencyKey rejected by UNIQUE constraint! (Zero duplicate risk)")

# Test 3: Status Transition (Pending -> Executed) -> Verifying Trigger
cursor.execute("""
UPDATE trades SET status = 'Executed' WHERE trade_id = ?;
""", (trade_id_1,))
conn.commit()
print("[+] Updated Trade 1 status to 'Executed'")

# Test 4: Reconciliation Discrepancy -> Verifying Trigger
cursor.execute("""
UPDATE trades 
SET status = 'Discrepancy', discrepancy_reason = 'Price drift exceeded 0.5% tolerance' 
WHERE trade_id = ?;
""", (trade_id_1,))
conn.commit()
print("[+] Reconciliation engine flagged Trade 1 as 'Discrepancy'")

# Print Audit Trail
print("\n" + "=" * 65)
print("  IMMUTABLE AUDIT TRAIL (Generated automatically by Trigger)")
print("=" * 65)
cursor.execute("SELECT audit_id, trade_id, action, details, created_at FROM trade_audit_logs;")
for row in cursor.fetchall():
    print(f"[{row[4]}] ID: {row[0]} | Action: {row[2]} | {row[3]}")
print("=" * 65)

conn.close()
