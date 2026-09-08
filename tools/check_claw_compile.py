"""Compile repo C# against the pinned real tML API, with an isolated Rogue facade.
Not a full Calamity build. Temporary task helper, removed from the final tree.
"""
from pathlib import Path
import hashlib, os, subprocess, urllib.request, zipfile
from xml.sax.saxutils import escape
root=Path(__file__).resolve().parents[1]
temp=Path(os.environ['RUNNER_TEMP'])/'claw-api-check';temp.mkdir(exist_ok=True)
archive=temp/'tml.zip'
url='https://github.com/tModLoader/tModLoader/releases/download/v2026.07.3.0/tModLoader.zip'
urllib.request.urlretrieve(url,archive)
assert hashlib.sha256(archive.read_bytes()).hexdigest()=='6f51610f4b0f167d1620d0e8c83264ba14e047421e97d9f8053eb40e3d764e1a','pinned engine archive hash mismatch'
engine=temp/'engine'
with zipfile.ZipFile(archive) as z:z.extractall(engine)
refs=[]
for name in ['tModLoader.dll','FNA.dll','ReLogic.dll','log4net.dll','Newtonsoft.Json.dll','Steamworks.NET.dll']:
    matches=list(engine.rglob(name))
    if matches:refs.append(f'<Reference Include="{Path(name).stem}"><HintPath>{escape(str(matches[0]))}</HintPath></Reference>')
assert len(refs)>=4,'missing real engine references'
files=[]
for folder in ['Common','Content','Client']:
    files.extend((root/folder).rglob('*.cs'))
files.append(root/'ConvergenceMod.cs')
files=[p for p in files if p.name!='CalamityRogueArmament.cs']
# Exclude only the unchanged adapter requiring the privately installed Calamity binary.
# No changed weapon, drawing, geometry, TrueMelee lookup or Terraria API is stubbed.
(temp/'RogueFacade.cs').write_text('''using Terraria; using Terraria.ModLoader;
namespace Convergence.Common.Compatibility.Calamity {
public abstract class CalamityRogueArmament : ModItem {
protected static DamageClass RogueClass => DamageClass.Melee;
protected static bool HasStealthStrike(Player p)=>false;
protected static void MarkStealthStrike(int i,bool b){} }
internal static class CalamityRogueArmamentDamage {internal static DamageClass Class => DamageClass.Melee;}}
''')
files.append(temp/'RogueFacade.cs')
includes=''.join(f'<Compile Include="{escape(str(p))}" />' for p in files)
project=f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><LangVersion>12.0</LangVersion><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><EnableNETAnalyzers>false</EnableNETAnalyzers></PropertyGroup><ItemGroup>{includes}{''.join(refs)}</ItemGroup></Project>'''
(temp/'ClawApi.csproj').write_text(project)
print('PINNED TML API COMPILE: native Rogue adapter excluded; not a full Calamity/package build',flush=True)
subprocess.run(['dotnet','build',str(temp/'ClawApi.csproj'),'-c','Release','--nologo'],check=True)
