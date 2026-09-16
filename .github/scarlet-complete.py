#!/usr/bin/env python3
"""One-shot integration on the requested Scarlet branch; removed before commit."""
from pathlib import Path
import hashlib
import json

root = Path('.')
def edit(path, old, new):
    p = root / path
    text = p.read_text(encoding='utf-8')
    if text.count(old) != 1:
        raise RuntimeError(f'{path}: expected one exact integration anchor, found {text.count(old)}')
    p.write_text(text.replace(old, new), encoding='utf-8')
def section(path, heading, following, body):
    p = root / path
    text = p.read_text(encoding='utf-8')
    a = text.index(heading)
    b = text.index(following, a + len(heading))
    p.write_text(text[:a] + body.rstrip() + '\n\n' + text[b:], encoding='utf-8')

runtime = 'Content/Encounters/CrimsonFoundry/CrimsonRuntime.cs'
edit(runtime, 'internal sealed class CrimsonRuntime : IEncounterRuntime', 'internal sealed partial class CrimsonRuntime : IEncounterRuntime')
edit(runtime, '            if (age >= nextPhrase && stage is CrimsonStage.Countdown or CrimsonStage.Performance)\n                SchedulePhrase();', '            TickChorus();\n            if (age >= nextPhrase && stage is CrimsonStage.Countdown or CrimsonStage.Performance)\n                SchedulePhrase();')
edit(runtime, '    private void SchedulePhrase()\n    {\n        if (actor is null) return;', '    private void SchedulePhrase()\n    {\n        if (actor is null) return;\n        if (TryScheduleChorus()) return;\n        phrasesSinceChorus++;')
edit(runtime, '    private void ClearHazards(int source = -1)\n    {', '    private void ClearHazards(int source = -1)\n    {\n        ClearChorus(source);')
# Both authoritative membership and attack epochs are checked in a linked pure predicate.
rules = 'Content/Encounters/CrimsonFoundry/CrimsonChorusRules.cs'
anchor = '    internal bool MatchesRoster(int count) => count is >= 1 and <= 8 && (Members >> count) == 0;'
edit(rules, anchor, anchor + '''
    internal bool AppliesTo(in CrimsonState state) => Fight != Guid.Empty && Fight == state.Fight
        && Epoch == state.PhaseStart && state.Stage == CrimsonStage.Performance
        && MatchesRoster(state.Members.Length)
        && CrimsonPhaseRules.ActiveSource(state.Phase, state.DefeatedMask, state.PerformerDefeated, Source);
''')
edit('Content/Encounters/CrimsonFoundry/CrimsonChorus.cs', '''        return plan.Fight != Guid.Empty && boss is not null && boss.Fresh
            && boss.State.Fight == plan.Fight && boss.State.PhaseStart == plan.Epoch
            && boss.State.Stage == CrimsonStage.Performance && plan.MatchesRoster(boss.State.Members.Length)
            && CrimsonPhaseRules.ActiveSource(boss.State.Phase, boss.State.DefeatedMask, boss.State.PerformerDefeated, plan.Source);''', '''        return boss is not null && boss.Fresh && plan.AppliesTo(boss.State);''')
rig = 'Client/Encounters/CrimsonFoundry/CrimsonRig.cs'
edit(rig, '        return (CrimsonRigMotion.Charge(until), CrimsonRigMotion.Recoil(since));', '''        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is CrimsonChorus c && CrimsonChorus.TryBoss(c.Plan, out var parent) && parent == boss
                && age >= c.Plan.Born && age < c.Plan.Fire + 20 && (source < 0 || c.Plan.Source == source))
            {
                float delta = c.Plan.Fire - age;
                if (delta >= 0) until = Math.Min(until, delta); else since = Math.Min(since, -delta);
            }
        return (CrimsonRigMotion.Charge(until), CrimsonRigMotion.Recoil(since));''')
# Preserve prior identities and explicitly revise only the changed native schema.
edit('build.txt', 'version = 0.3.14', 'version = 0.3.15')
p = root / 'Common/Networking/Protocol/EncounterProtocol.cs'
t = p.read_text(encoding='utf-8')
import re
t, count = re.subn(r'(CurrentVersion\s*=\s*)47\b', r'\g<1>48', t)
if count != 1: raise RuntimeError('Expected protocol47 integration base')
p.write_text(t, encoding='utf-8')
p = root / 'Tests/Convergence.DomainTests/Directory.Build.targets'
t = p.read_text(encoding='utf-8')
t = t.replace('</Project>', '''  <ItemGroup>
    <Compile Include="../../Content/Encounters/CrimsonFoundry/CrimsonChorusRules.cs" Link="Domain/CrimsonChorusRules.cs" />
  </ItemGroup>
</Project>''')
p.write_text(t, encoding='utf-8')
# This source guard checks wiring only; behavioral tests live in the C# harness.
p = root / 'tools/tests/test_scarlet_chorus.py'
p.write_text('''import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
class ScarletChorusWiring(unittest.TestCase):
    def test_authority_and_cleanup_are_wired(self):
        runtime = (ROOT / "Content/Encounters/CrimsonFoundry/CrimsonRuntime.cs").read_text()
        self.assertIn("TickChorus();", runtime)
        self.assertIn("if (TryScheduleChorus()) return;", runtime)
        self.assertIn("ClearChorus(source);", runtime)
    def test_no_alternative_hp_or_immunity_pipeline(self):
        code = (ROOT / "Content/Encounters/CrimsonFoundry/CrimsonChorus.cs").read_text()
        self.assertNotIn(".Hurt(", code)
        self.assertNotIn("statLife", code)
        self.assertNotIn("immuneTime =", code)
        self.assertIn("Main.myPlayer == member.Slot", code)
        self.assertIn("marker.Plan != plan", code)
        self.assertIn("if (free < needed)", code)
    def test_marker_has_no_text_and_keeps_luminance_effects_local(self):
        visual = (ROOT / "Client/Encounters/CrimsonFoundry/CrimsonChorusVisuals.cs").read_text()
        self.assertNotIn("DrawBorderString", visual)
        self.assertIn("CrimsonEnergy.AddCore", visual)
        self.assertIn("CrimsonVisuals.Reduced", visual)
        self.assertIn("Main.dedServ", visual)
''', encoding='utf-8')
# An actual pure-state test, not a parallel reimplementation of the predicate.
p = root / 'Tests/Convergence.DomainTests/ScarletChorusTests.cs'
t = p.read_text(encoding='utf-8')
pos = t.rfind('\n}')
t = t[:pos] + '''
    [DomainTest("Scarlet chorus cannot survive foreign Fight phase rollback dormant source or terminal state")]
    private static void ScarletChorusOwnership()
    {
        var p = ChorusExample();
        var members = new CrimsonMember[4];
        for (int i = 0; i < members.Length; i++) members[i] = new((byte)i, Guid.NewGuid(), true, false);
        var state = new CrimsonState(p.Fight, 700, 100, -1, CrimsonStage.Performance, members,
            8000, 6000, Phase: 1, PhaseStart: 500, UnlockAt: 650);
        AssertEqual(true, p.AppliesTo(state), "own active source");
        AssertEqual(false, p.AppliesTo(state with { Fight = Guid.NewGuid() }), "another Fight");
        AssertEqual(false, p.AppliesTo(state with { PhaseStart = 600 }), "obsolete phase");
        AssertEqual(false, p.AppliesTo(state with { Phase = 2 }), "withdrawn source");
        AssertEqual(false, p.AppliesTo(state with { Stage = CrimsonStage.Defeat }), "terminal state");
        AssertEqual(false, p.AppliesTo(state with { Members = new CrimsonMember[3] }), "invalid roster mask");
        AssertEqual(false, p.AppliesTo(state with { Phase = 3, DefeatedMask = 2 }), "dead Final source");
    }
''' + t[pos:]
p.write_text(t, encoding='utf-8')

spec = 'docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md'
section(spec, '## Movement and attack posture', '## HP bar', '''## Physical technique decks and movement

The beam-corridor scheduler is superseded. Each apparition owns three physical techniques and Vespera owns two. Technique selection alternates two deterministic, nonrepeating decks independently per source; a rhythm pattern is not a technique. Final rotates the notes between retained living sources without copying every attack for every player. `CrimsonTechnique.cs` owns exact geometry/tuning; `CrimsonGesturePlan` is the immutable phase/phrase/note contract shared by movement, native collision and drawing.

| Source | Technique | Action and silhouette |
|---|---|---|
| Ember Crown | CrownRain | Nineteen possible columns of falling solid shards, with three neighboring columns omitted; successive notes move the opening. These are short moving shards with swept collision, not persistent lasers. |
| Ember Crown | CrownCinders | Three cinders follow visible harmless lob trajectories, then expand into local blast disks on the response notes. |
| Ember Crown | CrownCrash | Crown's actual body advances along a forecast dive in successive musical steps; contact uses its swept capsule plus a growing impact ring. No unannounced contact between notes. |
| Sable Mantle | MantleFan | A curved cloth blade sweeps an announced fan in front of the mantle; it is not a rectangular full-field beam. |
| Sable Mantle | MantleRush | Actual body lunges through a locked traversal in musical segments with matching swept contact and short silhouette afterimages. |
| Sable Mantle | MantleScissors | Two curved ribbon blades extend from opposite edges and close across the locked focus, then retract harmlessly. |
| Thorn Choir | ChoirThrust | Three articulated thorn tendrils extend from the body toward separate endpoints, visibly joining root to tip. |
| Thorn Choir | ChoirHook | A long curved tentacle reaches past the focus and hooks inward; the forecast includes both reach and hooked return. |
| Thorn Choir | ChoirRend | Three jagged finite spatial wounds open sequentially across different field cells per impact. A phrase cuts space nine to fifteen times, not one sustained beam. |
| Vespera, Final | VesperaOrbit | Four finite orbiting projectiles travel around a locked focus, with the intervening empty center preserved. |
| Vespera, Final | VesperaPetals | Six finite petals converge along curved paths rather than firing another straight laser. |

Ordinary motion loosely follows the authority-retained living target. Technique reservations move the active body from its actual position to its authored upper/side/corner staging point before the first impact. Rush/crash then move that same body along the locked path; replicated positions and fractional rendered poses consume the same descriptor. Between phrases the body blends back into pursuit. A moving or retargeted player cannot drag a forecast already issued. Physical attacks use frozen focus coordinates where appropriate; the superseded ban on all player-coordinate inputs applied only to the retired corridor generator.

Each species articulates differently on the original artwork: Crown opens its crown/legs and compresses on impact; Mantle stretches its outer cloth into wings and recoils asymmetrically; Choir ripples and extends its lower tendrils. Vespera blends float/cast poses and follows every source's call and release. These are procedural deformations and explicit path animations, not a claim of new hand-painted sprite frames. Vespera remains normal NPC size.

## Rhythm choreography: call, breath, response

The accepted music and measured beat map remain unchanged. Adjacent measured beats are subdivided; no hardcoded128BPM timer or cumulative seven-/fourteen-tick rounding is used. The complete four-beat phrase is reserved ahead of its first forecast. Reservation lead is overlapped with the prior phrase rather than inserting an accidental extra rest every bar.

| Phrase variant | Call subdivisions | Response subdivisions |
|---|---|---|
| Groove | 0,2,4 | 8,10,12 |
| Syncopated groove | 0,3,4 | 8,11,12 |
| Delayed accent | 0,2,5 | 8,10,13 |
| Flam | 0,1,4 | 8,9,12 |
| Fill | 0,1,3,5,6 | 8,9,11,13,14 |
| Final Roll | 0,1,2,3 | 8,9,10,11 |

Fill takes every fourth phrase; qualifying energetic Final passages may choose Roll; the remaining phrases vary the groove using the documented scheduler. This is a deterministic authored arrangement over the beat map, **not** a certified transcription of the recording's real drum fills. Audible alignment and phrasing remain listening checks. Each impact echoes its own forecast timing two measured beats later, retaining40–180ticks of lead. Short native collision pulses remain2–5ticks; visual aftermath is harmless. Native immunity/dodge/equipment are not cleared to force a hit on every sixteenth note.

Physical silhouettes replace universal laser materials. Crown emphasizes falling shards/disks, Mantle curved blades/body trails, Choir tapered thorned curves and jagged cuts. Forecasts contain the full swept footprint; the actual damaging shape is sampled from the identical pure geometry. Reduced Effects preserves these silhouettes and timing while reducing localized plasma, particles and shake. Project-authored effects are driven through Luminance's managed shaders at impacts, never used to paint every attack as a beam.

## Cooperative interludes: Stack and Spread

From Act II onward, after five physical phrases, an isolated cooperative interlude alternates **Stack** and **Spread** while Vespera remains present. Act I first teaches its physical deck. The call begins on a measured beat after the prior physical phrase and its tail; resolution falls eight measured beats later, followed by two beats of recovery. No rush, tentacle, spatial cut or incompatible spread/stack instruction runs simultaneously. The next physical phrase resumes after that recovery. Transitions, source death and cleanup invalidate the entire old interlude and its strikes.

Stack uses one fixed world-space220px-radius circle, inward chevrons and a shrinking progress arc. At the authority deadline the native source budget is360times the number of retained living announced participants. Gathered participants share that budget equally; an announced survivor outside the circle receives the unsplit budget. Thus a correct Stack still has shared **native** damage, unlike Doll's zero-cost successful gathering mechanic. Armor, DR, shields, dodge and immunity remain native. Death/disconnect removes that participant from the budget rather than making a dead slot a mandatory gatherer.

Spread attaches a140px-radius ring and outward chevrons to each announced living participant. At the same authority deadline, pairs whose centers are strictly closer than280px fail. Only overlapping participants receive600native source damage, once per person irrespective of overlap count. Tangency passes, proper separation costs zero, and solo Spread is harmless. The unchanged field has room for eight separated markers; movement fairness in actual terrain/latency is a playtest requirement.

The server owns the immutable roster mask, beat deadline and distance verdict. The marker is always harmless. Positive verdicts create bounded, recipient-gated ordinary hostile projectiles, not manual `Player.Hurt`, direct HP subtraction or client-claimed success. Recipient gating also runs in the receiving client's CanDamage/collision path, not only in CanHitPlayer. Hits expire after12authority-clock ticks and cannot be reapplied after a phase change or accepted terminal state; normal native invulnerability can suppress them. At most one marker and eight strikes are allocated per interlude. Whole allocation capacity is checked before issuing verdict strikes; invalid or missing owned resources fail through the existing coordinator cleanup.

The borrowed words Stack/Spread do not import Doll's Down/revival domain, equipment emulation, saved flags or participant system. Scarlet still uses normal death. Feedback is circles, inward/outward motion and existing project-owned cues, without restoring explanatory combat text.

## Ownership, preparation and scenery

`CrimsonRuntime` remains the sole phase/target/deck/interlude/terminal owner. Native gesture descriptors include exact Fight, source, phase epoch, phrase/note identity, bounded times and finite geometry; clients cannot retarget, alter a technique or revive an old phase. Source death cancels its remaining notes. Full-phase cleanup includes gestures, interlude markers/strikes, retained bodies and the exact pedestal lease. No new feature switch is added to Common or the global router.

Doll's attendant is no longer selected by the pedestal's generic Preparing flag. Creation, AI visibility and PreDraw require **Doll's exact encounter key, Fight/sequence, preparation projection and matching Core anchor**. Scarlet preparation therefore keeps only Vespera; an obsolete Doll replica hides immediately even before server despawn arrives. Doll's own preparation is retained.

Scarlet has its own client-only CustomSky: an ash-filled apse for Crown, draped velvet vault for Mantle, thorn-filled void for Choir and an eclipse combining those motifs in Final. Depth layers crossfade with the accepted phase. Luminance-owned noise/bloom textures and managed localized plasma support the original artwork. Scenery is low contrast behind combat, fades on loss of the exact participant/Fight and resets on world exit/reload; it does not alter world weather/time, tiles or another Raid's sky. No new external textures, recording or shader dependency is added.
''')
# Remove stale statements in the unchanged tail without altering approved music or rights text.
edit(spec, 'Measured intensity drives orb/casting tension and attack density.', 'Measured intensity helps select Final rhythmic accents; immutable gesture/interlude times drive casting and release.')
edit(spec, 'Existing project-authored PortalBeam/RaidEnergy materials supply narrow forecasts, sparse footprint particles, bright moving heads, continuous bodies, corona and source mouths.', 'Existing project-authored managed materials remain available, but physical techniques draw solid capsules, arcs, shards and finite projectiles; CrimsonReactor plasma is localized to core/impact accents rather than universal beam bodies.')
p = root / spec
t = p.read_text(encoding='utf-8')
a = t.index('Owner smoke:')
t = t[:a] + '''Owner smoke: build an isolated matching package and reload. Check Scarlet preparation without a Doll at the pedestal and ordinary Doll preparation separately. Play each three-technique deck through20% retreat, then the four-target Final; verify actual body motion, cloth sweeps, rooted tentacles, spatial cuts and short damage footprints. Inspect phase-specific scenery behind terrain/players and the HP bar. Listen to groove variants/fills/rolls and cooperative beat deadlines against the recording. Check Stack shares, Spread separation, source death/phase cancellation, normal equipment/immunity,1/2/4/8-player cases, delayed delivery, repeated exit/re-summon, Reduced Effects and frame time. Automated geometry and codecs do not certify musical fit, rendering or native gameplay.\n'''
p.write_text(t, encoding='utf-8')

section('docs/STATUS.md', '## Current build', '## Verification state', '''## Current build

Feature-branch source: **0.3.15 / protocol48**, on `feat/scarlet-rhythm-phases`, based on main0.3.12 (`9f46892`). Retains sequential20%-retreat acts, retained-HP four-target Final and explicit standard boss bar. Adds eleven species-specific physical techniques, body/path articulation, varied beat-map grooves/fills/rolls, isolated Stack/Spread interludes, exact-Doll pedestal presentation and Scarlet's phase-specific sky. Stable internal IDs and approved art/music remain. This branch is **not merged, installed or published**; use matching48 peers.

- **Doll:** initial prototype complete as designated by the owner; its existing combat, recovery and rewards remain. Only attendant ownership gating changes here.
- **Ghost Samurai:** existing implementation preserved, with outstanding user-owned playtests; not another completed Raid.
- **Scarlet Invocation:** [owning spec](encounters/crimson-foundry/ENCOUNTER_SPEC.md) describes the physical technique decks, normal native damage/death, cooperative interludes and scenery. No companion party substitution, new reward progression or equipment emulation is added.
- Solo admission remains normal gameplay; no fake players or compile-time multiplayer requirement.
''')
p = root / 'docs/STATUS.md'
t = p.read_text(encoding='utf-8').replace('## Verification state\n', '''## Verification state

- **0.3.15 Scarlet completion:** [evidence](evidence/2026-09-16-scarlet-techniques.json) records exact inputs and CI results. The earlier physical-technique commit had passing linked checks but left the status/spec on0.3.13; this revision reconciles the owning documents and adds cooperative domain/codec/wiring checks. **Native package compilation, loading, GPU rendering, listening and matching-peer gameplay are not claimed**. The chat working container was unavailable; verification runs in the branch's GitHub Actions environment. No workstation package is replaced.
''', 1)
p.write_text(t, encoding='utf-8')
section('docs/STATUS.md', '## Next change', '## History', '''## Next change

Build an isolated0.3.15/protocol48 package with the pinned tModLoader/Calamity/Luminance installation, then load/reload and inspect all physical techniques, exact-Doll/Scarlet pedestal separation, native boss bar and phase skies. On matching peers confirm retained HP, body/forecast alignment, cooperative shares/separation, source loss, stale phase cancellation, wipe versus clear and safe re-summon. Listen to the actual music; six deterministic rhythm variants are not proof of audible drum transcription. No merge, shared-profile installation or publication is implied.
''')

p = root / 'docs/adr/0026-crimson-score-and-native-projectiles.md'
t = p.read_text(encoding='utf-8')
t += '''
## Physical techniques and cooperative interludes amendment — 2026-09-16

Supersedes the prior beam/corridor-only choreography and phrase formations, not the accepted native receiving-player damage boundary. The owner's latest scope requires species-specific moving bodies, tentacles, sweeps, rushes, sequential spatial cuts, distinct scenery and optional cooperative mechanics. `CrimsonGesturePlan` owns immutable physical motion/collision facts; one native projectile represents an impact, not each rendered segment. Pure geometry and species decks are linked into domain tests. Clients sample the same paths and cannot choose targets, phases or results.

Protocol48 includes the preceding physical-gesture protocol47 and new immutable `CrimsonChorusPlan`/`CrimsonChorusImpact` ExtraAI descriptors. Existing explicit packet IDs are not renumbered. The runtime issues a bounded roster mask and musical Stack/Spread deadline, resolves only on authority, then emits recipient-gated native hostile strikes for positive budgets. Markers are harmless; no Player.Hurt adapter, HP rewrite or immunity override is introduced. Exact Fight, source and phase checks cover every marker and strike. Death, phase transition and cleanup revoke them, and missing/capacity failures enter the coordinator's normal failure path. The feature spec owns budgets and scheduling, not this structural record.

Doll's attendant requires its own exact preparation projection and Core anchor instead of interpreting another Raid's Preparing lease. Scarlet's CustomSky is client-only and ephemeral; Luminance managed shaders/textures stay presentation-only, with GPU creation on draw and captured-instance disposal as before. Source art/music are unchanged. Native compilation and hardware/MP acceptance remain separate from linked-source tests.
'''
p.write_text(t, encoding='utf-8')
p = root / 'docs/NETWORK_ARCHITECTURE.md'
t = p.read_text(encoding='utf-8')
# Prepend an amendment under the top-level title, preserving historical protocol notes.
lines = t.splitlines(True)
for i, line in enumerate(lines):
    if line.startswith('# '):
        lines.insert(i + 1, '''
## Scarlet development protocol48

Includes protocol47 physical gestures and the new phase-scoped cooperative marker/verdict ExtraAI. Gesture fields identify exact Fight, parent slot, source/technique, phase, phrase/note, warning/fire/end and finite motion geometry. Chorus markers carry Fight/parent/phase/source, ordered-roster mask, beat deadline and fixed gather anchor; strikes add one bounded roster index and positive native source budget. Parse complete descriptors before replacement. Runtime alone resolves distance/sharing; receiving-client recipient gates complement native CanHitPlayer. Old phases and dead sources cannot reactivate attacks. Explicit packet IDs, Doll's protocol contracts and saved internal IDs are retained. See [ADR-0026](adr/0026-crimson-score-and-native-projectiles.md) and the [feature spec](encounters/crimson-foundry/ENCOUNTER_SPEC.md).

''')
        break
p.write_text(''.join(lines), encoding='utf-8')
p = root / 'docs/history/PLAYTEST_FEEDBACK.md'
t = p.read_text(encoding='utf-8') + '''
## Scarlet individual identity, pedestal and scenery — 2026-09-16 / 0.3.15

Owner rejects all apparitions feeling like beam emitters and reports a Doll on the shared preparation pedestal. Requests strongly differentiated physical techniques and motions (tentacles, sweeps, rushes, sequential spatial cuts), musically varied rather than uniform phrasing, a fitting background and Luminance effects; permits Stack/Spread. Retain the physical-technique work already on the feature branch, finish isolated cooperative interludes and their native-damage/phase gates, correct stale owning docs, and verify exact-Doll presentation gates. This is user-reported experience, not a new recorded session. [Evidence](../evidence/2026-09-16-scarlet-techniques.json) separates automated checks from native/GPU/listening/MP acceptance. No main merge or installed package replacement.
'''
p.write_text(t, encoding='utf-8')
p = root / 'CHANGELOG.md'
t = p.read_text(encoding='utf-8').replace('## [Unreleased]\n', '''## [Unreleased]

- Scarlet feature branch0.3.15/protocol48: eleven physical technique decks with body-driven attacks, varied measured-beat call/response, isolated native Stack/Spread, exact-Doll pedestal gating and phase-specific Scarlet skies. Owning docs and verification boundaries updated; no release or installed-package claim.
''', 1)
p.write_text(t, encoding='utf-8')
# Replace obsolete corridor-only summaries without changing unrelated descriptions.
for path in ['README.md', 'description.txt', 'description_workshop.txt']:
    p = root / path
    t = p.read_text(encoding='utf-8')
    t = t.replace('musical call/response corridors', 'musical physical techniques and cooperative interludes')
    t = t.replace('dodge field-filling patterns through safe corridors', 'dodge distinct shard, sweep, rush, tentacle and spatial-cut phrases')
    p.write_text(t, encoding='utf-8')

tracked = ['Content/Encounters/CrimsonFoundry/CrimsonTechnique.cs', runtime, rules,
    'Content/Encounters/CrimsonFoundry/CrimsonChorus.cs', 'Client/Encounters/CrimsonFoundry/CrimsonChorusVisuals.cs',
    'Client/Encounters/CrimsonFoundry/CrimsonRig.cs', 'Client/Encounters/CrimsonFoundry/CrimsonSky.cs',
    'Content/Encounters/FirstSeverance/Actors/FirstSeveranceDollAttendant.cs']
evidence = {
    'date': '2026-09-16', 'feature': 'crimson_foundry', 'display_name': 'Scarlet Invocation',
    'source_version': '0.3.15', 'protocol': 48, 'branch': 'feat/scarlet-rhythm-phases',
    'integration_base': 'f03903563a1fdb69b02a15ed59caf5ba032d227c',
    'changes': ['eleven physical techniques retained and reviewed', 'six varied musical phrases',
        'native Stack/Spread interludes', 'exact-Doll preparation gate', 'phase skies and Luminance-managed impact effects',
        'stale spec/status reconciliation'],
    'source_sha256': {path: hashlib.sha256((root / path).read_bytes()).hexdigest() for path in tracked},
    'verification': {'linked_domain': 'pending current CI run', 'tooling_static_codec': 'pending current CI run',
        'native_package_build': 'not_run: pinned game and dependencies absent; working container unavailable',
        'gpu_and_loading': 'not_run', 'music_listening': 'not_run', 'matching_peer_playtest': 'not_run'},
    'limits': ['Deterministic rhythm variants are not certified drum transcription.',
        'Static source guards do not exercise the engine.', 'Native immunity may suppress a musical pulse or mechanic strike.',
        'Correct Stack still applies a native shared source budget; successful Spread is free.',
        'Movement fairness, sky draw order, boss bar and actual API binding require native verification.'],
    'new_external_assets': False, 'merge_install_publication': 'not performed'
}
(root / 'docs/evidence/2026-09-16-scarlet-techniques.json').write_text(json.dumps(evidence, indent=2) + '\n', encoding='utf-8')
print('Integrated Scarlet physical decks, cooperative interludes and owning documentation.')
