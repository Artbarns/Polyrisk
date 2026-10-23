using System.Collections.Generic;
using UnityEngine;

namespace PolyRisk.Core
{
    /// <summary>
    /// Phase D — Profil psychologique IA. Détermine la stratégie de décision.
    /// None = humain ou IA "neutre" (legacy). Predator = agressif. Rentier = défensif.
    /// Negotiator = orienté échange/sur-enchère.
    /// </summary>
    public enum PsychoProfile { None, Predator, Rentier, Negotiator }

    /// <summary>
    /// Phase E.1 — Dark Board : statut de richesse anonymisé d'un joueur.
    /// Affiché aux adversaires à la place du cash exact. Seuils indexés sur Sgo
    /// (GDD_Economy §8). Couleurs : LOW gris, MID bleu, HIGH ambre, MOGUL or.
    /// </summary>
    public enum WealthStatus { LOW, MID, HIGH, MOGUL }

    /// <summary>
    /// Helpers Dark Board — pures fonctions statiques, pas de state.
    /// </summary>
    public static class WealthStatusHelper
    {
        /// <summary>Calcule le WealthStatus d'un joueur à partir de son cash et du Sgo courant.</summary>
        public static WealthStatus GetStatus(PlayerModel player, double currentSgo)
        {
            if (player == null || currentSgo <= 0) return WealthStatus.LOW;
            double cash = player.CurrentBalance;
            if (cash > 50 * currentSgo) return WealthStatus.MOGUL;
            if (cash > 20 * currentSgo) return WealthStatus.HIGH;
            if (cash >  5 * currentSgo) return WealthStatus.MID;
            return WealthStatus.LOW;
        }

        /// <summary>Couleur hex officielle (sans #).</summary>
        public static string GetHexColor(WealthStatus status) => status switch
        {
            WealthStatus.LOW   => "8A8F9A",   // gris froid
            WealthStatus.MID   => "3B82F6",   // bleu signal
            WealthStatus.HIGH  => "F59E0B",   // ambre / orange
            WealthStatus.MOGUL => "FFD700",   // or
            _                  => "8A8F9A"
        };

        /// <summary>UnityEngine.Color équivalent.</summary>
        public static UnityEngine.Color GetUnityColor(WealthStatus status)
        {
            UnityEngine.ColorUtility.TryParseHtmlString("#" + GetHexColor(status), out var c);
            return c;
        }

        /// <summary>Libellé affichable.</summary>
        public static string GetLabel(WealthStatus status) => status.ToString();
    }

    /// <summary>
    /// Represents the core state of a player in the game.
    /// Updated to support 'double' for currency and AI identification.
    /// </summary>
    [System.Serializable]
    public class PlayerModel
    {
        /// <summary>
        /// The unique ID of the player.
        /// </summary>
        public int PlayerId { get; private set; }

        /// <summary>
        /// The current amount of money the player has (updated to double).
        /// </summary>
        public double CurrentBalance { get; private set; }

        /// <summary>
        /// Indicates if the player is bankrupt and out of the game.
        /// </summary>
        public bool IsBankrupt { get; set; } = false;

        /// <summary>
        /// Indicates if this player is controlled by AI.
        /// </summary>
        public bool IsAI { get; private set; }

        /// <summary>
        /// The list of properties currently owned by the player.
        /// </summary>
        private List<PropertyData> ownedProperties;

        /// <summary>
        /// Public read-only access to owned properties.
        /// </summary>
        public IReadOnlyList<PropertyData> OwnedProperties => ownedProperties.AsReadOnly();

        /// <summary>
        /// Indicates if the player is currently in Audit Fiscal (Prison).
        /// </summary>
        public bool IsInAudit { get; set; } = false;

        /// <summary>
        /// Number of turns remaining in Audit.
        /// </summary>
        public int AuditTurnsRemaining { get; set; } = 0;

        /// <summary>
        /// Number of full laps completed around the board.
        /// </summary>
        public int LapsCompleted { get; set; } = 0;
        public int TurnsStarted { get; set; } = 0;

        // Venture Card Effects Flags
        public bool HasGetOutOfJailCard { get; set; } = false;
        public bool HasGoldenTicket { get; set; } = false;
        public bool DoubleBurnRateNextGo { get; set; } = false;
        public int OilGasPenaltyTurns { get; set; } = 0;
        public bool HasGetOutOfAuditCard { get; set; } = false;
        public bool HasImmunityCard { get; set; } = false;

        // ── Phase B : Ability System Flags ───────────────────────────────────────

        // Pharma Ultime : immunité 1 tour (durée en tours restants)
        public int ImmunityTurnsRemaining { get; set; } = 0;

        // Oil & Gas Ultime : +80% loyers reçus pendant N tours
        public int OilGasBoostTurnsRemaining { get; set; } = 0;

        // Automobile Ultime : relancer les dés et choisir ce tour (flag consommé dans TurnManager)
        public bool AutoTurboReady { get; set; } = false;

        // Automobile Passif : ±1 aux dés disponible ce tour (reset à chaque début de tour)
        public bool AutoPrecisionAvailable { get; set; } = false;

        // Automobile Passif : delta en attente à appliquer au prochain ExecuteMovement (-1, 0 ou +1)
        public int PendingDiceDelta { get; set; } = 0;

        // Phase D — Profil psychologique (IA uniquement)
        public PsychoProfile Profile { get; set; } = PsychoProfile.None;

        /// <summary>
        /// Constructor to initialize a new player.
        /// </summary>
        /// <param name="playerId">The unique ID for the player.</param>
        /// <param name="initialBalance">The starting money (double).</param>
        /// <param name="isAI">Whether the player is an AI agent.</param>
        public PlayerModel(int playerId, double initialBalance, bool isAI = false)
        {
            PlayerId = playerId;
            CurrentBalance = initialBalance;
            IsAI = isAI;
            ownedProperties = new List<PropertyData>();
        }

        /// <summary>
        /// Updates the player's balance.
        /// </summary>
        /// <param name="newBalance">The new balance value.</param>
        public void SetBalance(double newBalance)
        {
            CurrentBalance = newBalance;
        }

        /// <summary>
        /// Adds a property to the player's ownership list.
        /// </summary>
        public void AddProperty(PropertyData property)
        {
            if (!ownedProperties.Contains(property))
            {
                ownedProperties.Add(property);
            }
        }

        /// <summary>
        /// Removes a property from the player's ownership list.
        /// </summary>
        public void RemoveProperty(PropertyData property)
        {
            if (ownedProperties.Contains(property))
            {
                ownedProperties.Remove(property);
            }
        }

        /// <summary>
        /// Clears all properties from the player's ownership list.
        /// Used during bankruptcy.
        /// </summary>
        public void ClearProperties()
        {
            ownedProperties.Clear();
        }
    }
}
