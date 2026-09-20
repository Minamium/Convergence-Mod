---
doc_id: encounter.azure-cathedral.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-09-20
source_of_truth_for:
  - encounter.azure_cathedral.experience
  - encounter.azure_cathedral.presentation
aliases:
  - Cathedral of the White Night
  - Liora
  - Vitrion
related_code:
  - Content/Encounters/AzureCathedral
  - Client/Encounters/AzureCathedral
related_docs:
  - project.status
  - adr.0028
---

# Cathedral of the White Night

An independent, initially playable ice/glass/water Raid. Names are provisional: **Liora — Warden of Refraction**, an NPC-sized girl with light-blue side-ponytail hair, an elegant dress and glass sword; **Vitrion — The Glass Leviathan**, a giant articulated worm. This does not replace Doll, Vespera or Ghost Samurai. Current package and verification belong to [Status](../../STATUS.md).

## Start, ownership and recovery

- Craft **Glacial Chime / 氷硝子の鐘** at a Work Bench from30 Ice Blocks,15 Glass and3 Sapphires; reusable. Use it on the existing Foundation Core. Right-clicking the pedestal while holding the key also enters the shared key route. Item alt-use cancels preparation for the summoner.
- Admit all connected non-ghost players,1–8; solo is allowed. The grounded shared field appears during preparation; living participants see a small Ready button over themselves and Ready! over confirmed peers. Roster changes clear readiness. All must be alive/Ready to start.
- Same160×70-tile logical field as the other pedestal Raids, with the bottom exactly at pedestal ground. No tile generation/destruction; participants receive field-scoped infinite wing/rocket flight and no fall damage. Ordinary natural spawns are suppressed for participants. Late joiners are spectators, not added silently.
- The server/SP owns start, frozen connection-bound roster, targets, clocks, worm movement, HP/result, and exact-Fight cleanup. Native receiving-player damage is narrowly authorized by [ADR-0028](../../adr/0028-azure-cathedral-native-actors.md); normal defense, dodge, immunity and accessory hooks remain. No direct HP subtraction, new mandatory damage debuff, invulnerability or Doll Downed/Revive adapter.
- Normal deaths/disconnects remove that member from this attempt. Last retained living participant gone means Defeat. Both named actors defeated means Victory. Missing actors/segments invalidate rather than granting a kill. Cancellation, timeout, unload and partial-construction failure all release only that Fight's NPCs, projectiles and pedestal lease. A fresh invocation must work without world reload.

## First combat loop

Each phrase is7 seconds. The repeating first-pass order is:

| Phrase | Vitrion | Liora |
|---|---|---|
| 1 — Refracted pursuit | Three flank/locked-aim windups, accelerated charges and curved recovery | Sword anticipation → fan of traveling icicles |
| 2 — Glacial orbit | Circles the field while releasing a cyan mouth jet with continuous growth/sweep/contraction | Glass rain with two adjacent missing columns in each five-column group |
| 3 — Broken reflection | Another three-charge phrase; target rotates among surviving members | Repeated sword fans |
| 4 — White-night deluge | Second traveling/sweeping mouth jet | Offset glass rain; then loop |

Starting clocks/radii/source-damage values live only in `AzureRules`, `AzureRuntime.Schedule` and `AzureAttack.Geometry`. Forecasts and collisions use the same world geometry. Icicles travel along their warning direction; they do not become full-length damaging lasers. The beam is emitted from the moving head, not an unrelated fixed screen point. Its near-zero ignition and shrinking tail reduce collision along with visible width. Only the charge window gives the worm native contact damage; its silhouette remains visible during harmless orbit/recovery.

Actors have independent native HP; segments forward native hits to the worm head via `realLife`, not23 independent HP bars. One standard-compatible bar aggregates girl + worm. Initial solo total7.2m; each additional player adds3.9m, frozen at Ready acceptance. This is an uncalibrated initial endgame-equipment budget, not an observed DPS target. Aggregate half HP increases movement and fan/rain cadence without a new phase UI. Defeating one actor disables its attacks, leaves a dim remnant, and requires defeating the other. No bespoke drop or companion was requested in this slice.

## Presentation

- Preparation: world-space pale-cyan boundary and black exterior, glacial cathedral gradually emerges; Liora stands at the pedestal. No global UI/input setting changes.
- Start: an8-second protected opening after the common lead; small sword-bearing girl rises, the linked leviathan crystallizes into view, introduction bars/title hold through the entrance, then release the HUD. No oversized girl sprite.
- Vitrion:23 connected head/body/tail parts, shared joint spacing, native head steering and trailing body directions. Alternate stained-glass segment art, liquid caustics and local continuous scale flex; no whole-worm floating PNG. The mouth turns with the emitted beam and its shared muzzle anchor.
- Liora:8 authored small pixel poses (idle/blink/steps/draw/raise/release/hover), connected sprite root, fractional breathing/lean and bounded icy motes. This deliberately small NPC treatment is not claimed to be a full skeletal rig or a high-frame authored humanoid animation set.
- Luminance: existing original PortalBeam/RaidEnergy warning, dust, corona and mouth passes retinted cyan; new `AzureGlass` managed material animates segmented glass surfaces, faceted icicles and reflected-water cathedral shading. Noise textures are dependency-owned runtime references, not extracted/vendored. GPU state is scoped/restored. No server texture/audio loading.
- Short charge/strike punctuation reuses project-owned Doll PortalCharge/PortalFire, CoreHit, PhaseRupture and terminal cues, with feature-local gain/pitch and bounded voices. Reduced Effects/shake-off preserve hazard information. No repetitive fullscreen strobing.
- Field mask/Ready/button/letterbox transform world coordinates once into physical viewport pixels; UI scale is not reapplied. Bright/dark,107% UI and zoom/remote acceptance remain explicit playtest cases.
- Terminal: frozen hazards, cyan dissolution/waning glass material, local impact accent and temporary bars; cleanup is not owned or delayed by the client animation.

## Music

**Music: EigHt — 白夜に耀うステンドグラス.** [Creator video](https://www.youtube.com/watch?v=k0-SQQkRxis), [creator's BOOTH entry](https://bgm-cathedral.booth.pm/items/6112209), [governing terms](https://eight-novel.fanbox.cc/posts/7647818).

The owner supplied and selected this exact recording. On2026-09-20, the unrestricted official FANBOX `post.info` response (post7647818, updated2026-07-14) explicitly permitted game/video background use and editing, retaining EigHt's copyright. Rhythm-game inclusion has a separate contact requirement: this Raid is an action fight using background music, not note-scoring/chart gameplay. Do not claim that exception is blanket permission for a future rhythm-game mode. Standalone recording/streaming-service/Content-ID redistribution is not authorized. BOOTH/YouTube bodies were unavailable to the web fetch; no unseen track-specific conditions are claimed. Existing governing terms were independently read via the public creator API.

Use the complete owner-provided recording, not a short excerpt or cut climax. Export trims only leading/trailing silence, lowers gain2.9dB (measured source mean−13.0dB vs DollP1−15.9dB), and crossfades the final1.5seconds into the first1.5seconds. Native OGG loop metadata resumes just after that overlap. Native music mixing supplies entry/exit fades. It is background playback, not a sample-synchronized authority clock. Subjective seam/mix acceptance requires listening; no claim of having listened is made. [Attribution](../../../Assets/ATTRIBUTION.md#azure-cathedral--2026-09-20) owns exact export recipe, authorship and license separation.

## Assets and pending acceptance

Original built-in-generated images, reviewed and integrated into this feature: `Liora.png` (512×256, eight128px cells, rendered at NPC size), `Vitrion.png` (2×2 parts atlas), `Cathedral.png` (wide original matte), `GlacialChime.png` (48×60 item export). Source prompts and original/export identities are in [asset brief](ASSET_BRIEF.md); raw originals are retained externally, not deleted.

First owner smoke: hold Glacial Chime/use pedestal → Ready solo, then2+ matching clients; inspect visible girl, stable connected worm and mouth attachment, hit both with ordinary weapons, normal accessory damage reactions, charge warnings, rain gaps, music seam, total-HP-half escalation, deaths/cleanup and immediate repeat invocation. Confirm UI107%/zoom, Reduced Effects and large dash/teleport confinement. No new reward balance, rejoin substitution, companion or release certification is implied.
