#!/usr/bin/env python3
"""Copy one atomically published lobby snapshot into a private, new gzip archive."""
import datetime
import gzip
import json
import os
import pathlib


def main():
    os.umask(0o077)
    root = pathlib.Path(__file__).resolve().parent
    # The server replaces this file atomically. A read sees one complete version.
    payload = (root / "data/lobbies.json").read_bytes()
    document = json.loads(payload)
    if document.get("schemaVersion") != 1 or not isinstance(document.get("lobbies"), list):
        raise SystemExit("Unsupported snapshot; no backup created")
    backups = root / "backups"
    backups.mkdir(mode=0o700, exist_ok=True)
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    target = backups / f"lobbies-{stamp}.json.gz"
    with target.open("xb") as output:
        output.write(gzip.compress(payload, mtime=0))
    print(f"Private backup saved: {target.name}")


if __name__ == "__main__":
    main()
