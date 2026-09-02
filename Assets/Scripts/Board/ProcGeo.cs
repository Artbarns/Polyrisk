// ProcGeo.cs — helpers de maillage procédural pour le plateau PolyRisk.
// Aucune dépendance externe. Tout est construit à partir de deux primitives :
//   - un profil "rectangle arrondi" (convexe, donc triangulable en éventail)
//   - une extrusion verticale de ce profil, pleine ou en bandeau
// C'est suffisant pour l'intégralité du plateau : ni booléens, ni triangulation
// générale, ni découpe de trous.

using System.Collections.Generic;
using UnityEngine;

namespace PolyRisk
{
    public static class ProcGeo
    {
        /// Profil rectangle arrondi centré sur (cx, cz), parcouru en sens direct.
        public static Vector2[] RoundedRect(float w, float h, float r, int cornerSegs,
                                            float cx = 0f, float cz = 0f)
        {
            r = Mathf.Min(r, w * 0.5f - 1e-4f, h * 0.5f - 1e-4f);
            r = Mathf.Max(r, 0f);
            cornerSegs = Mathf.Max(1, cornerSegs);

            float x = w * 0.5f - r, z = h * 0.5f - r;
            var pts = new List<Vector2>((cornerSegs + 1) * 4);

            // Les quatre centres d'arc, dans l'ordre direct.
            Vector2[] c = { new Vector2(x, -z), new Vector2(x, z),
                            new Vector2(-x, z), new Vector2(-x, -z) };
            float[] start = { -90f, 0f, 90f, 180f };

            for (int k = 0; k < 4; k++)
            {
                for (int s = 0; s <= cornerSegs; s++)
                {
                    float a = (start[k] + 90f * s / cornerSegs) * Mathf.Deg2Rad;
                    pts.Add(new Vector2(cx + c[k].x + Mathf.Cos(a) * r,
                                        cz + c[k].y + Mathf.Sin(a) * r));
                }
            }
            return pts.ToArray();
        }

        /// Normales horizontales lissées : moyenne des deux segments adjacents.
        /// C'est ce qui évite le facettage visible dans les angles arrondis.
        public static Vector2[] OutwardNormals(Vector2[] p)
        {
            int n = p.Length;
            var res = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = p[(i - 1 + n) % n], next = p[(i + 1) % n];
                Vector2 d1 = (p[i] - prev).normalized, d2 = (next - p[i]).normalized;
                Vector2 nrm = new Vector2(d1.y, -d1.x) + new Vector2(d2.y, -d2.x);
                res[i] = nrm.sqrMagnitude < 1e-8f ? new Vector2(d1.y, -d1.x) : nrm.normalized;
            }
            return res;
        }

        /// Accumulateur : plusieurs pièces vont dans un seul Mesh, donc un seul draw call.
        /// La couleur porte la teinte de base ; uv2.x porte l'index de case (0..39),
        /// que le shader néon utilise pour lire l'état runtime de la case.
        public class MeshBuilder
        {
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<Vector3> nrm = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<Vector2> uv2 = new List<Vector2>();
            readonly List<Color> col = new List<Color>();
            readonly List<int> tri = new List<int>();

            public int VertexCount => v.Count;

            void Push(Vector3 p, Vector3 n, Vector2 t, Color c, float idx)
            {
                v.Add(p); nrm.Add(n); uv.Add(t); col.Add(c); uv2.Add(new Vector2(idx, 0f));
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                             Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd,
                             Color tint, float idx)
            {
                int i = v.Count;
                Push(a, na, new Vector2(0, 0), tint, idx);
                Push(b, nb, new Vector2(1, 0), tint, idx);
                Push(c, nc, new Vector2(1, 1), tint, idx);
                Push(d, nd, new Vector2(0, 1), tint, idx);
                tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 3);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 n, Color tint, float idx)
            {
                int i = v.Count;
                Push(a, n, new Vector2(0, 0), tint, idx);
                Push(b, n, new Vector2(1, 0), tint, idx);
                Push(c, n, new Vector2(0, 1), tint, idx);
                tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                m.indexFormat = v.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;
                m.SetVertices(v);
                m.SetNormals(nrm);
                m.SetUVs(0, uv);
                m.SetUVs(1, uv2);
                m.SetColors(col);
                m.SetTriangles(tri, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        /// Volume plein : capuchon haut, capuchon bas, paroi extérieure.
        /// Le profil étant convexe, l'éventail depuis le centre suffit.
        public static void AddSolid(MeshBuilder mb, Vector2[] path, float y0, float y1,
                                    Color tint, float idx = 0f)
        {
            var nrm = OutwardNormals(path);
            int n = path.Length;

            Vector2 c = Vector2.zero;
            for (int i = 0; i < n; i++) c += path[i];
            c /= n;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                mb.Tri(new Vector3(c.x, y1, c.y),
                       new Vector3(path[i].x, y1, path[i].y),
                       new Vector3(path[j].x, y1, path[j].y), Vector3.up, tint, idx);
                mb.Tri(new Vector3(c.x, y0, c.y),
                       new Vector3(path[j].x, y0, path[j].y),
                       new Vector3(path[i].x, y0, path[i].y), Vector3.down, tint, idx);

                Vector3 ni = new Vector3(nrm[i].x, 0, nrm[i].y);
                Vector3 nj = new Vector3(nrm[j].x, 0, nrm[j].y);
                mb.Quad(new Vector3(path[i].x, y0, path[i].y),
                        new Vector3(path[j].x, y0, path[j].y),
                        new Vector3(path[j].x, y1, path[j].y),
                        new Vector3(path[i].x, y1, path[i].y),
                        ni, nj, nj, ni, tint, idx);
            }
        }

        /// Bandeau creux entre deux profils concentriques : sert au cadre, aux
        /// parois de logement et à toutes les rainures néon.
        public static void AddBand(MeshBuilder mb, Vector2[] outer, Vector2[] inner,
                                   float y0, float y1, Color tint, float idx = 0f,
                                   bool capTop = true, bool capBottom = true)
        {
            int n = outer.Length;
            var no = OutwardNormals(outer);
            var ni = OutwardNormals(inner);

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;

                if (capTop)
                    mb.Quad(new Vector3(outer[i].x, y1, outer[i].y),
                            new Vector3(inner[i].x, y1, inner[i].y),
                            new Vector3(inner[j].x, y1, inner[j].y),
                            new Vector3(outer[j].x, y1, outer[j].y),
                            Vector3.up, Vector3.up, Vector3.up, Vector3.up, tint, idx);

                if (capBottom)
                    mb.Quad(new Vector3(outer[i].x, y0, outer[i].y),
                            new Vector3(outer[j].x, y0, outer[j].y),
                            new Vector3(inner[j].x, y0, inner[j].y),
                            new Vector3(inner[i].x, y0, inner[i].y),
                            Vector3.down, Vector3.down, Vector3.down, Vector3.down, tint, idx);

                Vector3 a = new Vector3(no[i].x, 0, no[i].y), b = new Vector3(no[j].x, 0, no[j].y);
                mb.Quad(new Vector3(outer[i].x, y0, outer[i].y),
                        new Vector3(outer[j].x, y0, outer[j].y),
                        new Vector3(outer[j].x, y1, outer[j].y),
                        new Vector3(outer[i].x, y1, outer[i].y),
                        a, b, b, a, tint, idx);

                Vector3 c = new Vector3(-ni[i].x, 0, -ni[i].y), d = new Vector3(-ni[j].x, 0, -ni[j].y);
                mb.Quad(new Vector3(inner[j].x, y0, inner[j].y),
                        new Vector3(inner[i].x, y0, inner[i].y),
                        new Vector3(inner[i].x, y1, inner[i].y),
                        new Vector3(inner[j].x, y1, inner[j].y),
                        d, c, c, d, tint, idx);
            }
        }

        /// Raccourci : bandeau défini par une épaisseur plutôt que deux profils.
        public static void AddRing(MeshBuilder mb, float w, float h, float r, float thickness,
                                   float y0, float y1, Color tint, float idx = 0f,
                                   float cx = 0f, float cz = 0f, int segs = 6)
        {
            var outer = RoundedRect(w, h, r, segs, cx, cz);
            var inner = RoundedRect(w - 2f * thickness, h - 2f * thickness,
                                    Mathf.Max(r - thickness, 0.002f), segs, cx, cz);
            AddBand(mb, outer, inner, y0, y1, tint, idx);
        }
    }
}
