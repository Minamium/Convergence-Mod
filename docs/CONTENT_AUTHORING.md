---
doc_id: project.content-authoring
document_type: governance
status: accepted
owners:
  - gameplay
last_reviewed: 2026-09-12
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

Keep each encounter under `Content/Encounters/<Feature>` with its presentation under `Client/Encounters/<Feature>` and its spec under `docs/encounters/<feature>`. Add subdirectories only when they own real files. `FirstSeverance` is the stable internal identity of 不幸な人形劇; `GhostSamurai` is an independent Boss.

The [feature specifications](README.md#sources-of-truth) own active behavior. [Status](STATUS.md) owns implementation and activation state; an old slice plan does not disable an accepted development path.

## Encounter definition and runtime

Each definition supplies a stable key, kind, participant range, optional arena profile, feature policies and runtime factory. Registration stays local to the module. The common registry/coordinator/router never gains feature switches.

The definition and active spec choose the activation policy and arena requirements. Validate untrusted activation inputs before spawning owned world resources. Core/Tile Entity arena validation applies to encounters that use that activation path; it is not a prerequisite for an independent summon-item Boss. Register every transient resource with its exact-Fight owner and expose bounded snapshots/events.

## Mechanics and presentation

Keep a new mechanic in its feature until a second encounter demonstrates a useful shared contract. A mechanic owns assignments, start/resolve ticks, results, failure policy and cue data; it does not draw UI, play audio, trust client-reported outcomes or bypass cleanup.

Each feature chooses its own phases, life-pool model, recovery and presentation. Do not give Ghost Samurai the Raid's Ready/Down/revive rules or copy the Doll's silhouette and marker policy into another Boss. Read the affected feature spec and relevant [Art Direction](ART_DIRECTION.md) section.

## Tuning

Feature-owned typed code holds timing, HP, damage and geometry. The spec names the tuning owner and the intended experience; avoid a second numeric table that can drift. Verify the player counts and boundaries affected by the change. Runtime JSON/config requires a demonstrated operator need and a schema; stable identity, protocol, authority and cleanup remain explicit contracts.

## Calamity integration

Keep compatibility policy behind the project gateway and the accepted dependency boundaries. Do not copy Calamity internals, binaries or assets. The [Windows build procedure](runbooks/WINDOWS_DEVELOPMENT.md#diagnose-or-build) covers installed Mod references; source inspection alone does not establish runtime compatibility.

## Assets

Follow [Asset Pipeline](ASSET_PIPELINE.md) and [IP Provenance](IP_PROVENANCE.md). Runtime exports enter `Assets`; final documentation illustrations may live under `docs/media` and are excluded from the Mod package. Keep editable/raw originals external and record every distributable export in [Attribution](../Assets/ATTRIBUTION.md).

## Definition of done

Use the [AGENTS completion contract](../AGENTS.md#verification) and [Verification Matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md). Complete the applicable automated work and distinguish remaining manual acceptance from a passing build.

For a new encounter or a changed contract, specify the authority/client split, activation and terminal behavior, supported participant counts, recovery interaction if any, and exact-Fight cleanup. Check the affected lifecycle paths and readability without optional effects. Measure performance/traffic when the change affects those budgets; a wording fix does not require a new benchmark or full multiplayer matrix.
