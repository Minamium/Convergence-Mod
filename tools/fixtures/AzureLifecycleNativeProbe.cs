// The installed engine executes StrikeNPC/CheckDead and SkyManager Reset. No
// replacement native hit algorithm or world/network playtest is claimed here.
public static class AzureLifecycleNativeProbe
{
    const System.Reflection.BindingFlags I=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
    const System.Reflection.BindingFlags S=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
    static void Require(bool ok,string label){if(!ok)throw new System.Exception(label);}
    public static void Run(System.Reflection.Assembly mod,System.Reflection.Assembly engine,bool old)
    {
        const string content="Convergence.Content.Encounters.AzureCathedral.",client="Convergence.Client.Encounters.AzureCathedral.";
        var main=engine.GetType("Terraria.Main",true);var npcType=engine.GetType("Terraria.NPC",true);
        main.GetField("netMode",S).SetValue(null,2);main.GetField("dedServ",S).SetValue(null,true);
        var npcs=System.Array.CreateInstance(npcType,200);
        for(int i=0;i<200;i++){var n=System.Activator.CreateInstance(npcType);npcType.GetField("whoAmI",I).SetValue(n,i);npcs.SetValue(n,i);}
        main.GetField("npc",S).SetValue(null,npcs);
        void Set(object n,string field,object value)=>npcType.GetField(field,I).SetValue(n,value);
        int Life(object n)=>(int)npcType.GetField("life",I).GetValue(n);
        object Attach(System.Type type,int slot)
        {
            var actor=System.Activator.CreateInstance(type,true);var n=npcs.GetValue(slot);
            type.GetProperty("Entity",I).SetValue(actor,n);npcType.GetProperty("ModNPC",I).SetValue(n,actor);
            Set(n,"active",true);Set(n,"type",1);Set(n,"life",50);Set(n,"lifeMax",240000);Set(n,"realLife",-1);
            return actor;
        }
        var bossType=mod.GetType(content+"AzureBoss",true);var wormType=mod.GetType(content+"AzureWorm",true);
        var boss=Attach(bossType,0);var fight=System.Guid.NewGuid();
        using var stream=new System.IO.MemoryStream();using var w=new System.IO.BinaryWriter(stream);
        w.Write(true);w.Write(fight.ToByteArray());w.Write(7000);w.Write(100);w.Write(1060);w.Write(-1);w.Write((byte)3);
        w.Write(5000);w.Write(5000);w.Write(2400000);w.Write(240000);w.Write(0);w.Write(50);w.Write((short)1);w.Write(true);w.Write((byte)2);w.Write(6500);w.Write((byte)1);
        w.Write((byte)0);w.Write(System.Guid.NewGuid().ToByteArray());w.Write(true);w.Write(false);stream.Position=0;
        using var r=new System.IO.BinaryReader(stream);
        var state=mod.GetType(content+"AzureState",true).GetMethod("ReadEnvelope",S).Invoke(null,new object[]{r});bossType.GetField("State",I).SetValue(boss,state);
        var actors=new object[45];
        for(int i=0;i<45;i++)
        {
            var a=actors[i]=Attach(wormType,i+1);
            wormType.GetField("Fight",I).SetValue(a,fight);wormType.GetField("Girl",I).SetValue(a,(short)0);
            wormType.GetField("Index",I).SetValue(a,(byte)i);wormType.GetField("Head",I).SetValue(a,(short)(i==0?-1:1));wormType.GetField("Previous",I).SetValue(a,(short)(i==0?-1:i));
            Set(npcs.GetValue(i+1),"realLife",i==0?-1:1);
        }
        var hitType=npcType.GetNestedType("HitInfo");var hit=System.Activator.CreateInstance(hitType);
        hitType.GetProperty("Damage").SetValue(hit,20);hitType.GetField("HideCombatText").SetValue(hit,true);
        var strike=npcType.GetMethod("StrikeNPC",new[]{hitType,typeof(bool),typeof(bool)});
        strike.Invoke(npcs.GetValue(3),new[]{hit,(object)false,true});
        Require(Life(npcs.GetValue(1))==30 && Life(npcs.GetValue(3))==30,"native nonlethal segment damage subtracts shared HP exactly once");
        System.Console.WriteLine("PASS native nonlethal segment damage: shared HP 50 -> 30, no heal or duplicate subtraction");
        hitType.GetProperty("Damage").SetValue(hit,500);
        strike.Invoke(npcs.GetValue(3),new[]{hit,(object)false,true});
        Require(Life(npcs.GetValue(1))>0,"native head CheckDead retained the head");
        if(old)
        {
            Require(Life(npcs.GetValue(3))<=0,"old package must reproduce dead struck segment despite living head");
            System.Console.WriteLine("PASS reproduced old native realLife bug: struck segment life<=0 while retained head life>0");
        }
        else
        {
            for(int i=1;i<=45;i++)Require(Life(npcs.GetValue(i))>0,"retained positive body shell "+i);
            // A late native packet for another body after the death callback.
            strike.Invoke(npcs.GetValue(7),new[]{hit,(object)true,true});
            for(int i=1;i<=45;i++)Require(Life(npcs.GetValue(i))>0 && (bool)npcType.GetField("active",I).GetValue(npcs.GetValue(i)),"late packet cannot erase retained chain "+i);
            var other=Attach(wormType,50);wormType.GetField("Fight",I).SetValue(other,System.Guid.NewGuid());Set(npcs.GetValue(50),"life",13);
            wormType.GetMethod("RetainChain",S).Invoke(null,new object[]{fight});
            Require(Life(npcs.GetValue(50))==13 && !(bool)npcType.GetField("immortal",I).GetValue(npcs.GetValue(50)),"retention is exact Fight");
            System.Console.WriteLine("PASS native lethal segment StrikeNPC -> head CheckDead: all45 parts retained, late strike harmless, other Fight untouched");
        }
        var systemType=mod.GetType(client+"AzureSkySystem",true);var system=System.Activator.CreateInstance(systemType,true);
        systemType.GetMethod("Load",I).Invoke(system,null);
        var sky=systemType.GetField("sky",I).GetValue(system);var skyType=sky.GetType();
        var managerType=engine.GetType("Terraria.Graphics.Effects.SkyManager",true);var manager=managerType.GetField("Instance",S).GetValue(null);
        bool Requested()=> (bool)skyType.GetField("requested",I).GetValue(sky);
        if(old)
        {
            systemType.GetField("active",I).SetValue(system,true);skyType.GetField("requested",I).SetValue(sky,true);
            managerType.GetMethod("Reset",I).Invoke(manager,null);
            Require(!Requested() && (bool)systemType.GetField("active",I).GetValue(system),"native reset desynchronizes old cached flag");
            System.Console.WriteLine("PASS reproduced stale sky activation flag after native SkyManager.Reset");
        }
        else
        {
            var sync=systemType.GetMethod("Synchronize",I);
            bool Sync(bool want)=>(bool)sync.Invoke(system,new object[]{want});
            for(int attempt=0;attempt<3;attempt++)
            {
                Require(Sync(true)&&Requested(),"activate/retry "+attempt);Require(!Sync(true),"no repeated activation");
                managerType.GetMethod(attempt%2==0?"Reset":"DeactivateAll",I).Invoke(manager,null);
                Require(!Requested(),"native visual reset/deactivation observed");
            }
            Require(Sync(true),"recover last reset");skyType.GetField("fade",I).SetValue(sky,.7f);
            Require(Sync(false)&&!Requested(),"deactivate with fade tail");Require(Sync(true)&&Requested(),"new ownership during fade tail");
            systemType.GetMethod("ClearWorld",I).Invoke(system,null);Require(!Requested(),"world clear");
            System.Console.WriteLine("PASS actual sky Reset/DeactivateAll/retry/fade-tail/world-clear reconciliation");
        }
    }
}
