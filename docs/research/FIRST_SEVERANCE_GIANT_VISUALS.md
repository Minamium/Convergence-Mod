---
doc_id: research.first-severance-giant-visuals
document_type: research
status: accepted
owners:
  - engineering
  - art
last_reviewed: 2026-09-06
source_of_truth_for: []
aliases:
  - observation lance rendering
  - giant Null Cantor source evidence
related_code:
  - Client/Encounters/FirstSeverance
  - Content/Encounters/FirstSeverance/FirstSeveranceLance.cs
related_docs:
  - encounter.first-severance.visual
  - encounter.first-severance.spec
  - compatibility.version-matrix
---

# Giant Boss / observation lance implementation evidence

## Confirmed API facts

- Question/scope: draw a very large single-NPC Boss without depending on its small hitbox's draw culling; keep effects/camera client-only; do not allocate thousands of decorative entities.
- Official repository: [tModLoader](https://github.com/tModLoader/tModLoader), pinned source `666f69962d3bdffde54fc14025f02634965b4e7c`, corresponding to the locally confirmed v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8 / C# 12 baseline. Calamity 2.2.4 remains installed but no Calamity drawing API is used.
- Source access: verified 2026-09-06. [Pinned ModSystem.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs), `PostDrawTiles`, `ModifyScreenPosition`, `ModifySunLightColor`, `PostUpdateEverything`, `OnWorldUnload`; [pinned ModNPC.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs), `PreDraw`. Moving [ModSystem API](https://docs.tmodloader.net/docs/stable/class_mod_system.html) is supplemental v2026.07 guidance, not the pin.
- License: [pinned tModLoader LICENSE](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE), MIT. No implementation was copied.
- Observation: `PostDrawTiles` is a client draw hook whose caller expects the Mod to begin/end its SpriteBatch. Camera and sunlight hooks are client-only; update hooks also run on servers. `ModNPC.PreDraw` permits suppressing the ordinary sprite.
- Adopt: independently authored client system with balanced begin/end in `try/finally`, explicit Dedicated Server guards, one lazily allocated render-thread falloff texture, disposal on unload, and the existing one-texel MagicPixel line technique. Content suppresses its placeholder sprite without importing Client. The system draws body/attack/marker layers from the current read-only projection.
- Inference requiring playtest: body readability across zoom/biomes, beam contrast against Calamity summon effects, and frame cost. Compilation/source inspection cannot establish these.

## Reference boundaries

- The user's size/spectacle benchmark is Avatar of Emptiness / Nameless Deity. The public [Wrath of the Gods repository README](https://github.com/TheFifthCircle/WrathOfTheGodsPublic) identifies those Bosses. Survey is limited to the README/repository listing and the previously inspected public build metadata, not rendering or attack code. Moving `main`; no shipping-version compatibility or exact pixel-size claim is made.
- License/reuse uncertainty: an attempted [root LICENSE](https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/main/LICENSE) was unavailable on 2026-09-06 and the inspected root listing did not establish a license. Treat as no copying permission; no source/asset/vendor dependency was introduced.
- Film-like high-energy beam contrast is a user-supplied broad reference, not a surveyed implementation. Reject extracted recordings, recognizable film iconography, copied frames, angel anatomy or cross-shaped film explosions. Adopt independently drawn warning rails, warm white-hot finite strips and a mechanical charge iris.
- Art: built-in ImageGen produced one original polar black-ice/ceramic body from a project brief with no reference images or franchise/artist imitation prompt. Runtime output and limitations are recorded in [Attribution](../../Assets/ATTRIBUTION.md).

## Architecture and focused checks

The current volley is immutable, bounded to two finite unit rays, with authoritative start/fire/end ticks and a finite 88-pixel collision width. Only the exact-Fight runtime targets participants and applies HP/Down transitions; its ledger prevents repeat damage. The client consumes the same rays for rails and firing, with no hit packet or gameplay RNG. Phase exit and ordinary runtime cleanup cancel everything. Protocol v3 carries the bounded extension; old v2 layouts are rejected.

Focused checks cover horizontal/vertical/diagonal/finite-end collision, warning/live/end boundaries, immutable validated data, and phase containment. Existing recovery-domain tests and one actual tModLoader package build complement these. User-run two-client telegraph/hit/Down/revive and visual performance checks remain distinct and unclaimed until observed. No broad test matrix is silently expanded for this pass.
