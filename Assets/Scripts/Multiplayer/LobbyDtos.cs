using System;
using System.Collections.Generic;
using PolyRisk.Core;

namespace PolyRisk.Multiplayer
{
    [Serializable]
    public class LobbyPlayerDto
    {
        public string playerId;
        public string displayName;
        public bool isHost;
        public bool isReady;
    }

    [Serializable]
    public class LobbySnapshotDto
    {
        public string code;
        public int maxPlayers;
        public string status;
        public long version;
        public string hostPlayerId;
        public LobbyPlayerDto[] players;
    }

    [Serializable]
    public class LobbySessionResponseDto
    {
        public string playerId;
        public string reconnectToken;
        public LobbySnapshotDto lobby;
    }

    [Serializable] public class CreateLobbyRequestDto { public string displayName; public int maxPlayers = 6; }
    [Serializable] public class JoinLobbyRequestDto { public string displayName; }
    [Serializable] public class SessionRequestDto { public string playerId; public string reconnectToken; }
    [Serializable] public class SetReadyRequestDto : SessionRequestDto { public bool isReady; }

    [Serializable]
    public class SubmitGameCommandRequestDto : SessionRequestDto
    {
        public long expectedStateVersion;
        public string type;
        public string payloadJson;
        public string clientCommandId;
    }

    [Serializable]
    public class PollGameCommandsRequestDto : SessionRequestDto
    {
        public long afterSequence;
    }

    [Serializable]
    public class PublishGameStateRequestDto : SessionRequestDto
    {
        public long expectedStateVersion;
        public long lastAppliedCommandSequence;
        public string payloadJson;
    }

    [Serializable]
    public class GameCommandDto
    {
        public long sequence;
        public long expectedStateVersion;
        public string playerId;
        public string type;
        public string payloadJson;
        public string createdUtc;
    }

    [Serializable]
    public class GameCommandBatchDto
    {
        public long stateVersion;
        public long latestSequence;
        public GameCommandDto[] commands;
    }

    [Serializable]
    public class GameStateDto
    {
        public long version;
        public long lastAppliedCommandSequence;
        public string payloadJson;
        public string updatedUtc;
    }

    [Serializable]
    public class OnlinePlayerStateDto
    {
        public int playerId;
        public string lobbyPlayerId;
        public string displayName;
        public double balance;
        public bool isBankrupt;
        public bool isInAudit;
        public int auditTurnsRemaining;
        public int lapsCompleted;
        public int turnsStarted;
        public int position;
        public bool hasGetOutOfAuditCard;
        public bool autoPrecisionAvailable;
        public int pendingDiceDelta;
        public float energy;
        public bool hasGetOutOfJailCard;
        public bool hasGoldenTicket;
        public bool doubleBurnRateNextGo;
        public int oilGasPenaltyTurns;
        public bool hasImmunityCard;
        public int immunityTurnsRemaining;
        public int oilGasBoostTurnsRemaining;
        public bool autoTurboReady;
    }

    [Serializable]
    public class OnlineGameSnapshotDto
    {
        public int schemaVersion = 1;
        public bool isGameOver;
        public int winnerPlayerId = -1;
        public string victoryReason;
        public int currentPlayerId;
        public string turnState;
        public int turnsStarted;
        public double currentSgo;
        public long matchStartedUtcTicks;
        public long gameEventSequence;
        public long correlationSequence;
        public string marketState;
        public OnlineCycleStateDto cycle;
        public bool hasVentureDeckState;
        public PolyRisk.Systems.VentureDeckState ventureDeck;
        public OnlineStockInvestmentDto[] investments;
        public int diceA = -1;
        public int diceB = -1;
        public int pendingDiceTotal;
        public int[] turboFirst;
        public int[] turboSecond;
        public int consecutiveDoubles;
        public bool hasMovementState;
        public PolyRisk.Systems.MovementResumeState movement;
        public bool justLandedInAudit;
        public OnlinePlayerStateDto[] players;
        public OnlinePropertyStateDto[] properties;
        public GameActivityEntry[] activities;
        public GameEventDto[] gameEvents;
        public OnlineMarketplaceStateDto marketplace;
        public int pendingVentureCardId = -1;
        public int pendingVenturePlayerId = -1;
        public long pendingVentureToken;
        public OnlineBankruptcyStateDto bankruptcy;
    }

    [Serializable]
    public sealed class GameEventSplitDto
    {
        public int payeeId;
        public double amount;

        public GameEventSplitDto() { }
        public GameEventSplitDto(int payeeId, double amount)
        {
            this.payeeId = payeeId;
            this.amount = amount;
        }
    }

    /// <summary>JSON-safe copy of a confirmed presentation fact; contains no game rules.</summary>
    [Serializable]
    public sealed class GameEventDto
    {
        public long id;
        public long correlationId;
        public GameEventPhase phase;
        public GameEventKind kind;
        public GameEventPriority priority;
        public int actorId;
        public int targetId;
        public int tileId;
        public double amount;
        public GameEventReason reason;
        public int reasonParameterId;
        public double reasonParameterValue;
        public int valueA;
        public int valueB;
        public bool coalescible;
        public int mergedCount;
        public GameEventSplitDto[] splits;

        public static GameEventDto FromGameEvent(GameEvent gameEvent)
        {
            if (gameEvent == null) return null;
            var splits = new GameEventSplitDto[gameEvent.Splits.Count];
            for (int i = 0; i < splits.Length; i++)
            {
                var split = gameEvent.Splits[i];
                splits[i] = new GameEventSplitDto(split.PayeeId, split.Amount);
            }
            return new GameEventDto
            {
                id = gameEvent.Id,
                correlationId = gameEvent.CorrelationId,
                phase = gameEvent.Phase,
                kind = gameEvent.Kind,
                priority = gameEvent.Priority,
                actorId = gameEvent.ActorId,
                targetId = gameEvent.TargetId,
                tileId = gameEvent.TileId,
                amount = gameEvent.Amount,
                reason = gameEvent.Reason,
                reasonParameterId = gameEvent.ReasonParameterId,
                reasonParameterValue = gameEvent.ReasonParameterValue,
                valueA = gameEvent.ValueA,
                valueB = gameEvent.ValueB,
                coalescible = gameEvent.Coalescible,
                mergedCount = gameEvent.MergedCount,
                splits = splits
            };
        }

        public GameEvent ToGameEvent()
        {
            if (id <= 0 || correlationId <= 0
                || !Enum.IsDefined(typeof(GameEventPhase), phase)
                || !Enum.IsDefined(typeof(GameEventKind), kind)
                || !Enum.IsDefined(typeof(GameEventPriority), priority)
                || !Enum.IsDefined(typeof(GameEventReason), reason)) return null;

            var restoredSplits = new List<GameEventSplit>();
            if (splits != null)
            {
                for (int i = 0; i < splits.Length; i++)
                {
                    var split = splits[i];
                    if (split == null || split.payeeId < 0 || double.IsNaN(split.amount)
                        || double.IsInfinity(split.amount) || split.amount < 0) return null;
                    restoredSplits.Add(new GameEventSplit(split.payeeId, split.amount));
                }
            }

            if (double.IsNaN(amount) || double.IsInfinity(amount)
                || double.IsNaN(reasonParameterValue) || double.IsInfinity(reasonParameterValue)) return null;
            return new GameEvent(id, correlationId, phase, kind, priority,
                actorId, targetId, tileId, amount, reason, reasonParameterId,
                reasonParameterValue, valueA, valueB, restoredSplits, coalescible,
                mergedCount);
        }
    }

    [Serializable]
    public class TurboChoiceCommandDto { public int choice; }

    [Serializable]
    public class DiceAdjustmentCommandDto
    {
        public int delta;
    }

    [Serializable]
    public class TransitChoiceCommandDto
    {
        public bool takeTransit;
    }

    [Serializable]
    public class StockInvestmentCommandDto
    {
        public double amount;
        public int duration;
    }

    [Serializable]
    public class OnlineCycleStateDto
    {
        public string cycleType;
        public int turnsRemaining;
    }

    [Serializable]
    public class OnlineStockInvestmentDto
    {
        public int playerId;
        public double amount;
        public int turnsRemaining;
        public int totalDuration;
    }

    [Serializable]
    public class UltimateCommandDto
    {
        public string family;
        public int targetTileIndex = -1;
    }

    [Serializable]
    public class VentureConfirmCommandDto
    {
        public long token;
    }

    [Serializable]
    public class BankruptcyCommandDto
    {
        public long token;
        public int tileIndex = -1;
    }

    [Serializable]
    public class OnlineBankruptcyStateDto
    {
        public int playerId = -1;
        public int creditorPlayerId = -1;
        public double debt;
        public long token;
        public PolyRisk.Core.DebtSettlementContext continuation;
    }

    [Serializable]
    public class MarketplaceSanctionCommandDto
    {
        public string sanction;
        public int targetPlayerId = -1;
        public int targetTileIndex = -1;
    }

    [Serializable]
    public class MarketplaceTenderCommandDto
    {
        public int targetTileIndex = -1;
        public int bidderPlayerId = -1;
        public double cashOffer;
        public int[] offeredTileIndices;
    }

    [Serializable]
    public class OnlineMarketplaceBidDto
    {
        public int playerId;
        public bool skipped;
        public double cashOffer;
        public int[] offeredTileIndices;
    }

    [Serializable]
    public class OnlineMarketplaceStateDto
    {
        public string auctionState;
        public int sellerPlayerId = -1;
        public int propertyTileIndex = -1;
        public OnlineMarketplaceBidDto[] bids;
    }

    [Serializable]
    public class OnlinePropertyStateDto
    {
        public int tileIndex;
        public int ownerPlayerId = -1;
        public int currentLevel;
        public bool isMortgaged;
        public bool mortgageExemptFromNextGo;
        public double totalUpgradeCostInvested;
        public bool wasVisitedSinceLastGo;
        public bool isEmbargoed;
        public int embargoTurnsRemaining;
        public int hijackedByPlayerId = -1;
        public int hijackTurnsRemaining;
    }

    [Serializable] public class ApiErrorDto { public string error; }
}
