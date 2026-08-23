# Convergence Architecture Map

Use this routing map after reading the repository's current architecture documents. Repository documents override this summary.

| Concern | Owner | Forbidden coupling |
|---|---|---|
| IDs and tile geometry | `Common/Foundation` | Terraria, Content, Client |
| Encounter contracts | `Common/Encounters/Abstractions` | Runtime implementation, tModLoader, feature types |
| Encounter lifecycle authority | `Common/Encounters/Runtime` | Content, UI, network transport |
| Reusable Raid rules | `Common/Raids` | Terraria hooks, feature names, UI |
| Packet envelope and routing | `Common/Networking` | Feature logic or client-decided results |
| Calamity API access | `Common/Compatibility/Calamity` | Content and presentation |
| Boss/Raid vertical slice | `Content/Encounters/<Feature>` | Client presentation and unrelated features |
| UI/VFX/audio | `Client` | Authoritative mutation |

## Composition pattern

Register an immutable `EncounterDefinition` from the feature module. Supply feature activation policies and an `IEncounterRuntimeFactory`. Construct pure feature state and narrow adapters in the factory, registering owned resources immediately with the cleanup registrar.

Do not store the complete Raid in NPC `ai[]`, a Tile Entity, `ModPlayer`, UI, or a global static. Those are adapters or projections around one authoritative session.

## Extraction rule

Implement a mechanic within the first feature. Extract it to `Common` only when a second feature uses it and the API can be named without either feature's terminology.
