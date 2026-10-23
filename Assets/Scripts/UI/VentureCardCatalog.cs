using System.Collections.Generic;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.2 — Catalogue statique des 20 cartes Venture "Cruel World 2026".
    /// Titre + description + type pour l'affichage UI.
    ///
    /// Fix #8 Phase F — RÉALIGNÉ sur les effets réels codés dans VentureSystem.ApplyCardEffect.
    /// Les textes précédents décrivaient les intentions GDD, pas ce que le code exécute.
    /// L'implémentation complète des effets GDD (choix Ransomware, Scandale sans Sgo, etc.)
    /// est un backlog Phase F séparé.
    /// </summary>
    public enum VentureCardType { Positive, Negative, Chaos }

    public class VentureCardInfo
    {
        public int Id;
        public string Title;
        public string Description;
        public VentureCardType Type;
    }

    public static class VentureCardCatalog
    {
        private static readonly Dictionary<int, VentureCardInfo> Cards = new()
        {
            // ── POSITIVES ────────────────────────────────────────────────────
            { 1,  new VentureCardInfo { Id=1,  Type=VentureCardType.Positive, Title="HÉRITAGE OFFSHORE",
                Description="Vous recevez 5 × Sgo d'un trust offshore." } },
            { 5,  new VentureCardInfo { Id=5,  Type=VentureCardType.Positive, Title="BULL MARKET",
                Description="Le marché s'emballe : Sgo global augmente de +20% de façon permanente." } },
            { 6,  new VentureCardInfo { Id=6,  Type=VentureCardType.Positive, Title="GOVERNMENT BAILOUT",
                Description="Si votre cash est inférieur à 1 × Sgo, l'État vous verse une aide de 5 × Sgo." } },
            { 7,  new VentureCardInfo { Id=7,  Type=VentureCardType.Positive, Title="MOON MINING IPO",
                Description="Vous recevez 0.2 × Sgo par propriété Aerospace ou Tech possédée." } },
            { 8,  new VentureCardInfo { Id=8,  Type=VentureCardType.Positive, Title="DON DE SANG DE STAGIAIRE",
                Description="Vous obtenez une carte 'Sortie d'Audit Fiscal' à usage unique." } },
            { 9,  new VentureCardInfo { Id=9,  Type=VentureCardType.Positive, Title="QUANTITATIVE EASING",
                Description="Sgo global augmente de +5% de façon permanente." } },
            { 10, new VentureCardInfo { Id=10, Type=VentureCardType.Positive, Title="GOLDEN TICKET",
                Description="Vous obtenez un Golden Ticket : la prochaine Sanction Hostile ciblant vos actifs est annulée." } },

            // ── NEGATIVES ────────────────────────────────────────────────────
            { 2,  new VentureCardInfo { Id=2,  Type=VentureCardType.Negative, Title="KIDNAPPING EXPRESS",
                Description="Vous êtes téléporté sur une case aléatoire du plateau. Sa règle s'applique immédiatement." } },
            { 3,  new VentureCardInfo { Id=3,  Type=VentureCardType.Negative, Title="ARRESTATION ARBITRAIRE",
                Description="Vous êtes envoyé directement en Audit Fiscal (via la case Direction Audit)." } },
            { 4,  new VentureCardInfo { Id=4,  Type=VentureCardType.Negative, Title="CYBERATTACK",
                Description="Une de vos propriétés upgradées perd un niveau (choisie aléatoirement)." } },
            { 11, new VentureCardInfo { Id=11, Type=VentureCardType.Negative, Title="EPSTEIN'S FLIGHT LOGS",
                Description="Vous payez 3 × Sgo de frais d'avocats. Un cycle WAR est déclenché pour 5 tours." } },
            { 12, new VentureCardInfo { Id=12, Type=VentureCardType.Negative, Title="THE MESSY DIVORCE",
                Description="Vous perdez 20% de votre capital cash actuel." } },
            { 13, new VentureCardInfo { Id=13, Type=VentureCardType.Negative, Title="DATA BREACH",
                Description="Vous payez 0.5 × Sgo à chaque adversaire non-failli." } },
            { 14, new VentureCardInfo { Id=14, Type=VentureCardType.Negative, Title="RANSOMWARE ATTACK",
                Description="Vous payez immédiatement une rançon de 3 × Sgo." } },
            { 15, new VentureCardInfo { Id=15, Type=VentureCardType.Negative, Title="WATER SCARCITY ACT",
                Description="Votre Burn Rate sera doublé lors de votre prochain passage GO." } },
            { 16, new VentureCardInfo { Id=16, Type=VentureCardType.Negative, Title="SCANDALE DE LA PETITE ÎLE",
                Description="Vous êtes envoyé directement en Audit Fiscal." } },
            { 17, new VentureCardInfo { Id=17, Type=VentureCardType.Negative, Title="ABONNEMENT OXYGÈNE PREMIUM",
                Description="Vous payez 0.1 × Sgo par niveau d'upgrade total de votre parc." } },
            { 18, new VentureCardInfo { Id=18, Type=VentureCardType.Negative, Title="MARÉE NOIRE INSTAGRAMMABLE",
                Description="Vos revenus Oil & Gas sont divisés par 2 pendant 3 tours." } },

            // ── CHAOS ────────────────────────────────────────────────────────
            { 19, new VentureCardInfo { Id=19, Type=VentureCardType.Chaos, Title="OPA HOSTILE",
                Description="Vous rachetez une propriété adverse abordable au hasard au prix le plus élevé entre 150% du prix d'achat et le prix d'achat + 75% des upgrades payés." } },
            { 20, new VentureCardInfo { Id=20, Type=VentureCardType.Chaos, Title="EFFONDREMENT GLOBAL",
                Description="Tirage aléatoire 50/50 : cycle BOOM ou CRASH mondial pour 5 tours." } },
        };

        public static VentureCardInfo GetInfo(int cardId)
        {
            return Cards.TryGetValue(cardId, out var info) ? info :
                new VentureCardInfo { Id = cardId, Type = VentureCardType.Positive,
                    Title = $"VENTURE #{cardId}", Description = "Effet personnalisé." };
        }
    }
}
