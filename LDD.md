# Level Design Document (LDD)
## CS4483 Activity 2 — Group 21 Graybox Prototype

**Game:** Untitled Top-Down Roguelike Wave Survival  
**Team:** Aydan Karmali · Daniel Harapiak · Derek Yuan · Zachary Goodman  
**Date:** March 2026  

---

## 1. Level Overview

**Level Name:** Main_Graybox  
**Level Type:** Arena survival — single continuous space, semi-open  
**Player Goal:** Survive escalating enemy waves, level up, defeat bosses  
**Tone:** Tense, action-heavy, empowering

The level is a bounded 50×50 unit arena divided into functional zones: a central
hub (player spawn), three combat pockets connected by choke-point corridors,
two elevated platforms, and a locked boss arena in the south. The design is
deliberate: every zone serves a specific gameplay purpose (wayfinding, pacing,
cover, or boss escalation).

---

## 2. Design Decisions

### 2.1 Genre & Game Type
**Choice:** Top-down roguelike action survival — fast-paced, arcade-style combat  
**Justification:** Chosen in Activity 1 as the highest-scoring concept (23/25). The core mechanic
is continuous movement under constant enemy-wave pressure with upgrade-based progression.
The level layout must support open movement lanes, multi-directional enemy approaches,
minimal downtime, and clear visuals under high enemy density.

### 2.2 2D vs. 3D Decision
**Choice:** Top-down perspective within a 3D environment  
**Justification:** Although gameplay functions similarly to a 2D arena, building in 3D allows
elevation and vertical layering, flexible camera control, and dynamic lighting. The
top-down camera supports strong spatial clarity, high combat readability, and smooth
decision-making during dense encounters — giving a good balance between simplicity
and technical flexibility.

### 2.3 Level Structure: Semi-Open Arena
**Choice:** Semi-open arena — player has full freedom of movement within clearly defined boundaries  
**Justification:** There are no branching paths or multiple rooms. The challenge comes from
increasing enemy spawns, enemy variation, and positional decision-making. This
structure simplifies AI pathfinding, reduces navigation confusion, and keeps focus on
combat mastery. The distinct combat zones (hub, flanking areas, boss arena) are
sub-areas within the single continuous space — not separate rooms.

**Layout diagram (top-down):**
```
    [NORTH ZONE - Green / Healing]
           ||
    [W]---HUB---[E]
           ||
   [WEST FLANK-Red]  [EAST FLANK-Yellow]
           ||
     [GATE DOOR]
           ||
      [BOSS ARENA]
```
*(All zones are open within the 50×50 arena — choke corridors define flow without splitting space.)*

### 2.4 Verticality: Moderate
**Choice:** Small elevated platforms accessible via ramps (no stairs); slight height differences  
**Justification:** Verticality is intentionally limited for better top-down experience. It prevents
the space from feeling flat and static while creating positional advantages and
micro-strategic depth. Ramps (not stairs) preserve smooth movement flow. Platforms
provide a tactical option but are not mandatory for navigation.

**Platforms:**
- **NE Platform** (+1.75 height) — overlooks the eastern flank and central hub corridor
- **SW Platform** (+1.75 height) — overlooks the western flank and boss gate approach

### 2.5 Navigation & Wayfinding
**Choice:** Central landmarks + distinct edge lighting zones + clear spatial boundaries  
**Justification:** Because the level is an arena, navigation is less about directional guidance
and more about spatial clarity. Players need to maintain orientation under pressure.
We support readability through central landmarks visible from all angles, distinct
lighting zones around the arena edges, clear spawn indicators at perimeter points,
and strong contrast between the play space and boundaries.

| Color | Zone | Meaning |
|-------|------|---------|
| Blue (pillar + white light) | Central Hub | "You are here / safe spawn zone" |
| Green (pillar + green light) | North Pocket | "Healing / safe zone" |
| Yellow (pillar + yellow light) | East Pocket | "Upgrade / reward area" |
| Red (pillar + red light) | West Pocket | "Danger / boss approach" |
| Dark red (arena floor + red light) | Boss Arena | "Boss encounter zone" |

The structural layout also guides the player naturally: corridors funnel movement,
pockets feel like distinct destinations, and the gate door creates a clear
"not yet accessible" barrier that becomes a goal.

### 2.6 Challenge & Pacing
**Choice:** Layered difficulty scaling — density, elite types, spawn variation, boss milestones  
**Justification:** Based on paper prototype feedback (Deliverable 1), waves 4–6 produced an
abrupt difficulty spike. To address this, we designed a staged escalation system:
- Increased enemy density (spawn interval decreases 10% per wave)
- Introduction of elite enemy types (Fast enemy unlocks at Wave 3)
- Spawn pattern variation (10 perimeter spawn points, random distribution)
- Boss milestones at Wave 5, 10, 15...

The pacing follows the rhythm established in our design document:  
**Pressure → Upgrade → Stabilization → Escalation → Boss → Reward → Repeat**

The 3-second break window between waves is intentionally short — a brief exhale
without removing tension, keeping the player in a near-constant flow state.

### 2.7 Enemy & Encounter Placement
**Choice:** Multi-directional perimeter spawn points (10 total); combat zones defined by occlusion walls  
**Justification:** Spawn points are placed around the outer perimeter so enemies always
approach from multiple directions. This prevents corner-camping (dominant strategy
identified in paper playtesting). Combat zones are created through cover placement
and sightline objects rather than room separation, consistent with the semi-open
structure. Enemies never teleport inside play zones — they navigate inward from edges.

**Enemy types:**
| Type | HP | Speed | Unlock | Behavior |
|------|----|-------|--------|----------|
| Chaser (red) | 60 | 3.5 | Wave 1 | Direct movement toward player |
| Fast (orange) | 30 | 6.0 | Wave 3 | Same direction, higher speed |
| Boss (purple) | 500 | 2.5 | Wave 5, 10... | Slower movement + charge ability |

### 2.8 Sightlines & Occlusion
**Choice:** 6 occlusion walls scattered across the arena + corridor choke-points  
**Justification:** Controlling what the player sees at any moment creates suspense, forces
repositioning, and prevents the arena from feeling trivial. Our occlusion design:

1. **Corridor choke-points** — enemies approaching via corridors are hidden until
   they emerge, creating ambush pressure and rewarding the player for holding angles
2. **Scattered blocky occluder walls** — break direct cross-arena sightlines;
   players on raised platforms gain some advantage but cannot see all threats,
   incentivizing movement over static play
3. **Boss arena gate** — fully occludes the boss until the gate opens, turning
   a mechanical event (gate rises) into a dramatic reveal moment

### 2.9 Environmental Storytelling
**Choice:** Color-coded materials (graybox color language) convey zone purpose without art assets  
**Justification:** Graybox prototypes intentionally avoid narrative assets, but color is a
zero-cost storytelling layer achievable with primitive materials:

| Color | Zone | Communicated Meaning |
|-------|------|---------------------|
| Green floor + pillar | North zone | Safe / healing |
| Yellow floor + pillar | East zone | Reward / upgrade area |
| Red floor + pillar | West zone | Danger / proceed with caution |
| Dark red floor | Boss arena | High stakes encounter |

Universal gaming color conventions mean players internalize this immediately without
instruction, satisfying the LDD requirement for environmental communication in graybox form.

---

## 3. Interactive Elements

### 3.1 Gate Door (Option A — selected)
- **Location:** South corridor, between hub and boss arena (0, 0, -12.8)
- **Function:** Blocks access to boss arena. Opens (rises upward) when a boss wave starts.
- **Design intent:** Creates a moment of anticipation. When the gate rises, the player
  knows a boss is coming. The mechanical gating prevents early exploration of
  the boss arena and directs flow.

### 3.2 Healing Shrine
- **Location:** North Pocket (green zone)
- **Function:** Press E when adjacent to heal +20 HP (30s cooldown)
- **Design intent:** Rewards players who navigate to the green zone during waves.
  Creates a risk/reward decision: move to the shrine (vulnerable en route) vs.
  stay mobile. Reinforces the green zone as a meaningful destination.

---

## 4. Level Flow Loop

The pacing follows our established design rhythm:

```
Pressure → Upgrade → Stabilization → Escalation → Boss → Reward → Repeat
```

In-run flow:
```
SPAWN at Hub → Survive enemy wave (WASD + auto-attack) → Kill enemies → Collect XP orbs
  → Level up: PAUSE → choose upgrade → UNPAUSE → continue wave →
Wave timer ends (3s break) → Boss wave 5: Gate opens → Kill boss → Boss upgrade reward →
Wave 6 begins with increased difficulty
```

Alternate sub-loop (player decision):
```
Take damage → Decide: keep fighting (risk death) 
                  OR reposition to North zone to use Healing Shrine (+20 HP, 30s cooldown)
```

---

## 5. Annotated Map

> **Note for submission:** Replace this text-map with a screenshot from the Unity editor
> (top-down overhead view of the full scene) with annotations drawn in Figma, PowerPoint,
> or any image editor. Label: spawn points, zone colors, platforms, gate, healing shrine,
> occlusion walls, and corridors.

```
         [SP]    [SP]    [SP]
          ┌────────────────┐
          │ ● GREEN PILLAR │  ← North Zone (GREEN)
          │  [HealShrine]  │     safe zone / healing
          └───────┬────────┘
         [SP]   ║ ║   [SP]     ← Choke corridor (occlusion)
     ┌──────────┴──┴──────────┐
     │   ■     [HUB]     ■    │  Central Hub (WHITE + Blue pillar)
     │  ■■  ●BluePillar  ■■   │  Player spawns here
     │   ■                ■   │  ■ = occlusion wall
     └────┬──────────┬────────┘
  [WEST]  ‖          ‖  [EAST]
  FLANK   ‖          ‖  FLANK
  (RED)  [SP]      [SP]  (YELLOW)
   ↑ danger /              ↑ reward /
   pre-boss zone           upgrade area

          ║ ║    ← Choke corridor
        [GATE DOOR]  ← Opens on boss wave
          ║ ║
     ┌────┴──┴────┐
     │ BOSS ARENA │  ← Dark red floor
     │  ● Red Pillar │
     └────────────┘

  ↗ NE Raised Platform (+1.75)    [SP]
      ↖ ramp connects to East flank
  ↙ SW Raised Platform (+1.75)    [SP]
      ↗ ramp connects to West flank
```

*[SP] = enemy spawn point (10 total around perimeter)*

---

## 6. Playtesting Insights

> **Note for submission:** Fill this section after conducting playtests with your graybox.
> Suggested format below — record observations during at least 2 playtests.

### Questions to observe/ask testers:
1. Did you understand the goal within 30 seconds (without being told)?
2. Did the color coding (green = heal, yellow = reward, red = danger) feel intuitive?
3. Did you use the raised platforms? Did the ramp feel easy to find?
4. Was the boss reveal (gate opening) a noticeable / memorable moment?
5. Did the corridor choke-points create meaningful positioning decisions?
6. On a scale of 1–5, how readable was the arena from the default camera angle?
7. Did the upgrade choices feel impactful / easy to understand?

### Observations (to be filled after testing):

| Issue Observed | Source | Planned Adjustment |
|----------------|--------|--------------------|
| *e.g. Players kept getting stuck at ramp* | Tester A | Widen ramp to 5 units |
| *e.g. Boss arrived too quickly* | Tester B | Increase base wave duration |
| | | |

### Key data to collect during testing:
- Survival time (first run)
- Waves cleared
- Time to first upgrade selection
- Most common cause of death
- Did tester use the Healing Shrine at least once?

---

## 7. Metrics & Success Criteria

Based on Activity 1 prototype and Deliverable 1 success criteria:

| Metric | Target | Rationale |
|--------|--------|-----------|
| Time to understand goal | < 30 seconds | Core loop should be self-evident |
| Survival time (first run) | 2–5 minutes | Not trivially easy, not immediately hopeless |
| Time to select upgrade | < 10 seconds | Card UI must be readable without explanation |
| Corner-camping dominant strategy | Not observed | Multi-directional spawning should prevent this |
| Boss wave emotional impact | Noticeable pause/reaction | Gate mechanic signals importance |
| Player navigates to shrine at least once | Observed in 50%+ of runs | Zone wayfinding is working |

---

## 8. Future Iterations (Post-Prototype)

- Bake full NavMesh so enemies navigate platforms and ramps properly
- Introduce a 4th elite enemy type (armored, slow but high HP) for waves 7+
- Sound design: ambient low drone in boss arena, chime when shrine activates, pitch shift on wave escalation
- Visual feedback: screen shake on boss charge, particle burst on enemy death, level-up flash
- Second level layout (if scope allows): outdoor tileset with cliff edges and bridges for enhanced verticality
- Meta-progression: boss drop currency feeds into permanent skill tree (Deliverable 2 scope)
