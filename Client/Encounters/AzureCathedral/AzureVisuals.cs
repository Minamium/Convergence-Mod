#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Convergence.Content.Encounters.AzureCathedral;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace Convergence.Client.Encounters.AzureCathedral;

public sealed class AzureVisualConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue(false)] public bool ReducedEffects;
    [DefaultValue(true)] public bool ScreenShake = true;
}

[Autoload(Side = ModSide.Client)]
internal sealed class AzureVisuals : ModSystem
{
    // Clock keys beyond the AzureCue values: screen shakes (at the picture event, not the sound), voice stops, verdicts.
    private const int ShakeKey = 100, StopKey = 160, VerdictKey = 170;
    private const int StopStaging = 0, StopDevour = 1, StopEnd = 2;
    private static readonly float[] ChorusPitch = { 0f, .25f, .583f };
    private float shake;
    private Matrix worldToViewport;
    private Guid projectedFight;
    private readonly AzureCueClock clock = new();
    private readonly AzureVoices voices = new();
    private readonly int[] lastPlayed = new int[(int)AzureCue.Count];
    private readonly Dictionary<byte, (uint Revision, bool Downed)> recovery = new();
    private readonly Dictionary<int, int> latticeEnd = new();
    private bool lastTerminal, beamLive;
    private int beamKey = -1, beamEnd, girlAliveAt;
    private SlotId beam = SlotId.Invalid;
    private Vector2 beamCenter;
    private static int lastTick;
    private static long received;
    internal static bool Reduced => ModContent.GetInstance<AzureVisualConfig>().ReducedEffects;
    internal static bool Local(AzureBoss girl) => Array.Exists(girl.State.Members ?? Array.Empty<AzureMember>(), m => m.Slot == Main.myPlayer);
    internal static float RenderAge(AzureBoss girl)
    {
        int tick = (int)girl.VisualAge;
        if (lastTick != tick) { lastTick = tick; received = Stopwatch.GetTimestamp(); }
        return tick + (Main.gamePaused ? 0 : (float)Math.Clamp((Stopwatch.GetTimestamp()-received)/(double)Stopwatch.Frequency*60,0,1));
    }
    public AzureVisuals() => Array.Fill(lastPlayed, int.MinValue / 2);
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        // Followers and fades keep running when there is no Boss, so a natural end can release its tails.
        voices.Tick();
        var girl = AzurePackets.Boss;
        // No Boss after a terminal state is the natural end: Victory/Defeat tails play out. Anything else is unexpected.
        if (girl is null) { Withdraw(lastTerminal ? 0 : 10); return; }
        // A stale or non-roster view keeps the clock (no replay when it returns); only the voices are lowered,
        // except after the result, whose Victory/Defeat tails are left to play out.
        if (!girl.Fresh || !Local(girl)) { Withdraw(lastTerminal ? 0 : 10); return; }
        var s = girl.State;
        int age = (int)girl.VisualAge;
        ModContent.GetInstance<AzureMusicScene>().UpdateFade(girl);
        bool first = clock.Advance(s.Fight, age);
        if (first) BeginFight();
        lastTerminal = s.EndAt >= 0;
        shake *= .85f;
        Ceremony(girl, s, first);
        Recovery(s);
        beamLive = false;
        latticeEnd.Clear();
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is AzureChorus marker) { if (marker.Plan.Fight == s.Fight) Chorus(s, marker, age); continue; }
            if (p.ModProjectile is AzureAttack attack && attack.Plan.Fight == s.Fight) Attack(girl, s, p, attack, age);
        }
        foreach (var line in latticeEnd) Cue(AzureCue.LatticeEnd, line.Key, line.Value + AzureCueRules.LatticeEndDelay);
        // The beam ending on its own (End) lets the 3s sweep finish; a beam removed early (Liora fell) fades it.
        if (beam.IsValid && !beamLive) { if (age < beamEnd) voices.Fade(beam, 8); beam = SlotId.Invalid; }
        Rush(girl, s, age);
    }

    private void Withdraw(int ticks)
    {
        voices.Leave(ticks);
        projectedFight = Guid.Empty;
        shake = 0;
    }

    private void BeginFight()
    {
        recovery.Clear(); latticeEnd.Clear();
        beamKey = -1; beam = SlotId.Invalid; beamLive = false;
        lastTerminal = false; shake = 0;
        girlAliveAt = clock.High;
    }

    // Intro, phases and ending. Sound ticks lead their picture event so the peak of each sound lands on it;
    // the screen shake stays on the picture event.
    private void Ceremony(AzureBoss girl, AzureState s, bool first)
    {
        Vector2 liora = girl.NPC.Center;
        if (s.MusicStart >= 0)
        {
            int m = s.MusicStart;
            Vector2 rift = Rift(s);
            Cue(AzureCue.PrisonBreak, m, m + AzureCueRules.PrisonBreak, liora);
            Cue(AzureCue.SwordLight, m, m + AzureCueRules.SwordLight, liora);
            Cue(AzureCue.RiftOpen, m, m + AzureCueRules.RiftOpen, rift);
            Cue(AzureCue.WormArrival, m, m + AzureCueRules.WormArrival, rift);
            Shake(0, m, m + AzureRules.IceBreak, 9);
            Shake(1, m, m + AzureRules.SwordLight, 5);
            Shake(2, m, m + AzureRules.WormArrival, 8);
        }
        // Liora's HP reaching 0 has no tick of its own: it sounds on the frame it is first received.
        // Already 0 on the first frame of a Fight (late join, reconnect), or first seen more than a second
        // after she was last seen alive (a replication gap), is only remembered.
        if (s.GirlLife > 0) girlAliveAt = clock.High;
        else
        {
            if (first || clock.High - girlAliveAt > 60) clock.Baseline((int)AzureCue.LioraFall, 0);
            else if (clock.Latch((int)AzureCue.LioraFall, 0))
            {
                Play(AzureCue.LioraFall, liora);
                voices.FadeOwned(AzureOwner.Liora, 6); // ClearHazards removed her attacks and the chorus
            }
        }
        if (s.Phase == AzurePhase.Duet && s.StagingAt >= 0 && s.EndAt < 0)
        {
            var id = Cue(AzureCue.WormRetreat, s.StagingAt, s.StagingAt, Head(girl) ?? liora);
            if (id.IsValid) FollowHead(id, AzureCue.WormRetreat, girl, AzureRules.StagingTicks);
            if (clock.Latch(StopKey + StopStaging, s.StagingAt)) voices.FadeOwned(AzureOwner.Worm, 6); // ClearHazards(FrostBolt)
        }
        if (s.Phase == AzurePhase.Devouring)
        {
            int p = s.PhaseAt;
            var rush = Cue(AzureCue.DevourRush, p, p + AzureCueRules.DevourRush, Head(girl) ?? liora);
            if (rush.IsValid) FollowHead(rush, AzureCue.DevourRush, girl, AzureCueRules.DevourRushLead);
            Cue(AzureCue.DevourBite, p, p + AzureRules.DevourContact, liora);
            Shake(7, p, p, 3);
            Shake(8, p, p + AzureRules.DevourRush, 5);
            Shake(9, p, p + AzureRules.DevourContact, 10);
            if (clock.Latch(StopKey + StopDevour, p)) voices.FadeOwned(AzureOwner.Liora | AzureOwner.Worm, 6); // ClearHazards()
        }
        if (s.Phase == AzurePhase.Fury)
        {
            Cue(AzureCue.FuryAwaken, s.PhaseAt, s.PhaseAt, Head(girl) ?? liora);
            Shake(10, s.PhaseAt, s.PhaseAt, 8);
        }
        if (s.EndAt < 0) return;
        int e = s.EndAt;
        if (s.Stage == AzureStage.Victory)
        {
            Cue(AzureCue.FinalBlow, e, e, Head(girl) ?? liora);
            var melt = Cue(AzureCue.MeltRush, e, e + AzureRules.MeltRush, Head(girl) ?? liora);
            if (melt.IsValid) FollowHead(melt, AzureCue.MeltRush, girl, AzureRules.MeltContact - AzureRules.MeltRush);
            Cue(AzureCue.MeltContact, e, e + AzureRules.MeltContact, liora);
            Cue(AzureCue.ChainMelt, e, e + AzureRules.MeltContact);
            Cue(AzureCue.Victory, e, e + AzureRules.VictoryCue);
            Shake(3, e, e + AzureRules.MeltRush, 3);
            Shake(4, e, e + AzureRules.MeltContact, 8);
            Shake(5, e, e + AzureRules.VictoryCue, 4);
        }
        else
        {
            Cue(AzureCue.Defeat, e, e);
            Shake(6, e, e, 9);
        }
        if (clock.Latch(StopKey + StopEnd, e)) voices.FadeOwned(AzureOwner.Liora | AzureOwner.Worm, 6); // ClearHazards()
    }

    private void Attack(AzureBoss girl, AzureState s, Projectile projectile, AzureAttack attack, int age)
    {
        var plan = attack.Plan;
        Vector2 at = new(plan.X, plan.Y);
        Shake(20 + (int)plan.Kind, plan.Fire, plan.Fire,
            plan.Kind == AzureAttackKind.MouthBeam ? 7f : plan.Kind == AzureAttackKind.GlacialCut ? 4.8f : 2.2f);
        // An attack the authority has already cleared (Liora fell, phase ended) no longer announces itself.
        if (!attack.TryGirl(out _)) return;
        switch (plan.Kind)
        {
            case AzureAttackKind.Icicle:
                Cue(AzureCue.FanCharge, plan.Born, plan.Born, at);
                Cue(AzureCue.FanRelease, plan.Fire, plan.Fire, at);
                break;
            case AzureAttackKind.GlassRain:
                Vector2 top = new(s.Field.CenterX, s.Field.Top + 300);
                Cue(AzureCue.RainCharge, plan.Born, plan.Born, top);
                Cue(AzureCue.RainRelease, plan.Fire, plan.Fire, top);
                break;
            case AzureAttackKind.MouthBeam:
                Cue(AzureCue.BeamCharge, plan.Born, plan.Born, at);
                Cue(AzureCue.BeamFire, plan.Born, plan.Fire, at);
                var sweep = Cue(AzureCue.BeamSweep, plan.Born, plan.Fire, projectile.Center);
                if (sweep.IsValid)
                {
                    beam = sweep; beamKey = plan.Born; beamEnd = plan.End; beamCenter = projectile.Center;
                    voices.Follow(sweep, () => beamCenter, AzureAudio.Specs[(int)AzureCue.BeamSweep].Reach, plan.End - plan.Fire);
                }
                // The sweep lives exactly as long as the beam: if the beam goes away first, the sound is faded out.
                if (plan.Born == beamKey && age < plan.End) { beamLive = true; beamCenter = projectile.Center; }
                break;
            case AzureAttackKind.FrostBolt:
                // Born is 24 ticks before Fire: the peak of the sound lands on the volley.
                Cue(AzureCue.MissileVolley, plan.Born, plan.Born);
                break;
            case AzureAttackKind.GlacialCut:
                if (AzureCueRules.IsLatticeLine(plan.Born, plan.Fire))
                {
                    // One volley for the whole lattice, one slice per line at its own position, one closing note.
                    Cue(AzureCue.LatticeVolley, plan.Born, plan.Born + AzureLattice.Warning);
                    Cue(AzureCue.LatticeSlice, plan.Fire, plan.Fire, at);
                    latticeEnd[plan.Born] = Math.Max(latticeEnd.TryGetValue(plan.Born, out int last) ? last : 0, plan.Fire);
                }
                else
                {
                    Cue(AzureCue.CutCharge, plan.Born, plan.Born, at);
                    Cue(AzureCue.CutRelease, plan.Fire, plan.Fire, at);
                }
                break;
        }
    }

    private void Chorus(AzureState s, AzureChorus marker, int age)
    {
        var plan = marker.Plan;
        bool stack = plan.Kind == AzureChorusKind.Stack;
        Vector2 center = new(plan.Center.X, plan.Center.Y);
        if (AzureChorus.TryGirl(plan, out _))
        {
            Cue(stack ? AzureCue.StackCall : AzureCue.SpreadCall, plan.Born, plan.Born, center);
            // Three rising notes before the verdict, each from its own sound file.
            for (int n = 0; n < AzureCueRules.ChorusTickCount; n++)
                Cue(AzureCue.ChorusTick, plan.Born * 4 + n, AzureCueRules.ChorusTickAt(plan.Fire, n), null, ChorusPitch[n], 1, n);
        }
        // The verdict is replicated and may arrive after its scheduled tick: it sounds once, on receipt.
        // One that arrives with less than a second left only has the picture's tail, so no sound.
        if (!marker.Resolved || age < plan.Fire || age >= plan.End - 24 || !clock.Latch(VerdictKey, plan.Born)) return;
        bool held = marker.FailedMask == 0;
        AzureCue cue = stack ? held ? AzureCue.StackHold : AzureCue.StackShatter : held ? AzureCue.SpreadFade : AzureCue.SpreadPierce;
        Vector2? at = center;
        if (!stack && !held)
        {
            int self = Array.FindIndex(s.Members, m => m.Slot == Main.myPlayer);
            if (self >= 0 && (marker.FailedMask >> self & 1) != 0) at = null; // the piercing is at the listener's own feet
            else
                for (int i = 0; i < marker.Positions.Length && i < 8; i++)
                    if ((marker.FailedMask >> i & 1) != 0) { at = new Vector2(marker.Positions[i].X, marker.Positions[i].Y); break; }
        }
        Play(cue, at);
        shake = Math.Max(shake, held ? 3 : 8);
        AzurePackets.Log($"event=ChorusHeard fight={plan.Fight} born={plan.Born} kind={plan.Kind} cue={cue} failed_mask={marker.FailedMask} verdict_delay_ticks={age - plan.Fire} observer={Main.myPlayer}");
    }

    // The worm's dash: a warning that comes in from the entrance side, and a pass that follows the head.
    private void Rush(AzureBoss girl, AzureState s, int age)
    {
        if (!AzureCueRules.RushActive(s, age)) return; // not while Devouring, nor during a Duet staging
        int serial = AzureCueRules.RushSerial(age, s.AttackEpoch);
        int warn = AzureCueRules.RushWarnTick(s.AttackEpoch, serial);
        var f = s.Field;
        Vector2 entrance = new(f.CenterX + AzureCueRules.RushSide(serial) * AzureCueRules.RushEntranceReach, f.CenterY);
        Cue(AzureCue.RushWarn, warn, warn, entrance);
        var pass = Cue(AzureCue.RushPass, warn, warn + AzureRules.ChargeWarning, Head(girl) ?? entrance);
        if (pass.IsValid) FollowHead(pass, AzureCue.RushPass, girl, AzureCueRules.RushFollow);
        Shake(30, warn, warn + AzureRules.ChargeWarning, 6);
    }

    // Down and revive come from the replicated recovery revision, the same path in single player and multiplayer.
    private void Recovery(AzureState s)
    {
        bool live = s.Stage is AzureStage.Countdown or AzureStage.Performance;
        for (int pass = 0; pass < 2; pass++) // the local player first: simultaneous Downs collapse into one cue
            foreach (var m in s.Members)
            {
                bool self = m.Slot == Main.myPlayer;
                if (self != (pass == 0)) continue;
                var r = m.Recovery;
                if (r.Revision == 0) continue;
                if (!recovery.TryGetValue(m.Slot, out var old)) { recovery[m.Slot] = (r.Revision, r.Downed); continue; } // baseline
                if (r.Revision <= old.Revision) continue;
                recovery[m.Slot] = (r.Revision, r.Downed);
                if (m.Out || !live) continue;
                Vector2? at = self ? null : Main.player[m.Slot].Center;
                float scale = self ? 1 : .7f;
                // A Down that was missed (two revisions at once) is neither a Down nor a revive: stay silent.
                if (!old.Downed && r.Downed && clock.Latch((int)AzureCue.Downed, m.Slot * 100000 + (int)r.Revision))
                    Play(AzureCue.Downed, at, 0, scale);
                else if (old.Downed && !r.Downed && clock.Latch((int)AzureCue.Revived, m.Slot * 100000 + (int)r.Revision))
                    Play(AzureCue.Revived, at, 0, scale);
            }
    }

    private static Vector2 Rift(AzureState s) => new(s.Field.CenterX + 1050, s.Field.CenterY - 280);

    private static Vector2? Head(AzureBoss girl)
    {
        var s = girl.State;
        return s.WormSlot >= 0 && s.WormSlot < Main.maxNPCs
            && Main.npc[s.WormSlot] is { active: true, ModNPC: AzureWorm head } npc && head.Fight == s.Fight ? npc.Center : null;
    }

    private void FollowHead(SlotId id, AzureCue cue, AzureBoss girl, int ticks)
        => voices.Follow(id, () => Head(girl), AzureAudio.Specs[(int)cue].Reach, ticks);

    // A cue on its authority tick, once per Fight and identity (see AzureCueClock).
    private SlotId Cue(AzureCue cue, int id, int tick, Vector2? source = null, float pitch = 0, float scale = 1, int variant = 0)
        => clock.Due((int)cue, id, tick, AzureAudio.Specs[(int)cue].Late) ? Play(cue, source, pitch, scale, variant) : SlotId.Invalid;

    private void Shake(int key, int id, int tick, float strength)
    {
        if (clock.Due(ShakeKey + key, id, tick, 8)) shake = Math.Max(shake, strength);
    }

    private SlotId Play(AzureCue cue, Vector2? source = null, float pitch = 0, float scale = 1, int variant = 0, bool track = true)
    {
        if (Main.dedServ || Main.gameMenu) return SlotId.Invalid;
        ref readonly var spec = ref AzureAudio.Specs[(int)cue];
        int now = (int)Main.GameUpdateCount;
        int since = now - lastPlayed[(int)cue]; // negative after the update counter restarts: treated as long ago
        if (spec.MinGap > 0 && since >= 0 && since < spec.MinGap) return SlotId.Invalid;
        // Decoration is dropped first when many voices are alive; a danger cue only at an absurd count.
        if (voices.Count >= (spec.Weight == AzureWeight.Detail ? 20 : 48)) return SlotId.Invalid;
        var style = AzureAudio.Style(cue, variant);
        style.Volume = spec.Volume * scale * AzureAudio.Scale(spec.Weight);
        if (pitch != 0) style.Pitch = pitch;
        Vector2? at = spec.Reach > 0 && source is { } p ? AzureAudio.Anchor(p, spec.Reach) : null;
        var id = SoundEngine.PlaySound(style, at);
        if (!id.IsValid) return id;
        lastPlayed[(int)cue] = now;
        if (track) voices.Add(id, spec.Owner);
        return id;
    }

    // Hit sounds come from a client GlobalNPC (not NPC.HitSound) so they are throttled, positioned and not tied to the Fight clock.
    internal void Hit(AzureCue cue, Vector2 at)
    {
        if (Main.dedServ || Main.gameMenu) return;
        Play(cue, at, track: false); // 0.2s clip: nothing to follow, fade or release
    }

    // World change / unload is the only hard stop. A Fight that ends keeps its tails (see Withdraw).
    private void Clear()
    {
        voices.StopAll();
        clock.Clear();
        Array.Fill(lastPlayed, int.MinValue / 2);
        recovery.Clear(); latticeEnd.Clear();
        projectedFight = Guid.Empty;
        beamKey = -1; beam = SlotId.Invalid; beamLive = lastTerminal = false;
        shake = 0;
    }
    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();
    public override void Unload() => Clear();
    public override void ModifyScreenPosition()
    {
        var girl=AzurePackets.Boss;
        if(!Main.gameMenu && girl is {Fresh:true} && Local(girl) && (girl.State.Stage==AzureStage.Countdown || girl.State.Phase==AzurePhase.Devouring || girl.State.Phase==AzurePhase.Melting))
        {
            float openingAge=RenderAge(girl)-(girl.State.Phase==AzurePhase.Duet?girl.State.MusicStart:girl.State.PhaseAt);
            int duration=girl.State.Phase==AzurePhase.Devouring?AzureRules.Devouring:girl.State.Phase==AzurePhase.Melting?AzureRules.MeltEnding:AzureRules.Intro;
            float frame=AzureRules.Ease(openingAge/55)*AzureRules.Ease((duration-openingAge)/80);
            var center=new Vector2(girl.State.Field.CenterX,girl.State.Field.CenterY-80);
            Main.screenPosition=Vector2.Lerp(Main.screenPosition,center-new Vector2(Main.screenWidth,Main.screenHeight)*.5f,frame);
        }
        if(Reduced || !ModContent.GetInstance<AzureVisualConfig>().ScreenShake || shake<.02f) return;
        float t=Main.GameUpdateCount%6000;
        Main.screenPosition+=new Vector2(MathF.Sin(t*2.11f),MathF.Cos(t*1.87f))*Math.Min(10,shake);
    }
    internal static void Stroke(SpriteBatch batch,Vector2 a,Vector2 b,Color color,float width)
    {
        var delta=b-a;
        batch.Draw(TextureAssets.MagicPixel.Value,a-Main.screenPosition,new Rectangle(0,0,1,1),color,delta.ToRotation(),new(0,.5f),new Vector2(delta.Length(),width),SpriteEffects.None,0);
    }
    public override void PostDrawTiles()
    {
        var girl=AzurePackets.Boss;if(Main.gameMenu || girl is null || !girl.Fresh) return;
        // Capture in the world pass. UI layers may temporarily change screen metrics;
        // neither their UI scale nor a second camera conversion belongs in this mask.
        worldToViewport=Matrix.CreateTranslation(-Main.screenPosition.X,-Main.screenPosition.Y,0)*Main.GameViewMatrix.TransformationMatrix;
        projectedFight=girl.State.Fight;
        var batch=Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,Main.Rasterizer,null,Main.GameViewMatrix.TransformationMatrix);
        try
        {
            float age=RenderAge(girl);AzureEnergy.Begin();
            AzureCeremony.Stage(batch,girl.State,age);
            AzureMaterials.Frost(batch,girl,age);
            // Draw the entire chain from the world pass: an off-screen head
            // must not let Terraria's NPC culling hide the visible body.
            if((girl.State.WormLife>0 || girl.State.Phase==AzurePhase.Melting) && girl.State.WormSlot>=0 && Main.npc[girl.State.WormSlot] is {active:true,ModNPC:AzureWorm head}
                && head.Fight==girl.State.Fight)AzureMaterials.Worm(head,girl,batch,Main.screenPosition);
            foreach(Projectile p in Main.ActiveProjectiles)
            {
                if(p.ModProjectile is AzureChorus marker && marker.Plan.Fight==girl.State.Fight && AzureChorus.TryGirl(marker.Plan,out _))
                    AzureChorusVisuals.Draw(batch,girl,marker,age);
                if(p.ModProjectile is not AzureAttack attack || attack.Plan.Fight!=girl.State.Fight || !attack.TryGirl(out _))continue;
                var h=attack.Plan;bool cut=h.Kind==AzureAttackKind.GlacialCut;
                if(age<h.Born || age>=h.End+(cut?AzureRules.CutResidue:0))continue;
                bool forecast=age<h.Fire;
                if(!attack.Geometry(girl,age,forecast || cut,out var a,out var b,out float radius))continue;
                var dir=(b-a).SafeNormalize(Vector2.UnitY);float length=Vector2.Distance(a,b);
                if(cut)
                {
                    AzureMaterials.Slash(batch,a,b,radius,age,h.Born,h.Fire,h.End);
                    continue;
                }
                if(h.Kind==AzureAttackKind.FrostBolt && forecast)
                {
                    float glow=AzureRules.Ease((age-h.Born)/5)*(1-AzureRules.Ease((age-h.Fire+4)/4));
                    Stroke(batch,a,b,new Color(162,228,255,0)*glow,.8f);
                    AzureCeremony.Bloom(batch,a,35+18*MathF.Sin(age*.18f)*MathF.Sin(age*.18f),.4f*glow);
                }
                else if(h.Kind==AzureAttackKind.FrostBolt)
                    AzureMaterials.EnergyBolt(batch,a,b,radius,age,h.Born*.13f+h.X*.001f);
                else if(h.Kind==AzureAttackKind.MouthBeam || forecast)
                    AzureEnergy.Add(a,dir,length,radius,age,h.Fire,h.End,1,Reduced,h.Born,true,h.Kind==AzureAttackKind.MouthBeam);
                else
                {
                    var glow=MiscTexturesRegistry.BloomCircleSmall.Value;
                    batch.Draw(glow,(a+b)*.5f-Main.screenPosition,null,new Color(82,207,255,0)*.5f,dir.ToRotation(),glow.Size()*.5f,new Vector2(length*1.2f,radius*3)/glow.Size(),SpriteEffects.None,0);
                    AzureMaterials.Shard(batch,a,b,radius,age,1);
                }
                if(!Reduced)
                    for(int i=0;i<(forecast?3:5);i++)
                    {
                        float u=(i*.217f+age*.009f+h.Born*.03f)%1;
                        Vector2 n=new(-dir.Y,dir.X),at=a+(b-a)*u+n*MathF.Sin(i*3.7f+age*.06f)*radius*.6f;
                        var bloom=MiscTexturesRegistry.BloomCircleSmall.Value;
                        batch.Draw(bloom,at-Main.screenPosition,null,new Color(160,231,255,0)*(forecast?.18f:.4f),0,bloom.Size()*.5f,(forecast?5:8)/bloom.Width,SpriteEffects.None,0);
                    }
            }
            // The same condition as the rush sound: no forecast while Devouring or during a Duet staging.
            if(girl.State.WormSlot>=0 && Main.npc[girl.State.WormSlot].ModNPC is AzureWorm worm && worm.Fight==girl.State.Fight
                && AzureCueRules.RushActive(girl.State,(int)age))
            {
                int dash=AzureRules.Clock((int)age,girl.State.AttackEpoch)%AzureRules.ChargeTicks;
                if(dash<AzureRules.ChargeWarning)
                {
                    Vector2 target=new(worm.NPC.ai[0],worm.NPC.ai[1]);var dir=(target-worm.NPC.Center).SafeNormalize(Vector2.UnitX);
                    if(girl.State.Field.ClipAxis(worm.NPC.Center.X,worm.NPC.Center.Y,dir.X,dir.Y,out float first,out float last))
                        AzureEnergy.Add(worm.NPC.Center+dir*Math.Max(0,first),dir,Math.Max(0,last-Math.Max(0,first)),AzureRules.SegmentRadius,
                            age,age+(AzureRules.ChargeWarning-dash),age+(AzureRules.ChargeEnd-dash),.6f,Reduced,age-dash,true);
                }
            }
            AzureEnergy.Draw(batch);
        }
        finally{batch.End();}
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var girl=AzurePackets.Boss;if(Main.gameMenu || girl is null || !girl.Fresh || !Local(girl)) return;
        layers.Insert(0,new LegacyGameInterfaceLayer("Convergence: Azure field",()=>Overlay(girl),InterfaceScaleType.None));
    }
    private bool Overlay(AzureBoss girl)
    {
        var batch=Main.spriteBatch;var s=girl.State;var f=s.Field;float age=RenderAge(girl);var v=Main.instance.GraphicsDevice.Viewport;
        if(projectedFight!=s.Fight) return true;
        Vector2 p0=Vector2.Transform(new(f.Left,f.Top),worldToViewport),p1=Vector2.Transform(new(f.Right,f.Top),worldToViewport);
        Vector2 p2=Vector2.Transform(new(f.Left,f.Bottom),worldToViewport),p3=Vector2.Transform(new(f.Right,f.Bottom),worldToViewport);
        Vector2 a=Vector2.Min(Vector2.Min(p0,p1),Vector2.Min(p2,p3)),b=Vector2.Max(Vector2.Max(p0,p1),Vector2.Max(p2,p3));
        int l=Math.Clamp((int)a.X,0,v.Width),r=Math.Clamp((int)b.X,0,v.Width),t=Math.Clamp((int)a.Y,0,v.Height),bottom=Math.Clamp((int)b.Y,0,v.Height);
        if(s.Phase==AzurePhase.Devouring && AzureRules.Silhouette(age-s.PhaseAt))
        {
            Fill(new(0,0,v.Width,v.Height),Reduced?new Color(87,110,119):new Color(194,224,230));
            if(s.WormSlot>=0 && Main.npc[s.WormSlot] is {active:true,ModNPC:AzureWorm shadow} && shadow.Fight==s.Fight)
                AzureMaterials.Worm(shadow,girl,batch,Vector2.Zero,worldToViewport,true);
            AzureCeremony.Silhouette(batch,girl,worldToViewport,age);
        }
        if(s.Contains(Main.myPlayer) && !Main.LocalPlayer.dead)
        {
            Fill(new(0,0,v.Width,t),Color.Black);Fill(new(0,bottom,v.Width,v.Height-bottom),Color.Black);
            Fill(new(0,t,l,Math.Max(0,bottom-t)),Color.Black);Fill(new(r,t,v.Width-r,Math.Max(0,bottom-t)),Color.Black);
            var ice=new Color(128,215,237);
            if(a.X>=0)Fill(new(l,t,2,Math.Max(0,bottom-t)),ice);
            if(b.X<=v.Width)Fill(new(Math.Max(0,r-2),t,2,Math.Max(0,bottom-t)),ice);
            if(a.Y>=0)Fill(new(l,t,Math.Max(0,r-l),2),ice);
        }
        bool opening=s.Stage==AzureStage.Countdown,ending=s.EndAt>=0,deploy=s.Stage==AzureStage.Deployment,devour=s.Phase==AzurePhase.Devouring;
        if(opening || ending || deploy || devour)
        {
            float clock=ending?age-s.EndAt:devour?age-s.PhaseAt:opening?age-s.MusicStart:age;
            float duration=ending?AzureRules.ExitDuration(s.Stage):devour?AzureRules.Devouring:opening?AzureRules.Intro:AzureRules.Deploy;
            float opacity=AzureRules.Ease(clock/30)*AzureRules.Ease((duration-clock)/45);
            Fill(new(0,0,v.Width,(int)(v.Height*.1f)),Color.Black*opacity);
            Fill(new(0,(int)(v.Height*.9f),v.Width,(int)(v.Height*.11f)),Color.Black*opacity);
            if(opening)
                Utils.DrawBorderString(batch,"CATHEDRAL OF THE WHITE NIGHT",new(v.Width*.5f,v.Height*.84f),new Color(201,239,250)*opacity,.92f,.5f);
            if(ending && s.Stage==AzureStage.Victory)
                Utils.DrawBorderString(batch,"THE GLASS FALLS SILENT",new(v.Width*.5f,v.Height*.84f),new Color(201,239,250)*AzureRules.VictoryTitle(clock),.86f,.5f);
            return false;
        }
        if(s.Stage!=AzureStage.Ready) return true;
        int count=0;foreach(var member in s.Members)if(member.Ready)count++;
        foreach(var m in s.Members)
        {
            var player=Main.player[m.Slot];if(!player.active)continue;
            Vector2 pos=Vector2.Transform(player.Top-new Vector2(0,24),worldToViewport);
            if(m.Ready)Utils.DrawBorderString(batch,"Ready!",pos,Color.LightCyan,.70f,.5f);
            if(m.Slot!=Main.myPlayer)continue;
            // Match Doll's physical-pixel pill. It does not chase a moving player
            // or inherit UI scale; the world-space label above is separate.
            var button=new Rectangle(v.Width/2-100,64,200,36);
            bool hover=button.Contains(Main.mouseX,Main.mouseY);
            Fill(button,hover?new Color(30,63,76):new Color(12,25,36));
            Fill(new(button.X+12,button.Bottom-1,(button.Width-24)*count/s.Members.Length,1),Color.LightCyan);
            Fill(new(button.X+14,button.Y+13,6,6),Color.LightCyan*(m.Ready?1:.25f));
            Utils.DrawBorderString(batch,"READY",new(button.X+30,button.Y+8),Color.LightCyan,.7f);
            Utils.DrawBorderString(batch,$"{count}/{s.Members.Length}",new(button.Right-13,button.Y+8),Color.Silver,.7f,1);
            if(hover){Main.LocalPlayer.mouseInterface=true;if(Main.mouseLeft && Main.mouseLeftRelease){Main.mouseLeftRelease=false;AzurePackets.Ready(!m.Ready);}}
        }
        return true;
        void Fill(Rectangle rect,Color c)=>batch.Draw(TextureAssets.MagicPixel.Value,rect,new Rectangle(0,0,1,1),c);
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class AzureActorVisuals : GlobalNPC
{
    public override bool AppliesToEntity(NPC n,bool lateInstantiation)=>n.ModNPC is AzureBoss or AzureWorm;
    // Not NPC.HitSound: the Fury chain has 45 hittable segments, so hit sounds are throttled, positioned and
    // variant-chosen by AzureVisuals. HitEffect runs on every client for a struck NPC and never on a dedicated server.
    public override void HitEffect(NPC npc,NPC.HitInfo hit)
    {
        if(Main.dedServ || Main.gameMenu)return;
        if(npc.ModNPC is AzureBoss)ModContent.GetInstance<AzureVisuals>().Hit(AzureCue.LioraHit,npc.Center);
        else if(npc.ModNPC is AzureWorm)ModContent.GetInstance<AzureVisuals>().Hit(AzureCue.WormHit,npc.Center);
    }
    public override bool PreDraw(NPC npc,SpriteBatch batch,Vector2 screen,Color drawColor)
    {
        if(npc.ModNPC is AzureWorm)return false;
        if(npc.ModNPC is not AzureBoss g || g.State.Fight==Guid.Empty) return false;
        AzureCeremony.Girl(batch,g,screen);
        return false;
    }
}
[Autoload(Side = ModSide.Client)]
internal sealed class AzureProjectileVisuals : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile p,bool lateInstantiation)=>p.ModProjectile is AzureAttack;
    public override bool PreDraw(Projectile p,ref Color lightColor)=>false;
}
