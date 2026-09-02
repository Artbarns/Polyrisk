using UnityEngine;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.6 — Le widget OnGUI a été supprimé. Le passif Automobile "Assistance de Précision"
    /// reste actif via TurnManager.UseAutoPrecision(player, ±1) — l'UI sera intégrée dans le
    /// PlanningPanel UI Toolkit dans une itération future.
    ///
    /// On conserve la classe (vide) pour ne pas casser les références d'éventuels GameObjects
    /// existants dans la scène. Elle peut être supprimée définitivement après nettoyage des scènes.
    /// </summary>
    [System.Obsolete("Le widget OnGUI a été retiré (Phase E.6). Le passif Auto est exposé via TurnManager.UseAutoPrecision.")]
    public class AutoPrecisionWidget : MonoBehaviour
    {
        // Vide — placeholder pour compatibilité scène
    }
}
