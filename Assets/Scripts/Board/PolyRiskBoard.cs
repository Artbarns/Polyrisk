// PolyRiskBoard.cs — génère le plateau 40 cases et pilote l'état lumineux
// de chaque case au runtime.
//
// Architecture : tout le plateau tient en 4 meshes fusionnés, donc 4 draw calls
// quelle que soit la complexité visible.
//
//   Chassis + Cadre + Champ  -> 1 mesh métal        (statique, jamais modifié)
//   Corps des 36 tuiles      -> 1 mesh métal        (statique)
//   Toutes les rainures néon -> 1 mesh émissif      (dynamique via texture d'état)
//   Gravures sombres         -> 1 mesh mat          (statique)
//
// La couleur ET l'intensité de chaque case sont lues par le shader dans une
// texture 64x1. Allumer une case = écrire un pixel. Aucun changement de
// matériau, aucun MaterialPropertyBlock, aucun draw call supplémentaire.
//
// PATCHS PROJET POLYRISK V1 :
//   A. BuildSlotModel horaire, angle 0 en bas-droite (parité BoardGenerator existant)
//   B. Ancres Tile_N enfants (BoxCollider + TileIdentifier + TileVisuals) pour
//      préserver BoardInteractionManager et le highlight/pulse Marketplace.
//   C. BindToBoardManager() peuple BoardManager.Instance.visualTiles automatiquement.
//   Corr. 1  ResetTile(index) restaure couleur du groupe (pas juste alpha).
//   Corr. 2  highlightIntensity champ public sérialisé, plus de constantes en dur.
//   Corr. 3  cache board en instance côté TileVisuals (pas static).
//   Corr. 4  stateDirty + LateUpdate = 1 seul upload GPU par frame ; push immédiat en Edit Mode.

using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Visuals;

namespace PolyRisk
{
    [ExecuteAlways]
    public class PolyRiskBoard : MonoBehaviour
    {
        // ------------------------------------------------------------ réglages
        [System.Serializable]
        public class Settings
        {
            [Header("Issus du banc d'essai")]
            public float emissiveIntensity = 1.0f;
            public float grooveWidth = 0.025f;
            public float deckThickness = 0.15f;
            [Range(0.02f, 0.3f)] public float tileThickness = 0.11f;  // au-delà, la géométrie déborde du cadrage caméra/DoF prévu
            [Range(0f, 1f)] public float metalRoughness = 0.82f;
            [Range(0f, 1f)] public float metalness = 0.93f;
            [Range(0f, 1f)] public float baseGrey = 0.14f;
            [Tooltip("Teinte du corps des tuiles, distincte du cadre/collier (baseGrey) pour qu'une tuile se voie comme une carte posée, pas comme un trou dans le même métal.")]
            [Range(0f, 1f)] public float tileBodyGrey = 0.24f;
            [Tooltip("Metallic dédié aux tuiles, plus bas que metalness (cadre) : à Metallic ~0.9+, _BaseColor a très peu d'effet visuel (surface dominée par la réflexion, pas par sa teinte diffuse) — c'est ça qui rendait tileBodyGrey inefficace malgré un matériau séparé.")]
            [Range(0f, 1f)] public float tileMetalness = 0.35f;
            [Range(0f, 1f)] public float tileRoughness = 0.55f;
            public float brushTiling = 4.75f;

            [Header("Couleurs de groupe (8)")]
            public Color[] groupColors = new Color[8];

            [Header("Géométrie")]
            [Tooltip("Volontairement < cellPitchLong : c'est CE qui rend les tuiles standards rectangulaires (étroites le long du bord, profondes vers le centre). Sans danger — AssertSlot(BuildSlotModel) est dynamique, pas figée sur 1.5/1.5. Déplace la position de toutes les cases, donc la caméra (cadrage à la main dans la scène) peut avoir besoin d'un réajustement si l'écart est important.")]
            public float cellPitchShort = 1.0f;   // largeur d'une case latérale
            public float cellPitchLong  = 1.5f;   // profondeur d'une case + taille des 4 coins carrés
            public float gap = 0.10f;             // matière entre deux logements
            public float rowSeam = 0.02f;         // séparation visuelle résiduelle entre tuiles le long d'un bord
            public float slotPlay = 0.03f;        // jeu tuile / paroi
            public int cornerSegments = 6;        // subdivisions d'angle arrondi
            [Tooltip("Extension des 4 coins carrés, vers l'extérieur du plateau uniquement (les bords intérieurs, côté tuiles voisines, ne bougent pas). Marge sûre avant le bord réel du châssis : ~0.45.")]
            public float cornerGrow = 0.4f;
        }

        public Settings settings = new Settings();

        [Header("Matériaux")]
        public Material metalMaterial;   // URP/Lit — cadre, collier, châssis
        [Tooltip("Matériau séparé pour le corps des tuiles. URP/Lit ne lit pas la couleur par sommet : sans un matériau à part, une tuile rend exactement comme le cadre (même _BaseColor), quelle que soit tileBodyGrey. Dupliquer M_BoardMetal.mat suffit (ex: M_BoardTile.mat) — ApplyMaterialSettings() pilote sa _BaseColor via tileBodyGrey.")]
        public Material tileMaterial;    // URP/Lit — corps des tuiles, teinte distincte
        public Material neonMaterial;    // PolyRisk/NeonState
        public Material darkMaterial;    // URP/Lit, mat

        [Header("Highlight & pulse (Correction 2)")]
        [Tooltip("Intensité HDR appliquée par TileVisuals.Highlight().")]
        public float highlightIntensity = 3.5f;

        [Header("Ancres de case (B)")]
        [Tooltip("Hauteur du BoxCollider des ancres Tile_N (unités monde).")]
        public float anchorColliderHeight = 0.2f;
        [Tooltip("Décalage Y global appliqué à toutes les ancres, en plus de la surface du slot. Ajustable sans régénérer.")]
        public float pawnLift = 0f;

        [Header("Auto-branchement (C)")]
        [Tooltip("Peuple BoardManager.Instance.visualTiles automatiquement.")]
        public bool bindToBoardManager = true;

        // --------------------------------------------------------- constantes
        const int SIDE = 9;
        const int SLOT_COUNT = 40;
        const int STATE_WIDTH = 64;      // puissance de 2, >= SLOT_COUNT
        const int TRIM_INDEX = 40;       // pixel réservé (hors 0..39) pour le liseré statique, jamais touché par SetTile/ResetTile

        // ------------------------------------------------------------- modèle
        public struct Slot
        {
            public int index;            // 0..39, ordre de parcours du plateau
            public Vector2 pos;          // x, z en unités monde
            public float w, d;           // empreinte alignée sur les axes
            public float rotY;           // orientation de la tuile
            public bool isCorner;
            public int groupId;          // -1 = case neutre
            public float surfaceY;       // Y de la surface haute du slot, pour poser les pions
            public bool rowIsW;          // true si l'axe "le long du bord" est porté par w (sinon par d)
        }

        public IReadOnlyList<Slot> Slots => slots;
        readonly List<Slot> slots = new List<Slot>();

        // Deux groupes par côté, motif identique sur les quatre côtés.
        // -1 = case neutre (gare, taxe, chance...).
        static readonly int[] PATTERN = { 0, 0, 0, -1, 1, 1, -1, 1, -1 };

        Texture2D stateTex;
        Color[] statePixels;
        bool stateDirty;                 // Correction 4 : batching
        float outerExtent;

        // Ancres Tile_N (B) — cachées ici pour BindToBoardManager sans re-scan
        readonly List<Transform> tileAnchors = new List<Transform>();

        // ============================================================ runtime

        void OnEnable()
        {
            EnsureStateTexture();
            if (slots.Count == 0) BuildSlotModel();
            // Fix "cases noires au Play" : statePixels perdus lors de la sérialisation
            // Play/Edit, il faut repeupler avec la couleur de repos AVANT le premier push.
            ResetAllTiles();
            PushStateTextureImmediate(); // push immédiat car pas forcément un LateUpdate au frame 0
            if (bindToBoardManager) BindToBoardManager();
        }

        void LateUpdate()
        {
            // Correction 4 : un seul upload GPU par frame si dirty.
            if (!stateDirty) return;
            PushStateTextureImmediate();
            stateDirty = false;
        }

        void EnsureStateTexture()
        {
            if (stateTex != null) return;
            stateTex = new Texture2D(STATE_WIDTH, 1, TextureFormat.RGBAHalf, false, true)
            {
                name = "PolyRisk_TileState",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            statePixels = new Color[STATE_WIDTH];
            for (int i = 0; i < STATE_WIDTH; i++) statePixels[i] = Color.black;
        }

        /// Couleur et intensité d'une case. L'intensité est un multiplicateur
        /// HDR : 0 = éteinte, 1 = repos, 3+ = franchement lumineuse.
        public void SetTile(int index, Color color, float intensity)
        {
            if (index < 0 || index >= SLOT_COUNT) return;
            EnsureStateTexture();
            statePixels[index] = new Color(color.r, color.g, color.b, intensity);
            MarkStateDirty();
        }

        public void SetTileIntensity(int index, float intensity)
        {
            if (index < 0 || index >= SLOT_COUNT) return;
            EnsureStateTexture();
            var c = statePixels[index];
            statePixels[index] = new Color(c.r, c.g, c.b, intensity);
            MarkStateDirty();
        }

        /// Correction 1 : restaure la couleur du groupe ET l'intensité de repos.
        /// L'ancienne combinaison Highlight() + ResetHighlight() écrasait le RGB puis
        /// ne remettait que l'alpha, laissant la case teintée du highlight à vie.
        public void ResetTile(int index)
        {
            if (index < 0 || index >= SLOT_COUNT) return;
            EnsureStateTexture();
            if (slots.Count == 0) BuildSlotModel();
            var s = slots[index];
            statePixels[index] = WithAlpha(GroupColor(s.groupId), settings.emissiveIntensity);
            MarkStateDirty();
        }

        /// Remet chaque case à la couleur de son groupe, à l'intensité de repos.
        public void ResetAllTiles()
        {
            EnsureStateTexture();
            if (slots.Count == 0) BuildSlotModel();
            foreach (var s in slots)
                statePixels[s.index] = WithAlpha(GroupColor(s.groupId), settings.emissiveIntensity);
            MarkStateDirty();
        }

        void MarkStateDirty()
        {
            // Correction 4 : en Play Mode on batch via LateUpdate.
            // En Edit Mode (pas de LateUpdate garanti), on push immédiatement.
            if (Application.isPlaying)
                stateDirty = true;
            else
                PushStateTextureImmediate();
        }

        void PushStateTextureImmediate()
        {
            if (stateTex == null || statePixels == null) return;
            stateTex.SetPixels(statePixels);
            stateTex.Apply(false, false);
            if (neonMaterial != null) neonMaterial.SetTexture("_StateTex", stateTex);
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        Color GroupColor(int groupId)
        {
            if (groupId < 0 || settings.groupColors == null ||
                groupId >= settings.groupColors.Length)
                return new Color(0.81f, 0.85f, 0.89f);
            return settings.groupColors[groupId];
        }

        // ===================================================== modèle des cases

        /// PATCH A — Parcours du plateau existant du projet POLYRISK V1 :
        /// sens HORAIRE vu du dessus, angle 0 en bas-droite.
        ///   cornerPos = [ (+co,-co), (-co,-co), (-co,+co), (+co,+co) ]
        ///   side 0 : bord bottom (z=-co), x de +6 à -6, rotY = 0
        ///   side 1 : bord left   (x=-co), z de -6 à +6, rotY = 90
        ///   side 2 : bord top    (z=+co), x de -6 à +6, rotY = 180
        ///   side 3 : bord right  (x=+co), z de +6 à -6, rotY = -90
        void BuildSlotModel()
        {
            slots.Clear();
            var st = settings;
            float W = st.cellPitchShort, D = st.cellPitchLong, C = st.cellPitchLong;
            float tw = W - st.gap, td = D - st.gap, tc = C - st.gap;
            outerExtent = SIDE * W * 0.5f + C;

            // centre d'un coin, sur l'axe qui l'écarte du centre du plateau
            float co = outerExtent - C * 0.5f;

            // PATCH A : coins dans l'ordre horaire, angle 0 = bas-droite.
            Vector2[] cornerPos =
            {
                new Vector2( co, -co), // 0  Bottom-Right
                new Vector2(-co, -co), // 10 Bottom-Left
                new Vector2(-co,  co), // 20 Top-Left
                new Vector2( co,  co)  // 30 Top-Right
            };

            int idx = 0;
            for (int side = 0; side < 4; side++)
            {
                slots.Add(new Slot
                {
                    index = idx++,
                    pos = cornerPos[side],
                    w = tc, d = tc, rotY = 0f, isCorner = true, groupId = -1
                });

                for (int i = 0; i < SIDE; i++)
                {
                    // Position latérale sur l'axe parallèle au bord (variable)
                    // Décroit de +(SIDE-1)/2 * W vers -(SIDE-1)/2 * W = de +6 à -6 pour SIDE=9, W=1.5
                    float off = ((SIDE - 1) * 0.5f - i) * W;
                    Vector2 p;
                    float rot;
                    bool horiz;

                    switch (side)
                    {
                        // side 0 : bord Bottom, z = -co fixe, rotY 0 (tuile en portrait long axe Z)
                        case 0:
                            p = new Vector2(off, -(outerExtent - D * 0.5f));
                            rot = 0f;
                            horiz = false;
                            break;

                        // side 1 : bord Left, x = -co fixe, z croissant, rotY 90
                        case 1:
                            p = new Vector2(-(outerExtent - D * 0.5f), -off);
                            rot = 90f;
                            horiz = true;
                            break;

                        // side 2 : bord Top, z = +co fixe, x croissant, rotY 180
                        case 2:
                            p = new Vector2(-off, outerExtent - D * 0.5f);
                            rot = 180f;
                            horiz = false;
                            break;

                        // side 3 : bord Right, x = +co fixe, z décroissant, rotY -90
                        default:
                            p = new Vector2(outerExtent - D * 0.5f, off);
                            rot = -90f;
                            horiz = true;
                            break;
                    }

                    int pat = PATTERN[i];
                    int group = pat < 0 ? -1 : side * 2 + pat;

                    slots.Add(new Slot
                    {
                        index = idx++,
                        pos = p,
                        w = horiz ? td : tw,
                        d = horiz ? tw : td,
                        rotY = rot,
                        isCorner = false,
                        groupId = group,
                        rowIsW = !horiz
                    });
                }
            }

            // Assertions (PATCH A) — garde-fou sur l'algorithme, pas sur une valeur figée :
            // dérivées de co/W (calculés ci-dessus pour CE run), donc valables quels que
            // soient cellPitchShort/cellPitchLong. Continuent de détecter un vrai bug de
            // parcours/ordre des coins ; ne réagissent plus à un choix de forme de tuile.
            //
            // Slot[1] (i=0 dans la boucle side) est positionnée par sa PROPRE formule
            // ( (SIDE-1)*0.5*W ), indépendante de co — "co - W" ne coïncidait que tant que
            // cellPitchShort == cellPitchLong (donc C == W). Avec des tuiles rectangulaires
            // (C != W), il faut la vraie formule, pas un raccourci qui supposait l'égalité.
            float slot1X = (SIDE - 1) * 0.5f * W;
            AssertSlot(0,  new Vector2(co, -co),      "Slot[0] doit être Bottom-Right");
            AssertSlot(1,  new Vector2(slot1X, -co),  "Slot[1] doit être 1 pas à gauche de 0 sur le bord Bottom");
            AssertSlot(10, new Vector2(-co, -co),     "Slot[10] doit être Bottom-Left");
            AssertSlot(20, new Vector2(-co, co),      "Slot[20] doit être Top-Left");
        }

        void AssertSlot(int i, Vector2 expected, string message)
        {
            if (i >= slots.Count) { Debug.LogError($"[PolyRiskBoard] Slot[{i}] absent ({message})"); return; }
            var got = slots[i].pos;
            if (Mathf.Abs(got.x - expected.x) > 1e-3f || Mathf.Abs(got.y - expected.y) > 1e-3f)
                Debug.LogError($"[PolyRiskBoard] {message}. Attendu {expected}, obtenu {got}.");
        }

        // ========================================================== génération

        [ContextMenu("Générer le plateau")]
        public void Generate()
        {
            EnsureStateTexture();
            BuildSlotModel();
            ClearChildren();

            var st = settings;
            int segs = st.cornerSegments;
            float H = st.deckThickness;
            float PAD = st.slotPlay;
            float S = outerExtent * 2f + 0.9f;
            float INNER = (outerExtent - st.cellPitchLong) * 2f + 0.3f;

            Color metalTint = new Color(st.baseGrey, st.baseGrey, st.baseGrey);
            Color tileTint = new Color(st.tileBodyGrey, st.tileBodyGrey, st.tileBodyGrey);
            Color darkTint = metalTint * 0.32f;

            var mMetal = new ProcGeo.MeshBuilder();
            var mTiles = new ProcGeo.MeshBuilder();
            var mNeon = new ProcGeo.MeshBuilder();
            var mDark = new ProcGeo.MeshBuilder();

            // --- châssis : ferme le fond de tous les logements
            ProcGeo.AddSolid(mMetal,
                ProcGeo.RoundedRect(S + 0.55f, S + 0.55f, 0.62f, segs),
                -0.02f - H * 0.85f, -0.02f, metalTint);

            // --- lèvre extérieure et lèvre intérieure du cadre
            float slotOuter = outerExtent;
            float slotInner = outerExtent - st.cellPitchLong;
            ProcGeo.AddBand(mMetal,
                ProcGeo.RoundedRect(S, S, 0.55f, segs),
                ProcGeo.RoundedRect(slotOuter * 2f + PAD, slotOuter * 2f + PAD, 0.3f, segs),
                0f, H, metalTint);
            ProcGeo.AddBand(mMetal,
                ProcGeo.RoundedRect(slotInner * 2f - PAD, slotInner * 2f - PAD, 0.3f, segs),
                ProcGeo.RoundedRect(INNER, INNER, 0.35f, segs),
                0f, H, metalTint);

            // --- liseré blanc fin, sur la marche du châssis (le châssis fait S+0.55 de large,
            // la lèvre extérieure qui vient par-dessus ne fait que S — la différence expose
            // un rebord plat juste après la base, à Y proche de 0). Statique, jamais éteint,
            // via le pixel réservé TRIM_INDEX (n'entre jamais en conflit avec les 40 cases).
            ProcGeo.AddRing(mNeon, S + 0.25f, S + 0.25f, 0.58f, st.grooveWidth * 0.7f,
                            -0.015f, 0.005f, Color.white, TRIM_INDEX, 0f, 0f, segs);

            // --- champ central gravé, en retrait
            float fieldT = H * 0.55f;
            ProcGeo.AddSolid(mMetal,
                ProcGeo.RoundedRect(INNER + 0.5f, INNER + 0.5f, 0.4f, segs),
                H * 0.34f - fieldT, H * 0.34f, metalTint * 0.62f);

            // --- par logement : collier de matière, paroi néon, contenu
            // On mute Slots en place pour renseigner surfaceY, utilisé par les ancres.
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                Color tint = GroupColor(s.groupId);

                if (s.isCorner)
                {
                    // Coin agrandi vers l'extérieur uniquement : le centre est décalé de
                    // grow/2 dans la direction du coin (signe de sa position), donc les
                    // bords intérieurs (côté tuiles voisines) restent exactement en place —
                    // seuls les bords extérieurs avancent, dans la marge du bandeau du cadre.
                    float grow = st.cornerGrow;
                    Vector2 outward = new Vector2(Mathf.Sign(s.pos.x), Mathf.Sign(s.pos.y));
                    Vector2 center = s.pos + outward * (grow * 0.5f);
                    float cw = s.w + grow, cd = s.d + grow;
                    float cr = Mathf.Min(cw, cd) * 0.19f;
                    float chw = cw + PAD, chd = cd + PAD;

                    // collier + paroi néon du logement, redimensionnés avec le coin
                    ProcGeo.AddRing(mMetal, chw + st.gap, chd + st.gap, cr + st.gap * 0.5f,
                                    st.gap * 0.5f, 0f, H, metalTint, 0f, center.x, center.y, segs);
                    ProcGeo.AddRing(mNeon, chw, chd, cr, st.grooveWidth,
                                    0f, H * 0.96f, tint, s.index, center.x, center.y, segs);

                    AddCornerInlay(mDark, mNeon, s, darkTint, tint, segs, cw, cd, center);
                    // corner : creusé, surface à ~0.034 (top de l'inlay clair)
                    s.surfaceY = 0.034f;
                }
                else
                {
                    float hw = s.w + PAD, hd = s.d + PAD;
                    float r = Mathf.Min(s.w, s.d) * 0.19f;

                    // le collier comble l'espace entre logements voisins ;
                    // les colliers se rejoignent exactement au milieu du gap
                    ProcGeo.AddRing(mMetal, hw + st.gap, hd + st.gap, r + st.gap * 0.5f,
                                    st.gap * 0.5f, 0f, H, metalTint, 0f, s.pos.x, s.pos.y, segs);

                    // paroi lumineuse du logement, sur toute la hauteur
                    ProcGeo.AddRing(mNeon, hw, hd, r, st.grooveWidth,
                                    0f, H * 0.96f, tint, s.index, s.pos.x, s.pos.y, segs);

                    // Tuiles standards étirées le long du bord pour se toucher (pas de vide
                    // visible), sans empiéter sur le jeu (slotPlay) réservé face au collier.
                    // La forme rectangulaire vient directement de cellPitchShort < cellPitchLong
                    // (Settings) — plus de mécanisme séparé pour ça, un seul levier à tourner.
                    float rowWiden = Mathf.Max(0f, st.gap - st.rowSeam - st.slotPlay);
                    float tileW = s.w + (s.rowIsW ? rowWiden : 0f);
                    float tileD = s.d + (s.rowIsW ? 0f : rowWiden);

                    s.surfaceY = AddTile(mTiles, mNeon, s, tileTint, tint, segs, H, tileW, tileD, s.pos);
                }

                slots[i] = s;
            }

            Spawn("Board_Metal", mMetal.ToMesh("Board_Metal"), metalMaterial);
            // Repli sur metalMaterial si tileMaterial n'est pas encore assigné dans l'Inspector —
            // évite un plateau sans tuiles visibles, mais tileBodyGrey n'aura alors aucun effet
            // (cf. tooltip du champ) tant qu'un matériau séparé n'est pas branché.
            Spawn("Board_Tiles", mTiles.ToMesh("Board_Tiles"), tileMaterial != null ? tileMaterial : metalMaterial);
            Spawn("Board_Neon", mNeon.ToMesh("Board_Neon"), neonMaterial);
            Spawn("Board_Dark", mDark.ToMesh("Board_Dark"), darkMaterial);

            ApplyMaterialSettings();
            ResetAllTiles();

            // Liseré blanc statique : intensité fixe, jamais remis à zéro (contrairement aux
            // cases 0..39, TRIM_INDEX n'est jamais touché par SetTile/ResetTile/highlight).
            statePixels[TRIM_INDEX] = new Color(1f, 1f, 1f, st.emissiveIntensity * 1.3f);
            MarkStateDirty();

            // PATCH B — ancres Tile_N enfants (colliders + TileIdentifier + TileVisuals)
            BuildSlotAnchors();

            // PATCH C — auto-branchement
            if (bindToBoardManager) BindToBoardManager();
        }

        /// Angle : creusé dans le plateau, jamais une tuile rapportée.
        /// (cw, cd, center) = empreinte déjà agrandie (cornerGrow) calculée par l'appelant.
        void AddCornerInlay(ProcGeo.MeshBuilder dark, ProcGeo.MeshBuilder neon,
                            Slot s, Color darkTint, Color tint, int segs,
                            float cw, float cd, Vector2 center)
        {
            ProcGeo.AddSolid(dark,
                ProcGeo.RoundedRect(cw - 0.06f, cd - 0.06f, 0.22f, segs, center.x, center.y),
                -0.02f, 0.01f, darkTint);

            ProcGeo.AddRing(neon, cw - 0.40f, cd - 0.40f, 0.16f, settings.grooveWidth * 0.8f,
                            0.008f, 0.034f, tint, s.index, center.x, center.y, segs);
            ProcGeo.AddRing(neon, cw - 0.72f, cd - 0.72f, 0.10f, settings.grooveWidth * 0.6f,
                            0.010f, 0.030f, tint, s.index, center.x, center.y, segs);
        }

        /// Tuile : dalle plate pleine (métal anthracite, teinte distincte du cadre) +
        /// un seul header émissif sur le côté tourné vers le centre du plateau. Plus de
        /// double étage ni d'anneau néon périphérique — cf. cahier des charges mockup.
        /// `center` = position de la tuile (`s.pos` pour les tuiles standards, décalée
        /// pour les coins agrandis — cf. AddCornerInlay) ; le header se recale toujours
        /// sur le vrai bord intérieur à partir de (center, tileD/tileW).
        /// Retourne la Y de la surface haute pour l'ancre de pion.
        float AddTile(ProcGeo.MeshBuilder body, ProcGeo.MeshBuilder neon,
                      Slot s, Color bodyTint, Color tint, int segs, float H,
                      float tileW, float tileD, Vector2 center)
        {
            float hb = settings.tileThickness;
            float r = Mathf.Min(tileW, tileD) * 0.17f;

            // la tuile affleure légèrement le cadre, quelle que soit son épaisseur
            float baseY = Mathf.Max(0.01f, H + 0.05f - hb - 0.022f);

            ProcGeo.AddSolid(body,
                ProcGeo.RoundedRect(tileW, tileD, r, segs, center.x, center.y),
                baseY, baseY + hb, bodyTint);

            // Header : pavé coloré émissif, uniquement sur le bord intérieur.
            Vector2 inward = InwardDir(s.rotY);
            float depthAxis = s.rowIsW ? tileD : tileW;   // taille perpendiculaire au bord
            float rowAxis   = s.rowIsW ? tileW : tileD;   // taille le long du bord

            float headerDepth = depthAxis * 0.24f;
            const float headerInset = 0.06f;
            float headerW = s.rowIsW ? rowAxis - headerInset : headerDepth;
            float headerD = s.rowIsW ? headerDepth : rowAxis - headerInset;
            float headerR = Mathf.Min(headerW, headerD) * 0.25f;

            // Bord intérieur réel de la dalle (déjà décalée), puis on rentre le header
            // depuis ce bord — reste donc fixe même si tileD/tileW grandit côté extérieur.
            Vector2 inwardEdge = center + inward * (depthAxis * 0.5f);
            Vector2 headerCenter = inwardEdge - inward * (headerDepth * 0.5f + 0.02f);

            ProcGeo.AddSolid(neon,
                ProcGeo.RoundedRect(headerW, headerD, headerR, segs, headerCenter.x, headerCenter.y),
                baseY + hb - 0.006f, baseY + hb + 0.018f, tint, s.index);

            // Surface haute réelle où le pion se pose (top de la dalle)
            return baseY + hb;
        }

        /// Direction unitaire "vers le centre du plateau" pour une tuile de bord,
        /// dérivée de sa rotation (fixée par côté dans BuildSlotModel).
        static Vector2 InwardDir(float rotY)
        {
            if (Mathf.Approximately(rotY, 0f)) return new Vector2(0f, 1f);
            if (Mathf.Approximately(rotY, 90f)) return new Vector2(1f, 0f);
            if (Mathf.Approximately(rotY, 180f)) return new Vector2(0f, -1f);
            return new Vector2(-1f, 0f); // -90
        }

        // ================================================== PATCH B — ancres

        /// Crée 40 GameObjects enfants nommés Tile_0..Tile_39 avec BoxCollider,
        /// TileIdentifier et TileVisuals. Position Y = surface réelle du slot + pawnLift.
        void BuildSlotAnchors()
        {
            tileAnchors.Clear();
            var anchorsParent = FindOrCreateChild("Tile_Anchors");

            foreach (var s in slots)
            {
                var anchor = new GameObject($"Tile_{s.index}");
                anchor.transform.SetParent(anchorsParent.transform, false);
                anchor.transform.localPosition = new Vector3(s.pos.x, s.surfaceY + pawnLift, s.pos.y);
                anchor.transform.localRotation = Quaternion.Euler(0f, s.rotY, 0f);

                // BoxCollider dimensionné sur l'empreinte du slot
                var box = anchor.AddComponent<BoxCollider>();
                box.size = new Vector3(s.w, anchorColliderHeight, s.d);
                // Centre du collider aligné sur la surface (haut à surface + height/2)
                box.center = new Vector3(0f, anchorColliderHeight * 0.5f, 0f);

                // TileIdentifier (script existant PolyRisk.Visuals.TileIdentifier)
                var identifier = anchor.AddComponent<TileIdentifier>();
                identifier.SetIndex(s.index);

                // TileVisuals (script existant) — sans MeshRenderer, ses appels forwardent
                // automatiquement vers PolyRiskBoard.SetTile/ResetTile/SetTileIntensity
                anchor.AddComponent<TileVisuals>();

                tileAnchors.Add(anchor.transform);
            }
        }

        GameObject FindOrCreateChild(string name)
        {
            var t = transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go;
        }

        // ================================================== PATCH C — bind

        /// Peuple BoardManager.Instance.visualTiles pour que TurnManager, Marketplace,
        /// RulesEngine, Venture, Ability, BoardInteractionManager continuent de tourner
        /// sans modification.
        public void BindToBoardManager()
        {
            var bm = PolyRisk.Systems.BoardManager.Instance;
            if (bm == null) bm = FindObjectOfType<PolyRisk.Systems.BoardManager>();
            if (bm == null) return; // scène de test sans BoardManager, silencieux

            if (bm.visualTiles == null || bm.visualTiles.Length != 40)
                bm.visualTiles = new Transform[40];

            // Si les ancres n'existent pas encore (OnEnable avant Generate), on skip.
            if (tileAnchors.Count == 0)
            {
                // Fallback : chercher les enfants Tile_N si Generate a déjà eu lieu dans un précédent Play
                var anchorsParent = transform.Find("Tile_Anchors");
                if (anchorsParent == null) return;
                for (int i = 0; i < 40; i++)
                {
                    var t = anchorsParent.Find($"Tile_{i}");
                    if (t != null) bm.visualTiles[i] = t;
                }
                return;
            }

            for (int i = 0; i < tileAnchors.Count && i < 40; i++)
                bm.visualTiles[i] = tileAnchors[i];
        }

        // ------------------------------------------------------------ plomberie

        void ApplyMaterialSettings()
        {
            var st = settings;
            if (metalMaterial != null)
            {
                metalMaterial.SetColor("_BaseColor", new Color(st.baseGrey, st.baseGrey, st.baseGrey));
                metalMaterial.SetFloat("_Metallic", st.metalness);
                metalMaterial.SetFloat("_Smoothness", 1f - st.metalRoughness);
                metalMaterial.SetTextureScale("_BaseMap", Vector2.one * st.brushTiling);
            }
            if (tileMaterial != null)
            {
                // Metallic/Smoothness dédiés (tileMetalness/tileRoughness), plus bas que le cadre :
                // à Metallic ~0.9+ la _BaseColor n'a presque aucun effet visuel (surface dominée
                // par la réflexion), donc tileBodyGrey ne se voyait pas malgré le matériau séparé.
                tileMaterial.SetColor("_BaseColor", new Color(st.tileBodyGrey, st.tileBodyGrey, st.tileBodyGrey));
                tileMaterial.SetFloat("_Metallic", st.tileMetalness);
                tileMaterial.SetFloat("_Smoothness", 1f - st.tileRoughness);
                tileMaterial.SetTextureScale("_BaseMap", Vector2.one * st.brushTiling);
            }
            if (neonMaterial != null)
            {
                neonMaterial.SetTexture("_StateTex", stateTex);
                neonMaterial.SetFloat("_Count", STATE_WIDTH);
                neonMaterial.SetFloat("_Boost", 1f);
            }
        }

        void Spawn(string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
            tileAnchors.Clear();
        }
    }
}

