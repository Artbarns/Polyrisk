using System;
using System.Collections.Generic;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Visuals;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyRisk.UI
{
    /// <summary>Read-only tile identities over the existing anchors; financial details stay in the inspector.</summary>
    internal sealed class BoardTileOverlayWidget
    {
        private const string OverlayName = "BoardTileIdentityOverlay";
        private readonly VisualElement root, overlay;
        private readonly Camera camera;
        private readonly BoardManager board;
        private readonly TurnManager turns;
        private readonly ActionButtonsWidget inspector;
        private readonly BoardInteractionManager framing;
        private readonly List<Entry> entries = new List<Entry>();
        private float nextOwnerPoll;
        private bool hasPrevious;
        private Signature previous;

        private sealed class Entry
        {
            public Transform anchor;
            public PropertyData property;
            public PlayerModel ownerPlayer;
            public int tileIndex;
            public VisualElement chip, icon, owner, position;
        }

        private struct Signature : IEquatable<Signature>
        {
            public Vector3 position;
            public Quaternion rotation;
            public float size, width, height;
            public int state, screenWidth, screenHeight;
            public Rect viewport;
            public bool Equals(Signature other) => position == other.position && rotation == other.rotation &&
                size == other.size && width == other.width && height == other.height && state == other.state &&
                screenWidth == other.screenWidth && screenHeight == other.screenHeight && viewport == other.viewport;
        }

        private BoardTileOverlayWidget(VisualElement root, BoardManager board, Camera camera)
        {
            this.root = root; this.board = board; this.camera = camera;
            turns = UnityEngine.Object.FindObjectOfType<TurnManager>();
            inspector = UnityEngine.Object.FindObjectOfType<ActionButtonsWidget>(true);
            framing = UnityEngine.Object.FindObjectOfType<BoardInteractionManager>();
            overlay = new VisualElement { name = OverlayName, pickingMode = PickingMode.Ignore };
            overlay.AddToClassList("board-tile-identity-overlay");
            root.Add(overlay);
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
                overlay.Add(chip);
                entries.Add(new Entry { anchor = anchor, property = tile.Property, tileIndex = i,
                    chip = chip, icon = icon, owner = owner, position = position });
            }
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
                viewport = framing != null ? framing.BoardViewport : new Rect(0,0,Screen.width,Screen.height) };
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
            }
        }
    }
}
