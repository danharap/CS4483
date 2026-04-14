# Cloud accounts & saves (Supabase)

This project uses **[Supabase](https://supabase.com)** for production-style auth and persistence:

- **PostgreSQL** stores save rows; **Supabase Auth** handles sign-up / sign-in (passwords are hashed by Supabase, not stored in game code).
- **Row Level Security (RLS)** ensures each user only sees and mutates their own `game_saves` rows.
- A **database trigger** rejects inserts when a user already has **3 saves** (enforced server-side, not only in the Unity UI).

## Why Supabase

The game is a **Unity desktop** client with no existing custom backend. Hosting a bespoke API + DB would add deployment and ops overhead for a course project. Supabase provides hosted Postgres, JWT-based auth compatible with `UnityWebRequest`, and RLS for authorization rules close to the data.

## One-time setup

1. Create a free Supabase project at https://supabase.com  
2. In the SQL editor, run the migration:  
   `Server/supabase/migrations/001_game_saves.sql`  
3. In **Project Settings → API**, copy:
   - **Project URL** (e.g. `https://xxxx.supabase.co`)
   - **anon public** key  
4. In Unity, create **Assets → Create → CS4483 → Cloud Backend Config** (or add a `CloudBackendConfig` ScriptableObject under `Assets/Resources/` named **`CloudBackendConfig`**) and paste:
   - `Supabase Url`
   - `Supabase Anon Key`
   - Enable **`Use Cloud Saves`**

Without a valid config, the main menu shows cloud saves as disabled and the legacy local flow (PlayerPrefs meta progression) still works.

## Environment / secrets

Do **not** commit real keys. Keep the ScriptableObject out of git or use a local override. The **anon** key is safe to embed in a shipped client only together with RLS policies (as configured here); service role keys must never ship in the client.

## Unity flow

- Main menu **Cloud panel**: sign up, sign in, list saves, create (if &lt; 3), load, delete (with confirmation), logout.
- Session **refresh token** is stored in `PlayerPrefs` so “stay logged in” works across restarts until refresh fails (then the user signs in again).
- **Load** downloads the save `payload`, then loads `MainScene` and `GameSaveHydrator` applies world + player + meta state.

## Save payload versioning

`payload.version` (inside JSON) is incremented when the C# snapshot schema changes; see `GameSaveModels.cs` and extend migration logic in `GameSaveSerializer.cs` if needed.
