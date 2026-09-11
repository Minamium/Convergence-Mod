---
doc_id: project.audio-cues
document_type: spec
status: provisional
owners:
  - audio
last_reviewed: 2026-09-11
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

This page owns **current playback/design choices**. [Attribution](../Assets/ATTRIBUTION.md) owns exact masters, source hashes, recipes and rights; [Status](STATUS.md) owns verification. [Audio evolution](history/2026-09-11-audio-evolution.md) preserves rejected/superseded mixes and API evidence. Historical “keep unchanged” statements apply to their old batch, not today's source.

## Current BGM

All four phase slots now use owner-approved edits of **EigHt — 不幸な人形劇**. Maintain one recognizable musical identity, moving from sparse/lower/slower early phases to the full source and slightly beyond in Final. This replaces both the Ninth/chiptune brief and the later project-authored orchestral A/B/C masters; do not regenerate those older tracks over the active ones.

| Runtime slot | Active treatment |
|---|---|
| `Assets/Music/ObsidianLiturgy.ogg` | Phase I: reduced harmonic/percussive/low/high/stereo body, lower and slower |
| `Assets/Music/UnboundLiturgy.ogg` | Phase II: denser, faster, progressively restored bands and stereo |
| `Assets/Music/DistantLiturgy.ogg` | Phase III: full original-pitch/source identity |
| `Assets/Music/TerminalLiturgy.ogg` | Final: small further tempo/pitch/presence lift with controlled peak headroom |

Exact BPM, pitch, duration and hashes have one owner: [EigHt attribution/export table](../Assets/ATTRIBUTION.md#eight-不幸な人形劇-phase-masters--0246). [PrototypePresentation](../Client/Encounters/FirstSeverance/FirstSeverancePrototypePresentation.cs) maps accepted phase to MusicLoader slots. The legacy filenames are runtime IDs, not titles/ownership of the current composition.

**Preparation is silent.** [PreparationSilence](../Client/Encounters/FirstSeverance/FirstSeverancePreparationSilence.cs) selects silence only while the local member has accepted preparation without combat. Combat start selects phase music; cancel/lost preparation releases the scene to ordinary music. No user music/SFX slider, world state or input flag changes.

Music never drives authority timers. Tracks are not sample-accurate seek-synchronized for joining peers. Numeric decode/peak/RMS checks do not prove loop/transition/mix acceptance.

## Accepted SFX selection

The owner accepted **Stack** but rejected the other 0.2.42/43 remaster timbres. Current selection is therefore deliberate:

- Keep `StackSummon`, `ShellMassLatch`, `ShellMassArc`, `ShellMassShed` and `ShellMassCollapse` from the accepted Stack pass.
- All other First Severance SFX match project commit **5fba4d7** (pre-rebuild 0.2.41), including restored weapon sustain voices. [Restoration evidence](evidence/2026-09-11-selective-sfx-rollback.json) and [prior salvo restoration](evidence/2026-09-10-grounded-posts-audio-tails.json) own exact identities.
- Keep the later attack-bounded playback fixes. Restoring an old master must **not** restore its formerly overlong sustained-beam tail.
- Claw swipe uses the accepted unsheathing/swing accent, without its rejected overlapping `BladeSweep` after-sound. Existing contact/right-click cues remain.

## Cue language and scheduling

| Action | Current articulation |
|---|---|
| Stack anticipation | Irregular `ShellMassLatch` arrivals plus restrained `ShellMassArc` friction/buzz aligned to visible births |
| Stack verdict | `ShellMassShed` for loose success debris; `ShellMassCollapse` for inward failure impact, once from accepted result |
| Spread | Same `SpreadExecution` high metallic launch ping/white cross on success and failure; `SpreadDissolve` accompanies successful endpoint dispersion |
| Prism / curtains / grid / Core salvo | Distinct charge, lock and release; bounded shared accents, not one sound per ray/tooth/player |
| P3 Iron Interdict / central crush | Weight/presence from `IronPressure/Descent` and `CrushPressure/Cataclysm`, following shared wave/collision clocks |
| P2 twin rotation | `BladeOrbitFirst/Second` follow the actual two-turn curve; second replaces first, exit stops owned rotation |
| NPC hits | `ShellHit`: unsettling hard metal; `CoreHit`: glass microfracture; `PylonHit`: metal-plate knock. Native hit cues, no damage-request packet |
| Recovery / terminal | Accepted Down/revive/defeat/victory only, no channel sound, no HP-zero-before-Final victory cue |
| Non-melee weapons | Assembly/lock/launch accents plus continuous Magic/Ranged/Summon sustain; [Weapons](encounters/first-severance/WEAPONS.md) owns macro scores |

[Feedback](../Client/Encounters/FirstSeverance/FirstSeveranceFeedback.cs) is the Raid cue/voice owner; [AudioCueClock](../Client/Encounters/FirstSeverance/FirstSeveranceAudioCueClock.cs) bounds once-only scheduled accents. A result received ahead of estimated time waits for its accepted tick instead of being dropped. Critical late arrivals have bounded catch-up/expiry, not a burst of old sounds. Native hit cues and weapon voices have their own bounded identities so rapid attacks do not multiply or evict unrelated Boss accents.

Charge ends at fire; sustained beams fade at their accepted live end, using per-pulse deadlines for Final and the flood's own fade boundary. Stack/Spread anticipation retires at verdict, preparation assembly at Ready opening. Early action transitions shorten old timed voices to a brief fade; exact-Fight cleanup clears their bounded ledger.

Accepted impact tails may complete across same-Fight terminal cleanup, including a same-tick lethal verdict. They never retain gameplay or play across a new Fight/world unload. Victory/revive stingers and projectile-owned sustain are not treated as long beam tails. These distinctions prevent both missing impacts and attack sound hanging after the action.

## Mix, diagnostics and exports

Prefer body/transient/space separation and contrasting beats over global gain escalation. Master peaks alone do not establish loudness: examine body/RMS and actual device playback at unchanged user sliders. Do not “fix” one quiet cue by changing the user's sliders or all BGM/SFX. BGM may remain deliberately quieter/sparser in Phase I than later stages.

`AudioCue` records accepted scheduling/gain/focus; `AudioVoice` samples whether the device voice is still playing shortly afterward. Neither proves physical audibility. Use these to separate missing scheduling, stale expiry, focus/voice suppression and an actual quiet master before editing. Dedicated Server never initializes audio.

For private listening/regeneration use the exact current runtime exports and the external source manifest in local records. **The earlier `bgm-mp3-0.2.45` audition folder contains superseded project-authored music, not this BGM.** Preserve it as history. Game-use approval of EigHt's work does not authorize exporting a standalone redistributable MP3/BGM pack; follow [Attribution](../Assets/ATTRIBUTION.md#eight-不幸な人形劇-phase-masters--0246) and [asset policy](ASSET_PIPELINE.md#music-rights-policy). Raw source, predecessor masters and recipes remain external, not deleted.
