using System;
using System.Collections.Generic;
using System.Globalization;
using PolyRisk.Core;
using PolyRisk.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyRisk.UI
{
    /// <summary>
    /// Lightweight screen-space labels for property tiles. Reuses the inspector's
    /// family glyph and baseRent/basePrice yield formula; never mutates game data.
    /// Layout is recalculated only when the camera or viewport changes.
    /// </summary>
    internal sealed class BoardTileOverlayWidget
    {
        private const string OverlayName = "BoardTileYieldOverlay";
        private const int PropertyTileCount = 40;
        private readonly VisualElement root;
        private readonly VisualElement overlay;
        private readonly Camera camera;
        private readonly BoardManager board;
        private readonly List<Entry> entries = new List<Entry>();
        private Signature previous;
        private bool hasPrevious;

        private sealed class Entry
        {
            public Transform anchor;
            public PropertyData property;
            public int tileIndex;
            public VisualElement chip;
            public VisualElement icon;
            public Label yield;
            public Vector3 worldPoint;
        }

        private struct Signature : IEquatable<Signature>
        {
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public float orthographicSize;
            public int screenWidth;
            public int screenHeight;
            public float rootWidth;
            public float rootHeight;
            public ulong sabotagedTileMask;

            public bool Equals(Signature other) =>
                cameraPosition == other.cameraPosition && cameraRotation == other.cameraRotation &&
                Mathf.Approximately(orthographicSize, other.orthographicSize) &&
                screenWidth == other.screenWidth && screenHeight == other.screenHeight &&
                Mathf.Approximately(rootWidth, other.rootWidth) && Mathf.Approximately(rootHeight, other.rootHeight) &&
                sabotagedTileMask == other.sabotagedTileMask;
        }

        private BoardTileOverlayWidget(VisualElement root, BoardManager board, Camera camera)
        {
            this.root = root;
            this.board = board;
            this.camera = camera;
            overlay = new VisualElement { name = OverlayName, pickingMode = PickingMode.Ignore };
            overlay.AddToClassList("board-tile-yield-overlay");
            root.Add(overlay);
            BuildEntries();
            root.RegisterCallback<GeometryChangedEvent>(_ => Refresh(force: true));
            root.schedule.Execute(Poll).Every(150);
            Refresh(force: true);
        }

        public static void Mount(VisualElement root)
        {
            if (root == null || root.Q<VisualElement>(OverlayName) != null) return;
            var board = BoardManager.Instance != null ? BoardManager.Instance : UnityEngine.Object.FindObjectOfType<BoardManager>();
            var camera = Camera.main;
            if (board == null || camera == null) return;
            new BoardTileOverlayWidget(root, board, camera);
        }

        private void BuildEntries()
        {
            if (board.visualTiles == null) return;
            for (int i = 0; i < PropertyTileCount && i < board.visualTiles.Length; i++)
            {
                var tile = board.GetTileAt(i);
                var property = tile != null ? tile.Property : null;
                var anchor = board.visualTiles[i];
                if (property == null || anchor == null) continue;

                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("board-tile-yield-chip");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("icon-box--compact");
                icon.AddToClassList("board-tile-yield-icon");
                TileIconResolver.PopulateIcon(icon, property);
                var label = new Label(FormatYield(property));
                label.pickingMode = PickingMode.Ignore;
                label.AddToClassList("board-tile-yield-value");
                chip.Add(icon);
                chip.Add(label);
                overlay.Add(chip);

                entries.Add(new Entry
                {
                    anchor = anchor,
                    property = property,
                    tileIndex = tile.TileIndex,
                    chip = chip,
                    icon = icon,
                    yield = label,
                    worldPoint = anchor.position + anchor.forward * 0.24f + Vector3.up * 0.02f
                });
            }

            if (entries.Count == 0)
            {
                overlay.RemoveFromHierarchy();
                root.schedule.Execute(() => Mount(root)).StartingIn(250);
            }
        }

        private static string FormatYield(PropertyData property)
        {
            double percent = property.BasePrice > 0.0 ? property.BaseRent / property.BasePrice * 100.0 : 0.0;
            return percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private void Poll()
        {
            if (root.panel == null) return;
            ulong sabotagedTileMask = 0;
            foreach (var entry in entries)
            {
                var property = entry.property;
                if (entry.tileIndex >= 0 && entry.tileIndex < 64 && property != null
                    && property.HijackTurnsRemaining > 0 && property.HijackedBy != null)
                    sabotagedTileMask |= 1UL << entry.tileIndex;
            }
            var current = new Signature
            {
                cameraPosition = camera.transform.position,
                cameraRotation = camera.transform.rotation,
                orthographicSize = camera.orthographicSize,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                rootWidth = root.resolvedStyle.width,
                rootHeight = root.resolvedStyle.height,
                sabotagedTileMask = sabotagedTileMask
            };
            if (hasPrevious && previous.Equals(current)) return;
            previous = current;
            hasPrevious = true;
            Refresh(force: false);
        }

        private void Refresh(bool force)
        {
            if (root.panel == null || camera == null || Screen.width <= 0 || Screen.height <= 0) return;
            float rootWidth = root.resolvedStyle.width;
            float rootHeight = root.resolvedStyle.height;
            if (rootWidth <= 0f || rootHeight <= 0f) return;

            // In landscape the same 40-tile board has much less screen height;
            // use smaller markers so neighboring ROI labels do not collide.
            bool compactLandscape = rootWidth >= rootHeight * 1.35f;
            float scaleX = rootWidth / Screen.width;
            float scaleY = rootHeight / Screen.height;
            Rect rootBounds = root.worldBound;
            foreach (var entry in entries)
            {
                bool sabotaged = entry.property != null && entry.property.HijackTurnsRemaining > 0
                    && entry.property.HijackedBy != null;
                entry.chip.EnableInClassList("board-tile-yield-chip--sabotaged", sabotaged);
                entry.chip.tooltip = sabotaged ? "Cyber-sabotage active" : string.Empty;
                if (entry.anchor == null) { entry.chip.style.display = DisplayStyle.None; continue; }
                entry.worldPoint = entry.anchor.position + entry.anchor.forward * 0.24f + Vector3.up * 0.02f;
                Vector3 screen = camera.WorldToScreenPoint(entry.worldPoint);
                if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height)
                {
                    entry.chip.style.display = DisplayStyle.None;
                    continue;
                }

                Vector3 along = camera.WorldToScreenPoint(entry.worldPoint + entry.anchor.right * 0.55f);
                Vector3 inward = camera.WorldToScreenPoint(entry.worldPoint + entry.anchor.forward * 0.30f);
                float longSize = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(along.x, along.y)) * 2f * scaleX;
                float depthSize = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(inward.x, inward.y)) * 2f * scaleY;
                // Keep text upright on every board edge. A compact vertical stack fits
                // the narrow sides; the long sides can use a horizontal icon/ROI pair.
                Vector2 axis = new Vector2(along.x - screen.x, screen.y - along.y);
                bool horizontalTile = Mathf.Abs(axis.x) >= Mathf.Abs(axis.y);
                float width = compactLandscape
                    ? Mathf.Clamp((horizontalTile ? longSize : depthSize) * 0.62f, 26f, 36f)
                    : Mathf.Clamp((horizontalTile ? longSize : depthSize) * 0.76f, 30f, 42f);
                float height = compactLandscape
                    ? Mathf.Clamp((horizontalTile ? depthSize : longSize) * 0.56f, 18f, 22f)
                    : Mathf.Clamp((horizontalTile ? depthSize : longSize) * 0.66f, 20f, 27f);

                float x = screen.x * scaleX - rootBounds.x - width * 0.5f;
                float y = (Screen.height - screen.y) * scaleY - rootBounds.y - height * 0.5f;
                entry.chip.style.display = DisplayStyle.Flex;
                entry.chip.style.left = x;
                entry.chip.style.top = y;
                entry.chip.style.width = width;
                entry.chip.style.height = height;
                entry.chip.style.flexDirection = FlexDirection.Row;
                entry.icon.style.width = compactLandscape
                    ? Mathf.Clamp(height * 0.38f, 8f, 9f)
                    : Mathf.Clamp(height * 0.40f, 9f, 11f);
                entry.icon.style.height = compactLandscape
                    ? Mathf.Clamp(height * 0.38f, 8f, 9f)
                    : Mathf.Clamp(height * 0.40f, 9f, 11f);
                // The projected tile size changes substantially across phones and
                // orientations. Keep the ROI label readable without letting it
                // grow beyond the available tile marker.
                entry.yield.style.fontSize = compactLandscape
                    ? Mathf.Clamp(height * 0.40f, 7.5f, 8.5f)
                    : Mathf.Clamp(height * 0.43f, 8.5f, 10f);
            }
        }
    }
}
