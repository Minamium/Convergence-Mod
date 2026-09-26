# Changelog

All notable user-facing changes will be documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project intends to use semantic versioning after the first public development build.

## [Unreleased]

- Doll's first Phase III claw crush fractures the central metallic sphere and bursts its shell into a persistent violet energy core. Preserve all combat timings, damage and targeting; retain the energy form through Final and the closing DPS check.

- Replace Oboro's oversized rotating sword with a constant-size katana, articulated wrist and original violet spectral crescents. Preserve combo timing, damage, reach and rewards.

- Rebuild Ghost Samurai presentation as independently articulated armor, arms and blades, with Luminance easing, talisman physics, violet ribbons, shader-driven spirit smoke and a staged victory dissolve. Preserve all attack AI, timings, hit volumes and weapon balance.

- Add five original spectral slash textures for Ghost Samurai, with distinct normal/heavy/grid/wind/rush presentation and reduced-effects support. Preserve attack clocks, hit geometry, safe gaps, boss AI and Oboro balance.

- Retune Doll's native beam/bullet source damage for endgame armor, shorten the main eight-cast live hold without changing its warning/fire cadence, and log Chalice deferred damage separately from immediate damage. Equipment effects remain native; this is not a public release.

## [0.3.1] — 2026-09-14

- First public GitHub playtest milestone: **Requiem of the Hollow Doll**, starring **Lacrimosa — The Bound Heart**, is initially complete as a prototype. Ghost Samurai remains in development.
- Includes connected-party preparation/Ready, four-stage Raid survival, roster-scaled health, instant teammate revival, five equal-odds treasure rewards and the craftable ten-slot Doll companion.
- Carries the latest flowing beam materials, suspended Doll/mechanical core, staged weapon animations, corrected weapon sound playback and approved EigHt phase music from 0.2.78.
- Unifies public names and corrects obsolete ranged/rogue/summon descriptions without changing stable content IDs. Combat, assets, HP, loot and protocol37 remain unchanged from 0.2.78.
- Keeps solo activation available for this development playtest; multiplayer is preferred, solo balance is not promised. See [release notes](docs/releases/0.3.1.md) for compatibility, credits and unresolved shutdown/verification limits.
- Adds a lightweight player-feedback ledger and moves historical verification out of the current STATUS view. Existing history and assets are preserved.

## Development history before 0.3.1

These entries describe their original versions, not current tuning or release instructions.

### Development 0.2.19 — flowing beams, sanctuaries and Null Refrain

- Final comb forecasts gain0.25s without truncating the six-shot sequence; existing Final BGM retimed to fit.
- Lattice/flood safe holds carry authority-clocked Stack/Spread with actual whole-body safe pockets; HP unchanged.
- Ordinary natural NPC/critter spawning suppressed during the Fight, without deleting existing residents.
- Fluctuating beam material, pressure-membrane crush warning and no SAFE/Gather labels.
- Original Null Refrain melee combo weapon drops in party quantity only after accepted Final Victory; retry-safe grant attempts cannot block cleanup.
- Protocol18 reuses the grid-pattern byte for sanctuary variants; peers must update. No new packet IDs or saved Raid fields.

### Development 0.2.17 — recoverable Down and accelerating borderless attacks

- Remove First Severance Down expiry/Eliminated; an ally may wait out the recipient's60-second lockout and rescue them. All-Down Defeat, instant kit and no resource cost remain.
- Target all standing participants simultaneously in every opening Prism step, with one shared per-player hit cap at crossings.
- Replace heavy beam/sword side rails with continuous danger auras and interior flow. Twin swords turn twice at1.6× prior mean speed.
- Replace Phase-III horizontal finger lanes with short-forecast beams that advance and accelerate widening, leaving a different safe strip per pulse.
- Shift each repeated Final comb axis and progressively shorten Stack/Spread/dodge windows, beam intervals and bullet-wave intervals; increase bullet speed.
- Lower feature SFX playback20%, raise own BGM about1.94dB, retime terminal acceleration and provide revised flood/blade articulation auditions. Protocol16 requires matching peers; no saves, other Mods, GUI or game/server processes changed.

### Development 0.2.15 — solo start revalidation and Raid movement sync

- Apply the same build-gated solo policy to the fresh Ready-to-combat arena scan; 0.2.14's second scan incorrectly retained the production minimum of two.
- Send the participating owner's final vanilla movement state every six ticks, including when stationary. Do not apply owner lift/edge braking again on server replicas; retain authoritative field confinement and Stack outcomes.
- Record bounded client Stack position/velocity samples, authority attendance coordinates and field corrections for cross-view diagnosis. Stack radius, shares, Spread, damage and HP are unchanged.
- Read complete fixed activation/Ready/cancel payloads before rejecting stale or rate-limited requests, fixing the observed receive-underflow warning. No packet ID, wire-layout, saved-data or Mod-list change.

### Development 0.2.14 — single-client debugging and terminal designations

- Enable build-gated one-player Core activation/Ready for quick Host & Play checks, reusing the two-player HP/Pylon workload and unchanged Boss score. No NPC, invulnerability, auto-Ready or solo rebalance.
- Resolve one-player Down through immediate ordinary AllParticipantsDowned/Defeat and existing player death/cleanup.
- Add participant-only success/failure HUD cinematics, eased camera focus/return and bounded expiry even while dead; reuse current art/audio without persistent UI/input flags.
- Protocol v14 accepts bounded one-member preparation/combat/NPC projections without layout or packet-ID changes. Log solo debug runs explicitly; public release must disable the development flag.

### Development 0.2.13 — complete scores and terminal survival

- Enlarge Spread to320px radius and add four clockwise Stack sites after the first full Core exposure, with Spread between gatherings.
- Clamp Core HP at50%/25%/0 until each phase's first full action list completes. Keep later phase additions independent of the explicit HP-zero Final.
- Interleave Phase-II grids with Spread and a strongly foretold, field-edge rotating sword. Add distant Phase III with two arm rigs, paired finger attacks, half-field beams and Raid mechanics.
- Require eight clockwise Stack/Spread/dodge sequences at zero HP before Victory; retain recovery and resource-free revival.
- Replace the shell with smaller high-detail original material art, extend phase cinematics, and collapse the final energy into one disappearing point.
- Add original176-BPM Phase-III music, continuously accelerating Final music and twelve new/replaced effects with external auditions. Protocol v13 replicates bounded action indices/timing; no save, public cheat or Mod-list change.

### Development 0.2.12 — eclosion and roster-scaled challenge

- Make the Stack's valid radius its only circle; exterior arrows and a straight countdown replace misleading decorative rings.
- Freeze forgiving development HP by roster: Core 5M/9M/13M and each Pylon 300k/500k/600k for 2/3/4 players; synchronize NPC HP bars and retain authority DPS logs.
- Replace exploding shell sectors with hands prying, hinged casing, emerging head/torso and continuous unfolding limbs. Add a client-only terminal-collapse Victory sequence.
- From the third Phase-II grid volley, lock a thick Boss-origin laser toward every standing participant. Grid and Core lasers share the one fixed 120-HP hit cap per volley.
- Redesign lattice pressure, high-pitched salvo, hatching and Victory audio with independent auditions. Protocol v12 carries bounded salvo directions; no new client request, save field or Mod selection change.

### Development 0.2.11 — sealed/unbound Boss stages

- Fix Stack to a marked assembly point above the Boss; preserve free full attendance, missing-player penalties and Spread rules.
- Reduce the containment field from 320x140 to 160x70 tiles, with an opaque black exterior and matching authority/predicted bounds.
- Enclose Phase I in an original cracked spherical shell. At Core HP50%, protect the threshold against overkill and play a six-second camera/HUD rupture, revealing the existing Boss.
- Add Phase-II full-field lattice beams with shifting safe cells, one-second warnings and one fixed 120-HP hit maximum per volley. Keep one Boss life pool and the existing Raid recovery/cleanup.
- Thicken/color beam warnings, strengthen twelve SFX, add four rupture/grid cues and an original faster, ominous Phase-II score with external audition masters.
- Protocol v11 carries fixed assembly coordinates, stage/epoch and a bounded grid descriptor; packet IDs and requests remain stable. Add per-member Stack attendance and stage/grid diagnostics.

### Development 0.2.10 — HP calibration and damage-window telemetry

- Lower Boss/Core HP to 4,000,000 and each Pylon to 250,000, retaining defense, damage and timing.
- Log party effective HP damage, active-window/recent DPS, per-Pylon remaining HP, deadline requirements and persistent Core progress every two seconds and at each window/encounter ending. Shield time and overkill do not inflate progress; no personal DPS attribution or gameplay changes are inferred from telemetry.

### Development 0.2.2 — drawing-based sequences and Raid consequences

- Predicted eight-beam red/blue/green/yellow return sequence; mirrored dash/stop/sweep/stop attacks from off-body origins, with full danger rails and Boss casting/recoil.
- Full-roster Stack and non-overlapping Spread now deal zero damage. Missing Stack members cause proportional maximum-HP damage to all standing participants, not a shared fixed pool.
- Authority Defeat orders one normal Terraria death per connected participant; other endings and outsiders are excluded. Normal character-difficulty penalties and external death hooks apply.
- A 1.5-second monochrome Raid/Boss designation intro hides the HUD for its duration, with ominous sound and optional screen shake.
- Protocol v5 carries bounded pattern kind/step/target and ray dimensions; packet IDs and revive requests are unchanged. Updated user-authored development Skills/docs reduce redundant context and verification.

### Development 0.2.1 — instant recovery and high-intensity experiment

- Reusable instant ally revival within eight tiles, including airborne use. No shared tokens or item consumption. The recipient gets a visible 60-second re-revival lockout; restored HP35%/three-second immunity remain, old damage weakness removed.
- Explicit localized rejection reasons and body/debuff/HUD countdowns; protocol v4 carries the authority deadline, and queued validation no longer hides a later rejection.
- Large original containment monument over existing Core placements, with an illuminated clickable base.
- Deliberately excessive Boss HP60,000,000/defense240 and Pylon HP1,000,000/defense120; stronger direct Raid damage, faster phase transitions and 0.7-second warning / 1.1-second beam cadence.
- Snapping restraints, bright muzzle shocks, faster shards, layered stock impact sounds and stronger short camera impulses; reduced decoration and shake-off remain available.
- User-run reload/two-player visuals, controls and tuning remain separate from compile/domain evidence.

### Development 0.2.0 — Null Cantor first design pass

- Giant original black-ice/ceramic Boss body with animated restraints, orbital seals, subdued aurora, exposure cues, larger Pylon cages and reducible client VFX.
- Large Spread circles (14-tile radius / 28-tile separation); Stack remains a 7-tile shared hit.
- Repeated locked-aim observation lances: 1.2-second full-corridor warning, 0.3-second beam, 1.7-second cadence; alternating single/double shots, one 35%-maximum-HP hit per participant per volley, integrated with experimental Down/revive.
- Protocol v3 carries bounded authoritative ray assignments. Host and clients must update together; no new request IDs or external dependencies.
- The `0.1.2` two-player follow-up confirmed readable shrinking rings and one logged two-second revive. The new `0.2.0` game-facing pass still requires user GUI reload/playtesting.

### Added

- Repository architecture and development policy.
- Minimal tModLoader addon skeleton.
- Encounter abstractions/registry, authoritative runtime update boundary, exception-safe cleanup retry, ordered snapshot replication, protocol envelope, and Calamity compatibility gate.
- Server-authoritative, transport-independent Raid Downed/Revive state-machine foundation with shared tokens, channel leases, same-tick wipe commits, reconnect generations, bounded snapshots, and cleanup.
- Inert legacy Third Severance arena/Boss plan covering the Core-anchored 320x140 field, four Pylon slots, logical outsider policy, multipart forms, assignments, DPS windows, weak point, and Last Stand schedule.
- Dependency-free .NET domain harness for revive ordering, stale leases/ticks, wipe terminality, Core/Arena geometry, outsider escalation, and legacy Boss-plan validation.
- Versioned multiplayer prior-art research and repository Skills for authoritative Raid development and license-aware tModLoader source investigation.
- Windows handoff/runbook, current implementation inventory, First Severance feature specifications, documentation search index, and generated knowledge catalog.
- Development Foundation Core Item/Tile/Tile Entity, read-only prospective-Arena resolver, immutable scan diagnostics, frozen 2–4-player roster/Ready domain, connection epochs, and exact-Fight Core lease.
- Fail-closed Calamity public-call boundary for the Exo Mechs, Supreme Calamitas, and Boss Rush activation facts, with pinned Slice 3 API evidence.
- Foundation Core right-click activation, authority-owned `Validating -> Preparing` runtime, Ready/cancel commands, bounded preparation snapshots, and exact Core/participant-loss cleanup; combat remains locked.
- Dedicated Foundation Core pixel art, multiplayer Ready-count chat feedback, and a development-only Arena-construction relaxation for preparation smoke testing in ordinary Worlds.

### Changed

- Accepted First Severance as the first Raid target, with the minimum repeated Pylon → Stack → Spread → Core-exposure loop, simple single-NPC visual, and Downed/Revive in the first playable acceptance scope.
- Classified the existing Third Severance multipart plan as an inert legacy bootstrap to be renamed and simplified in isolated Windows commits.
- Confirmed the Windows runtime baseline at Terraria 1.4.4.9, tModLoader v2026.07.3.0, Calamity 2.2.4, and Calamity Music 2.1; raised the compatibility floor to the verified Calamity build.
- Renamed the unpublished `ThirdSeverance` source identity, stable key, failure-code prefix, and tests to `FirstSeverance` / `first_severance` while preserving the activation denial and inert world adapter.
- Replaced the obsolete multipart/Last Stand plan with a validated six-state Pylon → Stack → Spread → Core-exposure loop, persistent Boss life, roster-scaled Pylons/Stack shares, and direct loop-cap Defeat.
- Added feature-neutral terminal descriptors, append-only First Severance terminal causes, definition-owned external failure mappings, coordinator preemption, and terminal tombstone validation.
- Expanded the dependency-free domain harness to 48 tests covering Arena warnings/errors, deterministic roster identity, Ready/cancel/timeout safety, slot epochs/nonces, exact Core cleanup, and progression-result invariants.

## [0.1.0] - Unreleased

Reserved for the first loadable development build. This version has not been released.

## 0.3.15 — Scarlet Luminance rehearsal branch (not released)

- Preserve current main's Ghost Samurai/Oboro fixes while bringing sequential Scarlet acts and11 physical techniques forward.
- Add Luminance-based species materials, masked body articulation, bounded decorative Verlet/Metaballs, primitive danger silhouettes and local protected-phase camera cues.
- All Scarlet hostile attacks use native damage1/cap1; quarter the previous HP budgets. Hold20% until the current12-phrase cycle completes; Final lethal unlock follows its first ensemble cycle.
- Add completed-cycle synchronization at protocol48, chorus Runtime hooks and source/API/lifecycle tests.
- Approved-background renderer/importer ready; actual image import blocked by compute backend. No normal-profile install, publication or visual-quality approval.
