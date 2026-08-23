# Content Authoring

## Feature-first placement

New content begins in the feature that owns it:

```text
Content/Encounters/<Name>/
  <Name>Definition.cs
  <Name>RegistrationSystem.cs
  Activation/
  Arena/
  Actors/
  Phases/
  Mechanics/
  Projectiles/
  Rewards/
  Cues/
```

Do not create every empty directory in advance. Add one when it owns a real file.

## Encounter definition

Each definition provides a stable key, kind, participant range, optional arena profile, feature-scoped activation policies, and runtime factory. Registration is local to the module. The runtime owns every transient actor/resource it creates and releases them through its cleanup registrar/contract. Global policies contain only cross-cutting authority/compatibility rules; feature policies never scan or branch over unrelated encounter keys. The common registry, coordinator, and router must not gain feature-specific branches.

`EncounterStartCommand.RequestedAnchor` remains untrusted. Global policies may accept it into `Validating`, but the feature must resolve the actual server-side Core Tile Entity and return a structured Arena validation result before any World mutation. The creation context calls this an `AcceptedEncounterStart`, not a validated Arena.

## Mechanics

Implement the first Stack, Spread, or targeted line mechanic inside Third Severance. A reusable common mechanic is extracted only when a second Encounter needs it and both variants can be expressed without feature-specific flags.

A mechanic produces authoritative assignments, timing, resolution, soft-failure results, and presentation cues. It does not draw UI or play audio directly; `Client/Encounters/<Feature>` implements those effects.

## Tuning

Initial tuning values remain typed code owned by the feature and are changed through review. Runtime-editable JSON or server configuration is introduced only for values that server operators must control. Do not make progression, protocol, or invariants data-driven without schema/versioning.

## Calamity integration

Ask the compatibility gateway for progression, difficulty, Rogue, Rage, or Adrenaline information. Do not reference Calamity internals from an NPC, item, phase, or reward implementation.

## Assets

Follow [ASSET_PIPELINE.md](ASSET_PIPELINE.md) and [IP_PROVENANCE.md](IP_PROVENANCE.md). Runtime exports go under `Assets`; source files remain outside distribution paths. Every runtime asset requires an `Assets/ATTRIBUTION.md` entry before commit.

## Definition of done

- Server authority and client replica are identified.
- Start, cancel, failure, disconnect, world unload, and repeated cleanup are handled.
- 2/3/4-player behavior is specified.
- Telegraphs remain readable without optional effects or music.
- Dedicated Server does not initialize presentation code.
- Performance and packet budgets are measured.
- Localization and attribution are complete.
- Relevant docs, ADRs, tests, and changelog are updated.
