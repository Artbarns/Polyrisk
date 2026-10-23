using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.5 — Helpers pour les transitions UI Toolkit du projet.
    /// Pattern utilisé : on ajoute une classe "panel-enter-from" qui pose l'état initial
    /// (translated/scaled/opacity 0), puis on la retire au frame suivant pour déclencher
    /// la transition CSS définie dans Theme.uss.
    /// </summary>
    public static class UIToolkitTransitions
    {
        /// <summary>Animation d'entrée bottom-sheet (slide-up + fade).</summary>
        public static void SlideUpEnter(MonoBehaviour host, VisualElement target,
            Action onComplete = null, float durationSeconds = 0.28f)
        {
            if (target == null) return;
            if (UIUXRuntime.ReducedMotion) { onComplete?.Invoke(); return; }
            target.AddToClassList("panel-enter-from");
            host.StartCoroutine(RemoveClassNextFrame(target, "panel-enter-from", durationSeconds, onComplete));
        }

        /// <summary>Animation de sortie bottom-sheet (slide-down + fade).</summary>
        public static void SlideDownExit(MonoBehaviour host, VisualElement target,
            Action onComplete = null, float durationSeconds = 0.20f)
        {
            if (target == null) return;
            if (UIUXRuntime.ReducedMotion) { onComplete?.Invoke(); return; }
            target.AddToClassList("panel-exit-to");
            host.StartCoroutine(WaitAndCallback(durationSeconds, () =>
            {
                target.RemoveFromClassList("panel-exit-to");
                onComplete?.Invoke();
            }));
        }

        /// <summary>Animation d'entrée modale centrée (scale + fade).</summary>
        public static void ScaleInEnter(MonoBehaviour host, VisualElement target,
            Action onComplete = null, float durationSeconds = 0.24f)
        {
            if (target == null) return;
            if (UIUXRuntime.ReducedMotion) { onComplete?.Invoke(); return; }
            target.AddToClassList("modal-enter-from");
            host.StartCoroutine(RemoveClassNextFrame(target, "modal-enter-from", durationSeconds, onComplete));
        }

        /// <summary>Animation d'entrée fullscreen overlay (fade global).</summary>
        public static void FadeInOverlay(MonoBehaviour host, VisualElement target,
            Action onComplete = null, float durationSeconds = 0.35f)
        {
            if (target == null) return;
            if (UIUXRuntime.ReducedMotion) { onComplete?.Invoke(); return; }
            target.AddToClassList("modal-enter-from");
            host.StartCoroutine(RemoveClassNextFrame(target, "modal-enter-from", durationSeconds, onComplete));
        }

        // ─────────────────────────────────────────────────────────────────────

        private static IEnumerator RemoveClassNextFrame(VisualElement target, string className,
            float durationSeconds, Action onComplete)
        {
            // Attendre 2 frames pour que UI Toolkit pose le style initial avant qu'on le retire
            yield return null;
            yield return null;
            target.RemoveFromClassList(className);

            if (onComplete != null)
            {
                yield return new WaitForSecondsRealtime(durationSeconds);
                onComplete.Invoke();
            }
        }

        private static IEnumerator WaitAndCallback(float seconds, Action callback)
        {
            yield return new WaitForSecondsRealtime(seconds);
            callback?.Invoke();
        }
    }
}
