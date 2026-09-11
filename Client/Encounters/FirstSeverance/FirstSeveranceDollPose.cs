using System;
using System.Collections.Generic;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance;

// Terraria-independent *presentation* coordinates. The offline contact sheet uses
// this same rig; none of its joints or cables participates in hit detection.
internal readonly record struct DollSprite(int Cell, Vector2 Position, Vector2 Pivot, Vector2 Scale, float Rotation, int CropHeight = 128, bool Harness = false);
internal readonly record struct DollCord(Vector2 Attachment, float AnchorX, float AnchorY, float Slack);

internal sealed class FirstSeveranceDollPose
{
    internal readonly List<DollSprite> Sprites = new(24);
    internal readonly List<DollCord> Cords = new(6);
    internal float Tilt { get; private set; }
    private static Vector2 V(float x, float y) => new(x, y);
    private Vector2 Turn(Vector2 p) => Vector2.Transform(p, Matrix3x2.CreateRotation(Tilt));

    internal void Body(float seconds, float cast, float recoil, float unfurl, float emergence, bool reduced, float pry = -1)
    {
        Sprites.Clear(); Cords.Clear();
        float sway = reduced ? 0 : MathF.Sin(seconds * .69f) * .013f + MathF.Sin(seconds * 1.17f) * .004f;
        Tilt = .08f + .295f * unfurl + sway; // Already crooked inside the coffin; no upright reveal.
        float drag = reduced ? 0 : MathF.Sin(seconds * .83f - .9f) * 3;
        if (pry < 0) pry = emergence;
        Vector2 neck = Vector2.Lerp(V(69,-80), V(22,-183), emergence), waist = V(-5, 51);
        Vector2 leftShoulder = Vector2.Lerp(V(-50,-28), V(-77,-35), unfurl), rightShoulder = V(83,-49);
        Vector2 leftElbow = Vector2.Lerp(V(-94-pry*117,54+pry*14), V(-160,105 - cast*80 + recoil*23),unfurl);
        Vector2 rightElbow = Vector2.Lerp(V(169+pry*15,-98), V(191,-132 - cast*49 + recoil*19),unfurl);
        Vector2 leftWrist = Vector2.Lerp(V(-95-pry*103,125-pry*18), V(-218,225 - cast*167 + recoil*41 + drag),unfurl);
        Vector2 rightWrist = Vector2.Lerp(V(221+pry*40,-110-pry*65), V(249,-264 - cast*49 + recoil*32),unfurl);
        // Restore the old elongated, pointed restraint silhouette. The person
        // is caught inside it, not a scaled copy of the complete NPC costume.
        Part(7,waist+V(0,65),V(95,15),V(.60f,.58f+.16f*unfurl),-.04f,true);
        Part(2,leftShoulder+V(-12,14),V(120,40),V(.53f,.56f+.15f*unfurl),.17f+drag*.003f,true);
        Part(2,rightShoulder+V(12,14),V(120,40),V(.51f,.61f+.12f*unfurl),-.22f-drag*.002f,true);
        Part(8,neck+V(-31,-20),V(63,18),V(.93f,2.0f),-.13f+sway*2);
        Part(8,neck+V(31,-10),V(63,18),V(.77f,1.80f),.14f-sway*3);
        Limb(3,leftShoulder,leftElbow,false); Limb(4,leftElbow,leftWrist,false);
        Limb(3,rightShoulder,rightElbow,false); Limb(4,rightElbow,rightWrist,false);
        Part(1,V(0,-25),V(64,66),V(1.35f,1.36f),0);
        Part(2,waist,V(64,13),V(1.55f,1.4f),-.065f+drag*.002f);
        // Ivory frame wraps the dark bodice. Her face is a small captive remnant,
        // partially screened by the old crown rather than a giant NPC portrait.
        Part(0,V(0,0),V(220,260),V(.64f,.64f),0,true);
        Part(0,neck,V(64,66),new Vector2(.68f+.12f*emergence),
            .32f-.20f*emergence - sway*1.7f - recoil*.025f);
        Part(1,neck+V(0,-18),V(222,248),V(.62f,.57f),-.07f,true);
        // One asymmetric plate veils the hair/cheek; do not cover the whole face.
        Part(2,neck+V(-25,-37),V(120,40),V(.13f,.19f),-.24f,true);
        Hand(leftWrist,leftElbow,.65f+.80f*pry-.32f*unfurl,-.12f+.21f*unfurl);
        Hand(rightWrist,rightElbow,1.22f,-.12f);
        Cords.Add(new(Turn(leftShoulder), -460, -374, 6));
        Cords.Add(new(Turn(rightShoulder), 248, -244, 1));
        Cords.Add(new(Turn(leftWrist), -248, -244, 17 - cast*13));
        Cords.Add(new(Turn(rightWrist), 460, -374, 1.5f));
        Cords.Add(new(Turn(waist+V(-54,9)), -460, -374, 23));
        // A remaining porcelain plate is added by the client at the hip. It is
        // deliberately not treated as a new bone or a gameplay attachment.
    }

    internal void Encased(float seconds, float pressure, bool reduced)
    {
        Sprites.Clear(); Cords.Clear();
        Tilt = .08f;
        float shiver = reduced ? 0 : MathF.Sin(seconds*1.17f)*1.8f;
        Vector2 shoulder=V(-50,-28), elbow=V(-94,54+shiver*.35f), wrist=V(-95,125-pressure*3);
        // Mostly behind the existing opaque coffin. Only fingertips and a short
        // hair lock cross its rim; no face portrait is layered over the casing.
        Part(8,V(133,-88),V(63,18),V(.28f,.70f),-.12f+shiver*.002f);
        Limb(3,shoulder,elbow,false); Limb(4,elbow,wrist,false);
        Hand(wrist,elbow,.65f,-.12f);
        // The outer shell owns its suspension; hidden wrist cables need not
        // cross the casing or disclose the whole arm before it opens.
    }

    private void Part(int cell,Vector2 at,Vector2 pivot,Vector2 scale,float rotation,bool harness=false)
        => Sprites.Add(new(cell,Turn(at),pivot,scale,rotation+Tilt,Harness:harness));

    private void Limb(int cell,Vector2 start,Vector2 end,bool harness)
    {
        var (top,bottom)=Anchors(cell,harness);
        Vector2 authored=bottom-top, target=end-start;
        float rotation=MathF.Atan2(target.Y,target.X)-MathF.Atan2(authored.Y,authored.X);
        // Uniform length scaling preserves spherical joints under limb rotation.
        float scale=target.Length()/authored.Length();
        // Width and length remain tied: no stretched oval ball-joints.
        Part(cell,start,top,new Vector2(scale),rotation,harness);
    }

    private void Hand(Vector2 wrist,Vector2 elbow,float scale,float twist,bool harness=false)
        => Part(5,wrist,harness?V(186,40):V(69,10),new Vector2(scale),MathF.Atan2(wrist.Y-elbow.Y,wrist.X-elbow.X)-MathF.PI/2+twist,harness);

    internal static (Vector2 Top,Vector2 Bottom) Anchors(int cell,bool harness=false) => (cell,harness) switch
    {
        (3,true) => (V(85,53),V(73,597)),
        (4,true) => (V(104,52),V(108,571)),
        (3,false) => (V(53,18),V(89,108)),
        (4,false) => (V(64,16),V(78,109)),
        (6,false) => (V(55,21),V(102,100)),
        (7,false) => (V(65,10),V(78,85)),
        _ => throw new ArgumentOutOfRangeException(nameof(cell))
    };

    // Shared UVs keep the offline study and runtime on the same retained atlas.
    internal static (int X,int Y,int Width,int Height) Region(DollSprite part) => !part.Harness
        ? (part.Cell%3*128,part.Cell/3*128,128,part.CropHeight)
        : part.Cell switch
        {
            0 => (0,0,448,657), 1 => (0,675,445,495), 2 => (465,655,241,599),
            3 => (470,0,236,651), 4 => (740,0,205,651), 5 => (953,0,301,655),
            6 => (957,787,297,318), 7 => (745,660,189,585),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
}
