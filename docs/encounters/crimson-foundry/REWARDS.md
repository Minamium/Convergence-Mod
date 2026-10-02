---
doc_id: encounter.crimson-foundry.rewards
document_type: spec
status: provisional
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-10-02
source_of_truth_for:
  - encounter.crimson_foundry.rewards
aliases:
  - Scarlet Score Reliquary
  - 緋の聖譜櫃
  - Scarlet reward weapons
  - Sable Scythe
  - Canticle Organ
  - Scarlet Baton
  - Ember Censer
  - Bloodink Quill
related_code:
  - Content/Encounters/CrimsonFoundry
  - Client/Encounters/CrimsonFoundry
  - Content/Encounters/EbonManor/Rewards
  - Client/Encounters/EbonManor/Rewards
  - Assets/AutoloadedEffects/Shaders/ScarletInk.fx
related_docs:
  - encounter.crimson-foundry.spec
  - encounter.ebon-manor.rewards
  - project.art-direction
  - project.status
---

# Scarlet Invocation — rewards

On 2026-10-02 the owner asked for a reward set for this Raid built like the one for Waltz of the Ebon Manor: a treasure box, one weapon per class, and a companion crafted from all five weapons. Each weapon hands the player one of this Raid's own techniques, and the companion is Vespera herself.

- The [encounter spec](ENCOUNTER_SPEC.md) owns the fight and the Scarlet Covenant's behaviour.
- [Status](../../STATUS.md) owns the implementation state. This document does not claim any code exists.
- The [Ebon rewards](../ebon-manor/REWARDS.md) are the structural precedent. Departures are listed under [Differences from Ebon](#differences-from-ebon).
- The [Luminance presentation policy](../../ART_DIRECTION.md#luminance-presentation-policy) applies to every effect here.

| Class | Item | Code ID | Build | Release | Full-build finale |
|---|---|---|---|---|---|
| Melee | Sable Scythe / 帷の大鎌 | `CrimsonSableScythe` | staff lines engraved by connecting strokes (≤ 5) | Staff Reap / 五線刈り | Final Barline / 終止線 |
| Ranged | Canticle Organ / 聖歌のオルガン銃 | `CrimsonCanticleOrgan` | marks from shard hits (≤ 8 per enemy) | Hymn of Hands / 手の聖歌 | Clasp / 合掌 |
| Magic | Scarlet Baton / 緋の指揮棒 | `CrimsonBaton` | conducted black-blood strokes (≤ 8) | Tutti / 総奏 | Black-Blood River / 黒血の奔流 |
| Summon | Ember Censer / 灰燼の香炉 | `CrimsonEmberCenser` | each censer's widening pendulum swing | a pour at every apex | Grand Pour / 大注ぎ (every fourth pour) |
| Rogue | Bloodink Quill / 血墨の羽根筆 | `CrimsonBloodinkQuill` | stuck quills and the ink they wrote (≤ 8) | Sealed Score / 封緘の楽譜 (stealth strike) | Full Melody (eight quills) |
| Box | Scarlet Score Reliquary / 緋の聖譜櫃 | `CrimsonScoreReliquary` | — | — | — |
| Companion | Scarlet Covenant / 緋の契約 | `CrimsonPact` (existing) | — | — | — |

English names and technique names are proposals. Japanese item names follow the art brief. Technique names deliberately differ from the Raid's signature moves (Cinder Curtain, Shroud Rope, Four Hands), so callouts in a fight stay unambiguous. Content classes keep the `Crimson*` prefix of `Content/Encounters/CrimsonFoundry`, because ModItem class names persist in saves.

## Shared rules

### Signature

- Every weapon builds something visible, and then releases it in one moment the player chooses.
  - The build is dark and quiet: dormant black-blood ink (see [Material](#black-blood-material)) or, for the censer, a visibly widening swing.
  - The release is the brightest and loudest thing the weapon does. It re-ignites the build as the live black-blood river.
  - A full build earns a finale (see the table above).
- Each build step rings one step of the toll ladder, for the owner only. Each release pairs a **windup** cue with a **release** cue and ends on a cadence.

### Timing

- Builds and releases follow the player's input. No weapon, minion, idle pose or effect moves on a beat, reads the music clock or a world clock, or shares a clock with other players. The owner rejected motion that dances to the music.
- Idle motion is driven by physics (the owner's movement, a pendulum) or by aperiodic noise, never by a periodic pulse.
- Only a release cascade is spaced in Graceful Ordeal's sixteenths, counted from the release itself:
  - S(k) = round(k × 225/32) ticks, computed in integers as `(225k + 16) / 32`: 0, 7, 14, 21, 28, 35, 42, 49, 56, 63, 70.
  - A beat is S(4) = 28.
  - A release therefore sounds musical with or without the Raid music, and it is deliberately not phase-locked to the song.

### Acquisition

- An accepted Victory drops one **Scarlet Score Reliquary** per frozen Raid member at the field's ground centre. The reliquaries are shared world items, like the Doll and Ebon boxes (see [Owner decisions](#owner-decisions)).
- Right-clicking a reliquary opens it and gives exactly one of the five weapons, 20% each, independent of Luck.
- Five different weapons craft the **Scarlet Covenant** at a Bookcase. The Covenant's old Grimoire recipe is removed.
- There is no other drop, exchange or reverse recipe.

### Power

- **Tier:** the same as the Doll and Ebon weapons: Red rarity, sell value 40 gold, knockback 6, crit 8 (summon 0).
- **Base damage:** Ebon's: melee 3600, ranged 1900, magic 2400, summon 1000, rogue 1800.
- **Secondary hits** always derive from the live weapon damage (`Hit(damage, ×)`, at least 1).
- **Segmented NPCs** count once per release part, through a ledger keyed by the `realLife` root.
- **Nominal budgets** are raw, zero-defense starting values compared in one unit (see [Nominal parity](#nominal-parity)), not measured DPS.
- Raid HP, the action-cycle gate and `DebugOneDamagePlaytest` are not changed to hide weapon balance. The rehearsal mapping touches hostile damage only, never these weapons.
- There is no PvP damage, no tile cutting and no contact damage from any carrier projectile.

### Usability

A weapon cannot be used, its held projectiles stop, and its censers hover idle without pouring while the owner is:

- dead;
- Down in Scarlet (`CrimsonRecoveryPlayer.IsIncapacitated`);
- Down in Azure Cathedral (`AzureRecoveryPlayer.IsIncapacitated`);
- Down in Ebon (`EbonRecoveryPlayer.IsIncapacitated`);
- Down or eliminated in Doll (`FirstSeveranceRaidPlayer.IsIncapacitated`, which covers `IsRaidDowned`, `IsRaidEliminated` and the pending Down latch).

The weapons work anywhere. None is restricted to the Raid.

### Commitment

- A release schedules every part at the moment it is cast. Cuts, hands, ignitions, the river and bursts wait through negative ages or countdowns. An item swap does not stop a cast release.
- The owner's death or Down kills the parts that have not started. Parts already live finish their window; their hits still pass through each Raid's own Down rules. Nothing is refunded.
- Builds keep their own lifetimes across item swaps and Down; they simply cannot be released while the weapon is unusable.
- Death clears the owner's builds: the owner bookkeeping and every dormant carrier. Leaving the world clears everything.

### Ownership and replication

- **Owner client:** samples input, spends mana and ammo, keeps the bookkeeping (combo index, the organ's mark ledger, throw serials, cast ids), and alone spawns or changes projectiles. Only the owner deals damage.
- **Replication:** everything another client draws travels by native projectile replication (`netImportant`, `netUpdate`), as with the other Convergence weapons. No Encounter packet, ModPacket, protocol or Encounter state is added. The only point where the rewards touch the Raid is the Victory drop.
- **Visible builds:** a build other players should see rides a harmless carrier projectile: the scythe's staff, the baton's strokes, and the quills with their ink. The organ's marks stay owner-only, following Ebon's mark precedent, because four players' marks over shared targets would clutter the Raid; other players see the hymn.
- **Pure motion:** motion is a pure function of the replicated `ai`, position and velocity, so other clients rebuild it without a stream of updates.
- **Validation:** each projectile checks its `ai` (finite and in range) and kills itself on invalid data, as `CrimsonCompanionRay` does. A remote held projectile tolerates 6 ticks of held-item lag.
- **NPC identity:** a projectile that refers to an NPC carries its slot. The owner also keeps that NPC's `CrimsonCovenantIncarnation` value and retires the projectile when the slot is reused, as the companion does.
- **Boundaries:** Content never references Client code; presentation enters through static hooks, as `EbonHatbox.Opened` does. A dedicated server loads no texture, shader or sound. World unload clears client state; Mod unload releases cached assets.

### Presentation

- **Bodies and icons** are the Codex pixel art at the 2 px dot, drawn with point sampling. Textures are requested with `ImmediateLoad` or held as `Asset<T>`; a cached AsyncLoad `.Value` once left every Ebon reward sprite invisible.
- **Effects** are Luminance-managed painterly materials, never flat fill and never a pixel layer. The only pixel-dot effect fragments are bone chips and wax flakes, which are pieces of the sprites.
- **Draw equals collide:** every drawn damaging footprint is exactly the capsule list that collides, plus its anti-aliased rim. A circular hit is a zero-length capsule (a disc); "an N px disc" in this document has radius N. A live point opens over 3 ticks without the Raid strike's overshoot, so its collision radius is the radius × min(t / 3, 1). Flames, drips, embers and smoke never extend damage.
- **Reduced Effects** (`CrimsonVisualConfig.ReducedEffects`) halves particles and droplets, damps ignition flashes through the material's reduced flag, removes the censers' optional drape tails and disables shake. Shake also honours `CrimsonVisualConfig.ScreenShake`.
- There is no fullscreen flash, hit-stop, camera move or input lock.

### Black-blood material

All reward ink uses the owner-approved black-blood river of `ScarletInk.fx`: a dark ink body, burning crimson lips, red filaments streaming along it, a wandering hot core and an ignition blaze.

- **Existing passes are unchanged.** `AutoloadPass`, `ForecastPass` and `ResiduePass` keep their code. Offline frames of the Raid's strikes must match the approved frames. The rewards never use `ForecastPass`: it is the forecast hairline the owner rejected, and it carries a periodic heat pulse.
- **Three new path passes** are added to the same file: `PathLivePass`, `PathDormantPass` and `PathResiduePass`.
  - They evaluate the body over a triangle strip, with u in world pixels along the whole path and v across it. Noise therefore runs continuously along a curve, with no seams, doubled alpha or lip rings at joints. Round caps come from u outside [0, L].
  - Per-vertex data carries the radius, the time since that point ignited (or its fade), the path seed, opacity, and flags (fire, reduced). Every path of one pass therefore draws in a single call.
- **Live** (`PathLivePass`): the `AutoloadPass` body. With the fire flag (censer only) flame tongues lift off the lips, as on the Raid's Ember Crown strikes.
- **Dormant** (`PathDormantPass`): the build look. It is the residue scar held at fade 0.6: a narrow dried scar with a faint warm lip. An aperiodic, noise-driven ember breath animates it in every flavour; there is no periodic term. A full build warms its lips steadily, without pulsing.
- **Residue** (`PathResiduePass`): after a release, the `ResiduePass` scar for 20–24 ticks, harmless.
- **Sprite burn** (`SpriteBurnPass`): a fourth additive pass erodes a sprite from its edges by noise behind a burning crimson lip. Only the reliquary's exit uses it.
- **Writing head:** a path that is still being written, live or dormant, carries a small ember-gold bead at its head. The Raid's ink has no such bead, so it marks friendly black blood.
- **Flavour:** the only material difference is fire (`flavor.x`). The `y` channel is the Raid's legacy "silk" channel and stays 0. The scythe, organ, baton and quill are told apart by shape, motion and code-drawn accents (bone chips, wax flakes, the writing bead), not by flavour values.
- **Not used:** `ScarletRibbon` (`ScarletMaterials`) is an older Scarlet path material, not the approved black blood.
- **Droplets** are tiny live paths (two-point strips) in the same batch, so no Metaball render target is needed.
- **Particles** come from one fixed pool: embers (soft additive sparks), smoke (alpha, broken by noise), bone chips and wax flakes.

### Layering

- Reward ink draws once per frame through `ScarletRewardInk.DrawWorld`, which is frame-stamped and idempotent.
  - Scarlet's `PostDrawTiles` drawers, `CrimsonGestureVisuals` and `CrimsonChorusVisuals`, call it before they draw forecasts and chorus markers.
  - A reward ModSystem's `PostDrawTiles` calls it otherwise.
  - Whichever runs first draws. Reward ink therefore always lies beneath the Raid's forecasts and chorus markers, in the same world layer as the Raid's strikes (beneath NPCs and players).
- Pixel bodies (held weapons, hands, quills, censers, the score, the reliquary) draw as ordinary items and projectiles, above that layer.

### Multiplayer readability

- **Opacity:** the local player's builds and releases draw at full opacity. Other players' dormant builds draw at 0.5 and their live releases at 0.85.
- **Shape:** friendly black blood is short, curved or local. It never spans the field and never follows a forecast band, and its writing heads are ember-gold.
- **Shake** comes only from the local player's own weapon.
- **Sound:** build tolls play only for their owner. Per-swing and per-shot cues play for the owner at full level and for other players 8 dB lower with one voice. Windups and releases are positional for everyone.

### Audio

- **Pairs:** every attack pairs a windup cue with a release cue.
- **Toll ladder:** build steps ring `Toll0`–`Toll7`, an E♭ sus2 ladder: E♭3 F3 B♭3 E♭4 F4 B♭4 E♭5 F5. It has no third, so it sits under Graceful Ordeal's harmony whatever the mode.
- **Cadence:** releases end on `Cadence`, the same E♭–F–B♭ voicing on organ and tubular bell over a low timpani or gong stroke, or on a weapon's own finale cue built on that voicing.
- **Restraint:** tolls are short and soft. Per-shot cues are unpitched, so firing never becomes a music box.
- **Recordings only:** cues are layered CC0 recordings, with no synthesized tone or noise layer.
  - Libraries: VSCO 2 CE (including its organ and blower recordings) and VCSL (tubular bells, hand chimes, woodblock, slit drum, gong, timpani), both already in [Attribution](../../../Assets/ATTRIBUTION.md).
  - Foley: the Freesound and OpenGameArt CC0 foley already listed there.
  - Processing: trimming, envelopes, filters, resampling to pitch, layering and short reflections. Every pitched layer sits on E♭, F or B♭.
- **Voices** are bounded (`MaxInstances`, replace oldest), stop when the game is paused or loses focus, are never built on a dedicated server, and are skipped when a cue is not packaged yet.

## Nominal parity

The figure compared is raw damage per busy tick on one target at zero defense. A busy tick runs from the first build action until the later of the release's use time and its first damaging tick. The Ebon weapons are restated in the same unit:

- Moonshear: two katas and a cut, 15.9× in 204 ticks.
- Moonloom Harp: 32× in 152 ticks (glissando use 24).
- Ebon Thimble: 40× in 288 ticks.
- Ballroom Chandelier: 4.5× per 115 ticks.
- Severing Silk: 32× in about 152 ticks.
- The Last Waltz: 10.8 × 10,000 per 230 ticks.

| Class | Weapon | Base | Loop | Raw per tick | Ebon raw per tick |
|---|---|---|---|---|---|
| Melee | Sable Scythe | 3600 | 12.4× in 156 ticks | 286 | 281 |
| Ranged | Canticle Organ | 1900 | 28.25× in 136 ticks | 395 (before ammo) | 400 |
| Magic | Scarlet Baton | 2400 | 23× in 168 ticks | 329 | 333 |
| Summon | Ember Censer | 1000 | 5.2× per 134 ticks | 39 per slot | 39 per slot |
| Rogue | Bloodink Quill | 1800 | 37× in about 170 ticks | 392 (target stays on the ink) | about 380 |
| Companion | Scarlet Covenant | 1500 | 16–20× per 60 ticks | 400–500 | 470 |

Each weapon's section derives its figure. Partial builds are always worth less per tick than full ones, so the build matters and releases cannot be spammed.

## Scarlet Score Reliquary — 緋の聖譜櫃

`CrimsonScoreReliquary` is an ordinary openable container in every difficulty.

- **Item:** consumable, `Item.CommonMaxStack`, Red rarity, sell value 40 gold, `ResearchUnlockCount` 3, `CanRightClick`.
- **Loot:** `ModifyItemLoot` adds `ItemDropRule.OneFromOptionsNotScalingWithLuck(1, CrimsonRewardItems.RewardTypes())`.
- It is not `ItemID.Sets.BossBag`, because that flag injects vanilla developer drops.
- `RightClick` invokes a client-only static `Opened` hook for the opener.

**Victory drop.**

- `CrimsonRuntime.Cleanup` drops the reliquaries on `EndReason == Victory` only, before `ClearHazards`, as `EbonRuntime` does.
- Cleanup already runs after the 150-tick Victory melt, so the reliquaries appear as the giant finishes melting. The interval is not extended.
- One reliquary drops per frozen member (`members.Length`), including members who are Down or disconnected at Victory.
- Placement: at the pedestal ground, centred, 36 px apart, 180 px up.
- A `rewardsAttempted` counter is incremented before each `Item.NewItem`, so a cleanup retry never drops duplicates.
- Each attempt is logged through `CrimsonPackets.Log` as `event=VictoryRewardDropped fight=… index=… item=…` or `event=VictoryRewardFailed … reason=item_limit|<exception>`.
- Defeat, cancellation, the safety cap and invalidation drop nothing.

**Opening show.** A small, personal, client-only show plays about 70 px above the opener's head. It lasts 60 ticks, at most four play at once, and it is built from the SR01 parts.

- **0–4:** the closed reliquary appears and settles.
- **4–10, windup:** the wax seal heats. Three fine crimson cracks run across it from inside, and the casket trembles by one dot.
- **10, release:** the seal splits; its two halves (the seal sprite drawn as left and right halves) tumble away under gravity. The lid swings back on its rear hinge to about 110° in 8 ticks, with a small overshoot and no spin.
- **10–40:**
  - A soft crimson glow wells up out of the velvet. It is painted and moved by noise; there are no rays, rings or glyphs.
  - Five short dormant ink lines rise about 120 px from the mouth and curl apart like a staff. At tick 20 they ignite as live black blood with the cadence, then dry and break into embers.
  - A few black-blood droplets leap and fall back.
- **44–60:** the reliquary burns away from its edges: noise erosion with a burning crimson lip, not an alpha fade.

`ReliquaryOpen` is timed to the show: a heat crackle from tick 0, the wax crack and the lid's knock on tick 10, a rising run of chimes on the toll ladder, and the cadence near tick 20.

## Melee — Sable Scythe / 帷の大鎌

Sable Mantle's bone hook on a black lacquered haft, swung in figure eights.

- The swinging blade is true melee (Calamity `TrueMeleeDamageClass` through the compatibility adapter).
- The release's cuts are plain Melee damage, as Moonshear's Cut Line is. `Item.DamageType` switches in `CanUseItem`, as in Moonshear.
- Base damage 3600, crit 8. `useTime = useAnimation = 2`: each stroke owns its own clock.

**Left click — the figure eight.** Hold to keep swinging.

- One held stroke projectile exists at a time; the next stroke starts the tick after the previous one ends. The cycle returns to the first stroke after 60 idle ticks or an item change.
- The cycle is **Over, Under, Over, Under, Whip**:
  - **Over** (18 ticks, live 5–13, ×1.0): the hook comes over the shoulder and cuts down and forward through the aim. The hand rises with it: the Mantle's high slash floats.
  - **Under** (18 ticks, live 5–13, ×1.0): the hook comes from low behind and rises up through the aim. The hand sinks with it: the low slash sinks.
  - **Between halves the blade never stops.** The hand loops the haft behind the hip or shoulder and the hook rolls over the wrist into the next half, so the hook tip traces an ∞ along the aim.
  - **Whip** (28 ticks, ×1.8 plus a crescent at ×0.6):
    - After the second Under, the hook keeps rising behind the head and draws back on ticks 6–12: the Mantle's brace.
    - It then lashes forward flat through the aim, live on ticks 14–20, while the arm thrusts 24 px forward.
    - The hook tip's path over ticks 14–20 stays in the air as a black-blood crescent: radius 16, live until tick 26, ×0.6 once per root, then a 20-tick scar.
    - The Whip ends in the Over's starting pose.
- **No hard stops** (Moonshear's rule, extended):
  - Strokes join with matched angle and angular speed (Hermite knots).
  - The hook tip never moves slower than 3 px/tick, and its turns keep a radius of at least 24 px.
  - Through a roll, angular speed stays at least 25% of the stroke's peak; through the Whip's draw-back, at least 20%.
  - Peak angular acceleration stays under 0.3 rad/tick².
- **Reach** is measured from the exported art: grip anchor to hook tip at the 2 px dot (about 120–150 px at the briefed size). Placeholder tuning uses 136 px. The Whip adds the 24 px thrust.
- **Collision:** the curved bone edge as three capsules 26 px wide along the measured blade curve; the haft does not hurt. Collision is swept with 9 sub-samples per tick (Soboro's method). Each part of a stroke (the blade, the Whip's crescent) hits each root once per stroke through its own root ledger; native local immunity is only 1 tick, so the Whip's blade and its crescent can both land on the same NPC.

**Staff (the build).**

- Every stroke that damages at least one NPC engraves one line of a five-line staff, up to 5 lines. A full measure that connects therefore fills the staff.
- The staff hangs 40 px behind the shoulder: five 56 px dormant lines, 8 px apart. Each new line is written in with its ember-gold head and the next toll; a full staff warms its lips.
- Lines last 360 ticks after the last engraving, then drain one per 30 ticks.
- Other players see the staff through its carrier, at half opacity.

**Right click — Staff Reap / 五線刈り.** Needs at least one line; with none, nothing happens.

- **Input:** pressed during a stroke, the release begins when that stroke's live window ends (at most 8 ticks later); the pose blends from wherever the blade is.
- **Windup (ticks 0–16):** the reaper draws the scythe high behind and whips it forward at tick 16.
- **Placement:**
  - C is the cursor, clamped within 720 px of the player.
  - If a chaseable NPC's hitbox lies within 96 px of C, the staff centres on the nearest such NPC, and the line spacing becomes s = clamp(h / 4, 14, 36) px, where h is that NPC's hitbox height. Otherwise s = 36.
  - The staff therefore closes on its target. Every line crosses any target at least 56 px tall; smaller enemies take the middle three.
- **Lines:**
  - Horizontal, 840 px long (C.x ± 420), radius 12.
  - The middle line fires first and the rest fill outward, so a partial staff is always centred on its target.
  - Line k fires at tick 16 + S(k), alternately from the player's side and from the far side. Its head crosses the whole length in 4 ticks.
  - Each point is live for 10 ticks after the head passes: ×0.8 once per root per line. The line then dries into a 20-tick scar.
- **Final Barline / 終止線** (five lines only): at 16 + S(5) = 51, two vertical cuts 28 px apart drop through the staff at C.x.
  - Height 4s + 48 px, radius 16. They fall in 3 ticks and stay live for 12.
  - The pair deals ×2.0 once per root. The whole staff ignites together and dries together. Local shake 3.
  - The scythe holds its follow-through until tick 56, and the next Over starts from that pose.
- **Partial staff:** the scythe is released 8 ticks after the last line fires.
- **Scale:** the Raid's Shroud Rope lines have radius 36, lie 224 px apart and span the whole field. This staff is a small, aimed cut around one target. Its 840 px lines still cut crowds; that is an acceptance check.

**Nominal:**

- A measure (Over, Under, Over, Under, Whip) is 100 ticks for 6.4× base. Moonshear's kata is 82 ticks for 5.2×.
- A full staff on a target every line crosses adds 5 × 0.8 + 2.0 = 6.0× in a 56-tick release, during which the scythe cannot swing.
- Together that is 12.4× in 156 ticks. A one-line release (0.8× in 24 ticks) is worth less than swinging, so the staff is worth filling.

**State and replication.**

| Projectile | Role | Data |
|---|---|---|
| `SableStroke` | Held stroke | `ai = (stroke index, aim, age)`; `netUpdate` at age 1, at the live start and every 6 ticks. Velocity is data (`ShouldUpdatePosition` false): x = what came before (−1 nothing, 0 the previous stroke, 1–5 a Staff Reap of that many lines), y = the aim it was cast along, so every client eases the windup from the previous pose |
| `SableStaff` | Harmless carrier while lines > 0, drawn on the owner | `ai = (lines, ticks since last engraving, —)`; `netUpdate` on each change |
| `SableRelease` | Held release pose, harmless | `ai = (aim, lines, age)`; velocity is data: x = the interrupted stroke as index × 32 + age (−1 when cast from rest), y = that stroke's aim |
| `StaffCut` | Owner child, spawned at the cast (≤ 6 per cast) | position = the starting end; `ai = (signed length, index (5 = barline) + 8 × lines spent, age)`; a negative age is the wait |

**Presentation.**

- SR02 rotated about its measured grip anchor; SR02I icon.
- **Swing wake:** a `PathLivePass` strip sampled from the real hook-tip positions of the last 9 ticks. It tapers from 14 px to 0, is hot only in the live window and cools to residue as it trails. It lies inside the area the blade swept, so it never shows harm where there was none.
- A short white-red glint marks each crossing of the aim. On a hit, up to 6 droplets spray along the swing.
- The crescent, lines and barline are live paths with their ignition blaze, then scars.

**Audio.**

| Cue | When |
|---|---|
| `ScytheSwingHigh`, `ScytheSwingLow` | Each Over and Under: a drawn breath into the cut, peaking on the first live tick |
| `Toll(k)` | Line k engraved (owner only) |
| `ScytheWhipBrace` → `ScytheWhip` | The Whip's draw-back → the lash |
| `StaffWindup` → `StaffCut` | Release windup → each line |
| `StaffBarline` | Final Barline: a heavy double cut on the cadence voicing |
| `Cadence` | A partial staff: with the last `StaffCut` |

## Ranged — Canticle Organ / 聖歌のオルガン銃

Four bone organ pipes bound in a ribcage frame with a crimson heart-gem: the Thorn Choir as a gun.

- **Ammo:** uses bullets. Every shot becomes a bone shard; ammo damage and conservation stay native through `PickAmmo`.
- **Stats:** base damage 1900, use time 14 ticks (auto), crit 8.
- **Aim:** follows the cursor with a critically damped spring.

**Left click — shards.**

- The four pipes fire in turn; each kicks back 2 px and springs back.
- A shard flies at speed 20 with one extra update for 40 ticks (1,600 px). It pierces nothing: ×1.0 to the first NPC it hits. It shatters into bone chips on tiles.

**Marks (the build).**

- Each shard hit puts one mark on that NPC's root: up to 8 marks per NPC, on up to 8 NPCs. The ledger is keyed by root, type and incarnation.
- Marking a ninth NPC drops the marks of the NPC whose newest mark is oldest.
- Marks expire 360 ticks after that NPC's newest mark, and are cleared when it dies or its slot is reused.
- The owner sees one short dormant ink arc above each marked NPC, its width swelling into a note-head at each mark (one path per NPC). The newest note-head flares as it lands, with the next toll. At eight marks the arc's lips warm.

**Right click — Hymn of Hands / 手の聖歌.**

- **Conditions:** at least one mark, and no hymn from an earlier cast still pending. With no marks, nothing happens.
- **No ammo:** as the Moonloom Harp does, `CanUseItem` switches `useAmmo` to none and the use time to 24 when `altFunctionUse == 2`, so the hymn works with no bullets and costs none.
- **Pose:** 24 ticks. The organ lifts and inhales (0–10) and the heart-gem brightens.
- **Schedule (at the cast):** the marks are spent and every hand is spawned at once.
  - One hand per mark, assigned round-robin over the marked NPCs in order of each NPC's first mark, at most 16 per hymn. Extra marks stay.
  - NPCs more than 2,000 px from the player are skipped and keep their marks.
- **Each hand:**
  - Hand k slams at tick 10 + S(k).
  - It condenses out of a black-blood smear 240 px above its NPC, 8 ticks before its slam (2 ticks forming).
  - It falls 240 px in 6 ticks, accelerating and tracking the NPC's x, onto the NPC's centre.
  - The Choir's four arms take turns: outer left, inner right, inner left, outer right. Each hand is offset by ±25% of the NPC's width, at most 40 px.
  - On the slam it is live for 3 ticks in a 48 px disc around the palm: ×2.25 once per root it touches.
  - A hand whose NPC has died, or whose slot was reused, is retired.
- **Clasp / 合掌** (full build): a fully marked NPC's eighth hand is the Clasp.
  - Four hands fall together from the four arm offsets and close on the target.
  - ×4.5 to that root in a 64 px disc, plus a splash crown of six radial capsules (64 to 140 px out, radius 18): ×1.0 once per other root.
  - The Choir cadence and local shake 2.5.

**Nominal:** eight shards into one target (112 ticks, 8×), then a hymn (24 ticks): seven hands (15.75×) and the Clasp (4.5×). That is 28.25× in 136 ticks. Four marks give 13× in 80 ticks, so the full build pays.

**State and replication.**

- `CanticleShard`: ordinary owner projectile.
- `BoneHand`: owner child, `ai = (NPC slot, ordinal 0–15 + 16 × role, age)`, where the role is 0 a hand, 1 the Clasp, 2 the cadence hand (the last hand of a hymn without a Clasp, so every client plays the cadence on it); a negative age is the wait.
- The owner keeps the mark ledger and each hand's target incarnation. Other clients draw a hand at its NPC's replicated position.

**Presentation.**

- **Pixel art (SR03):** the gun, held at its grip and flipped vertically when aiming left, with four measured pipe-mouth anchors; the shard; the palm-down bone hand; the icon.
- **Muzzle:** a brief breath of crimson-black vapour at the firing mouth (smoke particles, not a disc).
- **Shard wake:** a short live path over its last 3 positions, radius 4, inside its own path.
- **Hands:** the forming smear and a short falling smear are dormant ink. The slam is a live disc with its ignition blaze, bone chips and droplets, drying over 24 ticks.
- **Readability:** hands exist only from 8 ticks before their slam, so no hand ever hovers over an enemy. That avoids any puppeteer image, and it reads differently from the Raid's Four Hands, whose upright fingers slam whole field quarters.

**Audio.**

| Cue | When |
|---|---|
| `OrganShot` | Each shot: an unpitched bone-pipe chiff and click (four variants, one per pipe) |
| `Toll(m − 1)` | Mark m on that NPC (owner only) |
| `HymnInhale` → `HandSlam` | Windup: bellows breath (the VSCO organ blower) and a pipe swell → each hand: bone crunch, low thump, wet splash |
| `ChoirClasp` | The Clasp: the slam on the full cadence |
| `Cadence` | A hymn without a Clasp: with the last hand |

## Magic — Scarlet Baton / 緋の指揮棒

Vespera's baton. Conducting writes black-blood strokes where you point, and the cutoff releases them.

- Base damage 2400, use time 18 ticks per stroke (hold to keep conducting), crit 8.
- Mana 8 per stroke (`CheckMana`, no automatic potion). The right click costs no mana.

**Left click — conduct and write.**

- **Gestures:** the baton beats the 4/4 pattern: Down, In, Out, Up. The player's clicks advance it, not a clock, and it returns to Down after 90 idle ticks.
- **Motion:** each 18-tick gesture is anticipation (3 ticks), sweep (8) and follow-through (7) about the grip. It starts from the previous gesture's end pose and speed, with a light rebound at each ictus and never a stop.
- **The stroke** is fixed at the gesture's first tick, so its whole shape is known when it is spawned:
  - It is a quadratic curve centred on the cursor (clamped to 900 px from the player), with a 180 px chord in the gesture's direction, mirrored when facing left.
  - The cursor's sweep over the last 6 ticks turns the chord by up to 35° toward the sweep and sets its bend (−64 to 64 px) and skew. Holding the mouse still gives a clean conducting mark; sweeping steers it.
- **Writing:** the pen writes the stroke during the sweep (6 ticks), led by its ember-gold bead. The freshly written ink is live for 6 ticks behind the head (radius 10): ×0.5 once per root per stroke. It then dries.

**Score (the build).**

- A written stroke dries into a dormant stroke, which is harmless. Each new stroke rings the next toll.
- A stroke lasts 480 ticks, fading over its last 60.
- At most 8 exist; a ninth dries the oldest away in 24 ticks.
- With eight, every stroke's lip warms steadily to show a full score.

**Right click — Tutti / 総奏.** Needs at least one dormant stroke and no tutti in progress.

- **Pose:** 24 ticks. The baton lifts (0–8) while the strokes' lips heat, and gives the downbeat at tick 8.
- **Ignition:** strokes ignite in writing order; stroke i at tick 8 + S(i).
  - Each swells from radius 8 to 24 in 3 ticks.
  - It is then a live black-blood river for 14 ticks: ×1.75 once per root per stroke.
  - It dries into a 24-tick scar.
- **Scope:** only the strokes dormant at the cast take part. Strokes written during a tutti belong to the next score.
- **Black-Blood River / 黒血の奔流** (eight strokes at the cast):
  - At 8 + S(9) = 71, the ink runs together into one river through all eight strokes in writing order. Straight runs join each stroke's end to the next stroke's start; a run longer than 900 px is left out, and the river continues from the next stroke.
  - Radius 28. Its head covers the whole path in 24 ticks, and each point stays live for 14 ticks after the head passes.
  - ×5.0 once per root. The cadence, local shake 4. The whole score then dries together.
- There is no orb or other new light material: the release is the black blood itself.

**Nominal:** eight strokes written across one target (144 ticks, 4×), then a tutti (24 ticks): eight ignitions (14×) and the river (5×). That is 23× in 168 ticks. Mana is 64 per score, about 23 per second (the Ebon Thimble spends about 20).

**State and replication.**

- **`BatonSwing`:** the held gesture, `ai = (gesture, aim, age)`.
- **`BatonStroke`:**
  - Its position is the chord's midpoint and its velocity holds the half-chord (`ShouldUpdatePosition` false, as the Moonshear, Moonloom Harp and Last Waltz projectiles already use velocity as data).
  - `ai = (shape, age, state)`. Shape packs bend and skew as an exact integer.
  - State uses the shared `CrimsonStrokeState` codec, exact in a float: dormant; scheduled (countdown 7 bits, rank 3 bits, cast id 6 bits); live; residue.
  - The owner sends `netUpdate` at the spawn and sets each stroke's schedule once, at the cast. Every client then counts down on its own, as with the Moonloom Harp's strings.
- **`BatonRiver`:** spawned at the cast, `ai = (age, negative while waiting; cast id; stroke count)`. Other clients collect the owner's strokes with that cast id, ordered by rank.

**Presentation.**

- SR04 drawn about its measured grip; its gem anchor is the pen's origin. The gem flares briefly at each ictus.
- For the first 6 ticks of a stroke, a few droplets fly from the gem toward the pen.
- Strokes are live while written, dormant while waiting, live through the tutti and the river, then residue.

**Audio.**

| Cue | When |
|---|---|
| `BatonStroke` | Each stroke: a wet swish ending in an ink tail |
| `Toll(k)` | Stroke k written (owner only) |
| `BatonLift` → `InkIgnite` | Windup: an inhaled swell → each ignition |
| `RiverRelease` | The river: a surge into the full cadence |
| `Cadence` | A partial score: with the last ignition |

## Summon — Ember Censer / 灰燼の香炉

Each use calls one small crown censer, the Ember Crown in miniature.

- One minion slot each; base damage 1000, crit 0, mana 10, use time 30.
- The buff (`CrimsonEmberCenserBuff`), sacrifice and minion targeting are native.

**Idle.**

- Censers hang from their own floating rings in a shallow arc above and behind the owner, up to six per row: slot i sits 40 + 34i px behind and 70 px up, odd slots 8 px higher, a second row 40 px higher.
- Each is a damped pendulum driven by the owner's motion. It swings when you run or stop and settles when you stand still.
- Thin smoke rises and a few embers trickle.

**Seek.**

- The target is the minion target if one is set, otherwise the nearest chaseable NPC within 1,200 px of the owner, kept out to 1,600 px.
- The censer glides to a station 200 px above the target's top. Censers sharing a target spread their rings across it: rank r of n sits at (r − (n − 1)/2) × min(72, (width + 96)/n) px.

**Swing (the build).**

- The pendulum pivots on the ring with the bowl about 60 px below it and a period of 64 ticks, chosen to have no simple relation to the 28.125-tick beat. Apexes come every 32 ticks.
- The first swing winds the amplitude from 25° to 55°: embers thicken and the bone crown warms.
- **Phase:** when a censer arrives, the owner starts its clock so its apexes fall in the middle of the largest gap between the apexes of censers already swinging over the same target. A censer is never re-phased afterwards. The pours therefore step from censer to censer across the target, and adding or losing a censer never makes the others jump.

**Pour (the release).** From the second apex on, at every apex:

- The bowl tips 35° inward over 4 ticks and pours a falling column of burning black blood from its mouth.
- The column's head falls 40 px/tick to the first solid tile below (sampled every 8 px), 600 px at most, with radius 24.
- Its top rides the mouth through the return swing, so the pour sweeps across the target for 16 live ticks.
- ×1.0 once per root per pour, then a 24-tick embered scar where it fell.

**Grand Pour / 大注ぎ** (every fourth pour):

- The censer swings out to 75° and braces for 6 ticks at the apex: the bowl trembles and its crown spikes heat ember-gold.
- It then dumps a wide curtain: radius 56, falling 60 px/tick, live 20 ticks, ×2.2 once per root.
- Local shake 1.5, only for the owner's own censers and at most once per 20 ticks.
- A cycle of four pours is 32 + 32 + 38 + 32 = 134 ticks.

**Target changes and owner state.**

- A new target: the station glides to it while the swing continues, without re-phasing.
- A lost target: the censer finishes its current pour, then returns to Seek or Idle.
- An unusable owner (dead or Down): the censers return to Idle and do not pour.

**Nominal:** per slot, 3 × 1.0 + 2.2 = 5.2× base per 134 ticks. The Ballroom Chandelier is 4.5× per 115 ticks.

**State and replication.**

- `ai = (state Idle/Seek/Swing, target slot, swing clock)`. The summon-order stamp travels in ExtraAI, as with the Ballroom Chandelier.
- The owner decides every transition and each censer's phase, with a `netUpdate` on each.
- Pose, apexes, pour count and grand pours are pure functions of the swing clock (`CenserRules`), so every client derives them.
- There is no child projectile. The censer collides only while pouring, using the column capsule (`CanDamage` true only in the live window), and clears its root ledger at each pour.

**Presentation.**

- SR05: the item icon, the unlit crown censer rotated about its measured ring anchor (bowl-mouth anchor measured too), and the buff icon.
- **Bowl:** an ember glow masked to the bowl's mouth, intensifying with each pour toward the grand pour.
- **Drapes:** two short drape tails (Verlet, 5 points) extend the sprite's drapes and swing with the pendulum. Reduced Effects removes them.
- **Pours:** live paths with the fire flag (flame tongues on the lips) and an ember splash at the floor. A pour is a short column under a visible censer, unlike the Raid's 256 px field columns.

**Audio.**

| Cue | When |
|---|---|
| `CenserSummon` | On summon |
| `CenserSwing` → `CenserPour` | Windup from 10 ticks before a pouring apex: chain creak and a rising breath of flame → a roaring pour |
| `CenserBrace` → `CenserGrandPour` | The brace → the curtain |

Sound budget per owner: at most one `CenserPour` per 7 ticks, one `CenserSwing` per 14 ticks and one `CenserGrandPour` per 20 ticks. The other pours are seen, not heard.

## Rogue — Bloodink Quill / 血墨の羽根筆

Quills of black feather edged in crimson. They use Calamity's `RogueDamageClass`, with stealth through `CalamityRogueArmament`. Calamity is a required dependency of the Mod, so the weapon always exists.

- Base damage 1800, use time 18, speed 20, crit 8.

**Throw.**

- **Flight:** the quill flies straight for 12 ticks, then falls with gravity 0.25 px/tick² (falling at most 16 px/tick), for up to 60 ticks.
- **Flight curve:** the flight is the pure `QuillFlight` curve, stepped exactly as native integration steps it (velocity, then position). Any client can therefore rebuild the path.
- **Outcomes:**
  - the first NPC hit: ×1.0, and the quill sticks in it nib first and moves with it;
  - a tile: the quill sticks in the tile;
  - timeout: the quill falls away and its ink fades.
- The arm follows through and returns along a curve.

**Ink (the build).**

- A flying quill visibly writes ink behind it, its nib carrying the ember-gold bead. When it sticks, the ink it wrote stays: a world-fixed calligraphic stroke from the throw point to where it stuck, tapering from 9 px at the nib to 3 px at the throw point.
- The ink is dormant and harmless. A stroke shorter than 48 px is kept as a blot at the nib, so a close throw still builds.
- Ink is written, not stretched: it never joins quills to each other, is never taut and never quivers. It cannot become a line strung between moving enemies.
- Each stuck quill rings the next toll. A quill and its ink last 480 ticks, fading over the last 60.
- At most 8 exist; a ninth pulls the oldest out. A quill whose NPC dies stays where it was.

**Stealth strike — Sealed Score / 封緘の楽譜.**

- **Claim:** the score takes the quills and ink standing when it is thrown. Quills thrown afterwards belong to the next build.
- **Flight:** the rolled score flies toward the cursor at 22 px/tick for at most 30 ticks; a tile stops it early.
- **Windup (8 ticks):** it halts and unseals. Crimson cracks run over the wax seal, its two halves fall away, and the score turns and unrolls. Five faint staff lines flash outward from it as a harmless dormant flourish.
- **Ignition (the unroll tick, U):**
  - Every claimed ink stroke ignites at its nib end. The flame front runs back to the throw point at 180 px/tick, and each part burns for 12 ticks behind the front.
  - Radius 18, ×1.75 once per root per stroke.
- **Playback:** the quills burst in throw order, quill i at U + S(i).
  - Each burst is a 56 px disc around the nib: ×1.5 once per root.
  - Each rings a toll chosen by the quill's height relative to the score: the higher the quill, the higher the toll. The melody you threw plays back.
- **Score burst:** at U + S(n), a sixteenth after the last of n quills, the score bursts: a 120 px disc, ×2.0. With a **Full Melody** of eight quills it is 160 px, ×3.0, with the cadence and local shake 3.5.
- Quills and ink are spent. With nothing claimed, only the score bursts.

**Nominal:**

- Eight quills stuck in one target (144 ticks, 8×), then a Sealed Score thrown about 400 px (about 26 ticks to the unroll).
- The ink (14×), eight bursts (12×) and the Full Melody (3×) bring it to 37× in about 170 ticks.
- That figure assumes the target stays on the ink. Against a target that has moved off all of it, the quills, bursts and score still give 23× (about 244 per tick). The ink is the reward for pinning a target down, as Severing Silk's strands are.
- Calamity's stealth regeneration bounds it further.

**State and replication.**

- **`BloodinkQuill`:** `ai = (stuck, serial, offset)`.
  - `stuck` is 0 while flying, NPC slot + 1 when stuck in an NPC, and −1 in a tile.
  - The serial runs 0–1023 and is compared modulo 1024.
  - The offset packs the stick offset from the NPC's centre (x and y within ±512 px, 1 px steps) as an exact integer.
  - In flight, position and velocity are native. The owner sends `netUpdate` on the stick tick, and other clients then place the quill at its NPC's centre plus the offset.
- **`BloodinkTrail`:** spawned by the owner when a quill sticks. Position is the throw origin, velocity the initial velocity, `ai = (stop tick, serial, state)`. Every client rebuilds the ink from `QuillFlight`.
- **`SealedScore`:** `ai = (age, flight ticks, tag)`, where tag = cast × 9 + n: the claiming cast id (0–1023) and the n quills it claimed (0–8), so a late-joining client can rebuild the timeline without a packet. At the throw the owner sets the claimed trails' state, with `netUpdate`.
- **Timeline:** each client runs the ignition timeline from a tick stamp taken when it first sees the unroll (the Severing Silk method). The owner processes damage, as for any player projectile.

**Presentation.**

- **Pixel art (SR06):** the quill (flying and stuck, rotated along its flight, with the nib at the stick point), the icon and the rolled score.
- **Code:**
  - wet ink written behind a flying quill, then dried ink (dormant);
  - the seal's crimson cracks, and wax flakes (pixel-dot fragments in the seal's red);
  - the burning ink, the bursts and the score burst (live), then scars.

**Audio.**

| Cue | When |
|---|---|
| `QuillThrow` | Each throw |
| `QuillStick` | The nib enters flesh or stone, with `Toll(n − 1)` for the n-th quill standing (owner only) |
| `ScoreUnseal` → `InkBlaze` | Windup: the wax cracks and the paper unrolls → the ink catches |
| Tolls | Each burst, by height; positional for everyone because they are part of the release |
| `ScoreChord` | The score burst on the full cadence |

## Scarlet Covenant — 緋の契約

`CrimsonPact` keeps its stable ID.

- **Recipe:** one of each of the five weapons (consumed, from `CrimsonRewardItems.RewardTypes()`), crafted at a Bookcase. This replaces the current recipe (Scarlet Grimoire, 10 Silk, 5 Souls of Night). The Grimoire remains only the Raid key. Existing Covenants keep working.
- **Behaviour** is owned by the [encounter spec](ENCOUNTER_SPEC.md#scarlet-covenant-companion): Vespera walks or floats beside the owner and, every 60 ticks, opens paired seals and beams on up to 20 NPCs chosen by current HP, concentrating size and damage on fewer targets. The only change is [owner decision 3](#owner-decisions): while her owner is dead or Down in any Raid, she opens no new cast and her live rays stop, as The Last Waltz does.
- **Item stats (owned here):** base damage 1500 (was 900), mana 10 (was 20), 10 slots, one per owner, Red rarity.
  - On one target, a cast deals ×4 per hit. Its ray eases in over 7 ticks, so with a 12-tick hit cooldown in the 52 live ticks it lands 4 or 5 hits: 16–20× base per 60 ticks, or 400–500 raw per tick. The Last Waltz is about 470.
  - Against 20 targets each hit is ×1, so the crowd total rises with the item damage too. Crowd damage stays the Covenant's identity and is an acceptance check.
- **Names:** the Japanese display name becomes 緋の契約 (was 紅の盟約), and the buff and ray names follow. The English name stays Scarlet Covenant.
- **Tooltip:** the outdated "up to 10 enemies" becomes 20, matching the encounter spec.
- **Art:** SR07's item icon and buff icon replace `CrimsonPact.png` and the borrowed vanilla Pygmies buff. Vespera's body stays `ScarletConjurer`. The Covenant keeps its existing sounds.
- The encounter spec's companion section links here for acquisition and item stats.

## Effect bounds

| Weapon | Carrier projectiles per owner | Longest life | Ink paths per owner |
|---|---|---|---|
| Sable Scythe | 1 stroke, 1 staff carrier, 1 release pose, ≤ 6 cuts per cast | lines 360 + up to 150 drain; cuts ≤ 90 from the cast | ≤ 20 |
| Canticle Organ | ≤ 3 shards in flight, ≤ 16 hands per hymn | hand ≤ 145 from the cast | ≤ 56 (8 of them owner-only mark arcs) |
| Scarlet Baton | 1 swing, ≤ 9 strokes (8 + 1 drying), 1 river; while a tutti burns, the next score's ≤ 9 strokes add to its ≤ 8 | stroke 480; river ≤ 135 from the cast | ≤ 11 outside a tutti (the river ≤ 600 sample points); a tutti overlapping the next score adds that score's strokes |
| Ember Censer | 1 per slot | minion | 2 per censer |
| Bloodink Quill | ≤ 4 in flight, ≤ 8 stuck, ≤ 8 trails, 1 score (a stealth throw waits while the owner's score is alive) | quill 480 | ≤ 28; briefly about 34 while a score's claimed ink, a newly stuck quill's ink and the windup flourish overlap |

- **Per frame, all owners together:** at most 256 ink paths and 8,192 strip vertices. Paths are sampled every 8–12 px, more densely on curves. The per-owner path counts above are design sizes; a brief overshoot is trimmed by this cap and its drop order, never by a weapon.
- **Over the cap,** drop in this order:
  1. other players' dormant ink;
  2. residue;
  3. other players' live ink;
  4. the local player's dormant ink.

  The local player's live ink is never dropped.
- **Draw calls:** each pass draws all of its paths in one call, so reward ink costs at most three draws per frame. A rendering exception disables reward ink for the session, with one warning.
- **Droplets:** at most 48 per owner and 160 in total, outside the path cap.
- **Particles:** at most 200 per owner and 600 in total.
- **Reduced Effects** halves droplets and particles.
- The renderer uses fixed arrays, allocates nothing per frame, restores the SpriteBatch and device state, and owns no render target.

## Implementation shape

**Content: `Content/Encounters/CrimsonFoundry/Rewards/`.**

- **Pure rule files** with no Terraria or XNA references, linked by the domain tests the way `EbonRewardRules` is:
  - `CrimsonRewardRules`: S(k), the damage and use-time tables, every constant in this document, and capsule, disc and box tests.
  - Per-weapon rules: `SableScytheMotion`, `CanticleRules`, `BatonRules`, `CenserRules`, `QuillRules` (including `QuillFlight`).
  - `CrimsonStrokeState`: the shared codec.
- **Independence from Ebon:** these files do not reference `EbonRewardRules`. Any helper worth sharing moves to `Common` in its own change.
- **`CrimsonRewardItems`:** `Icon` (with a `HasAsset` fallback), `Usable`, `DamageClassFor`, `TypeFor`, `RewardTypes`, `Defaults`, `Hit`. `RewardTypes` is the one pool for both the reliquary and the Covenant recipe.
- **`CrimsonRewardSprites`:** the one table of final sprite names, planned sizes, anchors and vanilla placeholders.
- **Items and projectiles:** the six items and the projectiles named in each section.
- **Runtime:** `CrimsonRuntime.DropRewards`, guarded by `rewardsAttempted`. The rewards add no Encounter policy, coordinator or packet-router code.

**Client: `Client/Encounters/CrimsonFoundry/Rewards/`.**

- `ScarletRewardInk` sits on the Terraria-free Vfx seam (`IScarletAssets`, `ScarletView`), so the offline preview renders it.
- Per-weapon visuals and the reliquary show.
- `ScarletRewardAudio`: built only off a dedicated server; skips a cue whose file is missing.
- `ScarletRewardArt`: `ImmediateLoad` or `Asset<T>`, never a cached AsyncLoad `.Value`.

**Shader:** `ScarletInk.fx` gains `PathLivePass`, `PathDormantPass` and `PathResiduePass`; `tools/compile_shaders.py` rebuilds the `.fxc`. The existing passes keep their behaviour.

**Tools:**

- `tools/export_scarlet_reward_art.py`;
- `tools/generate_scarlet_reward_sfx.py`;
- the offline fixture `tools/fixtures/ScarletRewardsPreview.cs`, as `EbonRewardsPreview`, run by `tools/preview-scarlet.ps1 -Rewards`.

**Localization:** `Localization/CrimsonRewards/{en-US,ja-JP}.hjson`, plus the Covenant's entries in `Localization/CrimsonFoundry`.

**Domain tests.**

- S(k) rounding.
- Every nominal figure in this document.
- The figure eight: continuity at the joins, minimum tip speed, turn radius and angular-acceleration bounds.
- Staff placement and spacing, the cut schedule and the barline only at five lines.
- The mark ledger: caps, expiry, eviction and incarnation; hymn round-robin order, the 16-hand cap, the range skip and the Clasp only at eight marks.
- Stroke geometry, the codec round trip and its bounds; the river only at exactly eight strokes, and its run-skipping.
- Pendulum apex times, the grand-pour cycle and the largest-gap phase choice.
- `QuillFlight` equals native integration; the burn front; the quill offset codec.
- Commitment: unstarted parts die on Down or death; builds survive Down and item swaps.
- Every projectile rejects invalid `ai`.

**Source-wiring checks.** No Client reference from Content; `Usable` gating on every weapon; the drop is Victory-only and counted before each grant; the rewards never call `ForecastPass`.

**Merge note.** `feat/scarlet-signature-moves` also edits `CrimsonRuntime` and the encounter spec. Expect a small conflict and rebase after it lands.

## Art and audio

Final pixel art comes from Claude's brief for Codex, `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md`, kept outside the repository with the deliveries. The brief is binding for shapes and parts.

**Export.** `tools/export_scarlet_reward_art.py` is mechanical, as Ebon's is:

- it checks each input's hash;
- it measures the dot pitch per sheet and takes the majority colour of each logical cell;
- it lists the measured anchors in the tool header.

Exports go to `Assets/Textures/Items/ScarletRewards/`: one texel per logical pixel, drawn at 2×. Selections and exports are recorded here on delivery, and [Attribution](../../../Assets/ATTRIBUTION.md) owns rights and exact identities.

**Placeholders.** Until delivery, items use vanilla textures by reference, so the mechanics can be played first: the reliquary `CrimsonFishingCrate`, the scythe `DeathSickle`, the organ `OnyxBlaster`, the baton `CrimsonRod`, the censer `ImpStaff`, the quill `BoneJavelin`. In-world bodies draw the item's placeholder texture (so the organ's shard and bone hand are the Onyx Blaster too).

- `CrimsonRewardSprites` is the one table of final names, planned sizes and placeholders; a delivered PNG under its root replaces the placeholder with no code change.
- Anchors to replace with the exporter's measured values on delivery (until then they are proportional stand-ins):
  - scythe: `ScytheArt` grip and hook tip, and `SableScytheMotion.BladeKnots` (the three blade capsules' knots; the placeholder values were measured on the Death Sickle);
  - organ: `OrganArt` grip, heart-gem, pipe mouths and palm;
  - baton: `BatonArt` grip and gem;
  - censer: `CenserBody` ring and bowl mouth in `CenserInk`, and the pendulum's 60 px `BowlDrop`, which the briefed 28 × 30 art (about 24 px from ring to mouth) does not reach;
  - quill: `QuillArt` nib and the score's seal.
- The held organ needs Ebon harp's enlargement with point sampling once real art arrives; the shard and hands already switch to it.

| ID | Use |
|---|---|
| SR01 / SR01P | Reliquary icon (complete), and the body, lid (rear hinge measured) and wax seal for the opening show |
| SR02 / SR02I | Held scythe (haft from lower left to blade at upper right; grip and hook-tip anchors) and icon |
| SR03 | Organ gun (held, facing right; four pipe-mouth anchors), bone shard, bone hand (palm down), icon |
| SR04 / SR04I | Held baton (grip at lower left; grip and gem anchors) and icon |
| SR05 | Censer item icon, unlit crown censer minion (ring and bowl-mouth anchors), buff icon |
| SR06 | Quill (nib anchor), icon, rolled score |
| SR07 | Covenant item icon and buff icon |

As the brief requires, the images contain no light, flame, black blood, strokes, staff lines, magic circles, sparks, trails, flashes or debris. The only such elements drawn into the art are the reliquary's wax seal, the ink bead on the quill's nib and the faint staff lines printed on the rolled score. Everything else is drawn in code.

**Cues** (35), in `Assets/Sounds/Weapons/ScarletRewards/`:

- shared: `ReliquaryOpen`; `Toll0`–`Toll7`; `Cadence`;
- scythe: `ScytheSwingHigh`, `ScytheSwingLow`, `ScytheWhipBrace`, `ScytheWhip`, `StaffWindup`, `StaffCut`, `StaffBarline`;
- organ: `OrganShot`, `HymnInhale`, `HandSlam`, `ChoirClasp`;
- baton: `BatonStroke`, `BatonLift`, `InkIgnite`, `RiverRelease`;
- censer: `CenserSummon`, `CenserSwing`, `CenserPour`, `CenserBrace`, `CenserGrandPour`;
- quill: `QuillThrow`, `QuillStick`, `ScoreUnseal`, `InkBlaze`, `ScoreChord`.

**Sources and rendering.**

- `tools/generate_scarlet_reward_sfx.py` reads already-attributed CC0 recordings from the local source store, checks their SHA-256 and never commits raw recordings.
- Tolls are a woodblock or slit-drum knock over tubular bell and hand chime, resampled to pitch. Organ cues use the VSCO 2 CE organ and its blower recording.
- Fire, liquid, paper and wax layers are likely missing from the attributed set. Any new recording needs the same CC0 survey record and owner approval of that exact recording on the audition page before use, and Attribution records it.
- Rendering is deterministic, with pinned Ogg serials.
- Loudness follows the Ebon script (BS.1770): releases peak about 2 dB under the Raid's loudest strike cue; build and per-shot cues sit at least 8 dB under it; true peak is at most −1 dBTP after the Vorbis round trip.
- The owner auditions variants on a local page before anything is committed.

## Differences from Ebon

- **Clock:** there is no shared beat clock. Ebon's chandeliers, thimble and companion run on AutoMatador's beat. Here only a release cascade uses tempo sixteenths, counted from the release, and the censers keep their own tempo-free pendulum clocks.
- **Effects:** Luminance materials, not a half-resolution pixel layer. Only sprites are pixel art.
- **Builds:** dormant black-blood ink that releases re-ignite, not chalk marks or silk lines. The rogue's ink is written along the flight and never taut, unlike Ebon's pinned strands.
- **Audio:** recordings only, an E♭ sus2 toll ladder and a windup/release pair per attack, instead of synthesized B-minor silk plucks.
- **Companion:** the existing Scarlet Covenant is re-crafted with new item stats, not a new body or pattern. It is crafted at a Bookcase, matching the Grimoire, instead of a Work Bench.
- **New rules:** a committed-release rule; other players' dormant builds draw dimmed; build tolls are owner-only; reward ink always lies beneath the Raid's forecasts.
- **Vocabulary:** no silk, thread, lace or sewing, no puppets and no furniture.

**Not in scope.**

- Any change to the Scarlet fight: Raid HP, the one-damage rehearsal, choreography, hostile visuals, forecasts, music, or the existing `ScarletInk.fx` passes.
- New packets, protocol, persistence or saved progress.
- Covenant targeting, beams, concentration and AI (owned by the encounter spec), and new companion poses.
- The Scarlet Grimoire recipe, which still uses Silk.
- Set bonuses, armour, accessories, trophies, relics, masks and pets; Expert or Master extras; Luck effects; exchanges, reverse recipes and developer items; interactions with the Ebon set.
- Ebon or Doll reward changes, and moving shared helpers into `Common`.
- Balance against measured DPS. The numbers here are nominal starting values.
- Public release gates.

## Owner decisions

Decided on 2026-10-02 for the first implementation; the damage and mana numbers above stay as written.

1. **Reliquary ownership:** shared world items, as the Doll and Ebon boxes are, so any player can pick up another's. One drops per frozen member at the field's ground centre.
2. **Quill ink against moving targets:** the ink stays where it was written, so it never becomes a line strung between enemies, and it misses a target that has moved away.
3. **Covenant while its owner is Down:** Vespera stops attacking while her owner is dead or Down in any Raid, as The Last Waltz does. The encounter spec's companion section records it.

## Implementation choices

The first implementation fixed these points, which the sections above left open or ambiguous. They are starting values like the rest of this document.

**Shared.**

- `CrimsonCovenantIncarnation` numbers an NPC on its first read when `OnSpawn` did not run (always on a multiplayer client), so a client owner also retires marks, hands and quills when a slot is reused. The Covenant's own guard benefits too.

**Sable Scythe.**

- **Figure eight:** the scythe keeps turning, and every half the swing plane flips about the aim axis, so the hook tip traces the ∞. "Angular speed" in the no-stop rules is the 3D angular speed (rotation and roll together).
- **Whip draw-back:** the rotation reverses for an instant while the blade rolls; the 3D angular speed stays at least 20% of the peak.
- **Release during the Whip:** Staff Reap starts when the crescent stops being live (tick 26), not at the end of the blade's live window.
- **Full staff:** a connecting stroke on a full staff engraves nothing but restarts the 360-tick clock.
- **Final Barline:** the staff's lines keep their scars until the barline has dried, so the staff dries together. The "whole staff ignites" moment is ember particles along the lines, not live ink, because live ink without collision would break draw equals collide.

**Canticle Organ.**

- A hit on an NPC that already has 8 marks adds no mark and rings no toll, but restarts its 360-tick expiry (Ebon's marks do the same). The toll plays at the owner.
- A hand is live on ticks 1–3 after its slam, its disc opening 16 → 32 → 48 px. For death or Down a hand has started once it has slammed.
- The Clasp hits other roots at ×1.0 if they touch either its 64 px disc or the crown.
- The whole organ kicks back 2 px per shot; the pipes do not move separately. Shards are `netImportant`, so the owner's kill reaches every client.

**Scarlet Baton.**

- A dormant stroke has radius 10. Skew is limited to ±40 px, which keeps every curve's radius at least 44 px so the river's strip never folds inside a bend.
- The river's runs between strokes are straight, but their last 32 px at each end bend along the stroke, so the strip never doubles back at a joint. "≤ 600 vertices" is 600 sample points.

**Ember Censer.**

- The top of a pour (about 40 px under the mouth) stays hot and the column below cools early to black blood; drawing and collision use the same column. A finished pour dries into its scar where it fell.

**Bloodink Quill.**

- A quill burst is live for 8 ticks and the score burst for 12; both leave a 22-tick scar.
- An ink stroke shorter than 48 px is a blot of radius 12.
- The playback toll rises one step per 32 px of height above the score; a quill level with the score rings `Toll4`.
- After its flight the score glides to a halt over 4 ticks. A claimed quill or trail waits at most 58 ticks for its score to unroll, then gives up.
- The seal's "two halves" fall as wax flakes, because SR06 has no separate seal part.

**Open for the owner.**

- **Censer spread:** with the spacing above (`min(72, (w + 96) / n)`) and a ±49 px swing, the outer censers of three or more over a target about 80 px wide miss it with their outward pours (half of their pours). Large bosses are hit by every pour. Tightening the spread is a balance decision.

## Acceptance (owner, not_run until played)

**Owner play checks:**

- Opening reliquaries.
- Each weapon's build, release, finale and paired cues.
- The scythe's figure eight flowing without stops, with its rise and sink; the staff closing on targets of different sizes; the Final Barline.
- The organ's marks and the hand cascade; the Clasp.
- Writing with the baton, steering strokes, the tutti and the river.
- Censer pendulum and pour timing; the stepping pours with three or four censers; the grand pour.
- The quill melody playing back, and the ink against a moving boss.
- Readability over the Scarlet sanctum background and on bright and dark surface ground.
- Telling friendly black blood apart from the Raid's strikes in a real fight; weapon effects never hiding forecasts or chorus markers.
- A second player seeing the builds and releases, with dormant builds dimmed.
- Reduced Effects and the shake switch.
- FPS with eight players' full builds and with ten censers.
- Balance against current Calamity endgame gear, including the staff's crowd cuts and the Covenant at 1500 against crowds.
- The mix against the Raid's cues and Graceful Ordeal, auditioned on the local page.

**Automated evidence** (does not replace the checks above):

- Domain tests of the rules (see [Implementation shape](#implementation-shape)).
- Static, catalog and source-wiring checks.
- Offline frames of the Raid's strikes matching the approved frames after the shader change.
- Offline frames that composite the real sprites with the real reward ink at actual size, over the Scarlet background and a bright surface.
- Loudness and peak checks.
- A native package build.
