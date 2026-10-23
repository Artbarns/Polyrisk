using System;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Multiplayer;
using PolyRisk.Systems;

namespace PolyRisk.Editor
{
    /// <summary>Regression checks for the non-visual typed event queue.</summary>
    public static class GameEventRegression
    {
        private static int assertions;

        private static void Check(bool condition, string label)
        {
            assertions++;
            if (!condition) throw new Exception("GAME EVENT REGRESSION FAILED: " + label);
            Debug.Log("PASS: " + label);
        }

        public static void Run()
        {
            assertions = 0;
            GameEvents.BeginActivitySession();
            GameEvents.OnGameEventPublished = null;
            try
            {
                using (var center = new GameEventPresenter { PreserveChronology = true })
                {
                    var payment = GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                        GameEventKind.TaxPaid, GameEventPriority.Normal, 0, amount: -600);
                    var roll = GameEvents.PublishGameEvent(GameEventPhase.Action,
                        GameEventKind.DiceRolled, GameEventPriority.Major, 1, valueA: 3, valueB: 4);
                    Check(center.TryBeginNext(out var first) && first.Id == payment.Id,
                        "Central presentation keeps a payment before the following player's higher-priority roll");
                    Check(!center.TryBeginNext(out _), "An active receipt cannot overlap the next central roll");
                    center.SkipCurrent();
                    Check(center.TryBeginNext(out var second) && second.Id == roll.Id,
                        "After the receipt, the confirmed dice event remains queued unchanged");
                }

                using (var presenter = new GameEventPresenter { LocalPlayerId = 0 })
                {
                    long splitCorrelation = GameEvents.CreateCorrelationId();
                    GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                        GameEventKind.TransferConfirmed, GameEventPriority.Normal,
                        0, 1, 8, -75, GameEventReason.Rent, splitCorrelation,
                        splits: new[] { new GameEventSplit(1, 75) }, coalescible: true);
                    GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                        GameEventKind.TransferConfirmed, GameEventPriority.Normal,
                        0, 2, 8, -25, GameEventReason.Rent, splitCorrelation,
                        splits: new[] { new GameEventSplit(2, 25) }, coalescible: true);

                    Check(presenter.History.Count == 2, "Trace keeps both confirmed transfer facts");
                    Check(presenter.PendingCount == 1, "Same-correlation rent splits coalesce for one presentation");
                    Check(presenter.TryBeginNext(out var rent), "Queued event begins");
                    Check(rent.Amount == -100 && rent.Splits.Count == 2
                        && rent.TargetId == -1 && rent.MergedCount == 2,
                        "Coalesced split keeps total and distinct payees");

                    GameEvents.PublishGameEvent(GameEventPhase.Action, GameEventKind.DiceRolled,
                        GameEventPriority.Major, 1, reason: GameEventReason.DiceRoll,
                        valueA: 4, valueB: 5);
                    GameEvents.PublishGameEvent(GameEventPhase.Turn, GameEventKind.TurnStarted,
                        GameEventPriority.Critical, 0);
                    Check(!presenter.TryBeginNext(out _), "An active sequence is not interrupted");
                    presenter.ReducedMotion = true;
                    Check(presenter.GetBeatDurationSeconds(rent, GamePresentationBeat.Anticipation) == 0,
                        "Reduced motion removes anticipation delay");
                    presenter.SkipCurrent();
                    Check(presenter.TryBeginNext(out var critical)
                        && critical.Priority == GameEventPriority.Critical,
                        "Critical queue item precedes older major item after skip");
                    Check(presenter.History.Count == 4,
                        "Skipping presentation preserves the event trace");

                    long transportCorrelation = GameEvents.CreateCorrelationId();
                    var transportSource = GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                        GameEventKind.TransferConfirmed, GameEventPriority.Normal,
                        0, tileId: 8, amount: -100, reason: GameEventReason.Rent,
                        correlationId: transportCorrelation,
                        splits: new[] { new GameEventSplit(1, 75), new GameEventSplit(2, 25) },
                        coalescible: true);
                    string json = JsonUtility.ToJson(GameEventDto.FromGameEvent(transportSource));
                    var restoredDto = JsonUtility.FromJson<GameEventDto>(json);
                    var restored = restoredDto != null ? restoredDto.ToGameEvent() : null;
                    Check(restored != null && restored.Id == transportSource.Id
                        && restored.CorrelationId == transportSource.CorrelationId
                        && restored.Kind == transportSource.Kind && restored.Amount == -100
                        && restored.Splits.Count == 2 && restored.Splits[0].Amount == 75
                        && restored.Splits[1].PayeeId == 2,
                        "Confirmed typed events survive JSON snapshot round-trip with split payees");

                    var snapshot = new OnlineGameSnapshotDto
                    {
                        schemaVersion = 1,
                        gameEventSequence = transportSource.Id,
                        correlationSequence = transportSource.CorrelationId,
                        gameEvents = new GameEventDto[12]
                    };
                    for (int i = 0; i < snapshot.gameEvents.Length; i++)
                        snapshot.gameEvents[i] = GameEventDto.FromGameEvent(transportSource);
                    string snapshotJson = JsonUtility.ToJson(snapshot);
                    var restoredSnapshot = JsonUtility.FromJson<OnlineGameSnapshotDto>(snapshotJson);
                    Check(restoredSnapshot != null && restoredSnapshot.schemaVersion == 1
                        && restoredSnapshot.gameEvents.Length == 12
                        && restoredSnapshot.gameEventSequence == transportSource.Id
                        && System.Text.Encoding.UTF8.GetByteCount(snapshotJson) < 32 * 1024,
                        "Bounded typed event history fits the existing snapshot payload limit");
                    var legacySnapshot = JsonUtility.FromJson<OnlineGameSnapshotDto>("{\"schemaVersion\":1}");
                    Check(legacySnapshot != null && legacySnapshot.gameEvents == null,
                        "Legacy online snapshots without typed events remain readable");

                    GameEvents.AdvanceSequences(40, 50);
                    Check(GameEvents.LastGameEventId == 40 && GameEvents.LastCorrelationId == 50
                        && GameEvents.CreateCorrelationId() == 51,
                        "Restored network sequences remain monotonic after authority recovery");
                }

                using (var filteredPresenter = new GameEventPresenter
                {
                    ShouldQueue = gameEvent => gameEvent.Kind != GameEventKind.DiceRolled
                })
                {
                    GameEvents.PublishGameEvent(GameEventPhase.Action, GameEventKind.DiceRolled,
                        GameEventPriority.Major, 0, valueA: 2, valueB: 3);
                    Check(filteredPresenter.History.Count == 1 && filteredPresenter.PendingCount == 0,
                        "Dock presenters can record dedicated dice-overlay events without replaying them");
                }

                using (var splitPresenter = new GameEventPresenter())
                {
                    long rentCorrelation = GameEvents.CreateCorrelationId();
                    GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                        GameEventKind.TransferConfirmed, GameEventPriority.Normal, 0,
                        targetId: 1, tileId: 4, amount: -70, reason: GameEventReason.Rent,
                        correlationId: rentCorrelation, splits: new[] { new GameEventSplit(1, 70) },
                        coalescible: true);
                    splitPresenter.TryBeginNext(out _);
                    GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                        GameEventKind.TransferConfirmed, GameEventPriority.Normal, 0,
                        targetId: 2, tileId: 4, amount: -30, reason: GameEventReason.Rent,
                        correlationId: rentCorrelation, splits: new[] { new GameEventSplit(2, 30) },
                        coalescible: true);
                    Check(splitPresenter.Current != null && splitPresenter.Current.Amount == -100
                        && splitPresenter.Current.Splits.Count == 2 && splitPresenter.PendingCount == 0,
                        "split rent receipts merge into the active presentation beat");
                }
            }
            finally
            {
                GameEvents.OnGameEventPublished = null;
            }
            Debug.Log($"GAME EVENT REGRESSION PASS ({assertions} assertions)");
        }
    }
}
