# Résultats Unity — 1er octobre 2026

- Unity : 2022.3.62f3, exécution `-batchmode -nographics`.
- Régressions ciblées : 27/27, code de sortie 0.
- Quatre joueurs : 30/30 parties, 0 anomalie, 0 erreur capturée, invariants CSV valides.
  - 25 `LAST PLAYER STANDING`, 5 `TIME OUT — HIGHEST NET WORTH`.
  - Cash gagnant min/médiane/moyenne/max : 68 019,31 / 471 236,24 / 823 994,82 / 2 990 240,28 $.
  - Export local : `StressTestResults/stress_test_20261001_112145_429_games.csv`.
- Six joueurs : 5/5 parties, 0 anomalie, 0 erreur capturée, invariants CSV valides.
  - 1 `LAST PLAYER STANDING`, 4 `TIME OUT — HIGHEST NET WORTH`.
  - Cash gagnant min/médiane/moyenne/max : 434 311,07 / 2 808 196,54 / 2 788 687,63 / 5 514 992,85 $.
  - Export local : `StressTestResults/stress_test_20261001_191015_412_games.csv`.
- Victoires à 10 M$ : 0/35.

Le banc utilise les classes, événements et coroutines du jeu dans la scène réelle. Il désactive le déplacement visuel et adapte le pas physique à l'accélération ; il ne constitue donc pas une recette de rendu, d'animation, d'interface humaine, de WebGL ou de réseau.
