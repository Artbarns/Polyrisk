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

## Itération 5 — perf réglée (reflection probe + stack trace), mais nouveau deadlock trouvé à l'échelle

Run de 5 parties (`timeScale=100`) : 0 anomalie, 8-50s réelles/partie. Les deux fixes de
perf de l'itération précédente tiennent la route. Mais un run suivant de 20 parties (même
config) est retombé à **17/20 `ANOMALY_TIMEOUT` (85%)**, toutes bloquées très tôt
(`EndedOnLap` 0-7), sans erreur console, sans faillite en cascade. Diagnostiqué à partir
d'un export console complet (70k lignes) fourni par Arthur — pas une supposition :

- [x] **Race condition trouvée et corrigée** (`TurnManager.cs`, `ExecuteMovement()`) :
  `GameEvents.OnPlayerPathCalculated?.Invoke(...)` était appelé **avant** la souscription
  à `OnPawnMovementFinished`. Si `BoardManager.GetPath()` retourne un chemin vide (arrive
  dès qu'une entrée `visualTiles[i]` est `null` sur le trajet — cf. `BoardManager.cs:131-140`),
  `PawnMovement.MoveAlongPath` termine de façon **synchrone** et déclenche
  `OnPawnMovementFinished` immédiatement — avant que `TurnManager` ne soit abonné. Le
  `yield return new WaitUntil(() => movementFinished)` attendait alors un event qui ne se
  reproduirait jamais : blocage permanent, sans aucune erreur. Confirmé par les logs :
  toutes les parties bloquées s'arrêtent net juste après un jet de dé / au tout début de
  `ExecuteMovement`, jamais ailleurs.
  Fix : souscription déplacée avant l'invocation (élimine la race), + garde-fou 10s
  réelles (`Time.realtimeSinceStartup`, indépendant du `timeScale`) en filet de sécurité,
  même pattern que le safety-net déjà existant sur la phase Planning humaine (qui, lui,
  anticipait déjà ce genre de blocage — juste jamais appliqué à `ExecuteMovement`).

**Pas vérifié** : aucun accès Unity dans cette session — fix basé sur traçage précis du
code (source du bug identifiée avec certitude), pas retesté après correction. Prochaine
étape pour Arthur : relancer un batch de 20+ parties et confirmer que le taux d'anomalie
retombe à 0 (ou au pire au bug latent connu `ANOMALY_ALL_BANKRUPT`, toujours pas corrigé
par choix — juste détecté par le harness).

## Itération 6 — vrai goulot trouvé : ce n'était pas un deadlock de code

Le fix de l'itération 5 n'a quasi rien changé (70-85% `ANOMALY_TIMEOUT` persistant sur
des runs de 20-30 parties). Diagnostiqué avec des logs `[DIAG]` temporaires (retirés
depuis) tracés dans `ExecuteMovement` + un heartbeat mesurant `Time.frameCount` :

- [x] **Vrai diagnostic** : l'Editor Unity ne tournait qu'à **1-4 frames par ~2s réelles**
  pendant tout le batch — pas un deadlock logique, un vrai ralentissement moteur.
  Cause la plus probable : la fenêtre Console qui dégrade très mal au-delà de dizaines de
  milliers de lignes (70k-120k lignes mesurées par run, avec 150+ points de log/tour).
- [x] **Fuite trouvée en même temps** (`GameEvents.OnPawnMovementFinished` : 1→2→...→6
  abonnés sur un seul batch) : `Destroy()` est différé en fin de frame, et un seul
  `yield return null` dans `DoReset()` ne suffit pas toujours à laisser `OnDisable()`
  désabonner avant le `SceneManager.LoadScene()` suivant, surtout sous le ralentissement
  ci-dessus. Fix : tous les `GameEvents` pertinents nettoyés explicitement dans `DoReset()`
  (sauf `OnGameVictory`/`OnPlayerBankruptcy`, dont `RunBatch` reste abonné tout le batch).
- [x] **Fix perf #1** : `Debug.unityLogger.filterLogType = LogType.Warning` pendant toute
  la boucle du batch (restauré après) — supprime le bavardage de routine sans toucher aux
  150+ call sites. Errors/Exceptions passent toujours (`ErrorCount` intact).
- [x] **Fix perf #2, le vrai morceau manquant** : Arthur a remarqué "les pions avancent
  d'une case en une case" — l'animation de hop (`PawnMovement.MoveRoutine`) impose au
  moins 1 frame réelle par case traversée, indépendamment de `Time.timeScale` (qui
  n'accélère que le temps de jeu, pas le nombre de frames réelles). `GameBootstrapper.SkipMovementAnimation`
  ajouté : téléportation directe pendant le stress test. Fonctionne correctement
  seulement grâce au fix subscribe-avant-invoke de l'itération 5 (la téléportation
  déclenche `OnPawnMovementFinished` de façon synchrone à chaque coup, sur tous les coups
  désormais, pas juste les cas limites).
- [x] **Faux problème résolu** : après ces fixes, Console totalement vide même en pressant
  F9 (mais jeu visiblement rapide). Cause : `Debug.unityLogger.filterLogType` restait
  "collé" en `Warning` d'une session Play interrompue à l'autre (`Reload Domain`
  désactivé). Un forçage `filterLogType = LogType.Log` + stack traces `ScriptOnly` en
  tout début de `RunBatch()` corrige ça de façon auto-réparante, quel que soit l'état
  laissé par un run précédent. Confirmé par un `Debug.LogError` inconditionnel par frame
  (retiré une fois confirmé) — les logs sont revenus dès la recompilation suivante.
- [x] **Bug trouvé au passage, corrigé** (`RulesEngine.cs`, `ExecutePrimaryAction`) : le
  cas `owner == null` + `TryBuyProperty` en échec (cash insuffisant) n'avait pas de
  `else` — tombait jusqu'au garde-fou générique en bas de la méthode, loggant
  `"Unhandled tile type Property"` (message trompeur, laisse croire à un case non géré).
  Comportement inchangé (la case reste juste sautée), mais le log dit maintenant
  vraiment ce qui se passe — utile pour la question de fond sur l'équilibrage (fréquence
  de ce skip = signal direct de joueurs à court de cash).

**Résultat** : le batch x100 tourne maintenant à vitesse normale avec logs propres.
Prochaine étape pour Arthur : laisser tourner le vrai run x100 (ou plus) et analyser les
résultats CSV — c'est la première fois que la question de fond du backlog (l'économie
reste-t-elle stable au tour 50 ?) peut vraiment être testée à l'échelle.

**Pas vérifié** : aucun accès Unity dans cette session — tous les fixes validés par
traçage précis de code + logs fournis par Arthur, pas par exécution directe de ma part.

## Itération 7 — le vrai run x100 (10h30, 99% timeout) révèle le bug de fond

Le run x100 réel a tourné 10h30 (17h24 → 03h48) : **99% ANOMALY_TIMEOUT**, 1 seule victoire
propre ($10M en 214s). Mais le CSV révèle deux populations très différentes :
- 6 parties ont réellement progressé (29-45 tours, Sgo $75K-$166K, soldes en millions) —
  preuve que les fixes de perf de l'itération 6 fonctionnent vraiment une fois lancés.
- 69/100 finissent à `EndedOnLap=0` avec Sgo quasi jamais monté, et plusieurs parties ont
  dépassé le plafond de 240s réelles jusqu'à **1845s (30 min)** — signe d'un vrai blocage,
  pas juste un manque de vitesse.

Le premier export console (10h30 de session) ne montrait presque rien d'utile — hypothèse :
buffer Console limité en lignes, ne gardait que la toute fin de la session. Un second run
ciblé de 10 parties (~qq dizaines de minutes, export console immédiat) a confirmé cette
hypothèse ET changé le diagnostic : sur ce run, **aucune** partie n'est restée bloquée à
`EndedOnLap` bas — les 10 ont progressé normalement (14-33 tours). Donc le blocage précoce
observé sur le run de 10h30 n'est probablement pas systématique, mais quelque chose s'est
quand même révélé dans les logs de ce run de 10 : **le vrai bug de fond**.

- [x] **Bug trouvé et corrigé** (`TransactionManager.cs`, `ProcessBankruptcyCash`) : le
  message `"Player X cannot afford $130000 rent. Triggering Bankruptcy flow."` se répétait
  **24 à 39 fois à l'identique** pour un même joueur dans une seule partie (confirmé par
  grep sur l'export console). Cause : `ProcessBankruptcyCash` — le filet de secours appelé
  par `RulesEngine` quand une IA insolvable ne peut toujours pas payer après le flow normal
  de liquidation (`GameEvents.OnInsolvency` → `BankruptcyScreenController.ProcessAILiquidation`)
  — transférait l'argent restant mais **ne posait jamais `IsBankrupt = true`**, contrairement
  à `ProcessAILiquidation` qui le fait correctement. Si le flow de liquidation normal ne se
  déclenche pas à temps pour une raison quelconque (même catégorie de risque que la fuite
  d'abonnement déjà trouvée sur `OnPawnMovementFinished` à l'itération 5 — souscription à
  `OnInsolvency` pas encore réarmée juste après un rechargement de scène), le joueur restait
  bloqué à $0 sans jamais être formellement en faillite : il continuait à jouer
  indéfiniment, retombant sur le même loyer impayable à chaque tour.
  **Impact direct sur le taux d'anomalie** : `TurnManager.cs:917-920` (condition de victoire
  "Last Player Standing") compte les joueurs non-bankrupt et déclare un gagnant dès qu'il
  n'en reste qu'un — ce bug bloquait donc directement la condition de victoire naturelle du
  jeu, forçant systématiquement le passage par le timeout de 240s peu importe la qualité du
  reste. Fix : `ProcessBankruptcyCash` pose maintenant `IsBankrupt` et déclenche
  `OnPlayerBankruptcy` lui-même, de façon autonome, peu importe ce qui s'est passé avant.

**Pas vérifié** : aucun accès Unity dans cette session — fix basé sur traçage précis de
code + comptage exact des répétitions dans les logs fournis par Arthur, pas retesté après
correction. Prochaine étape pour Arthur : relancer un batch (10-30 parties suffisent pour
valider avant de remonter à 100+) et vérifier que le taux de `LAST PLAYER STANDING` /
`REACHED $10M` remonte franchement au-dessus de 1%.
