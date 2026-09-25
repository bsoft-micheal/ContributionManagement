-- ============================================================================
-- TEAM CONTRIBUTION MANAGEMENT SYSTEM - DATABASE INITIALIZATION SCRIPT (init.sql)
-- Complete DDL, Schema Upgrades, Indexes, and Master Seed Data
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- TABLE CREATIONS (IF NOT EXISTS)
-- ============================================================================

-- 1. Roles Table
CREATE TABLE IF NOT EXISTS roles (
    role_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_name VARCHAR(100) NOT NULL UNIQUE,
    default_contribution_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 2. Event Types Table
CREATE TABLE IF NOT EXISTS event_types (
    event_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_type_name VARCHAR(100) NOT NULL UNIQUE,
    base_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 3. Users Table
CREATE TABLE IF NOT EXISTS users (
    user_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'User',
    full_name VARCHAR(150) NOT NULL,
    profile_image VARCHAR(500) NULL,
    password_reset_otp VARCHAR(10) NULL,
    password_reset_otp_expiry TIMESTAMP WITH TIME ZONE NULL,
    is_two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 4. User MFA Devices Table
CREATE TABLE IF NOT EXISTS user_mfa_devices (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    device_label VARCHAR(100) NOT NULL DEFAULT '',
    secret_key VARCHAR(100) NOT NULL DEFAULT '',
    date_added TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 5. Device Details Table
CREATE TABLE IF NOT EXISTS device_details (
    device_detail_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    device_id VARCHAR(255) NOT NULL DEFAULT '',
    device_name VARCHAR(100) NOT NULL DEFAULT '',
    brand VARCHAR(50) NOT NULL DEFAULT '',
    model VARCHAR(100) NOT NULL DEFAULT '',
    os VARCHAR(50) NOT NULL DEFAULT '',
    os_version VARCHAR(50) NOT NULL DEFAULT '',
    system_name VARCHAR(50) NOT NULL DEFAULT '',
    system_version VARCHAR(50) NOT NULL DEFAULT '',
    device_type SMALLINT NOT NULL DEFAULT 1,
    app_version VARCHAR(20) NOT NULL DEFAULT '',
    total_memory BIGINT NULL,
    browser VARCHAR(100) NOT NULL DEFAULT '',
    browser_version VARCHAR(50) NOT NULL DEFAULT '',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_seen_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 6. Device Login History Table
CREATE TABLE IF NOT EXISTS device_login_history (
    history_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    device_detail_id UUID NOT NULL REFERENCES device_details(device_detail_id) ON DELETE CASCADE,
    login_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    logout_time TIMESTAMP WITH TIME ZONE NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 7. Members Table
CREATE TABLE IF NOT EXISTS members (
    member_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    phone VARCHAR(20) NOT NULL,
    role_id UUID NOT NULL REFERENCES roles(role_id) ON DELETE RESTRICT,
    date_of_birth DATE NOT NULL,
    joining_date DATE NOT NULL,
    gender VARCHAR(20) NOT NULL DEFAULT '',
    member_type VARCHAR(20) NOT NULL DEFAULT 'Office',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_exited BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 8. Events Table
CREATE TABLE IF NOT EXISTS events (
    event_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_name VARCHAR(200) NOT NULL,
    event_type_id UUID NOT NULL REFERENCES event_types(event_type_id) ON DELETE RESTRICT,
    event_date DATE NOT NULL,
    created_by UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    description VARCHAR(1000) NOT NULL DEFAULT '',
    status VARCHAR(30) NOT NULL DEFAULT 'Planned',
    base_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 9. Event Participants Table
CREATE TABLE IF NOT EXISTS event_participants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(member_id) ON DELETE RESTRICT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT uq_event_participants UNIQUE (event_id, member_id)
);

-- 10. Contributions Table
CREATE TABLE IF NOT EXISTS contributions (
    contribution_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(member_id) ON DELETE RESTRICT,
    amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    payment_status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    payment_date TIMESTAMP WITH TIME ZONE NULL,
    payment_mode VARCHAR(20) NOT NULL DEFAULT 'None',
    cash_amount NUMERIC(12,2) NULL,
    upi_amount NUMERIC(12,2) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT uq_contributions UNIQUE (event_id, member_id)
);

-- 11. Role Rights Table
CREATE TABLE IF NOT EXISTS role_rights (
    role_right_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role VARCHAR(50) NOT NULL,
    module VARCHAR(100) NOT NULL,
    sub_module VARCHAR(100) NOT NULL,
    page VARCHAR(100) NOT NULL,
    access VARCHAR(50) NOT NULL DEFAULT 'readWrite',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT uq_role_rights UNIQUE (role, module, sub_module, page)
);

-- 12. Expenses Table
CREATE TABLE IF NOT EXISTS expenses (
    expense_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_name VARCHAR(200) NOT NULL,
    category VARCHAR(100) NOT NULL,
    amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    expense_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    submitted_by VARCHAR(150) NOT NULL,
    approved_by VARCHAR(150) NULL,
    description VARCHAR(1000) NOT NULL DEFAULT '',
    file_name TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
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
    description VARCHAR(2000) NOT NULL DEFAULT '',
    status VARCHAR(50) NOT NULL DEFAULT 'Open',
    priority VARCHAR(50) NOT NULL DEFAULT 'Medium',
    assigned_to VARCHAR(150) NULL,
    ref_no VARCHAR(100) NULL,
    utr VARCHAR(100) NULL,
    attachment TEXT NULL,
    resolution_notes VARCHAR(2000) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 14. System Settings Table
CREATE TABLE IF NOT EXISTS system_settings (
    setting_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    setting_key VARCHAR(100) NOT NULL UNIQUE,
    setting_value TEXT NOT NULL,
    category VARCHAR(100) NOT NULL DEFAULT 'General',
    description VARCHAR(500) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 15. Payment Transactions Table
CREATE TABLE IF NOT EXISTS payment_transactions (
    transaction_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    txn_number VARCHAR(50) NOT NULL UNIQUE,
    member_name VARCHAR(150) NOT NULL,
    event_name VARCHAR(200) NOT NULL,
    amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    payment_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    payment_mode VARCHAR(50) NOT NULL DEFAULT 'UPI',
    utr VARCHAR(100) NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    verified_by VARCHAR(150) NULL,
    verified_on TIMESTAMP WITH TIME ZONE NULL,
    notes VARCHAR(1000) NULL,
    screenshot TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 16. Gallery Photos Table
CREATE TABLE IF NOT EXISTS gallery_photos (
    photo_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(200) NOT NULL,
    event_name VARCHAR(200) NOT NULL,
    category VARCHAR(100) NOT NULL DEFAULT 'Moments',
    image_url TEXT NOT NULL,
    taken_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    description VARCHAR(1000) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 17. Budget Calculations Table
CREATE TABLE IF NOT EXISTS budget_calculations (
    budget_calculation_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    expense_item VARCHAR(150) NOT NULL,
    rate NUMERIC(12,2) NOT NULL DEFAULT 0,
    category VARCHAR(100) NULL DEFAULT 'Birthday',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 18. Ticket Types Table
CREATE TABLE IF NOT EXISTS ticket_types (
    ticket_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    type_name VARCHAR(150) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 19. Statuses Table
CREATE TABLE IF NOT EXISTS statuses (
    status_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    status_name VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- 20. Work Types Table
CREATE TABLE IF NOT EXISTS work_types (
    work_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    work_type_name VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(150) NULL DEFAULT 'System',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(150) NULL,
    modified_on TIMESTAMP WITH TIME ZONE NULL
);

-- ============================================================================
-- IDEMPOTENT SCHEMA UPGRADES (ALTER SCRIPTS)
-- ============================================================================

-- Roles Alterations
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS default_contribution_amount NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Event Types Alterations
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS base_amount NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Users Alterations
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS profile_image VARCHAR(500) NULL;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS password_reset_otp VARCHAR(10) NULL;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS password_reset_otp_expiry TIMESTAMP WITH TIME ZONE NULL;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Members Alterations
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS member_type VARCHAR(20) NOT NULL DEFAULT 'Office';
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS is_exited BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Events Alterations
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS base_amount NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Event Participants Alterations
ALTER TABLE IF EXISTS event_participants ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS event_participants ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS event_participants ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS event_participants ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS event_participants ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS event_participants ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS event_participants ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Contributions Alterations
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS cash_amount NUMERIC(12,2) NULL;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS upi_amount NUMERIC(12,2) NULL;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Role Rights Alterations
ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Expenses Alterations
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Support Tickets Alterations
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS attachment TEXT NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;
DO $$ BEGIN
    ALTER TABLE support_tickets ALTER COLUMN attachment TYPE TEXT;
EXCEPTION WHEN OTHERS THEN NULL; END $$;

-- System Settings Alterations
ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Payment Transactions Alterations
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;
DO $$ BEGIN
    ALTER TABLE payment_transactions ALTER COLUMN screenshot TYPE TEXT;
EXCEPTION WHEN OTHERS THEN NULL; END $$;

-- Gallery Photos Alterations
ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;
DO $$ BEGIN
    ALTER TABLE gallery_photos ALTER COLUMN image_url TYPE TEXT;
EXCEPTION WHEN OTHERS THEN NULL; END $$;

-- Budget Calculations Alterations
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Ticket Types Alterations
ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;
ALTER TABLE IF EXISTS ticket_types DROP COLUMN IF EXISTS description;

-- Statuses Alterations
ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Work Types Alterations
ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Device Details Alterations
ALTER TABLE IF EXISTS device_details ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS device_details ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS device_details ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS device_details ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- Device Login History Alterations
ALTER TABLE IF EXISTS device_login_history ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS device_login_history ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS device_login_history ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS device_login_history ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS device_login_history ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- User MFA Devices Alterations
ALTER TABLE IF EXISTS user_mfa_devices ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE IF EXISTS user_mfa_devices ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE IF EXISTS user_mfa_devices ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL DEFAULT 'System';
ALTER TABLE IF EXISTS user_mfa_devices ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS user_mfa_devices ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
ALTER TABLE IF EXISTS user_mfa_devices ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

-- ============================================================================
-- INDEXES
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

-- ============================================================================
-- MASTER REFERENCE / SEED DATA
-- ============================================================================

-- 1. Master Roles
INSERT INTO roles (role_id, role_name, default_contribution_amount, is_active, is_deleted, created_by) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Intern', 150.00, TRUE, FALSE, 'System'),
    ('22222222-2222-2222-2222-222222222222', 'Developer', 300.00, TRUE, FALSE, 'System'),
    ('33333333-3333-3333-3333-333333333333', 'Manager', 500.00, TRUE, FALSE, 'System'),
    ('44444444-4444-4444-4444-444444444444', 'Admin', 0.00, TRUE, FALSE, 'System'),
    ('55555555-5555-5555-5555-555555555555', 'Lead', 400.00, TRUE, FALSE, 'System')
ON CONFLICT (role_name) DO UPDATE SET
    default_contribution_amount = EXCLUDED.default_contribution_amount,
    is_active = TRUE;

-- 2. Master Event Types
INSERT INTO event_types (event_type_id, event_type_name, base_amount, is_active, is_deleted, created_by) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1', 'Birthday', 500.00, TRUE, FALSE, 'System'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', 'Farewell', 1000.00, TRUE, FALSE, 'System'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3', 'Team Dinner', 2000.00, TRUE, FALSE, 'System'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4', 'Custom Event', 1500.00, TRUE, FALSE, 'System')
ON CONFLICT (event_type_name) DO UPDATE SET
    base_amount = EXCLUDED.base_amount,
    is_active = TRUE;

-- 3. Master Work Types
INSERT INTO work_types (work_type_id, work_type_name, is_active, is_deleted, created_by) VALUES
    ('ffffffff-1111-0000-0000-000000000001', 'Office', TRUE, FALSE, 'System'),
    ('ffffffff-1111-0000-0000-000000000002', 'WFH', TRUE, FALSE, 'System')
ON CONFLICT (work_type_name) DO NOTHING;

-- 4. Master Ticket Types
INSERT INTO ticket_types (ticket_type_id, type_name, is_active, is_deleted, created_by) VALUES
    ('ffffffff-2222-0000-0000-000000000001', 'Payment Issue', TRUE, FALSE, 'System'),
    ('ffffffff-2222-0000-0000-000000000002', 'Event Clarification', TRUE, FALSE, 'System'),
    ('ffffffff-2222-0000-0000-000000000003', 'Application Issue', TRUE, FALSE, 'System'),
    ('ffffffff-2222-0000-0000-000000000004', 'Feedback', TRUE, FALSE, 'System'),
    ('ffffffff-2222-0000-0000-000000000005', 'Other', TRUE, FALSE, 'System')
ON CONFLICT (type_name) DO NOTHING;

-- 5. Master Statuses
INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_by) VALUES
    ('ffffffff-3333-0000-0000-000000000001', 'Open', TRUE, FALSE, 'System'),
    ('ffffffff-3333-0000-0000-000000000002', 'In Progress', TRUE, FALSE, 'System'),
    ('ffffffff-3333-0000-0000-000000000003', 'Resolved', TRUE, FALSE, 'System'),
    ('ffffffff-3333-0000-0000-000000000004', 'Closed', TRUE, FALSE, 'System')
ON CONFLICT (status_name) DO NOTHING;

-- 6. Master Budget Calculations
INSERT INTO budget_calculations (budget_calculation_id, expense_item, rate, category, is_active, is_deleted, created_by) VALUES
    ('ffffffff-4444-0000-0000-000000000001', '½ kg Cake', 300.00, 'Birthday', TRUE, FALSE, 'System'),
    ('ffffffff-4444-0000-0000-000000000002', 'Chicken Roll / Puffs', 20.00, 'Birthday', TRUE, FALSE, 'System'),
    ('ffffffff-4444-0000-0000-000000000003', 'Birthday Gift', 1000.00, 'Birthday', TRUE, FALSE, 'System')
ON CONFLICT (budget_calculation_id) DO NOTHING;

-- 7. Master System Settings
INSERT INTO system_settings (setting_id, setting_key, setting_value, category, description, is_active, is_deleted, created_by) VALUES
    (uuid_generate_v4(), 'orgName', 'Unit 1A Residents Association', 'General', 'Organization display name', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'defaultCurrency', 'INR', 'General', 'Default currency code', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'timeZone', 'Asia/Kolkata', 'General', 'Timezone', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'fromEmail', 'noreply@unit1a.com', 'Email', 'System sender email', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'fromName', 'Unit 1A Management', 'Email', 'Sender display name', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'smtpHost', 'smtp.gmail.com', 'Email', 'SMTP Server host', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'smtpPort', '587', 'Email', 'SMTP Server port', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'encryption', 'TLS', 'Email', 'Email encryption standard', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'otpExpiry', '10', 'Security', 'OTP Expiration in minutes', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'maxRetry', '3', 'Security', 'Max OTP retry attempts', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'enableOtpLogin', 'true', 'Security', 'Enable OTP based password reset', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'enable2faAdmin', 'false', 'Security', 'Two-Factor Auth for Admin', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'enableEmailNotif', 'true', 'Notifications', 'Global email notifications', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'notifNewMember', 'true', 'Notifications', 'New member alert', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'notifPaymentConfirm', 'true', 'Notifications', 'Payment confirmation alert', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'notifEventReminder', 'true', 'Notifications', 'Event reminder alert', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'notifSupportTicket', 'false', 'Notifications', 'Support ticket updates alert', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'qrReceiverName', 'Daniel A', 'PaymentQr', 'Payment QR Receiver Name', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'qrUpiId', 'danielrobertanto604@okicici', 'PaymentQr', 'Payment UPI ID', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'qrImage', 'https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=upi://pay?pa=danielrobertanto604@okicici&pn=Daniel%20A', 'PaymentQr', 'QR Image Source', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'enableAuditLogs', 'true', 'Audit', 'Enable audit trail recording', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'logUserLogin', 'true', 'Audit', 'Record user login activities', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'logDataChanges', 'true', 'Audit', 'Record data mutations', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'logConfigChanges', 'true', 'Audit', 'Record configuration modifications', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'retentionPeriod', '365', 'Audit', 'Audit logs retention in days', TRUE, FALSE, 'System')
ON CONFLICT (setting_key) DO UPDATE SET
    category = EXCLUDED.category,
    description = EXCLUDED.description;

-- 8. Master Users
INSERT INTO users (user_id, username, email, password_hash, role, full_name, is_active, is_deleted, created_by) VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'admin', 'admin@teamcontribution.local', '100000.v+81s0TtMH5cHma9kcAiGg==.yL2yB2nZvg/qorlAq/exryN969C/TW1HSSSeAPgN2ng=', 'Admin', 'System Administrator', TRUE, FALSE, 'System'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', 'member', 'member@teamcontribution.local', '100000.iij2d7FP46fK87x6bGdSoQ==.GgKzKQn4tepuZuoN5DGL8rhPLP4rphVrUZFSlq/aWk4=', 'Member', 'General Member', TRUE, FALSE, 'System')
ON CONFLICT (email) DO NOTHING;

-- 9. Master Role Rights
-- ADMIN Rights
INSERT INTO role_rights (role_right_id, role, module, sub_module, page, access, is_active, is_deleted, created_by) VALUES
    (uuid_generate_v4(), 'Admin', 'Dashboard', 'Analytics', 'Dashboard', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Members', 'Directory', 'Members', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Events', 'Registry', 'Events', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Events', 'Calendar', 'Calendar', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Contributions', 'Ledger', 'Contributions', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Contributions', 'Calculation', 'Calculation', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Contributions', 'Expenses', 'Expense', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Contributions', 'Payments', 'Payments', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Events', 'Media', 'Gallery', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Categories', 'Event Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Clearance', 'Exit Process', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Admin', 'User Rights', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Admin', 'Users', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Admin', 'Roles', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Helpdesk', 'Support Tickets', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Calculations', 'Budget Calculations', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Helpdesk', 'Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Helpdesk', 'Ticket Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Helpdesk', 'Status', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Support Data', 'Configuration', 'Settings', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Reports', 'Analytics', 'Event Audit', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Reports', 'Analytics', 'Member Velocity', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Reports', 'Analytics', 'Pending Dues', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Admin', 'Reports', 'Analytics', 'Member Category Paid', 'readWrite', TRUE, FALSE, 'System')
ON CONFLICT (role, module, sub_module, page) DO NOTHING;

-- MANAGER Rights
INSERT INTO role_rights (role_right_id, role, module, sub_module, page, access, is_active, is_deleted, created_by) VALUES
    (uuid_generate_v4(), 'Manager', 'Dashboard', 'Analytics', 'Dashboard', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Members', 'Directory', 'Members', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Events', 'Registry', 'Events', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Events', 'Calendar', 'Calendar', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Contributions', 'Ledger', 'Contributions', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Contributions', 'Calculation', 'Calculation', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Contributions', 'Expenses', 'Expense', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Contributions', 'Payments', 'Payments', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Events', 'Media', 'Gallery', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Categories', 'Event Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Clearance', 'Exit Process', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Admin', 'User Rights', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Admin', 'Users', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Admin', 'Roles', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Helpdesk', 'Support Tickets', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Calculations', 'Budget Calculations', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Helpdesk', 'Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Helpdesk', 'Ticket Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Helpdesk', 'Status', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Support Data', 'Configuration', 'Settings', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Reports', 'Analytics', 'Event Audit', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Reports', 'Analytics', 'Member Velocity', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Reports', 'Analytics', 'Pending Dues', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Manager', 'Reports', 'Analytics', 'Member Category Paid', 'readWrite', TRUE, FALSE, 'System')
ON CONFLICT (role, module, sub_module, page) DO NOTHING;

-- MEMBER Rights
INSERT INTO role_rights (role_right_id, role, module, sub_module, page, access, is_active, is_deleted, created_by) VALUES
    (uuid_generate_v4(), 'Member', 'Dashboard', 'Analytics', 'Dashboard', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Members', 'Directory', 'Members', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Events', 'Registry', 'Events', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Events', 'Calendar', 'Calendar', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Contributions', 'Ledger', 'Contributions', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Contributions', 'Calculation', 'Calculation', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Contributions', 'Expenses', 'Expense', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Contributions', 'Payments', 'Payments', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Events', 'Media', 'Gallery', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Categories', 'Event Types', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Clearance', 'Exit Process', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Admin', 'User Rights', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Admin', 'Users', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Admin', 'Roles', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Helpdesk', 'Support Tickets', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Calculations', 'Budget Calculations', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Helpdesk', 'Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Helpdesk', 'Ticket Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Helpdesk', 'Status', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Support Data', 'Configuration', 'Settings', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Reports', 'Analytics', 'Event Audit', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Reports', 'Analytics', 'Member Velocity', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Reports', 'Analytics', 'Pending Dues', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'Member', 'Reports', 'Analytics', 'Member Category Paid', 'readWrite', TRUE, FALSE, 'System')
ON CONFLICT (role, module, sub_module, page) DO NOTHING;

-- USER Rights
INSERT INTO role_rights (role_right_id, role, module, sub_module, page, access, is_active, is_deleted, created_by) VALUES
    (uuid_generate_v4(), 'User', 'Dashboard', 'Analytics', 'Dashboard', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Members', 'Directory', 'Members', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Events', 'Registry', 'Events', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Events', 'Calendar', 'Calendar', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Contributions', 'Ledger', 'Contributions', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Contributions', 'Calculation', 'Calculation', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Contributions', 'Expenses', 'Expense', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Contributions', 'Payments', 'Payments', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Events', 'Media', 'Gallery', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Categories', 'Event Types', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Clearance', 'Exit Process', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Admin', 'User Rights', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Admin', 'Users', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Admin', 'Roles', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Helpdesk', 'Support Tickets', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Calculations', 'Budget Calculations', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Helpdesk', 'Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Helpdesk', 'Ticket Types', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Helpdesk', 'Status', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Support Data', 'Configuration', 'Settings', 'deny', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Reports', 'Analytics', 'Event Audit', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Reports', 'Analytics', 'Member Velocity', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Reports', 'Analytics', 'Pending Dues', 'readWrite', TRUE, FALSE, 'System'),
    (uuid_generate_v4(), 'User', 'Reports', 'Analytics', 'Member Category Paid', 'readWrite', TRUE, FALSE, 'System')
ON CONFLICT (role, module, sub_module, page) DO NOTHING;
