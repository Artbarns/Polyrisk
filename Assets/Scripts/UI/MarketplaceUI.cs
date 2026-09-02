using UnityEngine;
using TMPro;
using System.Collections.Generic;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.UI
{
    public class MarketplaceUI : MonoBehaviour
    {
        public GameObject marketplacePanel;
        public TMP_Dropdown playerDropdown;
        public TMP_Dropdown propertyDropdown;

        public MarketplaceSystem marketplaceSystem;
        public TurnManager turnManager;

        private List<PlayerModel> activeOpponents = new List<PlayerModel>();
        private List<PropertyData> opponentProperties = new List<PropertyData>();

        private void OnEnable()
        {
            if (playerDropdown != null)
            {
                playerDropdown.onValueChanged.AddListener(OnPlayerSelected);
            }
        }

        private void OnDisable()
        {
            if (playerDropdown != null)
            {
                playerDropdown.onValueChanged.RemoveListener(OnPlayerSelected);
            }
        }

        public void OpenMarketUI()
        {
            if (marketplacePanel == null || turnManager == null || playerDropdown == null || propertyDropdown == null) return;

            marketplacePanel.SetActive(true);

            PlayerModel currentPlayer = turnManager.CurrentPlayer;
            activeOpponents.Clear();
            playerDropdown.ClearOptions();

            List<string> opponentNames = new List<string>();

            // Populate playerDropdown with opponents only
            foreach (var player in turnManager.Players)
            {
                if (player.PlayerId != currentPlayer.PlayerId && !player.IsBankrupt)
                {
                    activeOpponents.Add(player);
                    opponentNames.Add($"Player {player.PlayerId}");
                }
            }

            if (activeOpponents.Count > 0)
            {
                playerDropdown.AddOptions(opponentNames);
                playerDropdown.value = 0;
                OnPlayerSelected(0);
            }
            else
            {
                playerDropdown.AddOptions(new List<string> { "No Opponents" });
                propertyDropdown.ClearOptions();
                propertyDropdown.AddOptions(new List<string> { "No Properties" });
            }
        }

        private void OnPlayerSelected(int index)
        {
            if (index < 0 || index >= activeOpponents.Count) return;

            PlayerModel selectedOpponent = activeOpponents[index];
            RefreshPropertyDropdown(selectedOpponent);
        }

        private void RefreshPropertyDropdown(PlayerModel opponent)
        {
            propertyDropdown.ClearOptions();
            opponentProperties.Clear();

            if (opponent.OwnedProperties.Count == 0)
            {
                propertyDropdown.AddOptions(new List<string> { "No Properties" });
                return;
            }

            List<string> propertyNames = new List<string>();
            foreach (var prop in opponent.OwnedProperties)
            {
                opponentProperties.Add(prop);
                propertyNames.Add($"{prop.PropertyName} (Worth: {prop.BasePrice})");
            }

            propertyDropdown.AddOptions(propertyNames);
        }

        public void ConfirmTrade()
        {
            if (marketplaceSystem == null || turnManager == null)
            {
                CloseMarketUI();
                return;
            }

            int playerIndex = playerDropdown.value;
            int propIndex = propertyDropdown.value;

            if (playerIndex >= 0 && playerIndex < activeOpponents.Count && propIndex >= 0 && propIndex < opponentProperties.Count)
            {
                PlayerModel selectedOpponent = activeOpponents[playerIndex];
                PropertyData selectedProperty = opponentProperties[propIndex];
                PlayerModel currentPlayer = turnManager.CurrentPlayer;

                // Execute trade directly as requested by the architectural instructions
                marketplaceSystem.ExecuteTrade(currentPlayer, selectedOpponent, selectedProperty, 2000);
            }

            CloseMarketUI();
        }

        public void CloseMarketUI()
        {
            if (marketplacePanel != null)
            {
                marketplacePanel.SetActive(false);
            }

            if (turnManager != null)
            {
                // Return control back to TurnManager to progress the game loop properly
                turnManager.CompleteTileResolution();
            }
        }
    }
}
