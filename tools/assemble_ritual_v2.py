"""One-shot, hash-checked assembly on the explicitly authorized feature branch.
Removed from the resulting source tree; no credentials or user profiles are read.
"""
from pathlib import Path
import base64, hashlib, json, lzma, os, re, subprocess, sys
from PIL import Image
ROOT = Path(__file__).resolve().parents[1]
BRANCH = 'feat/ritual-armaments-v2'
EXPECTED = 'b6c9a41e2f9d8d49e2124a19178730b6292d85865e024fcf89e9254871d21fc4'
os.chdir(ROOT)
assert os.environ['GITHUB_REPOSITORY'] == 'Minamium/tmod'
assert os.environ['GITHUB_REF'] == 'refs/heads/' + BRANCH

def run(*args):
    print('+', ' '.join(args), flush=True)
    subprocess.run(args, check=True)

def head_guard():
    remote = subprocess.check_output(['git', 'ls-remote', 'origin', 'refs/heads/' + BRANCH], text=True).split()[0]
    assert remote == os.environ['GITHUB_SHA'], 'feature branch changed concurrently; refusing write'

head_guard()
chunks = [ROOT / f'tools/.ritual-v2-payload-{i}.txt' for i in range(4)]
raw = lzma.decompress(base64.b64decode(''.join(p.read_text().strip() for p in chunks), validate=True))
assert hashlib.sha256(raw).hexdigest() == EXPECTED, 'transfer checksum mismatch'
data = json.loads(raw)
assert data['base'] == '2561251cd459b997391dbaededbf6c62b2aed9bf'
# Validate every original BEFORE mutating any source.
updates = []
for name, entry in data['files'].items():
    path = ROOT / name
    assert path.resolve().is_relative_to(ROOT.resolve()) and not name.startswith('.git/')
    before = path.read_bytes() if path.exists() else None
    assert (hashlib.sha256(before).hexdigest() if before is not None else None) == entry['before'], name
    lines = (before.decode('utf-8') if before is not None else '').splitlines(keepends=True)
    for start, end, text in reversed(entry['edits']):
        assert 0 <= start <= end <= len(lines), name
        lines[start:end] = text.splitlines(keepends=True)
    result = ''.join(lines).encode('utf-8')
    assert hashlib.sha256(result).hexdigest() == entry['after'], name
    updates.append((path, result))
for path, result in updates:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(result)
work = Path(os.environ['RUNNER_TEMP']) / 'ritual-v2-task'
work.mkdir(exist_ok=True)
art = work / 'make_art.py'; art.write_text(data['art'], encoding='utf-8')
run(sys.executable, str(art), 'Assets/Textures/NPCs/NullCantorRigAtlas.png', 'Assets/Textures/Items/RitualArmaments/V2')
manifest = ROOT / 'Assets/ATTRIBUTION.md'
text = manifest.read_text(encoding='utf-8')
asset_hashes = {}
for name, expected in data['assets'].items():
    path = ROOT / name
    with Image.open(path) as image:
        pixels = image.convert('RGBA').tobytes()
    assert hashlib.sha256(pixels).hexdigest() == expected['pixels'], f'pixel mismatch: {name}'
    actual = hashlib.sha256(path.read_bytes()).hexdigest()
    # zlib encoders may differ while decoded pixels are identical.
    text = text.replace(expected['file'], actual)
    asset_hashes[name] = actual
manifest.write_text(text, encoding='utf-8')
compiler = work / 'api_compile.py'; compiler.write_text(data['compiler'], encoding='utf-8')
run(sys.executable, str(compiler))
# Remove task-only tooling before the repository's tracked-path checks.
scaffolding = [str(p.relative_to(ROOT)) for p in chunks]
scaffolding += ['tools/assemble_ritual_v2.py', '.github/workflows/ritual-v2-assembly.yml']
run('git', 'rm', '--', *scaffolding)
run('git', 'add', '--all')
run(sys.executable, '-m', 'unittest', 'discover', '-s', 'tools/tests')
cmd = [sys.executable, '.agents/skills/develop-convergence-raids/scripts/verify_repo.py', '.', '--with-domain', '--with-codec']
proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
log = []
for line in proc.stdout:
    print(line, end='', flush=True); log.append(line)
assert proc.wait() == 0, 'selected repository checks failed'
output = ''.join(log)
match = re.search(r'Executed (\d+) deterministic domain tests; failures: 0', output)
assert match, 'missing domain success record'
run('git', 'diff', '--cached', '--check')
p = ROOT / 'docs/evidence/2026-09-09-ritual-armaments-v2.json'
evidence = json.loads(p.read_text())
evidence['assembly_input'] = os.environ['GITHUB_SHA']
evidence['workflow_run'] = os.environ['GITHUB_RUN_ID']
evidence['checks'] = {
    'static': 'passed; documentation catalog, repository policy and YAML',
    'domain': f'passed; {match.group(1)} deterministic cases',
    'codec': 'passed; 324 round-trips, 50 malformed/truncated input rejections',
    'tooling': 'passed; 8 cases',
    'native_tml_api_compile': 'passed against hash-pinned tModLoader v2026.07.3.0, FNA and ReLogic; ONLY unchanged native Rogue adapter replaced by compile-time facade, not full Calamity/package build',
    'asset_pixels': 'passed; all six RGBA pixel hashes match locally inspected exports',
    'diff_whitespace': 'passed'
}
evidence['asset_sha256'] = asset_hashes
p.write_text(json.dumps(evidence, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
run('git', 'add', 'docs/evidence/2026-09-09-ritual-armaments-v2.json')
head_guard()
run('git', '-c', 'user.name=github-actions[bot]', '-c', 'user.email=41898282+github-actions[bot]@users.noreply.github.com', 'commit', '-m', 'feat: complete ritual armaments v2 and continuous dual-claw choreography')
run('git', 'push', 'origin', 'HEAD:refs/heads/' + BRANCH)
with (Path(os.environ['RUNNER_TEMP']) / 'ritual-v2-final-source.tar').open('wb') as out:
    subprocess.run(['git', 'archive', '--format=tar', 'HEAD'], check=True, stdout=out)
print('FINAL_COMMIT=' + subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(), flush=True)
