# CS4483 Activity 2 – Group 21 Graybox Prototype
## Setup Instructions

> **Team:** Aydan Karmali · Daniel Harapiak · Derek Yuan · Zachary Goodman  
> **Due:** March 05, 2026 @ 11:30 PM (Brightspace)

---

## You only need to do 4 things. Everything else is automated.

---

## STEP 1 — Install Unity Editor via Unity Hub

1. Open **Unity Hub**
2. Click **Installs** on the left sidebar → **Install Editor**
3. Find **Unity 2022.3 LTS** (look for the "LTS" badge) → click **Install**
4. In the install options, make sure **"Visual Studio"** is checked if you want to edit code
5. Wait for the install to finish (this takes a while)

> If you already see a 2022.3.x version installed in your Installs tab, skip to Step 2.

---

## STEP 2 — Open the Project (No Cloning Needed!)

The project is already on your computer at:
```
/Users/danielharapiak/Desktop/SEM2/CS4483/CS4483
```

1. In Unity Hub, click **Projects** on the left sidebar
2. Click **Open** (top right) → **Add project from disk**
3. Browse to: `Desktop → SEM2 → CS4483 → CS4483`
4. Click **Add Project**
5. If Unity says **"This project was made with a different version"** → click **Open with 2022.3.x** (whichever you have installed)
6. Wait for Unity to import everything (first open takes 2–5 minutes)

---

## STEP 3 — Import TextMeshPro Essential Resources

This makes the UI text work. You only need to do it once.

1. In Unity (top menu): **Window → TextMeshPro → Import TMP Essential Resources**
2. Click **Import** in the dialog
3. Wait for it to finish

---

## STEP 4 — Run the One-Click Setup

1. In Unity (top menu): **CS4483 → ▶  SETUP EVERYTHING  (Run This First!)**
2. Wait ~5–10 seconds

**That's it.** This single click builds the entire level, creates all game objects, wires every connection, and bakes the NavMesh. Then press **Play**.

---

## Controls

| Input | Action |
|-------|--------|
| WASD | Move |
| Mouse | Aim |
| Left Shift | Dash |
| E (near green shrine) | Heal +20 HP |

---

## What You Should See When You Press Play

- Blue player capsule in a colored arena
- Red enemy capsules spawning from edges after ~2 seconds and chasing you
- Yellow projectiles auto-firing toward enemies
- HP bar (red, bottom-left) drops when enemies touch you
- XP bar (green, bottom-right) fills as enemies drop green orbs
- Wave number + countdown timer at top-center
- After enough kills: game **pauses** → 3 upgrade cards appear → click one to continue
- Every 5 waves: a gate rises in the south and a large **purple boss** spawns
- On death: Game Over panel with time/waves/kills + **Restart** button

---

## If Something Doesn't Work

| Problem | Fix |
|---------|-----|
| "CS4483" menu not visible | Unity is still compiling — wait for the progress bar at the bottom-right to finish |
| Red errors about `TMPro` | Do Step 3 (Import TMP Essential Resources) first, then re-run Step 4 |
| Enemies walk through walls | Playable anyway (direct movement fallback). To fix: **Window → AI → Navigation → Bake** |
| `SetupAll` errors on first run | Wait for all packages to compile fully, then run it again |
| Black screen / no UI | Re-run `CS4483 → ▶ SETUP EVERYTHING` — TMP resources may not have been imported yet |

---

## Tuning (No Code Edits Needed)

Select `=== MANAGERS ===` in the Hierarchy to tweak values in the Inspector:

| What | Field | Default |
|------|-------|---------|
| Wave length | WaveManager → Wave Duration | 60s |
| Max enemies at once | WaveManager → Enemy Count Cap | 30 |
| Boss every N waves | WaveManager → Boss Every N Waves | 5 |
| Player move speed | PlayerController → Move Speed | 7 |
| Player starting HP | PlayerHealth → Max HP | 100 |
| Projectile damage | PlayerWeapon → Damage | 20 |
| Fire rate | PlayerWeapon → Fire Rate | 2/sec |

---

## 3 Required Screenshots for Submission

Take these while in **Play mode** (or temporarily stop play for screenshot 1):

| # | What | How |
|---|------|-----|
| 1 | Top-down map view | Stop play → set Camera Rotation to (90, 0, 0), Y position to 40 → screenshot → restore to (60, 0, 0) and Y=16 |
| 2 | Player in corridor with occlusion | Walk into a corridor between zones, enemies visible behind a wall |
| 3 | Upgrade UI | Kill ~5 enemies to level up → screenshot the 3-card panel |

---

## Submission Checklist

- [ ] Runs without errors and is playable
- [ ] Level shows wayfinding (colored pillars + lights), verticality (ramps + platforms), occlusion (walls)  
- [ ] Waves, upgrades, boss, and game over all work
- [ ] 3+ screenshots saved to a `/Screenshots` folder
- [ ] `LDD.md` converted to PDF (open in browser and print → Save as PDF, or use any Markdown→PDF tool)
- [ ] Zipped as `Group21_GrayboxPrototype.zip`
