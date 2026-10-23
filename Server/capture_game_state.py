#!/usr/bin/env python3
"""Export the public gameplay snapshot of one QA lobby, excluding credentials."""
import argparse
import json
import pathlib


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--storage", required=True, type=pathlib.Path)
    parser.add_argument("--code", required=True)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    storage = json.loads(args.storage.read_text())
    lobby = next((item for item in storage.get("lobbies", [])
                  if item.get("code") == args.code.upper()), None)
    if lobby is None or lobby.get("gameState") is None:
        parser.error("lobby has no persisted gameplay state")
    state = lobby["gameState"]
    # Never export the lobby players, reconnect hashes or queued command payloads.
    public = {"code": lobby["code"], "version": state["version"],
              "lastAppliedCommandSequence": state["lastAppliedCommandSequence"],
              "snapshot": json.loads(state["payloadJson"])}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(public, ensure_ascii=False, indent=2, sort_keys=True) + "\n")
    print("Gameplay state exported: version={}".format(public["version"]))


if __name__ == "__main__":
    main()
