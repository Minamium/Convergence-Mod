using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Convergence.Client.Encounters.FirstSeverance;

public sealed class FirstSeveranceVisualConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(false)]
    public bool ReducedEffects;

    [DefaultValue(true)]
    public bool ScreenShake = true;
}
