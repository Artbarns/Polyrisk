#!/usr/bin/env python3
"""Seed a disposable, ready lobby before clients initialize their game authority.

Run only against an isolated QA storage file while the lobby server is stopped.
The resulting lobby looks started to its clients, and its initial game snapshot
is available before the host begins InitializeAuthority.
"""
import argparse
import copy
import datetime as dt
import json
import os
import pathlib
import tempfile


def utc_now():
    return dt.datetime.now(dt.timezone.utc).isoformat().replace("+00:00", "Z")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--storage", required=True, type=pathlib.Path,
                        help="isolated lobby JSON storage file; server must be stopped")
    parser.add_argument("--code", required=True, help="exact QA lobby code")
    parser.add_argument("--fixture", required=True, type=pathlib.Path,
                        help="OnlineGameSnapshotDto JSON, optionally wrapped in {snapshot: ...}")
    args = parser.parse_args()

    storage = args.storage.resolve()
    if not storage.is_file():
        parser.error("QA storage file does not exist")
    disk = json.loads(storage.read_text(encoding="utf-8"))
    if disk.get("schemaVersion") != 1 or not isinstance(disk.get("lobbies"), list):
        parser.error("unsupported lobby storage schema")

    matches = [lobby for lobby in disk["lobbies"] if lobby.get("code") == args.code]
    if len(matches) != 1:
        parser.error("lobby code must match exactly one lobby in QA storage")
    lobby = matches[0]
    if lobby.get("status") != 0:
        parser.error("lobby must still be waiting; do not overwrite a started/abandoned game")
    players = lobby.get("players") or []
    if len(players) < 2 or any(not player.get("isReady") for player in players):
        parser.error("all players in the lobby must be ready before seeding")
    old_state = lobby.get("gameState") or {}
    if old_state.get("version", 0) != 0 or lobby.get("latestCommandSequence", 0) != 0 \
            or lobby.get("commands") or lobby.get("commandReceipts"):
        parser.error("lobby already has game state or commands")

    fixture = json.loads(args.fixture.read_text(encoding="utf-8"))
    if "snapshot" in fixture:
        fixture = fixture["snapshot"]
    fixture = copy.deepcopy(fixture)
    fixture_players = fixture.get("players")
    if not isinstance(fixture_players, list) or len(fixture_players) != len(players):
        parser.error("fixture player count must exactly match the lobby")
    for seat, game_player in enumerate(fixture_players):
        if game_player.get("playerId") != seat:
            parser.error("fixture playerId values must match their seat indices")
        lobby_player = players[seat]
        game_player["lobbyPlayerId"] = lobby_player["playerId"]
        game_player["displayName"] = lobby_player["displayName"]
    current_player = fixture.get("currentPlayerId")
    if not isinstance(current_player, int) or not 0 <= current_player < len(players):
        parser.error("fixture currentPlayerId must identify a lobby seat")

    timestamp = utc_now()
    lobby["status"] = 1  # LobbyStatus.Started
    lobby["version"] = int(lobby.get("version", 1)) + 1
    lobby["lastActivityUtc"] = timestamp
    lobby["gameState"] = {
        "version": 1,
        "lastAppliedCommandSequence": 0,
        "payloadJson": json.dumps(fixture, ensure_ascii=False, separators=(",", ":")),
        "updatedUtc": timestamp,
    }
    lobby["latestCommandSequence"] = 0
    lobby["commands"] = []
    lobby["commandReceipts"] = []

    fd, temporary = tempfile.mkstemp(prefix=storage.name + ".", suffix=".tmp", dir=storage.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as output:
            json.dump(disk, output, ensure_ascii=False, separators=(",", ":"))
            output.write("\n")
        os.replace(temporary, storage)
    except Exception:
        try:
            os.unlink(temporary)
        except FileNotFoundError:
            pass
        raise
    print(f"Seeded QA lobby {args.code}: {len(players)} ready seats, state v1, sequence 0")


if __name__ == "__main__":
    main()
