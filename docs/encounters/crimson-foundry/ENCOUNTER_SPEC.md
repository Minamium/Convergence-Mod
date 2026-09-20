---
doc_id: encounter.crimson-foundry.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-09-20
source_of_truth_for:
  - encounter.crimson_foundry.experience
  - encounter.crimson_foundry.music
aliases:
  - Scarlet Invocation
  - Crimson Invocation (historical name)
  - scarlet conjurer
related_code:
  - Content/Encounters/CrimsonFoundry
  - Client/Encounters/CrimsonFoundry
related_docs:
  - project.status
  - adr.0026
---

# Scarlet Invocation

This specification supersedes the simultaneous-three-apparition prototype and the flat physical renderer. [Status](../../STATUS.md) owns the current build and verification; implementation does not imply visual playtest approval.

**Vespera — The Scarlet Conjurer** conducts three independent apparitions. The player-facing Raid and item names are **Scarlet Invocation** and **Scarlet Grimoire**; stable `crimson_foundry`, CrimsonFoundry code, packet, save and asset IDs remain unchanged. The companion is Scarlet Covenant. Vespera remains ordinary NPC scale; only her apparitions are large.

## Party, preparation and authority

Use Scarlet Grimoire on the existing Foundation Core. Theater Doll still selects the separate Doll Raid. Server-held-item/range/world/lease validation, server-wide 1–8-player preparation, manual Ready and a frozen roster with connection tokens remain. A roster change resets Ready. No fake participants, autonomous companion substitution, new world generation or automatic rejoin is added. Scarlet uses ordinary player death; no Doll Down/revival pipeline is introduced. The 160×70-tile grounded field, containment/lift and natural-spawn suppression remain.

The Doll attendant is only visible when the exact Doll Encounter/Fight/preparation and pedestal position match. A generic protected pedestal does not summon her. Scarlet spawn, AI and draw do not acquire ownership of the Doll preparation actor.

## Acts, health and complete action cycles

| Stage | Target | Exit |
|---|---|---|
| Introduction | Vespera, then Ember Crown | Accepted score introduction finishes |
| Act I | Ember Crown | Hold at20% HP and finish the current full action cycle |
| Act II | Sable Mantle | Hold at20% and finish the current full action cycle |
| Act III | Thorn Choir | Hold at20% and finish the current full action cycle |
| Final | All three retained apparitions plus Vespera | Complete the first ensemble cycle, then defeat all four |

An action cycle is **12 complete attack phrases**, currently all using the single-beam rehearsal below. Five phrases can be followed by a Stack/Spread chorus in Act II onward. A chorus is not counted as one of the12 phrases. Completion waits for admitted phrases' last recovery/tail and any outstanding chorus. Issuing the last phrase is not completion. At a safe cycle boundary, a latched20% threshold changes exactly one act; otherwise another full cycle starts. No mid-phrase cancellation is used to accelerate the health transition.

Native incoming damage is capped before crossing20% in solo acts. Once reached, HP remains exactly at that floor while the apparition continues its repertoire. The same NPC and HP are retained offstage after retreat, with no loot, replacement or refill. Each next act has150 protected transition ticks. Final recalls those three bodies at20% each and exposes Vespera. All four have a1HP lethal floor until the **first Final action cycle** is finished. The floor then releases; no automatic death is invented. Attack-source activity is deliberately distinct from `dontTakeDamage`, so held targets keep performing and the boss bar stays present.

Vespera-only or apparitions-only kills cannot clear Final. An observed all-player wipe wins over a simultaneous boss clear. Missing actors invalidate the encounter rather than count as defeated. Phase changes/source deaths/teardown retire exact-Fight hazards and chorus markers. Phase, epoch, health and completed-cycle projections reject rollback; gameplay decisions remain server/SP-owned.

### Temporary rehearsal tuning

`CrimsonPlaytestTuning` is the single source of tuning. Each of the four targets has `750,000 + 500,000 × (participants − 1)` maximum HP, frozen at Ready. Total one-time budgets are3m/5m/9m/17m for1/2/4/8 players: one quarter of the earlier budget. Final's initial bar is one full target plus three20% remainders, not four refilled targets. The action-cycle gate prevents high-end weapons from skipping whole acts even at this lower HP.

**Every Scarlet damaging hostile projectile uses source damage1 and `SetMaxDamage(1)` in its native hit modifier.** Ordinary immunity/dodge/shields/accessory hooks remain; a dodged hit may cause0. This covers physical gestures, retained legacy attack types, and Stack/Spread verdict strikes. Contact damage remains disabled. It does not alter player/companion weapons, environmental damage, or Doll/Ghost Samurai attacks. Chorus rules still calculate the normal sharing/overlap budget for diagnostics, but a nonzero verdict is applied as1 in this rehearsal build. This is explicitly temporary test tuning, not release balance or a universal override of foreign final HP writes.

## Single-beam baseline — September20

The owner requests rebuilding the attack vocabulary from one basic attack. **All Act I/II/III/Final ordinary attacks now use `TrackingBeam`; the former decks below are not scheduled.** Keep the current two-strike/four-beat rhythm,32-tick live interval, complete-cycle gates, native rehearsal damage and separate Stack/Spread choruses. Final still rotates living sources, but emits only one beam per musical strike, not one per source/player simultaneously.

Each note selects one living roster member in round-robin order. The immutable note binds that player's slot and connection token. During its one-beat warning the server eases the aim toward that member, sending bounded monotonic aim samples every3ticks and at the final lock. Direction freezes10ticks before release; disconnected/dead targets leave their last valid aim, never switch to another person mid-warning. Clients interpolate accepted warning samples only, reject stale/different-Fight/phase/note data and cannot cause damage before receiving the final lock. The beam does not home after release.

One continuous forecast crosses **from one field boundary to the opposite boundary**, through the aimed player; it never begins at the Boss. The direction tracks around the staged source's reference point, but both visible/damaging ends are clipped against the arena. Reuse Doll's actual managed `PortalForecastPass` plus sparse `ForecastDustPass`: one fine spine in a soft veil, no capsule caps or bright edge rails. The red/magenta `PortalBeam` jet ignites at the field edge, reaches full length over5ticks, expands to72px full width over6ticks, then narrows over its final8ticks. Shared geometry owns warning, live width/reach and native collision; zero-width ignition and all residue are harmless. Accepted circles, large decorative energy sphere, cathedral, body articulation and camera options remain unchanged. Protocol54 adds connection-bound target admission and ordered native-projectile aim snapshots, not client targeting requests.

### Retained physical vocabulary (inactive reference)

| Performer | Techniques and body identity |
|---|---|
| Ember Crown / Act I | `CrownRain`:26-column falling energy bands with distinct continuous heads/tails and a fixed three-column refuge per phrase; `CrownCinders`:12 large arcing cinder bursts across two field-wide rows; `CrownCrash`:targeted body traversal, larger contact and expanding impact rings |
| Sable Mantle / Act II | `MantleFan`:four concentric field-reaching fan sweeps; `MantleRush`:larger targeted body traversal; `MantleScissors`:four long curved cloth blades in upper/lower pairs, with central and inter-blade passages |
| Thorn Choir / Act III | `ChoirThrust`:seven extending tendrils spreading from overhead across the floor; `ChoirHook`:large targeted hooked return curve; `ChoirRend`:six910px diagonal spatial tears across two rows |
| Vespera / Final ensemble | `VesperaOrbit`:12 orbiting energy bodies around the accepted target; `VesperaPetals`:12 curved projectiles converging from a field-sized ellipse toward a hollow center. Retained apparitions continue their own decks; defeated sources are skipped |

The retained IDs/geometries and their regression checks are not active decks. Reintroducing any of them requires a new requested design slice. Phrase admission still reserves all native resources before creation, and exact-Fight/epoch cleanup retires pending and live notes.

## Music: basic pulse rehearsal

Use the **same measured beats as `Score.Pulse`, which drives the four inward arrows on the Stack marker**. Forecast on beat2, strike on beat3, forecast on beat4, strike on the following beat1. Prime that first forecast before any strike; do not damage first simply to reverse the order. Each warning lasts one measured beat; do not introduce a separate fixed-BPM clock.

The September20 loop revision preserves the introduction but loops the sustained middle section instead of jumping from the outro into the quiet opening:99.8167s →54.8167s,96 measured beats/45s per repeat. A four-beat1.8667s pre-return crossfade joins similar level/harmonic material. No time/pitch stretch; existing beat positions before the new endpoint remain unchanged, later beats are omitted, and both audio/sample clock and scheduler use the same updated endpoints. The game-facing approved OGG is the source for this edit because the external WAV is currently unavailable; no replacement recording is downloaded. Modest1.15dB encode headroom prevents clipping of the prior lossy decode. `tools/reloop_crimson_score.py` records candidate scoring, exact hashes and a local seam audition; numerical matching is not subjective listening approval. Preserve kuku credit and license exclusion.

This deliberately plain baseline replaces the syncopated5/6-note patterns after owner feedback. All acts, including Final, use two equal-accent attacks per four-beat phrase. Technique/source choices may vary, but serial numbers and musical energy no longer add fills, rolls or warning-pitch ladders. The existing12-phrase action-cycle gates and separate chorus intervals are unchanged.

Timing/lifetime bounds live in `CrimsonRhythm`. A beat schedules the next release, **not a deadline to erase the current attack**. Extended flight overlaps the next forecast and harmless residue may overlap the next release. The actual score still leaves a gap between distinct damaging strikes. Body recovery, hazard lease and cycle completion follow the extended lifetime; a chorus waits for prior attack recovery. Phrase reservations keep the original four-beat spacing without a new per-bar rest. Rhythm/technique/pose/material cues share the authority timeline. Event ticks are rounded from absolute score beats; the fractional visual pulse agrees within one rendered tick, including the audio loop seam. This is **not verified transcription of the actual drum performance**. `CrimsonAudio` starts from the accepted sample position and can reanchor major drift; absence of reanchor logs is not proof of sample-accurate audible output. PhysicalPhrase logs include score start, first fire, warning ticks and last end. Listening, latency and gameplay fairness require owner playtests.

## Stack and Spread chorus

From Act II, after five physical phrases, alternate a fixed gather marker and player-following separation circles. Calls last8 measured beats; a2-beat recovery follows. They occupy their own interval instead of layering unavoidable assignments over dense attacks. Stack normally shares a native source budget across occupants; Spread normally strikes overlapping eligible members once each. The rehearsal hit is1, including a successful nonzero Stack share. Solo Spread is naturally harmless. No text-heavy combat instructions are restored: inward/outward textured chevrons distinguish the mechanics.

## Luminance presentation v2

Apply the [shared Luminance policy](../../ART_DIRECTION.md#luminance-presentation-policy) and its completion criteria. The choices below implement Scarlet's own identity; other features reuse the useful techniques with their own materials, motion and geometry.

The performers retain distinct body materials while the current rehearsal deliberately shares one ordinary beam attack. The original PNG silhouettes are preserved. `ScarletSurface` uses masked core/upper/lower side regions with per-part pivots; `ScarletRigMotion` provides fractional breathing, asymmetric cloth/limb follow-through, a held loading beat, sharp release and recovery. Crown retains a local heat core, Mantle develops silk sheen, and Choir uses vein/rift treatments. These are masked regions of existing art, not newly hand-painted animation cels or a full skeletal replacement. Vespera remains56px, point-sampled with a restrained silhouette rim. Before Final, her server-owned perch follows the active focus260px above rather than hiding at the field ceiling; Final accepted body paths/vulnerability stay unchanged. No per-client fake hit position is introduced.

Luminance `PrimitiveRenderer` and `ScarletRibbon` render the accepted physical strokes. Forecasts use a fine spine, delicate footprint edges and sparse moving grains; live strokes have warm scarlet/rose/magenta fibres, a hot core and dark flowing folds. World-length coordinates prevent stretching during extension. Rifts retain a dark cavity with hot lips. Physical end caps use actual half-disk geometry rather than an early-return shader discard. Identical future forecasts in a phrase draw only the nearest pending silhouette instead of accumulating opaque copies. Larger converging filaments/pressure connect sources to strikes without target circles or full-screen strobes. Extending blades/tendrils burst out, briefly brake, then bite with one monotone curve for drawing and collision. Chorus circles/arrows and the approved background keep their accepted treatment; residue never causes damage.

The September19 flow revision repairs truncated paths: pinned Luminance1.0.14 [`AssignIndicesRectangleTrail`](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/Core/Graphics/Primitives/PrimitiveRenderer.cs#L253) draws N−2 segments. The installed1.0.14 method's IL confirms that bound. Add a final tangent-support point and remap width/longitudinal UV over the visible points; do not restore the old three-point/half-line call. The previous offline upload facade drew N−1 and missed this defect; the revised preview follows the actual bound. Forecasts now arrive smoothly, cover the full danger path and dissolve briefly through ignition. Crown rain has a travelling head/tail, feathered energy and shaped aftermath rather than blinking short capsules. Cinder carriers, orbit/petal bodies and the performer's offset energy sphere are enlarged; hazard forecasts enlarge with collision, while the decorative sphere stays away from her face. [Evidence](../../evidence/2026-09-19-scarlet-flow.json) distinguishes library/CPU/offline GPU checks from pending owner acceptance.

Luminance pushdown state machines project Follow/Stage/Windup/Strike/Recover/Manifest/Hidden. Four bounded12-segment Verlet chains per apparition add cosmetic trailing cloth/tendrils; they reset on Fight/phase/teleport and never provide damage coordinates. Luminance manually composed Metaball types provide capped Crown cinders and Choir ink, with exact-Fight/phase ownership, finite lifetime and per-particle-only parallel mutation. Reusable render targets belong to Luminance. Reduced Effects removes optional residue, reduces chains and motion, and preserves attack forecasts/material footprints.

Piecewise easing separates anticipation and recoil. Locally owned Luminance screen shakes are bounded and cancelled on teardown. A Luminance cutscene can gently pan only during the existing protected phase-transition interval; it never extends gameplay or blocks input. `CinematicCamera` and `ScreenShake` are client options. The renderer preserves the caller's actual SpriteBatch parameters, textures/samplers, vertex/index bindings, scissor and graphics state; it does not create or dispose graphics resources on a loader worker. Managed shaders remain library-owned.

### Physical attack audio and recoil

The five bespoke Scarlet cues are no longer used. Ordinary attacks reference the existing project-authored Doll `FirstSeverance/Beams/ChargeLock` and `PortalFire` assets directly, at gains0.48/0.72 without a pitch ladder. Voices retain their natural tails under bounded50/100-tick leases and cancel on Fight/phase/world teardown. No duplicate WAVs or foreign samples are added; old originals remain available for history, not active playback. Chorus and cinematic cues already use Doll sounds. Impacts retain bounded local-camera recoil, respecting Reduced Effects and shake-off. Actual mix acceptance remains user-owned.

### Approved background

`Assets/Textures/Backgrounds/ScarletSanctum.png` is the1672×941 fiery cathedral supplied by the owner after the original transfer failed. This approved clipboard export is copied byte-for-byte, not regenerated, resized or recolored on disk. Its exact identity and the distinct earlier export are recorded in [asset provenance](../../../Assets/ATTRIBUTION.md#scarlet-sanctum-owner-supplied-background--2026-09-17).

`tools/import_scarlet_background.py` accepts only the two explicitly approved byte identities. The sky detects both source PNG and tML's compiled `.rawimg` name. `ScarletBackdrop` adds bounded overscan/parallax, background-only heat/veil drift, two sparse ember depths, quiet beat response and a Final eclipse. The fight center stays lower contrast. Reduced Effects removes optional moving embers/distortion. A genuinely absent painting still logs `scarlet.background_asset_missing` and retains normal sky. No world time/weather/tile changes occur. In-game visual acceptance remains separate from successful import and offline rendering.

## Verification boundary

Domain/codec/source/API tests validate timing, states, formulas, ownership and API compatibility within their stated scope. They do not establish that this presentation exceeds Doll, that the sprites articulate attractively, that the approved background is present, or that Luminance1.0.14/Calamity2.2.4 in-game MP works. Test the complete package on1/2/4/8 matching peers, normal and Reduced Effects, cold load/reload, phase-held HP, first Final cycle, body/forecast alignment, local camera cancellation and re-summon. Keep existing Doll/Ghost regression checks separate.
