"""Run bounded CPU/lifecycle smoke checks after verify_scarlet_api.py.
This runs linked production helpers with real FNA/Luminance types, not Terraria.
"""
from pathlib import Path
import argparse
import subprocess
from xml.sax.saxutils import escape

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('work_dir', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    work = args.work_dir.resolve()
    project = work/'convergence-build/Convergence.csproj'
    if not project.is_file(): parser.error('Run verify_scarlet_api.py --work-dir first')
    text = project.read_text()
    if 'ScarletApiProbe.cs' not in text:
        text = text.replace('</Project>', '<PropertyGroup><OutputType>Exe</OutputType><StartupObject>ScarletApiProbe</StartupObject></PropertyGroup>'
            + f'<ItemGroup><Compile Include="{escape(str(root / "tools/fixtures/ScarletApiProbe.cs"))}" /></ItemGroup></Project>')
        project.write_text(text)
    subprocess.run(['dotnet','run','--project',str(project),'-c','Release','--',str(work/'tml')],check=True)

if __name__ == '__main__': main()
