-- Migration: Add Sponsor Contribution Management for Events
-- 1. Add require_sponsor_contribution flag to events table
ALTER TABLE IF EXISTS public.events 
ADD COLUMN IF NOT EXISTS require_sponsor_contribution BOOLEAN NOT NULL DEFAULT FALSE;

-- 2. Create event_sponsors table
CREATE TABLE IF NOT EXISTS public.event_sponsors (
    sponsor_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES public.events(event_id) ON DELETE CASCADE,
    sponsor_user_id UUID NOT NULL REFERENCES public.users(user_id) ON DELETE RESTRICT,
    contribution_amount NUMERIC(12, 2) NOT NULL CHECK (contribution_amount > 0),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES public.users(user_id) ON DELETE SET NULL,
    modified_at TIMESTAMP WITH TIME ZONE,
    modified_by UUID REFERENCES public.users(user_id) ON DELETE SET NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_event_sponsors_event_id ON public.event_sponsors(event_id);
CREATE INDEX IF NOT EXISTS idx_event_sponsors_user_id ON public.event_sponsors(sponsor_user_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_event_sponsors_single_active 
    ON public.event_sponsors(event_id) 
    WHERE is_active = TRUE AND is_deleted = FALSE;

-- 3. Create event_sponsor_histories table
CREATE TABLE IF NOT EXISTS public.event_sponsor_histories (
    history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sponsor_id UUID NOT NULL REFERENCES public.event_sponsors(sponsor_id) ON DELETE CASCADE,
    event_id UUID NOT NULL REFERENCES public.events(event_id) ON DELETE CASCADE,
    sponsor_user_id UUID NOT NULL REFERENCES public.users(user_id) ON DELETE RESTRICT,
    previous_amount NUMERIC(12, 2),
    contribution_amount NUMERIC(12, 2) NOT NULL,
    change_type VARCHAR(50) NOT NULL, -- 'CREATED', 'UPDATED', 'DEACTIVATED'
    action_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    action_by UUID REFERENCES public.users(user_id) ON DELETE SET NULL,
    notes TEXT
);

CREATE INDEX IF NOT EXISTS idx_event_sponsor_histories_event_id ON public.event_sponsor_histories(event_id);
CREATE INDEX IF NOT EXISTS idx_event_sponsor_histories_sponsor_id ON public.event_sponsor_histories(sponsor_id);





BEGIN;

-- 1. Drop the existing foreign key constraint
ALTER TABLE public.event_sponsor_histories
DROP CONSTRAINT IF EXISTS event_sponsor_histories_sponsor_user_id_fkey;

-- 2. Rename sponsor_user_id to user_id
ALTER TABLE public.event_sponsor_histories
RENAME COLUMN sponsor_user_id TO user_id;

-- 3. Add the foreign key constraint again
ALTER TABLE public.event_sponsor_histories
ADD CONSTRAINT event_sponsor_histories_user_id_fkey
FOREIGN KEY (user_id)
REFERENCES public.users (user_id)
ON UPDATE NO ACTION
ON DELETE RESTRICT;

COMMIT;




BEGIN;

-- 1. Drop the existing foreign key constraint
ALTER TABLE public.event_sponsors
DROP CONSTRAINT IF EXISTS event_sponsors_sponsor_user_id_fkey;

-- 2. Rename sponsor_user_id to user_id
ALTER TABLE public.event_sponsors
RENAME COLUMN sponsor_user_id TO user_id;

-- 3. Re-create the foreign key constraint
ALTER TABLE public.event_sponsors
ADD CONSTRAINT event_sponsors_user_id_fkey
FOREIGN KEY (user_id)
REFERENCES public.users (user_id)
ON UPDATE NO ACTION
ON DELETE RESTRICT;

COMMIT;