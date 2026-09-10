---
doc_id: project.audio-cues
document_type: spec
status: provisional
owners:
  - audio
last_reviewed: 2026-09-10
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

Weapon-only0.2.25: the five ritual armaments reuse existing SFX masters with a separate `Convergence:RitualWeapon:` sound Identifier group, bounded positional volume, two-voice limits and unload cleanup. Weapon playback does not evict Boss cue voices. All Boss/music masters and cues remain unchanged; [weapon specification](encounters/first-severance/WEAPONS.md) owns the new timing.

The accepted direction is ominous, solemn original orchestral-textural music, not the discarded Ninth/chiptune arrangement. The current mix and cue overrides are below. Human listening/mix approval remains user-owned. Implementation/build/load evidence belongs to [Status](STATUS.md).


Current implementation is governed by the first section's overrides and the shared encounter/recovery specs. Versioned preceding sections preserve design history, not additional tuning or work orders. Exact timings, widths, gains and durations live in the relevant code/assets; do not restore an older value from a historical paragraph.
## Attack-bounded tails and restored salvo — 0.2.44

The full61-file First Severance SFX inventory was decoded and compared with cue call sites and authored windows. The CoreSalvoFire master was2.45s for a20-tick grid/core firing window; GridFire, EnergyCharge, LanceCharge/Fire, FinalSlicerFire and several mechanic forecasts also outlived their visual window. CoreSalvoFire returns to the pre-rebuild0.2.41 master (also shared by weapon fire); the other current timbres and0.2.43 music remain.

Raid beam/charge voices now carry their accepted descriptor deadline: charge ends at fire, live beam ends with a6-tick fade after the live window, Final uses each pulse's end, and remote floods use their own visual fade boundary. Stack/Spread summoning, shard birth/friction and countdown voices retire at the verdict; preparation assembly fades when Ready opens. Early action transitions shorten old timed voices to6ticks. Deadlines are client presentation only, bounded to64voices, and clear with existing Fight cleanup. Accepted verdict/impact tails, Victory/Defeat/revival stingers and projectile-owned sustained voices retain their distinct existing lifecycle, rather than being treated as sustained beams. Unreferenced historical assets remain preserved. In-game listening remains user-owned; duration/energy measurement is not subjective approval.

## Measured loudness recovery — 0.2.43

The 0.2.42 composition/timbres remain, but the four BGM and eleven one-shot SFX masters receive stereo-linked lookahead gain management. Measured active RMS had fallen roughly 8dB in Phase II/III/Final and up to17dB in individual one-shot cues against0.2.41; matching peaks alone did not maintain body level. The new export raises BGM about2.8–6.0dB and affected SFX2.6–10.3dB relative to0.2.42, while retaining encoded headroom and avoiding hard sample clipping. The old most compressed SFX level is not blindly restored. Three sustain voices were already comparable and remain byte-identical, as do all unlisted sounds. Exact measurements/provenance live in [integration evidence](evidence/2026-09-10-audio-preparation-polish.json).

No playback gains, cue timing/limits, user music/SFX sliders or authority clocks are changed. Active RMS is not LUFS or subjective listening approval. In-game mix/loop review remains user-owned.

## Continuous orchestral A/B/C phase masters — 0.2.42

The four phase masters are rebuilt as continuous orchestral A/B/C forms while retaining the accepted ominous/solemn instrumental language. The existing A introduction remains byte-source-derived until the B downbeat; the handoff is only a short anti-click overlap, not a multi-bar master fade. B begins by inheriting the A downbeat and orchestral weight, relaxes after entry, then accelerates by articulation density: sustained/pedal strings -> tremolo -> eighth-note short bows -> sixteenth-note short bows, with horn, low-brass and timpani joining late. C develops the same D-E-flat-A interval DNA and C-sharp-to-D return tension instead of switching to an electronic or retro palette. No synth lead, electronic drum kit or chiptune layer is added.

Phase I is 96 BPM / 48 bars / 120.000s and keeps the restrained ritual opening before a cantabile/inverted C development. Phase II is 144 BPM / 48 bars / 80.000s and moves into imitative violin/cello/horn writing. Phase III is 176 BPM / 48 bars / 65.455s and carries the established 3+3+2 urgency through acoustic short-string accents. Phase I-III music length is intentionally independent of the encounter action cycle. Final is 178.626 BPM / 52 bars / 69.866667s, covering the current 3,802-tick Final score plus the existing four-second entry and 2.5-second safety tail. Music playback still never drives authority timing or gameplay.

The render reuses the already documented pinned CC0 VSCO 2 CE source family at revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, including violin-section sustained/tremolo/spiccato, cello/viola sustained material, F-horn sustained/staccato and orchestral bass drum. The choir role remains independently synthesized. New note events, articulation changes, dynamics, spatial placement and the mix are project-authored; no third-party composition, recording outside that existing library, or WotG/Calamity asset is imported. Human in-game transition/mix acceptance remains user-owned.

## Prismatic / dimensional SFX rebuild — 0.2.42

Fourteen existing runtime masters are rebuilt in place while retaining every current cue trigger, gain, voice limit, authority relationship and cleanup path. Radiant cues (`EnergyCharge`, `LanceFire`, `CoreSalvoFire`, `FinalSlicerFire`) use a clean light transient, pitched energy body and delayed low pressure instead of clipped broadband impact. Dimensional cues (`PhaseRupture`, `HandClasp`, `CrushCataclysm`) use inward air/pressure motion, opposed pitch movement and a single physical closure. Mass cues (`StackSummon`, `ShellMassLatch`, `ShellMassShed`, `ShellMassCollapse`) use inharmonic shell resonances and separate latch/shed/collapse behavior. The three ritual sustain voices (`MeridianSustain`, `LacunaSustain`, `ChoirSustain`) are dedicated four-second continuous energy bodies. `ShellMassArc` and every unlisted First Severance sound remain unchanged.

The accepted audition recipe is reproduced deterministically at 96 kHz and downsampled to 48 kHz stereo PCM16. The masters do not use hard clipping, bitcrush or a brickwall loudness stage. This is an asset-content replacement only: cue code, names, playback gains, timing and gameplay/network behavior are untouched. WotG/Nameless Deity/Avatar of Emptiness informed only high-level separation of pre-motion, transient, body, resonance and space; no third-party recording, sample, code or exact sound design is imported. Human in-game mix and audibility remain user-owned.

## Critical impact delivery and two-turn orbit — 0.2.40

Preserve the accepted ShellMassLatch/ShellMassArc masters and gains byte-for-byte. Stack verdicts wait for their accepted event tick instead of being dropped if a snapshot arrives ahead of the local estimate. A pending result is consumed once, with a60-tick age bound and an explicit expiry log. Sword/Crush/orbit accents use an action-local once-only cue clock with30-tick catch-up, not the short live-ray interval or an8-tick sword window. Stale events are discarded; clock rollback cannot replay consumed events.

IronPressure/IronDescent and CrushPressure/CrushCataclysm receive mid-band mass/presence and controlled saturation, retaining their original shape/duration. ShellMassShed/Collapse get a smaller presence lift. Peak remains0.780PCM; selected impacts use the full feature gain, not a global slider change. Two sword contacts per wave retain a bounded voice budget. Critical impact tails survive same-Fight terminal cleanup but stop on new Fight/world unload. Accepted Defeat carries the otherwise-lost same-tick sword/crush presentation through the bounded terminal path; it does not delay death or cleanup.

New original BladeOrbitFirst/Second masters follow the actual accelerating two-turn curve: first starts at action tick180, second at363, rotation ends480. Each is a continuous loaded-metal/turbulent sweep with irregular low contacts, an accelerating pressure arc and a substantial second-turn entry. The second voice replaces the first, and action exit/HP interruption stops the owned rotation voice. They do not reuse the weapon's long BladeSweep sample.

`AudioCue` records cue/Fight/action/due tick/lateness, whether an active sound was accepted, selected gain, sound slider and focus. `AudioVoice` samples playing state and device-instance gain two client ticks later. This distinguishes missed scheduling, focus rejection and subsequent stopping/attenuation; it cannot prove physical audibility. Diagnostics are bounded to these critical accents, not every beam/frame. No external Mod settings or sound/music sliders are mutated.

API evidence: tModLoader2026.07.3.0, commit666f69962d3bdffde54fc14025f02634965b4e7c, [SoundStyle](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/SoundStyle.TML.cs), [ActiveSound](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/ActiveSound.cs.patch) and [tracked playback](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/SoundPlayer.TML.cs), verified2026-09-09. Volume is clamped to1; positionless playback avoids distance attenuation but still respects sound slider/focus. Source is tModLoader MIT; inspection only, no code/assets copied. Independent cue ledger, bounded diagnostics and original DSP use these API contracts. Actual multi-Mod listening remains user-owned.

## Audible weapon/Stack mix — 0.2.39

Claw normal swipe retains `BladeUnsheathe` unchanged and drops the long overlapping `BladeSweep` layer. Contact sound and right-click crush are retained. Non-melee ritual assembly/lock/fire accents keep their existing beat hierarchy with2× playback gain capped at0.95; continuous Magic/Ranged/Summon voices rise to0.65/0.52/0.56 before their existing envelopes. Reduced visual effects no longer attenuate these weapon cues. Voice caps, focus/pause, position, user sound slider and projectile/world cleanup remain; BGM and weapon timing/damage are unchanged.

Stack's four `ShellMass` masters retain bass weight but gain240–2200Hz contact/friction presence and controlled saturation. The generator's `--stack-only` export preserves unrelated masters. All peaks remain0.780; measured RMS is0.3898Latch,0.2764Arc,0.3325Shed,0.3026Collapse. Arrival/friction and both result gains also rise; the existing0.80Raid master still applies. This changes clarity and body, not repeated cue count. Subjective loudness/mix remains user-owned.

## Heavy Raid cue overrides — 0.2.37 (preceding)

Eight new independent masters replace only the specified Raid cues; old shared weapon/Victory assets and BGM remain untouched. [Generator](../tools/generate_raid_weight_sfx.py) owns the deterministic recipe, seed and mastering; [attribution](../Assets/ATTRIBUTION.md#heavy-raid-cues--0237) owns exact provenance. Low pressure noise, dense inharmonic metal, restrained upper transients and irregular reflections replace thin isolated tones. This is a timbre rebuild, not a global gain increase. PCM16 mono48kHz exports peak at0.780; numerical checks are not listening approval.

- Iron Interdict: a heavy latch at entry, `IronPressure` in each wave's last39 harmless ticks, then `IronDescent` at insertion. One shared accent at most every8ticks avoids28 competing blade voices.
- Stack fragment arrivals: `ShellMassLatch` plus a quieter `ShellMassArc` friction layer follow the existing irregular visual birth beats. `ShellMassShed` releases pressure with diminishing uneven debris on success; `ShellMassCollapse` compresses and impacts on failure. No damage/recipient changes.
- Final P3 crush: `CrushPressure` loads during the brace; `CrushCataclysm` strikes at the shared actual collision tick. The old generic high lock beep is removed from this attack.
- Read-ahead Final: three short, pitched arrival accents accompany the three forecast groups, followed by existing fire accents. Spread pursuit cues are deduplicated per cast and suppressed when a delayed packet misses their short playback window.

Existing SFX slider/master, focus/pause and exact-Fight voice cleanup remain. In-game mix and subjective weight/readability are user-owned; no clipping or performance claim is inferred from perceived loudness.

## Mechanic verdicts and music presence — 0.2.21 (preceding)

Current sword/impact additions: Iron Interdict uses the existing BladeGather at entry and original SwordImpale metallic insertion bursts, deduplicated by the stagger's fire tick with a two-voice limit. ShellHit is an unsettling hard-metal impact while the Boss remains sealed; CoreHit uses glass microfractures after eclosion; PylonHit is a low metal-plate knock. Native NPC hit playback is capped to one concurrent voice per material with IgnoreNew, so high-DPS hits do not continuously restart or multiply the sample. All retain sound-slider, focus and pause behavior; no hit request, damage change or new damageable shell NPC. Shielded/non-hittable actors do not invent fake hits. Existing music masters and Final action durations are unchanged.

Narrow API evidence: pinned tModLoader2026.07.3.0 local XML documents `NPC.HitSound` as the native hit sound, including custom SoundStyle; [official NPC reference](https://docs.tmodloader.net/docs/stable/class_n_p_c.html) verified2026-09-07. [Pinned ModNPC source](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs) documents hit effects on local/server/remote clients. Independent implementation uses HitSound rather than another custom hit packet; server paths never initialize audio. Actual multi-client audibility remains unverified.

The four phase BGM masters retain their arrangements, tempo, frame counts and existing loop/entry/tail envelopes. A1.7× input lift with a stereo-linked soft peak knee increases decoded RMS by4.41–4.57dB relative to0.2.20; encoded four-times peaks stay below0.99. This is measured signal gain, not a subjective in-game loudness guarantee. The user's music slider, other-Mod audio and existing feature SFX master gain are unchanged.

- SpreadExecution: a brief focused high metallic "ping", with a fast pitch scoop settling into a clear ringing tail, synchronized to every Spread verdict's white cross. Success and failure share the same firing cue once, not one per player. The0.2.22 original oscillator master, duration and gains are retained.
- SpreadDissolve: air/glass endpoint tail for successful recipients, accompanying the brighter split plume; same gain for mixed or fully successful results.
- ShellLatch: dry irregular clacks aligned to the shell-piece appearances, shared across participants.
- ShellArc: short original sputtering electrical buzz/sparks on irregular shell appearances after the first piece; one shared cue per new appearance beat, no delayed backlog, bounded by the existing two-instance limit and voice cleanup. This adds texture rather than boosting global SFX gain.
- ShellCollapse: compression/low impact and fractured high components for a failed Stack.
- ShellShed: detached, diminishing metallic fragments for a successful Stack, without the compression transient.

All five cues are original deterministic DSP,48kHz mono PCM16. Success/failure share the accepted mechanic revision and server-sampled recipients; delayed snapshots never replay a backlog. A same-tick lethal verdict remains available as cosmetic terminal data. [Attribution](../Assets/ATTRIBUTION.md) owns exact file/provenance records; external PCM24 auditions include the0.80 SFX playback multiplier. Human listening and live multi-Mod mix remain pending.

## Preceding safe-window and reward articulation — 0.2.19

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

The0.2.29 [dual-claw weapon specification](encounters/first-severance/WEAPONS.md) owns the new P3-derived hand animation and separately grouped weapon playback. Boss art, timing and audio master files are unchanged.
