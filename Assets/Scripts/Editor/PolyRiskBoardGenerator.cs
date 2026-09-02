using UnityEngine;
using UnityEditor;
using PolyRisk.Core;
using System.IO;
using System.Collections.Generic;

namespace PolyRisk.Editor
{
    public class PolyRiskBoardGenerator
    {
        private const string FAMILY_PATH = "Assets/Data/Families";
        private const string PROPERTY_PATH = "Assets/Data/Properties";
        private const string SPECIAL_PATH = "Assets/Data/SpecialTiles";

        [MenuItem("PolyRisk/Generate Full Board")]
        public static void GenerateFullBoard()
        {
            EnsureDirectories();
            var families = GenerateFamilies();
            GenerateTiles(families);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PolyRisk Board Generation Complete!");
        }

        private static void EnsureDirectories()
        {
            CreateFolder("Assets/Data", "Families");
            CreateFolder("Assets/Data", "Properties");
            CreateFolder("Assets/Data", "SpecialTiles");
        }

        private static void CreateFolder(string parent, string folder)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + folder))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static Dictionary<string, FamilyData> GenerateFamilies()
        {
            var families = new Dictionary<string, FamilyData>();

            var familyDefinitions = new List<(string Name, int Rank)>
            {
                ("Real Estate", 1),
                ("Pharma", 2),
                ("Automobile", 3),
                ("Oil & Gas", 4),
                ("Luxe", 5),
                ("Tech", 6),
                ("Aerospace", 7),
                ("Armement", 8),
                ("Logistics", 0)
            };

            foreach (var def in familyDefinitions)
            {
                var asset = GetOrCreateAsset<FamilyData>(FAMILY_PATH, def.Name + " Family");
                SetFamilyData(asset, def.Name, def.Rank);
                families[def.Name] = asset;
            }

            return families;
        }

        private static void SetFamilyData(FamilyData data, string name, int rank)
        {
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("familyName").stringValue = name;
            so.FindProperty("powerRank").intValue = rank;
            so.ApplyModifiedProperties();
        }

        private struct TileDefinition
        {
            public int Index;
            public string Name;
            public TileType Type;
            public double Price;
            public double Rent;
            public string FamilyName;

            public TileDefinition(int index, string name, TileType type, double price = 0, double rent = 0, string familyName = null)
            {
                Index = index;
                Name = name;
                Type = type;
                Price = price;
                Rent = rent;
                FamilyName = familyName;
            }
        }

        private static void GenerateTiles(Dictionary<string, FamilyData> families)
        {
            var tiles = new List<TileDefinition>
            {
                new TileDefinition(0, "GO", TileType.Go_Start),
                new TileDefinition(1, "Zuckerberg's Bunker", TileType.Property, 800, 80, "Real Estate"),
                new TileDefinition(2, "Venture", TileType.Venture),
                new TileDefinition(3, "Trump Tower", TileType.Property, 900, 90, "Real Estate"),
                new TileDefinition(4, "Taxe Douanière", TileType.Tax),
                new TileDefinition(5, "Temu Sweatshop Terminal", TileType.Hub, 1000, 0, "Logistics"),
                new TileDefinition(6, "French Riviera", TileType.Property, 1000, 100, "Real Estate"),
                new TileDefinition(7, "Venture", TileType.Venture),
                new TileDefinition(8, "Opioid Paradise", TileType.Property, 1100, 110, "Pharma"),
                new TileDefinition(9, "Pfizer-BioTech", TileType.Property, 1200, 120, "Pharma"),
                
                new TileDefinition(10, "AUDIT FISCAL", TileType.Prison),
                new TileDefinition(11, "Fentanyl Labs", TileType.Property, 1300, 130, "Pharma"),
                new TileDefinition(12, "Stock Market", TileType.StockMarket),
                new TileDefinition(13, "Hydrogen Speedways", TileType.Property, 1500, 150, "Automobile"),
                new TileDefinition(14, "CyberTruck Factory", TileType.Property, 1600, 160, "Automobile"),
                new TileDefinition(15, "Amazon Prime Drone-Pad", TileType.Hub, 1500, 0, "Logistics"),
                new TileDefinition(16, "Ferrari Maranello", TileType.Property, 1700, 170, "Automobile"),
                new TileDefinition(17, "Venture", TileType.Venture),
                new TileDefinition(18, "Greenwashing Solutions", TileType.Property, 1800, 180, "Oil & Gas"),
                new TileDefinition(19, "Deep Ocean Drill", TileType.Property, 2000, 200, "Oil & Gas"),
                
                new TileDefinition(20, "MARKETPLACE", TileType.GlobalAuction),
                new TileDefinition(21, "Nord Stream Pipe", TileType.Property, 2200, 220, "Oil & Gas"),
                new TileDefinition(22, "Tax / Impôt", TileType.Tax),
                new TileDefinition(23, "LVMH Palace", TileType.Property, 2500, 250, "Luxe"),
                new TileDefinition(24, "Rolex Tower", TileType.Property, 2700, 270, "Luxe"),
                new TileDefinition(25, "SpaceX Uplink Hub", TileType.Hub, 2000, 0, "Logistics"),
                new TileDefinition(26, "Tax-Haven Island", TileType.Property, 3000, 300, "Luxe"),
                new TileDefinition(27, "Antarctica Datacenter", TileType.Property, 3500, 350, "Tech"),
                new TileDefinition(28, "Boston Dynamics Factory", TileType.Property, 4000, 400, "Tech"),
                new TileDefinition(29, "Venture", TileType.Venture),
                
                new TileDefinition(30, "DIRECTION AUDIT", TileType.GoToPrison),
                new TileDefinition(31, "Quantum AI Labs", TileType.Property, 4500, 450, "Tech"),
                new TileDefinition(32, "CASINO", TileType.Casino),
                new TileDefinition(33, "NASA Moonbase", TileType.Property, 5000, 500, "Aerospace"),
                new TileDefinition(34, "Venture", TileType.Venture),
                new TileDefinition(35, "Silk Road 3.0 Port", TileType.Hub, 2500, 0, "Logistics"),
                new TileDefinition(36, "SpaceX Orbital Station", TileType.Property, 5500, 550, "Aerospace"),
                new TileDefinition(37, "Venture", TileType.Venture),
                new TileDefinition(38, "Lockheed Martin", TileType.Property, 6500, 650, "Armement"),
                new TileDefinition(39, "Wagner Army Base", TileType.Property, 7500, 750, "Armement")
            };

            foreach (var tile in tiles)
            {
                if (tile.Type == TileType.Property || tile.Type == TileType.Hub)
                {
                    // Create PropertyData
                    string assetName = $"Tile_{tile.Index}_{SanitizeName(tile.Name)}";
                    var asset = GetOrCreateAsset<PropertyData>(PROPERTY_PATH, assetName);
                    
                    SerializedObject so = new SerializedObject(asset);
                    so.FindProperty("tileName").stringValue = tile.Name;
                    so.FindProperty("basePrice").doubleValue = tile.Price;
                    so.FindProperty("baseRent").doubleValue = tile.Rent;
                    so.FindProperty("tileType").enumValueIndex = (int)tile.Type;
                    so.FindProperty("tileIndex").intValue = tile.Index;
                    
                    if (!string.IsNullOrEmpty(tile.FamilyName) && families.ContainsKey(tile.FamilyName))
                    {
                        so.FindProperty("familyData").objectReferenceValue = families[tile.FamilyName];
                    }
                    
                    so.ApplyModifiedProperties();
                }
                else
                {
                    // Create BoardTileData
                    string assetName = $"Tile_{tile.Index}_{SanitizeName(tile.Name)}";
                    var asset = GetOrCreateAsset<BoardTileData>(SPECIAL_PATH, assetName);
                    
                    SerializedObject so = new SerializedObject(asset);
                    so.FindProperty("tileName").stringValue = tile.Name;
                    so.FindProperty("tileIndex").intValue = tile.Index;
                    so.FindProperty("tileType").enumValueIndex = (int)tile.Type;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static T GetOrCreateAsset<T>(string path, string name) where T : ScriptableObject
        {
            string fullPath = $"{path}/{name}.asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, fullPath);
            }
            return asset;
        }

        private static string SanitizeName(string name)
        {
            return name.Replace(" ", "_").Replace("'", "").Replace("&", "and").Replace("/", "_");
        }
    }
}
