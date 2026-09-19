using ScarletBatchParameters = Convergence.Client.Graphics.WorldBatchParameters;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Convergence.Client.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// Linked only into the temporary API executable; never included in the Mod.
internal static class ScarletApiProbe
{
    public static int Main(string[] args)
    {
        var dlls = Directory.GetFiles(args[0], "*.dll", SearchOption.AllDirectories)
            .GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(g => g.Key!, g => g.First());
        AssemblyLoadContext.Default.Resolving += (context, name) => dlls.TryGetValue(name.Name!, out var path)
            ? context.LoadFromAssemblyPath(Path.GetFullPath(path)) : null;
        Run(); return 0;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var batch = (SpriteBatch)RuntimeHelpers.GetUninitializedObject(typeof(SpriteBatch));
        GC.SuppressFinalize(batch);
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(SpriteBatch).GetField("sortMode", fields)!.SetValue(batch, SpriteSortMode.Immediate);
        Matrix expected = Matrix.CreateTranslation(3, 5, 0);
        typeof(SpriteBatch).GetField("transformMatrix", fields)!.SetValue(batch, expected);
        var snapshot = ScarletBatchParameters.Capture(batch);
        Require(snapshot.Sort == SpriteSortMode.Immediate && snapshot.Transform == expected,
            "FNA UnsafeAccessor fields preserve real caller parameters");
        Console.WriteLine("PASS native FNA SpriteBatch parameter accessor runtime binding");
        for (int species = 0; species < 3; species++)
        {
            var body = new ScarletArticulatedBody();
            for (int tick = 0; tick < 1200; tick++)
            {
                var desired = (ScarletPose)((tick / 20) % Enum.GetValues<ScarletPose>().Length);
                Vector2 center = new(8000 + MathF.Sin(tick * .03f) * 150, 5400 + MathF.Cos(tick * .07f) * 90);
                if (tick > 600) center.X += 600; // reset local chains across a teleport
                body.Update(center, species, desired, tick, .5f + .5f * MathF.Sin(tick * .08f));
                Require(body.Pose == desired, "state follows the projected choreography without recursive transition loops");
                Require(float.IsFinite(body.Motion.X) && float.IsFinite(body.Motion.Y), "finite root motion");
                var chains = (IEnumerable)typeof(ScarletArticulatedBody).GetField("chains", fields)!.GetValue(body)!;
                foreach (IEnumerable chain in chains)
                    foreach (object segment in chain)
                    {
                        var type = segment.GetType();
                        object? value = type.GetField("Position")?.GetValue(segment) ?? type.GetProperty("Position")?.GetValue(segment);
                        var point = (Vector2)value!;
                        Require(float.IsFinite(point.X) && float.IsFinite(point.Y), "finite Verlet segment");
                        Require(Vector2.DistanceSquared(point, center) < 500 * 500, "bounded secondary silhouette");
                    }
            }
        }
        Console.WriteLine("PASS 3600 linked-production articulation/FSM/Verlet updates, including teleports");
        Console.WriteLine("NOT RUN native game load, SpriteBatch drawing, shader appearance, multiplayer");
    }
    private static void Require(bool condition, string name)
    { if (!condition) throw new InvalidOperationException(name); }
}
