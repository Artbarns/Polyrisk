using UnityEngine;

namespace PolyRisk.Core
{
    /// <summary>
    /// Represents the immutable data for a non-property tile on the game board.
    /// Used for Special tiles like GO, Venture, Casino, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "New Board Tile", menuName = "PolyRisk/BoardTileData")]
    public class BoardTileData : ScriptableObject
    {
        [Header("Tile Information")]
        [Tooltip("The display name of the tile.")]
        [SerializeField] private string tileName;

        [Tooltip("The index of the tile on the board (0-39).")]
        [SerializeField] private int tileIndex;

        [Tooltip("The type of tile.")]
        [SerializeField] private TileType tileType;

        public string TileName => tileName;
        public int TileIndex => tileIndex;
        public TileType TileType => tileType;

        public void Initialize(string name, int index, TileType type)
        {
            tileName = name;
            tileIndex = index;
            tileType = type;
        }
    }
}
