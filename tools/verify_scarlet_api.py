"""Compile the current C# against pinned tML and inspected Luminance source.

This is NOT a native Mod build, installed Calamity compatibility test, or playtest.
The small explicit Calamity API shim is used only in a temporary compile project;
it is neither packaged nor used in the game. All outputs remain outside the repo.
"""
from __future__ import annotations
import argparse
import hashlib
from pathlib import Path
import subprocess
import tempfile
import urllib.request
import zipfile
from xml.sax.saxutils import escape

TML_URL = 'https://github.com/tModLoader/tModLoader/releases/download/v2026.07.3.0/tModLoader.zip'
TML_SHA = '6f51610f4b0f167d1620d0e8c83264ba14e047421e97d9f8053eb40e3d764e1a'
LUM_SHA = 'b2468dfd2f299597602dc6826af781d436c29a57'
SHIM = '''using Terraria; using Terraria.ModLoader;
namespace CalamityMod {
 public sealed class RogueDamageClass : DamageClass { }
 public sealed class ShimPlayer : ModPlayer {
  public bool chaliceOfTheBloodGod; public double chaliceBleedoutBuffer;
  public bool StealthStrikeAvailable() => false;
 }
 public sealed class ShimProjectile : GlobalProjectile { public bool stealthStrike; public override bool InstancePerEntity => true; }
 public static class Extension {
  public static ShimPlayer Calamity(this Player p) => p.GetModPlayer<ShimPlayer>();
  public static ShimProjectile Calamity(this Projectile p) => p.GetGlobalProjectile<ShimProjectile>();
 }
}
namespace CalamityMod.Items.Weapons.Rogue {
 public abstract class RogueWeapon : ModItem {
  public virtual float StealthDamageMultiplier => 1;
  public virtual float StealthKnockbackMultiplier => 1;
  public virtual float StealthVelocityMultiplier => 1;
 }
}'''

def run(*args: str, cwd: Path) -> None:
    subprocess.run(args, cwd=cwd, check=True)

def acquire(url: str, destination: Path, sha: str | None = None) -> None:
    with urllib.request.urlopen(url, timeout=120) as response:
        data = response.read()
    if sha and hashlib.sha256(data).hexdigest() != sha:
        raise RuntimeError('Pinned download digest mismatch')
    destination.write_bytes(data)

def project(targets: Path, items: str, name: str) -> str:
    return f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><LangVersion>12</LangVersion>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<BuildMod>false</BuildMod><OutputTmlReferences>true</OutputTmlReferences><AssemblyName>{name}</AssemblyName>
<GenerateAssemblyInfo>false</GenerateAssemblyInfo><UseAppHost>false</UseAppHost>
</PropertyGroup><Import Project="{escape(str(targets))}" />
<ItemGroup>{items}</ItemGroup></Project>'''

def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--work-dir', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    work = args.work_dir or Path(tempfile.mkdtemp(prefix='scarlet-api-'))
    work.mkdir(parents=True, exist_ok=True)
    tml = work/'tml'; lum = work/'luminance-source'; lp = work/'luminance-build'; cp = work/'convergence-build'
    for p in (tml,lum,lp,cp): p.mkdir(exist_ok=True)
    acquire(TML_URL,work/'tml.zip',TML_SHA)
    acquire(f'https://codeload.github.com/LucilleKarma/Luminance/zip/{LUM_SHA}',work/'lum.zip')
    for path,target in [(work/'tml.zip',tml),(work/'lum.zip',lum)]:
        with zipfile.ZipFile(path) as archive:
            for member in archive.namelist():
                if Path(member).is_absolute() or '..' in Path(member).parts: raise RuntimeError('Unsafe archive entry')
            archive.extractall(target)
    targets = tml/'tMLMod.targets'
    if not targets.exists(): raise RuntimeError('Pinned native tML targets missing')
    sources = lum/f'Luminance-{LUM_SHA}'
    includes = f'<Compile Include="{escape(sources.as_posix())}/**/*.cs" Exclude="{escape(sources.as_posix())}/obj/**;{escape(sources.as_posix())}/bin/**" />'
    (lp/'Luminance.csproj').write_text(project(targets,includes,'Luminance'),encoding='utf-8')
    run('dotnet','build','Luminance.csproj','-c','Release','--nologo',cwd=lp)
    dll = lp/'bin/Release/net8.0/Luminance.dll'
    if not dll.exists():
        matches = list(lp.glob('bin/**/Luminance.dll'))
        if len(matches)!=1: raise RuntimeError('Ambiguous compiled Luminance assembly')
        dll=matches[0]
    (cp/'CalamityApiShim.cs').write_text(SHIM,encoding='utf-8')
    includes = f'<Reference Include="Luminance"><HintPath>{escape(str(dll))}</HintPath></Reference>\n'
    for pattern in ['Common/**/*.cs','Content/**/*.cs','Client/**/*.cs','ConvergenceMod.cs']:
        includes += f'<Compile Include="{escape(root.as_posix()+"/"+pattern)}" />\n'
    includes += '<Compile Include="CalamityApiShim.cs" />'
    (cp/'Convergence.csproj').write_text(project(targets,includes,'Convergence'),encoding='utf-8')
    run('dotnet','build','Convergence.csproj','-c','Release','--nologo',cwd=cp)
    print('PASS: current complete C# binds to pinned tML and inspected Luminance source.')
    print('NOT RUN: real Calamity binding, native packaging/loading, GPU rendering, audio, multiplayer.')

if __name__ == '__main__':
    main()
