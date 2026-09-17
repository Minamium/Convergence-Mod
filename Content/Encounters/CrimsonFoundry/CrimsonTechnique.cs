using System;
using System.IO;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Stable technique IDs: physical attacks, not alternate beam colours.
internal enum CrimsonTechnique : byte
{
    CrownRain, CrownCinders, CrownCrash,
    MantleFan, MantleRush, MantleScissors,
    ChoirThrust, ChoirHook, ChoirRend,
    VesperaOrbit, VesperaPetals
}

internal readonly record struct CrimsonPoint(float X, float Y)
{
    public static CrimsonPoint operator +(CrimsonPoint a, CrimsonPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static CrimsonPoint operator -(CrimsonPoint a, CrimsonPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static CrimsonPoint operator *(CrimsonPoint a, float b) => new(a.X * b, a.Y * b);
    internal static CrimsonPoint Lerp(CrimsonPoint a, CrimsonPoint b, float t) => a + (b - a) * t;
    internal float LengthSquared => X * X + Y * Y;
    internal bool Finite => float.IsFinite(X) && float.IsFinite(Y);
    internal static CrimsonPoint Polar(float angle, float length) => new(MathF.Cos(angle) * length, MathF.Sin(angle) * length);
}

internal readonly record struct CrimsonStroke(CrimsonPoint A, CrimsonPoint B, float Radius);

// One immutable musical note. Body motion and contact share the same descriptor.
internal readonly record struct CrimsonGesturePlan(
    Guid Fight, short Boss, int Epoch, int Phrase, byte Pulse, byte Source,
    CrimsonTechnique Technique, byte Step, byte Steps, byte Accent,
    int Begin, int Born, int Fire, int End, int FirstFire, int LastEnd,
    CrimsonPoint From, CrimsonPoint Stage, CrimsonPoint Target,
    int GroundX, int GroundY, int Damage)
{
    internal RaidFieldGeometry Field => RaidFieldGeometry.FromGround(GroundX, GroundY);
    internal bool Live(float age) => age >= Fire && age < End;
    internal bool MovesBody => Technique is CrimsonTechnique.CrownCrash or CrimsonTechnique.MantleRush;
    internal float Progress(float age) => Math.Clamp((age - Fire) / Math.Max(1, End - Fire - 1f), 0, 1);
    internal CrimsonPoint Body(float age)
    {
        if (age < FirstFire)
            return CrimsonPoint.Lerp(From, Stage, CrimsonInvocation.Ease((age - Begin) / Math.Max(1, FirstFire - Begin)));
        if (!MovesBody) return Stage;
        return CrimsonPoint.Lerp(Stage, Target, (Step + CrimsonInvocation.Ease(Progress(age))) / Steps);
    }
    internal void Validate()
    {
        if (Fight == Guid.Empty || Boss is < 0 or >= 200 || Epoch < 0 || Phrase is < 1 or > 100000 || Pulse >= 5
            || Source > 3 || !Enum.IsDefined(Technique) || CrimsonTechniqueGeometry.Owner(Technique) != Source
            || Steps is < 1 or > 5 || Step >= Steps || Accent > 2
            || Begin < Epoch || Begin > FirstFire || Born < Epoch || Born > 73000
            || (long)Fire - Born is < 40 or > 180 || (long)End - Fire is < 2 or > 16 || Fire > 73500
            || FirstFire > Fire || LastEnd < End || LastEnd > 73500 || (long)LastEnd - FirstFire > 600
            || !From.Finite || !Stage.Finite || !Target.Finite || From.X is < 0 or > 400000 || From.Y is < 0 or > 150000
            || GroundX is < 1600 or > 400000 || GroundY is < 1440 or > 150000 || Damage is < 1 or > 2000)
            throw new InvalidDataException("crimson.gesture_invalid");
        var f = Field;
        if (Stage.X < f.Left + 100 || Stage.X > f.Right - 100 || Stage.Y < f.Top + 100 || Stage.Y > f.Bottom - 100
            || Target.X < f.Left + 100 || Target.X > f.Right - 100 || Target.Y < f.Top + 100 || Target.Y > f.Bottom - 100)
            throw new InvalidDataException("crimson.gesture_outside_field");
    }
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight.ToByteArray()); w.Write(Boss); w.Write(Epoch); w.Write(Phrase);
        w.Write(Pulse); w.Write(Source); w.Write((byte)Technique); w.Write(Step); w.Write(Steps); w.Write(Accent);
        w.Write(Begin); w.Write(Born); w.Write(Fire); w.Write(End); w.Write(FirstFire); w.Write(LastEnd);
        w.Write(From.X); w.Write(From.Y); w.Write(Stage.X); w.Write(Stage.Y); w.Write(Target.X); w.Write(Target.Y);
        w.Write(GroundX); w.Write(GroundY); w.Write(Damage);
    }
    internal static CrimsonGesturePlan Read(BinaryReader r)
    {
        byte[] bytes = r.ReadBytes(16);
        if (bytes.Length != 16) throw new EndOfStreamException();
        var p = new CrimsonGesturePlan(new Guid(bytes), r.ReadInt16(), r.ReadInt32(), r.ReadInt32(),
            r.ReadByte(), r.ReadByte(), (CrimsonTechnique)r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte(),
            r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(),
            new(r.ReadSingle(), r.ReadSingle()), new(r.ReadSingle(), r.ReadSingle()), new(r.ReadSingle(), r.ReadSingle()),
            r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
        p.Validate(); return p;
    }
}

internal static class CrimsonTechniqueGeometry
{
    internal const int MaximumStrokes = 96;
    internal static int Owner(CrimsonTechnique t) => (int)t < 9 ? (int)t / 3 : 3;
    internal static CrimsonTechnique Select(int source, int serial)
    {
        if (source is < 0 or > 3 || serial < 0) throw new ArgumentOutOfRangeException();
        if (source == 3) return serial % 2 == 0 ? CrimsonTechnique.VesperaOrbit : CrimsonTechnique.VesperaPetals;
        // Two different decks, with no repeated skill at their join.
        int cycle = serial / 3, entry = serial % 3;
        int skill = cycle % 2 == 0 ? entry : (3 - entry) % 3;
        return (CrimsonTechnique)(source * 3 + (skill + source) % 3);
    }
    internal static CrimsonPoint Clamp(RaidFieldGeometry f, CrimsonPoint p, float margin = 180)
        => new(Math.Clamp(p.X, f.Left + margin, f.Right - margin), Math.Clamp(p.Y, f.Top + margin, f.Bottom - margin));
    internal static CrimsonPoint LimitTravel(CrimsonPoint stage, CrimsonPoint target, int steps, int minimumLiveFrames)
    {
        var delta = target - stage; float limit = 40 * steps * Math.Max(1, minimumLiveFrames);
        return delta.LengthSquared <= limit * limit ? target : stage + delta * (limit / MathF.Sqrt(delta.LengthSquared));
    }
    internal static CrimsonPoint Stage(RaidFieldGeometry f, CrimsonPoint focus, CrimsonTechnique technique, int serial)
    {
        float side = serial % 2 == 0 ? -1 : 1;
        return Clamp(f, technique switch
        {
            CrimsonTechnique.CrownRain => new(f.CenterX, f.Top + 210),
            CrimsonTechnique.CrownCinders => focus + new CrimsonPoint(-side * 280, -260),
            CrimsonTechnique.CrownCrash => focus + new CrimsonPoint(-side * 160, -330),
            CrimsonTechnique.MantleFan => focus + new CrimsonPoint(side * 320, -110),
            CrimsonTechnique.MantleRush => focus + new CrimsonPoint(side * 330, -80),
            CrimsonTechnique.MantleScissors => focus + new CrimsonPoint(side * 400, -110),
            CrimsonTechnique.ChoirThrust => focus + new CrimsonPoint(side * 360, -270),
            CrimsonTechnique.ChoirHook => focus + new CrimsonPoint(side * 320, -100),
            CrimsonTechnique.ChoirRend => new(f.CenterX, f.Top + 220),
            _ => focus + new CrimsonPoint(-side * 230, -240)
        });
    }
    internal static CrimsonPoint Target(RaidFieldGeometry f, CrimsonPoint focus, CrimsonTechnique technique, int serial)
    {
        float side = serial % 2 == 0 ? -1 : 1;
        return Clamp(f, technique switch
        {
            CrimsonTechnique.CrownCrash => focus + new CrimsonPoint(side * 120, 200),
            CrimsonTechnique.MantleRush => focus + new CrimsonPoint(-side * 330, 90),
            CrimsonTechnique.ChoirRend => new(f.CenterX, f.CenterY),
            _ => focus
        });
    }
    internal static CrimsonPoint Bezier(CrimsonPoint a, CrimsonPoint c, CrimsonPoint b, float t)
        => a * ((1 - t) * (1 - t)) + c * (2 * t * (1 - t)) + b * (t * t);

    // The same solid capsules are drawn and collided. Forecast is a conservative
    // full swept footprint; only Live(age) can generate damaging geometry.
    internal static int Write(in CrimsonGesturePlan p, float age, Span<CrimsonStroke> destination, bool forecast = false)
    {
        if (!forecast && !p.Live(age)) return 0;
        var w = new Writer(destination); var f = p.Field;
        float t = p.Progress(age), full = forecast ? 1 : t;
        float direction = p.Target.X >= p.Stage.X ? 1 : -1;
        switch (p.Technique)
        {
            case CrimsonTechnique.CrownRain:
                int gap = (p.Phrase + p.Pulse) % 13 + 2;
                for (int i = 0; i < 19; i++)
                {
                    if (i >= gap && i <= gap + 2) continue;
                    float x = f.Left + 100 + i * (f.Right - f.Left - 200) / 18;
                    float y = f.Top + 60 + (f.Bottom - f.Top - 100) * t;
                    float before = f.Top + 60 + (f.Bottom - f.Top - 100) * p.Progress(Math.Max(p.Fire, age - 1));
                    w.Add(new(x, forecast ? f.Top - 4 : before - 62), new(x, forecast ? f.Bottom - 20 : y), 12);
                }
                break;
            case CrimsonTechnique.CrownCinders:
                for (int i = -1; i <= 1; i++)
                {
                    var c = Clamp(f, p.Target + new CrimsonPoint(i * 260, (p.Pulse % 2 == 0 ? -1 : 1) * 90), 125);
                    w.Add(c, c, forecast ? 112 : 24 + 88 * MathF.Sin(t * MathF.PI * .5f));
                }
                break;
            case CrimsonTechnique.CrownCrash:
                if (forecast)
                    w.Add(CrimsonPoint.Lerp(p.Stage, p.Target, p.Step / (float)p.Steps),
                        CrimsonPoint.Lerp(p.Stage, p.Target, (p.Step + 1f) / p.Steps), 158);
                else
                {
                    var body = p.Body(age);
                    w.Add(p.Body(Math.Max(p.Fire, age - 1)), body, 78);
                    w.Ring(body, 85 + t * 58, 13, 20);
                }
                break;
            case CrimsonTechnique.MantleFan:
                float aim = MathF.Atan2(p.Target.Y - p.Stage.Y, p.Target.X - p.Stage.X);
                float start = aim - 1.3f + (forecast ? 0 : t * 1.6f);
                w.Arc(p.Stage, 340, start, forecast ? 2.55f : .95f, forecast ? 38 : 30, 24);
                break;
            case CrimsonTechnique.MantleRush:
                w.Add(forecast ? CrimsonPoint.Lerp(p.Stage, p.Target, p.Step / (float)p.Steps) : p.Body(Math.Max(p.Fire, age - 1)),
                    forecast ? CrimsonPoint.Lerp(p.Stage, p.Target, (p.Step + 1f) / p.Steps) : p.Body(age), 76);
                break;
            case CrimsonTechnique.MantleScissors:
                for (int side = -1; side <= 1; side += 2)
                    w.Curve(p.Stage + new CrimsonPoint(0, side * 90),
                        CrimsonPoint.Lerp(p.Stage, p.Target, .5f) + new CrimsonPoint(0, side * 290),
                        p.Target + new CrimsonPoint(direction * 110, -side * 45), full, forecast ? 29 : 25, 18);
                break;
            case CrimsonTechnique.ChoirThrust:
                for (int limb = -1; limb <= 1; limb++)
                    w.Curve(p.Stage + new CrimsonPoint(limb * 35, 10),
                        CrimsonPoint.Lerp(p.Stage, p.Target, .55f) + new CrimsonPoint(limb * 190, -80),
                        Clamp(f, p.Target + new CrimsonPoint(limb * 145, 50 + p.Pulse * 20)), full, forecast ? 22 : 18, 18);
                break;
            case CrimsonTechnique.ChoirHook:
                var hook = Clamp(f, p.Target + new CrimsonPoint(direction * 130, 100));
                w.Curve(p.Stage, p.Stage + new CrimsonPoint(direction * 500, -260), hook,
                    Math.Min(1, full * 1.65f), forecast ? 28 : 24, 24);
                if (full > .6f)
                    w.Curve(hook, p.Target + new CrimsonPoint(0, -30), p.Target + new CrimsonPoint(-direction * 150, -40),
                        (full - .6f) / .4f, forecast ? 24 : 20, 16);
                break;
            case CrimsonTechnique.ChoirRend:
                for (int cut = 0; cut < 3; cut++)
                {
                    int cell = (p.Phrase * 5 + p.Pulse * 3 + cut * 7) % 15;
                    var center = new CrimsonPoint(f.Left + 330 + (cell % 5) * (f.Right - f.Left - 660) / 4,
                        f.Top + 220 + (cell / 5) * (f.Bottom - f.Top - 440) / 2);
                    float angle = (p.Pulse % 2 == 0 ? .65f : -.8f) + cut * .23f;
                    var axis = CrimsonPoint.Polar(angle, 245);
                    w.Rift(center - axis, center + axis, full, forecast ? 26 : 18, 12);
                }
                break;
            case CrimsonTechnique.VesperaOrbit:
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathF.PI * .5f + p.Pulse * .29f;
                    if (forecast) w.Arc(p.Target, 290, a, 1.25f, 24, 10);
                    else w.Add(p.Target + CrimsonPoint.Polar(a + Math.Max(0, t - .13f) * 1.25f, 290),
                        p.Target + CrimsonPoint.Polar(a + t * 1.25f, 290), 22);
                }
                break;
            case CrimsonTechnique.VesperaPetals:
                for (int i = 0; i < 6; i++)
                {
                    float a = i * MathF.PI / 3 + p.Pulse * .21f;
                    var begin = p.Target + CrimsonPoint.Polar(a, 340);
                    var end = p.Target + CrimsonPoint.Polar(a + .75f, 105);
                    var control = p.Target + CrimsonPoint.Polar(a + .6f, 420);
                    if (forecast) w.Curve(begin, control, end, 1, 30, 12);
                    else w.Add(Bezier(begin, control, end, Math.Max(0, t - .12f)), Bezier(begin, control, end, t), 18);
                }
                break;
        }
        return w.Count;
    }
    internal static bool Intersects(in CrimsonStroke s, float x, float y, float width, float height)
    {
        var a = new CrimsonPoint(x, y); var b = new CrimsonPoint(x + width, y);
        var c = new CrimsonPoint(x + width, y + height); var d = new CrimsonPoint(x, y + height);
        if (Inside(s.A) || Inside(s.B)) return true;
        float r2 = s.Radius * s.Radius;
        return Segments(s.A, s.B, a, b) <= r2 || Segments(s.A, s.B, b, c) <= r2
            || Segments(s.A, s.B, c, d) <= r2 || Segments(s.A, s.B, d, a) <= r2;
        bool Inside(CrimsonPoint point) => point.X >= x && point.X <= x + width && point.Y >= y && point.Y <= y + height;
    }
    private static float Cross(CrimsonPoint a, CrimsonPoint b) => a.X * b.Y - a.Y * b.X;
    private static float PointSegment(CrimsonPoint p, CrimsonPoint a, CrimsonPoint b)
    {
        var v = b - a;
        float t = v.LengthSquared < .00001f ? 0 : Math.Clamp(((p.X - a.X) * v.X + (p.Y - a.Y) * v.Y) / v.LengthSquared, 0, 1);
        return (p - (a + v * t)).LengthSquared;
    }
    private static float Segments(CrimsonPoint a, CrimsonPoint b, CrimsonPoint c, CrimsonPoint d)
    {
        var u = b - a; var v = d - c; float cross = Cross(u, v);
        if (MathF.Abs(cross) > .00001f)
        {
            float t = Cross(c - a, v) / cross, q = Cross(c - a, u) / cross;
            if (t is >= 0 and <= 1 && q is >= 0 and <= 1) return 0;
        }
        return Math.Min(Math.Min(PointSegment(a, c, d), PointSegment(b, c, d)),
            Math.Min(PointSegment(c, a, b), PointSegment(d, a, b)));
    }
    private ref struct Writer
    {
        private Span<CrimsonStroke> output;
        internal int Count { get; private set; }
        internal Writer(Span<CrimsonStroke> output) { this.output = output; Count = 0; }
        internal void Add(CrimsonPoint a, CrimsonPoint b, float radius)
        {
            if (Count >= output.Length) throw new InvalidOperationException("crimson.geometry_capacity");
            output[Count++] = new(a, b, radius);
        }
        internal void Curve(CrimsonPoint a, CrimsonPoint c, CrimsonPoint b, float progress, float radius, int steps)
        {
            var previous = a;
            for (int i = 1; i <= steps; i++)
            {
                float u = i / (float)steps * progress;
                var point = Bezier(a, c, b, u); Add(previous, point, radius * (1 - u * .35f)); previous = point;
            }
        }
        internal void Arc(CrimsonPoint center, float radius, float start, float length, float thickness, int steps)
        {
            var previous = center + CrimsonPoint.Polar(start, radius);
            for (int i = 1; i <= steps; i++)
            {
                var point = center + CrimsonPoint.Polar(start + length * i / steps, radius);
                Add(previous, point, thickness); previous = point;
            }
        }
        internal void Ring(CrimsonPoint center, float radius, float thickness, int steps)
            => Arc(center, radius, 0, MathF.Tau, thickness, steps);
        internal void Rift(CrimsonPoint a, CrimsonPoint b, float progress, float width, int steps)
        {
            var normal = new CrimsonPoint(-(b.Y - a.Y), b.X - a.X);
            normal *= 7 / MathF.Sqrt(Math.Max(1, normal.LengthSquared));
            var previous = a;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps * progress;
                var point = CrimsonPoint.Lerp(a, b, t) + normal * (MathF.Sin(t * MathF.PI * 12) * MathF.Sin(t * MathF.PI));
                Add(previous, point, width); previous = point;
            }
        }
    }
}
