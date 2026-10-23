using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        private static readonly Dictionary<PanelSettings, PanelSettings> gamePanels = new Dictionary<PanelSettings, PanelSettings>();
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern float PolyRiskCanvasCssWidth();
        [DllImport("__Internal")] private static extern float PolyRiskSafeInset(int edge);
#endif

        public static void PrepareDocument(UIDocument document)
        {
            if (document == null || document.panelSettings == null) return;
            var source = document.panelSettings;
            if (gamePanels.ContainsValue(source)) return;
            if (!gamePanels.TryGetValue(source, out var settings))
            {
                settings = UnityEngine.Object.Instantiate(source);
                settings.name = source.name + " (Pulse runtime)";
#if UNITY_WEBGL || UNITY_EDITOR
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
#else
                settings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
                settings.referenceDpi = 160;
                settings.fallbackDpi = 160;
#endif
                settings.sortingOrder = source.sortingOrder - 1;
                gamePanels.Add(source, settings);
            }
            RefreshPanelScale(settings);
            document.panelSettings = settings;
            document.rootVisualElement.RegisterCallback<GeometryChangedEvent>(_ => RefreshPanelScale(settings));
        }

        private static void RefreshPanelScale(PanelSettings settings)
        {
            float scale = 1;
#if UNITY_WEBGL && !UNITY_EDITOR
            float cssWidth = PolyRiskCanvasCssWidth();
            if (cssWidth > 0) scale = Screen.width / cssWidth;
#endif
            if (!Mathf.Approximately(settings.scale, scale)) settings.scale = scale;
        }

        public static void FitBoardPresentation(VisualElement root, Rect viewport)
        {
            var stage = root?.Q<VisualElement>("PulseCenterStage");
            if (stage?.parent == null || root.worldBound.width <= 0 || root.worldBound.height <= 0) return;
            var parent = stage.parent.worldBound;
            float sx = root.worldBound.width / Screen.width, sy = root.worldBound.height / Screen.height;
            stage.style.left = viewport.xMin * sx - (parent.xMin - root.worldBound.xMin);
            stage.style.top = (Screen.height - viewport.yMax) * sy - (parent.yMin - root.worldBound.yMin);
            stage.style.width = viewport.width * sx;
            stage.style.height = viewport.height * sy;
            stage.style.right = StyleKeyword.Auto;
            stage.style.bottom = StyleKeyword.Auto;
        }

        private static Rect cachedSafeArea;
        public static Rect SafeArea => cachedSafeArea.width > 0 ? cachedSafeArea : Screen.safeArea;

        public static Rect IntersectSafeInsets(Rect nativeSafe, float screenWidth, float screenHeight, Vector4 insets)
        {
            return Rect.MinMaxRect(Mathf.Max(nativeSafe.xMin, insets.x), Mathf.Max(nativeSafe.yMin, insets.y),
                Mathf.Min(nativeSafe.xMax, screenWidth - insets.z), Mathf.Min(nativeSafe.yMax, screenHeight - insets.w));
        }

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
            bool wideLandscape = width > height && height > 600f;
            root.EnableInClassList("ux-wide-landscape", wideLandscape);
            var tileInspector = root.Q<VisualElement>("Panel_TileDetail");
            tileInspector?.EnableInClassList("ux-wide-landscape-inspector", wideLandscape);
            bool shortLandscape = width > height && height <= 600f;
            tileInspector?.EnableInClassList("ux-short-landscape-inspector", shortLandscape);
            root.EnableInClassList("ux-narrow", width <= 480f);
            root.EnableInClassList("ux-extra-narrow", width <= 370f);
            root.EnableInClassList("ux-small-portrait", width < height && height < 760f);

            Rect safe = Screen.safeArea;
#if UNITY_WEBGL && !UNITY_EDITOR
            float cssWidth = PolyRiskCanvasCssWidth();
            float pixelScale = cssWidth > 0 ? Screen.width / cssWidth : 1;
            safe = IntersectSafeInsets(safe, Screen.width, Screen.height,
                new Vector4(PolyRiskSafeInset(0), PolyRiskSafeInset(1), PolyRiskSafeInset(2), PolyRiskSafeInset(3)) * pixelScale);
#endif
            cachedSafeArea = safe;
            // These two roots are absolute; padding on their parent does not inset them.
            float left = width * safe.xMin / Screen.width;
            float right = width * (Screen.width - safe.xMax) / Screen.width;
            float top = height * (Screen.height - safe.yMax) / Screen.height;
            float bottom = height * safe.yMin / Screen.height;
            var hud = root.Q<VisualElement>("TopHUD_Bar");
            if (hud != null) { hud.style.left = left; hud.style.right = right; hud.style.top = top; }
            if (tileInspector != null)
            {
                root.pickingMode = PickingMode.Ignore;
                tileInspector.pickingMode = PickingMode.Ignore;
                tileInspector.style.left = left; tileInspector.style.right = right;
                tileInspector.style.top = top; tileInspector.style.bottom = bottom;
                tileInspector.style.width = StyleKeyword.Auto; tileInspector.style.height = StyleKeyword.Auto;
            }
            root.style.paddingLeft = width * safe.xMin / Screen.width;
            root.style.paddingRight = width * (Screen.width - safe.xMax) / Screen.width;
            root.style.paddingBottom = height * safe.yMin / Screen.height;
            root.style.paddingTop = height * (Screen.height - safe.yMax) / Screen.height;
        }
    }
}
