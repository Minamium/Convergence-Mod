---
doc_id: encounter.ebon-manor.assets
document_type: spec
status: accepted
owners:
  - art
last_reviewed: 2026-10-01
source_of_truth_for:
  - encounter.ebon_manor.asset_brief
aliases: []
related_code:
  - Assets/Textures/EbonManor
  - tools/export_ebon_art.py
related_docs:
  - encounter.ebon-manor.spec
---

# Ebon Manor asset brief

Claude wrote the brief; Codex generated every image with its built-in image generation and background extraction (exact model not exposed) and delivered candidates, raw files and a generation manifest to the local delivery folder `asset-deliveries/ebon-manor/2026-10-01/`, which stays outside the repository. Rules given to Codex: original designs only, no artist/work/franchise imitation, style references limited to this repository's own textures, no Git writes. [Feature spec](ENCOUNTER_SPEC.md) owns the design; [Attribution](../../../Assets/ATTRIBUTION.md#waltz-of-the-ebon-manor--2026-10-01) owns rights and exact export identities.

## Direction

- Painted dark-fantasy detail like Cathedral, Vespera and Doll; not the separate contributor's Ghost Samurai line. Characters and the item icon are Terraria-density pixel art; the hall and large props are painted.
- Moonlight (pale blue) as the key light with faint warm candles. Palette: black, charcoal, ivory, dusty rose, silver, tarnished gold; faded dark-green damask walls. Avoid all-red (Scarlet), all-purple (Doll/Ghost Samurai) and all-cyan (Cathedral).
- Owner decisions (2026-10-01): Noirette candidate b as delivered; tilted invitation b; no doll prop; keep doll/marionette motifs out of the Raid — threads move furniture and the manor.

## Selections and export

| ID | Content | Picked | Runtime export |
|---|---|---|---|
| EM01 | Boss 8-pose pixel sheet (4×2) | b | `Noirette.png` 192×128: nearest-neighbour, one sample per logical pixel, binary alpha at 128 |
| EM02 | Eight thrown props (4×2) | a, doll cell excluded | `ManorProps.png` 1232×176: seven 176 px cells, props fitted to 160 px |
| EM03 | Chandelier | a and c | `ChandelierWide.png` 300 px wide, `ChandelierTall.png` 240 px wide |
| EM04 | Shears halves (2×1) | b | `Shears.png` 360×280: upper half above lower half, pivot holes measured at runtime |
| EM05 | Hall backdrop 16:9 | b | `ManorHall.png` 1672×941 RGB |
| EM05F | Collapsed Finale hall (edit of EM05 b) | a | `ManorHallFinal.png` 1672×941 RGB |
| EM06 | Curtain/pillar frame | b | `ManorFrame.png` 1672×941 RGBA |
| EM07 | Black Invitation icon | b (tilted) | `BlackInvitation.png` 44×44 logical-cell majority colour |

`tools/export_ebon_art.py` is mechanical and verifies each input hash: painted alpha below 16 becomes 0 and 240+ becomes 255 (removes generation haze), content crop, Lanczos resize. Nothing is repainted.

## Prompts

Prompts are kept for maintenance; generation does not reproduce identical art.

- **EM01:** "An ORIGINAL elegant young adult woman in gothic lolita fashion, the mistress of a haunted mansion who controls furniture with invisible threads. Very long straight BLACK twin tails reaching below the knees, tied high with large dusty-rose ribbon bows; blunt bangs; pale skin; calm pale lilac-gray eyes; small serious face. Black bell-shaped knee-length dress over layered ivory lace petticoats, short puffed sleeves, long black lace gloves with thin silver rings on the fingertips, high ivory lace collar with a small cameo brooch, laced black corset waist, black tights, black strapped platform shoes, a lace headdress with a small black rose. A small black lace parasol." Terraria NPC density (about 26×44 logical pixels, 2-pixel clusters, one dark outline, 16–20 colours, hard nearest-neighbour enlargement), 4×2 cells: idle, idle sway, float, glide / cast, pull, parasol twirl, command; feet on one baseline, facing right; then background extraction.
- **EM02:** eight isolated haunted-mansion props, painted, each with a small silver ring eyelet at its top centre for a thread: carved armchair, silver candelabra, gilded portrait with a shadowed face, porcelain doll (excluded), grandfather clock, iron birdcage, cracked standing mirror, music box.
- **EM03:** a large ornate gothic crystal chandelier from the side, tarnished gold tiers, unlit dusty candles, crystal drops, a hanging chain ending in one ring at the top centre.
- **EM04:** two separate halves of giant antique tailor's shears, each horizontal with the blade pointing right, pivot hole at the same relative position on both halves.
- **EM05:** the grand hall of a decaying gothic Victorian mansion at midnight, straight on and near-symmetrical: twin curved staircases, tall arched windows with moonlight shafts, peeling dark-green damask, tarnished gold mouldings, two empty chandelier hooks; the central floor and lower middle third dark and uncluttered so attacks read clearly.
- **EM05F:** edit of the chosen hall with the same camera: the ceiling and upper back wall torn open by an unseen force, revealing a huge pale full moon; fragments of ceiling, frames and chairs suspended in the air; the lower middle third stays dark.
- **EM06:** a transparent foreground frame: dark velvet curtains with gold tassels at the far left/right, a carved pillar inside each, ornate moulding along the top; the central 70% fully transparent.
- **EM07:** an original sealed black invitation envelope with an ivory lace edge, a dusty-rose wax seal with a spider-web crest and a loose silver thread bow, chunky pixel-art clusters.
