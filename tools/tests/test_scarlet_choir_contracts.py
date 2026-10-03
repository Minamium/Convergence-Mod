"""Scarlet attack expression (S3): the Thorn Choir's drawing of its strikes. Source guards only; the look is checked
by the offline rig harness (tools/preview-scarlet-rigs.ps1) at real size, never by these checks."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT/'Client/Encounters/CrimsonFoundry'
RIG = CLIENT/'CrimsonChoirRig.cs'
BLOOD = CLIENT/'Vfx/ScarletChoirBlood.cs'
SHADER = ROOT/'Assets/AutoloadedEffects/Shaders/ScarletChoir.fx'


def read(path):
    return path.read_text(encoding='utf-8')


def code(path):
    """Source without // comments, so a rationale may name what the code must not do."""
    return '\n'.join(line.split('//', 1)[0] for line in read(path).splitlines())


def body(text, start, end):
    return text[text.index(start):text.index(end, text.index(start))]


class ScarletChoirContracts(unittest.TestCase):
    def test_strikes_read_only_plan_times(self):
        # No beat-driven motion: the Choir answers its cues' Born/Fire/End and a participant's notes only.
        banned = re.compile(r'CrimsonMeter|Score\.Pulse|BeatTick|baton|engagement|Transfusion\(|DrawSecondary\(')
        for path in (RIG, BLOOD):
            self.assertIsNone(banned.search(code(path)), f'{path.name} must not follow the beat grid')
        blood = code(BLOOD)
        for token in ('using Microsoft.Xna', 'using Terraria', 'using Luminance', 'Vector2', 'Main.', 'GameUpdateCount',
                      'DateTime', 'Stopwatch', 'new Random'):
            self.assertNotIn(token, blood, f'ScarletChoirBlood stays FNA/Terraria-free with no free clock ({token})')
        self.assertIn('ScarletEnvelope.Send(ScarletEnvelope.Progress(age, cue.Born, cue.Fire))', blood)
        self.assertIn('ScarletEnvelope.Return(t, cue.End - cue.Fire)', blood)
        self.assertIn('ScarletEnvelope.Ignite(since)', blood)

    def test_signature_keeps_the_s1_seam(self):
        rig = read(RIG)
        self.assertIn('float heave = 0, in Vfx.ScarletChoirState material = default)', rig)
        # Arms first, then the torso heaves with the slam (heave is 0 outside the Act effigy).
        draw = body(rig, 'internal static void Draw(', 'Vector2 Map(Vector2 p)')
        self.assertLess(draw.index('arms[i] = CrimsonChoirMotion.Arm(i, age, charge, recoil, cues);'), draw.index('Vector2 root ='))
        self.assertIn('+ heave * burst);', draw)

    def test_rest_picture_is_kept_without_cues_or_material(self):
        rig = code(RIG)
        self.assertIn('bool attacking = !armsOnly && !cues.IsEmpty;', rig)
        self.assertIn('bool lit = material.Active && !armsOnly;', rig)
        # The shared shader's attack uniforms are written on every draw (Final avatar and free apparitions reuse it).
        self.assertIn('shader.TrySetParameter("attack", lit ? new Vector4(', rig)
        self.assertIn('shader.TrySetParameter("heart", lit ? new Vector4(', rig)
        # Vertex alpha is the blood front: 0 unless lit.
        self.assertIn('float front = lit ? blood[armOf[index]].At(along[index]) : 0;', rig)
        # Today's afterimage and sleeve outline are kept verbatim.
        self.assertIn(': .14f + pose.Burst * .6f;', rig)
        self.assertIn('58 + pose.Power * 66 + pose.Burst * 48, arm * 3.7f + 4,', rig)
        # The fingers turn only by the strike's rotation away from the idle sway.
        self.assertIn('var rest = CrimsonChoirMotion.Arm(arm, age, 0, 0, default);', rig)

    def test_wings_answer_their_own_side(self):
        rig = code(RIG)
        self.assertIn('int upper = side < 0 ? 0 : 1, lower = upper + 2;', rig)
        self.assertIn('Math.Max(arms[upper].Power, arms[lower].Power)', rig)
        self.assertIn('Math.Max(arms[upper].Burst, arms[lower].Burst)', rig)

    def test_skeleton_is_measured_once_on_load(self):
        rig = read(RIG)
        load = body(rig, 'internal static void Load()', 'internal static void Unload()')
        self.assertIn('if (Main.dedServ) return;', load)
        self.assertIn('MeasureSkeleton();', load)
        self.assertIn('new float[(Columns + 1) * (Rows + 1)]', rig)
        self.assertNotIn('MeasureSkeleton();', body(rig, 'internal static void Draw(', 'private static void MeasureSkeleton()'))

    def test_sparks_are_one_draw(self):
        rig = code(RIG)
        self.assertEqual(1, rig.count('shader.Apply("SparkPass")'))
        self.assertNotIn('"SparkPass")', rig.replace('shader.Apply("SparkPass")', ''))

    def test_shader_attack_terms_vanish_at_rest(self):
        fx = read(SHADER)
        for uniform in ('float4 attack;', 'float4 heart;', 'float4 heartArea;'):
            self.assertIn(uniform, fx)
        self.assertIn('float front=i.C.a;', fx)
        self.assertIn('float tear=(shape.w>0&&shape.z<.5)', fx)
        # No bead on the thin flow line (it read as beads on a thread): the sent blood stays inside the painted limb.
        self.assertNotIn('knot', fx)
        # The strike in the heart's core only over the painted torso; the outer glow is the accepted one.
        self.assertIn('float core=heart.x*(1-smoothstep(.30,.55,radius))*smoothstep(.05,.35,tex2D(art,heartArea.xy+q*heartArea.zw).a);', fx)
        self.assertIn('c+=(HeartGlow(q/(1-heart.w),n,v,struck,heart.z)-c)*core*Calm();', fx)
        self.assertIn('shape.x*i.C.a*signal.z', fx)
        # The Ribbon adds no light for the tear (it only parts the smoke) and has no silk/thread branch.
        ribbon = body(fx, 'float4 Ribbon(VO i)', 'float4 Heart(VO i)')
        self.assertNotIn('lip', ribbon)
        self.assertIsNone(re.search(r'silk|thread|fibre|fiber', fx, re.IGNORECASE))
        self.assertTrue((SHADER.with_suffix('.fxc')).exists())

    def test_reduced_keeps_the_shape_and_quiets_the_light(self):
        # Reduced Effects: every vertex where Normal has it (the throb takes the full strike), the same blood front
        # (no halving in vertex alpha), and the shader quiets only the attack's light with Calm().
        rig = code(RIG)
        self.assertIn('throb = Math.Max(free * (1 - material.Engaged), material.Ignite / (reduced ? ScarletBodyMaterial.ReducedIgnite : 1));', rig)
        # The outer heart glow and its patch keep their accepted values; only the masked core answers the strike.
        self.assertIn('shader.TrySetParameter("shape", new Vector4(free, power, burst, 0));', rig)
        self.assertIn('Vector2 halfSize = new(235 + power * 60 + burst * 90, 260 + power * 70);', rig)
        self.assertIn('deform += (p - CrimsonChoirMotion.Heart) * heartWeight * (.09f * throb + .13f * power);', rig)
        self.assertIn('blood[i] = lit ? ScarletChoirBlood.Of(cues, i, age, material.Heat) : default;', rig)
        self.assertIn('shader.TrySetParameter("heartArea", ', rig)
        blood = code(BLOOD)
        self.assertNotIn('reduced', blood)
        self.assertIn('internal const float SendPeak = .7f;', blood)
        self.assertIn('Gain * Math.Max(SendPeak * Bead(u - Send, 9),', blood)
        fx = read(SHADER)
        self.assertIn('float Calm(){return signal.w*signal.w*.9+.1;}', fx)
        self.assertIn('float glow=saturate((front-.75)/.25);', fx)
        body = fx[fx.index('float4 Body(VO i)'):fx.index('float4 Aura(VO i)')]
        for term in ('flush=saturate(front*1.6)*(1-glow)*limb*bone*Calm()', 'front*1.4*Calm()', 'glow*.7*Calm()'):
            self.assertIn(term, body)

    def test_flow_ribbons_carry_no_knot(self):
        rig = code(RIG)
        self.assertIn('arm * 2.31f, (.30f + pose.Power * .48f + pose.Burst * .45f) * exposure * (1 - dissolve), 1);', rig)
        self.assertNotIn('knot', rig)
        self.assertNotIn('wristAlong', rig)


if __name__ == '__main__':
    unittest.main()
