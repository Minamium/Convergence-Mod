---
doc_id: encounter.first-severance.visual
document_type: spec
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-07
source_of_truth_for:
  - first_severance.visual_mvp
aliases:
  - First Severance boss visual
  - Null Cantor visual
related_code:
  - Content/Encounters/FirstSeverance
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.backlog
---

# First Severance — Null Cantor Visual Pass

The user requested giant scale and spectacle comparable in ambition to Avatar of Emptiness / Nameless Deity, combined with this project's polar containment/ritual theme. `0.2.0` implements an original first pass, not a reproduction of those Bosses or a claim of equivalent finished animation/shader quality. The original small-placeholder scope is superseded for this pass by [ADR-0010](../../adr/0010-giant-boss-observation-lances.md).

`0.2.1` increases speed and impact without changing the one-body boundary: rapid eased reveal/opening, 18-pixel decorative recoil, faster orbital motion, brief exposure shock rings, layered fire sounds, speed streaks, stronger short camera kicks (up to 12 pixels on firing / 9 on exposure) and narrow warm screen-edge accents. No gameplay pause, forced zoom or full-screen white flash is introduced. This is a user-requested high-intensity prototype, not a completed WotG-quality presentation claim.


Current implementation is governed by the first section's overrides and the shared encounter/recovery specs. Versioned preceding sections preserve design history, not additional tuning or work orders. Exact timings, widths, gains and durations live in the relevant code/assets; do not restore an older value from a historical paragraph.
## Boss-linked release and mechanic verdicts

The accepted broad plasma material remains. Boss-origin lattice Core salvos replace the square-cut first segment with a graduated filament neck, a contracting chest lens and inward gathering threads. The same cast clock drives the neck, full-width forecast, live body and dim recovery. Geometry, warning time and damage are unchanged.

Spread resolves as an instantaneous thin ruby ray from the Boss to each server-sampled standing participant. Overlapped recipients receive the sharp full-length ray/impact; successful recipients see a weaker ray stop short and disperse into fine mist. A mixed result is drawn per recipient, never inferred from locally observed positions. These are result animations, not dodgeable projectiles or new damage.

A failed Spread adds one brief, tapered white cross at the Boss's mouth, regardless of failed-recipient count. It fades within seven ticks, remains local rather than a full-screen flash, and is strongly attenuated by Reduced Effects. Successful Spread retains its quiet dissolution without this execution flash.

Stack retains its accepted gathering circle. Eight irregularly timed groups of faceted, high-resolution shell splinters snap into place around each standing participant. The fragments sample the project's existing NullCantorShell texture through disposable runtime masks. On failure they contract sharply, fracture and scatter; on success they do not contract and instead tumble down under a weak gravity-like curve. Sub-tick interpolation connects the quick poses. Result recipients/positions are frozen by authority before damage; even a same-tick Defeat may display its short result without reactivating combat. Textures/voices reset on unload and effects never change input, immunity or gameplay.

During anticipation, each existing splinter has independent smoothly interpolated stick/slip vibration, increasing subtly toward resolve. Intermittent thin, branching cold-white arcs bridge neighboring shard edges; no solid ring or filled lightning sheet obscures the authored surface. Successful release immediately loses tension; failed contraction briefly retains it. Reduced Effects reduces vibration, contacts and glow; deterministic visual noise never consumes gameplay randomness.

## Beam readability and clutter reduction

Retain the accepted Phase-I narrow Prism emission, removing its traveling forecast chevrons. Grid/Core forecasts likewise use light rather than arrow stamps. Stack markers keep the one true acceptance circle and inward gathering guidance, but no ordinal/count fraction. No SAFE/Gather labels or thick boundary rails return.

Stillness presents center-out ranks of tightly adjoining narrow lasers, with each rank's own forecast, pearl-hot release and dim cooling wake on the [shared curtain clock](../../../Content/Encounters/FirstSeverance/FirstSeveranceCurtainComb.cs). Only live teeth are damaging; do not illuminate an entire slab early. The lattice uses brighter fine pearl spines and saturated full-width auras, keeping its actual clipped safe pockets clear.

Broad floods, half-field lasers and Core salvos use dense laminar plasma: fine wavering filaments over closely packed soft light layers. Texture masks end at the real footprint; internal flow stays inside it. The full future flood volume is strongly visible before deployment/growth, while live energy brightens toward white. The expanding front no longer spills a round halo into the surviving strip. Reduced Effects preserves danger coverage and reduces decorative segment density. Live quality, contrast, latency and frame cost still need user playtesting; static drawing changes do not prove them.

## Retained flowing material and sanctuary readability — 0.2.19

[Beam material](../../../Client/Encounters/FirstSeverance/FirstSeveranceBeamMaterial.cs) gives narrow forecasts/lasers, lattice/Core salvos, Final combs and broad volumes a common low-density footprint, drifting plasma bands and finer luminous filaments. Internal brightness/width fluctuate inside the authoritative corridor without moving its hitbox. Counts are bounded and Reduced Effects lowers them. Flow segments share one pixel texture to avoid per-segment texture switches; actual FPS remains unmeasured.

Crush warnings form a translucent pressure membrane: bowed stress filaments pull inward as hands brace/close, followed by concentrated compression light. No boxed outline, repeated white arrows or world-space instructional text. Stack retains its true acceptance circle/inward guidance. Safe pockets/strips use negative space and directional cues, never SAFE/安全地帯/Gather labels. Their dimensions derive from collision geometry.

Null Refrain's original high-resolution sprite is independently scaled for inventory/world/held use. Anticipated left/right cuts lead to the extended third stroke, with gold/cyan afterimages, moving arc filaments and a brief impact burst. Local shake obeys shake-off/Reduced Effects; no new overlay or input state. [Attribution](../../../Assets/ATTRIBUTION.md) records provenance. Live appearance, feel, hit alignment and mix remain user-owned acceptance.

## Preceding continuous borderless danger materials — 0.2.17

Remove the paired heavy black/color/white edge rails from every beam forecast and emission, including Stillness curtains, observation/Prism lanes, charge bodies, grid/Core salvos, score combs, half-field/crush surfaces and rotating swords. Preserve the full-volume tint/aura, luminous center, inward-moving cues and internal fibers; do not reduce a broad danger volume to an ambiguous hairline. Filled boundaries and feathered light replace boxed outlines. Stack's accepted single valid circle and Spread circles are not beam rails and remain unchanged.

Twin blades retain the original unedited high-resolution rigid material and brief unsheathing, but now rotate through two accelerating revolutions. An88px damage aura and soft ivory interior continuously surround the blade, with bounded fluid filaments inside it; decorative trailing poses are lower opacity than the leading blade and derive from the shared angle. Recovery/withdrawal follows the revised motion endpoint, never the old four-second timer. There are no bright side rails.

Phase-III horizontal floods begin with a short smooth connected arm flex and aperture charge. A faint fill foretells both final occupied bands while discrete inward arrows and SAFE/安全地帯 identify the remaining192px strip. Thin luminous lines release into a rapidly advancing front, then widen with the same accelerated curve as authority collision, leaving that strip genuinely empty. The safe strip changes height on each pulse. The emitting wrist recoils; curved tendons connect it to the two apertures without hard corner joints. Flowing fibers, luminous pressure and a bright internal body scale with the growing volume, then withdraw smoothly after damage ends. No late width snap or decorative safe zone independent of hit geometry is intended.

Final comb lenses, fibers and emission envelopes use each station's actual shortened warning/live/cool schedule. Repeated-axis offsets move by53px; they are rebuilt for the next warning, not slid while live. Existing bead filaments follow the faster shared trajectory and revised wave birth times. All effects remain client-only with Reduced Effects, existing shake controls, fractional rendering and exact-Fight cleanup. A package/geometry check is not human acceptance of readability, smoothness or visual quality; [Status](../../STATUS.md) records that boundary.

## Preceding rigid blades, legible volumes and terminal density — 0.2.16

Two opposing blades use an original2172×724 RGBA obsidian/titanium/ivory material, copied unedited from built-in ImageGen. The brief and source layers are in [Attribution](../../../Assets/ATTRIBUTION.md). The physical material is1400px long and clipped at the field plane rather than stretched with the rectangular arena's changing radius. A held charging aperture opens into a12-tick draw; integrated fractional rotation and four bounded trailing poses carry the accelerated turn. Full-width initial hazard rails and clockwise guidance remain readable independently of the decorative echoes. No generated GPU texture or persistent actor is needed for the blade.

Broad Phase-I curtains and Phase-III half-field beams now share a full-face warning material: dark translucent backing, violet/cyan pressure tint, flowing internal strands, converging shutters, and high-contrast double edges on all four sides. The same footprint persists through emission/cooling; the unmarked safe side is not flooded with warning bloom. Reduced Effects lowers strand counts, not danger fill or edge visibility. A narrow central line is never the sole indicator of a broad hitbox.

During central crush, both existing high-resolution hand rigs enlarge, brace, face inward and travel rapidly toward the fixed red central volume. Connected forearms follow continuously; inward wakes accompany the0.2s strike, followed by a localized compression flare and decaying shake. Directional chevrons and localized text say to leave the red center. Only the fixed indicated volume is harmful; arm approach is cosmetic. Shake-off/Reduced Effects remain supported.

Final beads keep dark-outlined12px hit cores and gain ivory lancets, paired curved filaments and soft elongated wakes. Dense combs have per-tooth opening lenses, cyan/violet axis distinction, flowing fibers, exact continuous rails and white emission spines. Geometry/count/timing are owned by the [encounter spec](ENCOUNTER_SPEC.md); decorative trails do not widen collision or fill clear lanes. All animation uses the existing fractional clock, with live illumination gated by the authority tick. No UI/input flags, new camera ownership or Stack marker changes. Human readability, uncanny material quality and multiplayer timing acceptance remain user-owned; [Status](../../STATUS.md) records evidence.

## Success and failure designations — 0.2.14

Both accepted results use an intro-like, participant-only letterbox/designation overlay that temporarily replaces ordinary HUD layers. Victory keeps the existing 210-tick inward body/energy collapse and RaidVictory cue; eased camera focus presents the singularity, violet/ivory rules and RAID COMPLETE emerge toward its optical snap, then the camera/HUD return. Defeat lasts180 ticks: the cached surviving silhouette recedes between closing magenta-edged geometric shutters, a restrained blackout builds under RAID FAILED and a severed-connection inscription, with the existing RaidDefeat cue and a short decaying shake. No new texture or sound asset is required.

The result is visible even after the participant is dead. It is not a gameplay pause or input lock: server cleanup and ordinary death have already happened. It uses a disposable local timer and frame-local layer suppression, not `Main.hideUI` or other persistent flags. At its deadline HUD/death/respawn display returns without needing another snapshot. New Fight/world/mod unload clears cached ending data. Cancellation, missing/expired state, outsiders and a tombstone with no preceding participant combat do not create success/failure designations. Shake-off and Reduced Effects remain effective; the latter reduces screen pressure. Existing original rig/art/audio and all attack visuals are otherwise unchanged. Runtime layering against other Mods still requires user observation; [Status](../../STATUS.md) records that boundary. This supersedes the earlier statement that Victory does not hide HUD, not the immediate cleanup rule.

## Material shell, remote limbs and singularity — 0.2.13

The shell replaces thick code-drawn radial cracks with an original high-detail transparent obsidian/titanium/oxidized-bronze image. Fine irregular branching fractures, chipped ceramic laminations and restrained ivory depth light provide the surface. Runtime shell radius shrinks298→246px (about17%); cast bloom and external seal contract with it, then return smoothly during eclosion. Eight contiguous masked image sectors remain invisible as partitions until the existing hinged opening; no additional glowing radial seam is drawn over the authored cracks. In-memory masks are generated/disposed only on clients, preserving the source alpha. The central 144px aperture remains the single hit target.

Clock Stack markers retain one exact valid circle, exterior chevrons, an ordinal and straight timer. Spread uses the larger320px radius from the spec, not an unrelated decorative scale. The score/entry tick controls their full timer rather than assuming the opening's four-second duration. HP-lock closes the central braces and is visible in HUD.

The rotating sword materializes as dark metal, bevelled ivory facets, etched transverse marks and violet edge light, reaching the full forecast corridor before rotating. Constant fractional angular motion carries the blade through exactly one revolution; the initial full-width forecast and clockwise arrows remain independent from bloom. Remote claws use the existing original hand/arm atlas, connected segments/knuckles and three luminous fingers with readable attack lanes. Half-field charge colors the entire affected region; live vertical pressure strands fill that half without hiding the opposite safe half. Terminal beads have visible 12px cores and short trails; alternating slicing combs retain full-width rails and open gaps. Shared authority geometry governs all actual danger, while decorative trails do not hurt.

Phase III pulls the body into a smaller dimmer distant position while two full-size arms manifest laterally, connected by faint spatial filaments. The **central remote aperture stays at the actual NPC position** and remains the single attack target; arms/background body are not independent damage targets. Its five-second transition uses eased retreat/depth scaling, unfolding arms, conditional camera focus/return, letterbox and a remote-presence designation. Final keeps this form and adds a four-second zero-signal designation; there is no new normal numbered form yet. No persistent input, UI, weather or zoom flag is changed.

Accepted Victory lasts210 presentation ticks after immediate gameplay cleanup: distant body and arms contract inward; dozens of filaments accelerate toward one shrinking energy point, then a short optical snap erases it at roughly2.8s. The synchronized new RaidVictory cue rises inward before the pop. No outward debris spray or lingering live actor is needed. Reduced Effects reduces filaments/pressure and disables shake while preserving all hazards and phase information. User visual/audio approval and real multiplayer readability remain separate from compilation; see [Status](../../STATUS.md).

## Eclosion, single-boundary Stack and terminal silence — 0.2.12

Stack has exactly one stationary cyan circle at the authority's valid 112px radius, contrast-backed for visibility. Everything outside that marker is an inward-pointing chevron, never another ring. The timer is a short straight bar inside; the center is a diamond. Resolution fades the same fixed circle/arrows without expanding shock circles. Spread retains its existing discs/countdown and rules.

The user rejected the radial shell breakup. The retained six-second transition now reads as **eclosion**: hands appear through the seams and pry outward (8–36%); the shell peels around outer-rim hinges with perspective compression (20–76%); the head and torso slide up through the opening (25–73%); folded limbs extend into the wide Unbound silhouette (53–97%). Rear body/forearms, opaque shell petals and forward hands are drawn in separate depth passes. Membrane-like filaments stay attached to the moving casing, then thin away; there is no tile-like radial explosion or concentric rupture wave. The existing original atlas is reused. Bounded quintic curves, fractional render time and connected two-bone arm joints avoid stepwise pose swaps; the camera retains its conditional focus/return and the lower, longer strain shake.

Grid 3+ additionally foretells every locked Boss-origin laser as a broad rose/violet corridor with dark-backed full-width edges, a white targeting spine and flowing arrowheads. Release grows a white-hot spine, colored pressure ribbon, coherent longitudinal filaments and one shared Core aperture flare; after the authority's live ticks, exact danger edges disappear and only dim residue remains. Damage widths/ticks come from the [encounter spec](ENCOUNTER_SPEC.md), not the decorative light. Reduced Effects keeps essential width, edges and body while reducing glow and accents.

Accepted Victory retains only a disposable client rig for **210 ticks / 3.5 seconds**: an arrested pose folds inward, the body/core contracts, one narrow horizontal/vertical diffraction cross marks terminal collapse, and upward pale residue fades beneath a localized `NULL CANTOR // SIGNAL SILENCED` inscription. A bounded optional shake accompanies the collapse. Ordinary failure/cancel/lease expiry does not trigger this Victory sequence. Gameplay cleanup, flight/boundary release and attack cancellation happen immediately; the ending does not keep an NPC alive, delay victory, lock controls or hide HUD. New Fight/world unload removes all remaining visual/audio tails. [ADR-0018](../../adr/0018-roster-health-and-core-salvos.md) owns the terminal/authority boundary; [Status](../../STATUS.md) owns actual verification and human acceptance.

## Preceding sealed shell, rupture and lattice — 0.2.11

The following records the preceding visual pass. Its radial-breakup and multi-circle Stack treatment are superseded above; phase timing, camera safety, field mask and ordinary grid geometry remain.

Phase I uses an original code-native, almost spherical fractured metal/stone shell around the existing damage aperture. Eight independently shaded 768x768 sectors have a lit spherical normal, dark underside, engraved arcs, irregular open seams and secondary fault branches. The central hole stays readable as the only damageable NPC. Restrained cyan/amber fissures, seals and metal bands breathe and open with the attack envelope; the existing limb rig is concealed until the rupture. No external art or Boss asset is extracted.

The six-second Phase-II transition uses the authority's one `BossPhaseStartedTick`: eased camera pull-in during the opening 18%, held focus, and an eased return during the final 22%. Letterbox/designation replaces normal HUD layers only while that accepted transition is current and unexpired. At approximately three seconds, fissures flare and the eight plates translate/rotate outward along smooth curves; the retained Null Cantor rig emerges through the gap as fragments fade. Concentric shock fronts and a dedicated crack/impact cue mark the break. No input lock, persistent `hideUI`, zoom write, white-screen strobe or game pause is introduced; cancel/end/lease expiry immediately stops the conditional cinematic. Shake-off removes shake, not the requested focal-camera movement; Reduced Effects lowers bloom/branches but preserves stage timing and all danger cues.

The Unbound rig retains the existing atlas and joint motion, scaled to fit the reduced field. Lattice casts animate its seals/limbs even though the beams span the whole field. Cyan and amethyst patterns have dark-underlined 5px warning spines, growing translucent full-width strips, white accents and traveling chevrons. On authoritative active ticks the whole 24px corridor illuminates with precise white danger edges; the trailing 20-tick residue is dim and harmless. Safe cells stay visibly open. One bounded prior grid descriptor supplies cooling; it is cleared on stage/Fight exit.

Phase-I beam warnings now use an 11px near-black under-stroke, 5.5px fluorescent center and white inner highlight; side rails increase to 4.6px with 10px contrast backing. Rose-red, indigo, mint and warm gold differentiate the prism order. Stronger ribbons and lock glyphs increase contrast without widening hit geometry or covering the Stillness safe column. Fixed Stack uses one bright assembly seal above the Boss, converging directional marks and an on-screen gather label; it never follows a player. Spread retains its existing geometry/presentation.

Outside the shared 160x70 field, an opaque black mask is drawn after the world/foreground and before HUD/cinematic text. Four clipped rectangles use the actual game-view transform and UI scale, including during camera shake/focus. The field outline is no longer the only external-world treatment. Reduced Effects retains the black mask and all warning geometry. No terrain, biome or global weather state is changed. Current build and human visual acceptance are recorded only in [Status](../../STATUS.md).

## Preceding fluorescent attack readability — 0.2.9

The user rejected the dark beam body and weak prediction lines after the friend playtest. Attack light now deliberately contrasts with the retained dark Boss, architecture and background: fluorescent red/blue/green/yellow for Pursuit Prism, cyan for Stillness/charges, with a white-hot spine and layered premultiplied glow. This supersedes 0.2.5's desaturated **attack** palette, not the solemn world/Boss design. All damaging ticks have a bright full-length corridor and white exact edges; cooling afterimages are much dimmer and lose those edges. Beam glow stays inside its authority corridor, especially beside the Stillness safe column.

Warnings replace dashed rails with continuous contrast-backed rails, a thin targeting axis, travelling directional glyphs and inward-closing lock calipers. The stationary collision corridor never moves with those decorations. Emitters gather a larger luminous iris, revolving segmented arcs and smoothly converging fibers before a single pre-release brightness crest. The same process appears at the Boss even for off-body shots. There is no periodic screen flash or stronger forced camera motion.

Stack/Spread retain their fixed outer bounds and different inward-circle/outward-diamond meaning. A separate inner countdown, rotating luminous arcs, travelling chevrons and final closing teeth mark the deadline. A Downed Stack target keeps its still-active circle so the ally can locate the assigned body. The Boss visibly gathers for these mechanics too; remaining Pylons develop closing arches before their failure deadline. All new transforms use fractional presentation time and bounded quintic envelopes; wrapped traveling particles fade out at their wrap, not jump visibly back.

Original small radial/ribbon light masks are generated in client memory, shared by the renderer, and disposed on unload. Existing image/audio assets are unchanged. Reduced Effects removes extra halos/particles but preserves bright beam bodies, exact rails, countdowns and lock glyphs. No new packets, actors, timing changes, collision outcomes, persistent control state or server-side graphics. In-game beauty, readability and frame pacing remain user-reviewed; compilation and envelope tests do not establish visual acceptance.

## Continuous firing foundation — 0.2.8

The primary target is the **whole firing process**, not merely a sharper Boss texture. One bounded emitter per cast owns aperture opening, converging helical fibers, a gathered energy body, continuous acceleration/deformation, textured turbulent flow and a 24-tick-or-shorter cooling tail. Quintic phase envelopes preserve transform/intensity continuity across fire/end. Adjacent fast casts may overlap harmless tails; a phase cancellation immediately removes live rails and fades only decoration. Whole-corridor warning/live rails still use the exact authority geometry, even when a cosmetic pressure front is travelling down the beam. A shader or texture never decides a hit.

Tracking body positions/angles interpolate only during the harmless windup. The locked hold settles to the authority sample before launch; live position extrapolation follows the server's fixed heading/speed. Render-frame fractional time drives fibers, aperture blades and wake deformation. Reduced Effects lowers strand/texture layers but retains exact warning/live boundaries. No full-screen bloom, safe-lane occlusion or frame-stop is introduced.

The original high-resolution rig atlas provides separately drawn torso, upper arm, forearm, hand, armor, spine and joints. Child bones inherit their parent angle and share joint anchors. The cast/open/recoil envelopes overlap instead of instantly replacing each other at FireTick. Art remains a provisional original design, not imported WotG content.

A separate 12x4 placeable foundation has its own saved tile ID; old 2x2 Cores are not reinterpreted. A world-space render pass replaces the offset legacy special draw. Steel columns rise from the plinth/surrounding feet, latches converge and a central iris is raised to the validated field center. The full faint boundary appears immediately when containment starts; decorative erection does not conceal the physical extent. Intro labels progress through geometry lock, airborne-core reveal and sealed designation. [Encounter spec](ENCOUNTER_SPEC.md) owns geometry, flight and timing; [ADR-0016](../../adr/0016-ground-containment-and-continuous-emission.md) owns authority/cleanup. All graphics remain client-only.

The existing cathedral background, original music and SFX remain. WotG-equivalent visual quality, performance with two clients, live beam readability and animation acceptance require an actual user observation; mathematical continuity/build success alone cannot establish them.

## Preceding solemn direction — 0.2.5

This user-directed pass supersedes the earlier pixel/neon/high-density choices below. The original Null Cantor export is replaced with a finely textured basalt/aged-gunmetal reliquary, bone-white ceramic struts, quiet tarnished gold and a faceless dark aperture. Its three separated assemblies occupy a 1020-world-pixel canvas, aperture pivot (50%,36%); the central 144×144 damage target is unchanged. The side groups lift by up to 120px, spread by 90px and tilt 0.17 radians during a deliberate cast, hold visibly open, then snap on firing. High-resolution art uses linear sampling rather than nearest-neighbor pixel enlargement. No extra targetable body parts or player-control changes.

The original opaque **HollowCathedral** backdrop provides deep eroded architecture and a calm center for player readability. Aspect-cover scaling with bounded overscan/parallax prevents exposed edges; sparse ash remains behind world geometry. The spawn-safe direct SkyManager transition from 0.2.4 is retained. No world time/weather/biome changes or screen filter.

The high-resolution **ObsidianLance** has a dark shard, bone-white fibrous membranes and a dim harmless wake. Live beam corridors have a dark interior, fine white filaments and exact visible danger rails rather than broad solid orange fill. Accent colors remain distinguishable but desaturated; gameplay shape/number/cue meaning remains independent of color. During a charge's final 24 harmless ticks, static bright rails and a crossbar show the locked heading, with the separate latch cue. Both origin and direction stop tracking before damaging flight.

Stack/Spread get four-second shrinking marks in pale cold ivory / muted warning crimson with thin aged-gold perimeter details. Successful resolution stays zero damage. Boss idle rings, orbit speeds, fragments, bloom and screen-edge washes are reduced; strong pose contrast and focused attack transients carry the impact. Reduced Effects keeps all boundaries/locked-cue shapes while reducing parallax/ash/glow/shake. [Audio cues](../../AUDIO_CUE_SHEET.md) replaces the chiptune with the original orchestral-textural score and 18 new cues.

The intro is now three seconds and Reset two seconds; the [encounter spec](ENCOUNTER_SPEC.md) owns all durations. Runtime asset exports and generation briefs are listed in [Attribution](../../../Assets/ATTRIBUTION.md). Actual in-game art/readability and listening acceptance remain user-owned; high resolution is not a claim of finished animation or WotG-equivalent production.

## Historical Raid designation intro — 0.2.2

For the existing 90-tick / 1.5-second safe intro, a participant sees a monochrome designation card: RAID // 001, localized First Severance and Null Cantor names, thin rules, dark letterbox bars, and a short containment-loss caption. A low-pitched vanilla cue and decaying camera kick (up to 18 px) accompany the reveal. No gameplay hit-stop, camera zoom or full-screen white flash. Reduced Effects / Shake OFF suppress the shake.

One first `LegacyGameInterfaceLayer` draws the card and returns false to skip the remaining interface for that frame. Existing layer anchors and persistent `Main.hideUI`, inventory, input and zoom settings are untouched. At intro expiry, Fight end or world exit, the conditional layer is absent and normal HUD rendering resumes. Outsiders do not receive the card. Other Mods drawing outside the interface-layer pipeline are not globally suppressed.

## Historical Foundation Core monument — 0.2.1

An original ice-white/graphite/gold containment prism with a cyan suspended core now draws from `Assets/Textures/Tiles/FoundationCoreMonument.png` (1058x1487 RGBA). Its canvas is 176 world pixels tall, with a large matching placement preview and a lit control base. The original 2x2 Tile/TE, click target, save format, collision and exact-Fight ownership remain unchanged. The decorative upper body is not a larger clickable or solid tile; click the base. Existing placed Cores do not require replacement. A client-only `GlobalTile` registers the top-left tile for special drawing and uses the supplied SpriteBatch; Dedicated Server requests no texture. See [API evidence](../../research/INSTANT_REVIVAL_CORE_APIS.md) and [Attribution](../../../Assets/ATTRIBUTION.md).

## Accepted visual boundary

- one logical Boss NPC/body and one authority-owned Boss life pool, without a visual-size cap;
- no separately damageable Crown, Wings, arms, rings, casing, or other multipart actors;
- an unmistakable, non-color-only difference between shielded and exposed gameplay states;
- independent original art and layered client VFX for this requested development pass; final pixel cleanup and release art remain future work.

The exact silhouette, component count, palette, proportions, and animation are not yet user-accepted production design.

## Historical original design — 無響の唱導者 / The Null Cantor

- one faceless black-ice cathedral/keel body surrounding a dark circular aperture;
- frost-white ceramic ribs, charcoal metal, sparse cyan rim light and oxidized-gold restraints;
- four large lateral caliper-like structures; the two side groups separate and turn outward during exposure;
- broken industrial seal with six radial anchors, counter-rotating elliptical observation rings, subdued aurora and drifting fragments;
- no humanoid anatomy, face, eye, crown, wings, robe, or multiple breakable body parts;
- all rings/restraints are presentation attached to one Boss NPC, not separately damageable actors.

Runtime export: `Assets/Textures/NPCs/NullCantorBody.png`, a 1254x1254 RGBA original generated asset. The aperture pivot is approximately `(0.50, 0.44)` of the canvas. Code draws three source-rectangle groups (center, left, right) and adds independently authored geometry; the original file is not repainted from another work. Art brief/prompt remains in external working storage, with a summary in [Attribution](../../../Assets/ATTRIBUTION.md).

At full reveal, the sprite canvas is 820 world pixels tall; orbital geometry spans roughly 1,100 pixels. The Boss aperture is 360 pixels above the Foundation Core. Its clearly bracketed 144x144 NPC hitbox remains stationary while decoration moves. There is **no contact damage** on the enormous decorative body. Flight and ranged endgame weapons can attack the aperture. Pylons are separate 56x88 NPCs with code-drawn containment cages; left/right offsets are 360 pixels and the extra outer positions are 530 pixels.

## Gameplay states

| State | Gameplay meaning | Minimum presentation |
|---|---|---|
| `Dormant` / spawn | introduction, no damage | silhouette/seal reveal, working-title card, closed aperture |
| `Shielded` | Pylon, Stack, Spread, Reset; Boss invulnerable | dark body, diagonal braces across the aperture, inward tethers and gold ring |
| `Exposed` | Core damage window | separated side structures, absent braces/tethers, brighter aperture and four separated hitbox brackets |

`Shielded` and `Exposed` are accepted gameplay meanings. `Dormant`, the exact state names, and every presentation detail in the table are provisional implementation vocabulary. State change is replicated gameplay information. Rotation, particles, light flicker, afterimages, and interpolation are disposable client presentation.

## Readability rules

- Telegraphs and player markers always render above decorative Boss effects.
- Shielded hits must produce a clear harmless response without client-decided damage.
- Exposed uses changed structure/braces, not only color, including reduced-VFX mode.
- Stack and Spread use different shapes and motion, not color alone.
- The full 2/3/4-player/UI-resolution matrix is not claimed complete; first verify one user-run two-player loop.
- No visual component may imply a targetable part unless it has an authority-owned hit rule.

## Palette direction

Retain ice white/pale cyan, charcoal/black metal, warning red and restrained dark gold. Warm white-hot red/orange lances contrast with the cold body. There is no copied film cross-shaped explosion, logo, audio or recognizable face/angel anatomy.

## Historical attack language / retained accessibility rules

- Stack: 7-tile cyan circle, shrinking countdown ring and inward chevrons.
- Spread: 14-tile orange-red circles, shrinking countdown, outward chevrons and central diamonds. No two circles should overlap (28-tile center separation).
- Drawing-based sequences use the [encounter spec's geometry/timing](ENCOUNTER_SPEC.md#drawing-based-attack-sequences-development-022). Pursuit Prism changes red → blue → green → yellow and reverses, with a numbered HUD independent of color. All off-body warnings retain full dashed danger rails, direction chevrons, a charge iris and an explosive white-hot core.
- In 0.2.3, right/left energy bodies wind behind/overhead, track briefly, then streak past on a locked heading. A white-hot pointed core, angular gold fins, rotating seals and dim segmented wake replace the horizontal cut. Dashed moving aim rails warn during approach; only the white-bordered compact body damages during flight. The long wake is harmless. Stillness keeps its empty column, cyan rails and pause glyph; bloom never fills the gap.
- Every warning opens/raises the Boss's side structures and contracts a colored cast ring; firing snaps the structures into stronger recoil. This animation is attached to the Boss even when the attack itself originates elsewhere. The aperture hitbox remains fixed.
- Stack/Spread shrinking rings follow the current 135-tick tuning. Revival markers show an instant kit action or the remaining recipient-only lockout, not channel progress or shared tokens.
- Stack/Spread add rotating broken bars inside the exact boundary, a countdown accent and an authority-result shock ring/shards. A successful resolve gets cyan/warm light; failure gets a harsher red break and stronger optional shake. The important inner player space remains unfilled, and success still deals zero HP.
- The participant-only CustomSky fades the ordinary sky into a black occultation, cold interrupted corona and dark distant pillars. It changes no world time, weather or biome. Reduced Effects lowers opacity/ring density and omits pillars; actual terrain and player visibility remain foreground. Scene exit fades out, World/Mod unload resets immediately.
- Render body/seals first, lance effects next, and player mechanic/revive markers last. Draw all line primitives from an explicit one-texel MagicPixel source to preserve the `0.1.2` spoke fix.
- Reduced Effects lowers rings/aurora/shards/glow/darkening and disables shake. All danger rails, live beams, assignment shapes and damage-window structure remain. Screen Shake can be independently disabled. No full-screen white flash or forced camera zoom.
- [Audio Cue Sheet](../../AUDIO_CUE_SHEET.md) owns the 0.2.3 original synthesized SFX and Ninth chiptune. Core/beam/mechanic/recovery/result cues are participant-local rather than fading with distance from the very large Boss. No third-party recording is included.
- Effects consume snapshots and are disposable: phase exit cancels a lance, Fight end leaves a brief harmless seal dissipation, World/Mod unload clears state and disposes the one procedural glow texture on the render thread. Dedicated Server never requests textures or plays cues.

## Explicitly deferred

Separate damageable Crown/Wings/Heart Casing, humanoid masks, multipart break states, elaborate phase transformations, final music production and hand-cleaned animation remain in [Backlog](BACKLOG.md). The development chiptune and decorative structures are in the explicit user-requested pass and do not promote those deferred mechanics.
