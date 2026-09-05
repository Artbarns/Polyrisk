using UnityEngine;
using UnityEngine.UIElements;
using PolyRisk.Core;

namespace PolyRisk.UI
{
    /// <summary>
    /// Résout et peuple l'icône d'une tuile. Priorité :
    ///   1. PropertyData.Icon (Sprite) si assigné — override art réel, futur pass "dev -> prod".
    ///   2. Tuile Hub (Logistics), avant le switch famille générique.
    ///   3. Famille industrielle (switch sur FamilyData.FamilyName).
    ///   4. Tuile spéciale (switch sur TileType).
    ///   5. Repli neutre si rien ne correspond.
    ///
    /// Aucun asset requis pour 2-5 : glyphes composés en pur USS/VisualElement
    /// (position absolute, border-radius, rotate) — remplaçable plus tard par un
    /// simple <ui:Image> une fois qu'un Sprite est assigné (branche 1 prend le relais
    /// automatiquement, zéro changement de code).
    ///
    /// Le conteneur passé doit déjà porter la classe .icon-box ou .icon-box--compact
    /// (Theme.uss) — PopulateIcon ajoute seulement la teinte + le glyphe à l'intérieur.
    /// </summary>
    public static class TileIconResolver
    {
        static readonly string[] TintClasses =
        {
            "tile-icon--realestate", "tile-icon--pharma", "tile-icon--automobile",
            "tile-icon--oilgas", "tile-icon--luxe", "tile-icon--tech",
            "tile-icon--aerospace", "tile-icon--armement", "tile-icon--logistics",
            "tile-icon--go", "tile-icon--prison", "tile-icon--redirect",
            "tile-icon--auction", "tile-icon--safe", "tile-icon--tax",
            "tile-icon--venture", "tile-icon--casino", "tile-icon--stockmarket",
            "tile-icon--neutral"
        };

        public static void PopulateIcon(VisualElement container, BoardTileData tile)
        {
            if (container == null) return;
            container.Clear();
            foreach (var c in TintClasses) container.RemoveFromClassList(c);

            if (tile == null)
            {
                container.AddToClassList("tile-icon--neutral");
                return;
            }

            if (tile is PropertyData withSprite && withSprite.Icon != null)
            {
                var img = new VisualElement();
                img.style.width = Length.Percent(100);
                img.style.height = Length.Percent(100);
                img.style.backgroundImage = new StyleBackground(withSprite.Icon);
                container.Add(img);
                return;
            }

            if (tile is PropertyData hubProp && hubProp.TileType == TileType.Hub)
            {
                container.AddToClassList("tile-icon--logistics");
                BuildLogistics(container);
                return;
            }

            if (tile is PropertyData prop && prop.FamilyData != null)
            {
                BuildFamilyGlyph(container, prop.FamilyData.FamilyName);
                return;
            }

            BuildSpecialGlyph(container, tile.TileType);
        }

        // ─────────────────────────────────────────────────── dispatch

        static void BuildFamilyGlyph(VisualElement box, string familyName)
        {
            if (familyName.Contains("Real Estate")) { box.AddToClassList("tile-icon--realestate"); BuildSkyline(box); }
            else if (familyName.Contains("Pharma")) { box.AddToClassList("tile-icon--pharma"); BuildCross(box); }
            else if (familyName.Contains("Automobile")) { box.AddToClassList("tile-icon--automobile"); BuildCar(box); }
            else if (familyName.Contains("Oil")) { box.AddToClassList("tile-icon--oilgas"); BuildDroplet(box); }
            else if (familyName.Contains("Luxe")) { box.AddToClassList("tile-icon--luxe"); BuildGem(box); }
            else if (familyName.Contains("Tech")) { box.AddToClassList("tile-icon--tech"); BuildChip(box); }
            else if (familyName.Contains("Aerospace")) { box.AddToClassList("tile-icon--aerospace"); BuildRocket(box); }
            else if (familyName.Contains("Armement")) { box.AddToClassList("tile-icon--armement"); BuildCrosshair(box); }
            else if (familyName.Contains("Logistics")) { box.AddToClassList("tile-icon--logistics"); BuildLogistics(box); }
            else { box.AddToClassList("tile-icon--neutral"); BuildRing(box); }
        }

        static void BuildSpecialGlyph(VisualElement box, TileType type)
        {
            switch (type)
            {
                case TileType.Go_Start:      box.AddToClassList("tile-icon--go");         BuildRing(box); break;
                case TileType.Prison:        box.AddToClassList("tile-icon--prison");      BuildBars(box); break;
                case TileType.GoToPrison:    box.AddToClassList("tile-icon--redirect");    BuildRedirectArrow(box); break;
                case TileType.GlobalAuction: box.AddToClassList("tile-icon--auction");     BuildCoins(box); break;
                case TileType.SafeZone:      box.AddToClassList("tile-icon--safe");        BuildShield(box); break;
                case TileType.Tax:           box.AddToClassList("tile-icon--tax");         BuildInvoice(box); break;
                case TileType.Venture:       box.AddToClassList("tile-icon--venture");     BuildCards(box); break;
                case TileType.Casino:        box.AddToClassList("tile-icon--casino");      BuildDie(box); break;
                case TileType.StockMarket:   box.AddToClassList("tile-icon--stockmarket"); BuildTrendArrow(box); break;
                default:                     box.AddToClassList("tile-icon--neutral");     BuildRing(box); break;
            }
        }

        // ─────────────────────────────────────────────────── primitive

        static VisualElement Piece(VisualElement parent, float leftPct, float topPct,
                                    float wPct, float hPct, string extraClass = null, float rotateDeg = 0f)
        {
            var e = new VisualElement();
            e.AddToClassList("glyph-piece");
            if (extraClass != null) e.AddToClassList(extraClass);
            e.style.left = Length.Percent(leftPct);
            e.style.top = Length.Percent(topPct);
            e.style.width = Length.Percent(wPct);
            e.style.height = Length.Percent(hPct);
            if (rotateDeg != 0f) e.style.rotate = new StyleRotate(new Rotate(rotateDeg));
            parent.Add(e);
            return e;
        }

        static void Chevron(VisualElement box, float leftPct)
        {
            Piece(box, leftPct, 22, 8, 32, "glyph-bar", 45f);
            Piece(box, leftPct, 46, 8, 32, "glyph-bar", -45f);
        }

        // ─────────────────────────────────────────────────── glyphes famille

        static void BuildSkyline(VisualElement box)
        {
            Piece(box, 20, 55, 12, 30, "glyph-bar");
            Piece(box, 44, 30, 12, 55, "glyph-bar");
            Piece(box, 68, 45, 12, 40, "glyph-bar");
        }

        static void BuildCross(VisualElement box)
        {
            Piece(box, 25, 42, 50, 16, "glyph-bar");
            Piece(box, 42, 25, 16, 50, "glyph-bar");
        }

        static void BuildCar(VisualElement box)
        {
            Piece(box, 15, 40, 70, 25, "glyph-bar");
            Piece(box, 22, 62, 18, 18, "glyph-circle");
            Piece(box, 60, 62, 18, 18, "glyph-circle");
        }

        static void BuildDroplet(VisualElement box)
        {
            var e = Piece(box, 32, 28, 36, 36, null, 45f);
            e.style.borderTopLeftRadius = Length.Percent(0);
            e.style.borderTopRightRadius = Length.Percent(50);
            e.style.borderBottomLeftRadius = Length.Percent(50);
            e.style.borderBottomRightRadius = Length.Percent(50);
        }

        static void BuildGem(VisualElement box)
        {
            Piece(box, 26, 26, 48, 48, "glyph-outline", 45f);
            Piece(box, 38, 38, 24, 24, "glyph-bar", 45f);
        }

        static void BuildChip(VisualElement box)
        {
            Piece(box, 30, 30, 40, 40, "glyph-outline");
            Piece(box, 44, 8, 12, 14, "glyph-bar");
            Piece(box, 44, 78, 12, 14, "glyph-bar");
            Piece(box, 8, 44, 14, 12, "glyph-bar");
            Piece(box, 78, 44, 14, 12, "glyph-bar");
        }

        static void BuildRocket(VisualElement box)
        {
            Piece(box, 40, 20, 20, 45, "glyph-bar");
            Piece(box, 38, 10, 24, 24, "glyph-bar", 45f);
            Piece(box, 20, 58, 18, 18, "glyph-bar", 45f);
            Piece(box, 62, 58, 18, 18, "glyph-bar", 45f);
        }

        static void BuildCrosshair(VisualElement box)
        {
            Piece(box, 20, 20, 60, 60, "glyph-ring");
            Piece(box, 42, 42, 16, 16, "glyph-circle");
        }

        static void BuildLogistics(VisualElement box)
        {
            Chevron(box, 22);
            Chevron(box, 50);
        }

        static void BuildRing(VisualElement box) => Piece(box, 20, 20, 60, 60, "glyph-ring");

        // ─────────────────────────────────────────────────── glyphes tuiles spéciales

        static void BuildBars(VisualElement box)
        {
            Piece(box, 22, 20, 8, 60, "glyph-bar");
            Piece(box, 42, 20, 8, 60, "glyph-bar");
            Piece(box, 62, 20, 8, 60, "glyph-bar");
        }

        static void BuildRedirectArrow(VisualElement box)
        {
            Chevron(box, 28);
            Piece(box, 58, 30, 8, 40, "glyph-bar");
        }

        static void BuildCoins(VisualElement box)
        {
            Piece(box, 18, 45, 28, 28, "glyph-ring");
            Piece(box, 36, 30, 28, 28, "glyph-ring");
            Piece(box, 54, 45, 28, 28, "glyph-ring");
        }

        static void BuildShield(VisualElement box)
        {
            Piece(box, 24, 18, 52, 60, "glyph-outline");
            Piece(box, 34, 42, 14, 8, "glyph-bar", -45f);
            Piece(box, 44, 30, 22, 8, "glyph-bar", 45f);
        }

        static void BuildInvoice(VisualElement box)
        {
            Piece(box, 28, 15, 44, 60, "glyph-outline");
            Piece(box, 34, 30, 32, 6, "glyph-bar");
            Piece(box, 34, 44, 32, 6, "glyph-bar");
            Piece(box, 34, 58, 20, 6, "glyph-bar");
        }

        static void BuildCards(VisualElement box)
        {
            Piece(box, 24, 24, 44, 56, "glyph-outline", -8f);
            Piece(box, 32, 24, 44, 56, "glyph-outline", 8f);
        }

        static void BuildDie(VisualElement box)
        {
            Piece(box, 20, 20, 60, 60, "glyph-outline");
            Piece(box, 30, 30, 12, 12, "glyph-circle");
            Piece(box, 58, 30, 12, 12, "glyph-circle");
            Piece(box, 30, 58, 12, 12, "glyph-circle");
            Piece(box, 58, 58, 12, 12, "glyph-circle");
            Piece(box, 44, 44, 12, 12, "glyph-circle");
        }

        static void BuildTrendArrow(VisualElement box)
        {
            Piece(box, 25, 45, 50, 8, "glyph-bar", -30f);
            Piece(box, 62, 20, 20, 20, "glyph-bar", 45f);
        }
    }
}
