---
doc_id: encounter.ebon-manor.spec
document_type: spec
status: provisional
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-10-01
source_of_truth_for:
  - encounter.ebon_manor.experience
  - encounter.ebon_manor.presentation
aliases:
  - Waltz of the Ebon Manor
  - Noirette
  - ノワレット
  - 黒絹の館
related_code:
  - Content/Encounters/EbonManor
  - Client/Encounters/EbonManor
related_docs:
  - project.status
  - adr.0029
  - encounter.ebon-manor.assets
---

# Waltz of the Ebon Manor

A new, independent Raid in a moonlit Western manor, first version (v1). Display names: **Noirette — Mistress of the Ebon Manor / ノワレット ― 黒絹の館の主**, a gothic-lolita woman with very long black twin tails who dances the manor's furniture on silk threads; key item **Black Invitation / 黒い招待状**. The owner asked for a thread-wielding mistress, a creepy mansion, EigHt's *AutoMatador* and few doll/marionette motifs: threads move furniture and the manor itself, never a puppet body, and no thread ever holds Noirette up. It does not replace another Raid. Current package and verification belong to [Status](../../STATUS.md).

## Start, ownership and recovery

- Craft the reusable **Black Invitation** at a Work Bench from 10 Silk and 1 Black Ink. Use it on the Foundation Core; alt-use cancels preparation for the summoner.
- Admission, the 160×70-tile field, Ready pill/labels, flight, spawn suppression, spectators and exact-Fight cleanup follow the [Cathedral contract](../azure-cathedral/ENCOUNTER_SPEC.md#start-ownership-and-recovery) with this feature's own runtime. 1–8 players; solo is allowed.
- Native receiving-player damage is authorized for this feature only by [ADR-0029](../../adr/0029-ebon-manor-native-actors.md). Down, the Resuscitation Kit's instant revival, recipient lockout and all-Down defeat reuse the shared instant-unlimited recovery service exactly as Cathedral does.
- **Temporary one-damage rehearsal:** `EbonRules.DebugOneDamagePlaytest` caps every positive source budget and native final damage at 1, like the other in-development Raids. Clocks, geometry, verdicts and effects are real. Set that one switch false to restore the budgets below.

## Acts on the music

Noirette has one native HP pool, `EbonRules.Life` = 5.2M + 2.8M per extra member (frozen at Ready). Native hit ceilings, a runtime clamp and `CheckDead` stop each act at its floor; the floor starts the next act instead of being crossed by one hit or DoT.

| Act | HP | Music cue | Lead-in | Attack epoch |
|---|---|---|---|---|
| Act One | 100%→62% | AutoMatador bars 6–44, loop 20–44 | 8 bars (922 ticks) protected entrance | A drop, bar 14 |
| Act II | 62%→28% | bars 36–50, loop 38–50 | 2 bars (230 ticks) | B section, bar 38 |
| Finale | 28%→0 | bars 51–69, loop 57–69 | 3 bars (346 ticks) | A′, bar 54 |
| Curtain (Victory) | — | bar 72 to the end | — | — |

The runtime publishes the act and its start tick; each act's attack clock is its **epoch**. AutoMatador is a steady 125 BPM: one beat is 28.8 ticks, one bar 115.2. Every warning and every live window starts exactly on a beat (`EbonRules.Beat` rounds each beat from the epoch, so no drift accumulates). Act changes clear every owned hazard and pending stitch; the lead-in bars carry no hazards.

Noirette glides between four stations per 16-bar cycle (centre, left, centre, right), one smooth bar per 4-bar block, as a pure function of the epoch. Her server hitbox and every client's sprite use the same function, so no client extrapolates her position between NPC syncs.

## Attacks

Each act repeats a 16-bar table in `EbonSchedule`; the runtime resolves targets and geometry. Budgets before the rehearsal cap are in `EbonRules`.

- **Silken Snare (thrown furniture):** a chair, candelabra, portrait, clock, birdcage, mirror or music box rises by a wall or the ceiling on a beat, tied by silk to Noirette's hand and to an anchor across the hall. Two beats later it is yanked along its announced lane toward a focused member, accelerating (9 px/tick + 1.15 px/tick²) until it crashes into the far wall. Collision is a swept 46 px capsule between consecutive ticks. Act One opens and closes bars with four throws on consecutive beats.
- **Chandelier:** a chandelier lowers into place over a focused member (or away from a pending gathering point). Three beats later its silk is cut; it falls with gravity (118 px body) and bursts on the floor (270 px dome for 10 ticks).
- **Loom:** 13 parallel diagonal silk strings (28 px wide, 300 px apart) stretch across the whole field, rising or falling; the offset changes each cycle. After three beats of tightening they twang live for 12 ticks.
- **Shears:** giant tailor's shears open beside a horizontal or vertical line through a focused member. Two beats later they snip: the whole 148 px band tears for 14 ticks while the blades race along it.
- **Parasol waltz:** Noirette opens her parasol at the centre station; 6 (Act II) or 8 (Finale) silk spokes forecast for four beats, then turn around her for twelve beats, alternating direction by cycle. The 40 px hub is safe; spokes reach the field edge.
- **Binding Stitch (Stack) / Torn Stitch (Spread):** the same bounded marker/verdict/recipient-strike contract as the Cathedral chorus. Stack gathers inside 190 px around a fixed point near the floor; Spread fails every pair of standing members closer than two 352 px radii. 270-tick warning, 900 budget; the table's call alternates Stack/Spread each cycle so every act shows both.

| Act | Choreography per 16 bars |
|---|---|
| Act One | Throw bursts, loom (rising, then falling), chandelier triplets |
| Act II | Waltz (bar 0), throws, Stack/Spread, chandeliers, shears across then down, both looms, throw burst |
| Finale | Eight-throw barrage, loom, chandeliers, both shears, Spread/Stack, waltz (bar 8), loom, throws, four-chandelier cascade |

## Presentation

- **Luminance:** original `EbonManor` managed material (hall, frame, woven furniture/body, chalk lanes, shears tear, stitch hoops, shards, glow) and `EbonSilk` for primitive silk ribbons; bounded Verlet control threads; `CameraPanSystem` framing and `ScreenShakeSystem` impacts; Luminance noise by runtime reference.
- **Readable danger:** every warning is a tailor's-chalk lane or dome on the exact accepted footprint, brightening and warming toward rose as it tightens; live windows light the whole honest band. Furniture, chandeliers and shears sit where their collision is. Decoration never covers a future safe gap; Reduced Effects lowers motion, debris and exposure but keeps every footprint.
- **Noirette:** her authored 48×64 pixel atlas at the 2× Terraria pixel size. A deforming mesh adds lagging twin tails and petticoat flutter; she floats, glides between stations, casts/commands during warnings and yanks back on release with a braked recoil. Silk leaves her measured fingertips. A dark haze with a thin moonlit ring keeps her readable over the painted hall.
- **Entrance (8 bars):** hall reveal in moonlight, candles lighting outward beat by beat, the Black Invitation hanging on one thread, six threads descending to it, the card unravelling as Noirette is woven in, parasol twirl, then she points on the A drop (flash, ring, radiating silk).
- **Act changes:** threads snap, she unravels at her station and re-weaves at the centre along silk streaks; "ACT II" / "FINALE" titles. The Finale rips the hall apart one widening tear per beat for two bars; the last rip lands on A′ to reveal the collapsed moonlit hall.
- **Endings:** Victory holds her recoil, snaps her last six threads to the walls on consecutive beats, unravels her upward and titles "THE CURTAIN FALLS" over the outro. Defeat closes silk in from every wall while she curtsies and the music fades.
- Physical-pixel field mask, letterbox and Ready pill follow the Doll/Cathedral coordinate contract. The custom sky reconciles its requested state like Cathedral's.
- **Audio:** 20 feature cues from [`tools/generate_ebon_sfx.py`](../../../tools/generate_ebon_sfx.py): tuned silk plucks (B minor, the song's key; throw plucks climb an arpeggio across the bar), furniture crashes, chandelier creak/snap/shatter, loom tension/twang, shears open/snip, parasol opening/release, stitch call/bind/tear, weave, silk burst, act change, hall tear and curtain fall. Cues play once on accepted crossings, with bounded voices; Defeat reuses the project's existing defeat cue.

## Music

**Music: EigHt — AutoMatador.** [Creator video](https://www.youtube.com/watch?v=twMGsSzV_SQ), [creator's BOOTH entry](https://bgm-cathedral.booth.pm/items/6178144), [governing terms](https://eight-novel.fanbox.cc/posts/7647818). The owner supplied the exact recording and selected it. The public terms permit game background use and editing; they forbid standalone redistribution/sale, streaming-service and Content ID registration, and ask for contact about music-game inclusion. This Raid is an action fight with background music.

Four bar-exact section edits (`tools/edit_ebon_music.py`) carry native `LOOPSTART`/`LOOPEND` tags; loop points were chosen by beat-timbre seam scores and joined with a 60 ms equal-power seam. The scene selects the act's cue at its start tick; our outgoing cue ducks within a beat and the incoming cue starts at full weight so its lead-in bars are heard. The player's volume setting is never changed. Music is presentation only, not the hit clock. [Attribution](../../../Assets/ATTRIBUTION.md#waltz-of-the-ebon-manor--2026-10-01) owns exact edits and terms.

## Assets and pending acceptance

Original generated art from Claude's brief, delivered by Codex: [asset brief](ASSET_BRIEF.md). Offline GPU frames (`tools/preview-ebon-manor.ps1`) cover the compiled materials on the real textures; they are not a playtest.

Owner Host & Play (not_run): full Act One → Act II → Finale → Victory with the music in sync (lead-ins, loop seams, act handoffs, curtain outro); forecast readability of each attack over the hall; furniture flight and wall crashes; chandelier fall/burst; loom and shears readability; waltz spin speed; Stack/Spread verdicts; Noirette's size, poses and thread anchoring; the Finale tear; Reduced Effects, shake off, 107% UI/zoom; FPS; matching peers. Balance (HP, cadence, spoke speed) is untuned v1.
