---
doc_id: adr.0023
document_type: adr
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-13
source_of_truth_for:
  - architecture.ghost_samurai_native_wave_damage
aliases:
  - native slash wave immunity
related_code:
  - Content/Encounters/GhostSamurai/GhostSamuraiAttackProjectile.cs
  - Content/Encounters/GhostSamurai/GhostSamuraiRuntime.cs
related_docs:
  - encounter.ghost-samurai.spec
  - project.network-architecture
---

# ADR-0023: Native player damage for Ghost Samurai slash waves

## Decision

The owner's current request explicitly requires ordinary hostile Projectile damage and native dash immunity/dodge hooks for the charged slash wave. This narrowly supersedes ADR-0002's encounter-owned player-hit rule **only for Ghost Samurai SlashWave**. First Severance, Ghost Samurai's other hazards, phase decisions, attack selection, timing, geometry and exact-Fight resource ownership remain server/Single Player authoritative.

The runtime creates and registers each wave once on the server/SP. Its immutable schedule and bounded final aim travel through the existing native ExtraAI entity. Only a live, locked wave with the matching active boss/Fight enables hostile Projectile damage. Its current oriented footprint is evaluated in ModProjectile.Colliding; the standard cooldown slot remains -1. It cannot hit NPCs. The runtime explicitly excludes waves from its manual Player.Hurt loop, so the two paths cannot apply the same wave twice.

In multiplayer, native hostile Projectile damage evaluates the owning local player and its normal immunity, FreeDodge and ConsumableDodge hooks. This is an intentional local-player hit resolution exception, not a claim of server-only wave hits. We add no custom client hit requests, forced-hit packet, dash cancellation, universal dash invulnerability, or independent client targeting. The accepted bounded server schedule/aim is the only geometry input. Clients awaiting the final aim cannot hit or draw a stale live wave. Native Terraria health/dodge synchronization and its trust boundary apply.

Cleanup/phase transition removes all exact-Fight wave entities through the existing idempotent runtime scan. No saved state, identities or dependency versions change. Protocol36 appends the SlashWave shape without renumbering existing shapes/packet IDs; field-sized circle bounds,16-byte immutable summon-centered field geometry and shout-lock timing also require matching peers. The feature runtime owns field admission and expiring exact-Fight player containment; it does not attach First Severance Raid recovery or Ready rules.

## Evidence and limits

[Feature specification](../encounters/ghost-samurai/ENCOUNTER_SPEC.md) owns tuning and the focused user-owned smoke. Compilation and domain checks do not prove a particular accessory's invulnerability duration or multiplayer latency behavior.

Source review pins tModLoader2026.07.3.0 / commit 666f69962d3bdffde54fc14025f02634965b4e7c, Terraria1.4.4.9. tModLoader-authored patches are MIT; underlying Terraria code/assets remain proprietary. Only API observations and built-in SoundStyle references are used; no third-party code/audio/assets are copied.

- [Projectile patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Projectile.cs.patch): native Damage uses CanDamage, Colliding, the ModProjectile cooldown slot and Main.myPlayer for hostile-player hits.
- [Player patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch): player immunity and local-only dodge hooks explain why server manual Hurt cannot replace this native wave path.
- [Main patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Main.cs.patch) and [ModSystem](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs): projectile updates precede the SP/server world tick, so the native wave adapter advances the prior authority age by one; PostUpdateEverything provides a common client/SP audio point after the world clock update.
- [SoundID](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ID/SoundID.TML.cs): Item1 is the shared sword cue; ScaryScream references Roar_2 for the locked dash cue.

