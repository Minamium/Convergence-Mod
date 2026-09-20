---
doc_id: encounter.azure-cathedral.assets
document_type: spec
status: accepted
owners:
  - art
last_reviewed: 2026-09-20
source_of_truth_for:
  - encounter.azure_cathedral.asset_brief
aliases: []
related_code:
  - Assets/Textures/AzureCathedral
related_docs:
  - encounter.azure-cathedral.spec
---

# Azure Cathedral asset brief

All four are original built-in image generation, not API/CLI generation; no specific model name is verifiable. No Calamity/WotG artwork was supplied or copied. Generated originals remain in the local external image archive; only game exports are distributed. [Feature spec](ENCOUNTER_SPEC.md) owns the design and [Attribution](../../../Assets/ATTRIBUTION.md#azure-cathedral--2026-09-20) owns rights and export identity. These prompts are optional asset-maintenance references, not normal startup reading.

## Integration

- Liora: nearest-neighbor game export512×256;4×2 cells128×128. Draw at0.56scale, so the visible figure is about55px tall, not a giant illustration.
- Vitrion: original grid correction retained as final parts atlas;2×2 cells, measured padding. Shader reads source UVs and preserves authored albedo. Head/body/tail connected in code; no whole-body pose swap.
- Cathedral: original1672×941 backdrop, rendered behind combat with glacial water material; no world weather/time edits.
- GlacialChime: nearest-neighbor48×60 item export. Unconsumed pedestal key with visibly different shape from companions.
- Original normalized SHA256 values are recorded in Attribution. Repeatability is a preserved-input/export recipe, not a promise image generation reproduces identical art.

## Music export maintenance

Use the owner-selected source recording identified by hash in Attribution; do not download another master. With ffmpeg7.1, open that same input twice (two `-i` arguments), then use:

```text
[0:a]atrim=start=0.323:end=263.470,asetpts=PTS-STARTPTS[x];
[1:a]atrim=start=0.323:end=1.823,asetpts=PTS-STARTPTS[y];
[x][y]acrossfade=d=1.5:c1=tri:c2=tri,volume=-2.9dB[out]
```

Map `[out]`, resample48000Hz, encode `libvorbis -q:a 6 -map_metadata -1`, and set artist/title plus `LOOPSTART=72000`, `LOOPEND=12631056`. Decode the complete export before packaging:263.147seconds and12631056 stereo frames, mean−15.9dB. Do not replace the independent inputs with `asplit`: that short-branch graph prematurely terminated this ffmpeg export. Technical decode/length checks do not establish subjective loop quality.

## Liora.png

Use case: stylized-concept. Asset type: transparent production game sprite sheet for a Terraria-style 2D action game, 1024x512 canvas, exact 4 columns by 2 rows of equal 256x256 cells. Eight separate full-body sprites of ONE ORIGINAL girl, cyan ice-blue hair in a single long SIDE ponytail, graceful navy and pearl-white layered knee-length dress with cyan stained-glass trim, dark boots, holding a slender transparent ice-glass sword. Ordinary Terraria NPC proportions, recognizable small face, tasteful elegant slightly sorrowful expression. Authentic crisp clustered PIXEL ART, designed as ~32x52 logical pixels per figure enlarged with nearest-neighbor pixels. No painterly blending, no smooth high-detail illustration, no antialiasing. Each cell centered at same position, feet y=222 within each cell, figure height about200 pixels. First row: calm idle, blink/quiet breath, left foot step, right foot step. Second row: sword held back for anticipation, sword lifted to cast, sharp horizontal sword release to right, graceful hovering with skirt and side ponytail lifted. Keep recognizable same outfit/identity and proportions across all frames. Modest clothing, no exposed midriff. No text, no labels, no grid lines, no floor, no extra objects, NO BACKGROUND: actual transparent alpha. Fully visible uncut sprites with empty gutters inside each cell. Palette pearly ice white, light cyan hair, deep midnight navy, glacial teal, muted silver. Original design, not any existing character.

## Vitrion.png

Use case: stylized-concept. Production transparent 2D game boss PARTS atlas, square 1024x1024, precisely four equal 512x512 cells in 2 columns and 2 rows, isolated parts never overlap cell edges. Original enormous serpentine leviathan made of ice, water and light-cyan stained glass framed by gunmetal silver. This is NOT a complete worm picture: modular body parts to assemble with code. Top-left: long majestic HEAD in strict SIDE VIEW facing RIGHT, jaws OPEN around a brilliant icy throat, horn-like translucent cathedral spires swept backward, fierce inhuman slender angular snout. Head's entire silhouette inside cell, mouth center approximately x430 y270 in that cell. Top-right: ONE overlapping barrel-shaped BODY SEGMENT side view, horizontal longitudinal axis left-right, domed faceted glass plates, frosted silver rim, three long translucent swept-back dorsal fins, liquid cyan light shining within. Segment length320 height250 centered in cell. Bottom-left: alternate same-sized BODY SEGMENT with a long flowing water-glass fin/spine to add variation. Bottom-right: pointed forked TAIL, root at left center, tapering long icy needles pointing right, graceful thin translucent membranes. Strong readable silhouettes, 2D hand-authored pixel art style with crisp clustered stepped edges, detailed but no painted background. Ice-white highlights, desaturated turquoise, luminous azure interiors, navy shadows, NO purple/red/gold. No text, no diagram arrows, no ground, no grid lines. REAL transparent alpha everywhere outside silhouettes. Original design not copied from any game. Full uncut parts with gutter padding.

## Cathedral.png

Use case: stylized-concept. Asset type: wide 16:9 environment background for a 2D Terraria boss arena, no UI. An immense ICE AND PALE-CYAN STAINED-GLASS CATHEDRAL under a white polar night. View inside the nave, slender frozen Gothic ribs and massive translucent blue lancet windows, intricate silver tracery, icy stone galleries and a distant central rose window. Cold flowing water below, very faint suspended glass fragments, long soft moon shafts through turquoise and frosted white glazing. Noble, melancholy, quietly threatening, majestic rather than cute. Deep navy and teal shadows frame the sides and bottom; upper center stained glass softly luminous but restrained, foreground combat must remain legible. Straight-on game side-view scenic matte, subtle depth, completely empty of characters/monsters/weapons. Center half should be relatively quiet and dark teal; greatest architecture detail at perimeter. Original bespoke fantasy setting, not based on a real cathedral or existing game. Crisp handpainted pixel-friendly texture details rather than photo, cohesive readable broad shapes. No fire, no red, no text, no logos, no watermark. Composition fully fills image, no letterbox.

## GlacialChime.png

Use case: stylized-concept. A single Terraria inventory ITEM ICON, original glacial chime reliquary: small silver Gothic arched frame surrounding a luminous cyan stained-glass teardrop, delicate icy crystalline bell below. Readable compact silhouette at 32 by 40 pixels, crisp chunky pixel-art clusters enlarged nearest-neighbor, tightly grouped palette silver white, ice blue, deep navy. Inert beautiful glass object, not a character, not a sword. Entire icon centered with transparent padding, occupies 70% of square canvas. Real transparent alpha background. No text, no shadow ground, no ornament outside silhouette. Production game sprite, simple rich readable shading and clear outline, no painted illustration.

## Vitrion grid correction

Edit only the atlas layout; retain all four designs/colors/style and genuine alpha. Enforce a2×2 square grid with every part entirely in its own quadrant and empty gutter on every side. Reduce the head so no part crosses either center line. Preserve complete silhouettes and source order. Final export replaces only this task's draft; the initial generated original is retained externally.
