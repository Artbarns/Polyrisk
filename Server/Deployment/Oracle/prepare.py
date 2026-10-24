#!/usr/bin/env python3
"""Wrap an intact reviewed release bundle for a single Oracle Linux VM."""
import argparse
import hashlib
import json
import pathlib
import shutil


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--bundle", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    bundle = args.bundle.resolve(strict=True)
    output = args.output.resolve()
    if output.exists() or bundle in output.parents:
        parser.error("Choose a new output directory outside the input bundle")
    manifest = json.loads((bundle / "release-manifest.json").read_text())
    if not isinstance(manifest, dict) or not manifest:
        parser.error("Release manifest must be a nonempty object")
    for name, digest in manifest.items():
        path = pathlib.PurePosixPath(name)
        if path.is_absolute() or ".." in path.parts or "\\" in name:
            parser.error("Unsafe manifest path")
        file = bundle / name
        if not file.is_file() or hashlib.sha256(file.read_bytes()).hexdigest() != digest:
            parser.error(f"Release hash mismatch: {name}")
    actual = set()
    for file in bundle.rglob("*"):
        if file.is_symlink():
            parser.error("Symlinks are not accepted in release bundles")
        if file.is_file():
            relative = file.relative_to(bundle)
            # C# tooling can populate these even inside ignored export directories.
            # They are never trusted or copied into the deployment build context.
            if relative.parts[:2] in (("backend", "obj"), ("backend", "bin")):
                continue
            actual.add(relative.as_posix())
    if actual != set(manifest) | {"release-manifest.json"}:
        parser.error("Bundle contains missing or unreviewed files")
    required = {"Dockerfile", "entrypoint.sh", "webgl/index.html", "backend/PolyRisk.LobbyServer.csproj"}
    if not required <= set(manifest):
        parser.error("Bundle is not a complete same-origin release")
    here = pathlib.Path(__file__).resolve().parent
    output.mkdir(parents=True)
    for name in sorted(set(manifest) | {"release-manifest.json"}):
        target = output / "release" / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(bundle / name, target)
    for name in ("compose.yaml", "Caddyfile", ".env.example", "backup.py", "README.md"):
        shutil.copy2(here / name, output / name)
    shutil.copy2(here / "release.dockerignore", output / "release/.dockerignore")
    print(f"Prepared {len(manifest)} verified release files at {output}; not deployed")


if __name__ == "__main__":
    main()
