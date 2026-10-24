using System;
using System.Collections.Generic;
using PolyRisk.Core;

namespace PolyRisk.UI
{
    /// <summary>Turns confirmed event facts into concise copy; it never derives game outcomes.</summary>
    public static class GameEventTextFormatter
    {
        public static bool IsDockEvent(GameEvent gameEvent)
        {
            return gameEvent != null
                && (gameEvent.Kind == GameEventKind.PropertyPurchased
                    || gameEvent.Kind == GameEventKind.PropertyPassed
                    || gameEvent.Kind == GameEventKind.TransferConfirmed
                    || gameEvent.Kind == GameEventKind.TaxPaid
                    || gameEvent.Kind == GameEventKind.UpgradeApplied);
        }

        public static string Format(GameEvent gameEvent, int localPlayerId,
            Func<int, string> playerName, Func<int, string> tileName)
        {
            if (!IsDockEvent(gameEvent)) return string.Empty;
            string actor = ResolvePlayerName(gameEvent.ActorId, localPlayerId, playerName);
            string tile = ResolveTileName(gameEvent.TileId, tileName);

            if (gameEvent.Kind == GameEventKind.PropertyPurchased)
                return $"{actor} acquired {tile} · {FormatSigned(gameEvent.Amount)}";
            if (gameEvent.Kind == GameEventKind.PropertyPassed)
                return $"{actor} passed on {tile}";
            if (gameEvent.Kind == GameEventKind.TaxPaid)
                return $"{actor} paid {FormatSigned(gameEvent.Amount)} in tax";
            if (gameEvent.Kind == GameEventKind.UpgradeApplied)
                return $"{actor} upgraded {tile} · L{gameEvent.ValueA}→L{gameEvent.ValueB} · {FormatSigned(gameEvent.Amount)}";

            var parts = new List<string>();
            if (gameEvent.Splits.Count > 0)
            {
                foreach (var split in gameEvent.Splits)
                    parts.Add($"{ResolvePlayerName(split.PayeeId, localPlayerId, playerName)} +{CurrencyFormatter.Format(Math.Abs(split.Amount))}");
            }
            else if (gameEvent.TargetId >= 0)
            {
                parts.Add($"{ResolvePlayerName(gameEvent.TargetId, localPlayerId, playerName)} +{CurrencyFormatter.Format(Math.Abs(gameEvent.Amount))}");
            }

            string recipients = parts.Count > 0 ? string.Join(" · ", parts) : "BANK";
            string reason = gameEvent.Reason == GameEventReason.Rent ? "RENT" : "TRANSFER";
            string location = string.IsNullOrWhiteSpace(tile) ? string.Empty : " · " + tile;
            return $"{actor} {FormatSigned(gameEvent.Amount)} → {recipients} · {reason}{location}";
        }

        private static string FormatSigned(double amount)
        {
            string sign = amount < 0 ? "−" : "+";
            return sign + CurrencyFormatter.Format(Math.Abs(amount));
        }

        private static string ResolvePlayerName(int playerId, int localPlayerId, Func<int, string> resolver)
        {
            if (playerId == localPlayerId) return "YOU";
            string name = resolver != null ? resolver(playerId) : null;
            return string.IsNullOrWhiteSpace(name) ? "P" + (playerId + 1) : name.Trim();
        }

        private static string ResolveTileName(int tileId, Func<int, string> resolver)
        {
            string name = resolver != null ? resolver(tileId) : null;
            return string.IsNullOrWhiteSpace(name) ? "Tile " + (tileId + 1) : name.Trim();
        }
    }
}
