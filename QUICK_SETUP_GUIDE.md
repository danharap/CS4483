# Quick Setup Guide - Apply All Fixes

## Pull Latest Changes

```bash
git pull origin dev
```

---

## Step-by-Step Setup in Unity

### 1. Resize Your Custom Assets (ONE-TIME ONLY)
**Important:** Do this FIRST before any other setup!

In Unity menu:
```
CS4483 → 🔧 Resize Pickup Sprites to 40x40
```

This downscales your custom Experience orb and Medkit from 1024x1024 to 40x40.

---

### 2. Delete Old Generated Assets

Delete these folders:
- `Assets/Prefabs`
- `Assets/Materials`

---

### 3. Run Setup Scripts in Order

Run these menu items **in this exact order:**

```
1. CS4483 → 🎨 1. Slice Sprite Sheets
2. CS4483 → 🎨 2. Apply Sprites to Prefabs
3. CS4483 → SETUP EVERYTHING (Run This First!)
4. CS4483 → 🎨 3. Apply Sprites to Scene Objects
5. CS4483 → 🎨 4. Apply Environment Sprites
```

---

### 4. Press Play!

Test everything:
- Move with WASD - player should flip when moving left
- Aim with mouse - gun should point correctly in ALL directions
- Shoot - you should hear bullet sound
- Kill enemies - you should hear death sound
- Check floor - sMap.png inside walls, sBg.png outside
- Check pickups - XP orbs and medkits should be 40x40 with transparency

---

## What Was Fixed:

✅ **Gun aiming** - Correct rotation when facing left (up-left stays up-left)  
✅ **Gun offset** - 50% farther from player (0.6 instead of 0.4)  
✅ **Sprite flipping** - Player faces movement direction, gun flips properly  
✅ **Asset sizes** - XP orb and medkit now 40x40 (not 1024x1024!)  
✅ **Transparency** - No black backgrounds on pickups  
✅ **Map alignment** - Fits within border, background visible outside  
✅ **Audio** - Bullet and death sounds working with proper volume  

---

## Audio Details:

**Shooting Sound (`aBullet.wav`):**
- Plays on every shot
- Volume: 30%
- Uses `PlayOneShot()` to avoid stacking

**Death Sound (`aDeath.wav`):**
- Plays when enemy dies
- Volume: 50%
- Uses `PlayClipAtPoint()` at death location

---

## Troubleshooting:

**If sprites still look wrong:**
1. Make sure you ran the resize tool FIRST
2. Delete Prefabs and Materials folders
3. Re-run all setup steps in order

**If audio doesn't play:**
1. Check that audio files exist in `Assets/Audio/`
2. Re-run `SETUP EVERYTHING`
3. Audio is auto-wired in Step 10

**If transparency has black backgrounds:**
1. Re-run `🎨 1. Slice Sprite Sheets`
2. This reconfigures alpha settings

---

## New Tools Added:

**Image Resizer:**
`CS4483 → 🔧 Resize Pickup Sprites to 40x40`
- Downscales sExperience.png and sMedkit.png
- Preserves transparency and clean edges
- Only needs to run once

**Audio Setup (optional):**
`CS4483 → 🔊 Setup Game Audio`
- Manual tool to configure audio if needed
- Not required (SETUP EVERYTHING handles it)

---

All changes on `dev` branch! 🚀
