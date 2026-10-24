-- ============================================================================
-- ADD DRAFT AND PUBLISH WORKFLOW SUPPORT TO EVENTS TABLE
-- Database: PostgreSQL
-- ============================================================================

-- 1. Add published_at and published_by audit columns to events table if not already present
ALTER TABLE IF EXISTS events 
ADD COLUMN IF NOT EXISTS published_at TIMESTAMPTZ NULL;

ALTER TABLE IF EXISTS events 
ADD COLUMN IF NOT EXISTS published_by UUID NULL;

-- 2. Ensure existing historical events remain Published / Planned (DO NOT convert to Draft)
-- If status is null or empty, default to 'Planned'
UPDATE events
SET status = 'Planned'
WHERE status IS NULL OR status = '';

-- Note: Historical events retain their existing 'Planned', 'Active', 'Completed', or 'Cancelled' status.
