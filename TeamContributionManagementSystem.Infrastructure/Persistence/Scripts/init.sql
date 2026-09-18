CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE roles (
    role_id UUID PRIMARY KEY,
    role_name VARCHAR(100) NOT NULL UNIQUE,
    default_contribution_amount NUMERIC(12,2) NOT NULL
);

CREATE TABLE event_types (
    event_type_id UUID PRIMARY KEY,
    event_type_name VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    base_amount NUMERIC(12,2) NOT NULL DEFAULT 0
);

CREATE TABLE users (
    user_id UUID PRIMARY KEY,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    role VARCHAR(20) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    profile_image VARCHAR(500) NULL,
    password_reset_otp VARCHAR(10) NULL,
    password_reset_otp_expiry TIMESTAMP WITH TIME ZONE NULL
);

CREATE TABLE members (
    member_id UUID PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    phone VARCHAR(20) NOT NULL,
    role_id UUID NOT NULL REFERENCES roles(role_id) ON DELETE RESTRICT,
    date_of_birth DATE NOT NULL,
    joining_date DATE NOT NULL,
    gender VARCHAR(20) NOT NULL DEFAULT '',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_exited BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE events (
    event_id UUID PRIMARY KEY,
    event_name VARCHAR(200) NOT NULL,
    event_type_id UUID NOT NULL REFERENCES event_types(event_type_id) ON DELETE RESTRICT,
    event_date DATE NOT NULL,
    created_by UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    description VARCHAR(1000) NOT NULL DEFAULT '',
    status VARCHAR(30) NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    base_amount NUMERIC(12,2) NOT NULL DEFAULT 0
);

CREATE TABLE event_participants (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(member_id) ON DELETE RESTRICT,
    CONSTRAINT uq_event_participants UNIQUE (event_id, member_id)
);

CREATE TABLE contributions (
    contribution_id UUID PRIMARY KEY,
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(member_id) ON DELETE RESTRICT,
    amount NUMERIC(12,2) NOT NULL,
    payment_status VARCHAR(20) NOT NULL,
    payment_date TIMESTAMP NULL,
    payment_mode VARCHAR(20) NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_contributions UNIQUE (event_id, member_id)
);

CREATE TABLE role_rights (
    role_right_id UUID PRIMARY KEY,
    role VARCHAR(50) NOT NULL,
    module VARCHAR(100) NOT NULL,
    sub_module VARCHAR(100) NOT NULL,
    page VARCHAR(100) NOT NULL,
    access VARCHAR(50) NOT NULL
);

CREATE UNIQUE INDEX ix_role_rights_role_module_sub_module_page ON role_rights(role, module, sub_module, page);

CREATE INDEX ix_members_role_active ON members(role_id, is_active);
CREATE INDEX ix_events_date_type ON events(event_date, event_type_id);
CREATE INDEX ix_contributions_event_status ON contributions(event_id, payment_status);

INSERT INTO roles (role_id, role_name, default_contribution_amount) VALUES
('11111111-1111-1111-1111-111111111111', 'Intern', 150.00),
('22222222-2222-2222-2222-222222222222', 'Developer', 300.00),
('33333333-3333-3333-3333-333333333333', 'Manager', 500.00);

INSERT INTO event_types (event_type_id, event_type_name, is_active, base_amount) VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1', 'Birthday', TRUE, 500.00),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', 'Farewell', TRUE, 1000.00),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3', 'Team Dinner', TRUE, 2000.00),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4', 'Custom Event', TRUE, 1500.00);

INSERT INTO users (user_id, email, password_hash, role, full_name, is_active) VALUES
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'admin@teamcontribution.local', '100000.v+81s0TtMH5cHma9kcAiGg==.yL2yB2nZvg/qorlAq/exryN969C/TW1HSSSeAPgN2ng=', 'Admin', 'System Administrator', TRUE),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', 'member@teamcontribution.local', '100000.iij2d7FP46fK87x6bGdSoQ==.GgKzKQn4tepuZuoN5DGL8rhPLP4rphVrUZFSlq/aWk4=', 'Member', 'General Member', TRUE);

INSERT INTO members (member_id, name, email, phone, role_id, date_of_birth, joining_date, gender, is_active, is_deleted) VALUES
('cccccccc-cccc-cccc-cccc-ccccccccccc1', 'Aarav Patel', 'aarav.patel@team.local', '9876543210', '22222222-2222-2222-2222-222222222222', '1996-04-18', '2023-01-10', 'Male', TRUE, FALSE),
('cccccccc-cccc-cccc-cccc-ccccccccccc2', 'Nisha Verma', 'nisha.verma@team.local', '9876501234', '33333333-3333-3333-3333-333333333333', '1992-04-25', '2021-09-15', 'Female', TRUE, FALSE),
('cccccccc-cccc-cccc-cccc-ccccccccccc3', 'Rohan Das', 'rohan.das@team.local', '9012345678', '11111111-1111-1111-1111-111111111111', '1999-06-05', '2024-02-02', 'Male', TRUE, FALSE);

INSERT INTO events (event_id, event_name, event_type_id, event_date, created_by, description, status, is_deleted, base_amount) VALUES
('dddddddd-dddd-dddd-dddd-ddddddddddd1', 'April Team Dinner', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3', CURRENT_DATE + INTERVAL '10 day', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'Quarterly dinner contribution event.', 'Planned', FALSE, 2000.00);

INSERT INTO event_participants (id, event_id, member_id) VALUES
('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1', 'dddddddd-dddd-dddd-dddd-ddddddddddd1', 'cccccccc-cccc-cccc-cccc-ccccccccccc1'),
('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2', 'dddddddd-dddd-dddd-dddd-ddddddddddd1', 'cccccccc-cccc-cccc-cccc-ccccccccccc2'),
('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3', 'dddddddd-dddd-dddd-dddd-ddddddddddd1', 'cccccccc-cccc-cccc-cccc-ccccccccccc3');

INSERT INTO contributions (contribution_id, event_id, member_id, amount, payment_status, payment_date, payment_mode, is_deleted) VALUES
('ffffffff-ffff-ffff-ffff-fffffffffff1', 'dddddddd-dddd-dddd-dddd-ddddddddddd1', 'cccccccc-cccc-cccc-cccc-ccccccccccc1', 300.00, 'Paid', CURRENT_TIMESTAMP, 'Upi', FALSE),
('ffffffff-ffff-ffff-ffff-fffffffffff2', 'dddddddd-dddd-dddd-dddd-ddddddddddd1', 'cccccccc-cccc-cccc-cccc-ccccccccccc2', 500.00, 'Pending', NULL, 'None', FALSE),
('ffffffff-ffff-ffff-ffff-fffffffffff3', 'dddddddd-dddd-dddd-dddd-ddddddddddd1', 'cccccccc-cccc-cccc-cccc-ccccccccccc3', 150.00, 'Pending', NULL, 'None', FALSE);

-- Schema upgrades for existing databases:
ALTER TABLE users ADD COLUMN IF NOT EXISTS profile_image VARCHAR(500) NULL;
ALTER TABLE events ADD COLUMN IF NOT EXISTS base_amount NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_otp VARCHAR(10) NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_otp_expiry TIMESTAMP WITH TIME ZONE NULL;
ALTER TABLE event_types ADD COLUMN IF NOT EXISTS base_amount NUMERIC(12,2) NOT NULL DEFAULT 0;


