using System;
using System.Collections.Generic;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance;

// Terraria-independent *presentation* coordinates. The offline contact sheet uses
// this same rig; none of its joints or cables participates in hit detection.
internal readonly record struct DollSprite(int Cell, Vector2 Position, Vector2 Pivot, Vector2 Scale, float Rotation, int CropHeight = 128);
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
        Vector2 neck = V(0, -83), waist = V(-5, 51);
        Vector2 leftShoulder = Vector2.Lerp(V(-117,-38), V(-77,-35), unfurl), rightShoulder = V(83, -49);
        Vector2 leftElbow = Vector2.Lerp(V(-211,83-pry*15), V(-160,105 - cast*80 + recoil*23),unfurl);
        Vector2 rightElbow = Vector2.Lerp(V(169+pry*15,-98), V(191,-132 - cast*49 + recoil*19),unfurl);
        Vector2 leftWrist = Vector2.Lerp(V(-163-pry*35,232-pry*125), V(-218,225 - cast*167 + recoil*41 + drag),unfurl);
        Vector2 rightWrist = Vector2.Lerp(V(221+pry*40,-110-pry*65), V(249,-264 - cast*49 + recoil*32),unfurl);
        Vector2 leftHip = V(-102,105), rightHip = V(88,124);
        Vector2 leftKnee = V(-127,267), rightKnee = V(105,287);
        Vector2 leftAnkle = V(-153 + drag,426), rightAnkle = V(136 - drag,442);
        // Long hair responds late to the same neck, never a detached floating wig.
        Part(8, neck+V(-49,-19), V(63,18), V(1.48f,2.7f), -.13f + sway*2);
        Part(8, neck+V(49,-10), V(63,18), V(1.13f,2.45f), .14f - sway*3);
        Limb(6,leftHip,leftKnee,.91f); Limb(7,leftKnee,leftAnkle,.92f);
        Limb(6,rightHip,rightKnee,.92f); Limb(7,rightKnee,rightAnkle,.88f);
        Limb(3,leftShoulder,leftElbow,.95f); Limb(4,leftElbow,leftWrist,.98f);
        Limb(3,rightShoulder,rightElbow,1.10f); Limb(4,rightElbow,rightWrist,1.04f);
        Part(2,waist,V(64,13),V(2.18f,2.0f), -.065f + drag*.002f);
        // Side openings reveal the same hip spheres, not detached extra joints.
        Sprites.Add(Sprites[2] with { CropHeight = 49 });
        Sprites.Add(Sprites[4] with { CropHeight = 49 });
        Part(1,V(0,0),V(64,66),V(1.70f,1.60f),0);
        // The eyes are downcast. Head lag opposes the shoulder cable, not a nodding loop.
        Part(0,Vector2.Lerp(V(69,-80),neck+V(4,-5),emergence),V(64,66),new Vector2(1.38f+.62f*emergence),
            .32f-.20f*emergence - sway*1.7f - recoil*.025f);
        Hand(leftWrist,leftElbow,1.45f-.32f*unfurl,-.12f+.21f*unfurl); Hand(rightWrist,rightElbow,1.22f,-.12f);
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
        Vector2 shoulder=V(-117,-38), elbow=V(-211,83+shiver), wrist=V(-163,232-pressure*16);
        Part(8,V(126,-142),V(63,18),V(1.0f,2.4f),-.12f+shiver*.002f);
        Limb(3,shoulder,elbow,1.13f); Limb(4,elbow,wrist,1.17f);
        Hand(wrist,elbow,1.45f,-.12f);
        // The shell occludes the shoulder and hair roots; these endpoints stay fixed.
        Cords.Add(new(Turn(shoulder),-248,-244,8));
        Cords.Add(new(Turn(wrist),-460,-374,19));
    }

    internal void EncasedFace()
    {
        Sprites.Clear(); Cords.Clear(); Tilt=.08f;
        Part(0,V(69,-80),V(64,66),V(1.38f,1.38f),.32f);
    }

    private void Part(int cell,Vector2 at,Vector2 pivot,Vector2 scale,float rotation)
        => Sprites.Add(new(cell,Turn(at),pivot,scale,rotation+Tilt));

    private void Limb(int cell,Vector2 start,Vector2 end,float thickness)
    {
        var (top,bottom)=Anchors(cell);
        Vector2 authored=bottom-top, target=end-start;
        float rotation=MathF.Atan2(target.Y,target.X)-MathF.Atan2(authored.Y,authored.X);
        // Uniform length scaling preserves spherical joints under limb rotation.
        float scale=target.Length()/authored.Length();
        // Width and length remain tied: no stretched oval ball-joints.
        Part(cell,start,top,new Vector2(scale),rotation);
    }

    private void Hand(Vector2 wrist,Vector2 elbow,float scale,float twist)
        => Part(5,wrist,V(69,10),new Vector2(scale),MathF.Atan2(wrist.Y-elbow.Y,wrist.X-elbow.X)-MathF.PI/2+twist);

    internal static (Vector2 Top,Vector2 Bottom) Anchors(int cell) => cell switch
    {
        3 => (V(53,18),V(89,108)),
        4 => (V(64,16),V(78,109)),
        6 => (V(55,21),V(102,100)),
        7 => (V(65,10),V(78,85)),
        _ => throw new ArgumentOutOfRangeException(nameof(cell))
    };
}
