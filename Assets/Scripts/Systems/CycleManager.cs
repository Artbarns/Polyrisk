using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    public enum CycleType { NORMAL, BOOM, CRASH, WAR }

    /// <summary>
    /// Phase C — Gestion des cycles macro-économiques (GDD_Economy §7).
    /// BOOM/CRASH déclenchés par Venture #20. WAR déclenchée par Venture #11.
    /// Décrémenté à chaque tour complet (round-robin = 1 tour de cycle).
    /// </summary>
    public class CycleManager : MonoBehaviour
    {
        public static CycleManager Instance { get; private set; }

        [Header("État du cycle")]
        [SerializeField] private CycleType currentCycle = CycleType.NORMAL;
        [SerializeField] private int turnsRemaining = 0;

        public CycleType CurrentCycle => currentCycle;
        public int TurnsRemaining => turnsRemaining;
        public bool IsActive => currentCycle != CycleType.NORMAL && turnsRemaining > 0;

        public delegate void CycleChangedHandler(CycleType newCycle, int duration);
        public event CycleChangedHandler OnCycleChanged;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CONTRÔLE
        // ─────────────────────────────────────────────────────────────────────────

        public void StartCycle(CycleType type, int duration)
        {
            if (type == CycleType.NORMAL) { EndCycle(); return; }
            currentCycle   = type;
            turnsRemaining = Mathf.Max(1, duration);
            Debug.Log($"<color=yellow>[CycleManager] Cycle {type} démarré pour {turnsRemaining} tours.</color>");
            SyncToFinanceSystem();
            OnCycleChanged?.Invoke(currentCycle, turnsRemaining);
        }

        public void EndCycle()
        {
            if (currentCycle == CycleType.NORMAL) return;
            Debug.Log($"<color=yellow>[CycleManager] Cycle {currentCycle} terminé. Retour NORMAL.</color>");
            currentCycle   = CycleType.NORMAL;
            turnsRemaining = 0;
            SyncToFinanceSystem();
            OnCycleChanged?.Invoke(currentCycle, 0);
        }

        /// <summary>Propage l'état du cycle au FinanceSystem pour le TopHUD.</summary>
        private void SyncToFinanceSystem()
        {
            var fs = FindObjectOfType<FinanceSystem>();
            if (fs == null) return;
            fs.CurrentMarketState = currentCycle.ToString();
        }

        /// <summary>
        /// Appelé une fois par tour complet (round-robin) depuis TurnManager.
        /// </summary>
        public void TickOnTurnEnd()
        {
            if (currentCycle == CycleType.NORMAL) return;
            turnsRemaining--;
            if (turnsRemaining <= 0) EndCycle();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // MODIFICATEURS GLOBAUX — GDD_Economy §7
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Multiplicateur appliqué aux loyers reçus (toutes familles).</summary>
        public double GetRentMultiplier() => currentCycle switch
        {
            CycleType.BOOM  => 1.30,
            CycleType.CRASH => 0.60,
            _               => 1.00
        };

        /// <summary>Multiplicateur appliqué au Burn Rate au passage GO.</summary>
        public double GetBurnRateMultiplier() => currentCycle switch
        {
            CycleType.BOOM  => 1.20,
            CycleType.CRASH => 1.00, // burn standard pendant CRASH (juste les revenus baissent)
            _               => 1.00
        };

        /// <summary>Multiplicateur appliqué à Sgo et aux dividendes au passage GO.</summary>
        public double GetSgoMultiplier() => currentCycle switch
        {
            CycleType.BOOM  => 1.30,
            CycleType.CRASH => 0.60,
            _               => 1.00
        };

        /// <summary>
        /// Shift additif appliqué aux bornes ROI du Stock Market (en points).
        /// BOOM : +0.20 sur min+max. CRASH : −0.20 sur min, max plus volatil.
        /// </summary>
        public float GetStockShiftMin() => currentCycle switch
        {
            CycleType.BOOM  => +0.20f,
            CycleType.CRASH => -0.20f,
            _               => 0f
        };
        public float GetStockShiftMax() => currentCycle switch
        {
            CycleType.BOOM  => +0.20f,
            CycleType.CRASH => -0.28f,
            _               => 0f
        };
        public float GetStockTotalLossMultiplier() => currentCycle == CycleType.CRASH ? 1.75f : 1.0f;

        // ─────────────────────────────────────────────────────────────────────────
        // MODIFICATEURS SPÉCIAUX — WAR
        // ─────────────────────────────────────────────────────────────────────────

        public bool IsWar => currentCycle == CycleType.WAR;

        /// <summary>Multiplicateur loyer spécifique à une famille pendant WAR.</summary>
        public double GetWarFamilyMultiplier(PropertyData property)
        {
            if (!IsWar || property?.FamilyData == null) return 1.0;
            string fn = property.FamilyData.FamilyName;
            if (fn.Contains("Armement")) return 2.00;
            if (fn.Contains("Luxe"))     return 0.50;
            return 1.00;
        }

        /// <summary>Pendant WAR, les Hubs ne génèrent plus de loyer (mais Infrastructure Tax due).</summary>
        public bool ShouldZeroHubRent() => IsWar;
    }
}
