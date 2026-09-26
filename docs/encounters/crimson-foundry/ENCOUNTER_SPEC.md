---
doc_id: encounter.crimson-foundry.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-09-25
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

Ready uses the same minimal fixed top-center button as Doll and Cathedral: physical viewport center,200×36px at y64, on an unscaled interface layer. Click bounds match drawing. Small world-space `Ready!` labels appear above all ready participants, including the local player; UI scale does not reposition the button or double-transform those labels.

### Orb-to-invocation opening

Preparing shows only the floating red energy body, Ready/field containment and the normal world; neither Vespera nor the cathedral appears yet. All-Ready starts a protected approximately16-second opening: the orb unravels into the56px Vespera silhouette, then etched counter-rotating seals grow above her; the cathedral fades in only after her materialization. Ember Crown emerges at the shared summoning gate around8.7seconds, with an owned rupture cue/recoil. A longer designation and optional camera pan connect these events before combat unlocks. Input is not disabled; Reduced Effects/camera-off preserve timing and forecasts without forced camera motion. State/timing live in `CrimsonChoreography`, not independent client timers.

Vespera's native center remains fixed at the field center in every Act, including Final. Apparitions alone stage/move. Grounded containment uses the actual support surface, not a2px raised plane. Owner prediction handles ordinary edge contact; server jitter correction does not repeatedly emit native teleport sounds. Only corrections exceeding48px send a rate-limited teleport notification, logged as `MajorFieldCorrection`; large escape correction remains functional.

## Acts, health and complete action cycles

| Stage | Target | Exit |
|---|---|---|
| Introduction | Vespera, then Ember Crown | Accepted score introduction finishes |
| Act I | Ember Crown | Hold at20% HP and finish the current full action cycle |
| Act II | Sable Mantle | Hold at20% and finish the current full action cycle |
| Act III | Thorn Choir | Hold at20% and finish the current full action cycle |
| Final | Scarlet Eidolon, one giant apparition formed by sacrificing all three | Complete its first ensemble cycle, then defeat the single giant |

An action cycle is **12 complete attack phrases**, using the eight-beat choreography below. Five phrases can be followed by a Stack/Spread chorus in Act II onward. A chorus is not counted as one of the12 phrases. Completion waits for admitted phrases' last recovery/tail and any outstanding chorus. Issuing the last phrase is not completion. At a safe cycle boundary, a latched20% threshold changes exactly one act; otherwise another full cycle starts. No mid-phrase cancellation is used to accelerate the health transition.

Native incoming damage is capped before crossing20% in solo acts. Once reached, HP remains exactly at that floor while the apparition continues its repertoire. The same NPC and HP are retained offstage after retreat, without loot/replacement/refill. Act II/III use240 protected ticks: the retiring body moves to the gate, its surface erodes into flowing red energy, and the next body reconstitutes after it dissolves. `CrimsonEnsemble` owns these curves and deadlines.

Final uses540 protected ticks. Three child seals at top/lower-right/lower-left visibly re-summon the retained apparitions. Vespera's sphere settles at the center; she dissolves into it. Red energy streams emerge from all three bodies, gather into the growing sphere, and consume their surfaces. After complete convergence a shaped crimson flash releases the giant. Server-side sacrifice retires exactly the three owned NPCs without loot before combat unlock. **Only the giant is a Final combat target**; Vespera is visually absorbed, not a second target. Her existing native actor carries the giant's420×360 central hitbox and combined remaining HP. No fifth actor or extra HP budget is created. A large flowing red sphere is seated in its chest during combat.

The giant retains a1HP lethal floor until the **first Final action cycle** is finished. The floor then releases; no automatic death is invented. Attack-source activity remains distinct from `dontTakeDamage`, so the held giant keeps performing and the boss bar stays present.

Victory requires completed sacrifice plus the giant's defeat; the sacrifice itself is not Victory. An observed all-player wipe wins over a simultaneous clear. Unexpected missing actors invalidate the encounter, whereas the explicit sacrifice is an owned retirement. Phase changes/source deaths/teardown retire exact-Fight hazards and chorus markers; a resolved failure marker may retain its harmless48-tick visual tail through Defeat. Phase, epoch, health and completed-cycle projections reject rollback; gameplay decisions remain server/SP-owned.

### Temporary rehearsal tuning

`CrimsonPlaytestTuning` is the single source of tuning. Each original target budget is `750,000 + 500,000 × (participants − 1)` HP, frozen at Ready. Total one-time budgets remain3m/5m/9m/17m for1/2/4/8 players. Final transfers one full conductor budget plus the three20% remainders into **one** giant bar:1.2m/2m/3.6m/6.8m respectively. Transfer is not damage, healing or a fourth apparition refill; progress logging counts it once. The action-cycle gate prevents high-end weapons from skipping whole acts.

**Ordinary Scarlet hostile gestures retain source damage1 and `SetMaxDamage(1)` for choreography rehearsal.** Contact damage remains disabled. Stack/Spread are now the requested failure-only high-damage exceptions, owned by `CrimsonChorusRules` below. All hits retain ordinary immunity/dodge/shields/accessory hooks; these are source budgets, not guaranteed HP subtraction or bypasses. Player/companion weapons, environmental damage and Doll/Ghost attacks are untouched. This remains temporary rehearsal balance.

## Beat choreography and Final ensemble

The active composition is **four consecutive beat releases → two beats paired-seal charge → two beats crossflow**. Basic warnings occur on beats0/1/2/3 and strikes on1/2/3/4;32-tick flights may overlap the next warning/strike. On beat4, two opposing vertical seals appear beside one captured player position. Beat6 launches **one horizontal stream from the right seal into the left seal**, enclosing the player's captured position between them; evade above/below it. It amplifies and shrinks across two beats using shared visible/collision geometry. Near a field edge the pair is clamped inward as a unit. The next forecast begins at beat8, immediately when the previous stream finishes, and its first strike follows one beat later.

Final cycles three paired families: **tracking beam + spatial cut → spatial cut + shifted lattice → shifted lattice + tracking beam**. Each of its first four beats releases both families simultaneously, each with a complete measured-beat warning. **Final alone replaces the closing crossflow with a scarlet cluster orb**: a giant energy sphere grows above the Eidolon on beat4; frozen thin forecasts and sparse full-width grains announce five radial bouquets before beat6 releases them. Each bouquet contains one large, two medium and two small energy carriers. Sizes, speeds, exit bounds and continuous acceleration are owned by `CrimsonClusters`; rendering and native swept collision consume that same geometry. Directions are deterministic per phrase, never homing after warning. Gaps between bouquets admit a full player footprint; overlapping earlier paired attacks still require playtesting.

The sphere is a harmless emitter, not an additional enemy, hitbox or HP pool. Carriers shrink before reaching the field edge; trails are harmless. Their bounded96-tick flight may cross the next bar without disappearing at the old crossflow end. The next bar keeps its musical schedule; the action-cycle gate waits for the final owned flight and cleanup lease. One giant owns all nine descriptors, with distinct pulse IDs; the25 carriers share one descriptor/native gesture, not25 new projectiles. The maximum descriptor bound remains10. Source death/epoch replacement cancels the whole owned batch. Protocol65 appends `ClusterVolley` without renumbering existing IDs; matching peers are required. The giant recombines Crown core, Mantle wings and Choir limbs with separate masks, connected fractional articulation and the existing Luminance materials, rather than enlarging a single complete PNG.

Each aimed note selects one living roster member in round-robin order, bound to their slot/connection token. At warning onset the server takes one observation: basic attacks use Doll's18-tick velocity lead capped at220px horizontal/160px vertical; paired seals use the current position without lead. The shown forecast is immediately fixed, never dragged after presentation or retargeted to another player. Basic axes cycle vertical/horizontal/two diagonals. Replicas require the authoritative lock before showing/damaging and reject stale/different-Fight/phase/note samples. A dead/disconnected target retains its original bounded fallback. The bounded immutable chorus outcome is replicated, not client-authored damage requests. Unowned/debug-spawned native Boss/Effigy packets carry an empty-state envelope and remain harmless; owned payload validation and exact-Fight cleanup remain strict.

Act I retains four basic forecasts crossing **from one field boundary to the opposite boundary** through the predicted position, never starting at the Boss. Doll's managed `PortalForecastPass`, sparse `ForecastDustPass` and red/magenta `PortalBeam` remain the foundation. Shared geometry owns visible width/reach and native collision; zero-width ignition and residue are harmless.

**Act II always replaces all four basics with SpatialRift slashes**, not alternating tracking-beam phrases. Each freezes one predicted player line, gives the measured beat's white space-tear warning and releases a fast red travelling cut. **Act III always uses ChoirRakes:** three parallel bowed/hooked cuts per beat, entering clockwise from top/right/bottom/left. Their full curves are forecast for a complete measured beat; rapid extension and width taper share `CrimsonChoirRakes` with native swept collision. Spacing, modest deterministic phrase offset, radius, edge inset and the96-capsule bound live there, not in local RNG. The wide passages accommodate a player footprint. Every Act I–III phrase still closes with the unchanged crossflow. Final's paired tracking/slash/shifted-lattice families and cluster closing are unchanged. Protocol68 appends `ChoirRakes=16`; existing technique/packet IDs and payload layout are preserved, and peers must match.

Slash presentation adapts the sharp travelling core and contracting afterimage observed in the owner's Orderbringer footage, not its rainbow palette or homing behavior. A narrow white/red blade follows the accepted capsule, with continuous sub-frame amplitude/width variation, elongated recoil filaments and sparse glints. Faint turbulent smoke lingers after the cut and where the left seal catches the beam. Smoke, glints and vibration do not move/extend damage; bright blade light ends with the live interval. Reduced Effects damps decorative flutter, flashes and smoke while retaining warning/strike timing. [Reference analysis](../../research/WOTG_RAID_BENCHMARK.md#f18--orderbringer-cursor-hit-reference-2026-09-20) records observed frames, pinned source and copying boundaries.

### Retained physical vocabulary (inactive reference)

| Performer | Techniques and body identity |
|---|---|
| Ember Crown / Act I | `CrownRain`:26-column falling energy bands with distinct continuous heads/tails and a fixed three-column refuge per phrase; `CrownCinders`:12 large arcing cinder bursts across two field-wide rows; `CrownCrash`:targeted body traversal, larger contact and expanding impact rings |
| Sable Mantle / Act II | `MantleFan`:four concentric field-reaching fan sweeps; `MantleRush`:larger targeted body traversal; `MantleScissors`:four long curved cloth blades in upper/lower pairs, with central and inter-blade passages |
| Thorn Choir / Act III | `ChoirThrust`:seven extending tendrils spreading from overhead across the floor; `ChoirHook`:large targeted hooked return curve; `ChoirRend`:six910px diagonal spatial tears across two rows |
| Vespera / Final ensemble | `VesperaOrbit`:12 orbiting energy bodies around the accepted target; `VesperaPetals`:12 curved projectiles converging from a field-sized ellipse toward a hollow center. Retained apparitions continue their own decks; defeated sources are skipped |

The retained IDs/geometries and their regression checks are not active decks. Reintroducing any of them requires a new requested design slice. Phrase admission still reserves all native resources before creation, and exact-Fight/epoch cleanup retires pending and live notes.

## Music: basic pulse rehearsal

Use the **same measured beats as `Score.Pulse`, which drives the four inward arrows on the Stack marker**. The composition above places each new forecast on the prior strike's beat. Prime the first forecast before any strike; do not damage first. Each warning lasts one measured beat; do not introduce a separate fixed-BPM clock. The older alternating four-beat helper remains tested but does not schedule the active choreography.

The follow-up **restores all134.5127seconds of the previously approved full OGG**, including the climax cut by the earlier99.8s return. `tools/restore_crimson_full_score.py` preserves all original decoded samples/beat data before encoding (with1.15dB headroom), then appends a1.8707s bridge; return136.3833s →54.8167s. No time/pitch stretch or replacement recording. Original WAV remains external/unavailable; exact OGG identities, modifications and kuku credit/license exclusion remain in attribution. Both sample clock and scheduler use the restored score and bridge beats. A local seam audition and numeric checks do not certify subjective musical continuity.

`CrimsonChoreography` owns consecutive basic beats and the paired two-beat warning/two-beat release. The12-phrase gates remain, so a cycle lasts longer than the former four-beat baseline. Musical energy does not invent unannounced fills, rolls or warning-pitch ladders.

Timing/lifetime bounds live in `CrimsonRhythm`/`CrimsonChoreography`. A beat schedules a release, **not a deadline to erase the previous attack**. Body recovery, hazard leases and cycle completion follow actual lifetimes; a chorus waits for prior recovery. Event ticks derive from absolute score beats and fractional visual pulses agree within one rendered tick across loops. This is **not verified transcription of the drum performance**. Audio drift handling and PhysicalPhrase logs provide diagnostics, not proof of audible synchronization. Listening, latency and gameplay fairness require owner playtests.

## Stack and Spread chorus

From Act II, after five phrases, alternate a fixed gather marker and player-following separation circles. Calls last8 measured beats with2-beat recovery, in their own interval. Preserve the approved rings/arrows. **Both are Vespera's attacks**, independent of which apparition is active. Stack builds paired black engraved seals above/below each announced living player in three irregular growth steps. Complete gathering causes0 damage; otherwise each eligible member receives a900-source budget scaled by the missing fraction, shown as fast black flame between their seals. Its opaque ink core has bright crimson moving lips so it reads on both sky and cathedral. Spread grows red seals behind each member; only overlapping members receive one900-source targeted red cut, still respecting native dodge/immunity hooks. Solo Spread passes. The server evaluates once and replicates the terminal failure mask **and bounded resolve positions**, then spawns recipient-gated native impacts. A48-tick cosmetic result survives death/Defeat at the original position, never a respawn location. Delayed results can still display within that lease; clients never infer failure from local circle positions. Damage and all remaining hazards stop immediately on terminal state; phase/world/new-Fight cleanup still clears the visual.

Accepted Victory projects a150-tick energy hemorrhage/melt: red streams erupt, the articulated body deforms downward and dissolves, and its chest sphere drains. This reuses the existing terminal cleanup interval, never prolongs damage/rewards or starts on sacrifice alone. Final convergence and Victory have bounded buildup/impact shake. Reduced Effects/shake-off suppress optional motion/glare. Original managed material erosion/flow and mesh deformation preserve the approved art, not a whole-image fade.

Spread circles use the280px radius in `CrimsonChorusRules` (previously200): rendering and pair-overlap failure share this constant. Eight complete nonoverlapping circles fit in a four-by-two layout inside the unchanged field with tight vertical margins. Stack radius, failure budgets and chorus timing are unchanged.

## Scarlet Covenant companion

The10-slot companion keeps its ordinary owner-replicated summon lifetime, walking/floating and native Summon damage. Within the existing1600px/line-of-sight search, choose up to20 chaseable NPCs by **current HP descending**, ties by NPC slot. No manual target overrides priority. Every60ticks a new cast opens all selected opposing pairs while the prior90-tick cast remains active. Pairs follow the same NPC throughout charge and live beam, adapting span to its current width. The incarnation guard cancels a dead/despawned/reused target. Other peers receive owner-selected centers/spans and bounded batch count, never choose replacement targets.

`CrimsonCovenantRules` owns inverse-square-root concentration:20 targets have0.82× prior size/1× per-hit damage; one target has2.6× size/4× damage, with monotonic values between. Count is frozen per cast and re-evaluated next cast, so a kill cannot silently enlarge an existing hit. Enemy width affects separation; concentration affects seal size and the exact beam width. Preserve38 charge/52 live ticks, harmless charge/closure, native Summon hooks and collateral beam hits. No PvP/contact damage or Raid-member substitution. Parent identity/buff/lifetime and remote-parent grace remain; at most two20-ray batches overlap per companion, with one sound per batch event. Protocol66 adds the bounded count to the native child payload and supports the longer protected ceremony; match peers.

## Luminance presentation v2

Apply the [shared Luminance policy](../../ART_DIRECTION.md#luminance-presentation-policy) and its completion criteria. The choices below implement Scarlet's own identity; other features reuse the useful techniques with their own materials, motion and geometry.

Crown and Mantle keep their original organic source art but now use continuous species-specific skins and `ScarletApparitions`, not the old five-region hover. Crown is a **venting censer**: independently opening lateral crown structures, a hollow furnace throat, upward curling fire jets, downwind cloth and rising embers. Mantle is a **hooked shroud**: four independently delayed scythe tips pull elongated crimson membrane trails, brace before release and whip forward; moving veins connect them to the small interior heart. Neither duplicates Choir's radial wing membranes. All effects remain harmless and body-local.

**Thorn Choir remains a summoned organic aberration**, not an armored reliquary. Its accepted `ThornChoir.png` supplies the hollow hood, ivory four-arm anatomy, red heart and ragged shroud. `CrimsonChoirMotion` independently articulates all four shoulder→elbow→wrist chains; `CrimsonChoirRig` skins the original image continuously rather than translating a whole pose or separating it into rigid rectangles. Individual accepted notes select alternating arms; broad attacks conduct with all four. Rapid intake, a braked tension beat, explosive release and shaped recovery follow the existing fractional `Born/Fire/End` clock. Cloth lags behind the bones, and hand trails sample those same joint trajectories. There is no local attack timer or additional hitbox.

`ScarletChoir` is an original Luminance-managed five-pass material: dark retained albedo plus flowing white/crimson bone excitation, a diffuse anatomical glow, asymmetrical torn energy-wing/sleeve ribbons, a pulsing vascular heart, and bounded drifting sparks. Heart→hand currents follow the moving joint chain. Preserve dark negative space and brief bright discharge; a faint outline or uniform red overlay is not sufficient. These are harmless body-local effects, below attack information. Reduced Effects cuts wing/filament/particle density and exposure. The renderer reuses fixed geometry arrays and borrowed ModContent/Luminance resources, restores SpriteBatch/device state and allocates no owned render target; Dedicated Server never loads its graphics. WotG's separation of articulated anatomy/material/aftermath informs the technique, but no third-party art or shader source is copied; the [existing benchmark](../../research/WOTG_RAID_BENCHMARK.md) retains source evidence. The rejected nine-cell armor draft is externally archived, not runtime art.

The four Choir arms also appear in the Final ensemble; the complete body appears in Act III and the sacrificial return. **Scarlet Eidolon has a dedicated local composition:** broad dark Mantle, exposed four-arm chains, detached Crown crest, asymmetric arterial ribs, a suspended turbulent scarlet nucleus and flowing lower tendrils. Its accepted Final gestures excite the hands and heart. `ScarletAvatarTarget` composes1536² body and768² emissive blur surfaces in Luminance's PreDraw update event, then composites with restrained distortion/glow; Reduced Effects halves both dimensions and reduces density/exposure. Exactly two wrappers are reused. Fight/epoch/stage/clock mismatch rejects stale frames, world/terminal cleanup releases surfaces on the render thread, and a direct-render fallback preserves anatomy if offscreen composition fails. No gameplay coordinates or global camera/UI matrices are changed. The same anatomy melts/bleeds during the existing Victory interval. This adopts Avatar's separated rig/material/composition approach, not its assets, code, resolution budget or a claim of equivalent visual quality/performance.

Vespera remains56px and point-sampled at her fixed native center. Native actors, damage budgets, phase clocks and Final mechanics remain unchanged; Act II/III's new decks are specified above. `ScarletSorcery` supplies engraved red/black seals, white-to-red spatial cuts and black-flow material. Client effects never change native targets.

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
