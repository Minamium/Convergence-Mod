---
doc_id: encounter.azure-cathedral.assets
document_type: spec
status: accepted
owners:
  - art
last_reviewed: 2026-09-22
source_of_truth_for:
  - encounter.azure_cathedral.asset_brief
aliases: []
related_code:
  - Assets/Textures/AzureCathedral
related_docs:
  - encounter.azure-cathedral.spec
---

# Azure Cathedral asset brief

Current images are original built-in image generation, not API/CLI generation; no specific model name is verifiable. No Calamity/WotG artwork was supplied or copied. Generated originals remain in the local external image archive; only game exports are distributed. [Feature spec](ENCOUNTER_SPEC.md) owns the design and [Attribution](../../../Assets/ATTRIBUTION.md#azure-cathedral--2026-09-20) owns rights and export identity. These prompts are optional asset-maintenance references, not normal startup reading.

## Integration

- Liora: nearest-neighbor1536×1024→192×128 game export;4×2 cells48×64. Draw at1×: visible body about40–45logical pixels, raised sword up to60. The8poses retain one small NPC, not a shrunken high-detail illustration. Built-in alpha-removal output is the preserved source; do not infer transparency from RGB preview alone.
- Vitrion: nearest-neighbor1254×1254→1024×1024,2×2 cells. Dorsal view facing right, head/alternate segments/tail. `AzureMaterials` records each row's axial spine and `AzureGlass` mirrors its top half across the longitudinal axis, ensuring bilateral silhouette symmetry even if a generated detail differs. Code connects/follows the head with no whole-body pose swap.
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

### Current contrast revision / 0.3.32

Built-in image tool; exact model unavailable. Reference1: previous Liora edit target. Reference2: project-owned `DollTheater/DollAttendant.png`, **pixel contrast/cluster reference only**. Preserve4×2 layout, eight pose roles, side ponytail, ice sword and small NPC identity. No third-party art input. Final prompt:

> Terraria NPC production transparent atlas. Improve Liora's flat white/cyan shading using Nameless Doll's crisp outline, expressive small face and dark-fabric/light-hair separation, not her costume or identity. Light ice-blue single side ponytail; pale shaded skin; muted midnight-blue/cobalt fitted bodice, layered pale-blue skirt with dark inner pleats and silver trim. Graceful, quiet, slightly sorrowful.48×64logical cells, total192×128, about42logical pixels head-to-foot. Hard pixel clusters, restrained palette, no blur/oversized anime eyes. Exact top row: sleeping/awake/blink/awakening sway. Bottom row: sword draw/raised sword/forward casting/floating follow-through. Aligned roots and scale, sword contained in its cell, transparent padding, no text, grid or scenery.

Export the generated1536×1024PNG to192×128 with ffmpeg `scale=192:128:flags=neighbor`, preserving alpha. [Attribution](../../../Assets/ATTRIBUTION.md#azure-cathedral--2026-09-20) records source/export hashes. Original previous-source hashes/prompts below remain history, not the current export recipe.

### Initial native-density sheet (superseded)

Use case: stylized-concept. Make an ORIGINAL Terraria-style NPC pixel sprite sheet for Liora: small graceful girl with LIGHT CYAN HAIR in a single long SIDE PONYTAIL, elegant modest pearl-white and deep navy dress with a cyan hem, dark small shoes, and a slender ice-glass sword. Asset: genuine transparent background sheet, EXACT 4 columns by 2 rows, 8 square cells with generous transparent gutters, no grid lines or text. Each entire figure is made at the density of a native Terraria NPC: about 24 logical pixels wide and 46 logical pixels tall, then enlarged with perfectly hard nearest-neighbor square pixels. Big clean two-pixel clusters, 12-16-color palette only, one dark outline, NO tiny lace, NO intricate jewelry, NO gradients, NO painterly detail, NO antialiasing, NO giant anime face. This must read as a small in-game Terraria NPC rather than a shrunken detailed illustration. Same character, size, central root and foot baseline in all eight cells. First row left to right: 1 eyes closed floating peacefully arms near chest as if sleeping inside ice, 2 awake calm idle facing slightly right, 3 blink calm idle same stance, 4 gentle hover skirt and ponytail lifted. Second row: 5 sword drawn diagonally down to right, 6 sword raised exactly vertically to the sky, 7 dramatic diagonal sword release, 8 quiet post-cast hover. Weapon must remain entirely inside its cell, all characters and sword blades fully visible, no cropped hair or limbs. Consistent silhouette, innocent graceful but composed, normal NPC proportions and modest dress, no wings or halo. Entire sheet actual transparent alpha outside characters. Do not include ice prison or any background prop; those will be drawn separately in code.

Background extraction edit: Remove the entire dark blue background and all soft blue glow around every character to genuine transparent alpha. Keep all eight original pixel characters exactly as drawn, in precisely the same poses and4-column2-row layout, with sharp pixel edges. No dark matte, no shadow, no halos, no background color. Do not change the girl's design, scale, grid placement, clothing or sword. Only isolated full-body pixel sprites on a truly transparent background. Production sprite-sheet background removal.

## Vitrion.png

Use case: stylized-concept. Asset type: transparent production sprite PARTS atlas for an ORIGINAL ice-glass giant worm named Vitrion, for a2D action game. Square image, precise2by2 equal quadrant grid, isolated parts with real transparent alpha. This is a modular top-down DORSAL VIEW leviathan, absolutely NOT a side-view fish or koi, no fabric fins. Every part has strict bilateral symmetry about its horizontal longitudinal axis, paired identical structures above and below, all parts facing RIGHT. Top-left quadrant: massive wedge-like crystalline DRAGON WORM HEAD, viewed straight from above, symmetrical twin sets of swept-back horns and armored mandibles, narrow bright icy water-laser mouth at the right tip, axial cyan core. Top-right: one stout overlapping annular BODY SEGMENT viewed from above, centered spinal ridge along X, two symmetrical sweeping solid glass armor blades on both sides; the front/back ends connect left-right to identical segments. Bottom-left: an alternate same-scale annular BODY SEGMENT with three paired shorter glass blades, same silhouette size and connection axis. Bottom-right: long slender symmetric TAIL with root left, needle point right, paired decreasing spikes; no fish tail. Each silhouette completely contained in its quadrant with ample12% transparent gutter. Render as deliberate clean sharp PIXEL ART, clustered faceted ice and pale cyan stained glass, dark navy silver frames, strong bright sharp cyan edge highlights, quieter deep teal glass interiors; not a photoreal/painted illustration, no airbrush noise. Strong simple shape hierarchy, noble formidable ancient glacial cathedral guardian. Do not reproduce any existing game boss. No background, no text, no labels, no grid lines, no cast ground shadows, no extra motifs. Source orientation and exact2x2 layout are critical for a continuously articulated game worm.

## Intact armor and articulated beetle mouth / 0.3.37

Built-in edits of the project-owned first/Fury atlases; original detached mouth assembly. Keep two2×2 atlases with existing head/body/alternate/tail roles, but the head is now one rigid carapace. New `VitrionMandible.png`, `VitrionMouth.png` and `VitrionMouthClosed.png` provide independently articulated pincers, toothed throat and closed lamella. One pincer is mirrored to guarantee paired bilateral roots; do not split the entire head texture. `AzureMaterials.Mouth` owns measured hinge/orientation and `AzureGlass.PartPass` supplies the same caustics, pressure and dissolution as armor. Frost exits the opening; no detached UI ring is added.

Production briefs:

> First-form atlas: edit the project-owned2×2 Vitrion sheet, preserving dorsal right-facing roles and transparent padding. A single intact dark cobalt beetle-like crown with a small recessed nose aperture, no separable skull halves or baked moving jaws. Streamlined cyan stained-glass armor with navy interiors and silver rim clusters; bold paired swept-back blades, no fish fins, gore, guns, scenery or letters. Retain readable connected body/tail roots and genuine transparent alpha.

> Fury atlas: preserve that exact layout and attachment axes, with leaner smoked-indigo pressure glass, stronger white-cyan stress veins and sharper paired crystalline armor. Keep one intact head shell and inset mouth, not a bisected head. Original project design only; no copied game sprite.

> Detached assembly: original icy stained-glass stag-beetle pincers with fixed left-hand hinge, hooked right tips and internal teeth; a toothed recessed throat and closed lamella. Navy glass, bright cyan facets and silver edge clusters, transparent outside every part, no ground/background. Generate2×2 equal cells: upper pincer, lower pincer, open throat, closed throat. Preserve the first upper pincer as the symmetric pair's common source. Remove the surrounding background with the built-in tool, preserving interior throat detail and part positions.

Exports are mechanical nearest-neighbor only: first atlas1254²→1024²; Fury atlas→1024²; final1536×1024 assembly cropped to768×512 top-left/bottom-left/bottom-right, then384×256 each. Original sources and earlier art remain external/Git history. Alpha was checked numerically and against bright/dark actual-shader previews; an RGB-only preview may display hidden background colors. No manual painted/reconstructed alpha or extracted third-party art.

## Superseded VitrionFury.png brief / 0.3.33

Dedicated second-form atlas, not an overwrite of `Vitrion.png`. Built-in image edit of the project-owned original atlas only. Generated1280×1280 →1024×1024 with ffmpeg `scale=1024:1024:flags=neighbor`, preserving real alpha. Four unchanged cell roles; material mirrors each axial spine, morphs from the old atlas after the bite, and code opens the front two mandibles around a retained rear hinge. Authored texture changes armor; motion, pressure waves and transition remain continuous code/shader work. Source/export hashes are in Attribution.

Final brief: original enraged ice/stained-glass leviathan, transparent2×2 parts atlas with head top-left facing right, body top-right, alternate body bottom-left and pointed tail bottom-right. Strict dorsal bilateral symmetry, matched attachment axes and contained cells. Split pale frost-glass plates into sharp swept-back cathedral spires over dark indigo smoked-glass structure; white-blue branching stress seams and a luminous frozen heart. Lean predatory pressure, recognizable compact segments, quieter dark interiors rather than solid white. Upper/lower jaw blades visibly separable for code articulation. Preserve original layout but redesign armor, not merely recolor. Crisp clustered sprite texture, no fuzzy outer glow, flesh, guns, text, scene or copied game design. Original and first-form atlas remain retained.

## Cathedral.png

Use case: stylized-concept. Asset type: wide 16:9 environment background for a 2D Terraria boss arena, no UI. An immense ICE AND PALE-CYAN STAINED-GLASS CATHEDRAL under a white polar night. View inside the nave, slender frozen Gothic ribs and massive translucent blue lancet windows, intricate silver tracery, icy stone galleries and a distant central rose window. Cold flowing water below, very faint suspended glass fragments, long soft moon shafts through turquoise and frosted white glazing. Noble, melancholy, quietly threatening, majestic rather than cute. Deep navy and teal shadows frame the sides and bottom; upper center stained glass softly luminous but restrained, foreground combat must remain legible. Straight-on game side-view scenic matte, subtle depth, completely empty of characters/monsters/weapons. Center half should be relatively quiet and dark teal; greatest architecture detail at perimeter. Original bespoke fantasy setting, not based on a real cathedral or existing game. Crisp handpainted pixel-friendly texture details rather than photo, cohesive readable broad shapes. No fire, no red, no text, no logos, no watermark. Composition fully fills image, no letterbox.

## GlacialChime.png

Use case: stylized-concept. A single Terraria inventory ITEM ICON, original glacial chime reliquary: small silver Gothic arched frame surrounding a luminous cyan stained-glass teardrop, delicate icy crystalline bell below. Readable compact silhouette at 32 by 40 pixels, crisp chunky pixel-art clusters enlarged nearest-neighbor, tightly grouped palette silver white, ice blue, deep navy. Inert beautiful glass object, not a character, not a sword. Entire icon centered with transparent padding, occupies 70% of square canvas. Real transparent alpha background. No text, no shadow ground, no ornament outside silhouette. Production game sprite, simple rich readable shading and clear outline, no painted illustration.

## Superseded source

The0.3.30 side-view atlas and denser girl export are preserved in Git history (`b5d80ac`) and their external originals remain intact. They are not current authoring instructions. No Calamity/WoTM image or texture was fed into generation or shipped.
