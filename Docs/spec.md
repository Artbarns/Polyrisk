# Board Generation Spec

## 1. Data Structures

### FamilyData (ScriptableObject)
- **Path**: `Assets/Scripts/Core/FamilyData.cs`
- **Fields**:
  - `familyName` (string)
  - `powerRank` (int)

### PropertyData (ScriptableObject)
- **Path**: `Assets/Scripts/Core/PropertyData.cs`
- **Fields**:
  - `propertyName` (string)
  - `basePrice` (double)
  - `baseRent` (double)
  - `familyData` (FamilyData)
  - `tileIndex` (int)

### BoardTileData (ScriptableObject)
- **Path**: `Assets/Scripts/Core/BoardTileData.cs`
- **Fields**:
  - `tileName` (string)
  - `tileIndex` (int)
  - `tileType` (TileType enum - optional but good for identification)

## 2. Editor Script Logic (`PolyRiskBoardGenerator.cs`)

### Menu Item
`PolyRisk/Generate Full Board`

### Generation Steps
1.  **Create Directories**:
    - `Assets/Data/Families`
    - `Assets/Data/Properties`
    - `Assets/Data/SpecialTiles` (for non-properties)

2.  **Generate Families**:
    - Create `FamilyData` assets in `Assets/Data/Families`.
    - **List**:
      - Real Estate (Rank 1)
      - Pharma (Rank 2)
      - Automobile (Rank 3)
      - Oil & Gas (Rank 4)
      - Luxe (Rank 5)
      - Tech (Rank 6)
      - Aerospace (Rank 7)
      - Armement (Rank 8)
      - Logistics (Rank 0 - Special family for Hubs to organize them)

3.  **Generate Tiles**:
    - Iterate from 0 to 39.
    - **Data Array**:
      - 0: GO (Go_Start)
      - 1: Zuckerberg's Bunker (Real Estate, 800, 80)
      - 2: Venture (Venture)
      - 3: Trump Tower (Real Estate, 900, 90)
      - 4: Taxe Douanière (Tax)
      - 5: Temu Sweatshop Terminal (Hub 1, 1000, 0, Logistics)
      - 6: French Riviera (Real Estate, 1000, 100)
      - 7: Venture (Venture)
      - 8: Opioid Paradise (Pharma, 1100, 110)
      - 9: Pfizer-BioTech (Pharma, 1200, 120)
      - 10: AUDIT FISCAL (Prison)
      - 11: Fentanyl Labs (Pharma, 1300, 130)
      - 12: Stock Market (StockMarket)
      - 13: Hydrogen Speedways (Automobile, 1500, 150)
      - 14: CyberTruck Factory (Automobile, 1600, 160)
      - 15: Amazon Prime Drone-Pad (Hub 2, 1500, 0, Logistics)
      - 16: Ferrari Maranello (Automobile, 1700, 170)
      - 17: Venture (Venture)
      - 18: Greenwashing Solutions (Oil & Gas, 1800, 180)
      - 19: Deep Ocean Drill (Oil & Gas, 2000, 200)
      - 20: MARKETPLACE (GlobalAuction)
      - 21: Nord Stream Pipe (Oil & Gas, 2200, 220)
      - 22: Tax / Impôt (Tax)
      - 23: LVMH Palace (Luxe, 2500, 250)
      - 24: Rolex Tower (Luxe, 2700, 270)
      - 25: SpaceX Uplink Hub (Hub 3, 2000, 0, Logistics)
      - 26: Tax-Haven Island (Luxe, 3000, 300)
      - 27: Antarctica Datacenter (Tech, 3500, 350)
      - 28: Boston Dynamics Factory (Tech, 4000, 400)
      - 29: Venture (Venture)
      - 30: DIRECTION AUDIT (GoToPrison)
      - 31: Quantum AI Labs (Tech, 4500, 450)
      - 32: CASINO (Casino)
      - 33: NASA Moonbase (Aerospace, 5000, 500)
      - 34: Venture (Venture)
      - 35: Silk Road 3.0 Port (Hub 4, 2500, 0, Logistics)
      - 36: SpaceX Orbital Station (Aerospace, 5500, 550)
      - 37: Venture (Venture)
      - 38: Lockheed Martin (Armement, 6500, 650)
      - 39: Wagner Army Base (Armement, 7500, 750)

4.  **Asset Creation**:
    - For Properties: `PropertyData` instance. Name = Tile Name. Set Price, Rent, Family, TileIndex.
    - For Special: `BoardTileData` instance. Name = Tile Name. Set TileIndex.

5.  **Save Assets**:
    - Use `AssetDatabase.CreateAsset`.
    - Use `AssetDatabase.SaveAssets`.
    - Use `AssetDatabase.Refresh`.
