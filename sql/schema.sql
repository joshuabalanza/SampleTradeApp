-- ============================================================================
-- Blueberry Markets Operate Squad: High-Throughput Trade Database Schema
-- Target Engine: PostgreSQL 15+
-- ============================================================================

-- 1. Custom Types
CREATE TYPE order_side AS ENUM ('Buy', 'Sell');
CREATE TYPE trade_status AS ENUM ('Pending', 'Executed', 'Reconciled', 'Discrepancy', 'Failed');

-- 2. Core Trades Table
CREATE TABLE IF NOT EXISTS trades (
    trade_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key VARCHAR(128) NOT NULL,
    account_id VARCHAR(64) NOT NULL,
    symbol VARCHAR(32) NOT NULL,
    side order_side NOT NULL,
    quantity NUMERIC(18, 6) NOT NULL CHECK (quantity > 0),
    price NUMERIC(18, 6) NOT NULL CHECK (price > 0),
    status trade_status NOT NULL DEFAULT 'Pending',
    discrepancy_reason TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

-- Unique index guaranteeing strict idempotency
CREATE UNIQUE INDEX IF NOT EXISTS uq_trades_idempotency_key 
    ON trades (idempotency_key);

-- Composite index for fast back-office query filtering
CREATE INDEX IF NOT EXISTS idx_trades_status_created 
    ON trades (status, created_at_utc DESC);

CREATE INDEX IF NOT EXISTS idx_trades_account_created 
    ON trades (account_id, created_at_utc DESC);

-- 3. Append-Only Audit Log Table
CREATE TABLE IF NOT EXISTS trade_audit_logs (
    audit_id BIGSERIAL PRIMARY KEY,
    trade_id UUID NOT NULL REFERENCES trades(trade_id) ON DELETE CASCADE,
    action VARCHAR(64) NOT NULL,
    details TEXT,
    payload JSONB,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX IF NOT EXISTS idx_audit_trade_created 
    ON trade_audit_logs (trade_id, created_at_utc ASC);

-- 4. PostgreSQL Trigger for Automatic Audit Logging on Status Changes
CREATE OR REPLACE FUNCTION fn_audit_trade_status_change()
RETURNS TRIGGER AS $$
BEGIN
    IF (TG_OP = 'UPDATE' AND OLD.status IS DISTINCT FROM NEW.status) THEN
        INSERT INTO trade_audit_logs (trade_id, action, details, payload)
        VALUES (
            NEW.trade_id,
            'STATUS_CHANGE',
            format('Status transitioned from %s to %s', OLD.status, NEW.status),
            jsonb_build_object(
                'old_status', OLD.status,
                'new_status', NEW.status,
                'discrepancy_reason', NEW.discrepancy_reason,
                'timestamp', clock_timestamp()
            )
        );
        NEW.updated_at_utc = clock_timestamp();
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_trades_audit_status_change
    BEFORE UPDATE ON trades
    FOR EACH ROW
    EXECUTE FUNCTION fn_audit_trade_status_change();

-- 5. User Directory Table
CREATE TABLE IF NOT EXISTS users (
    user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(64) NOT NULL,
    password_hash VARCHAR(256) NOT NULL,
    display_name VARCHAR(128) NOT NULL,
    role VARCHAR(32) NOT NULL DEFAULT 'Trader',
    account_id VARCHAR(64) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_users_username ON users (LOWER(username));
