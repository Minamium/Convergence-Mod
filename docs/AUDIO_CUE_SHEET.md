---
doc_id: project.audio-cues
document_type: spec
status: provisional
owners:
  - audio
last_reviewed: 2026-09-07
source_of_truth_for:
  - first_severance.audio_cues
aliases:
  - audio cue sheet
  - First Severance music
related_code:
  - Assets
related_docs:
  - project.asset-pipeline
  - encounter.first-severance.spec
---

# First Severance Audio Cue Sheet

The accepted direction is ominous, solemn original orchestral-textural music, not the discarded Ninth/chiptune arrangement. The latest request reduces excessive SFX while raising BGM slightly. Human listening/mix approval remains user-owned. Implementation/build/load evidence belongs to [Status](STATUS.md).


Current implementation is governed by the first section's overrides and the shared encounter/recovery specs. Versioned preceding sections preserve design history, not additional tuning or work orders. Exact timings, widths, gains and durations live in the relevant code/assets; do not restore an older value from a historical paragraph.
## Safe-window and reward articulation — 0.2.19

Current curtain override: the two center-out Stillness fans share four bounded LanceFire accents across their launch, with a modest rising pitch. They do not create a voice for every tooth or replay missed transients after a delayed snapshot. Existing masters, global mix and BGM are unchanged. Phase-II Stack cues disappear with its retired assignment; the remaining Spread cues retain their deadline identity.

Sanctuary Stack/Spread summons and countdowns reuse existing masters once per accepted deadline; duplicate snapshots do not replay the summon. Fire follows the extended Final clock used by collision. Null Refrain reuses project-authored BladeUnsheathe/BladeSweep and PylonBreak/CoreSalvoFire at bounded positional volume, one impact cue per stroke and at most two instances per cue. No global slider or other-Mod mix changes.

TerminalLiturgy is retimed from the same PCM24 source to cover the longer score plus the existing entry/tail, avoiding a premature return to its slow beginning. [Attribution](../Assets/ATTRIBUTION.md) owns export/recipe details. Other masters and mix settings are unchanged; the preceding duration is historical.

## Preceding rebalanced mix and continuous flood articulation — 0.2.17

Apply0.80× to the existing feature SFX playback gains (−1.94dB), retaining their relative warning/result balance, limited voices and sound-slider control. Re-render all four original music assets from existing PCM24 masters with1.25× gain (+1.94dB) and a gentle stereo-linked peak knee; no global music/sound slider, other Mod track or game setting is changed. This is a measured gain revision, not a perceived-loudness guarantee.

TerminalLiturgy is retimed to its new4-second entry +4,114-tick (68.567s) score +2.5s safety tail, total75.067s. Continuous offline speed/pitch still rises1.00→1.55× over the score. Gameplay derives only from authority ticks; playback is not seek-synchronized for late join. The three ordinary phase compositions, pitches, lengths and loop seams are retained.

- HandGather: original0.78s metallic/pressure rise, once per horizontal flood pulse instead of only at action entry. The0.8s forecast and short late-lock accent remain distinct.
- HandClasp: original1.6s needle transient and accelerating broad air/low-pressure body, aligned to deployment then widening rather than the old finger-lane click.
- BladeSweep: existing original material condensed to2.6s per revolution. Both blades share one voice per turn; the second turn can articulate again without doubling two same-tick blade voices.

Recipes, PCM24 auditions and decoded/oversampled peak metrics are external under audio0217. SFX auditions already include0.80×; runtime files rely on the client multiplier. Every revised runtime file preserves its exact source layers in [Attribution](../Assets/ATTRIBUTION.md). No external recording or new sample library is introduced; live multi-Mod mix, focus/pause and subjective quality await the user.

## Preceding forewarnings and blade/crush articulation — 0.2.16

Music is unchanged. Ten existing warning masters receive an immediate metallic transient and soft-knee crest-factor reduction. StackSummon/SpreadSummon source RMS rises approximately5.17/4.92dB; together with playback0.78→0.98 this is about7.15/6.90dB before the user's sound slider. Lance/energy/grid/hand/half-field warnings also gain presence. MechanicTick is an original0.33s inharmonic bell/low-impact accent; playback rises0.34→0.95. These are sample-RMS/gain measurements, not LUFS or a claim of live perceived balance. Mono48kHz PCM16 masters keep decoded sample peaks below0.911 and preserve the sound slider/focus/pause behavior.

- ExecutionLock0.46s: one conspicuous short accent per score-pulse late warning; never repeated per ray or heartbeat.
- BladeUnsheathe0.78s: metallic draw transient at action age144; BladeSweep is remade as4.1s accelerating flutter/air for the four-second turn. Both blades share one sound event, avoiding doubled peaks.
- HandCrushGather2.5s: brace/pressure rise at central-crush entry. HandCrushImpact1.4s: low mechanical impact and metallic crack at the first live contact, with the existing client shake limit/options.
- Final release and slicing cues use stronger playback; warning locks match the denser six-pulse comb. Existing fire sounds and BGM are not replaced.

All new SFX are independently synthesized, without borrowed recordings or samples. External audio0216/render.py owns the reproducible recipe, PCM24 individual auditions and Warnings_and_Actions reel. Exact distributable files and revised asset families are recorded in [Attribution](../Assets/ATTRIBUTION.md). The exact-Fight client owns bounded per-action sound keys and voice cleanup; Dedicated Server loads none. Human audition and in-game mix remain pending.

## Historical distant urgency and terminal acceleration — 0.2.13

Phase III selects **DistantLiturgy**, an original176-BPM,40-bar,54.545s orchestral-textural loop. Interlocked fast string attacks, asymmetrical3+3+2 accents, low brass/pedal, synthetic choir and short high glass/string responses retain the ominous ritual mood while increasing urgency. The same already-attributed local CC0 VSCO instruments are used; no new external recording, melody or sample library is acquired.

Final selects **TerminalLiturgy**, an83-second edit of that same new master. Offline continuous resampling raises both tempo and pitch from1.00× to1.55× across its four-second entry and76.5-second active score. It does not jump between restarted tracks or change game speed. Existing MusicLoader fade/focus/pause behavior remains; this is not a sample-accurate, late-join-seek music transport. Authority ticks remain the only gameplay clock. Normal Phase-II music now stays selected through its Spread/blade actions and transition instead of dropping to Phase-I music outside Lattice.

New original48kHz mono effects: BladeGather (3s), BladeSweep (6s), RemoteDeparture (5s), HandGather (1.8s), HandClasp (1.2s), HalfFieldCharge (3s), HalfFieldFire (1.8s), TerminalEntry (4s), FinalGather (0.65s), FinalSlicerFire (0.85s), FinalBulletRelease (0.6s). New ray/bullet pulse IDs de-duplicate their firing sounds; action changes clear the local pulse set. RaidVictory is replaced by a3.5s inward strain ending in a sharp synthetic singularity pop at2.8s. Output headroom is about0.88 sample peak for effects; sound/music sliders and bounded voices still apply. Decoder/finite/peak checks are technical QA, not human listening approval.

External PCM24 masters include each music track, all new/replaced effects and a composite audition reel. The external recipe is audio0213/render.py; distributable files and source layers are recorded in [Attribution](../Assets/ATTRIBUTION.md). No raw library, recipe, prompt or master is packaged. [Status](STATUS.md) owns actual evidence.

## Lattice pressure, plasma salvo and hatching — 0.2.12

Music is unchanged. Five original 48kHz mono PCM16 effects replace or extend the prior pass, with PCM24 individual auditions/reel retained outside the repository. No external sound recording, sample library or melody is used for these effects.

| Runtime cue | Trigger and treatment |
|---|---|
| GridFire | Grid-only fire serial: dense low/mid pressure, metallic crack and sustained active body; gain 1.0 |
| CoreSalvoFire | Grid-plus-Core fire serial: the grid impact plus a high-pitched FM/plasma transient, mastered into **one** composite voice; replaces GridFire for this volley, never doubles it |
| PhaseRupture | Accepted transition entry: rising air, beating low resonance and synthetic choir pressure, replacing the old immediate explosion |
| ShellBreak | Transition epoch +112 ticks: sequenced creaks/tears follow the hands and hinged opening; stale catch-up after +152 is silent |
| RaidVictory | Accepted exact-Fight Victory: inward suction, low collapse around 1.91 s, metallic diffraction and a grave synthetic choral tail |

The final masters retain sample-peak headroom (at most 0.92); current GridFire/combined first-0.33-second RMS is approximately 0.538/0.572, versus the preceding GridFire's 0.483. This is a narrow numeric mix comparison, not integrated loudness or proof of perceived/live-game balance. The composite avoids summing two independently limited peaks. Existing serial deduplication, two-voice ReplaceOldest limits, sound slider, pause/focus and cleanup remain. Later beam tails can overlap within that limit; the game's complete multi-Mod mix still needs listening. [Attribution](../Assets/ATTRIBUTION.md) identifies the independent recipe and ownership.

## Preceding Phase-II propulsion and stronger attacks — 0.2.11

The following retains the music and prior cue history. The five revised effects and ShellBreak trigger above supersede those details here.

Phase I and its transition retain `ObsidianLiturgy`; entry to `Lattice` selects **`Assets/Music/UnboundLiturgy.ogg`**, an independently composed **144 BPM / 40-bar / 66.667-second** loop. Fast interlocking short-string figures, low string/brass calls, original synthetic formant choir, heavy drums and metallic punctuation raise momentum while retaining minor/flattened-second tension. This is not a sped-up copy of the Phase-I master, a Beethoven arrangement, or a borrowed game/film recording. It uses the same pinned CC0 VSCO instrumental sources as the earlier composition; provenance is in [Attribution](../Assets/ATTRIBUTION.md). Release/reverb is folded to the beginning; music playback never drives phase timing.

Twelve existing action cues are remixed with a sharper initial crack, lower impact body, descending plasma layer and stronger metallic resonance: `LanceCharge`, `LanceFire`, `EnergyGather`, `EnergyLock`, `EnergyCharge`, `StackSummon`, `SpreadSummon`, `StackRelease`, `SpreadRelease`, `MechanicFailure`, `PylonBreak`, `CoreExposure`. Four new assets cover `PhaseRupture` at transition entry, `ShellBreak` at its authority epoch +180 ticks, and `GridCharge` / `GridFire` once per warning/fire serial. All remain original synthetic effects, with no external recording or sample library in the SFX.

The client voice limit is now explicitly **two per asset, ReplaceOldest**. The API's prior default was also ReplaceOldest, not IgnoreNew: the change retains one older tail under the next impact while keeping a hard bound. Firing gain is stronger but still obeys the game's sound slider. Revision/serial guards, expiry suppression, focus/pause policy, and exact-Fight/unload voice cleanup remain. Dedicated Server never requests sound/music. Independent PCM24 audition masters include the new BGM, sixteen individual effects, and a labeled SFX reel in external working output; they are not packaged. Decode/finite/peak/loop checks do not establish perceived quality or in-game mix acceptance.

## Preceding development mix — 0.2.5

`Assets/Music/ObsidianLiturgy.ogg` is an original 80-second, 32-bar, 96 BPM stereo loop. Low strings, bowed tremolo, restrained brass calls, uneven short-string ostinati, bass drum, struck bronze and original formant-choir synthesis replace the old joy-theme/pulse arrangement. Four eight-bar sections move from pedal tension through propulsion and a brass apex into a restrained return. Harmony uses a D pedal, flattened second and minor-sixth dissonance, without a triumphant major resolution. Real instrumental voices use VSCO 2 CE CC0 samples; raw samples, orchestration MIDI and render recipe stay outside the repository. No existing composition, recording or MIDI is reused. The complete 3,840,000-sample file loops with the release/reverb tail folded into its beginning. Rights/source layers and the pinned sample-library revision are in [Attribution](../Assets/ATTRIBUTION.md).

The `FirstSeveranceFeedback` client owns 18 redesigned mono 48kHz PCM16 cues. These effects are independently synthesized modal-metal resonances, low impacts, filtered air and formant layers with diffuse tails; they contain no instrument samples, speech or borrowed effects. External PCM24 WAV masters include the BGM, each individual cue and a timestamped audition reel for game-independent review.

| Runtime stem under `Assets/Sounds/FirstSeverance` | Accepted state/cue |
|---|---|
| RaidDesignation | current intro |
| LanceCharge / LanceFire | warning serial / live fire serial |
| EnergyGather / EnergyLock / EnergyCharge | approaching body / 24-tick prelaunch lock / launch |
| StackSummon / SpreadSummon | rising bronze gather / falling separated bronze tones |
| MechanicTick | final three half-second countdown accents |
| StackRelease / SpreadRelease / MechanicFailure | committed mechanic revision and result |
| CoreExposure / PylonBreak | exposure transition / observed decrease during Pylon state |
| Downed / Revive | participant transition / increased recipient lockout deadline |
| RaidDefeat / RaidVictory | exact preceding Fight terminal |

Each cue has one voice per asset, obeys the game's sound-volume setting and stops when paused/unfocused. Critical cues are non-spatial so distant/off-body origins cannot silence a warning. Serial/revision comparisons prevent heartbeat duplicates; expired fire and historical results/revives on initial join are not replayed. Active voices stop on replacement Fight/World unload/Mod unload. Cancel has no false victory/defeat cue. BGM uses MusicLoader and the music-volume slider, never a gameplay clock. No audio is requested on Dedicated Server. Human listening, multiplayer timing and Reduced Effects readability remain the next user smoke, not established by peak/loop checks.

## Global rules

- Gameplay clock and mechanic resolution use server ticks only.
- Server replicates cue ID/start tick or a feature-state transition; playback position never controls gameplay.
- Every gameplay audio signal has a visual/shape/text equivalent.
- Client may restart/approximate audio after join/rejoin without changing state.
- Dedicated Server never initializes audio.
- Final runtime assets require provenance and license review.

## Provisional cues

| Cue ID | Use | Loop | Required sync |
|---|---|---|---|
| `ACTIVATION_01` | Core activation/Boss spawn | no | accepted encounter start/intro |
| `PYLON_01` | Pylon DPS check | yes | substate start and deadline |
| `STACK_01` | Stack marker lock/resolve accent | no | assignment and resolve cue |
| `SPREAD_01` | Spread marker lock/resolve accent | no | assignment and resolve cue |
| `CORE_OPEN_01` | Core exposure/burst | short or loop | authoritative gate open/close |
| `OVERLOAD_01` | Pylon failure/escalation | no | committed Overload change |
| `REVIVE_START_01` | local channel feedback | loop/short | accepted lease only |
| `REVIVE_COMPLETE_01` | restrained recovery cue | no | authority completion only |
| `VICTORY_01` | Boss life-zero Victory | no | terminal outcome |
| `DEFEAT_01` | Raid Defeat | no | terminal outcome |

Do not author active cues for Part Break, Personal Effigies, or Last Stand unless those backlog mechanics are separately promoted.

## Motif direction

- facility/Foundation: open fifth, mechanical pulse, measured grid;
- containment/Core: narrow semitone cluster opening into a clearer interval;
- Overload: progressively destabilized bass/harmony without requiring pitch recognition for gameplay;
- revive: concise linked/consonant response, rate-limited to avoid overlap;
- victory/defeat: terminal contrast that follows, never announces before, authority outcome.

## Archived composition direction

The original brief proposed a newly authored arc inspired by the public-domain composition of Beethoven's Ninth: sparse low-register formation for activation, scherzo-like percussion for coordination/DPS pressure, a slower chorale character for Core exposure, participant-specific fragments for Personal Effigies, and a final transformed theme whose harmony reflects mechanic failures or full-party survival. This is a creative seed, not a required score or a request to imitate an existing film arrangement.

If retained, work must begin from a verified public-domain score and a new project-owned arrangement, orchestration/MIDI, performance or render, and master. Never reuse or closely reproduce a film, CD, stream, modern arrangement, MIDI, sample library, choir recording, or SoundFont without separately verified rights. See [Asset Pipeline](ASSET_PIPELINE.md) and `Assets/ATTRIBUTION.md` before production.

## Deliverables when production begins

- project-owned composition source (MIDI/MusicXML/score as applicable);
- tempo map and cue-to-tick notes;
- DAW/tool/library/version/license record;
- full mix master and reviewed game export;
- loop sample positions and transition tails;
- composer/performer credits and `Assets/ATTRIBUTION.md` entry;
- in-game client/join/reload/accessibility notes.
