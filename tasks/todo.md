# Objectif global d’achèvement PolyRisk

> Achever PolyRisk en construisant le multijoueur en ligne jusqu’à six joueurs, puis terminer la recette mobile/WebGL, les tests humains, l’équilibrage et les finitions de publication.

Cette phrase est l’objectif de référence du projet. Le suivi détaillé ci-dessous documente le travail, mais ne remplace pas les jalons de clôture. L’objectif Codex associé est actif ; son état et cette todo restent deux suivis distincts.

- [x] Base du multijoueur en ligne jusqu’à six sièges ; smoke test public six joueurs et démarrage protocole PASS (`Docs/Multiplayer_foundation_2026-10-04.md`, `Docs/Validation/Reconnect_2026-10-12/`, rapport préversion ci-dessous).
- [x] Serveur lobby actuel : test de redémarrage isolé PASS, identifiants hachés, file de commandes et état de partie restaurés (`Server/PolyRisk.LobbyServer/persistence_test.py`).
- [ ] Partie complète avec de vrais amis : recette humaine, incidents observés, liste des correctifs et validation après correctifs.
- [x] Relancer la préversion publique V44 ; le tunnel HTTPS et les services de recette répondent à nouveau. Le lien est temporaire et dépend du Mac hôte.
- [ ] Recette finale sur appareils mobiles et WebGL, notamment iPhone 14 Pro Max, performances, netteté, orientation et reprise de partie.
- [ ] Équilibrage final de l’économie et validation du plaisir de jeu avec des joueurs humains.
- [ ] Finir l’UI/UX et les interactions visuelles, contrôler les panneaux et enchaînements face au mockup, profiler et préparer la publication.
- [ ] Construire et vérifier les livrables de publication après clôture des jalons précédents.

# Journal de travail et tâches détaillées

## Banc de stress IA ×1000 — tâche historique

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

## Itération 8 — le fix marche (96.7% LAST PLAYER STANDING), le vrai sujet est l'équilibrage

Batch de 30 parties après le fix de l'itération 7 : **96.7% `LAST PLAYER STANDING`** (contre
99% d'anomalie avant), 1 seule `ANOMALY_TIMEOUT`. Le fix de faillite marche exactement comme
prévu. Mais les données propres révèlent le vrai sujet : parties très courtes (`Tour de fin`
moyen = 0.4), **29/30 parties se terminent avec exactement 3 joueurs sur 4 en faillite** —
pattern quasi uniforme. Winner rate par profil sur cet échantillon plus large : Negotiator
52%, Rentier 31%, Predator 17% (correction d'une lecture précédente sur trop petit
échantillon qui suggérait Rentier "ne gagne jamais").

- [x] **Diagnostic `[BANKRUPTCY-TRACE]` posé** (`RulesEngine.cs`, avant `OnInsolvency.Invoke`)
  pour tester l'hypothèse "IA achète sans réserve de trésorerie" proposée par une revue
  externe. Run de 30 parties → 143 événements capturés.
- [x] **Analyse quantifiée des 79 vrais déclenchements** (après dédoublonnage, voir bug
  ci-dessous) :
  - 71% des loyers tueurs viennent de tuiles **niveau 4-5** (quasi/full upgrade), 10%
    seulement de niveau 0 — le vrai moteur n'est pas "achète sans réserve" mais
    **"upgrade trop vite jusqu'au niveau 5 (×200)"**.
  - 94% des morts arrivent en 0-2 tours de plateau complétés.
  - Loyer médian tueur $19 200, mais 30% des morts viennent d'un loyer ≥$100K (jusqu'à
    $260K) — aucune réserve de cash raisonnable à ce stade de l'économie (Sgo encore
    ~$2-5K) ne protège contre un pic pareil.
  - Tuiles les plus mortelles : Lockheed Martin, CyberTruck Factory, NASA Moonbase,
    SpaceX, Antarctica Datacenter — toutes familles Aerospace/Tech/Auto (les plus chères
    du plateau).
- [x] **Bug trouvé au passage, corrigé** : 45% des événements tracés étaient des doublons
  exacts (même tuile/loyer, `cash_before=0`) — preuve d'un **double-dispatch IA**.
  Root cause (`TurnManager.cs`, case `ResolveTile`) : le bloc Auto-Pay Rent/Tax (qui
  appelle `rulesEngine.ExecutePrimaryAction` directement et résout déjà la tuile via
  `CompleteTileResolution`) était suivi, sans condition, par `aiManager.RequestAIDecision`
  pour toute IA — qui rappelle `ExecutePrimaryAction` une 2e fois sur la même tuile. Fix :
  la délégation IA ne se déclenche plus que si `currentState` est encore `ResolveTile`
  (i.e. la tuile a vraiment encore besoin d'une décision : case libre, Marketplace,
  Venture, Casino, Stock Market — les seuls cas que `TurnManager` ne pré-résout pas).
- [x] **Vérification demandée par la revue externe faite, RAS** : le tableau
  `rentMultipliers = {1,2,5,12,50,200}` (6 entrées, indices 0-5) est indexé exactement par
  `property.CurrentLevel` (qui va aussi de 0 à 5) — pas d'off-by-one, le ×200 ne s'applique
  bien qu'au niveau 5.
- [x] **3 logs d'instrumentation ajoutés** (tous en `LogWarning` pour survivre au filtre du
  stress test) pour la suite de l'investigation demandée par la revue externe :
  `[UPGRADE]` (`UpgradeSystem.TryUpgradeProperty` — joueur, niveau avant/après, coût,
  cash avant/après), `[ACQUISITION]` (les 4 vrais points de transfert de propriété trouvés
  par grep : `RulesEngine.TryBuyProperty`=Landing, `AuctionManager.EndAuction`=Auction —
  **jamais atteint en 100% IA, réservé au flow humain via `ActionButtonsWidget`** —,
  `MarketplaceSystem.ExecuteTrade`=Marketplace-Trade et `SealTenderDeal`=Marketplace-Tender
  / -Tender-Swap), `[RENT-CALC]` (niveau, loyer de base, multiplicateur appliqué, monopole,
  loyer final).

**Pas vérifié** : aucun accès Unity dans cette session. Prochaine étape pour Arthur :
relancer un batch de 20-30 parties, exporter la console, et transmettre pour l'étape 3 de
la revue externe (analyse forensique des 5 kills les plus chers : comment le tueur a
acquis la propriété, comment il a financé l'upgrade, à quel tour).

## 2026-09-26 — fiabilisation moteur et banc de test (autorisé par Arthur)

- [x] Paiement obligatoire au GO et liquidation sans dépendance UI pour les IA.
- [x] Arrêt centralisé des victoires et protection zéro survivant.
- [x] Valeur nette partagée conforme au GDD.
- [x] État neuf entre parties, contrôleurs et abonnements préservés correctement.
- [x] CSV 2–6 joueurs, compteurs explicites, graine et restauration du mode test.
- [x] Compilation Unity, régressions ciblées, puis smoke test 5 parties et batch 30 si propre.

Validation terminée le 1er octobre dans Unity 2022.3.62f3 : 27 assertions ciblées,
30/30 parties à quatre joueurs et 5/5 parties à six joueurs, sans anomalie ni erreur
capturée et avec invariants CSV validés. Le run de diagnostic à quatre joueurs a détecté
puis permis de corriger les revenus post-faillite liés au sabotage. Voir
`Docs/Validation_economie.md` pour les résultats, la portée et les dettes hors périmètre
encore ouvertes. Ces validations ne constituent pas une recette visuelle ou réseau.

## 2026-10-02 — familles, OPA et sanctions

- [x] Énergie Tier 3 corrigée à `100/3` et testée sur trois passages GO.
- [x] Huit passifs et huit ultimes couverts par une régression Unity ciblée.
- [x] OPA hostile alignée sur le GDD, avec transfert de propriété, paiement au vendeur,
  conservation des upgrades et absence de création monétaire.
- [x] Embargo aligné à `5 × Sgo`; Audit, Strike, Embargo et Sabotage testés avec leurs
  immunités et leurs refus sans fonds.
- [x] Durée du Cyber-Sabotage Armement unifiée sur trois tours du propriétaire.
- [x] Validation finale : 54/54 assertions mécaniques, 27/27 assertions économiques,
  30/30 parties à quatre joueurs et 5/5 à six joueurs, sans anomalie.
- [x] Ajouter l'étape de choix après le jet pour Assistance de Précision, côté humain et IA.
- [x] Ajouter les pickers humains : ultime disponible, destination Aerospace et cible Armement.
- [ ] Étendre le batch six joueurs au-delà de cinq graines avant toute décision d'équilibrage
  sur le pic observé à 8,18 M$.

Résultats et pré-évaluation : `Docs/Validation_mecaniques_2026-10-02.md`.

## 2026-10-02 — UI Marketplace et préparation WebGL

- [x] Réduire dynamiquement le panneau Marketplace pendant le choix d'une propriété.
- [x] Restaurer le panneau avec le nom, le revenu et l'action de confirmation après le clic.
- [x] Installer le module Unity WebGL 2022.3.62f3 et produire un build de développement.
- [x] Corriger le crash URP au premier rendu : textures de dithering absentes des profils
  de qualité et reflection probe temps réel désactivée dans le navigateur.
- [x] Passer WebGL sur le profil Low et réserver 256 Mo de mémoire initiale.
- [x] Valider dans le navigateur : démarrage, HUD, plateau et parcours Marketplace complet.
- [x] Auditer les 16 écrans UI Toolkit et le panneau historique : tailles tactiles,
  focus, états vides, fermeture, confirmations destructives, listes et transitions.
- [x] Rendre le template WebGL responsive et valider dans le navigateur les formats
  téléphone paysage, téléphone portrait et desktop.
- [x] Recetter en WebGL portrait les écrans rares : Venture longue, faillite sans actif
  avec confirmation en deux temps, et victoire avec classement complet.
- [x] Produire et démarrer un build WebGL Release compressé de 12 Mo, puis fiabiliser les
  mises à jour navigateur avec des noms de fichiers fondés sur leur hash.
- [x] Pré-évaluer le pipeline iOS : module Unity présent, compilation et génération IL2CPP
  lancées, projet Xcode créé ; blocage Burst identifié sur l'absence du SDK `iphoneos`.
- [ ] Installer Xcode complet, sélectionner son répertoire développeur, relancer l'export
  Unity iOS, compiler dans Xcode et installer le build sur un appareil.
- [ ] Installer Unity Android Build Support avec SDK, NDK et OpenJDK, puis produire et
  démarrer un export Gradle Release.
- [ ] Terminer la recette sur iPhone/iPad et Android physiques : encoche, barre d'accueil,
  densité réelle, chauffe, mémoire, gestes tactiles et textes exceptionnellement longs.
- [ ] Concevoir puis implémenter le multijoueur en ligne jusqu'à six joueurs : lobby,
  reconnexion, autorité de partie, réplication des commandes et tests de latence.

Preuves et limites : `Docs/Validation_ui_webgl_2026-10-02.md` et
`Docs/Validation_device_readiness_2026-10-04.md`.

## 2026-10-04 — multijoueur, tranche 1 : accueil et lobby

- [x] Évaluer Unity MPS, Photon, Nakama et un protocole HTTP adapté au tour par tour.
- [x] Ajouter un service de lobby ASP.NET Core sans dépendance externe ni compte fournisseur.
- [x] Créer et rejoindre une partie par code privé de six caractères.
- [x] Gérer deux à six joueurs, prêt/pas prêt, lancement par l'hôte et transfert d'hôte.
- [x] Ajouter les identifiants de reconnexion et les snapshots versionnés.
- [x] Ajouter l'accueil et le lobby UI Toolkit auto-initialisés dans Unity.
- [x] Mettre le moteur local en attente derrière le choix du mode de jeu.
- [x] Valider le serveur par un test bout en bout à six joueurs.
- [x] Recompiler Unity et conserver 54/54 assertions mécaniques.
- [x] Produire le build WebGL, corriger le responsive portrait et recetter le parcours
  hôte + second joueur + prêt + lancement sans erreur navigateur.
- [x] Définir et tester le protocole commandes/snapshots de la partie, puis ajouter les
  opérations typées correspondantes au client Unity.
- [x] Initialiser deux à six sièges humains et publier le snapshot initial autoritaire.
- [x] Synchroniser le lancer et le déplacement du joueur actif dans le premier tour.
- [x] Synchroniser la décision de case, l'achat, le passage et la fin du premier tour
  minimal, puis confirmer un tour déclenché par le second siège.
- [x] Synchroniser les quatre décisions Audit et l'Assistance de Précision −1/0/+1 ;
  corriger et tester le blocage « rester en Audit » après l'arrivée.
- [x] Synchroniser l'énergie, les cartes persistantes et les huit Ultimes, y compris les
  cibles Aerospace et Armement ; recette deux clients encore à intégrer au test complet.
- [x] Ajouter un journal d'activité autoritaire et léger : tour, dés, arrivée, achat,
  famille complétée, passage, Audit, faillite et activation de pouvoir ; conserver les
  douze derniers événements dans le snapshot et afficher un ticker animé unique.
- [x] Valider le ticker en WebGL desktop et portrait sans masquer le plateau ni le panneau
  de case ; régression Unity portée à 59 assertions et build WebGL réussi.
- [x] Synchroniser la Marketplace : passage, quatre sanctions, appel d'offres, réponses
  simultanées jusqu'à cinq enchérisseurs, offres cash/propriétés, annulation et choix du
  vendeur ; répliquer l'état complet pour la reconnexion et publier le feedback partagé.
- [x] Recompiler Unity avec 62 assertions, le serveur .NET Release sans avertissement et
  produire le WebGL de développement actualisé (`WEBGL PLAYER BUILD PASS`).
- [x] Répliquer la carte Venture en attente et sa confirmation sur le siège actif ; corriger
  l’avancement prématuré du tour avant application de l’effet.
- [x] Synchroniser l’OPA hostile : tirage et prix autoritaires, transfert cash/propriété,
  conservation des upgrades et feedback partagé ; régression portée à 64 assertions.
- [x] Synchroniser le flux multi-écran de faillite : dette, créancier et jeton dans le
  snapshot ; commandes autoritaires pour vendre les upgrades, hypothéquer, vendre le titre
  ou abandonner ; écran limité au siège débiteur et reprise du tour seulement après paiement
  ou élimination. Régression portée à 70 assertions ; panneau recetté en WebGL portrait et
  paysage, confirmation en deux temps et fermeture sans erreur navigateur.
- [x] Synchroniser les investissements et cycles économiques : commande dédiée autoritaire,
  placements actifs et marché dans le snapshot, rendements calculés par l’hôte, feedback
  partagé et panneau de choix compact. Régression 76/76 ; WebGL recetté en 430×840 et
  1280×720 sans chevauchement ni erreur navigateur.
- [x] Ajouter la persistance serveur atomique, les jetons hachés, la validation JSON et
  l’allow-list de commandes, CORS restreint, rate limiting, journaux structurés et option
  d’obligation HTTPS. Build .NET sans avertissement ; smoke test six joueurs, test de
  sécurité et reprise après deux redémarrages réussis. Le certificat/TLS réel sera fourni
  par l’hébergement de production.
- [ ] Tester reconnexion, latence et déconnexion avec deux à six clients WebGL.

Architecture et limites : `Docs/Multiplayer_foundation_2026-10-04.md`.

## À reprendre plus tard — validation du fun et équilibrage final

- [ ] Organiser **5 à 10 parties humaines** à 3–6 joueurs avant de modifier à nouveau
  les paramètres économiques.
- [ ] Mesurer la durée réelle, le temps d'attente entre deux décisions, la fréquence des
  choix intéressants et l'envie de rejouer.
- [ ] Relever les éliminations jugées injustes, particulièrement celles causées par les
  multiplicateurs de loyer niveau 4 (`×50`) et niveau 5 (`×200`).
- [ ] Vérifier en situation réelle que les pouvoirs de famille, OPA, sanctions et montants
  économiques sont compris sans explication extérieure.
- [ ] Étendre le batch six joueurs : le maximum actuel de **8,18 M$** repose sur un petit
  échantillon et ne justifie pas encore de changer la cible de 10 M$.
- [ ] Décider seulement après ces tests s'il faut adoucir les niveaux 4–5, accélérer la
  progression vers 10 M$ ou conserver les valeurs actuelles.

État mémorisé : le moteur économique est cohérent et ses régressions passent, mais
« économie parfaitement équilibrée » et « jeu confirmé fun » ne sont pas encore validés.

## Notifications d’activité — reprise du 8 octobre 2026

- [x] Ajouter des événements pour les loyers réglés, améliorations, taxes payées et résultats du casino.
- [x] Prioriser les événements importants dans le HUD, avec deux messages maximum sur écran large.
- [x] Raccorder les styles et la capacité mobile à la classe existante `ux-narrow`, et réduire la pile lors du redimensionnement.
- [x] Recetter le build final en 1280×720 et 430×840 : achat et passage côte à côte sur desktop ; passage conservé lors du tour suivant sur mobile ; aucune erreur navigateur.
- [ ] Compléter les cas limites : rotation avec deux notifications encore actives et noms de joueurs très longs.
- [ ] Vérifier la visibilité effective des achats/passages sur deux clients puis six ; le journal partagé existe, mais cette recette reste nécessaire.

Les builds précédents ont réussi et la régression mécanique précédente compte 76 assertions.
Cela ne prouve pas la lisibilité de la dernière disposition : le premier essai a montré
une seconde ligne masquée par le panneau, et le seuil mobile utilisait une classe absente.
Le binaire WebGL antérieur au dernier raccordement responsive ne constitue pas sa validation.

Validation du build final des notifications : `Build Finished, Result: Success` et
`WEBGL PLAYER BUILD PASS` dans `/tmp/polyrisk_activity_priority_webgl.log`.
Captures et périmètre exact : `Docs/Validation/Activity_2026-10-08/README.md`.

## Reconnexion de l’hôte — état de restauration

- [x] Répliquer le compteur de doubles consécutifs et les tours commencés par joueur.
- [x] Restaurer les doubles dans TurnManager/DiceSystem : régression Unity 80/80, dont relance après double et fin de tour après jet normal (`/tmp/polyrisk_reconnect_counters.log`).
- [x] Implémenter le démarrage sans boucle, la lecture serveur et la reprise de version/queue/historique ; compilation Unity validée.
- [ ] Recetter ce démarrage par HTTP avec réponse de publication perdue, état serveur invalide et reconnexion réelle de deux/six clients.
- [x] Extraire les fins de règlement loyer/GO ; conserver dans le snapshot le type de dette, la case, le détourneur et le solde du créancier avant règlement. Reconstruction de la continuation loyer à l’application autoritaire (recette réseau encore à faire).
- [x] Brancher les continuations loyer et DÉPART après application autoritaire, partager la fin du mouvement, bloquer la réexécution des cases pendant une dette. Validation C# partielle ; recette réseau à faire.
- [ ] Valider ces continuations avec un hôte réellement reconnecté, y compris paiement, faillite, victoire et Transit Express.
- [x] Brancher AdjustDice restauré sur l’attente de choix partagée, puis le déplacement avec le total sauvegardé ; RollDice avec total reprend sans relancer.
- [x] Persister départ/destination/phase GO et brancher MovingPawn hors dette sur la continuation partagée.
- [ ] Recetter le mouvement restauré avec deux/six clients, passage GO, insolvabilité, Transit Express et animation.
- [ ] Recetter Ajustement -1/0/+1 après reconnexion réelle, expiration et état immédiatement après confirmation.
- [x] Persister le résultat de partie et restaurer le gagnant/raison sans redémarrer les tours ; garde des commandes après victoire.
- [ ] Recetter l’écran de victoire et le rejet d’actions après reconnexion avec deux/six clients.
- [ ] Recetter la reconnexion de l’hôte sur partie neuve, décision de case, Venture, Marketplace et liquidation avec deux puis six clients.

Le WebGL actuellement servi à l’utilisateur conserve la version des notifications validée.
Le changement des compteurs a compilé et passé les tests Unity ; il sera intégré au prochain
build réseau après la mise en place du démarrage restauré. Les champs sont additifs dans
le snapshot schema 1 : les anciens snapshots sans ces données fournissent zéro, et leur
série de doubles historique ne peut pas être retrouvée.


## Retour humain mobile reçu le 12 octobre 2026

- [x] Premier essai WebGL sur iPhone 14 Pro Max : démarrage, boutons, panneaux et widget Marketplace jugés réactifs par l’utilisateur.
- [ ] Recette approfondie sur cet appareil avec le nouveau multijoueur et des amis ; relever puis prioriser les corrections finales.


## État Venture après reconnexion — 12 octobre 2026

- [x] Préparer la capture/restauration de pioche et défausse avec validation des vingt cartes.
- [ ] Exécuter les régressions de ce correctif dans Unity après le build actif.
- [ ] Intégrer le correctif au build suivant et recetter les tirages après reconnexion réelle.


## Recette navigateur du 12 octobre 2026 — portée précise

- [x] Deux clients réels : création/code/ready/lancement et reconnexion de l’hôte en Planning avec snapshot identique.
- [x] Corriger la non-publication des mouvements/effets autonomes ; V2 confirme une arrivée sans commande publiée.
- [x] V2 : réponse de publication perdue après achat, débit de 1100 une seule fois, propriétaire unique, séquence acquittée 1.
- [x] Venture/présence des états, génération de mutation et publication sérialisée : Economy 69, Mechanics 81 assertions.
- [x] Régression du correctif de sélection : Economy 71 assertions ; ordre nouveau tour puis sélection, sans reset durant le même tour.
- [ ] Vérifier ce correctif dans le build V3, puis poursuivre les commandes du second joueur et la recette à six.
- [x] V9 : vérifier la liste de six joueurs en paysage 900×430, actions visibles et Quitter fonctionnel ; publier sur le lien mobile existant.
- [ ] Vérifier la règle des doubles avec Moteur Turbo : le code appelle RollDice deux fois et doit ne compter/publier que le résultat retenu ; le choix humain du meilleur jet prévu au GDD reste à auditer.
- [ ] Continuer la recette complète à deux puis six clients, mobile, tests humains et corrections de publication.

- [x] V8 : six clients WebGL, six lancers manuels, achats/loyer, rechargement des six et achat après reprise validés (voir preuves Reconnect_2026-10-12).
- [ ] Confirmer V9 sur Safari/iPhone réel et organiser la recette approfondie entre amis.

- [x] Moteur Turbo : séparer candidats sans effets et validation du résultat retenu ; Mechanics Unity 85 assertions PASS.
- [ ] Moteur Turbo : choix humain des deux jets, état/snapshot/commande/boutons, reconnexion et recette réseau ; correctif non publié en V9.

- [x] Choix humain Turbo intégré au moteur, boutons, snapshot et commande ; Mechanics Unity 91 assertions PASS, serveur compile.
- [x] V10 : recette visuelle des deux choix Turbo, précision ensuite, pénalité et reconnexion réelle ; déployer API compatible après validation. — confirmé par les preuves V10/V11 ci-dessous.

- [x] V10 navigateur autorité : choix Turbo restauré depuis snapshot, jet inférieur choisi sans fausse amende, +1 ensuite et arrivée case6 ; v4 seq2.
- [x] Turbo : cinquième double retenu, invité navigateur, dimensions mobiles et publication API/build compatible. — confirmé par les preuves V10/V11 ci-dessous.

- [x] V10 navigateur : cinquième double choisi = amende500 une fois, aucun déplacement, tour suivant ; rechargement conserve acquittement seq3.

- [x] Turbo invité deux navigateurs : deux jets restaurés, choix natif transmis à hôte v3 seq1 AdjustDice total5 doubles0.
- [x] Précision invité après recharge : premier essai arrivé case5 au lieu6 (délai possible) ; retester et vérifier boutons face au timeout avant publication. — confirmé par les preuves V10/V11 ci-dessous.

- [x] Précision invité après reprise : essai chronométré réussi, v8 seq3 position6 (contrôle à53,4s).
- [x] UI Turbo portrait : titre/contextualisation de choix, bouton plein écran empiétant sur JET2, overlays debug à retirer pour livraison. — confirmé par les preuves V10/V11 ci-dessous.

- [x] Préparer V11 : contexte Turbo/précision, statistiques masquées pendant choix, overlays debug éditeur uniquement, plein écran hors formats courts.
- [x] Confirmer build V11 et rendu portrait/paysage, puis publication API/build compatible. — confirmé par les preuves V10/V11 ci-dessous.

- [x] V11 : Economy106/Mechanics92/build PASS, rendu Turbo portrait/paysage corrigé et publication même lien ; API5091 compatible, smoke public6 PASS.
- [ ] Poursuivre recette Marketplace avec offres, OPA et autres ultimes en réseau, tests humains et équilibrage final.

- [x] OPA V11 deux navigateurs : carte restaurée invité, confirmation réseau, paiement1550 au vendeur, propriété/niveau/upgrades transférés sans création de cash.
- [ ] OPA : vérifier absence de second transfert après reprise, cible inabordable en réseau et poursuivre marketplace avec offre acceptée.

- [x] OPA après recharge invité : soldes et séquence4 inchangés, carte non répétée.

- [x] OPA inabordable réseau : aucun débit/transfert, carte acquittée seq5.
- [x] Identifier et corriger source de superposition après reprise marketplace Reviewing/Spectator : masquer panneau case.
- [ ] Rebuild/recette du correctif et accepter offre mixte cash2500 + tile3 contre tile1 ; vente non encore confirmée.

- [x] V12 : offre mixte cash2500 + tile3 acceptée contre tile1, cash conservé/upgrades conservés ; reload deux clients sans second transfert, actions suivantes visibles.
- [x] Terminer rendu mobile de revue marketplace et publierV12 ; preuves portrait/paysage ci-dessous.

- [x] V12 revue marketplace : portrait/paysage contrôlés, vente mixte et reprise validées, publiée sur même lien HTTPS.

- [x] V12 parcours natif mise en vente : panneau dynamique, titre8 sélectionné et commande reçue ; invitation expire après5s.
- [ ] V13 : vérifier délai online30s, pseudo et bouton acceptation désactivé avant sélection ; reprendre soumission native jusqu’au deal.

- [ ] Audit durée GDD90min : TurnManager termine actuellement au dernier survivant ou50 tours de plateau ; aucune horloge de partie trouvée dans le code parcouru. Vérifier la règle90min et sa persistance réseau avant validation finale.

- [x] V13 build/test198 PASS et invitation avec pseudo/délai30 observée.
- [x] Identifier et corriger source de panneau case devant invitation : masquer à ShowAuctionNotification.
- [ ] V14 recette Participate/formulaire/soumission/deal ; V13 non publié.

- [ ] Marketplace vendeur après reprise InvitingPlayers : vérifier ShowWaitingForBids (pas de masquage explicite du panneau case) ; fixture market_waiting_fixture.json préparée.

- [x] Confirmer défaut reprise vendeur attente : panneau case devant offres ; correction source centralisée SwitchView préparée pourV15.
- [ ] Copier correctif au clone après buildV14, compilerV15 et vérifier attente vendeur/invitation/vente.

- [x] V14 invitation accessible et Participate ouvre formulaire ; superposition corrigée.
- [ ] Formulaire offre : actions hors écran1280×720 ; correction source ScrollView/footer séparé préparée pourV16, à compiler/recetter.

- [x] V14 soumission cash native mobile : offre2908 reçue et acceptée sur deux clients, soldes15092/20908 et titre8 transféré.
- [ ] V16 : vérifier formulaire desktop/mobile corrigé, offre avec propriétés proposée manuellement et attente vendeur restaurée.

- [x] V16 : actions formulaire desktop visibles, scroll fonctionnel et attente vendeur restaurée sans superposition.
- [x] Identifier/corriger source de sélection des titres du vendeur dans offre acheteur (3 chemins) ; ajouter3 régressions de propriété.
- [ ] V17 : vérifier nouvelles régressions, sélection titre acheteur, offre mixte manuelle et styles de défilement avant publication.

- [x] V17 Economy106/Mechanics95 : trois tests de propriété acheteur/vendeur PASS.
- [ ] V18 préparée : redéployer brouillon après chaque titre choisi, consigne de sélection, conserver brouillon sur réponse autre acheteur ; nouvelle régression à exécuter.

- [x] V17/V18 : offre mixte soumise par UI acheteur avec title3, puis acceptée par vendeur ; cash et deux transferts vérifiés.
- [x] V18 retour au brouillon après choix du titre : cash conservé, titre et total affichés, tests202/build PASS.
- [ ] V18 : terminer contrôle mobile/paysage (tab32), reprise post-deal et publication même lien.

- [x] V18 offre mixte native : cash2721 + title3 reçus/acceptés ; soldes15279/20721 et échange des titres corrects.
- [x] V18 formulaire mobile entier visible ; sélection réduit le panneau et affiche la consigne.
- [x] V19/V20 : cadrage plateau pendant sélection mobile ; plateau complet visible en 430×900, propriété ciblée sélectionnée et panneau réouvert avec le titre. Preuves `Docs/Validation/Reconnect_2026-10-12/visual_board_fit_v20.png` et `visual_board_selection_v20.png`.

- [x] V19 Economy106/Mechanics99 : 205 assertions PASS, dont cadrage portrait/landscape/vertical.
- [x] V20 : build Unity terminé avec Economy106 + Mechanics99 assertions PASS ; préversion publiée sur le lien HTTPS existant avec index V20 vérifié.

## Chantier UI/UX visuel et motion — avant fin du jeu (13 octobre 2026)

Périmètre strict : présentation et interactions uniquement ; aucune règle, donnée économique ou mécanique à modifier. Audit initial : `Docs/UI_Motion_Audit_2026-10-13.md`. La comparaison fidèle au mockup attend les deux captures actuelles et le mockup cible, dont les chemins/images ne sont pas encore accessibles dans cette session.

- [x] Identifier la cause principale du flou WebGL mobile : template forçait `devicePixelRatio = 1` ; V20 adapte la densité avec plafond 2 et budget 2,4 millions de pixels. Build et calcul vérifiés.
- [ ] Mesurer la netteté, la cadence et la chauffe sur iPhone 14 Pro Max réel avec V20 ; ajuster le plafond si nécessaire, comparer la taille CSS et le buffer de rendu.
- [x] V44 — overlay des icônes de famille et du rendement sur les propriétés : réutilise `TileIconResolver`, le ratio Rent/Price de l’inspecteur et les ancres projetées dans l’UI Toolkit. Le build WebGL a été vérifié en portrait 430×900 et en paysage 900×430 ; aucun mesh, matériau ou Canvas n’est ajouté. Le coût de rendu des éléments UI n’est pas profilé. Les hubs dont le loyer de base est nul affichent 0 % conformément aux données. Reste la recette de lisibilité et performance sur iPhone réel.
- [x] Plateau : icônes sectorielles, rendement de base et couleur de famille issue des données affichés sur les propriétés ; les cases spéciales gardent leur glyphe dédié. La recette du cadrage complet en sélection Marketplace et l’observation des zones visibles restent distinctes.
- [x] Plateau V21 : couleur des propriétés liée à leur vraie famille (17/26 écarts de l'ancien motif corrigés) ; lumière au repos abaissée, sélection Marketplace toujours visible et cliquable en portrait. Build 205 assertions PASS ; capture locale conservée.
- [x] Plateau — intensité au repos réduite en V21, cases admissibles seules en pulse et autres accents atténués pendant la sélection Marketplace en V43 ; aucune lueur forte permanente visible sur le build V44.
- [x] V45 audit de profondeur : le prefab du pion et les meshes de plateau autorisent déjà le shadow casting ; aucune géométrie ni draw call supplémentaire nécessaire sur les profils qui rendent les ombres.
- [ ] Ombres visuellement garanties sur tous les profils : Low/Very Low désactivent les ombres URP ; comparer les profils et mesurer l’impact mobile avant de changer le preset ou d’ajouter une ombre factice.
- [ ] HUD : hiérarchie typographique, espacements 8 pt, CAPITAL/NET WORTH/TURN/MARKET sur une ligne, couleurs sémantiques et séparateurs discrets.
- [x] Typographie V35 : Inter existant appliqué au menu et aux libellés/valeurs du HUD ; titre droit, gras et lisible, quatre indicateurs HUD sans débordement à 430 × 900. Build WebGL et 205 assertions PASS, +132 Kio sur V31, HTTPS chargé en navigateur. La comparaison au mockup et la typographie des autres panneaux restent ouvertes.
- [x] Overlays debug release : le build WebGL public utilise `BuildOptions.None` ; F1/F2, F5–F8 et F9 n'affichent leur OnGUI que dans l'éditeur, et les raccourcis de showcase sont compilés seulement pour Editor/Development. V24 publique ne montrait aucun overlay.
- [x] Accueil V31 : ScrollView vertical et barre sombre, sans superposition en 1280 × 720 et 900 × 430 ; ouverture de « Connection » et champ serveur atteints par défilement. La barre horizontale parasite a été supprimée en 430 × 900. Build WebGL + 205 assertions PASS ; nouveau lien HTTPS chargé en navigateur.
- [ ] Inspecteur : badge icône/famille/niveau, Rent/Risk/ROI seulement à partir de données existantes, tendance fiable, priorité visuelle primaire/secondaire ; vérifier le panneau réduit puis restauré.
- [x] Inspecteur V37 : famille/niveau affichés seulement sur une vraie propriété ; action principale pleine et secondaire en contour, lisibles en portrait 430 × 900. Unity WebGL et 205 assertions PASS ; capture `Docs/Validation/UI_Motion_2026-10-13/inspector_property_v37.png`. Restent le contrôle face au mockup, l'état réduit/restauré en marketplace et les statistiques Risk/ROI si des données fiables existent.
- [ ] Enchères : minuteur visuel et état critique, avatars par joueur, offres alignées et animées à l'arrivée, état vide stylé ; contrôler jusqu'à six joueurs sans surcharge UI.
- [x] Enchère Marketplace V38–V40 : minuteur orange puis rouge à 5 s, avatar rond coloré par siège, nom/offres alignés et état vide en carte ; cinq offres accessibles dans un salon de six joueurs en 430 × 900 et 900 × 430, actions visibles en paysage. 205 assertions et build WebGL PASS ; captures dans `Docs/Validation/UI_Motion_2026-10-13/`. L'animation d'une offre reçue en direct reste ouverte car la liste n'apparaît qu'à la revue.
- [ ] Avant publication, distinguer la vente Marketplace de l'enchère `AuctionManager` déclenchée depuis une case : aucun écran ne s'abonne actuellement aux événements `OnAuctionStarted`/`OnNewHighestBid`/`OnAuctionEnded`. Vérifier le parcours réel en Unity/WebGL et décider de son intégration sans changer les règles dans la passe UI actuelle.
- [x] V43 : sélection Marketplace avec pulse des cases admissibles et accents des autres cases atténués/désaturés ; restauration de l'état visuel au retour. Réutilise le shader/texture d'état sans draw call ajouté. Build WebGL + 205 assertions PASS ; lien V43 servi en HTTPS. L'échelle individuelle des tuiles est limitée par le mesh fusionné.
- [ ] Mesurer l'effet de sélection sur iPhone 14 Pro Max réel ; évaluer si un indicateur d'échelle mérite une géométrie dédiée. Haptique seulement si disponible sur appareil réel.
- [x] Achat V42 : glissière avec résistance initiale, seuil de validation à 90 %, retour sans achat si relâchée trop tôt, clavier Entrée/Espace et bouton classique avec « Réduire les animations ». Achat local contrôlé en WebGL 430 × 900 ; 205 assertions PASS.
- [x] HUD V41 : CAPITAL et NET WORTH interpolés par un animateur commun limité à 20 mises à jour/s, cible finale exacte, nouvelles cibles prises en compte en cours d'animation ; affichage immédiat avec « Réduire les animations ». Achat local contrôlé en WebGL à 430 × 900, 205 assertions PASS, taille du build stable. La glissière d'achat, l'haptique et le profilage réel restent ouverts.
- [x] Accessibilité V24 : toggle persistant « Réduire les animations » sur accueil et lobby, état synchronisé, arc des pions désactivé sans changer le temps de déplacement. WebGL 205 assertions PASS, contrôle navigateur en portrait.
- [ ] Étendre « Réduire les animations » aux futurs effets d'achat, d'enchère et de compteurs tout en gardant les confirmations d'action lisibles ; vérifier avec un utilisateur sensible au mouvement.
- [ ] Profiler avant/après sur WebGL mobile et desktop, contrôler temps de chargement et taille du build ; recette visuelle écran par écran face au mockup ; un commit logique par système.
