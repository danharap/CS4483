# Local accounts & saves (JSON, no signup services)

Accounts and up to **three save slots per user** are stored in a single JSON file on disk. **No Supabase, no API keys, no internet.**

## Where the file lives

- **Editor / builds:** `Application.persistentDataPath/CS4483/local_accounts.json`  
  On Windows this is typically under `AppData\LocalLow\<Company>\<Product>\`.

Passwords are stored as **SHA-256 hashes** with a per-user salt (not plaintext).

## Optional starter file in the repo / build

If `local_accounts.json` does not exist yet, the game copies:

`StreamingAssets/DefaultLocalAccounts/accounts.json`

into that folder. The committed default is an **empty user list**; you can edit the JSON in the repo to pre-seed test accounts **only if** you generate matching `passwordSalt` and `passwordHash` (easiest is to sign up once in-game, then copy the generated user object from the created file into the template for teammates).

For a shipped `.exe`, the same `StreamingAssets` folder is beside the executable; the first run seeds from it if no save exists yet.

## Flow

1. Main menu **Sign up** / **Sign in**
2. **Create new save** (blocked at 3 slots) or **Load** / **Delete** with confirmation
3. Autosave runs when returning to the lobby or opening the main menu from gameplay (active slot only)

Legacy **New Game** / **Load Game** buttons still work without signing in (PlayerPrefs meta progression).
