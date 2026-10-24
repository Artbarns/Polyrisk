using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine.UIElements;
#endif

namespace PolyRisk.DebugTools
{
    /// <summary>Development-only F10 toggle for unretouched artistic inspection.</summary>
    public sealed class GraphiteVisualIsolation : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        readonly Dictionary<VisualElement, bool> previousVisibility = new Dictionary<VisualElement, bool>();
        bool isolateOnLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureExists()
        {
            if (Application.isBatchMode || FindObjectOfType<GraphiteVisualIsolation>() != null) return;
            var host = new GameObject("[QA] Graphite visual isolation");
            DontDestroyOnLoad(host);
            host.AddComponent<GraphiteVisualIsolation>();
        }

        void Start()
        {
            System.Uri url;
            isolateOnLoad = System.Uri.TryCreate(Application.absoluteURL, System.UriKind.Absolute, out url)
                && System.Array.IndexOf(url.Query.TrimStart('?').Split('&'), "graphiteIsolation=1") >= 0;
        }

        void Update()
        {
            if (isolateOnLoad && HideBadges()) isolateOnLoad = false;
            if (!Input.GetKeyDown(KeyCode.F10)) return;
            if (previousVisibility.Count > 0) Restore();
            else HideBadges();
        }

        bool HideBadges()
        {
            foreach (var document in FindObjectsOfType<UIDocument>())
            {
                var overlay = document.rootVisualElement?.Q<VisualElement>("BoardTileYieldOverlay");
                if (overlay == null || previousVisibility.ContainsKey(overlay)) continue;
                previousVisibility.Add(overlay, overlay.visible);
                overlay.visible = false;
            }
            if (previousVisibility.Count == 0) return false;
            Debug.Log("GRAPHITE ISOLATION: badges hidden for visual QA only.");
            return true;
        }

        void OnDisable() => Restore();

        void Restore()
        {
            foreach (var entry in previousVisibility) entry.Key.visible = entry.Value;
            previousVisibility.Clear();
            Debug.Log("GRAPHITE ISOLATION: badge visibility restored.");
        }
#endif
    }
}
