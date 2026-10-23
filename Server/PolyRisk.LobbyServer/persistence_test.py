#!/usr/bin/env python3
"""Restart test proving that sessions, command queue and game state survive a process restart."""

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


def credentials(session):
    return {"playerId": session["playerId"], "reconnectToken": session["reconnectToken"]}


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
        ["dotnet", str(DLL)],
        cwd=ROOT,
        env=environment,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.STDOUT,
    )
    for _ in range(80):
        if process.poll() is not None:
            raise RuntimeError("Lobby server exited before its isolated port became ready")
        try:
            with urllib.request.urlopen(BASE_URL + "/", timeout=1) as response:
                if response.status == 200:
                    return process
        except (urllib.error.URLError, TimeoutError):
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


with tempfile.TemporaryDirectory(prefix="polyrisk-persistence-") as directory:
    storage = pathlib.Path(directory) / "lobbies.json"
    server = start_server(storage)
    try:
        host = post("/api/lobbies", {"displayName": "Persistent Host", "maxPlayers": 2})
        code = host["lobby"]["code"]
        guest = post(f"/api/lobbies/{code}/join", {"displayName": "Persistent Guest"})
        for player in (host, guest):
            post(f"/api/lobbies/{code}/ready", credentials(player) | {"isReady": True})
        post(f"/api/lobbies/{code}/start", credentials(host))
        command_request = credentials(guest) | {
            "expectedStateVersion": 0,
            "type": "turn.roll",
            "payloadJson": '{"requested":true}',
            "clientCommandId": "persistent-guest-roll-v0",
        }
        command = post(f"/api/lobbies/{code}/game/commands", command_request)
        assert command["sequence"] == 1
        assert storage.exists()
        persisted_text = storage.read_text(encoding="utf-8")
        assert host["reconnectToken"] not in persisted_text
        assert guest["reconnectToken"] not in persisted_text
    finally:
        stop_server(server)

    server = start_server(storage)
    try:
        snapshot = post(f"/api/lobbies/{code}/snapshot", credentials(guest))
        assert snapshot["status"] == "started"
        assert len(snapshot["players"]) == 2
        replay = post(f"/api/lobbies/{code}/game/commands", command_request)
        assert replay["sequence"] == command["sequence"] == 1
        queued = post(
            f"/api/lobbies/{code}/game/commands/poll",
            credentials(host) | {"afterSequence": 0},
        )
        assert queued["commands"][0]["sequence"] == 1
        published = post(
            f"/api/lobbies/{code}/game/state",
            credentials(host)
            | {
                "expectedStateVersion": 0,
                "lastAppliedCommandSequence": 1,
                "payloadJson": '{"turn":2,"phase":"planning"}',
            },
        )
        assert published["version"] == 1
        post(f"/api/lobbies/{code}/game/commands", command_request)
    finally:
        stop_server(server)

    server = start_server(storage)
    try:
        state = post(f"/api/lobbies/{code}/game/state/get", credentials(guest))
        assert state["version"] == 1
        assert state["lastAppliedCommandSequence"] == 1
        assert json.loads(state["payloadJson"])["phase"] == "planning"
        replay = post(f"/api/lobbies/{code}/game/commands", command_request)
        assert replay["sequence"] == 1
        emptied = post(
            f"/api/lobbies/{code}/game/commands/poll",
            credentials(host) | {"afterSequence": 0},
        )
        assert emptied["commands"] == []
    finally:
        stop_server(server)

print("LOBBY PERSISTENCE RESTART PASS: credentials hashed, queue restored, state restored")
