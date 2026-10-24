using System;
using System.Collections.Generic;
using System.Globalization;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Visuals;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyRisk.UI
{
    /// <summary>Read-only tile identities and optional base-yield analysis over the existing anchors.</summary>
    internal sealed class BoardTileOverlayWidget
    {
        private const string OverlayName = "BoardTileYieldOverlay";
        private readonly VisualElement root, overlay, legend;
        private readonly Button toggle;
        private readonly Camera camera;
        private readonly BoardManager board;
        private readonly TurnManager turns;
        private readonly ActionButtonsWidget inspector;
        private readonly BoardInteractionManager framing;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Rect> placedBadges = new List<Rect>();
        private float nextOwnerPoll;
        private bool analysis;
        private bool hasPrevious;
        private Signature previous;

        private sealed class Entry
        {
            public Transform anchor;
            public PropertyData property;
            public PlayerModel ownerPlayer;
            public int tileIndex;
            public VisualElement chip, icon, owner, position, badge, leader;
            public Label yield;
        }

        private struct Signature : IEquatable<Signature>
        {
            public Vector3 position;
            public Quaternion rotation;
            public float size, width, height;
            public int state, screenWidth, screenHeight;
            public Rect viewport;
            public bool analysis;
            public bool Equals(Signature other) => position == other.position && rotation == other.rotation &&
                size == other.size && width == other.width && height == other.height && state == other.state &&
                screenWidth == other.screenWidth && screenHeight == other.screenHeight && viewport == other.viewport && analysis == other.analysis;
        }

        private BoardTileOverlayWidget(VisualElement root, BoardManager board, Camera camera)
        {
            this.root = root; this.board = board; this.camera = camera;
            turns = UnityEngine.Object.FindObjectOfType<TurnManager>();
            inspector = UnityEngine.Object.FindObjectOfType<ActionButtonsWidget>(true);
            framing = UnityEngine.Object.FindObjectOfType<BoardInteractionManager>();
            overlay = new VisualElement { name = OverlayName, pickingMode = PickingMode.Ignore };
            overlay.AddToClassList("board-tile-yield-overlay");
            root.Add(overlay);
            toggle = root.Q<Button>("Btn_BoardAnalysis");
            legend = root.Q<VisualElement>("BoardAnalysisLegend");
            if (toggle != null) toggle.clicked += ToggleAnalysis;
            BuildEntries();
            root.RegisterCallback<GeometryChangedEvent>(_ => hasPrevious = false);
            if (framing != null)
            {
                framing.FramingChanged += OnFramingChanged;
                root.RegisterCallback<DetachFromPanelEvent>(_ => framing.FramingChanged -= OnFramingChanged);
                root.RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    framing.FramingChanged -= OnFramingChanged;
                    framing.FramingChanged += OnFramingChanged;
                });
            }
            root.schedule.Execute(Poll).Every(33);
        }

        public static void Mount(VisualElement root)
        {
            if (root == null || root.Q<VisualElement>(OverlayName) != null) return;
            var board = BoardManager.Instance ?? UnityEngine.Object.FindObjectOfType<BoardManager>();
            if (board != null && Camera.main != null) new BoardTileOverlayWidget(root, board, Camera.main);
        }

        private void ToggleAnalysis()
        {
            analysis = !analysis;
            root.EnableInClassList("board-analysis", analysis);
            if (toggle != null)
            {
                toggle.text = analysis ? "Retour normal" : "Analyse";
                toggle.tooltip = analysis ? "Masquer les rendements de base" : "Afficher les rendements de base sur le plateau";
            }
            hasPrevious = false;
        }

        private void BuildEntries()
        {
            if (board.visualTiles == null) return;
            for (int i = 0; i < 40 && i < board.visualTiles.Length; i++)
            {
                var tile = board.GetTileAt(i);
                var anchor = board.visualTiles[i];
                if (tile == null || anchor == null) continue;
                var chip = new VisualElement { name = "BoardIdentity_" + i, pickingMode = PickingMode.Ignore };
                chip.AddToClassList("board-tile-identity");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("icon-box--compact"); icon.AddToClassList("board-identity-icon");
                TileIconResolver.PopulateIcon(icon, tile.Data);
                var owner = new VisualElement { pickingMode = PickingMode.Ignore };
                owner.AddToClassList("board-owner-marker");
                var position = new VisualElement { pickingMode = PickingMode.Ignore };
                position.AddToClassList("board-position-marker");
                chip.Add(icon); chip.Add(owner); chip.Add(position);
                var leader = new VisualElement { pickingMode = PickingMode.Ignore };
                leader.AddToClassList("board-analysis-leader");
                overlay.Add(leader); overlay.Add(chip);
                var badge = new VisualElement { name = "BoardYield_" + i, pickingMode = PickingMode.Ignore };
                badge.AddToClassList("board-tile-analysis-badge");
                var property = tile.Property;
                var label = new Label(property != null ? FormatYield(property) : "") { pickingMode = PickingMode.Ignore };
                label.AddToClassList("board-tile-yield-value");
                badge.Add(label); overlay.Add(badge);
                entries.Add(new Entry { anchor = anchor, property = property, tileIndex = i,
                    chip = chip, icon = icon, owner = owner, position = position, badge = badge, leader = leader, yield = label });
            }
        }

        private static string FormatYield(PropertyData property)
        {
            double percent = property.BasePrice > 0.0 ? property.BaseRent / property.BasePrice * 100.0 : 0.0;
            return percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private void Poll()
        {
            if (root.panel == null || camera == null || root.worldBound.width <= 0 || Screen.width <= 0) return;
            if (entries.Count == 0) BuildEntries();
            if (Time.unscaledTime >= nextOwnerPoll)
            {
                nextOwnerPoll = Time.unscaledTime + 0.15f;
                foreach (var entry in entries) entry.ownerPlayer = null;
                if (turns?.Players != null) foreach (var player in turns.Players)
                    foreach (var property in player.OwnedProperties)
                        foreach (var entry in entries) if (entry.property == property) entry.ownerPlayer = player;
            }
            int state = inspector != null ? inspector.InspectedTileIndex : -1;
            if (turns?.CurrentPlayer != null) state = unchecked(state * 397 ^ turns.GetPlayerPosition(turns.CurrentPlayer));
            foreach (var entry in entries)
            {
                if (entry.property == null) continue;
                int ownerId = entry.ownerPlayer?.PlayerId ?? -1;
                state = unchecked(state * 397 ^ ownerId);
                state = unchecked(state * 397 ^ entry.property.HijackTurnsRemaining);
            }
            var current = new Signature { position = camera.transform.position, rotation = camera.transform.rotation,
                size = camera.orthographicSize, width = root.worldBound.width, height = root.worldBound.height,
                screenWidth = Screen.width, screenHeight = Screen.height, state = state,
                viewport = framing != null ? framing.BoardViewport : new Rect(0,0,Screen.width,Screen.height), analysis = analysis };
            if (hasPrevious && previous.Equals(current)) return;
            previous = current; hasPrevious = true;
            Refresh();
        }

        private void OnFramingChanged()
        {
            if (root.panel == null || root.worldBound.width <= 0 || root.worldBound.height <= 0) return;
            hasPrevious = false;
            Refresh();
        }

        private Vector2 PanelPoint(Vector3 world)
        {
            Vector3 screen = camera.WorldToScreenPoint(world);
            return new Vector2(screen.x * root.worldBound.width / Screen.width,
                (Screen.height - screen.y) * root.worldBound.height / Screen.height);
        }

        private void Refresh()
        {
            float scale = root.worldBound.width / Screen.width;
            Rect v = framing != null ? framing.BoardViewport : new Rect(0,0,Screen.width,Screen.height);
            Rect available = new Rect(v.x * scale, (Screen.height - v.yMax) * scale, v.width * scale, v.height * scale);
            if (legend != null)
            {
                var hud = root.Q<VisualElement>("TopHUD_Bar");
                if (hud != null) legend.style.top = hud.worldBound.yMax - root.worldBound.yMin + 4f;
            }
            placedBadges.Clear();
            // Keep identities, ownership and selection readable under the analysis layer.
            foreach (var entry in entries)
            {
                if (entry.anchor == null) continue;
                Vector2 point = PanelPoint(entry.anchor.position + Vector3.up * 0.02f);
                placedBadges.Add(new Rect(point.x - 9, point.y - 8, 18, 16));
            }
            int activePosition = turns?.CurrentPlayer != null ? turns.GetPlayerPosition(turns.CurrentPlayer) : -1;
            int selected = inspector != null ? inspector.InspectedTileIndex : -1;
            foreach (var entry in entries)
            {
                if (entry.anchor == null) continue;
                Vector3 world = entry.anchor.position + Vector3.up * 0.02f;
                Vector2 center = PanelPoint(world);
                bool visible = center.x >= 0 && center.x <= root.worldBound.width && center.y >= 0 && center.y <= root.worldBound.height;
                entry.chip.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                entry.chip.style.left = center.x - 9; entry.chip.style.top = center.y - 8;
                entry.chip.EnableInClassList("board-tile-identity--selected", selected == entry.tileIndex);
                bool sabotaged = entry.property != null && entry.property.HijackTurnsRemaining > 0 && entry.property.HijackedBy != null;
                entry.chip.EnableInClassList("board-tile-identity--sabotaged", sabotaged);
                var owner = entry.ownerPlayer;
                entry.owner.style.display = owner != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (owner != null) entry.owner.style.backgroundColor = PlayerVisualManager.GetPlayerColor(owner.PlayerId);
                entry.position.style.display = activePosition == entry.tileIndex ? DisplayStyle.Flex : DisplayStyle.None;
                entry.badge.style.display = DisplayStyle.None;
                entry.leader.style.display = DisplayStyle.None;
                if (!analysis || !visible || entry.property == null) continue;
                Vector2 inward = (PanelPoint(world + entry.anchor.forward) - center).normalized;
                Rect rect = PlaceBadge(center, inward, available, entry.tileIndex);
                placedBadges.Add(rect);
                entry.yield.text = FormatYield(entry.property);
                entry.badge.style.display = DisplayStyle.Flex;
                entry.badge.style.left = rect.x; entry.badge.style.top = rect.y;
                entry.badge.EnableInClassList("board-tile-yield-chip--sabotaged", sabotaged);
                Vector2 end = new Vector2(Mathf.Clamp(center.x, rect.xMin, rect.xMax), Mathf.Clamp(center.y, rect.yMin, rect.yMax));
                Vector2 delta = end - center;
                if (delta.magnitude > 2)
                {
                    entry.leader.style.display = DisplayStyle.Flex;
                    entry.leader.style.width = delta.magnitude;
                    entry.leader.style.left = (center.x + end.x) * 0.5f - delta.magnitude * 0.5f;
                    entry.leader.style.top = (center.y + end.y) * 0.5f - 0.5f;
                    entry.leader.style.rotate = new Rotate(new Angle(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, AngleUnit.Degree));
                }
            }
        }

        private Rect PlaceBadge(Vector2 center, Vector2 inward, Rect available, int tileIndex)
        {
            Rect candidate = default;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int lane = (attempt / 2 + tileIndex % 3) % 12;
                float offset = (12f + lane * 17f) * (attempt % 2 == 0 ? 1f : -1f);
                Vector2 p = center + inward * offset;
                candidate = new Rect(Mathf.Clamp(p.x - 16, available.xMin, Mathf.Max(available.xMin, available.xMax - 32)),
                    Mathf.Clamp(p.y - 8, available.yMin, Mathf.Max(available.yMin, available.yMax - 16)), 32, 16);
                bool collision = false;
                foreach (var placed in placedBadges)
                    if (new Rect(placed.x - 1, placed.y - 1, placed.width + 2, placed.height + 2).Overlaps(candidate)) { collision = true; break; }
                if (!collision) return candidate;
            }
            return candidate;
        }
    }
}
