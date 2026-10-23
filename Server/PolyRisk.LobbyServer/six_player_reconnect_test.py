#!/usr/bin/env python3
"""Six-seat protocol recovery test across a lobby-server restart.

This covers transport, persistence, and host handoff only; it does not simulate
Unity gameplay rules or six rendered WebGL clients.
"""

import json
import os
import pathlib
import signal
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request


ROOT = pathlib.Path(__file__).resolve().parent
DLL = ROOT / "bin" / "Release" / "net10.0" / "PolyRisk.LobbyServer.dll"


def free_loopback_port():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


BASE_URL = f"http://127.0.0.1:{free_loopback_port()}"


def post(path, payload, expected=200):
    request = urllib.request.Request(
        BASE_URL + path,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=5) as response:
            status = response.status
            body = json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        status = error.code
        body = json.loads(error.read().decode("utf-8"))
    assert status == expected, (path, status, expected, body)
    return body


def credentials(player):
    return {"playerId": player["playerId"], "reconnectToken": player["reconnectToken"]}


def start_server(storage_path):
    environment = os.environ.copy()
    environment.update(
        {
            "POLYRISK_URLS": BASE_URL,
            "POLYRISK_DATA_PATH": str(storage_path),
            "ASPNETCORE_ENVIRONMENT": "Development",
        }
    )
    process = subprocess.Popen(
        ["dotnet", str(DLL)], cwd=ROOT, env=environment,
        stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT,
    )
    for _ in range(100):
        if process.poll() is not None:
            raise RuntimeError("Lobby server exited before its isolated port became ready")
        try:
            with urllib.request.urlopen(BASE_URL + "/", timeout=1) as response:
                if response.status == 200:
                    return process
        except (urllib.error.URLError, TimeoutError, OSError):
            time.sleep(0.1)
    process.terminate()
    raise RuntimeError("Lobby server did not become ready")


def stop_server(process):
    process.send_signal(signal.SIGTERM)
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=5)


with tempfile.TemporaryDirectory(prefix="polyrisk-six-reconnect-") as directory:
    storage = pathlib.Path(directory) / "lobbies.json"
    server = start_server(storage)
    try:
        host = post("/api/lobbies", {"displayName": "Six-player host", "maxPlayers": 6})
        code = host["lobby"]["code"]
        players = [host]
        for seat in range(2, 7):
            players.append(post(f"/api/lobbies/{code}/join", {"displayName": f"Seat {seat}"}))
        assert len(players[-1]["lobby"]["players"]) == 6
        post(f"/api/lobbies/{code}/join", {"displayName": "Seat 7"}, expected=400)

        for player in players:
            post(f"/api/lobbies/{code}/ready", credentials(player) | {"isReady": True})
        started = post(f"/api/lobbies/{code}/start", credentials(host))
        assert started["status"] == "started" and len(started["players"]) == 6

        queued = []
        for index, player in enumerate(players[1:], start=1):
            queued.append(post(
                f"/api/lobbies/{code}/game/commands",
                credentials(player) | {
                    "expectedStateVersion": 0,
                    "type": "turn.roll",
                    "payloadJson": json.dumps({"seat": index}),
                    "clientCommandId": f"seat-{index}-roll-v0",
                },
            ))
        assert [command["sequence"] for command in queued] == [1, 2, 3, 4, 5]
        batch = post(
            f"/api/lobbies/{code}/game/commands/poll",
            credentials(host) | {"afterSequence": 0},
        )
        assert [command["sequence"] for command in batch["commands"]] == [1, 2, 3, 4, 5]
        assert {command["playerId"] for command in batch["commands"]} == {
            player["playerId"] for player in players[1:]
        }
        exact_balances = [500001.0, 5000.0, 5001.0, 20000.0, 20001.0, 50000.0]
        checkpoint = {
            "currentSgo": 1000.0,
            "players": [
                {"playerId": index, "lobbyPlayerId": player["playerId"], "balance": exact_balances[index]}
                for index, player in enumerate(players)
            ],
            "checkpoint": "six-seat-reconnect",
            "commandsApplied": 5,
        }
        published = post(
            f"/api/lobbies/{code}/game/state",
            credentials(host) | {
                "expectedStateVersion": 0,
                "lastAppliedCommandSequence": 5,
                "payloadJson": json.dumps(checkpoint),
            },
        )
        assert published["version"] == 1
        saved = json.loads(storage.read_text(encoding="utf-8"))
        saved_lobby = next(item for item in saved["lobbies"] if item["code"] == code)
        saved_receipts = saved_lobby.get("commandReceipts", [])
        assert len(saved_receipts) == 5
        assert any(item["clientCommandId"] == "seat-5-roll-v0" for item in saved_receipts)
    finally:
        stop_server(server)

    server = start_server(storage)
    try:
        for player in players:
            snapshot = post(f"/api/lobbies/{code}/snapshot", credentials(player))
            assert snapshot["status"] == "started" and len(snapshot["players"]) == 6
            state = post(f"/api/lobbies/{code}/game/state/get", credentials(player))
            assert state["version"] == 1
            assert state["lastAppliedCommandSequence"] == 5
            received = json.loads(state["payloadJson"])
            assert received["checkpoint"] == checkpoint["checkpoint"]
            assert received["commandsApplied"] == 5
            received_balances = [item["balance"] for item in received["players"]]
            if player == host:
                assert received_balances == exact_balances
            else:
                own_index = players.index(player)
                assert received_balances[own_index] == exact_balances[own_index]
                for index, (actual, visible) in enumerate(zip(exact_balances, received_balances)):
                    if index == own_index:
                        continue
                    expected_band = 0 if actual <= 5000 else 10000 if actual <= 20000 else 30000 if actual <= 50000 else 51000
                    assert visible == expected_band, (player["playerId"], index, actual, visible, expected_band)
                    assert visible != actual, (player["playerId"], index, actual)

        # A hijacked rent is one confirmed payment represented by two correlated
        # transfer facts: 25% to the hijacker, 75% to the property owner.
        split_rent_events = [
            {
                "id": 101,
                "correlationId": 9001,
                "phase": 2,  # GameEventPhase.Resolution
                "kind": 5,  # GameEventKind.TransferConfirmed
                "priority": 1,  # GameEventPriority.Normal
                "actorId": 2,
                "targetId": 1,
                "tileId": 6,
                "amount": -25.0,
                "reason": 3,  # GameEventReason.Rent
                "reasonParameterId": 0,
                "reasonParameterValue": 0.0,
                "valueA": 0,
                "valueB": 0,
                "coalescible": True,
                "mergedCount": 1,
                "splits": [{"payeeId": 1, "amount": 25.0}],
            },
            {
                "id": 102,
                "correlationId": 9001,
                "phase": 2,
                "kind": 5,
                "priority": 1,
                "actorId": 2,
                "targetId": 0,
                "tileId": 6,
                "amount": -75.0,
                "reason": 3,
                "reasonParameterId": 0,
                "reasonParameterValue": 0.0,
                "valueA": 0,
                "valueB": 0,
                "coalescible": True,
                "mergedCount": 1,
                "splits": [{"payeeId": 0, "amount": 75.0}],
            },
        ]
        post_rent_balances = [500076.0, 5025.0, 4901.0, 20000.0, 20001.0, 50000.0]
        split_rent_checkpoint = {
            "currentSgo": 1000.0,
            "gameEventSequence": 102,
            "correlationSequence": 9001,
            "players": [
                {"playerId": index, "lobbyPlayerId": player["playerId"], "balance": post_rent_balances[index]}
                for index, player in enumerate(players)
            ],
            "gameEvents": split_rent_events,
        }
        split_publish = post(
            f"/api/lobbies/{code}/game/state",
            credentials(host) | {
                "expectedStateVersion": 1,
                "lastAppliedCommandSequence": 5,
                "payloadJson": json.dumps(split_rent_checkpoint),
            },
        )
        assert split_publish["version"] == 2
        for index, player in enumerate(players):
            state = post(f"/api/lobbies/{code}/game/state/get", credentials(player))
            assert state["version"] == 2
            received = json.loads(state["payloadJson"])
            assert received["gameEventSequence"] == 102
            assert received["correlationSequence"] == 9001
            assert received["gameEvents"] == split_rent_events
            received_balances = [item["balance"] for item in received["players"]]
            if player == host:
                assert received_balances == post_rent_balances
            else:
                assert received_balances[index] == post_rent_balances[index]
                for seat, (actual, visible) in enumerate(zip(post_rent_balances, received_balances)):
                    if seat == index:
                        continue
                    expected_band = 0 if actual <= 5000 else 10000 if actual <= 20000 else 30000 if actual <= 50000 else 51000
                    assert visible == expected_band, (player["playerId"], seat, actual, visible, expected_band)

        replay = post(
            f"/api/lobbies/{code}/game/commands",
            credentials(players[5]) | {
                "expectedStateVersion": 0,
                "type": "turn.roll",
                "payloadJson": json.dumps({"seat": 5}),
                "clientCommandId": "seat-5-roll-v0",
            },
        )
        assert replay["sequence"] == 5

        empty_queue = post(
            f"/api/lobbies/{code}/game/commands/poll",
            credentials(host) | {"afterSequence": 0},
        )
        assert empty_queue["commands"] == []

        # A fresh command after recovery is delivered to the replacement host.
        resumed_command = post(
            f"/api/lobbies/{code}/game/commands",
            credentials(players[5]) | {
                "expectedStateVersion": 2,
                "type": "action.secondary",
                "payloadJson": '{"action":"pass"}',
                "clientCommandId": "seat-5-pass-v1",
            },
        )
        assert resumed_command["sequence"] == 6

        handoff = post(f"/api/lobbies/{code}/leave", credentials(host))
        successor = players[1]
        assert handoff["hostPlayerId"] == successor["playerId"]
        assert handoff["players"][0]["isHost"] is True
        recovered_by_successor = post(f"/api/lobbies/{code}/game/state/get", credentials(successor))
        recovered_snapshot = json.loads(recovered_by_successor["payloadJson"])
        assert [item["balance"] for item in recovered_snapshot["players"]] == post_rent_balances
        assert recovered_snapshot["gameEvents"] == split_rent_events
        migrated_batch = post(
            f"/api/lobbies/{code}/game/commands/poll",
            credentials(successor) | {"afterSequence": 5},
        )
        assert [command["sequence"] for command in migrated_batch["commands"]] == [6]
        migrated_state = post(
            f"/api/lobbies/{code}/game/state",
            credentials(successor) | {
                "expectedStateVersion": 2,
                "lastAppliedCommandSequence": 6,
                "payloadJson": '{"checkpoint":"after-host-handoff"}',
            },
        )
        assert migrated_state["version"] == 3
        for player in players[1:]:
            state = post(f"/api/lobbies/{code}/game/state/get", credentials(player))
            assert state["version"] == 3
            assert json.loads(state["payloadJson"]) == {"checkpoint": "after-host-handoff"}
    finally:
        stop_server(server)

print("SIX-PLAYER RECONNECT PASS: six sessions restored; split-rent typed events and beneficiary shares survived reconnect and host handoff; command delivery verified")
