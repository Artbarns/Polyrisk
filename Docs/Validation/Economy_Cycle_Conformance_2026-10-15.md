# Conformité des cycles économiques — 15 octobre 2026

## Décision

Le code ne respectait pas entièrement les règles macroéconomiques du GDD. La
correction rétablit les règles déjà documentées ; elle ne constitue pas un
nouvel équilibrage. Les tests déterministes, les lots IA et un build WebGL
Release passent. L’équilibrage ressenti par des joueurs reste ouvert.

## Écarts relevés et correction

- En CRASH, le multiplicateur `×0,6` s’appliquait aux loyers, mais pas au salaire
  Sgo ni aux dividendes. `FinanceSystem` applique maintenant `×0,6` à ces crédits
  aussi ; le Burn Rate reste intégralement dû.
- Le plafond du déplacement boursier en CRASH ne dépendait pas de la durée. Il
  suit maintenant le GDD : 1 tour `−50/+20`, 3 tours `−50/+42`, 5 tours
  `−40/+72` (points de pourcentage).

Cas chiffré vérifié : avec $1 000 de salaire Sgo, $240 de dividendes et $3 000
de Burn Rate, l’ancienne exécution créditait $1 240 puis débitait $3 000
(solde net −$1 760). La règle GDD crédite $744 (`0,6 × $1 240`) et conserve le
débit de $3 000 (solde net −$2 256), soit $496 de moins.

## Vérifications

- `EconomyRegression` : **114/114 assertions PASS**.
- `MechanicsRegression` : **126/126 assertions PASS**.
- Unity 2022.3.62f3, build WebGL Release `BuildOptions.None` : **PASS**,
  environ 12 MiB.
- Lot IA de 30 parties à 4 joueurs : CSV valide, zéro anomalie, 8 fins par
  limite de temps et 22 par dernier joueur debout ; cash gagnant médian
  $288 665, maximum $5 562 287 ; aucune victoire à $10 M.
- Lot IA de 5 parties à 6 joueurs, comparé aux mêmes 5 graines du lot
  précédent : CSV valide, zéro anomalie, aucune victoire à $10 M. Les cinq
  graines conservent leur type de fin ; les cash gagnants médians passent de
  $3 066 552 à $2 208 046, mais les résultats individuels évoluent dans les
  deux sens. Cinq parties donnent une indication, pas une estimation robuste
  de l’équilibrage six joueurs.

Les lots à 4 joueurs ont des distributions très dispersées et la médiane du
cash gagnant a augmenté dans le lot corrigé malgré le revenu de crise réduit.
Ces simulations suivent des trajectoires IA déterministes sensibles aux
branches de jeu ; elles ne permettent donc pas d’attribuer causalement une
variation globale aux seuls multiplicateurs. Il faut conserver une recette
humaine sur plusieurs parties avant toute nouvelle modification des paramètres.

## Artefacts

- Source : `Assets/Scripts/Systems/CycleManager.cs`,
  `Assets/Scripts/Systems/FinanceSystem.cs`.
- Régressions : `Assets/Scripts/Editor/EconomyRegression.cs`,
  `Assets/Scripts/Editor/MechanicsRegression.cs`.
- Comparaison quatre joueurs :
  `/tmp/PolyRiskCrashCycleRuns/stress_test_20261015_151555_639_games.csv`.
- Échantillon six joueurs :
  `/tmp/PolyRiskCrashCycleRuns/stress_test_20261015_153109_565_games.csv`.
- Log de validation combinée : `/tmp/PolyRiskCrashCycleValidation.log`.
