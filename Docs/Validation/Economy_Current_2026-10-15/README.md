# Rejeu économique sur la source courante — 15 octobre 2026

## Résultat

Un joueur autonome Unity macOS Development a été reconstruit depuis la source
courante avec Unity 2022.3.62f3. Les règles et paramètres économiques n’ont pas
été changés. Deux lots séparés de 30 parties IA ont été joués et leurs exports
ont passé `Docs/Validation/check_results.py` : aucune anomalie, aucun `ErrorCount`,
et les invariants CSV (vainqueur, faillite, cash, valeur nette et tours) sont valides.

| Participants | Dernier survivant | Classement à 50 GO | Victoire à 10 M$ | Cash gagnant min / médiane / moyenne / max |
|---:|---:|---:|---:|---:|
| 4 | 24 | 6 | 0 | 36 889 $ / 255 507 $ / 728 024 $ / 2 768 302 $ |
| 6 | 18 | 12 | 0 | 85 569 $ / 1 557 870 $ / 2 128 488 $ / 8 479 286 $ |

Valeur nette des gagnants : médiane 404 534 $ (4 joueurs) et 1 735 676 $
(6 joueurs). Le plus haut pic de liquidités individuel est de 3,73 M$ à quatre
joueurs et 9,68 M$ à six joueurs. Ces pics ne sont pas les soldes finaux ni des
victoires. Le lot six joueurs antérieur (14 octobre, 30 autres graines) avait
1 victoire à 10 M$ et un maximum de 13,54 M$ : cette issue est possible, mais
rare et très dispersée dans les simulations actuelles.

## Lecture

Dans ces stratégies IA, 80 % des parties à quatre et 60 % des parties à six se
terminent par élimination. Le cash médian du gagnant est très inférieur au seuil
de 10 M$, et les deux lots du 15 octobre ne l’atteignent jamais. Cela confirme
un écart entre le seuil nominal et les issues habituelles des IA ; cela ne
prouve pas à lui seul que le jeu humain doit être accéléré ou que le seuil doit
être changé. Le nombre de joueurs, les stratégies IA et la durée d’une partie
humaine affectent fortement le résultat. Aucune règle économique n’a été
ajustée sur la base de ces seuls lots.

La prochaine décision d’équilibrage doit rapprocher ces distributions des
observations de la partie humaine à quatre : durée, fréquence de faillite,
progression des actifs, sensation de rythme et envie de rejouer. Un multiplicateur
uniforme sur revenus et coûts pourrait simplement provoquer des faillites plus
rapides ; il doit être simulé comme une variante contrôlée avant d’être appliqué.

## Reproduction et limites

- Unity 2022.3.62f3, lecteur macOS `BuildOptions.Development`, source actuelle.
- Quatre joueurs : graines 20261016–20261045 ; CSV `four_players.csv`.
- Six joueurs : graines 20261046–20261075 ; CSV `six_players.csv`.
- La suite a utilisé le moteur de règles Unity et le banc IA accéléré. Elle ne
  mesure ni le rendu, ni le réseau, ni la durée réelle d’une partie, ni le plaisir
  humain. Des messages de shader non pris en charge apparaissent au démarrage
  headless ; le banc ne compte aucune erreur de jeu dans les 60 parties.
- Logs détaillés du build et des parties conservés temporairement sous `/tmp`
  sur la machine de recette ; ils ne font pas partie des livrables du dépôt.
