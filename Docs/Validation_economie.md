# Validation du moteur économique

## Tests ciblés

Dans Unity 2022.3.62f3, la méthode Editor `PolyRisk.Editor.EconomyRegression.Run` vérifie les classes de production : dettes envers la banque et les joueurs, liquidation IA sans UI, attente de décision humaine au GO, arrêt à 10 M$, zéro survivant, valeur nette, indépendance des propriétés runtime et démarrage de 2 à 6 participants.

Exécution hors interface (remplacer les chemins selon la machine) :

```sh
Unity -batchmode -nographics -quit -projectPath /chemin/PolyRisk -executeMethod PolyRisk.Editor.EconomyRegression.Run -logFile /tmp/polyrisk-regression.log
```

Le log doit contenir `ECONOMY REGRESSION PASS` et le processus doit terminer avec le code 0.

## Parties dans la scène réelle

```sh
Unity -batchmode -nographics -projectPath /chemin/PolyRisk -executeMethod PolyRisk.Editor.EconomyBatchValidation.Start -polyGames 5 -polyPlayers 4 -polySeed 260926 -logFile /tmp/polyrisk-smoke.log
```

Ne pas ajouter `-quit` : le contrôleur Editor attend la fin des parties puis quitte lui-même. Code de sortie 0 seulement si le nombre demandé de parties s'est terminé sans anomalie. Limite de sécurité du contrôleur : 30 minutes réelles. La scène est chargée sans réécriture de ses paramètres sérialisés ni ajout obligatoire aux Build Settings.

Dans l'éditeur interactif, F9 lance le batch configuré dans StressTestRunner. Commencer par 5 parties ; passer à 30 si le résultat est propre. Le nombre de participants du banc est distinct de celui de la partie normale et peut être réglé entre 2 et 6. Le mode accéléré, le générateur aléatoire et les flags IA sont restaurés à la fin ; une partie normale neuve est rechargée.

Le pas physique est multiplié par l'accélération pendant chaque simulation, puis restauré avec les réglages temporels d'origine. Le banc ne valide pas la physique ni l'animation des pions : cette dernière est déjà désactivée. Les décisions et flux économiques utilisent les classes de production, leurs événements et coroutines.

Les fichiers sont écrits dans `StressTestResults/`, ignoré par Git :

- `_games.csv` : une ligne par partie, six blocs joueurs (vides pour les places absentes), graine, vainqueur, motif, cash final/pic, valeur nette, tours individuels et passages GO.
- `_trace.log` : identifiant de partie, changements de cash et logs Warning/Error/Exception, dont acquisitions, loyers et upgrades payants/gratuits Tech.
- `_summary.txt` : nombre de parties, anomalies, motifs et moyenne de cash des gagnants.

`TurnsStarted` compte les tours individuels (les relances de doubles restent dans le même tour). `MinActiveLaps` exclut les éliminés ; les colonnes `P*_Laps` conservent les parcours de chacun. Aucun de ces compteurs n'est une estimation directe d'une durée de partie humaine. Le pic de cash inclut les crédits avant règlement des frais du GO ; le cash final est distinct.

La graine initialise les dés et le paquet Venture. Elle aide à reproduire un scénario ; une reproductibilité exacte entre plateformes ou versions du moteur n'est pas garantie, car la simulation utilise encore des coroutines et le générateur Unity partagé.

Vérifier aussi les invariants des exports :

```sh
python3 Docs/Validation/check_results.py StressTestResults/nom_du_batch_games.csv 30 4
```

Le contrôle exige le nombre attendu de parties/joueurs, zéro anomalie et zéro erreur capturée, un vainqueur vivant, un seul survivant pour ce type de victoire, des soldes nuls chez les éliminés, des montants finis, des pics au moins égaux aux soldes finaux et des compteurs cohérents.

## Portée

Ces tests ne remplacent pas la recette visuelle et humaine des panneaux, ni les essais navigateur ou réseau. Aucun paramètre d'équilibrage (prix, loyers, coûts, bonus) n'est modifié pour faire réussir les tests. L'objectif reste de mesurer une économie exécutée correctement avant de rééquilibrer.

La correction des dettes couvre les loyers et la maintenance au GO. Elle ne démontre pas la conformité de toutes les pénalités : `FinanceSystem.ProcessTax` conserve un simple avertissement en cas de débit refusé, et plusieurs cartes Venture (#11, #14, #17 notamment) terminent encore leur action après un `DeductMoney` potentiellement refusé. Ces parcours, ainsi que les enchères des biens des éliminés prévues au GDD, restent à traiter avant de qualifier l'économie de totalement conforme. Les campagnes mesurent le moteur actuel et les stratégies IA actuelles, pas une économie finale certifiée ni une partie humaine.

## Résultats validés du 1er octobre 2026

Unity 2022.3.62f3 a exécuté 27 assertions ciblées avec le code de sortie 0. Le détail compact est archivé dans `Docs/Validation/2026-10-01/regression.txt`.

Le batch final à quatre joueurs (`stress_test_20261001_112145_429`) contient 30 parties sur 30 sans anomalie ni erreur capturée. Le validateur CSV confirme les participants, graines, vainqueurs, survivants, soldes, valeurs nettes, pics de cash, tours et passages GO. Résultats : 25 victoires par dernier survivant et 5 au classement après 50 passages GO actifs. Le cash des gagnants va de 68 019,31 $ à 2 990 240,28 $, avec une médiane de 471 236,24 $ et une moyenne de 823 994,82 $. Le maximum de passages GO observé par partie va de 5 à 57, médiane 24,5.

Le smoke test à six joueurs (`stress_test_20261001_191015_412`) contient 5 parties sur 5 sans anomalie ni erreur capturée, avec invariants CSV valides et les six blocs joueurs renseignés. Résultats : 4 victoires au classement après 50 passages GO actifs et 1 par dernier survivant. Le cash des gagnants va de 434 311,07 $ à 5 514 992,85 $, avec une médiane de 2 808 196,54 $ et une moyenne de 2 788 687,63 $. Le maximum de passages GO va de 15 à 57, médiane 53.

Aucune des 35 parties finales n'atteint 10 M$ en liquidités. À quatre joueurs, l'élimination arrive le plus souvent avant ce seuil. À six joueurs, les fortunes montent davantage mais la limite de 50 passages GO actifs intervient généralement avant 10 M$. Le moteur économique corrigé fonctionne sur ce périmètre ; les paramètres actuels ne produisent donc pas l'objectif de fin à 10 M$ dans ces graines et avec ces stratégies IA.

## Lecteur autonome pour les longues séries

Le 26 septembre, un lancement dans l'éditeur sans interface est tombé à 623 images en 240,42 secondes : son timeout ne permettait pas de conclure à un blocage logique. Le CSV conserve cette mesure (`FramesElapsed`) ; ne pas mélanger ce run interrompu avec les séries propres.

Le 1er octobre, un échantillonnage macOS `sample` de trois secondes pendant le ralentissement a trouvé 907/983 échantillons du thread principal dans `FixedUpdatePhysicsFixedUpdate`. Le banc utilisait ×100 avec un pas physique inchangé à 0,02 s. La correction adapte ce pas uniquement pendant les tests. Aucun script de jeu ne dépend de `FixedUpdate`, `WaitForFixedUpdate`, `Rigidbody` ou des callbacks de collision. Le lancement de diagnostic du 1er octobre a été interrompu avant toute partie complète ; il ne constitue pas un résultat économique.

Après cette correction, les cinq premières graines ont retrouvé exactement les mêmes données économiques que le smoke propre du 26 septembre (tous les champs CSV hors temps/images), en 6,97 à 17,11 secondes par partie. Ce lancement (`20261001_111128_116`) a ensuite révélé un défaut à la graine 260939 : un auteur de sabotage éliminé recevait encore des commissions. Il a été arrêté après 20 parties, sans être validé. La faillite annule désormais ses détournements sur le plateau et la résolution de sa propre dette ne lui reverse rien après élimination. Un loyer partiellement récupéré reste compté comme revenu pour la maintenance. Ces cas sont couverts par les régressions ciblées ; le banc signale également tout éliminé conservant du cash ou une propriété.

Un lecteur Mac de développement peut être construit, sans modifier la liste des scènes du projet :

```sh
Unity -batchmode -nographics -quit -projectPath /chemin/PolyRisk -executeMethod PolyRisk.Editor.EconomyBatchValidation.BuildPlayer -logFile /tmp/polyrisk-build.log
```

La sortie est `/tmp/PolyRiskEconomyValidation.app`. Lancer son exécutable avec `-batchmode -nographics -polyGames 30 -polyPlayers 4 -polySeed 260926 -polyResults /chemin/PolyRisk/StressTestResults -logFile /tmp/polyrisk-native.log`. Le lecteur quitte avec le code 0 seulement si toutes les parties demandées sont terminées sans anomalie. Le mode simulation automatique est activé uniquement en batch et en présence de `-polyGames`.

## Campagne six joueurs — 14 octobre 2026

Le batch six joueurs a été étendu à 30 graines (`20261014`–`20261043`) dans un
lecteur macOS standalone compilé avec Unity 2022.3.62f3. Les 30 parties finissent
sans anomalie et sans erreur capturée. Le validateur confirme six blocs joueurs,
les invariants de faillite, les gagnants et les compteurs après normalisation du
motif de victoire contenant des virgules. Le writer du banc est corrigé et un
rejeu de la graine `20261042` vérifie le CSV cité avec le validateur existant.

Résultats : 17 victoires par dernier survivant, 12 au classement à 50 passages GO,
et 1 à 10 M$ de cash. Le cash final des gagnants varie de 90 453,32 $ à
13 536 753,93 $ (médiane 1 601 678,52 $, moyenne 2 495 258,95 $). Le plus haut
cash final et pic de cash individuels sont respectivement 13 536 753,93 $ et
14 280 918,67 $. Ainsi, 29/30 gagnants finissent sous 10 M$, et un dépasse le
seuil de 3,54 M$. Les causes et limites de cette campagne sont détaillées dans
`Docs/Validation/Economy_SixPlayer_2026-10-14/README.md`; elle ne remplace pas
les tests humains avant de modifier l’équilibrage.
