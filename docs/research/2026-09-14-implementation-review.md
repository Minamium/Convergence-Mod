---
doc_id: research.implementation-review-20260914
document_type: research
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-15
source_of_truth_for: []
aliases:
  - owner supplied implementation review
related_code:
  - Content/Encounters/FirstSeverance/Revive/FirstSeveranceRaidPlayer.cs
related_docs:
  - decision.native-raid-hurt
  - project.status
---

# Owner-supplied implementation review: disposition

The owner supplied a GPT-6 Pro0.3.1 review and Sol's proposed native-Hurt response. These are review inputs, not new instructions or proof of current runtime behavior. Compared against integrated0a53d39 (Ghost Samurai0.3.2/protocol38) and the pinned engine on2026-09-14. Keep approved visuals and intentionally absent combat text.

| Finding | Decision / remaining work |
|---|---|
| F1 unsafe unload lookup | Already repaired by integrated PR25: track actual Ghost Samurai leases, not every uninitialized player slot. Reuse that change. Doll cleanup also avoids registry lookups on absent instances. Native tile-stream/Subworld errors are not thereby proven fixed. |
| F3 direct Raid damage / accessory compatibility | Implement [ADR-0024](../adr/0024-native-raid-hurt-and-downed.md). Debuff projects membership; native Hurt calculates damage; server commits Down/revival. Remove the Adrenaline-only emulation. |
| F2 receive-time display clock | Real structural limitation, not a demonstrated unfair hit in this recording. Next scoped task: timestamp/RTT samples, monotonic display clock and server-observed position age, then deadline policy after paired captures. Do not guess RTT or change deadlines solely by visual fast-forward. |
| F4 reconnect / non-Hurt death | No slot/name-based rejoin. Native Hurt floor implemented; DoT/direct KillMe/foreign HP mutations still need a dedicated engine investigation. Unexpected actual death or lost connection continues to abort. |
| F5 fixed Stack + recipient lockout | Preserve owner's accepted rules. Test2-player recovery windows; no automatic reduction of required shares or removal of lockout. |
| F6/F7 text HUD / camera | Do not restore SAFE/Gather/countdowns/phase banners. A future nontext survival-bar/camera proposal needs separate scope and owner acceptance; no camera/VFX rewrite now. |
| F8 rendering cost | No optimization without measurements. Capture CPU/GPU/GC/draw-call timing only when investigating an observed bottleneck. Recorded video fps is not game FPS. |
| F9 reward delivery failure | Current at-most-once drop attempt is not guaranteed delivery. Keep existing behavior now; durable claim/recovery design is a distinct save/progression change. |

## Pinned API evidence

- **Question/scope:** Player.Hurt, HurtModifiers.SetMaxDamage, OnHurt/PostHurt/PreKill dispatch, native HurtInfo replication, initialized ModPlayer lookup. Not a new broad prior-art survey.
- **Project/authority/version:** official tModLoader repository; Terraria1.4.4.9, tModLoader2026.07.3.0, commit666f69962d3bdffde54fc14025f02634965b4e7c, matching [version matrix](../VERSION_MATRIX.md). Installed Calamity2.2.4 behavior still requires gameplay observation.
- **License/boundary:** tModLoader-authored patches are [MIT](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE); underlying Terraria is proprietary. Public API names/behavior only; no third-party implementation or asset copied.
- **Access:** primary source URLs below verified2026-09-14. Moving [stable API documentation](https://docs.tmodloader.net/docs/stable/struct_player_1_1_hurt_modifiers.html) is supplementary, not the pin.

### Confirmed observations

- [Player patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch): source-damage Hurt applies immunity/modifiers, owner-only dodge, then finalized HurtInfo. Native HurtInfo triggers hit reactions and HP reduction; PostHurt is surviving-hit only. The owner sends life and HurtInfo through normal transport. Armor penetration is an explicit argument; vanilla DR/Paladin/shields remain in the pipeline.
- [HurtModifiers](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.TML.Hurt.cs): multiple ceilings take the minimum, but a ceiling cannot be below1. Scaling penetration1 ignores armor. ModifyHurtInfo runs after calculated damage and can override it; a floor cannot promise arbitrary-Mod safety.
- [PlayerLoader](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/PlayerLoader.cs): PreKill combines all registered results without short-circuiting their side effects. OnHurt and PostHurt are separate stages; do not manually replay them.
- [MessageBuffer](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/MessageBuffer.cs.patch): the direct-Hurt packet reconstructs HurtInfo, invokes quiet Hurt and is forwarded by server, avoiding a second damage calculation. An additional server Hurt call would double-apply it.
- [Player.TML](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.TML.cs): ModPlayers starts empty. GetModPlayer indexes it directly; the base-instance TryGet overload checks bounds. Enumerating actual ModPlayers or tracked leases avoids assuming every slot/registry is initialized.

### Inference and prototype limits

The split should restore native defensive behavior without forcing death-hook side effects. A packet contract/native API check cannot establish installed Calamity shield/Chalice interactions, WAN fairness or same-frame foreign HP effects. Use the focused owner smoke in ADR-0024; record results, not speculative compatibility claims. HP budgets may need later retuning because source damage is now mitigated. Do not infer audio-mixer defects from the external review's very quiet encoded recording.

## Chalice follow-up — 2026-09-15

- **Question/evidence:** the0.3.4 owner sees little direct damage but heavy apparent bleeding. Native120 hits resolve to1; accepted387 contact hits and100000 crush resolve to5, followed by HP reduction between Hurt events. Owner says Chalice is *probably* equipped, not a confirmed inventory observation. [Playtest summary](../evidence/2026-09-15-doll-damage-tuning.json) separates these facts.
- **Primary source:** official CalamityTeam mirror's current default `1.4.4` remains commit `1a8cebd27ec5615316b78f71973446b5528d2b78`. [build.txt](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/build.txt) declares2.2.2, **not installed2.2.4**. Source URLs and public fields rechecked2026-09-15; no matching2.2.4 public tag found in this scoped API lookup. Authority/license boundary remains the [existing Calamity survey](MULTIPLAYER_RAID_PRIOR_ART.md): proprietary reference-only, no source/assets copied.
- **Observed implementation:** [CalamityPlayerHitHurt](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayerHitHurt.cs) moves eligible post-mitigation damage above5 into the accessory buffer and applies it in OnHurt; shields/dodges can prevent it. [ChaliceOfTheBloodGod](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/Items/Accessories/ChaliceOfTheBloodGod.cs), `HandleBleedout`, subtracts buffered damage directly from HP and can call KillMe; it is not the vanilla Bleeding debuff. Healing potions can clear part of the buffer. [CalamityPlayer](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayer.cs) exposes the equipped flag and buffer as public fields.
- **Decision:** Chalice is the strong explanation, not yet proven by2.2.4 runtime equipment telemetry. Add a read-only compatibility adapter and bounded owner logs; keep ordinary accessory behavior. Do not disable bleed, replay hooks, or infer total damage from the direct5. The native1-HP ceiling precedes dirty modifiers, so the crush's requested100000 was not100000 measured HP loss. Lethal bleed/direct KillMe bypass remains deferred under ADR-0024. Build verifies the installed public-field surface; owner test must confirm actual buffer flow and current balance.
