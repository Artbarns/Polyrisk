# Campagne économique à six joueurs — 14 octobre 2026

## Résultat

30 parties avec six IA, graines `20261014` à `20261043`, moteur Unity 2022.3.62f3 et lecteur macOS autonome. Le build standalone réussit ; le lot se termine en 30/30 sans anomalie et avec `ErrorCount=0`. Les invariants du CSV validé passent via `Docs/Validation/check_results.py`.

| Mesure | Résultat |
|---|---:|
| Dernier survivant | 17 parties |
| Classement au seuil de 50 passages GO | 12 parties |
| Victoire à 10 M$ de cash | 1 partie |
| Cash final du gagnant — min / médiane / moyenne / max | 90 453,32 $ / 1 601 678,52 $ / 2 495 258,95 $ / 13 536 753,93 $ |
| Plus haut cash final d’un joueur | 13 536 753,93 $ |
| Plus haut pic de cash individuel | 14 280 918,67 $ |
| Passages GO maximum — min / médiane / max | 8 / 50,5 / 68 |

29 gagnants sur 30 finissent sous 10 M$. Une partie franchit le seuil et se termine à 13,54 M$ ; le jeu ne s’arrête donc pas systématiquement « autour de 10 M$ ou un peu moins ». La victoire vérifie le cash après règlement du passage GO : les crédits et frais de ce règlement peuvent produire un dépassement du seuil. Le cas isolé doit être rapproché des futures parties humaines avant de décider d’un ajustement.

## Portée et limites

Ce sont des parties IA accélérées, avec les profils actuels et les règles actuelles. Elles ne mesurent ni la durée humaine, ni le plaisir, ni la compréhension des mécaniques, ni le risque d’élimination jugé injuste. Aucun prix, loyer, coût, bonus ou règle n’a été modifié. Les tests humains à 3–6 joueurs restent nécessaires avant toute décision d’équilibrage.

Le premier export du lot contenait une ligne mal formée : le motif `REACHED $10,000,000 IN NET CASH` avait ses virgules interprétées comme des délimiteurs. La ligne a été normalisée sans modifier les valeurs et les 30 lignes ont passé le validateur. L’exporteur du banc cite désormais correctement les champs CSV ; le rejeu de la graine `20261042` confirme l’écriture avec guillemets et passe le validateur.

## Artefacts

- `games.csv` — 30 lignes, avec le motif de victoire correctement cité.
- Logs complets du lot et du rejeu conservés hors du dépôt sous `/Users/arthurbarreau/.codex/validation/PolyRiskNetwork/` et `/Users/arthurbarreau/.codex/validation/PolyRiskEconomySixPlayer30/`.
