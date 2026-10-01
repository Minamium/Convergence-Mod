---
doc_id: encounter.ebon-manor.rewards
document_type: spec
status: provisional
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-10-02
source_of_truth_for:
  - encounter.ebon_manor.rewards
aliases:
  - Ebon Hatbox
  - 黒絹の帽子箱
  - Ebon reward weapons
related_code:
  - Content/Encounters/EbonManor
  - Client/Encounters/EbonManor
related_docs:
  - encounter.ebon-manor.spec
  - encounter.ebon-manor.assets
  - encounter.first-severance.weapons
---

# Waltz of the Ebon Manor — rewards

The owner asked (2026-10-02) for a Doll-style reward set for this Raid: a treasure box and one weapon per class, "good ones that hit the dopamine", built with the care the owner liked in Soboro (pixel-art cuts, flowing motion, layered CC0 audio). Each weapon turns one of Noirette's own techniques into the player's: the shears, the loom strings, the thrown furniture, the chandeliers and the severing web; the companion brings the parasol waltz and Noirette herself. [Encounter spec](ENCOUNTER_SPEC.md) owns the fight; current package and verification belong to [Status](../../STATUS.md).

## Shared rules

- **Signature:** every weapon builds something visible (marks, strings, raised furniture, hanging chandeliers, a web) and releases it in one satisfying moment. Hits that build climb a tuned silk-pluck arpeggio in B minor (the song's key); a release lands on a chord. Weapon timing uses AutoMatador's tempo (125 BPM: beat 28.8 ticks, sixteenth 7.2 ticks) so the set feels musical even without the Raid music.
- **Acquisition:** accepted Victory drops one **Ebon Hatbox** per frozen Raid member at the field's ground centre (shared world items, like the Doll box). Right-click opens one: exactly one of the five weapons, 20% each, Luck-independent. Five different weapons craft **The Last Waltz** at a Work Bench. No other drop, exchange or reverse recipe.
- **Power:** the same tier as the Doll weapons (Red rarity, sell 40 gold). Nominal budgets below are raw, pre-defense seeds, not measured DPS. Do not change Boss HP to hide weapon balance.
- **Usability:** a weapon cannot be used, and its held/channel controllers stop, while the owner is dead, Down in the Ebon Raid (`EbonRecoveryPlayer.IsIncapacitated`), or Down or eliminated in the Doll Raid.
- **Ownership:** the owner client samples input, spends mana/ammo, tracks its own marks/strings and spawns child projectiles through native projectile replication, as the other Convergence weapons do. Remote peers draw the replicated projectiles; owner-only bookkeeping (marks, the string list) is visual for peers only when it is carried by a projectile. No Encounter packet or protocol change. World unload clears client state; Mod unload disposes render targets.
- **Presentation:** pixel art at the Terraria 2-pixel dot, drawn with point sampling. A half-resolution Ebon pixel layer (the Soboro technique, our own palette: navy-black outline, charcoal, silver, ivory, moon white, dusty rose) draws cuts, threads, tears, shards and stars with a one-dot outline. Reduced Effects (the Ebon client config) halves particle counts and disables shake; screen shake also honours the config's shake switch. No fullscreen flash or hit-stop.
- **Audio:** `tools/generate_ebon_reward_sfx.py` renders the cues below from the already-attributed CC0 recordings plus original synthesis (Karplus-Strong silk plucks tuned to B minor), deterministic with pinned Ogg serials, into `Assets/Sounds/Weapons/EbonRewards/`. Voices are bounded and focus-gated like Soboro.

## Ebon Hatbox — 黒絹の帽子箱

`EbonHatbox`: consumable, stack 9999, Red, `CanRightClick`, loot via `ModifyItemLoot` with `ItemDropRule.OneFromOptionsNotScalingWithLuck(1, EbonRewardItems.RewardTypes())`. Not `ItemID.Sets.BossBag`. Opening plays a small personal show above the opener: the ribbon bow unties and flies off as two silk streamers, the lid pops up spinning, lace and moonlight motes burst out, a rising pluck run ends on a chord (`HatboxOpen`). Victory drop: `EbonRuntime.Cleanup` on `EndReason == Victory` only, one per frozen member, guarded by a `rewardsAttempted` counter incremented before each `Item.NewItem` (no duplicate on a cleanup retry), logged as `event=VictoryRewardDropped`/`VictoryRewardFailed`.

## Melee — Moonshear / 月裁ちの大鋏

Giant tailor's shears wielded as twin blades; true melee (Calamity `TrueMeleeDamageClass` through the compatibility adapter). Base damage 3600, crit 8.

- **Left click — four-stroke kata** (one held stroke projectile at a time, combo resets after 80 idle ticks or an item change): A upper blade, downward diagonal (18 ticks, live 7–14); B lower blade, rising reverse (16, live 6–12); C upper blade, wide horizontal (18, live 7–14); D **Snip** — both blades swing open behind, drive forward and close at full reach (30, live 14–20, ×2.2, shake 3.5). Reach 162 px (the drawn blade at the 2 px dot), blade capsule 26 px, swept collision with 9 sub-samples per tick (Soboro's method), 24-tick local immunity per stroke. Strokes join with matched angle and speed (Hermite knots, peak angular acceleration under 0.3 rad/tick²) so the kata flows.
- **Marks:** each A/B/C hit puts one tailor's-chalk mark on that NPC (max 5, owner-tracked, expire after 360 ticks without a new mark, cleared on death). Marked NPCs show small stitched crosses.
- **Right click — Cut Line:** a chalk stroke runs from the player to the cursor (max 900 px) for 18 ticks, then the open shears race along it in 12 ticks and the 64 px band tears for 10 ticks (×1.5 to every NPC touching it). Then every mark on every NPC the band touched pops in order, one per 4 ticks (×0.8 each), climbing the arpeggio; the last pop rings the chord. Cooldown 90 ticks after the tear. Like the Doll claws' crush, the Cut Line and its pops deal plain Melee damage; only the swinging blades are true melee.
- Nominal: a full kata is 82 ticks for (1+1+1+2.2) = 5.2× base; a five-mark cut adds 1.5 + 4.0 = 5.5×.

## Ranged — Moonloom Harp / 月光の糸竪琴

A lyre-shaped bow. Uses arrows; every shot becomes a silver needle arrow (ammo damage and conservation stay native through `PickAmmo`). Base damage 1900, use 16 ticks, speed 22 (one extra update).

- **Strings:** an arrow that hits an NPC, hits a tile or reaches 1600 px leaves a taut silk string from where it was fired to where it ended (strings shorter than 24 px are not made). Up to 8 strings (the oldest is replaced), each lasting 480 ticks and fading in its last 60. Strings are harmless until plucked; they shimmer faintly and quiver when an enemy crosses them.
- **Right click — Glissando:** plucks every string in order of age, one per sixteenth (7.2 ticks). A plucked string flashes and is live for 10 ticks: ×3.0 to every NPC crossing it (20 px wide, once per string per NPC). Each pluck plays the next arpeggio note; the last ends on `HarpChord`. Plucked strings are spent.
- The bowstring is drawn in code between the two horn-tip pegs and pulls back with each draw.
- Nominal: 8 arrows (2.1 s) then a glissando into a target they all hit = 8 + 24 = 32× base in about 3.1 s.

## Magic — Ebon Thimble / 黒絹の指ぬき

A silver thimble; silk from the fingertips lifts the manor's furniture. Channel weapon, base damage 2400, mana 12 per lifted piece (`CheckMana`, no automatic potion).

- **Hold:** the first piece rises after 6 ticks, then one more on every beat (28.8 ticks), in order armchair, candelabra, portrait, clock, birdcage, mirror, music box, cello. Raised pieces hang in a fan above and behind the player, bobbing, each on a silk thread from the hand.
- **Release:** pieces are yanked at the cursor one per sixteenth, accelerating (10 px/tick + 1.2 px/tick², max 40), crashing into the first NPC or tile: ×2.5 and a burst of wood/glass/silk shards.
- **Finale:** releasing with all eight raised drops a black **grand piano** 520 px above the cursor two sixteenths after the last piece; it falls (6 px/tick + 0.8 px/tick²) and lands with a 220 px crash: ×20, a crashing piano chord, shake 4.
- Nominal: a full score (8 beats + release, about 4.8 s) is 20 + 20 = 40× base.

## Summon — Ballroom Chandelier / 舞踏会のシャンデリア

A silver chandelier pole summons one chandelier per use (1 slot each), base damage 1000, crit 0, normal minion targeting and sacrifice.

- Idle chandeliers hang above and behind the owner on threads that run up off-screen, candles lit, swaying.
- With a target, each chandelier glides to hang 260 px above it (several spread left to right in beat order within reach of the target's width, odd slots 28 px higher), sways for 14 ticks, its thread is snipped and it falls (2 px/tick + 1.1 px/tick², max 30) to the target or the ground: a 110 px shatter (×4.5 once per NPC per drop) of crystal and candle sparks. Its pieces then fly back together over 24 ticks while it is reeled up, and the candles relight one by one.
- **Cascade:** drops are scheduled on the shared beat clock: chandelier *i* drops on beat *i* of a 4-beat cycle, so four or more chandeliers rain one per beat.
- Nominal: ×4.5 every 4 beats (1.92 s) per slot.

## Rogue — Severing Silk / 断ち糸

Spools of black silk; Calamity `RogueDamageClass` and stealth through `CalamityRogueArmament`. Base damage 1800, use 18 ticks, speed 18.

- **Throw:** the spool flies with light gravity for up to 70 ticks, piercing one NPC (×1.0), unspooling a strand from the throw point; where it stops (tile, second NPC or timeout) the strand is pinned (strands shorter than 40 px are not made). Up to 10 strands (oldest replaced), 360 ticks each. Where a new strand crosses an old one a silk knot glints.
- **Stealth strike — Sever:** the throw becomes a pair of bird-shaped embroidery scissors that fly to the cursor (24 px/tick, 30 ticks max) and snip: every strand tightens for 6 ticks and then all of them part at once, ×4.0 to every NPC crossing each strand (24 px wide), plus ×2.0 in an 80 px snip at the scissors. Strands are spent. A chord and a cascade of snapping plucks.
- Nominal: six throws then a sever on a target every strand touches = 6 + 24 + 2 = 32× base.

## The Last Waltz — 最後の円舞曲

Crafted at a Work Bench from one of each of the five weapons (consumed). A 10-slot companion, one per owner: **Noirette** herself floats at the owner's shoulder (her Raid pixel sheet at 1× scale, mesh-driven twin tails), never held up by a thread. Base damage 10000, summon class, mana 10.

- **2-bar score** (230 ticks, beat clock): on beats 1–6 she lifts a small piece of furniture and flings it at the target (×0.6 each); on beats 7–8 she opens her parasol and six silk spokes waltz once around the target in beat steps (`EbonRules.WaltzTurn`), ×0.6 per spoke contact; a spoke can strike the same NPC body again only after 8 ticks and the next beat step, so a body takes at most two contacts per spoke in one waltz.
- Nominal: six flings (3.6×) plus at most twelve spoke contacts (7.2×) = 10.8× base per 230-tick score on one target.
- Normal minion targeting; owner Down suspends attacks, death/dismissal removes her. Owner-only dismissal on a missing buff, as the Doll companion.

## Art and audio

Final pixel art comes from Claude's reward brief for Codex (`asset-deliveries/ebon-rewards/2026-10-02/BRIEF.md`, kept outside the repository with the deliveries) and will be recorded in [Asset brief](ASSET_BRIEF.md) and Attribution. Until delivery, items use placeholder vanilla item textures by reference and in-world bodies reuse this Raid's existing textures (shear halves, props, chandelier, Noirette), so the mechanics can be played first.

| Asset | Use |
|---|---|
| ER01 | Hatbox icon and opening parts |
| ER02 / ER02I | Shear halves (pivot-rotated) and the item icon |
| ER03 | Bow body (string in code), needle arrow, icon |
| ER04 | Thimble icon |
| ER05 / ER06 | Pixel furniture and the grand piano |
| ER07 | Chandeliers (unlit), flames, crystal drop, staff, buff icon |
| ER08 | Spool, icon, bird scissors closed/open |
| ER09 | Companion keepsake and buff icons |
| ER10 | Optional extra companion poses |

Cues: `HatboxOpen`; `Note0`–`Note7` (B3–D6 arpeggio plucks); `ShearSwing`, `ShearSwingRise`, `ShearSnip`, `ShearCut`; `HarpLoose`, `HarpChord`; `ThimbleLift`, `FurnitureYank`, `FurnitureCrash`, `PianoCrash`; `ChandelierSnip`, `ChandelierShatter`, `ChandelierReel`; `SpoolThrow`, `SilkPin`, `ScissorsSnip`, `SeverAll`; `WaltzOpen`.

## Acceptance (owner, not_run until played)

Opening boxes; every weapon's build-up, release, chord and readability on bright/dark ground; the kata's flow; string and web lines not cluttering a Raid; chandelier cascade and companion score timing; a second peer seeing the projectiles; Reduced Effects; FPS with 10 chandeliers or full webs; balance against current Calamity endgame gear.
