# Current Task

Stress test IA x1000 parties. Plan complet :
[wondrous-discovering-teacup.md](/Users/arthurbarreau/.claude/plans/wondrous-discovering-teacup.md)

## Plan
- [x] `Assets/Scripts/Debug/StressTestRunner.cs` — driver complet (batch coroutine,
      séquence de reset, capture par partie, CSV + résumé)
- [x] `Assets/Scripts/Systems/GameBootstrapper.cs` — `ForceAllAI` (2 lignes, additif,
      comportement par défaut inchangé)

## Review

**2 bugs pré-existants trouvés et vérifiés en lisant le code (pas des suppositions)** :
1. `GameBootstrapper.cs:99` — le joueur 0 était toujours humain, bloquant toute partie
   100% IA. Corrigé via `ForceAllAI` (flag statique, off par défaut).
2. `FinanceSystem.cs:261` (victoire à 10M$) ne positionne jamais `isGameOver` dans
   `TurnManager` — la coroutine de tour continuerait indéfiniment après ce type de
   victoire précis. Géré côté harness : `StopAllCoroutines()` appelé dans TOUS les cas
   de reset, pas seulement en cas d'anomalie.
3. Bug latent supplémentaire trouvé (pas corrigé, hors scope — juste détecté) : si les
   4 joueurs font faillite au même tour, aucun gagnant n'est jamais désigné et le jeu
   boucle silencieusement. Le harness s'abonne à `OnPlayerBankruptcy` et détecte ce cas
   immédiatement (`ANOMALY_ALL_BANKRUPT`) plutôt que d'attendre le plafond de 90s.

**Ce qui a changé** :
- Nouveau : `StressTestRunner.cs` — `MonoBehaviour` de debug (même patron que
  `DebugMarketplaceSetup.cs`), touche F9. Accélère via `Time.timeScale` (aucun bypass de
  la logique de jeu). Reset entre parties : `StopAllCoroutines` + destruction des 6
  singletons `DontDestroyOnLoad` + `SceneManager.LoadScene`. Garde-fou 90s réelles par
  partie (`Time.realtimeSinceStartup`, indépendant de `timeScale`). Résultats écrits
  dans `StressTestResults/` (hors `Assets/`) : un CSV par partie + un résumé texte.
- Modifié : `GameBootstrapper.cs` — `ForceAllAI` statique, 2 lignes.

**Pas vérifié** : aucun accès Unity dans cette session — ni compilation, ni exécution.
Étapes manuelles pour Arthur (détaillées dans le plan) :
1. Ajouter le script sur un GameObject vide dans la scène.
2. **Smoke test d'abord** : 5 parties, `timeScale=10`. Entrer en Play, appuyer F9.
3. Vérifier le CSV (5 lignes, balances plausibles, zéro anomalie) avant de monter à 1000.
4. Lire le résumé pour la vraie question du backlog : l'économie reste-t-elle stable ?

## Itération 2 — bugs trouvés en testant réellement

Retours d'Arthur (premier vrai test en jeu) :
- [x] **Bug dans `StressTestRunner.cs`** : avec `numberOfGames=1`, Arthur devait quand
  même cliquer Buy/Roll Dice lui-même. Cause : `GameBootstrapper.Start()` s'exécute
  automatiquement à l'entrée en Play Mode, AVANT que F9 ne soit pressé — la partie déjà
  en cours a donc été bootstrappée avec `ForceAllAI` encore à `false`. Fix : reset
  initial (`DoReset()`) avant la boucle de `RunBatch()`, pour que même la partie 0
  tourne avec `ForceAllAI=true`.
- [x] **Bug Marketplace confirmé et corrigé** (`Marketplace_UIManager.cs`,
  `HandleAuctionStateChanged`, cas `Closing`) : ne fermait le panneau que si l'IA était
  vendeuse — supposait à tort que `CloseMarketplace()` était toujours appelé ailleurs
  pour un vendeur humain (vrai seulement pour le cas "deal scellé", pas pour "tender
  annulé, aucune enchère"). Le bottom sheet restait bloqué indéfiniment après un Reject
  All sans offres. Fix : appel inconditionnel, sans risque (double appel déjà idempotent
  ailleurs dans le code).
- [ ] **Bug IA qui ne misent jamais dans le Marketplace** — pas encore diagnostiqué,
  besoin d'un retour d'Arthur sur les logs Console (`[AI Bid] ... skip` présents ou pas)
  pour savoir si c'est un vrai bug ou juste un comportement probabiliste normal.
- [x] **Bug racine du blocage systématique trouvé grâce au stress test** — 7/7 parties
  timeout à 90s, `EndedOnLap` quasi nul à chaque fois. Logs Console (3 runs identiques)
  montrent le blocage exactement après "Paid fine immediately after landing. Turn Ends."
  Cause (`TurnManager.cs:441-445`, `AuditPayFine()`) : appelle `CompleteTileResolution()`
  qui exige `currentState == ResolveTile`, mais l'état réel à ce moment est
  `WaitingForAuditDecision` — l'appel était un no-op silencieux, la partie restait
  bloquée indéfiniment dans cet état. La branche voisine ("IA ne peut pas payer") avait
  déjà le bon correctif (`currentState = TurnState.EndTurn;` direct) — jamais appliqué
  à la branche "paiement réussi", qui est celle qui plantait à chaque fois. Fix : même
  correctif appliqué. Vérifié que l'autre branche audit (`ExecuteMovementAfterAuditEscape`)
  n'a pas le même problème — elle passe par le flow normal `MovingPawn → ResolveTile`.

**Pas vérifié** : toujours aucun accès Unity — le fix est validé par traçage précis de la
séquence d'états (pas une supposition, le bug est 100% reproductible dans les logs
fournis), mais pas par un test réel après correction.

## Itération 3 — plus de blocage, juste un mauvais calibrage

Après le fix audit, tous les batches (10 parties sur 2 runs) montrent encore 100%
`ANOMALY_TIMEOUT` à 90s. Mais Arthur confirme qu'une partie laissée tourner va bien
jusqu'à la victoire du dernier joueur, sans blocage — juste lentement. Conclusion :
plus de vrai deadlock, seulement un plafond de 90s trop juste pour la durée réelle
d'une partie complète à `timeScale=10`.

- [x] `StressTestRunner.cs` — defaults remontés : `timeScale` 10→30, `perGameTimeoutRealSeconds`
      90→240. Pas encore reconfirmé par un run réel après ce changement.

**Pas vérifié** : aucun accès Unity — à confirmer par Arthur que ces nouvelles valeurs
donnent enfin des parties `Normal` plutôt que 100% anomalies.

## Itération 4 — fix du Min() pour le timeout tour 50

- [x] `TurnManager.cs:921` — `players.Min(p => p.LapsCompleted)` incluait les joueurs en
      faillite (compteur figé dès l'élimination) → le timeout tour 50 pouvait ne jamais
      se déclencher. Exclu les faillis du calcul (`Where(!IsBankrupt)` avant le `Min`),
      garde-fou si la liste filtrée est vide.
- Clarifié à Arthur (pas un bug) : le TopHUD affiche `Player0.LapsCompleted + 1`
  spécifiquement, pas un compteur global — reste visuellement figé si Player 0 est
  éliminé, même partie/fix qui fonctionnent bien en interne. Pas changé sauf si demandé.
- Clarifié à Arthur (pas un bug) : le harness enchaîne automatiquement les parties du
  batch (`numberOfGames`) — "ça recommence de zéro" tout seul est normal si
  `numberOfGames > 1`.

**Pas vérifié** : aucun accès Unity — fix du Min() basé sur lecture de code, pas testé.
