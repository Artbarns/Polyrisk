"""Validate exported game invariants; usage: python3 check_results.py CSV GAMES PLAYERS."""
import csv
import math
import statistics
import sys
from collections import Counter
from pathlib import Path


def check(path, expected_games, expected_players):
    with Path(path).open(newline="") as source:
        rows = list(csv.DictReader(source))
    assert len(rows) == expected_games, ("game count", len(rows), expected_games)
    cash, laps = [], []
    for index, row in enumerate(rows):
        assert None not in row and None not in row.values(), (index, "malformed CSV row")
        assert int(row["GameIndex"]) == index
        assert int(row["PlayerCount"]) == expected_players
        assert int(row["Seed"]) == int(rows[0]["Seed"]) + index
        assert int(row["ErrorCount"]) == 0, (index, "runtime errors")
        assert not row["VictoryReason"].startswith("ANOMALY"), (index, row["VictoryReason"])
        assert float(row["RealSecondsElapsed"]) > 0 and int(row["FramesElapsed"]) > 0
        winner = int(row["WinnerId"])
        assert 0 <= winner < expected_players
        alive, turns = [], 0
        for player in range(6):
            prefix = f"P{player}_"
            if player >= expected_players:
                assert all(value == "" for key, value in row.items() if key.startswith(prefix))
                continue
            balance, net_worth, peak = (float(row[prefix + field]) for field in ("Balance", "NetWorth", "PeakCash"))
            assert all(math.isfinite(value) and value >= 0 for value in (balance, net_worth, peak))
            assert net_worth >= balance and peak >= balance
            assert row[prefix + "Bankrupt"] in ("True", "False")
            if row[prefix + "Bankrupt"] == "True":
                assert balance == 0 and net_worth == 0
            else:
                alive.append(player)
            turns += int(row[prefix + "Turns"])
        assert turns == int(row["TurnsStarted"])
        assert winner in alive
        if row["VictoryReason"] == "LAST PLAYER STANDING":
            assert alive == [winner]
        elif row["VictoryReason"] == "TIME OUT — HIGHEST NET WORTH":
            assert int(row["MinActiveLaps"]) >= 50
            assert float(row[f"P{winner}_NetWorth"]) == max(float(row[f"P{p}_NetWorth"]) for p in alive)
        elif row["VictoryReason"] == "REACHED $10,000,000 IN NET CASH":
            assert float(row[f"P{winner}_Balance"]) >= 10_000_000
        else:
            raise AssertionError((index, "unknown victory reason", row["VictoryReason"]))
        assert int(row["MinActiveLaps"]) == min(int(row[f"P{p}_Laps"]) for p in alive)
        assert int(row["MaxLaps"]) == max(int(row[f"P{p}_Laps"]) for p in range(expected_players))
        cash.append(float(row[f"P{winner}_Balance"]))
        laps.append(int(row["MaxLaps"]))
    print(f"PASS: {len(rows)} games, {expected_players} players; CSV invariants valid")
    print("Reasons:", dict(Counter(row["VictoryReason"] for row in rows)))
    print(f"Winner cash min/median/max: {min(cash):.2f} / {statistics.median(cash):.2f} / {max(cash):.2f}")
    print(f"Max laps min/median/max: {min(laps)} / {statistics.median(laps)} / {max(laps)}")


if __name__ == "__main__":
    check(sys.argv[1], int(sys.argv[2]), int(sys.argv[3]))
