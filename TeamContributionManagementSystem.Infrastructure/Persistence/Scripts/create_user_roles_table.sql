-- ============================================================================
-- Migration: Create 'user_roles' table and drop 'role' column from 'users'
-- Database: PostgreSQL
-- ============================================================================

-- Step 1: Create the 'user_roles' junction table
CREATE TABLE IF NOT EXISTS user_roles (
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_users FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_roles FOREIGN KEY (role_id) REFERENCES roles(role_id) ON DELETE CASCADE
);

-- Step 2: Create indexes for fast lookup by user and by role
CREATE INDEX IF NOT EXISTS ix_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS ix_user_roles_role_id ON user_roles(role_id);

-- Step 3: Backfill existing user roles from users table into user_roles
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' AND column_name = 'role_id'
    ) THEN
        INSERT INTO user_roles (user_id, role_id, created_at)
        SELECT u.user_id, u.role_id, CURRENT_TIMESTAMP
        FROM users u
        WHERE u.role_id IS NOT NULL
        ON CONFLICT (user_id, role_id) DO NOTHING;
    END IF;
END $$;

-- Step 4: Drop the 'role' column from the 'users' table if it exists
ALTER TABLE users DROP COLUMN IF EXISTS role;

-- Step 5: Ensure 'work_type', 'is_exited', and other merged columns exist on 'users'
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' AND column_name = 'member_type'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' AND column_name = 'work_type'
    ) THEN
        ALTER TABLE users RENAME COLUMN member_type TO work_type;
    END IF;
END $$;

ALTER TABLE users 
ADD COLUMN IF NOT EXISTS work_type VARCHAR(20) DEFAULT 'Office',
ADD COLUMN IF NOT EXISTS is_exited BOOLEAN DEFAULT FALSE NOT NULL,
ADD COLUMN IF NOT EXISTS phone VARCHAR(20) DEFAULT '',
ADD COLUMN IF NOT EXISTS gender VARCHAR(20) DEFAULT 'Male',
ADD COLUMN IF NOT EXISTS date_of_birth TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
ADD COLUMN IF NOT EXISTS joining_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN DEFAULT FALSE NOT NULL;
