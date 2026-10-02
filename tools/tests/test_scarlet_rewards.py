"""Source-wiring guards for the Scarlet Invocation rewards (docs/encounters/crimson-foundry/REWARDS.md).

Not visual-quality or native gameplay approval; the owner's play checks stay not_run.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT / 'Content/Encounters/CrimsonFoundry'
REWARDS = CONTENT / 'Rewards'
CLIENT = ROOT / 'Client/Encounters/CrimsonFoundry'
SHADER = ROOT / 'Assets/AutoloadedEffects/Shaders/ScarletInk.fx'
WEAPONS = {
    'ScarletScythe.cs': 'CrimsonSableScythe',
    'ScarletOrgan.cs': 'CrimsonCanticleOrgan',
    'ScarletBaton.cs': 'CrimsonBaton',
    'ScarletCenser.cs': 'CrimsonEmberCenser',
    'ScarletQuill.cs': 'CrimsonBloodinkQuill',
}


def read(path: Path) -> str:
    return path.read_text(encoding='utf-8')


def reward_client_files():
    files = list((CLIENT / 'Rewards').glob('*.cs'))
    files += [CLIENT / 'Vfx' / name for name in ('ScarletRewardInk.cs', 'ScarletRewardParticles.cs', 'ScarletSpriteBurn.cs')]
    return files


class ScarletRewardWiring(unittest.TestCase):
    def test_content_never_references_client_code(self):
        for path in REWARDS.glob('*.cs'):
            self.assertNotIn('Convergence.Client', read(path), f'{path.name} must not reference Client code')

    def test_rule_files_stay_terraria_free_and_independent_of_ebon(self):
        rules = ['CrimsonRewardRules.cs', 'CrimsonStrokeState.cs', 'SableScytheMotion.cs', 'CanticleRules.cs',
                 'BatonRules.cs', 'CenserRules.cs', 'QuillRules.cs']
        project = read(ROOT / 'Tests/Convergence.DomainTests/Convergence.DomainTests.csproj')
        for name in rules:
            text = read(REWARDS / name)
            for forbidden in ('using Terraria', 'using Microsoft.Xna', 'EbonRewardRules', 'ModContent.'):
                self.assertNotIn(forbidden, text, f'{name} must stay pure ({forbidden})')
            self.assertIn(f'Content/Encounters/CrimsonFoundry/Rewards/{name}', project, f'{name} is linked into the domain tests')

    def test_every_weapon_gates_use_on_the_shared_usable_rule(self):
        items = read(REWARDS / 'CrimsonRewardItems.cs')
        for player in ('CrimsonRecoveryPlayer', 'AzureRecoveryPlayer', 'EbonRecoveryPlayer', 'FirstSeveranceRaidPlayer'):
            self.assertIn(f'GetModPlayer<{player}>().IsIncapacitated', items)
        self.assertIn('player.active && !player.dead', items)
        for file, cls in WEAPONS.items():
            text = read(REWARDS / file)
            self.assertRegex(text, rf'public sealed class {cls} : (ModItem|CalamityRogueArmament)')
            head = text[text.index('override bool CanUseItem'):][:400]
            self.assertRegex(head, r'CrimsonRewardItems\.(Usable|CanAct)\(player\)', f'{cls} gates CanUseItem first')
            self.assertIn('CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.', text)
        self.assertIn('CrimsonRewardItems.Usable(player)', read(CONTENT / 'CrimsonCompanion.cs'))

    def test_damage_classes_go_through_the_calamity_adapters(self):
        items = read(REWARDS / 'CrimsonRewardItems.cs')
        self.assertIn('CrimsonRewardKind.Melee => CalamityTrueMelee.Damage', items)
        self.assertIn('_ => CalamityRogueArmamentDamage.Class', items)
        self.assertIn(': CalamityRogueArmament', read(REWARDS / 'ScarletQuill.cs'))
        for path in list(REWARDS.glob('*.cs')) + reward_client_files():
            self.assertNotIn('using CalamityMod', read(path), f'{path.name}: Calamity access stays in Common/Compatibility/Calamity')

    def test_victory_drop_is_victory_only_counted_before_each_grant(self):
        runtime = read(CONTENT / 'CrimsonRuntime.cs')
        cleanup = runtime[runtime.index('public void Cleanup'):]
        self.assertLess(cleanup.index('if (context.EndReason == EncounterEndReason.Victory) DropRewards();'),
                        cleanup.index('ClearHazards();'))
        self.assertEqual(runtime.count('DropRewards()'), 1, 'one call site')
        drop = read(REWARDS / 'CrimsonRuntime.Rewards.cs')
        self.assertIn('int count = members.Length;', drop)
        self.assertIn('int index = rewardsAttempted++;', drop)
        self.assertLess(drop.index('rewardsAttempted++'), drop.index('Item.NewItem'))
        for event in ('event=VictoryRewardDropped', 'event=VictoryRewardFailed', 'reason=item_limit'):
            self.assertIn(event, drop)
        self.assertNotIn('ModPacket', drop)

    def test_reliquary_is_an_ordinary_box_and_one_pool_feeds_box_and_covenant(self):
        box = read(REWARDS / 'CrimsonScoreReliquary.cs')
        self.assertIn('ItemDropRule.OneFromOptionsNotScalingWithLuck(1, CrimsonRewardItems.RewardTypes())', box)
        self.assertNotIn('BossBag', box.replace('ItemID.Sets.BossBag (that flag', ''))
        self.assertIn('if (!Main.dedServ && player.whoAmI == Main.myPlayer) Opened?.Invoke(player);', box)
        pact = read(CONTENT / 'CrimsonCompanion.cs')
        recipe = pact[pact.index('public override void AddRecipes()'):pact.index('public sealed class CrimsonPactBuff')]
        self.assertIn('CrimsonRewardItems.RewardTypes()', recipe)
        self.assertIn('TileID.Bookcases', recipe)
        self.assertNotIn('CrimsonConductor', recipe)
        self.assertNotIn('ItemID.Silk', recipe)
        self.assertIn('Item.damage = CrimsonRewardRules.CovenantDamage', pact)
        self.assertIn('Item.mana = CrimsonRewardRules.CovenantMana', pact)
        self.assertIn('CrimsonRewardItems.CanAct(owner) ? Targets() : 0', pact)

    def test_rewards_never_use_the_forecast_pass_and_existing_passes_are_unchanged(self):
        for path in reward_client_files() + list(REWARDS.glob('*.cs')):
            self.assertNotIn('ForecastPass', read(path), f'{path.name} must not use the rejected forecast hairline')
        shader = read(SHADER)
        technique = shader[shader.index('technique ScarletInk'):]
        self.assertTrue(technique.startswith(
            'technique ScarletInk {\n'
            ' pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Live(); }\n'
            ' pass ForecastPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Forecast(); }\n'
            ' pass ResiduePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Residue(); }\n'))
        for name in ('PathLivePass', 'PathDormantPass', 'PathResiduePass', 'SpriteBurnPass'):
            self.assertIn(f'pass {name} ', technique)
        # The reward passes have no periodic term: no sin/cos of the clock.
        rewards = shader[shader.index('// ---- Reward paths'):shader.index('technique ScarletInk')]
        self.assertIsNone(re.search(r'\b(sin|cos)\s*\(', rewards))
        ink = read(CLIENT / 'Vfx/ScarletRewardInk.cs')
        for name in ('"PathLivePass"', '"PathDormantPass"', '"PathResiduePass"'):
            self.assertIn(name, ink)

    def test_reward_ink_draws_once_beneath_raid_forecasts(self):
        host = read(CLIENT / 'Rewards/ScarletRewardInk.Host.cs')
        self.assertIn('drawnFrame == frame', host)
        self.assertIn('public override void ModifyScreenPosition() => ScarletRewardInk.BeginFrame();', host)
        self.assertIn('public override void PostDrawTiles() => ScarletRewardInk.DrawWorld();', host)
        self.assertIn('[Autoload(Side = ModSide.Client)]', host)
        for name in ('CrimsonGestureVisuals.cs', 'CrimsonChorusVisuals.cs'):
            text = read(CLIENT / name)
            draw = text[text.index('public override void PostDrawTiles()'):]
            self.assertLess(draw.index('ScarletRewardInk.DrawWorld();'), draw.index('batch.Begin('), name)

    def test_dedicated_server_paths_never_touch_graphics_or_audio(self):
        for path in (CLIENT / 'Rewards').glob('*.cs'):
            text = read(path)
            if ': ModSystem' in text:
                self.assertIn('[Autoload(Side = ModSide.Client)]', text, f'{path.name} systems are client-only')
        audio = read(CLIENT / 'Rewards/ScarletRewardAudio.cs')
        self.assertIn('if (Main.dedServ || Main.gameMenu || Main.gamePaused || !Main.hasFocus || !Exists(cue)) return;', audio)
        self.assertIn('ModContent.HasAsset(Root + cue)', audio)
        art = read(CLIENT / 'Rewards/ScarletRewardArt.cs')
        self.assertIn('AssetRequestMode.ImmediateLoad', art)
        self.assertNotIn('AsyncLoad', art.replace('a cached AsyncLoad .Value', '').replace('AsyncLoad request', ''))

    def test_placeholders_live_in_one_table(self):
        sprites = read(REWARDS / 'CrimsonRewardSprites.cs')
        for placeholder in ('ItemID.CrimsonFishingCrate', 'ItemID.DeathSickle', 'ItemID.OnyxBlaster', 'ItemID.CrimsonRod',
                            'ItemID.ImpStaff', 'ItemID.BoneJavelin'):
            self.assertIn(placeholder, sprites)
        for path in list(REWARDS.glob('*.cs')) + reward_client_files():
            if path.name == 'CrimsonRewardSprites.cs':
                continue
            self.assertNotIn('Terraria/Images/', read(path), f'{path.name}: placeholders resolve through CrimsonRewardSprites')

    def test_localization_covers_every_reward_item_buff_and_projectile(self):
        names = ['CrimsonScoreReliquary', *WEAPONS.values(), 'CrimsonEmberCenserBuff', 'SableStroke', 'SableStaff',
                 'SableRelease', 'StaffCut', 'CanticleShard', 'BoneHand', 'BatonSwing', 'BatonStroke', 'BatonRiver',
                 'EmberCenserMinion', 'BloodinkQuill', 'BloodinkTrail', 'SealedScore']
        for culture in ('en-US', 'ja-JP'):
            text = read(ROOT / f'Localization/CrimsonRewards/{culture}.hjson')
            for name in names:
                self.assertIn(name, text, f'{culture}: {name}')
        ja = read(ROOT / 'Localization/CrimsonFoundry/ja-JP.hjson')
        self.assertIn('DisplayName: 緋の契約', ja)
        self.assertNotIn('紅の盟約', ja)
        self.assertIn('up to 20 enemies', read(ROOT / 'Localization/CrimsonFoundry/en-US.hjson'))


if __name__ == '__main__':
    unittest.main()
