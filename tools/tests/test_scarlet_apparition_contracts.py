"""Scarlet attack expression (S2): the Ember Crown and Sable Mantle drawing their strikes. Source guards only; the
look is checked by the offline rig harness (tools/preview-scarlet-rigs.ps1) at real size, never by these checks."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT/'Client/Encounters/CrimsonFoundry'
RIG = CLIENT/'ScarletApparitionRig.cs'
SHADER = ROOT/'Assets/AutoloadedEffects/Shaders/ScarletApparitions.fx'


def read(path):
    return path.read_text(encoding='utf-8')


def code(path):
    """Source without // comments, so a rationale may name what the code must not do."""
    return '\n'.join(line.split('//', 1)[0] for line in read(path).splitlines())


def body(text, start, end):
    return text[text.index(start):text.index(end, text.index(start))]


class ScarletApparitionContracts(unittest.TestCase):
    def test_shared_uniforms_are_written_on_every_draw(self):
        # The effect is shared with the Final avatar and the free apparitions: the attack uniforms are set on every
        # draw, to their rest values when the body has no notes, never left over from the previous body.
        draw = body(code(RIG), 'internal static void Draw(', 'Vector2 Skin(Vector2 uv,in Pose pose,float t)')
        for uniform in ('"attack", body.Active ? new Vector4(body.Heat, body.Ignite, body.Front, body.Drain) : new Vector4(0, 0, -1, 0)',
                        '"attack2", body.Active ? new Vector4(body.Lean, body.PourLimit, body.Run, body.Row) : new Vector4(0, 1, -1, 0)',
                        '"attack3", body.Active ? new Vector4(body.Return, body.Swing, pulse, 0) : new Vector4(0, 0, free, 0)'):
            self.assertIn('shader.TrySetParameter(' + uniform + ');', draw)
            self.assertLess(draw.index(uniform), draw.index('if (species == 0)'))
        fx = read(SHADER)
        self.assertIn('rest (0, 0, -1, 0)', fx)
        self.assertIn('rest (0, 1, -1, 0)', fx)

    def test_ribbons_skip_invisible_draws(self):
        rig = code(RIG)
        ribbon = body(rig, 'void Ribbon(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float radius', 'void Patch(')
        self.assertIn('if(opacity*(1-dissolve)<=.001f) return;', ribbon)

    def test_mantle_filaments_give_way_only_outside_the_final_avatar(self):
        rig = code(RIG)
        draw = body(rig, 'internal static void Draw(', 'Vector2 Skin(Vector2 uv,in Pose pose,float t)')
        # The Final avatar (cut) keeps its accepted hook residues; every other Mantle draws the wake instead.
        final = body(draw, 'else if (cut >= .5f)', 'else\n')
        self.assertIn('Ribbon(hook,past,old,tail,', final)
        wake = draw[draw.index('history[j] = j == 0 ? now'):draw.index('for(int y=0;y<=Rows;y++)')]
        self.assertIn('shader.Apply("WispPass")', wake)
        self.assertNotIn('Ribbon(', wake)

    def test_only_the_painted_red_emits_and_only_drain_darkens(self):
        fx = read(SHADER)
        self.assertIn('float Paint(float blood){return smoothstep(.05,.45,blood);}', fx)
        crown = body(fx, 'float3 CrownAttack(', 'float3 MantleAttack(')
        mantle = body(fx, 'float3 MantleAttack(', 'float4 Body(VO i)')
        # Near-black iron and dark cloth never light (no "black body + burning lip").
        for term in ('cloth*paint*step(', 'saturate((f-y)/.2)*cloth*paint', '(1-bone)*paint*max('):
            self.assertIn(term, crown)
        self.assertIn('lip*paint*', mantle)
        # The pour is light on the painted cloth (its own folds and embroidery), not a flat wash.
        self.assertIn('(art*float3(1.25,.8,.55)+float3(.5,.16,.04)*value)*band', crown)
        self.assertNotIn('float3(1,.55,.25)*band', crown)
        # Only Drain darkens (design §2.0.3-4), at .25.
        self.assertIn('float dim=attack.w*.25*cloth;', crown)
        self.assertIn('float dim=attack.w*.25*(1-bone)*lit;', mantle)
        for heat_dim in ('attack.x*(1-ring)', 'attack.x*(1-seam)'):
            self.assertNotIn(heat_dim, fx)
        # The lip is a soft gradient against the mean of its neighbours, not a one-pixel contour.
        lip = body(fx, 'float Lip(float2 uv,float alpha)', 'float3 CrownAttack(')
        self.assertIn('float2 d=float2(.012,0);', lip)
        self.assertIn('*.25;', lip)

    def test_heart_answers_the_attack_only_over_the_painted_body(self):
        heart = body(read(SHADER), 'float4 Heart(VO i):COLOR0', 'float4 Spark(VO i)')
        self.assertIn('float torso=smoothstep(.25,.65,Opaque(heartArea.xy+q*heartArea.zw));', heart)
        # Around the body the accepted free beat and spiral; over it the attack clock's beat and the wound spiral.
        self.assertIn('float beat=lerp(shape.x,attack3.z,torso);', heart)
        self.assertIn('float spiral=lerp(Spiral(phase),Spiral(phase-r*9.5*twist+twist*2.5),torso)*exp2(-r*3);', heart)
        # The tear keeps its accepted pale term; the strike adds the Mantle's own red over the painted torso only.
        self.assertIn('float tear=exp2(-abs(q.x+sin(q.y*8+clock)*.08)*23)*exp2(-abs(q.y)*3);', heart)
        self.assertIn('color+=min(Red()*tear*shape.w*species*torso*(1-smoothstep(.12,.40,r))*.8*power*Calm(),float3(.55,.06,.12));', heart)
        self.assertNotIn('1+shape.w', heart)
        # The rig hands the free pulse to shape.x and the attack clock's to attack3.z, and the patch to heartArea.
        rig = code(RIG)
        self.assertIn('shader.TrySetParameter("shape",new Vector4(free,charge,recoil,body.Active?body.Ignite:0));', rig)
        self.assertIn('shader.TrySetParameter("heartArea",new Vector4(heart.X,heart.Y,size,size));', rig)
        self.assertIn('Vector2 heart=species==0?new(.5f,.30f):new(.527f,.428f);', rig)

    def test_no_thread_language_or_ribbon_material(self):
        fx = read(SHADER)
        self.assertIsNone(re.search(r'silk|thread|fibre|fiber', code(RIG), re.IGNORECASE))
        self.assertNotIn('ScarletRibbon', code(RIG))
        wisp = body(fx, 'float4 Wisp(VO i):COLOR0', 'technique ScarletApparitions')
        self.assertNotIn('lip', wisp)


if __name__ == '__main__':
    unittest.main()
