---
doc_id: research.instant-revival-core-apis
document_type: research
status: accepted
owners:
  - engineering
  - art
last_reviewed: 2026-09-06
source_of_truth_for: []
aliases:
  - instant revive API evidence
  - Foundation Core special draw
related_code:
  - Content/Encounters/FirstSeverance/Revive
  - Content/Encounters/FirstSeverance/FirstSeveranceClientActions.cs
  - Client/Encounters/FirstSeverance/FoundationCoreVisuals.cs
related_docs:
  - decision.instant-revival-recipient-lockout
  - encounter.first-severance.revive
  - encounter.first-severance.visual
  - compatibility.version-matrix
---

# Instant revival / Foundation Core API evidence

## Question and pinned source

Implement the user-requested reusable instant revival, a visible 60-second recipient lockout, and a larger Core without changing existing saved Tile/TE coordinates. Research was inspected on 2026-09-06 against official tModLoader source commit `666f69962d3bdffde54fc14025f02634965b4e7c`, matching the confirmed v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8 / C# 12 baseline. Calamity 2.2.4 remains installed; none of these seams use its private API.

The [pinned tModLoader license](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE) is MIT. No source implementation, decompiled binary, external texture or recording was copied.

## Confirmed observations and independent implementation

- [ModBuff.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModBuff.cs): buff defaults may mark a debuff; `RightClick` can reject manual removal. Adopt a visible non-saved/non-cancellable status using the project's existing original icon. The buff does not decide eligibility: authority stores an absolute deadline, validates it on use, and the owner refreshes its visual buff from a bounded projection. Removing the icon cannot bypass the server deadline.
- [GlobalTile.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/GlobalTile.cs), [ModTile.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModTile.cs), and [ModBlockType.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModBlockType.cs): inspected `DrawEffects`, `SpecialDraw`, `PostDrawPlacementPreview` and `AddSpecialLegacyPoint` contracts. Adopt top-left-only special drawing with the provided SpriteBatch, a client-only loader, the original 2x2 tile coordinates and no TE schema change. The 176-pixel canvas is decorative; its base remains the interaction target.
- [MessageID.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ID/MessageID.cs.patch): `SyncEquipment` describes item-slot/type/quantity/prefix synchronization, with client-owned inventory caveats; `PlayerControls` describes player control/motion synchronization. Adopt these existing messages before the custom nonce-only revive intent. No item-consumption transaction is needed or added because the user explicitly rejected consumption.

## Inferences and limits

The inspected MessageID documentation does not prove the exact selected-item ordering inside the installed binary. Sending ordinary sync first is an integration measure, not an established cure for every multiplayer slot-switch race or a hostile-client inventory proof. Authority still rechecks the held kit, current sender binding, Alive state, range and target deadline after this tick's damage. Dedicated localized failures replace the old conflated rejection message.

Movement/mount/hook observations no longer cancel revival because the accepted feature has no held channel. Ordinary engine restrictions on starting an item use may still apply. No ordinary `PreKill` interception, outsider recovery or reconnect support is introduced.

Core overlap/lighting/preview appearance and the more forceful client VFX require an actual user-run game view. Existing [giant visual research](FIRST_SEVERANCE_GIANT_VISUALS.md) records the WotG/film reference boundary; this pass only increases independently authored effects and does not import reference assets or code.

## Focused checks

The domain harness passes 56 cases, including instant same-tick revival, exact 3,600-tick eligibility, no recipient-to-rescuer lock propagation, stable simultaneous-request arbitration, repeat rescues without resources and cleanup. The actual compiled protocol-v4 codec preserves each recipient deadline beside absent/single/double volleys and consumes only its payload in a shared buffer. Actual ModSources `0.2.1` packages with 0 warnings/errors. No GUI reload, airborne two-client revival, Core render or high-intensity balance result is claimed for this build.

## 0.2.2 narrow lookup: normal Defeat death and temporary HUD suppression

Accessed 2026-09-06; same official repository, MIT license and pinned runtime/source above. API use only; no source implementation copied.

- [Player.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch), `Player.KillMe`: the public death path invokes `PlayerLoader.PreKill` and returns if it cancels; the accepted path sets death/respawn state and invokes `PlayerLoader.Kill`. Decision: invoke normal death on the participant's owner after the accepted authority Defeat and clearing Convergence's Down state. Do not set remote `dead` flags or bypass other Mods' hooks. Owner-side networking/penalties are inherited engine behavior and still require a multiplayer smoke; the inspected patch is not a complete proof of the unpatched vanilla death implementation.
- [ModSystem.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs), `ModifyInterfaceLayers`; [Main.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Main.cs.patch), interface drawing around `SystemLoader.ModifyInterfaceLayers`: the engine copies its layer list, applies hooks, and stops drawing when a layer returns false. Decision: insert a first intro layer and return false for that frame, retaining other Mods' named layer anchors. Do not mutate persistent UI flags or layer objects. Restore normal drawing by omitting the layer after the deadline/cleanup. Another Mod rendering outside this pipeline is not suppressed.

Required focused observations: two-client accepted Defeat → normal death/respawn once, non-Defeat/outsider exclusion, and intro HUD restoration at expiry/cancel with the installed Mod pack. Current build/check state belongs in [Status](../STATUS.md), not this API record.
