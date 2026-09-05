# Changelog

All notable user-facing changes will be documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project intends to use semantic versioning after the first public development build.

## [Unreleased]

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
