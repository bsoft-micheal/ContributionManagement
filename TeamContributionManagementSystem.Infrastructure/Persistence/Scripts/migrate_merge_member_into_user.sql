-- ============================================================================
-- Migration Script: Merge 'members' table directly into 'users' table
-- Team Contribution Management System
-- Database: PostgreSQL
-- ============================================================================

-- If a previous query failed in your session, run ROLLBACK first:
ROLLBACK;

-- ----------------------------------------------------------------------------
-- Step 1: Add new member profile columns to 'users' table
-- ----------------------------------------------------------------------------
ALTER TABLE users ADD COLUMN IF NOT EXISTS phone VARCHAR(20) DEFAULT '';
ALTER TABLE users ADD COLUMN IF NOT EXISTS gender VARCHAR(20) DEFAULT 'Male';
ALTER TABLE users ADD COLUMN IF NOT EXISTS member_type VARCHAR(20) DEFAULT 'Office';
ALTER TABLE users ADD COLUMN IF NOT EXISTS work_type VARCHAR(20) DEFAULT 'Office';
ALTER TABLE users ADD COLUMN IF NOT EXISTS role_id UUID NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS date_of_birth TIMESTAMPTZ DEFAULT CURRENT_DATE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS joining_date TIMESTAMPTZ DEFAULT CURRENT_DATE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_exited BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_first_login BOOLEAN DEFAULT TRUE;

-- ----------------------------------------------------------------------------
-- Step 2: Add foreign key constraint for role_id on users table
-- ----------------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_users_roles_role_id'
    ) THEN
        ALTER TABLE users
        ADD CONSTRAINT fk_users_roles_role_id
        FOREIGN KEY (role_id) REFERENCES roles(role_id)
        ON DELETE RESTRICT;
    END IF;
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Notice: fk_users_roles_role_id already exists or skipped: %', SQLERRM;
END $$;

-- ----------------------------------------------------------------------------
-- Step 3: Copy existing member data to matching users (by email)
-- Supports both 'work_type' and legacy 'member_type' column names in members
-- ----------------------------------------------------------------------------
DO $$
DECLARE
    has_work_type BOOLEAN;
    has_member_type BOOLEAN;
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'members') THEN
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'members' AND column_name = 'work_type'
        ) INTO has_work_type;

        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'members' AND column_name = 'member_type'
        ) INTO has_member_type;

        IF has_work_type THEN
            EXECUTE '
                UPDATE users u
                SET 
                    phone = COALESCE(NULLIF(m.phone, ''''), u.phone),
                    gender = COALESCE(NULLIF(m.gender, ''''), u.gender),
                    work_type = COALESCE(NULLIF(m.work_type, ''''), u.work_type),
                    role_id = COALESCE(m.role_id, u.role_id),
                    date_of_birth = COALESCE(m.date_of_birth, u.date_of_birth),
                    joining_date = COALESCE(m.joining_date, u.joining_date),
                    is_exited = COALESCE(m.is_exited, u.is_exited)
                FROM members m
                WHERE LOWER(TRIM(u.email)) = LOWER(TRIM(m.email))';
        ELSIF has_member_type THEN
            EXECUTE '
                UPDATE users u
                SET 
                    phone = COALESCE(NULLIF(m.phone, ''''), u.phone),
                    gender = COALESCE(NULLIF(m.gender, ''''), u.gender),
                    work_type = COALESCE(NULLIF(m.member_type, ''''), u.work_type),
                    role_id = COALESCE(m.role_id, u.role_id),
                    date_of_birth = COALESCE(m.date_of_birth, u.date_of_birth),
                    joining_date = COALESCE(m.joining_date, u.joining_date),
                    is_exited = COALESCE(m.is_exited, u.is_exited)
                FROM members m
                WHERE LOWER(TRIM(u.email)) = LOWER(TRIM(m.email))';
        ELSE
            EXECUTE '
                UPDATE users u
                SET 
                    phone = COALESCE(NULLIF(m.phone, ''''), u.phone),
                    gender = COALESCE(NULLIF(m.gender, ''''), u.gender),
                    role_id = COALESCE(m.role_id, u.role_id),
                    date_of_birth = COALESCE(m.date_of_birth, u.date_of_birth),
                    joining_date = COALESCE(m.joining_date, u.joining_date),
                    is_exited = COALESCE(m.is_exited, u.is_exited)
                FROM members m
                WHERE LOWER(TRIM(u.email)) = LOWER(TRIM(m.email))';
        END IF;
    END IF;
END $$;

-- ----------------------------------------------------------------------------
-- Step 4: Insert members that do NOT exist in 'users' yet
-- Uses unique username suffix to prevent username unique collisions
-- ----------------------------------------------------------------------------
DO $$
DECLARE
    has_work_type BOOLEAN;
    has_member_type BOOLEAN;
    wt_expr TEXT := '''Office''';
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'members') THEN
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'members' AND column_name = 'work_type'
        ) INTO has_work_type;

        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'members' AND column_name = 'member_type'
        ) INTO has_member_type;

        IF has_work_type THEN
            wt_expr := 'COALESCE(NULLIF(m.work_type, ''''), ''Office'')';
        ELSIF has_member_type THEN
            wt_expr := 'COALESCE(NULLIF(m.member_type, ''''), ''Office'')';
        END IF;

        EXECUTE '
            INSERT INTO users (
                user_id,
                username,
                email,
                password_hash,
                role,
                full_name,
                is_active,
                phone,
                gender,
                work_type,
                role_id,
                date_of_birth,
                joining_date,
                is_exited,
                is_deleted,
                created_by,
                created_at,
                created_on,
                modified_by,
                modified_on,
                is_first_login
            )
            SELECT 
                m.member_id,
                LOWER(REGEXP_REPLACE(SPLIT_PART(m.email, ''@'', 1), ''[^a-zA-Z0-9_]'', ''_'', ''g'')) || ''_'' || SUBSTRING(REPLACE(m.member_id::text, ''-'', ''''), 1, 4),
                m.email,
                '''',
                COALESCE(r.role_name, ''Member''),
                m.name,
                m.is_active,
                COALESCE(m.phone, ''''),
                COALESCE(m.gender, ''Male''),
                ' || wt_expr || ',
                m.role_id,
                COALESCE(m.date_of_birth, CURRENT_DATE),
                COALESCE(m.joining_date, CURRENT_DATE),
                COALESCE(m.is_exited, FALSE),
                COALESCE(m.is_deleted, FALSE),
                m.created_by,
                COALESCE(m.created_at, CURRENT_TIMESTAMP),
                COALESCE(m.created_on, m.created_at, CURRENT_TIMESTAMP),
                m.modified_by,
                m.modified_on,
                TRUE
            FROM members m
            LEFT JOIN roles r ON m.role_id = r.role_id
            WHERE NOT EXISTS (
                SELECT 1 FROM users u WHERE LOWER(TRIM(u.email)) = LOWER(TRIM(m.email))
            )
            ON CONFLICT (email) DO NOTHING';
    END IF;
END $$;

-- ----------------------------------------------------------------------------
-- Step 5: Repoint FK values in contributions & event_participants to match users.user_id
-- ----------------------------------------------------------------------------
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'members') THEN
        UPDATE contributions c
        SET member_id = u.user_id
        FROM members m
        JOIN users u ON LOWER(TRIM(m.email)) = LOWER(TRIM(u.email))
        WHERE c.member_id = m.member_id AND c.member_id <> u.user_id;

        UPDATE event_participants ep
        SET member_id = u.user_id
        FROM members m
        JOIN users u ON LOWER(TRIM(m.email)) = LOWER(TRIM(u.email))
        WHERE ep.member_id = m.member_id AND ep.member_id <> u.user_id;
    END IF;
END $$;

-- ----------------------------------------------------------------------------
-- Step 6: Drop old foreign key constraints referencing 'members'
-- ----------------------------------------------------------------------------
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN (
        SELECT tc.table_name, tc.constraint_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.constraint_column_usage ccu 
          ON ccu.constraint_name = tc.constraint_name
        WHERE ccu.table_name = 'members' AND tc.constraint_type = 'FOREIGN KEY'
    ) LOOP
        EXECUTE 'ALTER TABLE ' || quote_ident(r.table_name) || ' DROP CONSTRAINT IF EXISTS ' || quote_ident(r.constraint_name);
    END LOOP;
END $$;

-- ----------------------------------------------------------------------------
-- Step 7: Clean orphan records (if any) and add new FK constraints referencing 'users(user_id)'
-- ----------------------------------------------------------------------------
DO $$
BEGIN
    -- Delete any dangling participant/contribution referencing non-existent users
    DELETE FROM contributions WHERE member_id NOT IN (SELECT user_id FROM users);
    DELETE FROM event_participants WHERE member_id NOT IN (SELECT user_id FROM users);

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_contributions_users_member_id'
    ) THEN
        ALTER TABLE contributions
        ADD CONSTRAINT fk_contributions_users_member_id
        FOREIGN KEY (member_id) REFERENCES users(user_id)
        ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_event_participants_users_member_id'
    ) THEN
        ALTER TABLE event_participants
        ADD CONSTRAINT fk_event_participants_users_member_id
        FOREIGN KEY (member_id) REFERENCES users(user_id)
        ON DELETE RESTRICT;
    END IF;
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Notice: FK constraint creation skipped or already exists: %', SQLERRM;
END $$;

-- ----------------------------------------------------------------------------
-- Step 8: Create index on users (role_id, is_active)
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS ix_users_role_id_is_active ON users (role_id, is_active);

-- ----------------------------------------------------------------------------
-- Step 9: Drop the 'members' table
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS members CASCADE;
