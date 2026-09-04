---
doc_id: project.content-authoring
document_type: governance
status: accepted
owners:
  - gameplay
last_reviewed: 2026-09-05
source_of_truth_for:
  - engineering.content_authoring
aliases:
  - content authoring
  - feature placement
related_code:
  - Content
  - Client
related_docs:
  - project.repository-layout
  - encounter.first-severance.plan
---

# Content Authoring

## Feature-first placement

```text
Content/Encounters/<Name>/
  <Name>Definition.cs
  <Name>RegistrationSystem.cs
  Activation/ Arena/ Actors/ Phases/ Mechanics/ Rewards/ Cues/
```

Create a directory only when it owns a real file. The first feature is `FirstSeverance`; its source identity and immutable six-state loop plan are implemented, while live adapters remain isolated behind activation denial.

## Encounter definition and runtime

Each definition supplies a stable key, kind, participant range, optional arena profile, feature policies, and runtime factory. Registration remains local to the module. The common registry/coordinator/router never gains feature switches.

`EncounterStartCommand.RequestedAnchor` is untrusted. Authority resolves an actual Core Tile Entity and validates the prospective arena before any world mutation. The feature runtime registers every transient resource and exposes only bounded snapshot/cue state.

## First mechanics

Implement Pylon, Stack, Spread, and Core exposure inside First Severance. Do not create a generic mechanic DSL or move them into `Common` until a second real encounter demonstrates a common contract.

A mechanic owns authoritative assignments, start/resolve ticks, results, soft failures, and cue data. It does not draw UI, play audio, trust client DPS/positions, or bypass exact-Fight cleanup.

The first Boss is one NPC and one life pool. If the provisional ring/arms placeholder is retained, those pieces are draw/presentation components; Pylons are separate authority-owned NPCs. Deferred Part Break/Effigy/Last Stand concepts must not shape the first runtime API.

## Tuning

Feature-owned typed code holds initial timing/HP/damage/radius values. Mark them provisional in the spec and tune from 2/3/4-player evidence. Runtime JSON/config is added only for a demonstrated server-operator need and must have a schema. Never make identity, protocol, authority, or cleanup data-driven casually.

## Calamity integration

Ask the compatibility gateway for progression, difficulty, class category, Rage/Adrenaline/Rogue information. Do not reference Calamity internals from NPC/item/phase/reward code or copy its source/assets.

## Assets

Follow [Asset Pipeline](ASSET_PIPELINE.md) and [IP Provenance](IP_PROVENANCE.md). Runtime exports alone enter `Assets`; editable/raw sources remain external. Every distributable asset requires an attribution record before merge.

## Definition of done

- Authority/client split and bounded packet intent are explicit.
- Start, cancel, failure, disconnect/rejoin, unload, and repeated cleanup are handled.
- 2/3/4-player behavior and Downed interaction are specified.
- Telegraphs remain readable without optional effects/music.
- Dedicated Server avoids presentation initialization.
- Performance/traffic budgets are measured.
- Docs/status/tests/changelog and asset provenance are current.
