using UnityEngine;
using UnityEngine.UIElements;

namespace PolyRisk.UI
{
    public readonly struct PulseDicePose
    {
        public readonly float Lift, Angle, ScaleX, ScaleY, Depth;
        public PulseDicePose(float lift, float angle, float sx = 1, float sy = 1, float depth = 0)
        { Lift = lift; Angle = angle; ScaleX = sx; ScaleY = sy; Depth = depth; }

        public static PulseDicePose Sample(bool second, float seconds)
        {
            var times = second ? SecondTimes : FirstTimes;
            var lifts = second ? SecondLifts : FirstLifts;
            var angles = second ? SecondAngles : FirstAngles;
            if (seconds >= times[times.Length - 1]) return new PulseDicePose(0, 0);
            int i = 0;
            while (i < times.Length - 2 && seconds > times[i + 1]) i++;
            float t = Mathf.InverseLerp(times[i], times[i + 1], seconds);
            bool falling = (second && (i == 1 || i == 6)) || (!second && (i == 1 || i == 3));
            t = falling ? t * t : Mathf.SmoothStep(0, 1, t);
            float sx = 1, sy = 1;
            float contact = second ? 0.935f : 0.2964f;
            float compression = Mathf.Clamp01(1 - Mathf.Abs(seconds - contact) / 0.065f);
            sx += compression * 0.04f; sy -= compression * 0.04f;
            float flight = second ? 0.64f : 0.52f;
            float depth = seconds < flight ? Mathf.Sin(Mathf.Clamp01(seconds / flight) * Mathf.PI) : 0;
            return new PulseDicePose(Mathf.Lerp(lifts[i], lifts[i + 1], t),
                Mathf.Lerp(angles[i], angles[i + 1], t), sx, sy, depth);
        }
        private static readonly float[] FirstTimes = { 0, .13f, .2964f, .3952f, .52f };
        private static readonly float[] FirstLifts = { 0, 64, -3, 17, 0 };
        private static readonly float[] FirstAngles = { 0, -5, 2, -2, 0 };
        private static readonly float[] SecondTimes = { 0, .1856f, .3904f, .512f, .64f, .728f, .84f, .935f, .991f, 1.06f };
        private static readonly float[] SecondLifts = { 0, 73, -2, 11, 0, 0, 0, 0, 0, 0 };
        private static readonly float[] SecondAngles = { 0, 8, 12, 29, 38, 35, 37, 0, -3, 0 };
    }

    public sealed class PulseDiceView : VisualElement
    {
        private int value;
        private PulseDicePose pose = new PulseDicePose(0, 0);
        private readonly Vector2[] corners = new Vector2[4];
        private Quaternion orientation;
        private Vector2 pivot;
        private static readonly int[][] Faces = {
            new[] { 0, 0, 0 }, new[] { 1, 2, 3 }, new[] { 2, 6, 3 }, new[] { 3, 2, 6 },
            new[] { 4, 2, 1 }, new[] { 5, 1, 3 }, new[] { 6, 5, 3 }
        };
        private static readonly int[][] Patterns = {
            new int[0], new[] { 4 }, new[] { 0, 8 }, new[] { 0, 4, 8 }, new[] { 0, 2, 6, 8 },
            new[] { 0, 2, 4, 6, 8 }, new[] { 0, 2, 3, 5, 6, 8 }
        };
        public PulseDiceView()
        {
            AddToClassList("pulse-volume-die");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }
        public void SetValue(int confirmedValue) { value = Mathf.Clamp(confirmedValue, 0, 6); MarkDirtyRepaint(); }
        public void SetPose(PulseDicePose next) { pose = next; MarkDirtyRepaint(); }
        public static int VisibleFace(int front, int side) => Faces[Mathf.Clamp(front, 0, 6)][side];
        public static Vector2 RotateAtPivot(Vector2 point, Vector2 groundPivot, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            var d = point - groundPivot;
            return groundPivot + new Vector2(d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a), d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
        }
        private Vector2 Project(Vector3 point)
        {
            Vector3 p = orientation * (point * 62f);
            float perspective = 700f / (700f - p.z);
            return new Vector2(p.x, -p.y) * perspective;
        }
        private Vector2 Place(Vector3 point)
        {
            Vector2 p = Project(point);
            p = pivot + Vector2.Scale(p - pivot, new Vector2(pose.ScaleX, pose.ScaleY));
            return new Vector2(contentRect.width * .5f + 23, 82)
                + RotateAtPivot(p, pivot, pose.Angle) - pivot - new Vector2(0, pose.Lift);
        }
        private void Draw(MeshGenerationContext ctx)
        {
            if (contentRect.width < 1) return;
            orientation = Quaternion.Euler(15 + 13 * pose.Depth, -22 - 15 * pose.Depth, 0);
            pivot = Project(new Vector3(.5f, -.5f, .5f));
            var p = ctx.painter2D;
            p.fillColor = new Color(0, 0, 0, .25f * (1 - Mathf.Clamp01(pose.Lift / 110)));
            p.BeginPath();
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24;
                var v = new Vector2(contentRect.width * .5f + Mathf.Cos(a) * (37 - pose.Lift * .1f), 85 + Mathf.Sin(a) * 5);
                if (i == 0) p.MoveTo(v); else p.LineTo(v);
            }
            p.ClosePath(); p.Fill();
            DrawFace(p, new Vector3(0, .5f, 0), Vector3.right, Vector3.back, VisibleFace(value, 1), new Color(.14f, .26f, .22f));
            DrawFace(p, new Vector3(.5f, 0, 0), Vector3.back, Vector3.up, VisibleFace(value, 2), new Color(.06f, .13f, .10f));
            DrawFace(p, new Vector3(0, 0, .5f), Vector3.right, Vector3.up, value, new Color(.105f, .205f, .17f));
        }
        private void DrawFace(Painter2D p, Vector3 center, Vector3 right, Vector3 up, int number, Color color)
        {
            corners[0] = Place(center - right * .5f + up * .5f);
            corners[1] = Place(center + right * .5f + up * .5f);
            corners[2] = Place(center + right * .5f - up * .5f);
            corners[3] = Place(center - right * .5f - up * .5f);
            p.fillColor = color; p.strokeColor = new Color(.40f, .51f, .47f, .85f); p.lineWidth = 1;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(corners[0]);
            p.LineTo(corners[1]); p.LineTo(corners[2]); p.LineTo(corners[3]);
            p.ClosePath(); p.Fill(); p.Stroke();
            p.fillColor = new Color(.93f, .97f, .94f);
            foreach (int dot in Patterns[number])
            {
                float x = (dot % 3 - 1) * .25f, y = (1 - dot / 3) * .25f;
                Vector3 at = center + right * x + up * y;
                p.BeginPath();
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI * 2 / 12;
                    var q = Place(at + right * (Mathf.Cos(a) * .052f) + up * (Mathf.Sin(a) * .052f));
                    if (i == 0) p.MoveTo(q); else p.LineTo(q);
                }
                p.ClosePath(); p.Fill();
            }
        }
    }
}
