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
        self.assertIn('private static bool Audible => !Main.dedServ && !Main.gameMenu && !Main.gamePaused && Main.hasFocus;', audio)
        emit = audio[audio.index('private static void Emit('):]
        self.assertLess(emit.index('if (!Audible) return;'), emit.index('Style(cue, file, remote)'), 'no SoundStyle before the client guard')
        self.assertLess(emit.index('if (!Exists(file)) return;'), emit.index('SoundEngine.PlaySound'), 'a missing cue is skipped')
        self.assertIn('ModContent.HasAsset(ScarletRewardCues.Root + file)', audio)
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


SOUNDS = ROOT / 'Assets/Sounds/Weapons/ScarletRewards'
CUES = CLIENT / 'Rewards/ScarletRewardCues.cs'
AUDIO_RECORD = '### Scarlet Invocation reward weapon audio — 2026-10-03'
# The Raid sound set the reward files' levels were set against (REWARDS.md#art-and-audio): the folder as it stands, the
# measured reference CrownRupture.wav, and the Doll strike and chorus cues at their call volumes, which the Raid plays.
RAID_SOUNDS = ['CrownRupture.wav', 'Foretell.wav', 'ScarletRelease.wav', 'SilkCleave.wav', 'ThornRend.wav']
RAID_REFERENCE_SHA256 = '4d6c7a4e513f462ea66d2ce4e68b4f16206c7a3c3c6961d905dc66976742db9a'
RAID_CALLS = {
    'CrimsonGestureVisuals.cs': ('"Convergence/Assets/Sounds/FirstSeverance/Beams/" + asset', 'Volume = impact ? .72f : .48f,'),
    'CrimsonChorusVisuals.cs': ('"Convergence/Assets/Sounds/FirstSeverance/" + asset', 'Volume = impact ? .48f : .25f,'),
}


def cue_table():
    """ScarletRewardCues.All: name -> (take, audience, voices, lead, seconds, files)."""
    text = read(CUES)
    body = text[text.index('internal static readonly ScarletCue[] All'):]
    body = body[:body.index('};')]
    rows = re.findall(r"new\((?:\"(\w+)\"|(\w+)), '([AB])', (Owner|Shot|Everyone), (\d+), ([\w.]+), ([\d.]+)f(?:, Files: (\d+))?\)", body)
    return {(quoted or named): (take, audience, int(voices), lead, float(seconds), int(files or 1))
            for quoted, named, take, audience, voices, lead, seconds, files in rows}


def cue_files(table):
    """Runtime file stem -> (cue, take)."""
    out = {}
    for name, (take, _, _, _, _, files) in table.items():
        for i in range(files):
            out[name if files == 1 else f'{name}{i + 1}'] = (name, take)
    return out


def ogg_pages(data):
    position = 0
    while position < len(data):
        if data[position:position + 4] != b'OggS':
            raise AssertionError('broken Ogg page layout')
        granule = int.from_bytes(data[position + 6:position + 14], 'little', signed=True)
        serial = int.from_bytes(data[position + 14:position + 18], 'little')
        count = data[position + 26]
        size = sum(data[position + 27:position + 27 + count])
        yield granule, serial, data[position + 27 + count:position + 27 + count + size]
        position += 27 + count + size


def arguments(text):
    """Top-level arguments of a call's argument text."""
    out, depth, start = [], 0, 0
    for i, c in enumerate(text):
        depth += {'(': 1, '[': 1, ')': -1, ']': -1}.get(c, 0)
        if c == ',' and depth == 0:
            out.append(text[start:i].strip())
            start = i + 1
    return out + [text[start:].strip()]


def reward_call_sites():
    """Every ScarletRewardAudio call in the reward presentation: (method, argument text, file)."""
    calls = []
    for path in (CLIENT / 'Rewards').glob('*.cs'):
        if path.name in ('ScarletRewardAudio.cs', 'ScarletRewardCues.cs'):
            continue
        calls += [(m.group(1), m.group(2), path.name) for m in re.finditer(r'ScarletRewardAudio\.(\w+)\(([^;]*)\);', read(path))]
    return calls


class ScarletRewardAudioContract(unittest.TestCase):
    """The shipped cues (REWARDS.md#art-and-audio): the owner's 2026-10-03 picks, wired as the spec says."""

    def test_the_table_holds_the_specs_35_cues_with_the_owners_picks(self):
        table = cue_table()
        self.assertEqual(35, len(table))
        doc = read(ROOT / 'docs/encounters/crimson-foundry/REWARDS.md')
        cues = doc[doc.index('**Cues** (35'):doc.index('**Sources and rendering.**')]
        named = set(re.findall(r'`(\w+)`', cues)) - {'Toll0', 'Toll7', 'OrganShot1', 'OrganShot4'}
        named |= {f'Toll{k}' for k in range(8)}
        self.assertEqual(named, set(table), "the table names exactly the spec's cues")
        picks_b = {'Cadence', 'ReliquaryOpen', 'ScytheWhip', 'StaffWindup', 'StaffBarline', 'RiverRelease'}
        for name, row in table.items():
            self.assertEqual('B' if name in picks_b else 'A', row[0], name)
        self.assertEqual(4, table['OrganShot'][5], 'one OrganShot per pipe')
        self.assertNotIn('using Terraria', read(CUES))
        self.assertNotIn('Microsoft.Xna', read(CUES))
        self.assertIn('Client/Encounters/CrimsonFoundry/Rewards/ScarletRewardCues.cs',
                      read(ROOT / 'Tests/Convergence.DomainTests/Convergence.DomainTests.csproj'), 'the domain tests check the timing')

    def test_every_cue_file_is_the_picked_take_as_rendered(self):
        table = cue_table()
        files = cue_files(table)
        self.assertEqual(38, len(files))
        self.assertEqual(sorted(f'{stem}.ogg' for stem in files), sorted(p.name for p in SOUNDS.iterdir()), 'exactly the shipped cue files')
        for stem, (cue, take) in files.items():
            with self.subTest(file=stem):
                pages = list(ogg_pages((SOUNDS / f'{stem}.ogg').read_bytes()))
                head = pages[0][2]
                self.assertEqual(b'\x01vorbis', head[:7])
                channels, rate = head[11], int.from_bytes(head[12:16], 'little')
                self.assertEqual((2, 48000), (channels, rate))
                self.assertAlmostEqual(table[cue][4], pages[-1][0] / rate, delta=.01, msg='length in the table')
                # The recipe pins each take's Ogg serial from its audition name, so the serial proves which take shipped.
                serial = int.from_bytes(hashlib.sha256(f'scarlet-rewards/{stem}_{take}'.encode()).digest()[:4], 'little')
                self.assertEqual({serial}, {s for _, s, _ in pages}, f'{stem} is take {take}')

    def test_attribution_records_every_file_with_its_hash_and_take(self):
        attribution = read(ROOT / 'Assets/ATTRIBUTION.md')
        record = attribution[attribution.index(AUDIO_RECORD):]
        record = record[:record.index('\n### ', 1)]
        self.assertIn('no cue is held back', record)
        for stem, (cue, take) in cue_files(cue_table()).items():
            entry = record.split(f'- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/{stem}.ogg`', 1)
            self.assertEqual(2, len(entry), f'{stem} has a record')
            entry = entry[1].split('- Runtime file:', 1)[0]
            digest = hashlib.sha256((SOUNDS / f'{stem}.ogg').read_bytes()).hexdigest()
            self.assertIn(f'- SHA256: `{digest}`', entry, stem)
            self.assertIn(f'owner selection of take {take}', entry, stem)
            self.assertIn(f'pinned Ogg serial of take `{stem}_{take}`', entry, stem)
            for key in re.search(r'- Source work and URL: (.*) in the table above', entry).group(1).split(', '):
                self.assertRegex(record, rf'\n\| {re.escape(key)} \| .*\| `[0-9a-f]{{64}}` \| CC0 1\.0', f'{stem}: {key} has a CC0 source row')

    def test_every_cue_is_played_by_the_presentation(self):
        calls = reward_call_sites()
        text = '\n'.join(args for _, args, _ in calls)
        for name, row in cue_table().items():
            if row[1] == 'Owner':
                continue
            if name == 'OrganShot':
                self.assertIn('OrganShot', [method for method, _, _ in calls])
                continue
            self.assertIn(f'ScarletRewardCues.{name}', text, f'{name} is played')
        tolls = {path for method, _, path in calls if method == 'BuildToll'}
        self.assertEqual({'ScytheVisuals.cs', 'OrganVisuals.cs', 'BatonVisuals.cs', 'QuillVisuals.cs'}, tolls, 'each build rings the ladder')
        self.assertIn(('Toll', 'QuillVisuals.cs'), {(method, path) for method, _, path in calls}, 'the Sealed Score plays the melody back')

    def test_audiences_and_gains_follow_the_multiplayer_rules(self):
        table = cue_table()
        for method, args, path in reward_call_sites():
            self.assertNotRegex(args, r'\d*\.\d+f', f'{path}: {method}({args}) passes a literal volume; the files carry the designed levels')
            for name in re.findall(r'ScarletRewardCues\.(\w+)', args):
                if name not in table:
                    continue
                audience = table[name][1]
                expected = {'Shot': 'Shot', 'Everyone': 'Play'}.get(audience)
                self.assertEqual(expected, method, f'{path}: {name} is a {audience} cue')
        audio = read(CLIENT / 'Rewards/ScarletRewardAudio.cs')
        # Other players' voices are a pool of their own, so another player's cue never cuts the local player's.
        self.assertIn('Identifier = Identity + file + (remote ? ":peer" : ""),', audio)
        self.assertIn('MaxInstances = remote ? cue.PeerVoices : cue.Voices,', audio)
        self.assertIn('SoundLimitBehavior = !remote || cue.PeerReplacesOldest ? SoundLimitBehavior.ReplaceOldest : SoundLimitBehavior.IgnoreNew,', audio)
        self.assertIn('internal int PeerVoices => Audience == ScarletCueAudience.Shot ? 1 : Voices;', read(CUES), 'other players share one voice of a per-shot file')
        self.assertIn('internal bool PeerReplacesOldest => Audience == ScarletCueAudience.Shot;', read(CUES))
        self.assertIn('internal static void Play(string cue, int owner, Vector2 at, float decibels = 0) => Emit(table[cue], 0, at, decibels, Remote(owner));', audio)
        self.assertIn('internal static void Toll(int step, int owner, Vector2 at) => Emit(table[ScarletRewardCues.Toll(step)], 0, at, 0, Remote(owner));', audio)
        self.assertIn('private static bool Remote(int owner) => owner != Main.myPlayer;', audio)
        self.assertEqual(1, audio.count('Main.myPlayer'), 'one test of who owns a cue')
        for method, args, path in reward_call_sites():
            if method in ('Play', 'Toll', 'Shot', 'OrganShot', 'BuildToll'):
                owner = arguments(args)[1]
                self.assertRegex(owner, r'^(owner|p\.owner|projectile\.owner|owner\.whoAmI|player\.whoAmI)$', f'{path}: {method}({args}) names the owner second')
        self.assertIn('PauseBehavior = PauseBehavior.StopWhenGamePaused,', audio)
        self.assertIn('PlayOnlyIfFocused = true,', audio)
        self.assertIn('remote ? decibels + CrimsonRewardRules.RemoteShotDecibels : decibels', audio)
        self.assertIn('internal const float Gain = 1f;', read(CUES))

    def test_the_scythe_rings_each_cue_once_through_a_late_sync(self):
        # A peer's stroke age can step back on a late netUpdate (the sync at 12 arriving after the lash at 15): cues cross
        # from the furthest age seen, so the lash never rings twice.
        scythe = read(CLIENT / 'Rewards/ScytheVisuals.cs')
        stroke = scythe[scythe.index('class ScytheStrokeVisuals'):scythe.index('class ScytheReleaseVisuals')]
        self.assertIn('int reached = heard;\n        heard = Math.Max(heard, age);', stroke)
        cues = re.findall(r'Crossed\((\w+), age, ScytheLook\.(\w+)\)', stroke)
        self.assertEqual({'SwingCue', 'WhipBraceCue', 'WhipCue'}, {cue for _, cue in cues})
        self.assertEqual({'reached'}, {start for start, _ in cues})

    def test_reward_levels_are_pinned_to_the_raid_sound_set_they_were_set_against(self):
        # REWARDS.md#art-and-audio: the files' levels were set against these Raid sounds. Scarlet's own sound set
        # (feat/scarlet-sfx) retires CrownRupture.wav and the Doll strike cues and plays its notes far softer, so when the
        # Raid's set changes, re-derive the reward levels role by role against its played cues, have the owner listen
        # again, and then move this pin.
        message = 'the Raid sound set changed: re-derive the Scarlet reward levels (REWARDS.md#art-and-audio)'
        self.assertEqual(RAID_SOUNDS, sorted(p.name for p in (ROOT / 'Assets/Sounds/CrimsonFoundry').iterdir()), message)
        reference = hashlib.sha256((ROOT / 'Assets/Sounds/CrimsonFoundry/CrownRupture.wav').read_bytes()).hexdigest()
        self.assertEqual(RAID_REFERENCE_SHA256, reference, message)
        for name, needles in RAID_CALLS.items():
            for needle in needles:
                self.assertIn(needle, read(CLIENT / name), f'{name}: {message}')
        self.assertIn('measured on `CrownRupture.wav`', read(ROOT / 'docs/encounters/crimson-foundry/REWARDS.md'))

    def test_reward_audio_reuses_no_other_sound_set(self):
        # The spec gives the rewards their own cues; only the Covenant keeps the companion's existing sounds, and its
        # visuals live outside the Rewards folders.
        for path in list(REWARDS.glob('*.cs')) + reward_client_files():
            text = read(path)
            for borrowed in ('Sounds/FirstSeverance', 'Sounds/Weapons/EbonRewards', 'Sounds/Weapons/DollWeapons', 'Sounds/Weapons/DollTheater',
                             'Sounds/CrimsonFoundry', 'EbonRewardAudio', 'DollWeaponAudio', 'SoundID.', 'UseSound'):
                self.assertNotIn(borrowed, text, f'{path.name} borrows {borrowed}')
            for match in re.findall(r'"Convergence/Assets/Sounds/[^"]*"', text):
                self.assertEqual('"Convergence/Assets/Sounds/Weapons/ScarletRewards/"', match, path.name)


if __name__ == '__main__':
    unittest.main()
