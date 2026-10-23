#!/usr/bin/env python3
"""End-to-end six-player smoke test; starts an isolated server unless given a URL."""

import json
import os
import pathlib
import signal
import socket
import sys
import subprocess
import tempfile
import time
import urllib.error
import urllib.request


ROOT = pathlib.Path(__file__).resolve().parent
SERVER_DLL = ROOT / "bin" / "Release" / "net10.0" / "PolyRisk.LobbyServer.dll"


def run_isolated_server():
    if not SERVER_DLL.exists():
        raise SystemExit("Build the lobby server first: dotnet build --configuration Release")

    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        port = sock.getsockname()[1]
    base_url = f"http://127.0.0.1:{port}"

    with tempfile.TemporaryDirectory(prefix="polyrisk-smoke-") as directory:
        environment = os.environ.copy()
        environment.update({
            "POLYRISK_URLS": base_url,
            "POLYRISK_DATA_PATH": str(pathlib.Path(directory) / "lobbies.json"),
            "ASPNETCORE_ENVIRONMENT": "Development",
        })
        server = subprocess.Popen(
            ["dotnet", str(SERVER_DLL)], cwd=ROOT, env=environment,
            stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT,
        )
        try:
            for _ in range(100):
                if server.poll() is not None:
                    raise RuntimeError("Isolated lobby server exited before becoming ready")
                try:
                    with urllib.request.urlopen(base_url + "/", timeout=1) as response:
                        if response.status == 200:
                            break
                except (urllib.error.URLError, TimeoutError):
                    time.sleep(0.1)
            else:
                raise RuntimeError("Isolated lobby server did not become ready")

            result = subprocess.run([sys.executable, __file__, base_url], check=False)
            if result.returncode:
                raise SystemExit(result.returncode)
        finally:
            if server.poll() is None:
                server.send_signal(signal.SIGTERM)
                try:
                    server.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    server.kill()
                    server.wait(timeout=5)


if len(sys.argv) > 1:
    BASE_URL = sys.argv[1].rstrip("/")
else:
    run_isolated_server()
    raise SystemExit(0)


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


def credentials(session):
    return {
        "playerId": session["playerId"],
        "reconnectToken": session["reconnectToken"],
    }


host = post("/api/lobbies", {"displayName": "Arthur", "maxPlayers": 6})
code = host["lobby"]["code"]
assert len(code) == 6
assert host["lobby"]["players"][0]["isHost"] is True

players = [host]
for number in range(2, 7):
    players.append(post(f"/api/lobbies/{code}/join", {"displayName": f"Player {number}"}))

assert len(players[-1]["lobby"]["players"]) == 6
post(f"/api/lobbies/{code}/join", {"displayName": "Player 7"}, expected=400)

post(f"/api/lobbies/{code}/start", credentials(players[1]), expected=400)
post(f"/api/lobbies/{code}/start", credentials(host), expected=400)

for player in players:
    payload = credentials(player) | {"isReady": True}
    ready_snapshot = post(f"/api/lobbies/{code}/ready", payload)

assert ready_snapshot["version"] == 12
started = post(f"/api/lobbies/{code}/start", credentials(host))
assert started["status"] == "started"
assert len(started["players"]) == 6
post(f"/api/lobbies/{code}/join", {"displayName": "Late Player"}, expected=400)

roll_request = credentials(players[1]) | {
    "expectedStateVersion": 0,
    "type": "turn.roll",
    "payloadJson": '{"requested":true}',
    "clientCommandId": "smoke-player-2-roll-v0",
}
roll_command = post(f"/api/lobbies/{code}/game/commands", roll_request)
replayed_roll = post(f"/api/lobbies/{code}/game/commands", roll_request)
assert replayed_roll["sequence"] == roll_command["sequence"] == 1
transit_request = credentials(players[1]) | {
    "expectedStateVersion": 0,
    "type": "transit.choose",
    "payloadJson": '{"takeTransit":true}',
    "clientCommandId": "smoke-player-2-transit-v0",
}
transit_command = post(f"/api/lobbies/{code}/game/commands", transit_request)
assert transit_command["sequence"] == 2
post(
    f"/api/lobbies/{code}/game/commands",
    roll_request | {"payloadJson": '{"requested":false}'},
    expected=400,
)
post(
    f"/api/lobbies/{code}/game/state",
    credentials(players[1])
    | {
        "expectedStateVersion": 0,
        "lastAppliedCommandSequence": 1,
        "payloadJson": "{}",
    },
    expected=400,
)

command_batch = post(
    f"/api/lobbies/{code}/game/commands/poll",
    credentials(host) | {"afterSequence": 0},
)
assert command_batch["stateVersion"] == 0
assert [command["type"] for command in command_batch["commands"]] == [
    "turn.roll", "transit.choose"
]

published_state = post(
    f"/api/lobbies/{code}/game/state",
    credentials(host)
    | {
        "expectedStateVersion": 0,
        "lastAppliedCommandSequence": 2,
        "payloadJson": '{"turn":1,"phase":"moving"}',
    },
)
assert published_state["version"] == 1
assert published_state["lastAppliedCommandSequence"] == 2

reconnected_state = post(
    f"/api/lobbies/{code}/game/state/get",
    credentials(players[5]),
)
assert reconnected_state["version"] == 1
assert json.loads(reconnected_state["payloadJson"])["phase"] == "moving"
post(
    f"/api/lobbies/{code}/game/commands",
    credentials(players[2])
    | {
        "expectedStateVersion": 0,
        "type": "turn.roll",
        "payloadJson": "{}",
    },
    expected=400,
)

post(
    f"/api/lobbies/{code}/game/commands",
    credentials(players[2])
    | {
        "expectedStateVersion": 1,
        "type": "debug.cheat",
        "payloadJson": "{}",
    },
    expected=400,
)
post(
    f"/api/lobbies/{code}/game/commands",
    credentials(players[2])
    | {
        "expectedStateVersion": 1,
        "type": "turn.roll",
        "payloadJson": "not-json",
    },
    expected=400,
)
post(
    f"/api/lobbies/{code}/game/state/get",
    {"playerId": players[2]["playerId"], "reconnectToken": "wrong-token"},
    expected=400,
)

# This verifies transport authorization and payload preservation, not game rules.
turbo_command = post(
    f"/api/lobbies/{code}/game/commands",
    credentials(players[5]) | {
        "expectedStateVersion": 1, "type": "dice.turbo", "payloadJson": '{"choice":1}',
    },
)
turbo_batch = post(
    f"/api/lobbies/{code}/game/commands/poll",
    credentials(host) | {"afterSequence": turbo_command["sequence"] - 1},
)
assert turbo_batch["commands"][0]["type"] == "dice.turbo"
assert json.loads(turbo_batch["commands"][0]["payloadJson"]) == {"choice": 1}

# Marketplace sanctions must cross the same authenticated host/guest transport
# path, and retrying the same client command must not enqueue it twice.
sanction_request = credentials(players[3]) | {
    "expectedStateVersion": 1,
    "type": "marketplace.sanction",
    "payloadJson": '{"sanction":"audit","targetPlayerId":"' + players[5]["playerId"] + '"}',
    "clientCommandId": "smoke-player-4-marketplace-audit-v0",
}
sanction_command = post(f"/api/lobbies/{code}/game/commands", sanction_request)
replayed_sanction = post(f"/api/lobbies/{code}/game/commands", sanction_request)
assert replayed_sanction["sequence"] == sanction_command["sequence"]
sanction_batch = post(
    f"/api/lobbies/{code}/game/commands/poll",
    credentials(host) | {"afterSequence": sanction_command["sequence"] - 1},
)
assert len(sanction_batch["commands"]) == 1
delivered_sanction = sanction_batch["commands"][0]
assert delivered_sanction["type"] == "marketplace.sanction"
assert delivered_sanction["playerId"] == players[3]["playerId"]
assert json.loads(delivered_sanction["payloadJson"]) == {
    "sanction": "audit", "targetPlayerId": players[5]["playerId"]
}

migration_host = post("/api/lobbies", {"displayName": "First Host", "maxPlayers": 3})
migration_code = migration_host["lobby"]["code"]
successor = post(f"/api/lobbies/{migration_code}/join", {"displayName": "Successor"})
leave_snapshot = post(f"/api/lobbies/{migration_code}/leave", credentials(migration_host))
assert leave_snapshot["hostPlayerId"] == successor["playerId"]
assert leave_snapshot["players"][0]["isHost"] is True

print(
    f"LOBBY + GAME PROTOCOL SMOKE PASS: code={code}, players=6, "
    f"lobby_version={started['version']}, state_version={published_state['version']}"
)
