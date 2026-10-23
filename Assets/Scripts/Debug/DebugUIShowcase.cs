using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.UI;

namespace PolyRisk.DebugTools
{
    /// <summary>
    /// Development-only shortcuts for visual QA of screens that normally require a
    /// long game state. No object or input listener is created in release builds.
    /// </summary>
    public sealed class DebugUIShowcase : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindObjectOfType<DebugUIShowcase>() != null) return;
            var go = new GameObject("[Debug] UI Showcase");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugUIShowcase>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) ShowVenture();
            if (Input.GetKeyDown(KeyCode.F6)) ShowBankruptcy();
            if (Input.GetKeyDown(KeyCode.F7)) ShowVictory();
            if (Input.GetKeyDown(KeyCode.F8)) ShowStockMarket();
        }

        private static void ShowVenture()
        {
            var controller = VentureCardModalController.Instance ?? FindObjectOfType<VentureCardModalController>();
            controller?.ShowCard(20, () => Debug.Log("[UI Showcase] Venture confirmed."));
        }

        private static void ShowBankruptcy()
        {
            var turnManager = TurnManager.Instance;
            var player = turnManager?.CurrentPlayer;
            var controller = FindObjectOfType<BankruptcyScreenController>();
            if (player == null || controller == null) return;
            controller.Show(player, player.CurrentBalance + 10000d, null,
                paid => Debug.Log($"[UI Showcase] Bankruptcy resolved, paid={paid}."));
        }

        private static void ShowVictory()
        {
            var player = TurnManager.Instance?.CurrentPlayer;
            if (player != null) GameEvents.OnGameVictory?.Invoke(player, "HYPER-MOGUL $10M");
        }

        private static void ShowStockMarket()
        {
            var turns = TurnManager.Instance;
            if (turns == null || turns.Players == null || turns.Players.Count == 0) return;
            turns.DEBUG_ForcePlayerOnTile(0, 12);
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;
            GUI.color = new Color(0.35f, 0.9f, 1f, 0.9f);
            GUI.Label(new Rect(10, 58, 620, 24),
                "[UI QA] F5 Venture  |  F6 Bankruptcy  |  F7 Victory  |  F8 Stock Market");
            GUI.color = Color.white;
        }
#endif
    }
}
