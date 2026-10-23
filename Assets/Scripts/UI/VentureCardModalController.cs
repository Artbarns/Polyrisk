using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.2 — Affiche la modale Venture card avant l'application de l'effet.
    /// Self-spawn + UIDocument indépendant.
    /// </summary>
    public class VentureCardModalController : MonoBehaviour
    {
        public UIDocument uiDocument;
        public VisualTreeAsset ventureTemplate;

        private VisualElement root;
        private static VentureCardModalController _instance;
        public static VentureCardModalController Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureExists()
        {
            if (FindObjectOfType<VentureCardModalController>() != null) return;
            var go = new GameObject("[Auto] VentureCardModal");
            var c  = go.AddComponent<VentureCardModalController>();
            var doc = go.AddComponent<UIDocument>();
            doc.sortingOrder = 95;
            c.uiDocument = doc;
            // Fix #9 Phase F — Resources.Load pour marcher en build device (pas juste éditeur)
            c.ventureTemplate = Resources.Load<VisualTreeAsset>("VentureCardModal");

            // Réutilise les PanelSettings d'un autre UIDocument de la scène
            var anyDoc = FindObjectsOfType<UIDocument>().FirstOrDefault(d => d != doc && d.panelSettings != null);
            if (anyDoc != null && anyDoc != doc) doc.panelSettings = anyDoc.panelSettings;

            DontDestroyOnLoad(go);
        }

        private void Awake() { _instance = this; }

        /// <summary>
        /// Affiche la carte. Action invoquée quand le joueur clique CONFIRM
        /// (c'est là que VentureSystem applique l'effet réel).
        /// </summary>
        public void ShowCard(int cardId, Action onConfirm)
        {
            if (uiDocument == null || ventureTemplate == null)
            {
                Debug.LogWarning("[VentureCardModal] UIDocument/template manquant — apply effect direct.");
                onConfirm?.Invoke();
                return;
            }

            root = uiDocument.rootVisualElement;
            root.Clear();
            ventureTemplate.CloneTree(root);
            UIUXRuntime.PrepareRoot(root);
            UIUXRuntime.PrepareView(root);

            var info = VentureCardCatalog.GetInfo(cardId);
            string suffix = info.Type switch
            {
                VentureCardType.Positive => "positive",
                VentureCardType.Negative => "negative",
                VentureCardType.Chaos    => "chaos",
                _                        => "positive"
            };

            var card  = root.Q<VisualElement>("VentureCard");
            var tag   = root.Q<VisualElement>("VentureTag");
            var lblTag   = root.Q<Label>("Lbl_VentureTag");
            var lblId    = root.Q<Label>("Lbl_VentureId");
            var lblTitle = root.Q<Label>("Lbl_VentureTitle");
            var lblDesc  = root.Q<Label>("Lbl_VentureDesc");
            var btn      = root.Q<Button>("Btn_VentureConfirm");

            if (card != null)
            {
                card.RemoveFromClassList("venture-card--positive");
                card.RemoveFromClassList("venture-card--negative");
                card.RemoveFromClassList("venture-card--chaos");
                card.AddToClassList($"venture-card--{suffix}");
            }
            if (tag != null)
            {
                tag.RemoveFromClassList("venture-tag--positive");
                tag.RemoveFromClassList("venture-tag--negative");
                tag.RemoveFromClassList("venture-tag--chaos");
                tag.AddToClassList($"venture-tag--{suffix}");
            }
            if (lblTag != null)
            {
                lblTag.RemoveFromClassList("venture-tag-label--positive");
                lblTag.RemoveFromClassList("venture-tag-label--negative");
                lblTag.RemoveFromClassList("venture-tag-label--chaos");
                lblTag.AddToClassList($"venture-tag-label--{suffix}");
                lblTag.text = info.Type.ToString().ToUpperInvariant();
            }
            if (lblId != null)    lblId.text = $"VENTURE #{cardId:D2}";
            if (lblTitle != null) lblTitle.text = info.Title;
            if (lblDesc != null)  lblDesc.text = info.Description;

            if (btn != null)
            {
                btn.clicked += () =>
                {
                    root.Clear();
                    onConfirm?.Invoke();
                };
            }

            // Animation flip + scale-in
            if (card != null && !UIUXRuntime.ReducedMotion)
            {
                card.AddToClassList("card-flip-from");
                StartCoroutine(RemoveFlipNextFrames(card));
            }
        }

        private System.Collections.IEnumerator RemoveFlipNextFrames(VisualElement card)
        {
            yield return null;
            yield return null;
            card.RemoveFromClassList("card-flip-from");
        }
    }
}
