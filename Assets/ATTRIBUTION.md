# Asset Attribution Register

## Records

- Runtime file: `Assets/Textures/NPCs/NullCantorBody.png`
- Asset ID: null-cantor-body-first-pass-2026-09-06
- Asset type: texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: original polar containment brief; no external image input or extracted artwork
- Tool/model/version: OpenAI built-in ImageGen; exact backend model not surfaced by the tool contract
- Prompt or brief location: external working storage, `null-cantor-art-brief.md`; summarized in `docs/encounters/first-severance/VISUAL_SPEC.md`
- Human modifications: no manual raster edits; agent inspected the output/alpha and authored source-rectangle side-group animation, pivot alignment, tinting, seals and VFX in C#; user in-game review pending
- License and redistribution terms: project source/asset license remains undecided; development use only, no public release approved
- Required attribution: none externally specified; preserve this provenance record
- Reviewer and review date: Codex task visual inspection, 2026-09-06; final human art review pending
- Notes: 1254x1254 RGBA; transparent exterior, dark aperture; first runtime art pass rather than final hand-cleaned pixel animation. WotG scale and film-like beam intensity are broad user benchmarks only. No third-party shapes, code, audio or image files are copied. One procedural falloff texture is generated and disposed in client memory; there is no additional exported VFX asset.

- Runtime file: `Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem.png`
- Asset ID: foundation-core-item-prototype-2026-09-05
- Asset type: texture
- Creator: project-generated prototype with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-05
- Source type: generated
- Source work and URL: original task concept; no external artwork used
- Tool/model/version: OpenAI built-in ImageGen; exact backend model not surfaced
- Human modifications: local low-resolution pixel cleanup and Terraria item layout export
- License and redistribution terms: project source/asset license remains undecided; development use only, no public release approved
- Required attribution: none externally specified; preserve this provenance record
- Reviewer and review date: Codex task, 2026-09-05
- Notes: Also reused as the experimental Boss/Pylon/kit placeholder.

- Runtime file: `Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreTile.png`
- Asset ID: foundation-core-prototype-2026-09-05
- Asset type: texture
- Creator: project-generated prototype with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-05
- Source type: generated
- Source work and URL: original task concept; no external artwork used
- Tool/model/version: OpenAI built-in ImageGen; exact backend model not surfaced
- Human modifications: local low-resolution pixel cleanup and Terraria item/tile layout export
- License and redistribution terms: project source/asset license remains undecided; development use only, no public release approved
- Required attribution: none externally specified; preserve this provenance record
- Reviewer and review date: Codex task, 2026-09-05
- Notes: Item sprite is also reused as the experimental Boss/Pylon/kit placeholder. Boss 3 music is a Terraria runtime ID reference; no recording/audio asset is copied into this repository.

## Record template

Copy this section for each asset family. In the Records section above, add one exact Markdown entry in the form `- Runtime file: \`path/from/repository/root\`` for every exported file. The repository check parses only that section and verifies both directions.

```text
- Runtime file: `Assets/example/path.png`
- Asset ID: example-stable-id
- Asset type: texture
- Creator: name or organization
- Creation/acquisition date: YYYY-MM-DD
- Source type: original | generated | commissioned | licensed | public-domain
- Source work and URL: none for original work, otherwise exact source
- Tool/model/version: exact toolchain or none
- Human modifications: concise description or none
- License and redistribution terms: exact project-compatible terms
- Required attribution: exact credit text or none
- Reviewer and review date: reviewer, YYYY-MM-DD
- Notes: optional details
```

For music, record the composition, edition or score, arrangement, performance or MIDI, sample library, recording, and final master separately. A public-domain composition does not make a modern edition, arrangement, performance, recording, or sample library public domain.
