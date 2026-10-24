# Contrôle économique à six joueurs — lot supplémentaire du 15 octobre 2026

## Résultat

Le batch Unity 2022.3.62f3 a terminé 30 parties IA à six joueurs, graines
20261016–20261045. Le processus a rendu le code 0 ; `StressTestRunner` indique
30 parties terminées et 0 anomalie. `check_results.py` confirme les invariants
des 30 lignes CSV et `ErrorCount` vaut 0 partout. Aucun paramètre de règle ou
d'économie n'a été modifié.

| Indicateur | 4 joueurs | 6 joueurs |
|---|---:|---:|
| Parties terminées sans anomalie | 30/30 | 30/30 |
| Dernier survivant | 22 | 18 |
| Classement à la limite de tours | 8 | 12 |
| Victoires à 10 M$ de cash | 0 | 0 |
| Valeur nette finale du gagnant, médiane | 398 868 $ | 1 549 153 $ |
| Valeur nette finale du gagnant, moyenne | 1 158 245 $ | 2 057 063 $ |
| Cash final du gagnant, médiane | 288 665 $ | 1 408 305 $ |
| Cash final du gagnant, maximum | 5 562 287 $ | 6 128 727 $ |

Les parties à six joueurs produisent ici une fortune gagnante plus élevée,
mais aucune des 30 ne finit au seuil de 10 M$. Les 30 autres graines à six
joueurs consignées le 14 octobre incluaient une victoire à 13,54 M$ : le seuil
reste donc atteignable dans les simulations, sans être fréquent dans les lots
observés. Ces résultats décrivent des stratégies IA et ne démontrent ni le
rythme humain ni le plaisir de jeu.

## Méthode et limites

Le lot à six joueurs utilise les graines 20261016–20261045, également utilisées
pour un lot à quatre joueurs conservé ici. Ce contrôle compare les distributions
agrégées ; il ne traite pas deux lignes portant le même numéro de graine comme
une paire déterministe. La validation humaine à quatre joueurs reste la seule
recette humaine consignée. Il faut encore recueillir 5 à 10 parties à 3–6 joueurs
avant toute décision d'équilibrage.

`RealSecondsElapsed` est le temps accéléré du banc de simulation, pas la durée
d'une partie vécue. `TurnsStarted` compte les tours individuels ; les tours de
chaque joueur et les tours de plateau sont détaillés dans le CSV.

## Fichiers de preuve

- `six_players.csv` : résultats des 30 parties Unity à six joueurs.
- `six_players_summary.txt` : résumé écrit par le batch Unity.
- `four_players.csv` : export à quatre joueurs utilisé pour la lecture agrégée.
- Validation : `python3 Docs/Validation/check_results.py <CSV> 30 <joueurs>`.
