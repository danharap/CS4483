# CS4483 — Wave Game (Unity)

## Cloud accounts & saves

This repo supports **optional** online accounts and up to **three save slots per user** via **Supabase** (PostgreSQL + hosted auth). Setup is documented here:

- **`Docs/CloudSave_Setup.md`** — create project, run SQL migration, configure Unity
- **`Server/supabase/migrations/001_game_saves.sql`** — schema, RLS, 3-save server limit

Without a `Resources/CloudBackendConfig` asset, the game behaves as before (local PlayerPrefs meta progression and legacy New Game / Load Game).

## Local development

Open the project in Unity, run **CS4483 → SETUP EVERYTHING** (or your team’s equivalent) to wire scenes and prefabs.
