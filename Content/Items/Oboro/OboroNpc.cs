using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Convergence.Content.Items.Oboro;

public sealed class OboroNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal ulong Generation;
    internal int FireRemaining, MarkCount;
    public override void SetDefaults(NPC npc)
    {
        Generation = OboroPlayer.Authority ? OboroPackets.NextGeneration() : 0;
        FireRemaining = MarkCount = 0;
    }
    public override void PostAI(NPC npc) { if (FireRemaining > 0) FireRemaining--; }
    private static NPC Root(NPC npc) => npc.realLife >= 0 && npc.realLife < Main.maxNPCs
        && Main.npc[npc.realLife].active ? Main.npc[npc.realLife] : npc;
    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (Root(npc).GetGlobalNPC<OboroNpc>().FireRemaining > 0) modifiers.Defense *= .5f;
    }
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        if (!OboroPlayer.Authority || FireRemaining <= 0 || Root(npc) != npc || npc.friendly || npc.dontTakeDamage) return;
        int dps = OboroRules.FireDps(npc.lifeMax);
        if (npc.lifeRegen > 0) npc.lifeRegen = 0;
        npc.lifeRegen = (int)Math.Max(int.MinValue, (long)npc.lifeRegen - dps * 2L); // Half HP/sec; stack other negative regen.
        damage = Math.Max(damage, Math.Max(1, dps / 10));
    }
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
    { writer.Write(Generation); writer.Write((ushort)FireRemaining); writer.Write((ushort)Math.Clamp(MarkCount, 0, 1275)); }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
    {
        ulong id = reader.ReadUInt64(); int fire = reader.ReadUInt16(), marks = reader.ReadUInt16();
        if (Main.netMode != NetmodeID.MultiplayerClient || id == 0 || fire > OboroRules.FireTicks || marks > 1275) return;
        Generation = id; FireRemaining = fire; MarkCount = marks;
    }
}
