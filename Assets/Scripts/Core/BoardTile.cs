using System;

namespace PolyRisk.Core
{
    /// <summary>
    /// Represents a single tile on the game board.
    /// Acts as a data container for the logical representation of a board space.
    /// </summary>
    [Serializable]
    public class BoardTile
    {
        /// <summary>
        /// The index of the tile on the board (0-39).
        /// </summary>
        public int TileIndex { get; private set; }

        /// <summary>
        /// The type of tile (Property, Go_Start, Prison, etc.).
        /// </summary>
        public TileType Type { get; private set; }

        /// <summary>
        /// The property data associated with this tile.
        /// Can be PropertyData or BoardTileData.
        /// </summary>
        public BoardTileData Data { get; private set; }

        // Deprecated: public PropertyData Property { get; private set; }

        /// <summary>
        /// Constructor for a tile.
        /// </summary>
        /// <param name="index">Position on the board.</param>
        /// <param name="type">Type of the tile.</param>
        /// <param name="data">Associated data (optional).</param>
        public BoardTile(int index, TileType type, BoardTileData data = null)
        {
            TileIndex = index;
            // Prefer data type if available, otherwise use passed type
            Type = (data != null) ? data.TileType : type;
            Data = data;
        }

        /// <summary>
        /// Helper property to safely access as PropertyData.
        /// </summary>
        public PropertyData Property => Data as PropertyData;
    }
}
