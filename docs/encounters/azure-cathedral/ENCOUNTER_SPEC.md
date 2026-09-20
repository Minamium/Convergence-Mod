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

An independent, initially playable ice/glass/water Raid. Display names: **Liora — Warden of Refraction / リオラ**, an NPC-sized girl with light-blue side-ponytail hair, an elegant dress and glass sword; **Vitrion — The Glass Leviathan / ヴィトリオン**, a giant articulated worm. This does not replace Doll, Vespera or Ghost Samurai. Current package and verification belong to [Status](../../STATUS.md).

## Start, ownership and recovery

- Craft **Glacial Chime / 氷硝子の鐘** at a Work Bench from30 Ice Blocks,15 Glass and3 Sapphires; reusable. Use it on the existing Foundation Core. Right-clicking the pedestal while holding the key also enters the shared key route. Item alt-use cancels preparation for the summoner.
- Admit all connected non-ghost players,1–8; solo is allowed. The grounded shared field appears during preparation; living participants see a small Ready button over themselves and Ready! over confirmed peers. Roster changes clear readiness. All must be alive/Ready to start.
- Same160×70-tile logical field as the other pedestal Raids, with the bottom exactly at pedestal ground. No tile generation/destruction; participants receive field-scoped infinite wing/rocket flight and no fall damage. Ordinary natural spawns are suppressed for participants. Late joiners are spectators, not added silently.
- The server/SP owns start, frozen connection-bound roster, targets, clocks, worm movement, HP/result, and exact-Fight cleanup. Native receiving-player damage is narrowly authorized by [ADR-0028](../../adr/0028-azure-cathedral-native-actors.md); normal defense, dodge, immunity and accessory hooks remain. No direct HP subtraction, new mandatory damage debuff, invulnerability or Doll Downed/Revive adapter.
- Normal deaths/disconnects remove that member from this attempt. Last retained living participant gone means Defeat. Both named actors defeated means Victory. Each actor's defeat is accepted exactly once; the defeated worm's remaining chain is intentionally retired, and Liora continues. Missing **living required** actors/segments still invalidate rather than granting a kill. The worm is not required before its timed summoning cue. Cancellation, timeout, unload and partial-construction failure all release only that Fight's NPCs, projectiles and pedestal lease. A fresh invocation must work without world reload.

## First combat loop

Each phrase is8 seconds. The repeating first-pass order is:

| Phrase | Vitrion | Liora |
|---|---|---|
| 1 — Refracted pursuit | Two broad outside-flank approaches, locked charges across/out of the field, curved recovery | Sword anticipation → fan of traveling icicles |
| 2 — Glacial orbit | Circles the field while releasing a cyan mouth jet with continuous growth/sweep/contraction | Glass rain with two adjacent missing columns in each five-column group |
| 3 — Glacial covenant | Harmless wide orbit; no overlapping mouth/contact attack | Stack: fixed gathering site below Liora; paired ice jaws grow beside participants |
| 4 — Broken reflection | Two more full-field charges; target rotates among surviving members | Repeated sword fans |
| 5 — White-night deluge | Second traveling/sweeping mouth jet | Offset glass rain |
| 6 — Rift judgment | Harmless wide orbit | Spread: growing rift below each player, then judgment; loop |

Starting clocks/radii/source-damage values live only in `AzureRules`, `AzureRuntime.Schedule` and `AzureAttack.Geometry`. Forecasts and collisions use the same world geometry. Icicles travel along their warning direction; they do not become full-length damaging lasers. The beam is emitted from the moving head, not an unrelated fixed screen point. Its near-zero ignition and shrinking tail reduce collision along with visible width. Only the charge window gives the worm native contact damage; its silhouette remains visible during harmless orbit/recovery.

Actors have independent native HP; segments forward native hits to the worm head via `realLife`, not23 independent HP bars. One standard-compatible bar aggregates girl + worm. HP scaling remains in `AzureRules.Life`, frozen at Ready acceptance and not retuned from the interrupted first tests. Aggregate half HP increases movement and fan/rain cadence without a new phase UI. Defeating one actor disables its attacks and requires defeating the other; Liora's dim harmless remnant retains the session projection, whereas Vitrion's native chain is retired. No bespoke drop or companion was requested in this slice.

Stack/Spread use `AzureChorusRules` for warning, radii and damage budgets. The server resolves only announced, still-living connection-bound members at the deadline. Full gather / no overlap deals zero damage; missing Stack members proportionally increase the native source budget for the living party, and every overlapping Spread participant fails. Solo can satisfy both. Verdicts and impact positions are immutable and synchronized; clients never infer success from their own local positions. A bounded recipient-only hostile projectile delivers each failed impact through normal equipment/defense/dodge/immunity hooks. No direct life edit or immunity reset. Growing ice compresses and bursts on Stack failure, falls away on success; a light sword thrusts from the rift on Spread failure, while successful rifts dissipate.

## Presentation

- Preparation: retain the normal world background inside the pale-cyan boundary/black exterior. Liora sleeps in a hovering faceted ice prison at the field center. No global UI/input setting changes.
- Start: a16-second protected opening after the common lead. Sword anticipation → diagonal ice break → shards peel away and Liora awakens → sword raised skyward → light pillar and gradual cathedral reveal → tall dimensional rift → head-first segmented arrival. `AzureRules` owns cue ticks. Camera frames the center temporarily and blends back before control of combat; bars/title hold through the entrance, not a single-frame flash.
- Vitrion:23 connected head/body/tail parts, enlarged joint spacing, broad cubic windup and turn-limited recovery/orbit. It may travel beyond arena edges, not beyond the world. Dorsal-view paired armor/spikes have strict bilateral material sampling; no fish silhouette. Alternate stained-glass art, flowing caustics and low-amplitude continuous flex; no whole-worm floating PNG. The world renderer draws visible body parts even when the head is offscreen. Mouth angle and muzzle stay shared with the emitted beam.
- Liora: fixed at field center. Eight simplified native-density pixel poses (sealed/awake/blink/hover/draw/raise/release/recovery), aligned roots and small breathing/lean. No highly detailed illustration downscaled to NPC dimensions, full skeletal rig claim or invented high-frame authored set.
- Luminance: original PortalBeam/RaidEnergy warning, dust, corona and mouth passes retinted cyan; `AzureGlass` animates mirrored segmented glass, luminous faceted flying crystals, reflected-water cathedral shading, ice prisons/jaws, dimensional rifts and actual player/gather countdown circles. No ornamental Boss UI circles. Noise textures remain dependency-owned runtime references, not extracted/vendored. GPU state is scoped/restored. No server texture/audio loading.
- Short charge/strike punctuation reuses project-owned Doll PortalCharge/PortalFire, CoreHit, PhaseRupture and terminal cues, with feature-local gain/pitch and bounded voices. Reduced Effects/shake-off preserve hazard information. No repetitive fullscreen strobing.
- Field mask/Ready/button/letterbox transform world coordinates once into physical viewport pixels; UI scale is not reapplied. Bright/dark,107% UI and zoom/remote acceptance remain explicit playtest cases.
- Terminal: frozen hazards, cyan dissolution/waning glass material, local impact accent and temporary bars; cleanup is not owned or delayed by the client animation.

## Music

**Music: EigHt — 白夜に耀うステンドグラス.** [Creator video](https://www.youtube.com/watch?v=k0-SQQkRxis), [creator's BOOTH entry](https://bgm-cathedral.booth.pm/items/6112209), [governing terms](https://eight-novel.fanbox.cc/posts/7647818).

The owner supplied and selected this exact recording. On2026-09-20, the unrestricted official FANBOX `post.info` response (post7647818, updated2026-07-14) explicitly permitted game/video background use and editing, retaining EigHt's copyright. Rhythm-game inclusion has a separate contact requirement: this Raid is an action fight using background music, not note-scoring/chart gameplay. Do not claim that exception is blanket permission for a future rhythm-game mode. Standalone recording/streaming-service/Content-ID redistribution is not authorized. BOOTH/YouTube bodies were unavailable to the web fetch; no unseen track-specific conditions are claimed. Existing governing terms were independently read via the public creator API.

Use the complete owner-provided recording, not a short excerpt or cut climax. Export trims only leading/trailing silence, lowers gain2.9dB (measured source mean−13.0dB vs DollP1−15.9dB), and crossfades the final1.5seconds into the first1.5seconds. Native OGG loop metadata resumes just after that overlap. Native music mixing supplies entry/exit fades. It is background playback, not a sample-synchronized authority clock. Subjective seam/mix acceptance requires listening; no claim of having listened is made. [Attribution](../../../Assets/ATTRIBUTION.md#azure-cathedral--2026-09-20) owns exact export recipe, authorship and license separation.

## Assets and pending acceptance

Original built-in-generated images: `Liora.png` (192×128, eight48×64 cells drawn at native scale), `Vitrion.png` (1024×1024 symmetric-material2×2 parts atlas), unchanged `Cathedral.png` and `GlacialChime.png`. Source prompts and original/export identities are in [asset brief](ASSET_BRIEF.md); raw originals are retained externally, not deleted.

Next owner smoke: ordinary background/sealed girl → Ready → full awakening/rift → broad offscreen charges with visible trailing body; kill Vitrion first and finish Liora, then the opposite order on another run. Check Stack success/failure and Spread overlap with2+ matching clients (solo checks do not validate overlap), native accessory responses, body hit forwarding, death/cleanup and repeated invocation. Confirm title/camera return,107% UI/zoom, Reduced Effects and frame times. Prior interrupted logs and the outstanding music-listening acceptance are recorded in [evidence](../../evidence/2026-09-20-azure-awakening.json). No release certification is implied.
