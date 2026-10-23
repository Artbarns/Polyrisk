using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace PolyRisk.UI
{
    public sealed class PulseSheetMotion
    {
        private readonly VisualElement panel, details;
        private ValueAnimation<StyleValues> animation;
        private bool expanded, measuring;
        private float startHeight;
        private int generation;

        public PulseSheetMotion(VisualElement panel, VisualElement details)
        {
            this.panel = panel;
            this.details = details;
            details.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            details.RegisterCallback<DetachFromPanelEvent>(_ => Stop());
        }

        public void SetExpanded(bool value, bool animate)
        {
            if (value == expanded && !measuring && animation == null) return;
            startHeight = details.resolvedStyle.display == DisplayStyle.None ? 0 : details.resolvedStyle.height;
            Stop();
            expanded = value;
            if (!animate || UIUXRuntime.ReducedMotion || details.panel == null)
            {
                ApplyFinalState();
                return;
            }
            panel.AddToClassList("pulse-panel--expanded");
            details.AddToClassList("pulse-details--visible");
            if (!value)
            {
                AnimateTo(0);
                return;
            }
            measuring = true;
            details.style.height = StyleKeyword.Auto;
            details.MarkDirtyRepaint();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.height <= 0) return;
            if (measuring)
            {
                measuring = false;
                AnimateTo(evt.newRect.height);
            }
            else if (expanded && animation == null && !UIUXRuntime.ReducedMotion
                && evt.oldRect.height > 0 && Mathf.Abs(evt.newRect.height - evt.oldRect.height) > 2)
            {
                startHeight = evt.oldRect.height;
                AnimateTo(evt.newRect.height);
            }
        }

        private void AnimateTo(float height)
        {
            int sequence = generation;
            animation = details.experimental.animation.Start(
                new StyleValues { height = Mathf.Max(0, startHeight) },
                new StyleValues { height = height }, 260)
                .Ease(t => 1f - Mathf.Pow(1f - t, 3f))
                .OnCompleted(() =>
                {
                    if (sequence != generation) return;
                    animation = null;
                    ApplyFinalState();
                });
        }

        private void ApplyFinalState()
        {
            measuring = false;
            panel.EnableInClassList("pulse-panel--expanded", expanded);
            details.EnableInClassList("pulse-details--visible", expanded);
            details.style.height = StyleKeyword.Auto;
        }

        public void Stop()
        {
            generation++;
            measuring = false;
            animation?.Stop();
            animation = null;
            ApplyFinalState();
        }
    }
}
