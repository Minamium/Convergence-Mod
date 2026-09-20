---
doc_id: encounter.azure-cathedral.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-09-21
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
- Admit all connected non-ghost players,1–8; solo is allowed. Deploy the grounded field/black exterior before collecting Ready. As in Doll, use one fixed top-center physical-pixel Ready pill and separate Ready! labels over confirmed players, including self. Roster changes clear readiness. All must be alive/Ready to start.
- Same160×70-tile logical field as the other pedestal Raids, with the bottom exactly at pedestal ground. No tile generation/destruction; participants receive field-scoped infinite wing/rocket flight and no fall damage. Ordinary natural spawns are suppressed for participants. Late joiners are spectators, not added silently.
- The server/SP owns start, frozen connection-bound roster, targets, clocks, worm movement, HP/result, and exact-Fight cleanup. Native receiving-player damage is narrowly authorized by [ADR-0028](../../adr/0028-azure-cathedral-native-actors.md); normal defense, dodge, immunity and accessory hooks remain. No direct HP subtraction, new mandatory damage debuff, invulnerability or Doll Downed/Revive adapter.
- Normal deaths/disconnects remove that member from this attempt. Last retained living participant gone means Defeat, including during the protected transformation. Victory requires the enraged worm's defeat, not the initial two HP gates. Missing required actors/segments still invalidate rather than granting a kill. The worm is not required before its timed summoning cue; its chain remains owned through the melting ending. Cancellation, timeout, unload and partial-construction failure release only that Fight's NPCs, projectiles and pedestal lease. A fresh invocation must work without world reload.

## First combat loop

Each phrase is8 seconds. The repeating first-pass order is:

| Phrase | Vitrion | Liora |
|---|---|---|
| 1 — Refracted pursuit | Two broad outside-flank approaches, locked charges across/out of the field, curved recovery | Sword fans plus three frozen-target, full-field cyan spatial cuts |
| 2 — Glacial crossfire | Curved entry → six-second slow diagonal passage; segment-attached thin target lines and limited-homing energy volley during transit | Glass rain with two adjacent missing columns per five; a fixed-origin sweeping cyan sword beam |
| 3 — Glacial covenant | Harmless wide orbit; no overlapping mouth/contact attack | Stack: fixed gathering site below Liora; paired ice jaws grow beside participants |
| 4 — Broken reflection | Two more full-field charges; target rotates among surviving members | Repeated sword fans and cyan spatial cuts |
| 5 — White-night deluge | Mirrored slow passage and segment volley, without a stationary hold | Offset rain and second sword-beam sweep |
| 6 — Rift judgment | Harmless wide orbit | Spread: growing rift below each player, then judgment; loop |

Starting clocks/radii/source-damage values live only in `AzureRules`, `AzureRuntime.Schedule` and `AzureAttack.Geometry`. Icicles travel, not full-length instant lasers. **All sweeping beams originate at fixed Liora**, never the moving worm. Their initial direction is frozen by authority; the smooth sweep, growing/narrowing width and bell-shaped throat share collision geometry. Doll's P2 managed portal material supplies pulsing charge, a connected narrow throat and broad flowing jet. The worm only has contact damage during announced charges. Segment missiles lock their initial target/direction on authority, briefly steer with a capped turn rate, then travel ballistically; they cannot make a chasing U-turn. Each segment distributes its target over the living frozen roster.

Actors retain independent native HP scaled in `AzureRules.Life`, frozen at Ready. Worm base and per-player HP are one twentieth of the previous iteration; Liora is unchanged. **Duet accepts head hits only; Fury accepts head, body and tail hits through one native `realLife` HP pool.** Do not manually forward/subtract segment damage a second time. Native projectile piercing/immunity behavior remains intact; balance under equipped piercing weapons needs playtesting. The normal boss bar shows Liora + Vitrion, then only Vitrion in Fury. No bespoke drop or companion was requested.

## Devouring storm and ending

1. **Duet:** Vitrion cannot fall below20% of its maximum. Native hit limits plus authority/CheckDead guards preserve this floor even under burst/DoT. Liora can reach0 first, disabling her attacks while the worm remains active, or the worm can reach its floor first. Neither condition alone changes phase.
2. **Devouring:** when both gates are satisfied, clear every owned hazard and protect both actors. Hide HUD for this bounded scene and frame Liora raising her sword. First retreat to an off-field staging point, even if the previous head position overlapped her; pause/turn, then rush in from afar. Brake with visible distance left and complete the final approach slowly. Code-articulated upper/lower mandibles open before contact, close through the bite, and a single brief monochrome silhouette marks that instant only. No gore, repeated fullscreen strobing or client-decided transition.
3. **Fury:** after consumption, serrated dark-indigo/white-cyan pressure-glass armor reveals head-to-tail from the dedicated Fury atlas. Once the scene completes, refill the worm to100% exactly once. Liora no longer renders or attacks; her hidden native actor retains the exact-Fight projection. Faster long charges replace Liora's former covenant/judgment windows; slow diagonal segment volleys remain. Old-phase projectiles cannot become damaging again after the refill.
4. **Victory:** enraged worm0HP commits Victory, clears hazards and begins its harmless center rush. Glass softens, sags and dissolves progressively from head to tail into frost and droplets. Fade the existing BGM to zero over the ending, then release the field/session. The authority owns the ending duration/cleanup; clients cannot delay or grant the result. If everybody is already out when the lethal tick commits, Defeat wins.

Native shared-HP death must retain every segment shell until that cleanup, including the struck child's cached HP and late queued native hits. Retention is exact-Fight and does not heal the live head, subtract damage again or turn arbitrary missing actors into Victory. [ADR-0028](../../adr/0028-azure-cathedral-native-actors.md#2026-09-21-native-lifecycle-correction) owns this engine boundary.

Stack/Spread use `AzureChorusRules` for warning, radii and damage budgets. The server resolves only announced, still-living connection-bound members at the deadline. Full gather / no overlap deals zero damage; missing Stack members proportionally increase the native source budget for the living party, and every overlapping Spread participant fails. Solo can satisfy both. Verdicts and impact positions are immutable and synchronized; clients never infer success from their own local positions. A bounded recipient-only hostile projectile delivers each failed impact through normal equipment/defense/dodge/immunity hooks. No direct life edit or immunity reset. Growing ice compresses and bursts on Stack failure, falls away on success; a light sword thrusts from the rift on Spread failure, while successful rifts dissipate.

## Presentation

- Preparation: retain the normal world background inside the pale-cyan boundary/black exterior. Liora sleeps in a hovering faceted ice prison at the field center. No global UI/input setting changes.
- Start: a16-second protected opening after the common lead. Sword anticipation → diagonal ice break → shards peel away and Liora awakens → sword raised skyward → light pillar and gradual cathedral reveal → tall dimensional rift → head-first segmented arrival. `AzureRules` owns cue ticks. Camera frames the center temporarily and blends back before control of combat; bars/title hold through the entrance, not a single-frame flash.
- Vitrion:45 connected head/body/tail parts, twice the former chain's joint count, broad cubic windup and turn-limited recovery/orbit. It may travel beyond arena edges; the authority clamps the head inside the world. Trailing-chain behavior near the world edge still needs a game check. Dorsal-view armor/spikes retain strict bilateral material sampling; no fish silhouette. Alternate stained-glass art, flowing caustics, fury illumination and continuous flex; no whole-worm floating PNG. The world renderer draws visible body parts even when the head is offscreen.
- Liora: fixed at field center. Eight native-density pixel poses, aligned roots and small breathing/lean. The current atlas borrows **our own Nameless Doll's contrast hierarchy**, not her identity: light-blue side ponytail, shaded pale face, dark cobalt bodice, layered ice-blue skirt and restrained silver edges. Preserve small NPC scale, no highly detailed giant illustration, full skeletal rig claim or invented high-frame authored set.
- Luminance: original PortalBeam/RaidEnergy warning, dust, corona and mouth passes retinted cyan; `AzureGlass` animates mirrored glass, reflected water, ice prisons/jaws, rifts and countdown circles. Segment bolts are luminous leading hearts with streaming filaments and feathered vapor, not solid icicles; ordinary sword icicles/rain remain crystalline. Liora's spatial cuts share Vespera's `ScarletSorcery` tear material, cyan palette, continuous fine vibration and harmless contracting smoke; authoritative reach/width match the live material. Vespera retains its original red palette and gameplay. Frost and melting remain layered continuous materials. No ornamental Boss UI circles, vendored dependency textures or server texture/audio loading.
- Short charge/strike punctuation reuses project-owned Doll PortalCharge/PortalFire, CoreHit, PhaseRupture and terminal cues, with feature-local gain/pitch and bounded voices. Reduced Effects/shake-off preserve hazard information. No repetitive fullscreen strobing.
- The feature's full projection travels through the reliable definition-routed snapshot every6authority ticks, not just native NPC ExtraAI/netSpam. Stage, phase epoch, HP and membership are bounded/monotone; the shared scene/field lease tolerates a short transport gap without restarting. Explicit Idle/unload cancels immediately. NPC ExtraAI remains a repair path, not a second authority.
- Field mask/labels/letterbox transform world coordinates once into physical viewport pixels; Ready uses Doll's fixed viewport pill. UI scale is not reapplied. Black exterior remains through entrance/transformation/ending. Preparation selects silence; combat music retains its scene through the ending fade. Bright/dark,107% UI and zoom/remote acceptance remain explicit playtest cases.
- Reconcile the custom sky's actual requested state, not a second cached active flag. Native visual reset/deactivation must allow the same or next owned scene to reactivate; world unload still resets it. `SkyActivated` diagnostics record each actual activation/recovery without per-frame logging.

## Music

**Music: EigHt — 白夜に耀うステンドグラス.** [Creator video](https://www.youtube.com/watch?v=k0-SQQkRxis), [creator's BOOTH entry](https://bgm-cathedral.booth.pm/items/6112209), [governing terms](https://eight-novel.fanbox.cc/posts/7647818).

The owner supplied and selected this exact recording. On2026-09-20, the unrestricted official FANBOX `post.info` response (post7647818, updated2026-07-14) explicitly permitted game/video background use and editing, retaining EigHt's copyright. Rhythm-game inclusion has a separate contact requirement: this Raid is an action fight using background music, not note-scoring/chart gameplay. Do not claim that exception is blanket permission for a future rhythm-game mode. Standalone recording/streaming-service/Content-ID redistribution is not authorized. BOOTH/YouTube bodies were unavailable to the web fetch; no unseen track-specific conditions are claimed. Existing governing terms were independently read via the public creator API.

Use the complete owner-provided recording, not a short excerpt or cut climax. Export trims only leading/trailing silence, lowers gain2.9dB (measured source mean−13.0dB vs DollP1−15.9dB), and crossfades the final1.5seconds into the first1.5seconds. Native OGG loop metadata resumes just after that overlap. Native music mixing supplies entry/exit fades. It is background playback, not a sample-synchronized authority clock. Subjective seam/mix acceptance requires listening; no claim of having listened is made. [Attribution](../../../Assets/ATTRIBUTION.md#azure-cathedral--2026-09-20) owns exact export recipe, authorship and license separation.

## Assets and pending acceptance

Original built-in-generated images: `Liora.png` (192×128, eight48×64 cells drawn at native scale), `Vitrion.png` and dedicated `VitrionFury.png` (1024×1024 symmetric-material2×2 parts atlases), unchanged `Cathedral.png` and `GlacialChime.png`. Source prompts and export identities are in [asset brief](ASSET_BRIEF.md); raw originals are retained externally, not deleted.

Next owner smoke: consecutive attempts without world reload restore the background; lethal body/tail damage in Fury retains the chain through melting/faded music, yields Victory and permits re-invocation. Then matching peers and equipped piercing weapons. [Lifecycle evidence](../../evidence/2026-09-21-azure-lifecycle-fix.json) distinguishes the observed abort and native regression from unplayed acceptance. Earlier attack/art/balance, entrance-mask/music,107% UI/zoom and Reduced Effects checks remain in [Fury refinement evidence](../../evidence/2026-09-21-azure-fury-refinement.json), not implicitly passed by this fix. No release certification is implied.
