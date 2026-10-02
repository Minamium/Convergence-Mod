"""Source-wiring guards for the Scarlet Invocation rewards (docs/encounters/crimson-foundry/REWARDS.md).

Not visual-quality or native gameplay approval; the owner's play checks stay not_run.
"""
from pathlib import Path
import hashlib
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT / 'Content/Encounters/CrimsonFoundry'
REWARDS = CONTENT / 'Rewards'
CLIENT = ROOT / 'Client/Encounters/CrimsonFoundry'
SHADER = ROOT / 'Assets/AutoloadedEffects/Shaders/ScarletInk.fx'
# sha256 of ScarletInk.fx before its reward section (the Raid's Live/Forecast/Residue bodies and the shared Local,
# Segment and Pixel helpers): identical to everything before the technique on main before the rewards (0.3.68).
RAID_SHADER_SHA256 = '1c3e259c35cb0fc5ecdcd07df7dbb2e1f1c43df8283800864465c9004d1c1773'
WEAPONS = {
    'ScarletScythe.cs': 'CrimsonSableScythe',
    'ScarletOrgan.cs': 'CrimsonCanticleOrgan',
    'ScarletBaton.cs': 'CrimsonBaton',
    'ScarletCenser.cs': 'CrimsonEmberCenser',
    'ScarletQuill.cs': 'CrimsonBloodinkQuill',
}


def read(path: Path) -> str:
    return path.read_text(encoding='utf-8')


def block(text: str, name: str) -> str:
    """The body of the hjson block `name: { ... }` (first match), by brace matching."""
    start = re.search(rf'^\s*{re.escape(name)}:\s*\{{', text, re.M)
    assert start, name
    depth, i = 1, start.end()
    while depth:
        depth += {'{': 1, '}': -1}.get(text[i], 0)
        i += 1
    return text[start.end():i - 1]


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
        self.assertIn('recipe.DisableDecraft();', recipe, 'Shimmer must not turn a Covenant back into the five weapons')
        self.assertLess(recipe.index('recipe.DisableDecraft();'), recipe.index('recipe.Register();'))
        self.assertNotIn('CrimsonConductor', recipe)
        self.assertNotIn('ItemID.Silk', recipe)
        self.assertIn('Item.damage = CrimsonRewardRules.CovenantDamage', pact)
        self.assertIn('Item.mana = CrimsonRewardRules.CovenantMana', pact)
        self.assertIn('CrimsonRewardItems.CanAct(owner) ? Targets() : 0', pact)

    def test_one_pool_holds_each_weapon_exactly_once(self):
        items = read(REWARDS / 'CrimsonRewardItems.cs')
        start = items.index('internal static int[] RewardTypes()')
        pool = items[start:items.index('};', start)]
        for kind in ('Melee', 'Ranged', 'Magic', 'Summon', 'Rogue'):
            self.assertEqual(pool.count(f'TypeFor(CrimsonRewardKind.{kind})'), 1, f'{kind} is one of five equal shares')
        self.assertEqual(pool.count('TypeFor('), 5, 'five weapons, 20% each')
        self.assertNotIn('ReliquaryShareChance', read(REWARDS / 'CrimsonRewardRules.cs'))

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
        # The Raid's pass bodies and shared helpers are byte-for-byte what main approved; rewards only append.
        raid = shader[:shader.index('// ---- Reward paths')]
        self.assertEqual(hashlib.sha256(raid.encode('utf-8')).hexdigest(), RAID_SHADER_SHA256,
                         'Live/Forecast/Residue and Local/Segment/Pixel must stay unchanged')
        ink = read(CLIENT / 'Vfx/ScarletRewardInk.cs')
        for name in ('"PathLivePass"', '"PathDormantPass"', '"PathResiduePass"'):
            self.assertIn(name, ink)

    def test_reward_ink_draws_once_beneath_raid_forecasts(self):
        host = read(CLIENT / 'Rewards/ScarletRewardInk.Host.cs')
        self.assertIn('drawnFrame == frame', host)
        self.assertIn('public override void ModifyScreenPosition() => ScarletRewardInk.BeginFrame();', host)
        self.assertIn('public override void PostDrawTiles() => ScarletRewardInk.DrawWorld();', host)
        self.assertIn('[Autoload(Side = ModSide.Client)]', host)
        for name in ('CrimsonGestureVisuals.cs', 'CrimsonChorusVisuals.cs', 'CrimsonVisuals.cs'):
            text = read(CLIENT / name)
            draw = text[text.index('public override void PostDrawTiles()'):]
            self.assertLess(draw.index('ScarletRewardInk.DrawWorld();'), draw.index('batch.Begin('), name)
        # The weapons work in every Raid, and tML runs PostDrawTiles in type-name order (Azure sorts first): every other
        # Raid's forecast drawer draws the friendly ink before it begins, through the neutral seam.
        self.assertIn('FriendlyWorldInk.Draw = ScarletRewardInk.DrawWorld;', host)
        for path in ('AzureCathedral/AzureVisuals.cs', 'EbonManor/EbonVisuals.cs',
                     'FirstSeverance/FirstSeverancePrototypePresentation.cs', 'GhostSamurai/GhostSamuraiBattlefield.cs'):
            text = read(ROOT / 'Client/Encounters' / path)
            body = text[text.index('public override void PostDrawTiles()'):]
            body = body[:body.index('\n    }\n')]
            first = body.split('{', 1)[1].strip().splitlines()[0]
            self.assertIn('FriendlyWorldInk.BeneathForecasts();', first, f'{path}: friendly ink first')

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

    def test_every_sprite_has_its_exported_art_at_the_recorded_size(self):
        # CrimsonRewardSprites records each export's logical size; icons are TexelScale (2) texels per logical pixel,
        # buff icons are centred on 16 x 16 logical (32 x 32 texels), world bodies one texel per logical pixel.
        sprites = read(REWARDS / 'CrimsonRewardSprites.cs')
        attribution = read(ROOT / 'Assets/ATTRIBUTION.md')
        entries = re.findall(r'new\((?:nameof\((\w+)\)|"(\w+)"), "[^"]*", (\d+), (\d+), (\d+), (\d+),[^;]*?(Icon: true)?\);', sprites)
        self.assertEqual(20, len(entries))
        for named, literal, width, height, max_width, max_height, icon in entries:
            name = named or literal
            path = ROOT / 'Assets/Textures/Items/ScarletRewards' / f'{name}.png'
            self.assertTrue(path.is_file(), name)
            header = path.read_bytes()[:24]
            self.assertEqual(b'\x89PNG\r\n\x1a\n', header[:8], name)
            size = (int.from_bytes(header[16:20], 'big'), int.from_bytes(header[20:24], 'big'))
            width, height = int(width), int(height)
            self.assertLessEqual(width, int(max_width), f'{name}: within its limit')
            self.assertLessEqual(height, int(max_height), f'{name}: within its limit')
            expected = (32, 32) if name.endswith('Buff') else (width * 2, height * 2) if icon else (width, height)
            self.assertEqual(expected, size, name)
            digest = hashlib.sha256(path.read_bytes()).hexdigest()
            record = f'- Runtime file: `Assets/Textures/Items/ScarletRewards/{name}.png`'
            self.assertIn(record, attribution, name)
            self.assertIn(f'- SHA256: `{digest}`', attribution.split(record, 1)[1].split('- Runtime file:', 1)[0], f'{name}: attribution hash')

    def test_localization_covers_every_reward_item_buff_and_projectile(self):
        # Exact keys inside their own blocks: a substring check would let 'BloodinkQuill' pass on 'CrimsonBloodinkQuill'.
        items = ['CrimsonScoreReliquary', *WEAPONS.values()]
        buffs = ['CrimsonEmberCenserBuff']
        projectiles = ['SableStroke', 'SableStaff', 'SableRelease', 'StaffCut', 'CanticleShard', 'BoneHand', 'BatonSwing',
                       'BatonStroke', 'BatonRiver', 'EmberCenserMinion', 'BloodinkQuill', 'BloodinkTrail', 'SealedScore']
        for culture in ('en-US', 'ja-JP'):
            text = read(ROOT / f'Localization/CrimsonRewards/{culture}.hjson')
            for group, names in (('Items', items), ('Buffs', buffs)):
                body = block(text, group)
                for name in names:
                    entry = block(body, name)
                    self.assertRegex(entry, r'(?m)^\s*DisplayName:\s*\S', f'{culture}: {group}.{name}.DisplayName')
                    self.assertRegex(entry, r'(?m)^\s*(Tooltip|Description):', f'{culture}: {group}.{name} text')
            body = block(text, 'Projectiles')
            for name in projectiles:
                self.assertRegex(body, rf'(?m)^\s*{name}\.DisplayName:\s*\S', f'{culture}: Projectiles.{name}.DisplayName')
        ja = read(ROOT / 'Localization/CrimsonFoundry/ja-JP.hjson')
        self.assertIn('DisplayName: 緋の契約', ja)
        self.assertNotIn('紅の盟約', ja)
        self.assertIn('up to 20 enemies', read(ROOT / 'Localization/CrimsonFoundry/en-US.hjson'))

    def test_scythe_release_lock_lives_on_the_player_and_engravings_republish(self):
        scythe = read(REWARDS / 'ScarletScythe.cs')
        use = scythe[scythe.index('public override bool CanUseItem'):][:600]
        self.assertIn('state.Releasing', use, 'an item swap must not cut the release lock short')
        player = scythe[scythe.index('public sealed class SableScythePlayer'):]
        post = player[player.index('public override void PostUpdate()'):player.index('public override void UpdateDead()')]
        self.assertNotIn('releaseUntil', post, 'an item change keeps the lock')
        self.assertIn('Released(SableScytheMotion.ReleaseTicks(lines))', scythe)
        staff = scythe[scythe.index('public sealed class SableStaff'):scythe.index('public sealed class SableRelease')]
        self.assertIn('if (resync && Projectile.owner == Main.myPlayer) { resync = false; Projectile.netUpdate = true; }', staff)
        self.assertIn('found.netUpdate = staff.resync = true;', staff)

    def test_root_ledger_covers_every_npc_slot(self):
        rules = read(REWARDS / 'CrimsonRewardRules.cs')
        self.assertIn('internal const int MaxRoots = 200;', rules)
        ledger = rules[rules.index('internal sealed class CrimsonRootLedger'):]
        self.assertNotIn('capacity', ledger)
        self.assertIn('!ValidRoot(root) ||', ledger, 'a root outside the slots fails closed')

    def test_reduced_effects_halves_particles_once_and_live_ink_closes(self):
        pool = read(CLIENT / 'Vfx/ScarletRewardParticles.cs')
        self.assertIn('if (reduced && !Keep(seed, ticket++)) return false;', pool)
        for name, halving in (('ScytheVisuals.cs', 'ReducedCount(10, ScarletRewardFx.Reduced)'),
                              ('QuillVisuals.cs', 'Reduced ? 1 : 2'), ('ScarletReliquaryShow.cs', 'Ticks % 2')):
            self.assertNotIn(halving, read(CLIENT / 'Rewards' / name), f'{name} must not halve its particles again')
        ink = read(CLIENT / 'Vfx/ScarletRewardInk.cs')
        self.assertIn('Close[SampleCount] = ScarletRewardInk.CloseAt(style, time);', ink)
        self.assertIn('style.W = c.Close[sample];', ink)
        # Every emitter whose live ink dries into a scar gives the canvas its live window.
        for name in ('BatonVisuals.cs', 'ScytheVisuals.cs', 'QuillInk.cs', 'CenserVisuals.cs', 'OrganVisuals.cs', 'ScarletReliquaryShow.cs'):
            self.assertRegex(read(CLIENT / 'Rewards' / name), r'with \{ (Window|Remaining) = ', name)

    def test_nominal_parity_table_matches_the_domain_test(self):
        doc = read(ROOT / 'docs/encounters/crimson-foundry/REWARDS.md')
        table = doc[doc.index('## Nominal parity'):doc.index('## Scarlet Score Reliquary')]
        tests = read(ROOT / 'Tests/Convergence.DomainTests/ScarletRewardTests.cs')
        rows = [line for line in table.splitlines() if line.startswith('| ') and not line.startswith('| Class') and '---' not in line]
        self.assertEqual(len(rows), 6, 'five weapons and the Covenant')
        for row in rows:
            cells = [c.strip() for c in row.strip('|').split('|')]
            for figure in (cells[4], cells[5]):
                for number in re.findall(r'\d+', figure.replace('about ', '')):
                    if number in ('per',):
                        continue
                    self.assertRegex(tests, rf'AssertNear\({number}f?,', f'{cells[1]}: {number} is asserted by ScarletRewardTests')


if __name__ == '__main__':
    unittest.main()
