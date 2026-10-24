-- ============================================================================
-- CREATE BIRTHDAY REMINDER LOGS TABLE & UNIQUE CONSTRAINT
-- Database: PostgreSQL
-- ============================================================================

CREATE TABLE IF NOT EXISTS birthday_reminder_logs (
    reminder_log_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    reminder_type VARCHAR(50) NOT NULL DEFAULT 'Scheduled',
    celebration_date TIMESTAMPTZ NOT NULL,
    celebrant_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    recipient_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    recipient_email VARCHAR(150) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Sent',
    sent_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    error_message VARCHAR(500) NULL
);

-- Unique composite index for durable deduplication:
-- Same celebration date, same celebrant, same recipient, and same reminder type cannot be notified twice!
CREATE UNIQUE INDEX IF NOT EXISTS ix_birthday_reminder_logs_dedup
ON birthday_reminder_logs (celebration_date, celebrant_user_id, recipient_user_id, reminder_type);

-- Concurrency safe partial index on events for monthly duplicate prevention (excluding soft-deleted events)
-- Note: Single primary event type uniqueness per year and month
CREATE UNIQUE INDEX IF NOT EXISTS ix_events_unique_event_type_month_year
ON events (event_type_id, EXTRACT(YEAR FROM event_date), EXTRACT(MONTH FROM event_date))
WHERE is_deleted = false;
