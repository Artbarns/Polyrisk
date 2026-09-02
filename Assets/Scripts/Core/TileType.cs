namespace PolyRisk.Core
{
    /// <summary>
    /// Defines the type of tile on the game board.
    /// Used by the turn loop to resolve the appropriate action.
    /// </summary>
    public enum TileType
    {
        Property,
        Go_Start,
        Prison,          // Audit Fiscal
        GlobalAuction,   // Marketplace
        SafeZone,        // Default / Empty
        Tax,             // Taxe Douanière
        Venture,         // Venture Cards
        Casino,          // Casino
        GoToPrison,      // Direction Audit
        Hub,             // Logistics Hub (treated as property but distinct type if needed)
        StockMarket      // Stock Market
    }
}
