-- ============================================================================
-- FIX MISSING COLUMNS - Run this against your live PostgreSQL database
-- if you see "42703: column X does not exist" errors
-- ============================================================================

-- payment_transactions: add member_name and event_name if missing
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS member_name VARCHAR(150) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS event_name VARCHAR(200) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS txn_number VARCHAR(50) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS amount NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS payment_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS payment_mode VARCHAR(50) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS utr VARCHAR(100) NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS status VARCHAR(50) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS verified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS verified_on TIMESTAMPTZ NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS notes VARCHAR(1000) NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS screenshot TEXT NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS modified_on TIMESTAMPTZ NULL;

-- support_tickets: add ticket_type, priority, member_name, member_id if missing
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS member_name VARCHAR(150) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS member_id VARCHAR(100) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS ticket_type VARCHAR(100) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS priority VARCHAR(50) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS status VARCHAR(50) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS assigned_to VARCHAR(150) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS related_event VARCHAR(200) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS subject VARCHAR(300) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS description VARCHAR(2000) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS ref_no VARCHAR(100) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS utr VARCHAR(100) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS attachment TEXT NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS resolution_notes VARCHAR(2000) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS modified_on TIMESTAMPTZ NULL;
ALTER TABLE IF EXISTS support_tickets ALTER COLUMN subject DROP NOT NULL;
ALTER TABLE IF EXISTS support_tickets ALTER COLUMN assigned_to DROP NOT NULL;

-- roles: add default_contribution_amount if missing
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS default_contribution_amount NUMERIC(12,2) NOT NULL DEFAULT 0;

-- budget_calculations: add category and event_type_id if missing
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS category VARCHAR(100) NULL;
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS event_type_id UUID NULL;

-- users: add extra columns from member migration if missing
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS phone VARCHAR(20) DEFAULT '';
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS gender VARCHAR(20) DEFAULT 'Male';
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS work_type VARCHAR(20) DEFAULT 'Office';
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS work_type_id UUID NULL;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS date_of_birth TIMESTAMPTZ DEFAULT CURRENT_DATE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS joining_date TIMESTAMPTZ DEFAULT CURRENT_DATE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_exited BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_first_login BOOLEAN DEFAULT TRUE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS enable_multiple_roles BOOLEAN DEFAULT FALSE;

-- user_roles: add is_primary and is_secondary
ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;

-- event_participants: rename member_id to user_id (if still using old schema)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'event_participants' AND column_name = 'member_id'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'event_participants' AND column_name = 'user_id'
    ) THEN
        ALTER TABLE event_participants RENAME COLUMN member_id TO user_id;
    END IF;
END $$;

-- contributions: rename member_id to user_id (if still using old schema)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'contributions' AND column_name = 'member_id'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'contributions' AND column_name = 'user_id'
    ) THEN
        ALTER TABLE contributions RENAME COLUMN member_id TO user_id;
    END IF;
END $$;

-- Verify columns exist
SELECT 'payment_transactions.member_name' AS check_column, EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'payment_transactions' AND column_name = 'member_name'
) AS exists;

SELECT 'support_tickets.ticket_type' AS check_column, EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'support_tickets' AND column_name = 'ticket_type'
) AS exists;

SELECT 'support_tickets.priority' AS check_column, EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'support_tickets' AND column_name = 'priority'
) AS exists;

SELECT 'contributions.user_id' AS check_column, EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'contributions' AND column_name = 'user_id'
) AS exists;
