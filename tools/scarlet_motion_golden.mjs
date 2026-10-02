// Writes the G0 golden that pins ScarletGestureMotion (Client/Encounters/CrimsonFoundry/Vfx) to the
// owner-approved motion prototype. Run it once per prototype revision, never as part of a build:
//
//   node tools/scarlet_motion_golden.mjs <dir holding motion.js and rigs.js> Tests/Convergence.DomainTests/Data/scarlet-motion-golden.json
//
// The prototype lives outside the repository (the attack-expression design folder); the output records
// the sha256 of both files it was generated from. Only the approved functions are sampled: the
// Crown censer swing and pour jolt (motion.crown), the Mantle wind/whip with its low/high rows
// (motion.mantle), the shared envelope (motion.envelope), the quarter -> arm table (motion.choirArm) and
// the Choir arm pose (rigs.choirArm, a port of CrimsonChoirMotion.Arm). The rejected 4/4 figure
// (baton, engagement) is never imported. Timing inputs are plain integers in the shape of game
// plans; the beat helper only lays out sample phrases and is not part of what is pinned.
import { readFileSync, writeFileSync, mkdtempSync, rmSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { pathToFileURL } from 'node:url';

const [dir, output] = process.argv.slice(2);
if (!dir || !output) {
  console.error('usage: node tools/scarlet_motion_golden.mjs <prototype dir> <output json>');
  process.exit(2);
}
const motionSource = readFileSync(join(dir, 'motion.js'), 'utf8');
const rigsSource = readFileSync(join(dir, 'rigs.js'), 'utf8');
const sha = text => createHash('sha256').update(text, 'utf8').digest('hex');
const work = mkdtempSync(join(tmpdir(), 'scarlet-golden-'));
let M, R;
try {
  writeFileSync(join(work, 'motion.mjs'), motionSource);
  writeFileSync(join(work, 'rigs.mjs'), rigsSource.replace("from './motion.js'", "from './motion.mjs'"));
  M = await import(pathToFileURL(join(work, 'motion.mjs')).href);
  R = await import(pathToFileURL(join(work, 'rigs.mjs')).href);
} finally {
  rmSync(work, { recursive: true, force: true });
}
const { envelope, crown, mantle, choirArm: quarterArm, beatTick } = M;
const { choirArm } = R;

// Seven significant digits keep the file small; the C# check tolerates 1e-4.
const round = v => (typeof v === 'boolean' ? v : Number(v.toPrecision(7)));
function sample(start, step, count, evaluate, channels) {
  const out = Object.fromEntries(channels.map(c => [c, []]));
  for (let i = 0; i < count; i++) {
    const value = evaluate(start + step * i);
    for (const c of channels) out[c].push(round(value[c]));
  }
  return { start, step, count, ...out };
}
// One four-note run of a signature phrase on the 128 BPM grid, as the game schedules it:
// Born(i) = beat i, Fire(i) = beat i + 1 = Born(i + 1); the prototype's end is Fire + live + residue.
function run(firstBeat, span, extra) {
  const cues = [];
  for (let i = 0; i < 4; i++) {
    const born = beatTick(firstBeat + i), fire = beatTick(firstBeat + i + 1);
    cues.push({ born, fire, end: fire + span, ...extra(i) });
  }
  return cues;
}
const window = (cues, before = 8, after = 8, step = .5) => {
  const start = cues[0].born - before + .125, stop = cues[cues.length - 1].end + after;
  return { start, step, count: Math.floor((stop - start) / step) };
};

const scenarios = [];
// Shared envelope on one Hands-length note.
{
  const cue = { born: 100, fire: 128, end: 168 };
  scenarios.push({ name: 'envelope', kind: 'envelope', cues: [cue],
    ...sample(92.125, .25, 320, age => envelope(cue, age), ['p', 't', 'live', 'prepare', 'decay', 'wind', 'follow']) });
}
// Crown: a curtain walking right then left (side = walk direction * (note even ? 1 : -1), lead = note > 0).
for (const [name, first, dir] of [['crown-walk-right', 400, 1], ['crown-walk-left', 1201, -1]]) {
  const cues = run(first, 44, i => ({ side: dir * (i % 2 === 0 ? 1 : -1), lead: i > 0 }));
  const w = window(cues);
  scenarios.push({ name, kind: 'crown', cues, ...sample(w.start, w.step, w.count, age => crown(age, cues), ['x', 'y', 'turn', 'flare', 'kick']) });
}
{
  const cues = [{ born: 2000, fire: 2028, end: 2072, side: -1, lead: false }];
  const w = window(cues, 6, 6);
  scenarios.push({ name: 'crown-single', kind: 'crown', cues, ...sample(w.start, w.step, w.count, age => crown(age, cues), ['x', 'y', 'turn', 'flare', 'kick']) });
}
// Mantle: the shroud rope alternates combs (even notes low and rightward).
{
  const cues = run(640, 32, i => ({ dir: i % 2 === 0 ? 1 : -1, low: i % 2 === 0 }));
  const w = window(cues);
  scenarios.push({ name: 'mantle-rope', kind: 'mantle', cues, ...sample(w.start, w.step, w.count, age => mantle(age, cues), ['x', 'y', 'turn', 'sweep', 'row']) });
}
{
  const cues = [{ born: 3000, fire: 3029, end: 3061, dir: -1, low: false }];
  const w = window(cues, 6, 6);
  scenarios.push({ name: 'mantle-single-high', kind: 'mantle', cues, ...sample(w.start, w.step, w.count, age => mantle(age, cues), ['x', 'y', 'turn', 'sweep', 'row']) });
}
// Choir: the arm that owns each slammed quarter (FourHands pairs rotate with the signature ordinal).
const PAIRS = [[0, 2], [0, 3], [1, 3], [1, 2]];
for (const [ordinal, flip] of [[0, false], [1, false], [2, true]]) {
  const cues = [];
  for (let i = 0; i < 4; i++) {
    const born = beatTick(900 + ordinal * 8 + i), fire = beatTick(900 + ordinal * 8 + i + 1);
    for (const q of PAIRS[i].map(q => (q + ordinal) % 4))
      cues.push({ born, fire, end: fire + 40, arm: quarterArm(q, flip), broad: false });
  }
  const w = window([cues[0], cues[cues.length - 1]], 8, 8, 1.25);
  const arms = [0, 1, 2, 3].map(index =>
    sample(w.start, w.step, w.count, age => choirArm(index, age, .3, .2, cues), ['s', 'e', 'w', 'power', 'burst']));
  scenarios.push({ name: `choir-hands-${ordinal}${flip ? '-flipped' : ''}`, kind: 'choir', ordinal, flip, charge: .3, recoil: .2, cues, arms });
}
{
  const cues = [{ born: 4000, fire: 4056, end: 4112, arm: 0, broad: true }];
  const w = window(cues, 8, 8, 1.25);
  const arms = [0, 1, 2, 3].map(index =>
    sample(w.start, w.step, w.count, age => choirArm(index, age, .3, .2, cues), ['s', 'e', 'w', 'power', 'burst']));
  scenarios.push({ name: 'choir-broad', kind: 'choir', charge: .3, recoil: .2, cues, arms });
}
// No descriptors: summoning/sacrifice fall back to charge/recoil.
for (const [charge, recoil] of [[0, 0], [.7, 0], [.25, .6]]) {
  const arms = [0, 1, 2, 3].map(index =>
    sample(5000.375, 3.5, 40, age => choirArm(index, age, charge, recoil, []), ['s', 'e', 'w', 'power', 'burst']));
  scenarios.push({ name: `choir-rest-${charge}-${recoil}`, kind: 'choir', charge, recoil, cues: [], arms });
}
const quarters = [];
for (const flip of [false, true]) for (let q = 0; q < 4; q++) quarters.push({ quarter: q, flip, arm: quarterArm(q, flip) });

writeFileSync(output, JSON.stringify({
  _about: 'G0 golden for ScarletGestureMotion: sampled from the owner-approved prototype; regenerate only with tools/scarlet_motion_golden.mjs',
  source: { 'motion.js': sha(motionSource), 'rigs.js': sha(rigsSource) },
  tolerance: 1e-4,
  quarters,
  scenarios,
}) + '\n');
