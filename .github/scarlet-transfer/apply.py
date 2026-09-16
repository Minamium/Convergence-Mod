"""Transfer the exact locally verified text edits; removed before verification/commit."""
import base64
import gzip
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import subprocess

assert os.environ['GITHUB_REPOSITORY'] == 'Minamium/Convergence-Mod'
assert os.environ['GITHUB_REF'] == 'refs/heads/feat/scarlet-rhythm-phases'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD^'], text=True).strip() == '65bdb45f3142b1f1d8382dc31fe8a56c88c1e0ae'
root = Path.cwd()
parts = [(root / f'.github/scarlet-transfer/{i}.b64').read_text() for i in range(4)]
# Repair the independently identified three-character transport transcription;
# both preimage and final manifest hashes are checked before any source write.
assert hashlib.sha1(b'blob 8503\0' + parts[1].encode()).hexdigest() == 'c9c3b037c18f41ed0172e004592c61d659b15818'
parts[1] = parts[1].replace('28/++X37n949', '28/++X949', 1)
assert hashlib.sha1(b'blob 8500\0' + parts[1].encode()).hexdigest() == '306fb8afe84c362c9961054be9c03668e31b6c6d'
with gzip.GzipFile(fileobj=io.BytesIO(base64.b64decode(''.join(parts), validate=True))) as stream:
    raw = stream.read(200000)
assert hashlib.sha256(raw).hexdigest() == '314e120260b82f329254decef102cf70c7f073aa57437ce3514269533b52a1cc'
manifest = json.loads(raw)
assert len(manifest) == 29
writes = []
for record in manifest:
    name = PurePosixPath(record['p'])
    assert not name.is_absolute() and '..' not in name.parts and '.git' not in name.parts
    path = root / name
    before = path.read_bytes() if path.exists() else b''
    assert (hashlib.sha256(before).hexdigest() if path.exists() else None) == record['b'], f'Changed preimage: {name}'
    lines = before.decode('utf-8').splitlines(keepends=True)
    for start, end, text in reversed(record['ops']):
        assert 0 <= start <= end <= len(lines)
        lines[start:end] = text.splitlines(keepends=True)
    after = ''.join(lines).encode('utf-8')
    assert hashlib.sha256(after).hexdigest() == record['a'], f'Wrong postimage: {name}'
    writes.append((path, after))
for path, after in writes:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(after)
print(f'Applied {len(writes)} exact hash-verified source/document/test edits.')
