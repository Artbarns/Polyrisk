using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PolyRisk.Core;
using PolyRisk.UI;
using PolyRisk.Visuals;

namespace PolyRisk.Editor
{
    /// <summary>
    /// Guards the UI Toolkit screen assets against missing templates and critical
    /// named controls. This verifies UXML structure, not runtime layout or visuals.
    /// </summary>
    public static class UIAssetRegression
    {
        private static int assertions;

        public static void Run()
        {
            assertions = 0;
            CheckPulseCurrencyFormatting();
            CheckActivityFeedLanguageConsistency();
            CheckPlayerRosterShortNames();
            CheckTypedGameEventFormatting();
            CheckAsset("Assets/UI/Resources/MainMenu.uxml",
                "HomeView", "Input_PlayerName", "Input_GameCode", "Btn_CreateGame", "Btn_JoinGame",
                "Btn_LocalGame", "Toggle_ReduceMotionHome", "Input_ServerUrl", "LobbyView", "Lbl_LobbyCode",
                "Lbl_LobbyStatus", "Lbl_PlayerRoster", "Btn_Ready", "Btn_StartOnline", "Btn_LeaveLobby",
                "Toggle_ReduceMotionLobby", "PauseView", "Btn_ResumeGame", "Btn_QuitGame",
                "QuitConfirmView", "Lbl_QuitConfirmMessage", "Btn_ConfirmQuit", "Btn_CancelQuit");
            CheckAsset("Assets/UI/Layouts/TopHUD.uxml",
                "TopHUD_Bar", "Lbl_Capital", "Lbl_NetWorth", "Lbl_Turn", "Lbl_Market", "Btn_GameMenu",
                "PlayerRoster", "ActivityFeed", "Lbl_LocalPlayerStatus", "ActivityFeedStack");
            CheckAsset("Assets/UI/Layouts/TileDetail_BottomSheet.uxml",
                "Panel_TileDetail", "GameStateDock", "Lbl_GameStateTitle", "Lbl_GameStateMessage",
                "Btn_GameStateHistory", "Btn_SkipGameEvent", "GameStateHistory", "GameStateHistoryList",
                "Icon_TileFamily", "Lbl_PulseOwnership", "Btn_PulseFamilyProgress",
                "Lbl_PulseFamilyOwned", "Lbl_PulseFamilyTotal", "PulseFamilySlots",
                "Lbl_TileName", "Lbl_TileFamily", "Lbl_TileEffect",
                "Lbl_TileRent", "Lbl_TileRisk", "Lbl_TileRoi", "Btn_Primary", "Slider_Buy",
                "Lbl_BuySlide", "Handle_BuySlide", "Btn_Secondary", "Btn_Tertiary",
                "Btn_PulseToggle", "Lbl_PulseToggleTitle", "Lbl_PulseToggleState",
                "Btn_PulseGoSummary", "Btn_PulseInspector", "Btn_PulsePortfolio", "Btn_PulseGo",
                "Lbl_PulseGoBase", "Lbl_PulseGoCosts", "Lbl_PulseGoNet", "Lbl_PrimaryTitle", "Lbl_PrimaryValue",
                "PulseDetails", "PulseTabContent");
            CheckAsset("Assets/UI/Layouts/MarketplaceChoice.uxml",
                "Btn_Close", "Btn_InitiateTender", "Btn_Sanctions");
            CheckAsset("Assets/UI/Layouts/Marketplace_Main.uxml",
                "Panel_Marketplace_Hub", "Btn_CloseHub", "Container_HubForks", "Btn_GoTenders", "Btn_GoSanctions");
            CheckAsset("Assets/UI/Layouts/Marketplace_Sanctions.uxml",
                "Panel_Marketplace_Sanctions", "Btn_CloseSanctions", "Container_SanctionsList",
                "Btn_SanctionEmbargo", "Btn_SanctionAudit", "Btn_SanctionStrike", "Btn_SanctionSabotage",
                "Container_SanctionConfirm", "Lbl_SanctionConfirm", "Btn_ConfirmSanction");
            CheckAsset("Assets/UI/Layouts/Marketplace_Select_Player.uxml",
                "Panel_Marketplace_SelectPlayer", "Btn_CloseSelectPlayer", "Lbl_SelectTitle", "Lbl_SelectDesc",
                "Container_SelectGrid", "Container_SelectConfirmBar", "Lbl_SelectConfirmCost", "Btn_SelectConfirm");
            CheckAsset("Assets/UI/Layouts/Marketplace_Tender_Init.uxml",
                "Panel_Marketplace_TenderInit", "Btn_CloseTenders", "Container_OwnedAssets", "Btn_PickProperty",
                "Btn_ConfirmTender");
            CheckAsset("Assets/UI/Layouts/Marketplace_Tender_Review.uxml",
                "Panel_Marketplace_TenderReview", "Btn_CloseTenderReview", "Container_BidList",
                "Btn_RejectAll", "Btn_SealDeal");
            CheckAsset("Assets/UI/Layouts/Marketplace_Waiting.uxml",
                "Panel_Marketplace_Waiting", "Container_WaitingProp", "Lbl_WaitingPropName",
                "Lbl_WaitingStatus", "Lbl_WaitingBidCount", "Btn_CancelWaiting");
            CheckAsset("Assets/UI/Layouts/Marketplace_Notification.uxml",
                "Panel_Marketplace_Notification", "Label_Title", "Label_Asset", "Label_Timer",
                "Btn_Skip", "Btn_Participate");
            CheckAsset("Assets/UI/Layouts/Marketplace_Bidder_Form.uxml",
                "Panel_Marketplace_BidderForm", "Lbl_BidSelectionHint", "Scroll_BidDetails",
                "Slider_CashOffer", "Container_BidAssets", "Btn_PickPropertyBid", "Label_TotalValue",
                "Btn_Skip", "Btn_SubmitBid");
            CheckAsset("Assets/UI/Layouts/Marketplace_Bidder_Spectator.uxml",
                "Panel_Marketplace_BidderSpectator", "Container_SpectatorBidList", "Lbl_SpectatorFooter");
            CheckAsset("Assets/UI/Layouts/AuctionItem_Component.uxml",
                "Item_BidRow", "Icon_PlayerAvatar", "Txt_PlayerName", "Container_Offers",
                "Tag_PropOffered", "Tag_CashOffered");
            CheckAsset("Assets/UI/Resources/VentureCardModal.uxml",
                "VentureOverlay", "VentureCard", "VentureTag", "Lbl_VentureTag", "Lbl_VentureId",
                "Lbl_VentureTitle", "Lbl_VentureDesc", "Btn_VentureConfirm");
            CheckAsset("Assets/UI/Resources/BankruptcyScreen.uxml",
                "BankruptcyOverlay", "BankruptcyCard", "Lbl_BankTitle", "Lbl_BankDebtLine",
                "Lbl_BankDebtValue", "Container_BankAssets", "Lbl_BankCurrentCash", "Btn_BankGiveUp");
            CheckAsset("Assets/UI/Resources/VictoryScreen.uxml",
                "VictoryOverlay", "VictoryCard", "Lbl_VictoryTitle", "Lbl_VictoryWinner",
                "Lbl_VictoryCondition", "Container_VictoryStats", "Btn_NewGame");

            // Check the element types used by code paths that submit actions or text input.
            var menu = LoadTree("Assets/UI/Resources/MainMenu.uxml");
            CheckButton(menu, "Btn_CreateGame"); CheckButton(menu, "Btn_JoinGame");
            CheckButton(menu, "Btn_Ready"); CheckButton(menu, "Btn_StartOnline");
            CheckButton(menu, "Btn_ConfirmQuit"); CheckButton(menu, "Btn_CancelQuit");
            CheckType<TextField>(menu, "Input_PlayerName"); CheckType<TextField>(menu, "Input_GameCode");
            CheckType<Toggle>(menu, "Toggle_ReduceMotionHome"); CheckType<Toggle>(menu, "Toggle_ReduceMotionLobby");

            var inspector = LoadTree("Assets/UI/Layouts/TileDetail_BottomSheet.uxml");
            CheckButton(inspector, "Btn_Primary"); CheckButton(inspector, "Btn_Secondary");
            CheckButton(inspector, "Btn_PulseToggle"); CheckButton(inspector, "Btn_PulseGoSummary");
            CheckType<Label>(inspector, "Lbl_PulseToggleTitle");
            CheckType<Label>(inspector, "Lbl_PulseToggleState");
            CheckButton(inspector, "Btn_PulseFamilyProgress");
            CheckType<Label>(inspector, "Lbl_PulseFamilyOwned");
            CheckType<Label>(inspector, "Lbl_PulseFamilyTotal");
            CheckType<VisualElement>(inspector, "PulseFamilySlots");
            CheckButton(inspector, "Btn_PulseInspector"); CheckButton(inspector, "Btn_PulsePortfolio"); CheckButton(inspector, "Btn_PulseGo");
            CheckType<Label>(inspector, "Lbl_PulseOwnership");
            CheckType<Label>(inspector, "Lbl_PulseGoBase");
            CheckType<Label>(inspector, "Lbl_PulseGoCosts");
            CheckType<Label>(inspector, "Lbl_PulseGoNet");
            CheckType<Label>(inspector, "Lbl_PrimaryTitle");
            CheckType<Label>(inspector, "Lbl_PrimaryValue");
            CheckType<ScrollView>(inspector, "PulseTabScroll");
            CheckType<VisualElement>(inspector, "PulseTabContent");
            CheckType<VisualElement>(inspector, "PulseDiceHost");
            CheckType<VisualElement>(inspector, "Slider_Buy");

            var sanctionTarget = LoadTree("Assets/UI/Layouts/Marketplace_Select_Player.uxml");
            CheckButton(sanctionTarget, "Btn_SelectConfirm");
            CheckType<ScrollView>(sanctionTarget, "Container_SelectGrid");
            CheckSanctionConfirmationLayout();

            var bidForm = LoadTree("Assets/UI/Layouts/Marketplace_Bidder_Form.uxml");
            CheckButton(bidForm, "Btn_SubmitBid"); CheckType<SliderInt>(bidForm, "Slider_CashOffer");

            CheckReducedMotionKeepsStaticTileHighlight();

            Debug.Log($"UI ASSET REGRESSION PASS: {assertions} assertions across 17 UXML screens/components.");
        }

        private static void CheckPulseCurrencyFormatting()
        {
            string groupSpace = "\u202f";
            Check(CurrencyFormatter.FormatExact(18000) == "18" + groupSpace + "000 $",
                "Pulse HUD keeps full grouped balances visible");
            Check(CurrencyFormatter.FormatExact(-480) == "−480 $",
                "Pulse financial rows preserve negative signs with full amounts");
            Check(CurrencyFormatter.FormatExactNumber(1300) == "1" + groupSpace + "300",
                "Pulse rent values support a separate currency suffix");
        }

        private static void CheckActivityFeedLanguageConsistency()
        {
            var formatter = typeof(TopHUDWidget).GetMethod("FormatActivityText",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Check(formatter != null, "activity feed formatter is available");
            if (formatter == null) return;

            string[,] cases =
            {
                { "commence son tour", "starts their turn" },
                { "arrive sur Opioid Paradise", "lands on Opioid Paradise" },
                { "passe l’appel d’offres", "passes on the tender" },
                { "passe sur Opioid Paradise", "passes on Opioid Paradise" },
                { "passe", "passes" },
                { "lance 2 + 5 · total 7", "rolls 2 + 5 · total 7" },
                { "lance une OPA sur Example Holdings pour $2,000", "launches a hostile takeover of Example Holdings for $2,000" },
                { "achète Example Holdings · 2 000 $", "buys Example Holdings · 2 000 $" },
                { "paie $110 de loyer · Opioid Paradise", "pays $110 in rent · Opioid Paradise" },
                { "règle $110 de loyer · Opioid Paradise", "pays $110 in rent · Opioid Paradise" },
                { "règle sa dette de 2 000 $", "settles a debt of 2 000 $" },
                { "investit $1,000 pour 2 tours", "invests $1,000 for 2 turns" },
                { "perd son investissement de $1,000", "loses their investment of $1,000" },
                { "récupère $1,100 en Bourse", "receives $1,100 from the stock market" },
                { "est en faillite", "is bankrupt" },
                { "vend les améliorations de Example Holdings", "sells upgrades on Example Holdings" },
                { "hypothèque Example Holdings", "mortgages Example Holdings" },
                { "vend Example Holdings à la banque", "sells Example Holdings to the bank" },
                { "quitte la Marketplace", "leaves the Marketplace" },
                { "annule l’appel d’offres", "cancels the tender" },
                { "met Example Holdings en appel d’offres", "lists Example Holdings for tender" },
                { "soumet une offre", "places a bid" },
                { "remporte Example Holdings", "wins Example Holdings" },
                { "active Cyber-sabotage", "activates Cyber-sabotage" },
                { "complète la famille Finance · pouvoir débloqué", "completes the Finance family · power unlocked" },
                { "cycle CRASH actif pour 3 tours", "market cycle CRASH active for 3 turns" },
                { "retour à un marché normal", "returns to a normal market" },
                { "gagne $500 au Casino", "wins $500 at the Casino" },
                { "perd $500 au Casino", "loses $500 at the Casino" },
                { "tente de sortir de l’Audit fiscal", "tries to leave Tax Audit" },
                { "peut activer Transit Express", "can activate Express Transit" },
                { "utilise Transit Express · case 4 → 8", "uses Express Transit · tile 4 → 8" },
                { "reste sur son Hub", "stays at their Hub" },
                { "reste en Audit fiscal", "stays in Tax Audit" },
                { "utilise sa carte de sortie d’Audit", "uses a Tax Audit exit card" },
                { "paie 500 $ et quitte l’Audit fiscal", "pays 500 $ and leaves Tax Audit" },
                { "pioche Venture #19", "draws Venture #19" },
                { "améliore Example Holdings · niveau 2", "upgrades Example Holdings · level 2" },
                { "place un embargo sur Example Holdings", "places an embargo on Example Holdings" },
                { "déclenche un audit ciblé contre Joueur 2", "launches a targeted audit against Player 2" },
                { "déclenche une grève chez Joueur 2", "starts a strike at Player 2" },
                { "sabote Example Holdings", "sabotages Example Holdings" }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                string input = cases[i, 0];
                string expected = cases[i, 1];
                var result = (string)formatter.Invoke(null, new object[] { input });
                Check(result == expected, $"activity feed translation: {input}");
            }
        }

        private static void CheckPlayerRosterShortNames()
        {
            var formatter = typeof(TopHUDWidget).GetMethod("ShortPlayerName",
                BindingFlags.Static | BindingFlags.NonPublic);
            Check(formatter != null, "player roster short-name formatter is available");
            if (formatter == null) return;
            Check((string)formatter.Invoke(null, new object[] { "Player 1" }) == "P1",
                "fallback player names remain distinct in a compact roster");
            Check((string)formatter.Invoke(null, new object[] { "Alexandra" }) == "ALEXAN…",
                "long lobby names are compacted for the roster");
        }

        private static void CheckTypedGameEventFormatting()
        {
            string PlayerName(int id) => id == 1 ? "Alex" : id == 2 ? "Jules" : "P" + (id + 1);
            string TileName(int id) => id == 4 ? "Atlas" : "Tile";
            var purchase = GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                GameEventKind.PropertyPurchased, GameEventPriority.Major, 0,
                tileId: 4, amount: -500, reason: GameEventReason.PropertyPurchase);
            Check(GameEventTextFormatter.Format(purchase, 0, PlayerName, TileName)
                    == "YOU acquired Atlas · −$500",
                "typed purchase feedback shows the confirmed payer, asset, and amount");

            var rent = GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                GameEventKind.TransferConfirmed, GameEventPriority.Normal, 0,
                targetId: -1, tileId: 4, amount: -100, reason: GameEventReason.Rent,
                splits: new[] { new GameEventSplit(1, 70), new GameEventSplit(2, 30) });
            Check(GameEventTextFormatter.Format(rent, 0, PlayerName, TileName)
                    == "YOU −$100 → Alex +$70 · Jules +$30 · RENT · Atlas",
                "typed rent feedback shows every confirmed payee and receipt");

            var passed = GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                GameEventKind.PropertyPassed, GameEventPriority.Normal, 1, tileId: 4);
            Check(GameEventTextFormatter.Format(passed, 0, PlayerName, TileName)
                    == "Alex passed on Atlas",
                "typed pass feedback identifies the player and property");

            var tax = GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                GameEventKind.TaxPaid, GameEventPriority.Major, 0,
                amount: -200, reason: GameEventReason.Tax);
            Check(GameEventTextFormatter.Format(tax, 0, PlayerName, TileName)
                    == "YOU paid −$200 in tax",
                "typed tax feedback shows the confirmed amount from the payer's perspective");

            var upgrade = GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                GameEventKind.UpgradeApplied, GameEventPriority.Major, 1,
                tileId: 4, amount: -500, reason: GameEventReason.Upgrade, valueA: 0, valueB: 1);
            Check(GameEventTextFormatter.Format(upgrade, 0, PlayerName, TileName)
                    == "Alex upgraded Atlas · L0→L1 · −$500",
                "typed upgrade feedback reports the confirmed level change and cost");
        }

        private static void CheckReducedMotionKeepsStaticTileHighlight()
        {
            const string preferenceKey = "PolyRisk.ReduceMotion";
            bool hadPreference = PlayerPrefs.HasKey(preferenceKey);
            int previousPreference = hadPreference ? PlayerPrefs.GetInt(preferenceKey) : 0;
            GameObject tileObject = null;
            Material testMaterialInstance = null;

            try
            {
                PlayerPrefs.SetInt(preferenceKey, 1);
                tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var visuals = tileObject.AddComponent<TileVisuals>();
                var renderer = tileObject.GetComponent<MeshRenderer>();
                typeof(TileVisuals).GetField("tileRenderer",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(visuals, renderer);
                Color selectedColor = new Color(0.12f, 0.82f, 0.46f, 1f);

                visuals.BeginPulse(selectedColor, 100f);
                testMaterialInstance = renderer.material;

                Check(ColorApproximately(testMaterialInstance.color, selectedColor),
                    "reduced motion keeps the selected tile highlight visible");
                var pulseField = typeof(TileVisuals).GetField("pulseCoroutine",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Check(pulseField != null && pulseField.GetValue(visuals) == null,
                    "reduced motion does not start a tile pulse coroutine");
            }
            finally
            {
                if (tileObject != null) UnityEngine.Object.DestroyImmediate(tileObject);
                if (testMaterialInstance != null) UnityEngine.Object.DestroyImmediate(testMaterialInstance);
                if (hadPreference) PlayerPrefs.SetInt(preferenceKey, previousPreference);
                else PlayerPrefs.DeleteKey(preferenceKey);
                PlayerPrefs.Save();
            }
        }

        private static void CheckSanctionConfirmationLayout()
        {
            const string layoutPath = "Assets/UI/Layouts/Marketplace_Select_Player.uxml";
            var layout = XDocument.Load(layoutPath);
            string InlineStyle(string elementName)
            {
                foreach (var element in layout.Descendants())
                    if ((string)element.Attribute("name") == elementName)
                        return (string)element.Attribute("style") ?? string.Empty;
                return string.Empty;
            }

            string grid = InlineStyle("Container_SelectGrid");
            Check(HasDeclarations(grid, ("max-height", "220px"), ("min-height", "0"),
                    ("flex-grow", "1"), ("flex-shrink", "1")),
                "sanction target list can contract and scroll within its height cap");
            string confirmBar = InlineStyle("Container_SelectConfirmBar");
            Check(HasDeclarations(confirmBar, ("display", "none"), ("flex-direction", "column"),
                    ("min-height", "92px"), ("flex-shrink", "0")),
                "sanction confirmation reserves a vertical non-shrinking action area");
            string confirmButton = InlineStyle("Btn_SelectConfirm");
            Check(HasDeclarations(confirmButton, ("width", "100%"), ("min-height", "48px"),
                    ("flex-shrink", "0")),
                "sanction confirmation button stays full-width and at least 48 px tall");

            string stylesheet = File.ReadAllText("Assets/UI/Styles/Theme.uss");
            Check(HasRuleDeclarations(stylesheet,
                    "#Panel_Marketplace_SelectPlayer .marketplace-panel--compact",
                    ("height", "96%"), ("max-height", "96%"), ("min-height", "0"), ("flex-shrink", "1")),
                "sanction selector panel can contract inside the viewport");
            Check(HasRuleDeclarations(stylesheet,
                    "#Panel_Marketplace_SelectPlayer .panel-content",
                    ("flex-grow", "1"), ("flex-shrink", "1"), ("min-height", "0")),
                "sanction panel content yields space to the confirmation action");
            Check(HasRuleDeclarations(stylesheet,
                    "#Panel_Marketplace_SelectPlayer #Container_SelectGrid",
                    ("flex-grow", "1"), ("flex-shrink", "1"), ("min-height", "0")),
                "sanction target list yields space while remaining scrollable");
            Check(HasRuleDeclarations(stylesheet,
                    "#Panel_Marketplace_SelectPlayer #Container_SelectConfirmBar",
                    ("flex-shrink", "0")),
                "sanction confirmation cannot shrink out of view");
        }

        private static bool HasDeclarations(string css, params (string property, string value)[] expected)
        {
            foreach (var pair in expected)
            {
                bool found = false;
                foreach (var declaration in css.Split(';'))
                {
                    var parts = declaration.Split(new[] { ':' }, 2);
                    if (parts.Length == 2
                        && string.Equals(parts[0].Trim(), pair.property, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(parts[1].Trim(), pair.value, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }

        private static bool HasRuleDeclarations(string stylesheet, string selector,
            params (string property, string value)[] expected)
        {
            var rules = Regex.Matches(stylesheet,
                $@"(?s){Regex.Escape(selector)}\s*\{{(?<body>[^}}]*)\}}");
            foreach (Match rule in rules)
                if (HasDeclarations(rule.Groups["body"].Value, expected)) return true;
            return false;
        }

        private static bool ColorApproximately(Color actual, Color expected) =>
            Mathf.Abs(actual.r - expected.r) < 0.001f
            && Mathf.Abs(actual.g - expected.g) < 0.001f
            && Mathf.Abs(actual.b - expected.b) < 0.001f
            && Mathf.Abs(actual.a - expected.a) < 0.001f;

        private static void CheckAsset(string path, params string[] requiredNames)
        {
            var root = LoadTree(path);
            foreach (var name in requiredNames)
                Check(root.Q<VisualElement>(name) != null, $"{path} contains '{name}'");
        }

        private static VisualElement LoadTree(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Check(asset != null, $"UXML asset loads: {path}");
            var root = new VisualElement();
            asset.CloneTree(root);
            Check(root.childCount > 0, $"UXML clones a non-empty tree: {path}");
            return root;
        }

        private static void CheckButton(VisualElement root, string name) => CheckType<Button>(root, name);

        private static void CheckType<T>(VisualElement root, string name) where T : VisualElement =>
            Check(root.Q<T>(name) != null, $"'{name}' is a {typeof(T).Name}");

        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"UI asset regression failed: {message}");
        }
    }
}
