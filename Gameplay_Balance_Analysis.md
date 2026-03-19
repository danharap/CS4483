# Game Play Balance Analysis (Team Submission)

**Team name:** \<TEAM NAME\>  
**Game title:** \<GAME TITLE\>  
**Team members:** \<NAMES\>  

This report follows the required structure from “Activity (Game Play Balance Analysis)”. Replace fields in \<angle brackets\> with your team’s real data. Keep the main body to ~2 pages.

---

## 1) Balance Problem Framing (1–3 issues)

### Issue A — Upgrade scaling can produce a dominant DPS strategy
**Mechanics:** The player’s base weapon starts at **20 damage**, **2 shots/second**, **14 projectile speed**, **1 projectile**, and **0 pierce** (`PlayerWeapon`). Upgrades stack as follows: **+Damage** multiplies damage by **1.20**, **+Attack Speed** multiplies fire rate by **1.15**, **+Pierce** adds **+1** pierce, and **+Multishot** adds **+1** projectile (`UpgradeManager` / `UpgradeData`). Enemy HP is scaled by **+12% per wave** (`EnemySpawner.hpScalePerWave = 0.12`).  
**Dynamics:** Because damage and fire rate are multiplicative, early offensive upgrades accelerate clear speed, which increases XP income and upgrade frequency. This creates a snowball pattern where the “best” path becomes self-reinforcing.  
**Aesthetics / player experience:** When offensive upgrades roll early the game can feel too easy; when they do not appear the run can feel RNG-gated. Players are incentivized to pick offensive cards even when they would prefer defense/utility because killing faster also reduces incoming contact damage.  
**Computational mechanics:** Upgrade selection is rarity-weighted each time the player levels up (`UpgradeManager.GetOptions`). Rarity weights are **Common = 6**, **Uncommon = 3 + 0.2·waveIndex**, **Rare = 1 + 0.15·waveIndex** (`UpgradeCatalogue.RarityWeight`), which increases the chance of high-impact upgrades later in a run and can cause power spikes.

### Issue B — Boss slam fairness is sensitive to telegraph time vs player mobility
**Mechanics:** The boss baseline is **500 HP**, **2.5 move speed**, **100 XP drop** (`BossEnemy`). The slam uses: **chargeTime 0.65s**, **jumpSpeed 10**, **jumpHeight 2.8**, **slamRadius 4.5**, **slamDamage 25**, **airContactDamage 15**, and **attackCooldown 5.5s**. The player baseline is **7 move speed** with dash **20 speed for 0.18s** and **1.5s cooldown** (`PlayerController`).  
**Dynamics:** The slam landing point is locked at the start of the charge, giving the player a reaction window. If the window is too short relative to movement/dash availability, the slam becomes effectively unavoidable; if too long, the slam becomes trivial and the boss loses its “peak challenge” role.  
**Aesthetics / player experience:** Players interpret unavoidable hits as unfair. Telegraph clarity and camera framing strongly influence whether the slam feels dodgeable and learnable.  
**Computational mechanics:** The slam includes a ground-circle telegraph and an impact ring. On hit, it applies a knock-up (player vertical velocity) for feedback, which increases spectacle but also increases punishment when combined with damage.

### Issue C — Post-wave-5 fast enemy mix can create difficulty spikes
**Mechanics:** Default contact damage is **10** with **1.0s cooldown** when an enemy is within **1.2 units** (`EnemyBase`). The enemy roster uses these baselines: Chaser **60 HP / 3.5 speed / 10 XP**, Fast **22 HP / 6 speed / 8 XP**, Heavy **180 HP / 2.0 speed / 18 XP**, and BigBat **65 HP / 6.2 speed / 14 XP** (`ChaserEnemy`, `FastEnemy`, `HeavyEnemy`, `BigBatEnemy`). BigBat begins spawning at **waveIndex 5** (Wave 6+) with a fixed mix after unlocks: **20% BigBat**, **25% Heavy**, **30% Fast**, remainder Chaser (`EnemySpawner.ChoosePrefab`).  
**Dynamics:** Introducing another fast unit after wave 5 increases the chance of multiple high-speed enemies reaching the player simultaneously. Because contact damage ticks repeatedly at a fixed interval, clustered arrivals can spike incoming damage.  
**Aesthetics / player experience:** Difficulty spikes feel unfair when they occur without clear warning or when dash cooldown/movement constraints can’t keep up with clustering.  
**Computational mechanics:** Spawn points reject locations within **5 units** of the player and attempt up to 10 picks. The mix probabilities, unlock timing, and spawn-point geometry together determine clustering frequency.

---

## 2) Evidence Collected (at least 2 sources)

### Evidence Source 1 — Quantitative (play sessions)
The repo includes built-in quantitative instrumentation via `PlaytestLogger` and `HighScoreManager`. Each run logs: **Time Survived**, **Waves Cleared**, **Total Kills**, **Time to First Kill**, and **Time to First Level Up** (`PlaytestLogger.LogSummary`). High score records persist best waves/time/kills (`HighScoreManager`). For this section, compile `playtest_log.txt` from `Application.persistentDataPath` across \<N sessions\> and compute summaries from the logged fields. These metrics are directly relevant to pacing (first kill/level-up timing) and overall challenge outcomes (waves/time/kills).

### Evidence Source 2 — Qualitative (observations + comments)
The repo does not automatically store player comments, so qualitative evidence must be collected during play sessions (notes/quotes). The most relevant themes for this game’s current systems are (1) whether offensive upgrades feel mandatory due to multiplicative DPS scaling, (2) whether boss slam telegraphs are noticed during the 0.65s charge, and (3) whether Wave 6+ enemy mix creates perceived “unfair spikes.” Summarize observations and include 2–3 representative quotes from \<N sessions\>.

#### Small summary table (example)

| Metric | Sessions | Mean | Range | Notes |
|---|---:|---:|---:|---|
| Waves reached | \<N\> | \<mean\> | \<min–max\> | \<e.g., spike at wave 6\> |
| Deaths per run | \<N\> | \<mean\> | \<min–max\> | \<boss slam implicated in X%\> |
| “Pierce” picked | \<N\> | \<%\> | — | \<dominant pick if >60%\> |

---

## 3) Statistical or Analytical Examination (pick ≥1)

### Option B — Descriptive statistics (recommended for this project)
The logger fields support descriptive statistics on time survived, waves cleared, total kills, time-to-first-kill, and time-to-first-level-up. Compute mean, median, and range for each metric across sessions. Separately, tally upgrade selections per run (since upgrades are not logged by default) and compute pick-rate percentages. A strong skew (e.g., one upgrade chosen >60%) or a strong association with waves cleared indicates a dominant strategy rather than healthy choice diversity.

### Option D — Qualitative thematic analysis (2–3 themes)
We grouped qualitative comments into recurring themes. Theme 1 (\<Telegraph clarity\>) is supported by \<quotes\> and suggests that players cannot parse danger quickly enough, producing perceived unfairness. Theme 2 (\<Upgrade necessity\>) is supported by \<quotes\> and suggests that players believe certain choices are mandatory, reducing meaningful decision-making and build variety.

---

## 4) Computational Balance Exploration (use or propose ≥1)

### Monte Carlo simulation (proposed)
We propose a Monte Carlo simulation that uses the repo’s actual parameters: enemy HP scaling (+12% per wave), the spawn mix probabilities (20% BigBat, 25% Heavy, 30% Fast, remainder Chaser after wave 5), and player DPS growth from upgrades (damage ×1.20 per pick; fire rate ×1.15 per pick; multishot +1 projectile; pierce +1). Under a simplified encounter model, the simulation can estimate time-to-clear and expected contact-damage exposure for different upgrade paths and test whether multiplicative DPS growth outpaces encounter pressure.

### Probability & randomness audit (optional add-on)
We would also audit randomness sources such as upgrade draws and spawn placement to check whether rare events are too rare to matter or whether unlucky sequences create difficulty spikes (e.g., clustered spawns near the player).

---

## 5) Proposed Balance Adjustments (concrete, evidence-linked)

### Adjustment A — Reduce dominant upgrade skew
If offensive upgrades dominate in logged outcomes and pick-rate tallies, tune the existing levers in the repo. Examples that map cleanly onto current code include reducing multiplicative stacking rates (damage +20% → +15%, fire rate +15% → +10%), slowing Uncommon/Rare weight growth in `RarityWeight`, or adding diminishing returns to pierce (e.g., the second/third pierce adds less effective value). These changes target the snowball dynamic created by multiplicative DPS growth plus faster XP gain.

### Adjustment B — Boss slam fairness/readability tuning
Boss slam tuning should start from the current values (charge 0.65s, radius 4.5, damage 25, cooldown 5.5s). Concrete adjustments include increasing charge time (e.g., 0.65s → 0.80–0.90s) if players cannot reposition reliably with 7 move speed, or reducing radius slightly if coverage is too high relative to arena space. If knock-up is retained for feedback (currently applied on hit), consider reducing slam damage so the hit is impactful without immediately ending the run.

### Adjustment C — Spawn mix after wave 5 (big bat introduction)
To smooth Wave 6+ spikes, adjust the existing spawn mix in `EnemySpawner.ChoosePrefab` (currently 20% BigBat, 25% Heavy, 30% Fast). If playtests show clustering damage spikes, reduce BigBat probability slightly or increase minimum player distance (currently 5) so fast enemies do not stack contact-damage ticks immediately. Because contact damage is 10 per second within 1.2 units, small clustering changes can have large fairness impact.

---

## 6) Expected Impact on Gameplay (evidence-based)

These adjustments are expected to improve fairness by increasing readability and reducing “unavoidable” hits (which should be reflected in a reduced share of deaths attributable to the boss slam). Challenge should become more consistent across runs, reducing variance in waves reached and deaths per run. Strategic diversity should increase as upgrade pick rates become less skewed (a concrete target is that no single upgrade exceeds 60% usage unless its trade-offs are clear). Engagement should improve because the boss wave remains a peak moment without becoming a hard stop. Frustration should decrease (fewer complaints about randomness or unfair spikes), while satisfaction should increase through more “learnable” dodges and visible counterplay. Replayability should improve through build variety and less of a solved meta. From a computational perspective, exploring parameter regions through simulation should reduce extreme outcomes under RNG and stabilize difficulty pacing.

---

### Optional Appendix (if needed)
If you run simulations, include raw output, extra tables, or screenshots in a separate appendix. The main report should still stand on its own.

