#!/usr/bin/env python3
"""Six authenticated transport clients at Unity's real polling cadence; not a Unity gameplay test."""
import argparse
import concurrent.futures
import contextlib
import json
import os
import pathlib
import socket
import subprocess
import tempfile
import threading
import time
import urllib.error
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parent
DLL = ROOT / "bin/Release/net10.0/PolyRisk.LobbyServer.dll"


@contextlib.contextmanager
def server(budget):
    with tempfile.TemporaryDirectory(prefix="polyrisk-poll-budget-") as directory:
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            port = sock.getsockname()[1]
        base = f"http://127.0.0.1:{port}"
        env = os.environ.copy()
        env.pop("POLYRISK_WEBGL_ROOT", None)
        env.update(POLYRISK_URLS=base, POLYRISK_REQUESTS_PER_MINUTE=str(budget),
                   POLYRISK_DATA_PATH=str(pathlib.Path(directory) / "lobbies.json"))
        with open(pathlib.Path(directory) / "server.log", "w") as log:
            process = subprocess.Popen(["dotnet", str(DLL)], cwd=ROOT, env=env, stdout=log, stderr=log)
            try:
                for _ in range(100):
                    try:
                        urllib.request.urlopen(base, timeout=3).close()
                        break
                    except OSError:
                        if process.poll() is not None:
                            raise AssertionError("Budget validation server failed to start")
                        time.sleep(.1)
                else:
                    raise AssertionError("Budget validation startup timeout")
                yield base
            finally:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)


def post(base, path, payload):
    request = urllib.request.Request(base + path, data=json.dumps(payload).encode(),
                                     headers={"Content-Type": "application/json"}, method="POST")
    try:
        with urllib.request.urlopen(request, timeout=8) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        return error.code, None


def credentials(session):
    return {key: session[key] for key in ("playerId", "reconnectToken")}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--duration", type=float, default=90)
    parser.add_argument("--output", type=pathlib.Path)
    args = parser.parse_args()
    assert args.duration >= 1
    results = {"clients": 6, "targetSeconds": args.duration, "budgetPerMinute": 3600,
               "hostPollSeconds": .25, "replicaPollSeconds": 1, "lobbyPollSeconds": 1.5,
               "scope": "HTTP authenticated polling only; no Unity rules or browser/device claim"}
    with server(3600) as base:
        status, host = post(base, "/api/lobbies", {"displayName": "Budget Host", "maxPlayers": 6})
        assert status == 200
        code = host["lobby"]["code"]
        players = [host]
        for index in range(1, 6):
            status, session = post(base, f"/api/lobbies/{code}/join", {"displayName": f"Budget {index}"})
            assert status == 200
            players.append(session)
        for player in players:
            assert post(base, f"/api/lobbies/{code}/ready", credentials(player) | {"isReady": True})[0] == 200
        assert post(base, f"/api/lobbies/{code}/start", credentials(host))[0] == 200
        assert post(base, f"/api/lobbies/{code}/game/state", credentials(host) | {
            "expectedStateVersion": 0, "lastAppliedCommandSequence": 0,
            "payloadJson": '{"transportFixture":true}'})[0] == 200
        barrier = threading.Barrier(6)
        def client(index):
            player = credentials(players[index])
            barrier.wait()
            start = time.monotonic()
            finish = start + args.duration
            next_game = next_lobby = start
            counts = {"game": 0, "lobby": 0}
            statuses = {}
            while time.monotonic() < finish:
                now = time.monotonic()
                if now >= next_game:
                    route = "game/commands/poll" if index == 0 else "game/state/get"
                    payload = player | {"afterSequence": 0} if index == 0 else player
                    status, _ = post(base, f"/api/lobbies/{code}/{route}", payload)
                    statuses[str(status)] = statuses.get(str(status), 0) + 1
                    counts["game"] += 1
                    next_game += .25 if index == 0 else 1
                if now >= next_lobby:
                    status, _ = post(base, f"/api/lobbies/{code}/snapshot", player)
                    statuses[str(status)] = statuses.get(str(status), 0) + 1
                    counts["lobby"] += 1
                    next_lobby += 1.5
                time.sleep(max(.001, min(next_game, next_lobby, finish) - time.monotonic()))
            return {"seat": index, "counts": counts, "statuses": statuses,
                    "elapsedSeconds": round(time.monotonic() - start, 3)}
        with concurrent.futures.ThreadPoolExecutor(max_workers=6) as executor:
            results["polling"] = list(executor.map(client, range(6)))
        assert all(set(client["statuses"]) == {"200"} for client in results["polling"]), results["polling"]
    with server(60) as base:
        statuses = []
        for _ in range(65):
            try:
                with urllib.request.urlopen(base, timeout=5) as response:
                    statuses.append(response.status)
            except urllib.error.HTTPError as error:
                statuses.append(error.code)
        assert 429 in statuses and statuses[0] == 200, statuses
        results["protection"] = {"configuredBudget": 60, "requestCount": len(statuses),
                                 "accepted": statuses.count(200), "rejected429": statuses.count(429)}
    results["pass"] = True
    if args.output:
        args.output.write_text(json.dumps(results, indent=2) + "\n")
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
