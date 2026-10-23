#!/usr/bin/env python3
"""Stage only the verified Unity export and backend sources for a single-service deployment."""
import argparse
import hashlib
import json
import pathlib
import shutil


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--webgl", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    webgl = args.webgl.resolve(strict=True)
    output = args.output.resolve()
    if not (webgl / "index.html").is_file() or not (webgl / "Build").is_dir():
        parser.error("--webgl must be a completed Unity WebGL export")
    if output.exists():
        parser.error("--output must not already exist; existing releases are never overwritten")
    for source in webgl.rglob("*"):
        if source.is_symlink():
            parser.error("Unity export must not contain symlinks")
    here = pathlib.Path(__file__).resolve().parent
    backend = here.parent / "PolyRisk.LobbyServer"
    output.mkdir(parents=True)
    (output / "backend").mkdir()
    (output / "webgl").mkdir()
    for source in sorted(backend.iterdir()):
        if source.is_file() and (source.suffix in (".cs", ".csproj") or source.name == "appsettings.json"):
            shutil.copy2(source, output / "backend" / source.name)
    # Never publish validation galleries, QA session files, local data, source, or credentials.
    for name in ("index.html", "favicon.ico", "Build", "TemplateData", "StreamingAssets"):
        source = webgl / name
        if source.is_dir():
            shutil.copytree(source, output / "webgl" / name)
        elif source.is_file():
            shutil.copy2(source, output / "webgl" / name)
    for name in ("Dockerfile", "entrypoint.sh"):
        shutil.copy2(here / name, output / name)
    manifest = {}
    for path in sorted(output.rglob("*")):
        if path.is_file():
            manifest[str(path.relative_to(output))] = hashlib.sha256(path.read_bytes()).hexdigest()
    (output / "release-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"Staged {len(manifest)} files into {output}")


if __name__ == "__main__":
    main()
