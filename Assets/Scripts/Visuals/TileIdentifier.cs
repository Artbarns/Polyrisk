using UnityEngine;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Simple helper to identify tiles in the scene.
    /// Added to generated tiles since BoardTile is not a Component.
    /// </summary>
    public class TileIdentifier : MonoBehaviour
    {
        public int TileIndex;
        public void SetIndex(int index) => TileIndex = index;
    }
}
