using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PolyRisk.Core
{
    public enum GameEventPhase { Turn, Action, Resolution, Network, Endgame }
    public enum GameEventKind
    {
        TurnStarted, DiceRolled, PawnArrived, PropertyPurchased, PropertyPassed,
        TransferConfirmed, TaxPaid, IncomeReceived, UpgradeApplied, PowerApplied,
        VentureResolved, AuctionOffer, AuctionResolved, MarketplaceResolved,
        InsolvencyOpened, Bankruptcy, Victory, Waiting, NetworkConfirmed, Other
    }
    public enum GameEventPriority { Ambient, Normal, Major, Critical }
    public enum GameEventReason
    {
        None, DiceRoll, PropertyPurchase, Rent, Tax, Transit, Salary, Upgrade,
        FamilyPower, VentureCard, Auction, HostileTakeover, Investment, Casino,
        BankruptcySettlement, NetworkCommand, VictoryCondition, Other
    }
    public enum GamePresentationBeat { Announcement, Anticipation, Impact, Transfer, Trace }

    public readonly struct GameEventSplit
    {
        public int PayeeId { get; }
        public double Amount { get; }

        public GameEventSplit(int payeeId, double amount)
        {
            PayeeId = payeeId;
            Amount = amount;
        }
    }

    /// <summary>
    /// Immutable presentation fact emitted only after its game action is confirmed.
    /// Player/tile IDs use -1 when there is no player/tile target (e.g. the bank).
    /// Amount is signed from the actor's perspective; split amounts are positive payee receipts.
    /// </summary>
    public sealed class GameEvent
    {
        private readonly ReadOnlyCollection<GameEventSplit> splits;

        public long Id { get; }
        public long CorrelationId { get; }
        public GameEventPhase Phase { get; }
        public GameEventKind Kind { get; }
        public GameEventPriority Priority { get; }
        public int ActorId { get; }
        public int TargetId { get; }
        public int TileId { get; }
        public double Amount { get; }
        public GameEventReason Reason { get; }
        public int ReasonParameterId { get; }
        public double ReasonParameterValue { get; }
        public int ValueA { get; }
        public int ValueB { get; }
        public bool Coalescible { get; }
        public int MergedCount { get; }
        public IReadOnlyList<GameEventSplit> Splits => splits;

        internal GameEvent(long id, long correlationId, GameEventPhase phase,
            GameEventKind kind, GameEventPriority priority, int actorId, int targetId,
            int tileId, double amount, GameEventReason reason, int reasonParameterId,
            double reasonParameterValue, int valueA, int valueB,
            IEnumerable<GameEventSplit> eventSplits, bool coalescible, int mergedCount = 1)
        {
            Id = id;
            CorrelationId = correlationId;
            Phase = phase;
            Kind = kind;
            Priority = priority;
            ActorId = actorId;
            TargetId = targetId;
            TileId = tileId;
            Amount = amount;
            Reason = reason;
            ReasonParameterId = reasonParameterId;
            ReasonParameterValue = reasonParameterValue;
            ValueA = valueA;
            ValueB = valueB;
            Coalescible = coalescible;
            MergedCount = Math.Max(1, mergedCount);
            var copy = eventSplits == null ? new List<GameEventSplit>() : new List<GameEventSplit>(eventSplits);
            splits = copy.AsReadOnly();
        }

        internal bool CanCoalesceWith(GameEvent other)
        {
            return other != null && Coalescible && other.Coalescible
                && Priority <= GameEventPriority.Normal && Priority == other.Priority
                && CorrelationId == other.CorrelationId && Kind == other.Kind
                && Phase == other.Phase && ActorId == other.ActorId
                && TileId == other.TileId && Reason == other.Reason
                && ReasonParameterId == other.ReasonParameterId
                && ReasonParameterValue.Equals(other.ReasonParameterValue)
                && ValueA == other.ValueA && ValueB == other.ValueB;
        }

        internal GameEvent MergeForPresentation(GameEvent other)
        {
            if (!CanCoalesceWith(other)) throw new InvalidOperationException("Events are not coalescible.");
            var mergedSplits = new Dictionary<int, double>();
            foreach (var split in splits) mergedSplits[split.PayeeId] = mergedSplits.TryGetValue(split.PayeeId, out var current) ? current + split.Amount : split.Amount;
            foreach (var split in other.splits) mergedSplits[split.PayeeId] = mergedSplits.TryGetValue(split.PayeeId, out var current) ? current + split.Amount : split.Amount;
            var resultSplits = new List<GameEventSplit>(mergedSplits.Count);
            foreach (var pair in mergedSplits) resultSplits.Add(new GameEventSplit(pair.Key, pair.Value));
            int mergedTargetId = TargetId == other.TargetId ? TargetId : -1;
            return new GameEvent(Id, CorrelationId, Phase, Kind, Priority, ActorId, mergedTargetId,
                TileId, Amount + other.Amount, Reason, ReasonParameterId,
                ReasonParameterValue + other.ReasonParameterValue, ValueA, ValueB,
                resultSplits, true, MergedCount + other.MergedCount);
        }
    }
}
