-- ============================================================================
-- TEAM CONTRIBUTION MANAGEMENT SYSTEM - DATABASE SCHEMA SCRIPT (init.sql)
-- Complete DDL: Extensions, Table Creations, Schema Upgrades (Alter), & Indexes
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- 1. TABLE CREATIONS (CREATE TABLE IF NOT EXISTS)
-- ============================================================================

-- 1. Roles Table
CREATE TABLE IF NOT EXISTS roles (
    role_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_name VARCHAR(100) NOT NULL UNIQUE,
    default_contribution_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 2. Event Types Table
CREATE TABLE IF NOT EXISTS event_types (
    event_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_type_name VARCHAR(100) NOT NULL UNIQUE,
    has_tenure_rule BOOLEAN NOT NULL DEFAULT FALSE,
    tenure_threshold_years NUMERIC(4,2) NOT NULL DEFAULT 1.0,
    new_entrant_share_percentage NUMERIC(5,2) NOT NULL DEFAULT 50.0,
    standard_share_percentage NUMERIC(5,2) NOT NULL DEFAULT 100.0,
    rule_description VARCHAR(200) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 3. Users Table
CREATE TABLE IF NOT EXISTS users (
    user_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    role VARCHAR(20) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    profile_image VARCHAR(500) NULL,
    password_reset_otp VARCHAR(10) NULL,
    password_reset_otp_expiry TIMESTAMPTZ NULL,
    is_two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- Add foreign keys for roles and event_types referencing users
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_roles_created_by') THEN
        ALTER TABLE roles ADD CONSTRAINT fk_roles_created_by FOREIGN KEY (created_by) REFERENCES users(user_id) ON DELETE SET NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_roles_modified_by') THEN
        ALTER TABLE roles ADD CONSTRAINT fk_roles_modified_by FOREIGN KEY (modified_by) REFERENCES users(user_id) ON DELETE SET NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_event_types_created_by') THEN
        ALTER TABLE event_types ADD CONSTRAINT fk_event_types_created_by FOREIGN KEY (created_by) REFERENCES users(user_id) ON DELETE SET NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_event_types_modified_by') THEN
        ALTER TABLE event_types ADD CONSTRAINT fk_event_types_modified_by FOREIGN KEY (modified_by) REFERENCES users(user_id) ON DELETE SET NULL;
    END IF;
END $$;

-- 4. User MFA Devices Table
CREATE TABLE IF NOT EXISTS user_mfa_devices (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    device_label VARCHAR(100) NOT NULL,
    secret_key VARCHAR(100) NOT NULL,
    date_added TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 5. Device Details Table
CREATE TABLE IF NOT EXISTS device_details (
    device_detail_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    device_id VARCHAR(255) NOT NULL,
    device_name VARCHAR(100) NOT NULL,
    brand VARCHAR(50) NOT NULL,
    model VARCHAR(100) NOT NULL,
    os VARCHAR(50) NOT NULL,
    os_version VARCHAR(50) NOT NULL,
    system_name VARCHAR(50) NOT NULL,
    system_version VARCHAR(50) NOT NULL,
    device_type SMALLINT NOT NULL DEFAULT 1,
    app_version VARCHAR(20) NOT NULL,
    total_memory BIGINT NULL,
    browser VARCHAR(100) NOT NULL,
    browser_version VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_seen_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 6. Device Login History Table
CREATE TABLE IF NOT EXISTS device_login_history (
    history_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    device_detail_id UUID NOT NULL REFERENCES device_details(device_detail_id) ON DELETE CASCADE,
    login_time TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    logout_time TIMESTAMPTZ NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 7. Members Table
CREATE TABLE IF NOT EXISTS members (
    member_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    phone VARCHAR(20) NOT NULL,
    role_id UUID NULL REFERENCES roles(role_id) ON DELETE RESTRICT,
    date_of_birth DATE NOT NULL,
    joining_date DATE NOT NULL,
    gender VARCHAR(20) NOT NULL,
    member_type VARCHAR(20) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_exited BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 8. Events Table
CREATE TABLE IF NOT EXISTS events (
    event_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_name VARCHAR(200) NOT NULL,
    event_type_id UUID NOT NULL REFERENCES event_types(event_type_id) ON DELETE RESTRICT,
    event_date DATE NOT NULL,
    event_dates VARCHAR(500) NULL,
    created_by UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    description VARCHAR(1000) NOT NULL,
    status VARCHAR(30) NOT NULL,
    base_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 9. Event Participants Table
CREATE TABLE IF NOT EXISTS event_participants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(member_id) ON DELETE RESTRICT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL,
    CONSTRAINT uq_event_participants UNIQUE (event_id, member_id)
);

-- 10. Contributions Table
CREATE TABLE IF NOT EXISTS contributions (
    contribution_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(member_id) ON DELETE RESTRICT,
    amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    payment_status VARCHAR(20) NOT NULL,
    payment_date TIMESTAMPTZ NULL,
    payment_mode VARCHAR(20) NOT NULL,
    cash_amount NUMERIC(12,2) NULL,
    upi_amount NUMERIC(12,2) NULL,
    last_reminder_sent_at TIMESTAMPTZ NULL,
    reminder_count INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL,
    CONSTRAINT uq_contributions UNIQUE (event_id, member_id)
);

ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS last_reminder_sent_at TIMESTAMPTZ NULL;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS reminder_count INT NOT NULL DEFAULT 0;

-- 11. Role Rights Table
CREATE TABLE IF NOT EXISTS role_rights (
    role_right_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role VARCHAR(50) NOT NULL,
    module VARCHAR(100) NOT NULL,
    sub_module VARCHAR(100) NOT NULL,
    page VARCHAR(100) NOT NULL,
    access VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL,
    CONSTRAINT uq_role_rights UNIQUE (role, module, sub_module, page)
);

-- 12. Expenses Table
CREATE TABLE IF NOT EXISTS expenses (
    expense_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_name VARCHAR(200) NOT NULL,
    category VARCHAR(100) NOT NULL,
    amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    expense_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(50) NOT NULL,
    submitted_by VARCHAR(150) NOT NULL,
    approved_by VARCHAR(150) NULL,
    description VARCHAR(1000) NOT NULL,
    file_name TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 13. Support Tickets Table
CREATE TABLE IF NOT EXISTS support_tickets (
    ticket_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_no VARCHAR(50) NOT NULL UNIQUE,
    member_name VARCHAR(150) NOT NULL,
    member_id VARCHAR(100) NULL,
    related_event VARCHAR(200) NULL,
    ticket_type VARCHAR(100) NOT NULL,
    subject VARCHAR(300) NOT NULL,
    description VARCHAR(2000) NOT NULL,
    status VARCHAR(50) NOT NULL,
    priority VARCHAR(50) NOT NULL,
    assigned_to VARCHAR(150) NULL,
    ref_no VARCHAR(100) NULL,
    utr VARCHAR(100) NULL,
    attachment TEXT NULL,
    resolution_notes VARCHAR(2000) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 14. System Settings Table
CREATE TABLE IF NOT EXISTS system_settings (
    setting_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    setting_key VARCHAR(100) NOT NULL UNIQUE,
    setting_value TEXT NOT NULL,
    category VARCHAR(100) NOT NULL,
    description VARCHAR(500) NULL,
    allowed_multiple_event BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 15. Payment Transactions Table
CREATE TABLE IF NOT EXISTS payment_transactions (
    transaction_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    txn_number VARCHAR(50) NOT NULL UNIQUE,
    member_name VARCHAR(150) NOT NULL,
    event_name VARCHAR(200) NOT NULL,
    amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    payment_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    payment_mode VARCHAR(50) NOT NULL,
    utr VARCHAR(100) NULL,
    status VARCHAR(50) NOT NULL,
    verified_by VARCHAR(150) NULL,
    verified_on TIMESTAMPTZ NULL,
    notes VARCHAR(1000) NULL,
    screenshot TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 16. Gallery Photos Table
CREATE TABLE IF NOT EXISTS gallery_photos (
    photo_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(200) NOT NULL,
    event_name VARCHAR(200) NOT NULL,
    category VARCHAR(100) NOT NULL,
    image_url TEXT NOT NULL,
    taken_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    description VARCHAR(1000) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 17. Budget Calculations Table
CREATE TABLE IF NOT EXISTS budget_calculations (
    budget_calculation_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    expense_item VARCHAR(150) NOT NULL,
    rate NUMERIC(12,2) NOT NULL DEFAULT 0,
    category VARCHAR(100) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 18. Ticket Types Table
CREATE TABLE IF NOT EXISTS ticket_types (
    ticket_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    type_name VARCHAR(150) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 19. Statuses Table
CREATE TABLE IF NOT EXISTS statuses (
    status_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    status_name VARCHAR(100) NOT NULL,
    module VARCHAR(100) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 20. Work Types Table
CREATE TABLE IF NOT EXISTS work_types (
    work_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    work_type_name VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 21. Priorities Table
CREATE TABLE IF NOT EXISTS priorities (
    priority_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    priority_name VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);

-- 22. Payment Modes Table
CREATE TABLE IF NOT EXISTS payment_modes (
    payment_mode_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_mode_name VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    is_cash BOOLEAN DEFAULT FALSE,
    supports_qr BOOLEAN DEFAULT TRUE,
    payment_type VARCHAR(50) DEFAULT 'Digital',
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);


-- ============================================================================
-- 2. SCHEMA UPGRADES (ALTER TABLE IF EXISTS ... ADD COLUMN / MIGRATE TO UUID)
-- ============================================================================

-- Helper function to resolve legacy string created_by/modified_by to valid UUID user_id
CREATE OR REPLACE FUNCTION resolve_user_to_uuid(val TEXT)
RETURNS UUID AS $$
DECLARE
    matched_id UUID;
BEGIN
    IF val IS NULL OR TRIM(val) = '' THEN
        RETURN NULL;
    END IF;

    -- Check if val is already a valid UUID string
    IF val ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN
        SELECT user_id INTO matched_id FROM users WHERE user_id = val::uuid;
        IF matched_id IS NOT NULL THEN
            RETURN matched_id;
        END IF;
    END IF;

    -- Match against username
    SELECT user_id INTO matched_id FROM users WHERE LOWER(TRIM(username)) = LOWER(TRIM(val)) LIMIT 1;
    IF matched_id IS NOT NULL THEN
        RETURN matched_id;
    END IF;

    -- Match against full_name
    SELECT user_id INTO matched_id FROM users WHERE LOWER(TRIM(full_name)) = LOWER(TRIM(val)) LIMIT 1;
    IF matched_id IS NOT NULL THEN
        RETURN matched_id;
    END IF;

    -- Match against email
    SELECT user_id INTO matched_id FROM users WHERE LOWER(TRIM(email)) = LOWER(TRIM(val)) LIMIT 1;
    IF matched_id IS NOT NULL THEN
        RETURN matched_id;
    END IF;

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Convert created_by and modified_by across all tables to UUID
DO $$
DECLARE
    tbl text;
    tables text[] := ARRAY[
        'roles', 'event_types', 'users', 'user_mfa_devices', 'device_details', 
        'device_login_history', 'members', 'events', 'event_participants', 
        'contributions', 'role_rights', 'expenses', 'support_tickets', 
        'system_settings', 'payment_transactions', 'gallery_photos', 
        'budget_calculations', 'ticket_types', 'statuses', 'work_types', 
        'priorities', 'payment_modes'
    ];
BEGIN
    FOREACH tbl IN ARRAY tables
    LOOP
        -- Convert created_by if it's text/varchar
        IF EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_schema = 'public' AND table_name = tbl AND column_name = 'created_by' AND data_type IN ('character varying', 'text', 'character')
        ) THEN
            EXECUTE format('ALTER TABLE %I ALTER COLUMN created_by TYPE UUID USING resolve_user_to_uuid(created_by::text);', tbl);
        END IF;

        -- Convert modified_by if it's text/varchar
        IF EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_schema = 'public' AND table_name = tbl AND column_name = 'modified_by' AND data_type IN ('character varying', 'text', 'character')
        ) THEN
            EXECUTE format('ALTER TABLE %I ALTER COLUMN modified_by TYPE UUID USING resolve_user_to_uuid(modified_by::text);', tbl);
        END IF;
    END LOOP;
END $$;

-- Drop obsolete helper function after migration
DROP FUNCTION IF EXISTS resolve_user_to_uuid(TEXT);

-- Add Foreign Key Constraints if they do not exist
DO $$
DECLARE
    tbl text;
    fk_name text;
    tables text[] := ARRAY[
        'roles', 'event_types', 'users', 'user_mfa_devices', 'device_details', 
        'device_login_history', 'members', 'event_participants', 
        'contributions', 'role_rights', 'expenses', 'support_tickets', 
        'system_settings', 'payment_transactions', 'gallery_photos', 
        'budget_calculations', 'ticket_types', 'statuses', 'work_types', 
        'priorities', 'payment_modes'
    ];
BEGIN
    FOREACH tbl IN ARRAY tables
    LOOP
        fk_name := 'fk_' || tbl || '_created_by';
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = fk_name) THEN
            EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I FOREIGN KEY (created_by) REFERENCES users(user_id) ON DELETE SET NULL;', tbl, fk_name);
        END IF;

        fk_name := 'fk_' || tbl || '_modified_by';
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = fk_name) THEN
            EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I FOREIGN KEY (modified_by) REFERENCES users(user_id) ON DELETE SET NULL;', tbl, fk_name);
        END IF;
    END LOOP;

    -- Events table modified_by FK
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_events_modified_by') THEN
        ALTER TABLE events ADD CONSTRAINT fk_events_modified_by FOREIGN KEY (modified_by) REFERENCES users(user_id) ON DELETE SET NULL;
    END IF;
END $$;

-- Support Tickets Specific Upgrades
ALTER TABLE IF EXISTS support_tickets ALTER COLUMN attachment TYPE TEXT;
ALTER TABLE IF EXISTS support_tickets ALTER COLUMN subject DROP NOT NULL;
ALTER TABLE IF EXISTS support_tickets ALTER COLUMN assigned_to DROP NOT NULL;

-- Gallery & Transactions Specific Upgrades
ALTER TABLE IF EXISTS payment_transactions ALTER COLUMN screenshot TYPE TEXT;
ALTER TABLE IF EXISTS gallery_photos ALTER COLUMN image_url TYPE TEXT;

-- Events: Add event_dates column for multi-date celebrations
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS event_dates VARCHAR(500) NULL;

-- Members: Allow role_id to be nullable
ALTER TABLE IF EXISTS members ALTER COLUMN role_id DROP NOT NULL;

-- Schema upgrades for contributions, system_settings, budget_calculations, roles, users, user_roles, payment_transactions, support_tickets
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS last_reminder_sent_at TIMESTAMPTZ NULL;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS reminder_count INT NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS allowed_multiple_event BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS category VARCHAR(100) NULL;
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS event_type_id UUID NULL;

ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS default_contribution_amount NUMERIC(12,2) NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS enable_multiple_roles BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS phone VARCHAR(20) DEFAULT '';
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS gender VARCHAR(20) DEFAULT 'Male';
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS work_type VARCHAR(20) DEFAULT 'Office';
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS work_type_id UUID NULL;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS date_of_birth TIMESTAMPTZ DEFAULT CURRENT_DATE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS joining_date TIMESTAMPTZ DEFAULT CURRENT_DATE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_exited BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_first_login BOOLEAN DEFAULT TRUE;
ALTER TABLE IF EXISTS users ALTER COLUMN profile_image TYPE TEXT;

ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;

ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS member_name VARCHAR(150) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS event_name VARCHAR(200) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS txn_number VARCHAR(50) NOT NULL DEFAULT '';

ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS member_name VARCHAR(150) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS member_id VARCHAR(100) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS ticket_type VARCHAR(100) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS priority VARCHAR(50) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS status VARCHAR(50) NOT NULL DEFAULT '';


-- ============================================================================
-- 3. INDEXES
-- ============================================================================

CREATE INDEX IF NOT EXISTS ix_members_role_active ON members(role_id, is_active);
CREATE INDEX IF NOT EXISTS ix_events_date_type ON events(event_date, event_type_id);
CREATE INDEX IF NOT EXISTS ix_contributions_event_status ON contributions(event_id, payment_status);
CREATE INDEX IF NOT EXISTS ix_expenses_date_status ON expenses(expense_date, status);
CREATE INDEX IF NOT EXISTS ix_support_tickets_status_priority ON support_tickets(status, priority);
CREATE INDEX IF NOT EXISTS ix_payment_transactions_date_status ON payment_transactions(payment_date, status);
CREATE INDEX IF NOT EXISTS ix_gallery_photos_event_category ON gallery_photos(event_name, category);
CREATE INDEX IF NOT EXISTS ix_budget_calculations_expense_item ON budget_calculations(expense_item);
CREATE UNIQUE INDEX IF NOT EXISTS ix_role_rights_role_module_sub_module_page ON role_rights(role, module, sub_module, page);
CREATE INDEX IF NOT EXISTS ix_payment_modes_active ON payment_modes(is_active, is_deleted);
