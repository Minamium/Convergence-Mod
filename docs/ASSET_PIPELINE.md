---
doc_id: project.asset-pipeline
document_type: governance
status: accepted
owners:
  - art
  - audio
last_reviewed: 2026-09-04
source_of_truth_for:
  - assets.production_pipeline
aliases:
  - asset pipeline
  - audio pipeline
related_code:
  - Assets
related_docs:
  - project.art-direction
  - project.audio-cues
---

# Asset and Audio Pipeline

## First-slice priority

Use simple original placeholders until authority, hitboxes, telegraphs, and multiplayer timing are stable. For the provisional First Severance silhouette, prefer separate runtime exports for central body/Core, broken ring, emissive mask, Pylon, and mechanic/UI primitives. If ring/arms are retained, they are code-positioned presentation on one Boss NPC; do not create Crown/Wings/Heart Casing art for the obsolete multipart plan.

## Appropriate generated/assisted work

- original concept variations, silhouette/mood/palette studies;
- UI/marker mockups and placeholder Tile/NPC/Projectile/VFX textures;
- sprite-sheet rough key poses for later human cleanup;
- shader/primitive-trail/particle code and export validation;
- original motif/harmony/form/tempo/orchestration/cue sheets;
- editable MIDI/MusicXML rough composition data;
- OGG conversion/loop metadata and technical audio QA.

Production pixel art still requires frame consistency, palette/edge cleanup, hitbox/readability playtests, and human review. Release orchestral/choral audio requires properly licensed instruments/samples and competent mix/master.

## Visual workflow

1. Lock current visual/gameplay constraints in [Art Direction](ART_DIRECTION.md) and feature visual spec.
2. Generate/sketch several original silhouette options without artist/作品 imitation prompts.
3. Select by 2D hitbox, readability, and distinction at 100% scale.
4. Define only required states/poses.
5. Clean pixel art manually in an appropriate editor.
6. Export transparent PNG at fixed frame dimensions with nearest-neighbor assumptions.
7. Test in-game over real telegraphs for 2/3/4 players and accessibility settings.
8. Record provenance before committing distributable files.

## Repository separation

```text
external working storage/       not committed
  Concepts/ Editable/ DAW/ Raw/

repository/Assets/              reviewed runtime exports only
  Textures/NPCs/
  Textures/Projectiles/
  Textures/Tiles/
  Textures/UI/
  Textures/VFX/
  Effects/
  Music/
  Sounds/
```

Concept batches, prompts, editable art, DAW projects, raw recordings, model weights, and third-party libraries stay in approved external storage. When a runtime export enters the repository, summarize creator/source/tool/date/human changes/license/attribution/reviewer in `Assets/ATTRIBUTION.md`.

Never commit Calamity `.tmod` files, extracted assets, source mirrors, or repainted/traced third-party work.

## VFX policy

- hitbox and gameplay telegraph use low-density explicit primitives;
- decoration/particles/afterimages/background attacks are client-only;
- broad beams use bounded gameplay actors plus client-drawn trails;
- marker meaning uses shape/icon/motion/text as well as color;
- screen shake/flash/chromatic/dense effects are reducible;
- Dedicated Server never loads graphic/audio assets.

## Music rights policy

Public-domain composition and a modern score/arrangement/performance/recording are different rights. A Beethoven Ninth-derived idea, if used, must start from a verified public-domain score and use a new project-owned arrangement, MIDI/orchestration, performance/render, and recording. Do not extract or imitate a film/CD/stream recording, reuse an unlicensed modern MIDI/arrangement, or use a license-unknown SoundFont/sample library.

This is a production policy, not legal advice; release materials require jurisdiction/source/license review.

## Initial score implementation

Use phase/substate-specific full mixes and short transitions/stingers before considering sample-accurate dynamic stems. Server sends cue ID and cue start tick; gameplay never reads audio playback position. See [Audio Cue Sheet](AUDIO_CUE_SHEET.md).

At 120 BPM, one beat is 30 game ticks and a 4/4 bar is 120 ticks, useful for prototypes. Do not narrow gameplay windows to hide device/frame latency.

## Audio workflow

1. Produce 60–90 second motif/instrumentation sketches only after the loop is stable.
2. Map accents/transitions to server ticks and feature cues.
3. Create project-owned MIDI/MusicXML and import into the DAW.
4. Orchestrate with commercially usable licensed instruments/samples.
5. Export phase mixes/transitions; preserve 48 kHz/24-bit masters externally.
6. Encode reviewed game assets as OGG when appropriate and add loop tags.
7. Test seam, clipping, loudness, focus/pause/resume, join-in-progress, and transitions.
8. Record composer/performer/library/license/provenance.

The first Raid does not require final music to satisfy gameplay acceptance.
