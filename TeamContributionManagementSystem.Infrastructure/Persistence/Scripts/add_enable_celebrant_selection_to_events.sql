-- ============================================================================
-- ADD ENABLE_CELEBRANT_SELECTION FLAG TO EVENTS TABLE
-- Database: PostgreSQL
-- ============================================================================

-- 1. Add enable_celebrant_selection column to events table if not already present
ALTER TABLE IF EXISTS events 
ADD COLUMN IF NOT EXISTS enable_celebrant_selection BOOLEAN NOT NULL DEFAULT FALSE;

-- 2. Backward compatibility for existing historical events:
-- If an event already had celebrants specified in event_dates or description,
-- set enable_celebrant_selection to TRUE so that existing event records load correctly.
UPDATE events
SET enable_celebrant_selection = TRUE
WHERE enable_celebrant_selection = FALSE 
  AND (
    (event_dates IS NOT NULL AND event_dates LIKE '%{%}%') 
    OR (description IS NOT NULL AND description LIKE '%<!--celebrants:%')
  );
