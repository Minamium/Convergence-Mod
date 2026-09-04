---
doc_id: project.audio-cues
document_type: spec
status: provisional
owners:
  - audio
last_reviewed: 2026-09-04
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

Audio is deferred from the first gameplay acceptance gate. This sheet reserves a minimal cue vocabulary without locking composition, duration, or final names.

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
