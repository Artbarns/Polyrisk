using UnityEngine;

namespace PolyRisk.UI
{
    /// <summary>
    /// Icônes/UI refonte — l'inspection de tuile a été absorbée dans
    /// ActionButtonsWidget.cs (UpdateInspectorSection), qui héberge déjà le même
    /// GameObject (BottomInspector_Panel) et le même déclencheur (GameEvents.OnTileSelected).
    /// Un seul contrôleur, une seule frame de rafraîchissement.
    ///
    /// On conserve la classe (vide) pour ne pas casser la référence du GameObject
    /// existant dans la scène — même patron que AutoPrecisionWidget.cs (Phase E.6).
    /// Peut être supprimée définitivement après nettoyage des scènes (Phase F).
    /// </summary>
    [System.Obsolete("Inspection de tuile absorbée dans ActionButtonsWidget.UpdateInspectorSection().")]
    public class InspectorWidget : MonoBehaviour
    {
        // Vide — placeholder pour compatibilité scène
    }
}
