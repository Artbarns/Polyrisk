using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyRisk.UI
{
    /// <summary>
    /// Réglages UI communs et peu coûteux : safe areas, focus clavier, tooltips et
    /// préférence de mouvement réduit. Aucun Update et aucune mesure de layout continue.
    /// </summary>
    public static class UIUXRuntime
    {
        private const string PreparedRootClass = "ux-runtime-root";
        public static bool ReducedMotion => PlayerPrefs.GetInt("PolyRisk.ReduceMotion", 0) == 1;

        public static void SetReducedMotion(bool reduced)
        {
            PlayerPrefs.SetInt("PolyRisk.ReduceMotion", reduced ? 1 : 0);
            PlayerPrefs.Save();
            // The menu, HUD and modal documents can already be mounted when the
            // preference changes. Update them once, without a per-frame search.
            foreach (var document in UnityEngine.Object.FindObjectsOfType<UIDocument>())
                document.rootVisualElement?.EnableInClassList("ux-reduced-motion", reduced);
        }

        public static void PrepareRoot(VisualElement root)
        {
            if (root == null || root.ClassListContains(PreparedRootClass)) return;
            root.AddToClassList(PreparedRootClass);
            root.EnableInClassList("ux-reduced-motion", ReducedMotion);
            root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea(root));
            root.schedule.Execute(() => ApplySafeArea(root));
        }

        public static void PrepareView(VisualElement view, Action closeAction = null)
        {
            if (view == null) return;

            view.Query<Button>().ForEach(button =>
            {
                button.focusable = true;
                button.AddToClassList("ux-interactive");
                if (button.ClassListContains("btn-close") && string.IsNullOrEmpty(button.tooltip))
                    button.tooltip = "Close";
            });

            if (closeAction != null && view.Q<Button>(className: "btn-close") != null)
            {
                view.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode != KeyCode.Escape) return;
                    evt.StopPropagation();
                    closeAction();
                });
            }
        }

        private static void ApplySafeArea(VisualElement root)
        {
            if (root == null || Screen.width <= 0 || Screen.height <= 0) return;
            float width = root.resolvedStyle.width;
            float height = root.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height) || width <= 0 || height <= 0) return;

            // UI Toolkit reports logical panel units here, after PanelSettings scaling.
            // A class switch on geometry changes is enough for responsive landscape
            // layouts and avoids polling Screen dimensions every frame.
            root.EnableInClassList("ux-compact-height", height <= 430f);
            root.EnableInClassList("ux-low-height", height <= 340f);
            root.EnableInClassList("ux-narrow", Screen.width <= 520 || width <= 480f);

            Rect safe = Screen.safeArea;
            root.style.paddingLeft = width * safe.xMin / Screen.width;
            root.style.paddingRight = width * (Screen.width - safe.xMax) / Screen.width;
            root.style.paddingBottom = height * safe.yMin / Screen.height;
            root.style.paddingTop = height * (Screen.height - safe.yMax) / Screen.height;
        }
    }
}
