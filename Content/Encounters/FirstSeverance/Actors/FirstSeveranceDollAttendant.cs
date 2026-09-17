#nullable enable
using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking.Replication;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Actors;

// A Core-owned conversation actor, never a participant, companion or Boss target.
public sealed class FirstSeveranceDollAttendant : ModNPC
{
    public override string Texture => "Convergence/Assets/Textures/NPCs/DollTheater/DollAttendant";
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = FirstSeveranceDollMannerisms.Frames;
        NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    }
    public override void SetDefaults()
    {
        NPC.width = 22; NPC.height = 46;
        NPC.lifeMax = 100; NPC.damage = 0; NPC.defense = 0;
        NPC.friendly = true; NPC.dontTakeDamage = true;
        NPC.knockBackResist = 0; NPC.aiStyle = -1;
        NPC.noGravity = true; NPC.noTileCollide = true;
        NPC.lavaImmune = true; NPC.netAlways = true; NPC.chaseable = false;
    }
    internal static FoundationCoreTileEntity? FindCore(NPC npc)
    {
        var point = new Point16((int)npc.ai[0], (int)npc.ai[1]);
        return TileEntity.ByPosition.TryGetValue(point, out var entity)
            && entity is FoundationCoreTileEntity core && WorldGen.InWorld(point.X, point.Y)
            && core.IsTileValidForEntity(point.X, point.Y) ? core : null;
    }
    internal static bool OwnsDollPreparation(FoundationCoreTileEntity core)
    {
        var snapshot = Main.netMode == NetmodeID.MultiplayerClient
            ? ModContent.GetInstance<EncounterReplicaSystem>().Snapshot
            : ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot;
        if (snapshot.DefinitionKey != FirstSeveranceIdentity.EncounterKey
            || snapshot.Lifecycle != EncounterLifecycle.Preparing) return false;
        FirstSeverancePreparationProjection? prep;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            prep = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Preparation;
        else FirstSeverancePreparationAuthority.TryCreateProjection(snapshot.EncounterSequence, snapshot.FightId, out prep);
        return prep is not null && prep.FightId == snapshot.FightId && prep.EncounterSequence == snapshot.EncounterSequence
            && Math.Abs(prep.GroundX - core.GroundCenter.X) < .5f && Math.Abs(prep.GroundY - core.GroundCenter.Y) < .5f;
    }
    internal static Vector2 StandingFoot(FoundationCoreTileEntity core)
        => core.GroundCenter - new Vector2(0, core.FootprintWidth == 12 ? 54 : 17);

    public override bool CanChat() => NPC.alpha < 40 && FindCore(NPC) is { ProtectionState: FirstSeveranceCoreProtectionState.Idle };
    public override string GetChat() => Language.GetTextValue("Mods.Convergence.NPCs.FirstSeveranceDollAttendant.Chat");
    public override bool CheckActive() => false;
    public override bool NeedSaving() => false;
    public override void AI()
    {
        var core = FindCore(NPC);
        if (core is null)
        {
            NPC.alpha = 255;
            if (Main.netMode != NetmodeID.MultiplayerClient) Retire(NPC);
            return;
        }
        NPC.velocity = Vector2.Zero;
        NPC.Bottom = StandingFoot(core);
        NPC.direction = NPC.spriteDirection = 1;
        bool idle = core.ProtectionState == FirstSeveranceCoreProtectionState.Idle;
        bool onStage = FirstSeveranceDollActivation.ShowAttendant(core.ProtectionState, OwnsDollPreparation(core));
        NPC.alpha = onStage ? Math.Max(0, NPC.alpha - 12) : 255;
        if (!idle && !Main.dedServ && Main.LocalPlayer.talkNPC == NPC.whoAmI) Main.LocalPlayer.SetTalkNPC(-1);
        if (Main.netMode != NetmodeID.MultiplayerClient && !onStage) Retire(NPC);
    }
    public override void FindFrame(int frameHeight)
    {
        ulong tick = Main.GameUpdateCount + (ulong)(NPC.whoAmI * 31);
        NPC.frame.Y = FirstSeveranceDollMannerisms.Frame(tick,
            !Main.dedServ && Main.LocalPlayer.talkNPC == NPC.whoAmI) * frameHeight;
    }
    public override bool PreDraw(SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (Main.dedServ || NPC.alpha >= 255 || FindCore(NPC) is not { } core || !OwnsDollPreparation(core)) return false;
        float breath = .003f * (float)Math.Sin(Main.GameUpdateCount * .025);
        batch.Draw(TextureAssets.Npc[Type].Value, NPC.Bottom - screenPos,
            new Rectangle(0, NPC.frame.Y, 32, 52), drawColor * (1 - NPC.alpha / 255f),
            0, new Vector2(16, 50), new Vector2(1, 1 + breath), SpriteEffects.None, 0);
        return false;
    }
    internal static void Retire(NPC npc)
    {
        npc.active = false;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }
}

public sealed class FirstSeveranceDollAttendantSystem : ModSystem
{
    internal const int MaximumAttendants = 4;
    internal static bool HasViewer(Vector2 at, float distance)
    {
        foreach (Player player in Main.ActivePlayers)
            if (Vector2.DistanceSquared(player.Center, at) < distance * distance) return true;
        return false;
    }
    public override void PostUpdateNPCs()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount % 60 != 0) return;
        int type = ModContent.NPCType<FirstSeveranceDollAttendant>();
        int count = NPC.CountNPCS(type);
        if (count >= MaximumAttendants) return;
        foreach (TileEntity entity in TileEntity.ByID.Values)
        {
            if (entity is not FoundationCoreTileEntity core
                || !core.IsTileValidForEntity(core.Position.X, core.Position.Y)
                || !FirstSeveranceDollActivation.ShowAttendant(core.ProtectionState, FirstSeveranceDollAttendant.OwnsDollPreparation(core))
                || !HasViewer(core.GroundCenter, 1800)) continue;
            bool exists = false;
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.type == type && npc.ai[0] == core.Position.X && npc.ai[1] == core.Position.Y) { exists = true; break; }
            if (exists) continue;
            Vector2 foot = FirstSeveranceDollAttendant.StandingFoot(core);
            int slot = NPC.NewNPC(new EntitySource_Misc("FirstSeveranceDollAttendant"), (int)foot.X, (int)foot.Y,
                type, ai0: core.Position.X, ai1: core.Position.Y);
            if (slot >= Main.maxNPCs) return;
            Main.npc[slot].netUpdate = true;
            if (++count >= MaximumAttendants) return;
        }
    }
}
