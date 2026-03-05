# Narrative Brief — "Fractured Proving Grounds"
## CS4483 Activity 3 — Group 21

**Team:** Aydan Karmali · Daniel Harapiak · Derek Yuan · Zachary Goodman  
**Due:** March 11, 2026 @ 11:30 PM (Brightspace)

---

## The World: Fractured Proving Grounds

### Question 1 — What happened in this world before the player arrived?

The Proving Grounds were built by an ancient civilization called **The Architects** — engineers of war who believed true growth came only through combat. The arena was designed as a voluntary proving ground: warriors came to test themselves, earn glory, and leave stronger. For centuries, it functioned as intended.

The catastrophe came when a warrior smuggled in an artifact called the **Shard of Chaos** — a crystallized fragment of unstable reality. When the Shard shattered inside the arena walls, it fractured the local fabric of time and space. Every warrior who had ever died within those walls was trapped. Their souls could not leave. The Architects sealed the arena and fled, leaving behind only their **Watchers** — ancient crystalline pillar-constructs designed to maintain order.

The arena now exists in an **eternal loop**. Warriors are drawn in by a pull they can't explain, fight endless waves of spectral echoes (the trapped souls of past warriors, twisted into enemies), and either die and join the cycle or — theoretically — shatter the Shard remnant held by the Corrupted Champion to break the loop forever. No one has succeeded yet.

**The player is the latest warrior to be drawn in.** They don't know why they're here. The environment tells them everything they need to know.

---

### Question 2 — Who are the key figures or entities that shaped this world?

| Entity | Role | Represented In-Level |
|--------|------|---------------------|
| **The Architects** | Built the arena; long gone. Their engineering is still visible in the structured layout, boundary walls, and the Watcher pillars. | Level geometry itself — the arena's clean, deliberate structure |
| **The Watchers** | Ancient AI-constructs left behind to observe and maintain order. They are the colored landmark pillars, each one holding residual consciousness and lore. | The 5 colored pillars (Blue/Central, Green/North, Yellow/East, Red/West, Dark Red/Boss) |
| **Watcher Nara** | Keeper of Restoration. Weakening but still powers the Healing Shrine. | North zone green pillar + Healing Shrine |
| **Watcher Vex** | Keeper of Growth. Records warrior power and channels it into upgrade choices. | East zone yellow pillar |
| **Watcher Drak** | Keeper of Passage. Guards the threshold to the boss arena. | West zone red pillar + Gate Door |
| **The Corrupted Champion (Thorn)** | The first and greatest warrior to die after The Fracture. Their refusal to accept death transformed them into the boss entity. Holds the last Shard remnant. | Boss enemy (large purple figure) in the south arena |
| **The Echo Warriors** | Every warrior who died here. Their essence is recycled as the enemies the player fights each wave. | All enemy types — they are fragments of past warriors |

---

### Question 3 — How does the environment reflect this history?

The level was designed so every visual element *tells a story without words*:

**The Arena Layout:**
- The clean, deliberate structure (hub, corridors, pockets) reveals **Architect craftsmanship** — this was built with purpose, not random
- The boundary walls are intact — the Architects built well — but the floor shows cracks and scorched marks from centuries of combat cycles
- The southern gate being **closed** signals that something dangerous is locked away, not that the area doesn't exist

**Color as History:**
- **Blue (Central Hub):** The heart of the Grounds — neutral, informational, where the Architects placed their primary Watcher record
- **Green (North):** The last functioning restoration point — life, healing, the fading warmth of Watcher Nara
- **Yellow (East):** The record of growth — warm, promising, Watcher Vex's domain of accumulated power
- **Red (West):** Warning — danger ahead. Watcher Drak's territory and the approach to the boss threshold
- **Dark Red (Boss Arena):** Corruption, blood, the end of cycles — The Corrupted Champion's domain

**Echo Props (prop-based storytelling):**
- Broken swords and spears scattered around the arena floor = past warriors who fought and fell here
- Cracked shields = battles that didn't go well
- Ancient banners near the boss gate = ceremonial markers the Architects placed to honor warriors entering the final trial
- **Scorched ground marks at spawn zones** = where the echo warriors repeatedly materialize, leaving permanent burns on the floor from hundreds of cycles

**The Watchers themselves:** Their pillars are tall, structured, glowing — clearly artificial but clearly *ancient*. The Central pillar is pristine blue. The Boss Arena pillar is dark, corrupted red. Physically showing their degradation over time.

---

### Question 4 — What interactive elements reinforce this story without cutscenes?

| Element | Implementation | Story Function |
|---------|----------------|----------------|
| **Lore Pillars** | NarrativeTrigger zones around each Watcher pillar — approach to read lore text pop-ups | Each Watcher speaks directly to the player, revealing history and warning about Thorn |
| **Ambient Story Zones** | AmbientStoryZone trigger colliders at Boss Gate and Hub Center — brief text lines appear | Establishes atmosphere and sense of presence when entering key zones |
| **Healing Shrine** | Press E interaction — already implemented; the lore text at the shrine explains Watcher Nara | Player acts to heal, receives lore naturally as reward |
| **Gate Door** | Opens mechanically when boss wave starts | The *act* of the gate opening signals importance — no cutscene needed, the world reacts |
| **World Beacons** | WorldBeacon system — dormant lights activate wave-by-wave as player clears waves | Shows the player's combat prowess literally *restoring light* to the Grounds — player impact on world |
| **Boss Enemy Scale** | Boss is 2.5× larger than normal enemies, purple (corrupted), with a golden crown detail | Visual design communicates "this was a great warrior, now corrupted" without any text |
| **Enemy Types** | Red (chaser) = heavy warrior echo; Orange (fast) = scout echo; Purple boss = the Champion | Enemy color mirrors the danger/warning color scheme established by the pillars |

---

### Question 5 — Additional Design Questions (from class)

**What is the player's emotional arc?**  
The player arrives confused → learns through lore they're trapped in a cycle → feels the weight of responsibility → faces the boss that created the cycle → experiences either relief (if they win) or confirmation of futility (if they die, reinforcing the loop)

**How does mechanics reinforce narrative?**  
The wave survival loop IS the narrative loop. Each enemy killed is a soul freed briefly, then reformed. The upgrade system is literally absorbing the power of past warriors. Dying and restarting mirrors the cycle described in the lore. The game design and the story are the same system.

**What is the tone?**  
Dark but empowering. The world is broken, but the player gains power each run. The aesthetic aim is *tension toward triumph* — consistent with our MDA targets from Activity 1.

---

## Activity 3 Implementation Summary

### Step 2: Environmental Storytelling Elements

| Requirement | Implementation |
|-------------|----------------|
| **Prop-based storytelling** | `EchoProps` parent: broken swords, spears, cracked shields, ancient banners, scorched ground cylinders at spawn zones — all placed by `NarrativeSetup.cs` |
| **Light & Atmosphere** | Flickering `LightFlicker.cs` on the boss arena light; dormant→glowing `WorldBeacon.cs` on 5 beacons; reduced ambient light for atmosphere; colored zone lights already in place |
| **Decals & Details** | Scorched cylinder marks at all 4 spawn-heavy zones; dark echo materials for remnant props; scorched/dark floor in boss arena |

### Step 3: Interactive Narrative Triggers

| Requirement | Implementation |
|-------------|----------------|
| **Trigger Collider on location** | 5 Lore Pillar triggers + 2 Ambient Story Zone triggers (Boss Gate, Hub Center) |
| **TextMeshPro text pop-up** | `NarrativeTrigger.cs` — fade-in/fade-out panel with title + body text; displayed on Canvas_HUD NarrativePanel |
| **Audio Source (ambient sound)** | `AmbientStoryZone.cs` has an AudioSource component — wire any audio clip in inspector for the boss gate zone |

### Step 4: Dynamic Worldbuilding System

**Chosen: Player Impact on the World**

`WorldBeacon.cs` system: 5 beacon objects placed on platforms and in key zones. Each beacon starts completely dark (dormant). As the player clears waves (Wave 1, 2, 3, 4, 5), the corresponding beacon activates — its light turns on and its material transitions from near-black to a vivid color. This creates a visible record of the player's progress in the world space itself: **the Grounds literally light up as the player fights through them.**

This reinforces the narrative theme that the player's combat is *restoring* something — each defeated wave frees echoes, and the Watchers channel that freed energy into the dormant beacons.

---

## Playtesting Walkthrough (Step 5)

Peers explore the scene without prior knowledge. Ask them to guess:
1. What is the story of this place?
2. Who or what are the enemies?
3. What does the green zone mean vs. the red zone?
4. What happens when the gate opens?
5. What do the lights activating over time suggest?

**Intended vs. Actual perception check:** If players can infer "this is a cursed arena" and "the colored areas mean different things" from environment alone, the storytelling is working.

**Reflection questions:**
- Did the lore pillar text feel rewarding or interruptive during combat?
- Was the boss gate dramatic or just mechanical?
- Did the beacon activation feel meaningful or go unnoticed?
- What would you add to make the world feel more alive?
