-- ============================================================================
-- ADD SUPPORT TICKETS RAISED COLUMN TO EVENTS TABLE
-- Database: PostgreSQL
-- ============================================================================

-- 1. Add the column to the events table (with default 0)
ALTER TABLE IF EXISTS events 
ADD COLUMN IF NOT EXISTS support_ticket_raised_count INT NOT NULL DEFAULT 0;

-- Optional alias column if you prefer "support_tickets_raised":
-- ALTER TABLE IF EXISTS events 
-- ADD COLUMN IF NOT EXISTS support_tickets_raised INT NOT NULL DEFAULT 0;

-- 2. Backfill existing counts from active support tickets
UPDATE events e
SET support_ticket_raised_count = COALESCE(sub.ticket_count, 0)
FROM (
    SELECT event_id, COUNT(*) AS ticket_count
    FROM support_tickets
    WHERE is_deleted = false AND event_id IS NOT NULL
    GROUP BY event_id
) sub
WHERE e.event_id = sub.event_id;

-- 3. Ensure any events without tickets explicitly remain 0 (not null)
UPDATE events
SET support_ticket_raised_count = 0
WHERE support_ticket_raised_count IS NULL;
