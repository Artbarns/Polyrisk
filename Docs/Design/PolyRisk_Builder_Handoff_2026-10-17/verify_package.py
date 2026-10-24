"""Verify exported files against manifest.json; no network or project mutation."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent
manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
failures = []
for item in manifest["files"]:
    path = root / item["path"]
    if not path.is_file():
        failures.append(f"MISSING {item['path']}")
    elif hashlib.sha256(path.read_bytes()).hexdigest() != item["sha256"]:
        failures.append(f"CHANGED {item['path']}")
if failures:
    raise SystemExit("\n".join(failures))
print(f"PASS: {len(manifest['files'])} packaged files match their SHA-256 hashes.")
