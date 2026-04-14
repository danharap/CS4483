-- CS4483 cloud saves + auth (Supabase)
-- Auth users live in auth.users (managed by Supabase Auth — passwords hashed server-side).
-- This migration adds per-user game saves with RLS and a hard cap of 3 rows per user.

create table if not exists public.game_saves (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  slot_label text not null default 'Untitled',
  payload jsonb not null,
  payload_version int not null default 1,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint game_saves_payload_is_object check (jsonb_typeof(payload) = 'object'),
  constraint game_saves_payload_size check (octet_length(payload::text) < 2000000)
);

create index if not exists game_saves_user_id_idx on public.game_saves (user_id);
create index if not exists game_saves_updated_at_idx on public.game_saves (updated_at desc);

-- Keep updated_at fresh on row changes
create or replace function public.set_game_saves_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

drop trigger if exists trg_game_saves_updated_at on public.game_saves;
create trigger trg_game_saves_updated_at
  before update on public.game_saves
  for each row
  execute procedure public.set_game_saves_updated_at();

-- Enforce max 3 saves per user at the database level (not only in the Unity client).
create or replace function public.enforce_max_three_saves_per_user()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  n int;
begin
  select count(*)::int into n from public.game_saves where user_id = new.user_id;
  if n >= 3 then
    raise exception 'MAX_SAVES_PER_USER'
      using detail = 'Each account may have at most 3 save slots.';
  end if;
  return new;
end;
$$;

drop trigger if exists trg_enforce_max_saves on public.game_saves;
create trigger trg_enforce_max_saves
  before insert on public.game_saves
  for each row
  execute procedure public.enforce_max_three_saves_per_user();

-- Default user_id from JWT on insert (client may omit user_id when using authenticated REST)
alter table public.game_saves
  alter column user_id set default auth.uid();

alter table public.game_saves enable row level security;

drop policy if exists "game_saves_select_own" on public.game_saves;
create policy "game_saves_select_own"
  on public.game_saves for select
  using (auth.uid() = user_id);

drop policy if exists "game_saves_insert_own" on public.game_saves;
create policy "game_saves_insert_own"
  on public.game_saves for insert
  with check (auth.uid() = user_id);

drop policy if exists "game_saves_update_own" on public.game_saves;
create policy "game_saves_update_own"
  on public.game_saves for update
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

drop policy if exists "game_saves_delete_own" on public.game_saves;
create policy "game_saves_delete_own"
  on public.game_saves for delete
  using (auth.uid() = user_id);
