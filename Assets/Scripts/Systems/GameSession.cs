using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PolyRisk.Core;
using PolyRisk.UI;

namespace PolyRisk.Systems
{
    public class GameSession : MonoBehaviour
    {
        private static bool restarting;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { restarting = false; }
        public static void Restart()
        {
            if (restarting) return;
            restarting = true;
            var host = new GameObject("Session reset").AddComponent<GameSession>();
            DontDestroyOnLoad(host.gameObject);
            host.StartCoroutine(host.RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            yield return Reload();
            restarting = false;
            Destroy(gameObject);
        }

        public static IEnumerator Reload()
        {
            string scenePath = SceneManager.GetActiveScene().path;
            TurnManager.Instance?.StopAllCoroutines();
            var objects = new HashSet<GameObject>();
            void Collect(Component c) { if (c != null) objects.Add(c.gameObject); }
            Collect(TurnManager.Instance);
            Collect(BoardManager.Instance);
            Collect(TransactionManager.Instance);
            Collect(AbilitySystem.Instance);
            Collect(AuctionManager.Instance);
            Collect(CycleManager.Instance);
            foreach (var c in FindObjectsOfType<VictoryScreenController>()) Collect(c);
            foreach (var c in FindObjectsOfType<BankruptcyScreenController>()) Collect(c);
            foreach (var c in FindObjectsOfType<VentureCardModalController>()) Collect(c);
            foreach (var go in objects) Destroy(go);
            yield return null;
            // OnDisable removes the destroyed objects' subscriptions. Never erase live listeners.
#if UNITY_EDITOR
            // Also works before the scene has been added to Build Settings.
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(scenePath);
#endif
        }
    }
}
