// Reflection against the exact built package and installed Luminance/FNA.
// No Main initialization, graphics device, game, server or saved worlds.
public static class SamuraiRigNativeProbe
{
    const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const string Prefix = "Convergence.Client.Encounters.GhostSamurai.";
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static object Field(object o, string name) => o.GetType().GetField(name, Instance).GetValue(o);
    static float Axis(object v, string axis) => (float)Field(v, axis);
    public static void Run(Assembly mod, AssemblyLoadContext context)
    {
        Assembly lumi = context.LoadFromAssemblyName(new AssemblyName("Luminance"));
        Type presentation = mod.GetType(Prefix + "GhostSamuraiPresentation", true);
        object system = Activator.CreateInstance(presentation, true);
        foreach (string name in new[] { "GhostSamuraiPresentation", "GhostSamuraiRigArt", "GhostSamuraiMist" }) {
            Type t = mod.GetType(Prefix + name, true);
            t.GetMethod("ValidateType", Instance)?.Invoke(Activator.CreateInstance(t, true), null);
        }
        presentation.GetMethod("Load").Invoke(system, null);
        Type motion = mod.GetType(Prefix + "SamuraiRigMotion", true);
        foreach (string curve in new[] { "Cubic", "Out" }) for (int i=0;i<=100;i++) {
            float x=i/100f;
            float expected=curve=="Out" ? 1-MathF.Pow(1-x,3) : x<.5f ? 4*x*x*x : 1-MathF.Pow(-2*x+2,3)/2;
            float actual=(float)motion.GetMethod(curve, Static).Invoke(null,new object[]{x});
            Require(Math.Abs(actual-expected)<.00001f,"Installed Luminance curve mismatch");
        }
        Console.WriteLine("PASS installed Luminance easing binding: 202 samples and client type validation");

        Type segment = lumi.GetType("Luminance.Common.VerletIntergration.VerletSegment",true);
        Type vector = segment.GetField("Position").FieldType;
        object V(float x,float y) => Activator.CreateInstance(vector,new object[]{x,y});
        Type blade=mod.GetType(Prefix+"SamuraiBladeMotion",true), pose=mod.GetType(Prefix+"SamuraiRigPose",true);
        object B(float angle) => Activator.CreateInstance(blade,new object[]{angle,1f,0f,0f});
        Type secondary=mod.GetType(Prefix+"GhostSamuraiSecondaryMotion",true);
        object rig=Activator.CreateInstance(secondary,true);
        float lastX=8000;
        for(int tick=0;tick<1200;tick++) {
            float x=8000+MathF.Sin(tick*.03f)*150+(tick>=600?600:0), y=5400+MathF.Cos(tick*.07f)*90;
            object p=Activator.CreateInstance(pose,new object[]{x,y,(float)tick,.1f,1f,B(2.16f),B(.98f),tick%10/10f,80f,.08f});
            secondary.GetMethod("Update",Instance).Invoke(rig,new[]{p,V(x-lastX,0)}); lastX=x;
            var chains=(System.Collections.IEnumerable)secondary.GetField("chains",Instance).GetValue(rig);
            int count=0;
            foreach(System.Collections.IEnumerable chain in chains) {
                object root=null;
                foreach(object s in chain) {
                    object point=Field(s,"Position"); root??=point;
                    float dx=Axis(point,"X")-Axis(root,"X"),dy=Axis(point,"Y")-Axis(root,"Y");
                    Require(float.IsFinite(dx)&&float.IsFinite(dy)&&dx*dx+dy*dy<=1600.1f,"Unbounded Verlet chain"); count++;
                }
            }
            Require(count==20,"Unexpected particle/chain growth");
        }
        secondary.GetMethod("Clear",Instance).Invoke(rig,null);
        secondary.GetMethod("Clear",Instance).Invoke(rig,null);
        Console.WriteLine("PASS 1200 installed Luminance Verlet ticks, 20-node bound, teleport and repeated clear");

        Type mist=mod.GetType(Prefix+"GhostSamuraiMist",true);
        object mistOwner=Activator.CreateInstance(mist,true);
        Type particle=lumi.GetType("Luminance.Core.Graphics.MetaballInstance",true);
        object Particle() => Activator.CreateInstance(particle,new object[]{V(20,30),V(0,0),40f,0f,60f,2f,-1f});
        object low=Particle(), high=Particle();
        for(int tick=0;tick<45;tick++) {
            foreach(object p in new[]{low,high}) mist.GetMethod("StepParticle",Static).Invoke(null,new[]{p});
            for(int draw=0;draw<8;draw++) {
                mist.GetMethod("UpdateParticle").Invoke(mistOwner,new[]{high});
                object velocity=Field(high,"Velocity");
                Require(Axis(velocity,"X")==0&&Axis(velocity,"Y")==0,"Render callback moves a particle");
            }
            Require(Field(low,"Center").Equals(Field(high,"Center"))&&Field(low,"Size").Equals(Field(high,"Size")),"Mist speed depends on FPS");
            Require(((float[])Field(high,"ExtraInfo"))[0]==tick+1,"Draw callback advances lifetime");
        }
        Console.WriteLine("PASS exact-package mist simulation: 1 vs 8 draw callbacks per tick, identical movement/lifetime");

        Type batch=vector.Assembly.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch",true);
        object uninitialized=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(batch);
        GC.SuppressFinalize(uninitialized);
        FieldInfo sort=batch.GetField("sortMode",Instance), matrix=batch.GetField("transformMatrix",Instance);
        object immediate=Enum.Parse(sort.FieldType,"Immediate"), transform=matrix.FieldType.GetMethod("CreateTranslation",new[]{typeof(float),typeof(float),typeof(float)}).Invoke(null,new object[]{3f,5f,0f});
        sort.SetValue(uninitialized,immediate); matrix.SetValue(uninitialized,transform);
        Type parameters=mod.GetType("Convergence.Client.Graphics.WorldBatchParameters",true);
        object saved=parameters.GetMethod("Capture",Static).Invoke(null,new[]{uninitialized});
        Require(parameters.GetProperty("Sort").GetValue(saved).Equals(immediate)&&parameters.GetProperty("Transform").GetValue(saved).Equals(transform),"FNA caller-state capture failed");
        Console.WriteLine("PASS installed FNA private-field accessors preserve caller sort/matrix");
        foreach(string hook in new[]{"ClearWorld","OnWorldUnload","OnWorldUnload","Unload"}) presentation.GetMethod(hook).Invoke(system,null);
        Console.WriteLine("PASS uninitialized-player/Mod state and repeated client teardown");
    }
}
