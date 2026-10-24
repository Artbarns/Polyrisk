# Échantillon économique apparié — 16 octobre 2026

## Résultat

La source Unity courante a exécuté 100 parties IA à quatre joueurs et 100 à
six joueurs, sur les mêmes graines 20261106–20261205. Aucun paramètre de règle
ou de donnée économique n’a été modifié. Les 200 lignes passent
`check_results.py` : aucune anomalie ni erreur enregistrée.

| Indicateur | 4 joueurs | 6 joueurs |
|---|---:|---:|
| Victoires par dernier survivant | 85/100 | 77/100 |
| Classements au timeout | 15/100 | 22/100 |
| Victoires à 10 M$ de cash | 0/100 | 1/100 |
| Cash final gagnant — médiane | 458 381 $ | 1 219 417 $ |
| Cash final gagnant — moyenne | 814 470 $ | 1 741 788 $ |
| Cash final gagnant — maximum | 5 956 779 $ | 11 187 282 $ |
| Plus haut pic de cash individuel | 6 096 170 $ | 11 187 282 $ |
| Valeur nette gagnante — médiane | 548 416 $ | 1 387 390 $ |
| Tours de plateau — médiane / maximum | 24 / 66 | 37 / 70 |

L’unique victoire à 10 M$ se produit à 6 joueurs, graine 20261197 (partie 92
si l’on compte depuis 1), au tour de plateau 61 : le joueur 1 finit avec
11 187 282,04 $ en cash et 11 197 582,04 $ de valeur nette. Le seuil est donc
atteignable, mais rare dans cet échantillon; le cash final peut le dépasser de
1 187 282,04 $ au moment où la condition de victoire est évaluée. À quatre
joueurs, aucune des 100 graines ne l’atteint. Les cashs gagnants sont
nettement plus élevés à six joueurs et les fins par timeout un peu plus
fréquentes.

## Méthode et limites

Les deux groupes utilisent des numéros de graine identiques. Cela facilite la
comparaison des distributions, mais ne forme pas des paires de trajectoires
déterministes : le nombre de joueurs change les actions, les tours et la
consommation aléatoire. Le batch six joueurs a dépassé sa limite de sécurité
de 30 minutes après 63 parties; ses 37 graines restantes ont été exécutées
dans une seconde session, puis les deux segments ont été réunis en réindexant
`GameIndex` de 0 à 99. Les graines restent continues et chaque CSV est validé.

Le temps `RealSecondsElapsed` est accéléré par le banc, pas une durée humaine.
Ces tests démontrent que le seuil de 10 M$ est atteint occasionnellement à six,
et qu’il ne l’a pas été dans cet échantillon à quatre. Ils ne disent pas si la
partie est amusante ou si le dépassement du seuil est satisfaisant. Aucune
correction économique n’est appliquée avant les 5 à 10 parties humaines déjà
prévues.

## Fichiers et reproduction

- `four_players.csv` et `six_players.csv` : exports complets des 100 parties.
- Journaux et traces bruts conservés sous `/tmp/PolyRiskEconomyPaired100_20261016/`
  sur la machine de recette.

```bash
python3 Docs/Validation/check_results.py \
  Docs/Validation/Economy_Paired100_2026-10-16/four_players.csv 100 4
python3 Docs/Validation/check_results.py \
  Docs/Validation/Economy_Paired100_2026-10-16/six_players.csv 100 6
```
