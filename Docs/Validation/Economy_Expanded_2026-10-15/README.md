# Échantillon économique supplémentaire — 15 octobre 2026

## Résultat

La source courante a été reconstruite en lecteur macOS Development Unity
2022.3.62f3, puis le banc `StressTestRunner` a joué 30 parties à quatre et 30 à
six joueurs avec la même plage de graines, 20261076–20261105. Le validateur
`check_results.py` passe pour les deux CSV : 60/60 parties, zéro anomalie,
aucun compteur d’erreur, lignes et invariants de victoire cohérents. Aucune
règle ni donnée économique n’a changé.

| Indicateur | 4 joueurs | 6 joueurs |
|---|---:|---:|
| Fins par dernier joueur debout | 23/30 | 14/30 |
| Fins au plafond de tours | 7/30 | 16/30 |
| Victoires à 10 M$ de cash | 0/30 | 0/30 |
| Cash du gagnant — médiane | 724 647 $ | 2 036 009 $ |
| Cash du gagnant — moyenne | 949 870 $ | 1 869 181 $ |
| Cash du gagnant — maximum | 4 582 410 $ | 4 847 291 $ |
| Plus haut pic individuel de liquidités | 4 582 410 $ | 6 041 785 $ |
| Maximum de tours de plateau | 62 | 69 |

## Lecture et limites

Dans cet échantillon IA, aucune partie n’approche une victoire à 10 M$ en cash
final, même si les pics de liquidités à six joueurs dépassent 6 M$. Les parties
à quatre finissent surtout par élimination ; à six, la limite de tours est
plus fréquente. Ces données décrivent les profils IA et ne mesurent ni le
rythme réel ni le plaisir. Elles ne justifient pas à elles seules de modifier
les règles ou le seuil. Les graines initiales communes facilitent une lecture
comparative, mais les parcours ne sont pas des paires déterministes : le
nombre de joueurs change la consommation aléatoire et les branches de jeu.

Le temps Unity (`RealSecondsElapsed`) est accéléré par le banc et n’est pas la
durée vécue d’une partie. Il reste nécessaire de recueillir 5 à 10 résultats
humains avant un nouvel équilibrage. Les autres lots et leur dispersion,
notamment l’ancienne graine à six joueurs qui atteint 13,54 M$, sont décrits
dans `Economy_Current_2026-10-15/`, `Economy_PairedSeeds_2026-10-15/` et
`Economy_SixPlayer_2026-10-14/`.

## Reproduction

```bash
python3 Docs/Validation/check_results.py \
  Docs/Validation/Economy_Expanded_2026-10-15/four_players.csv 30 4
python3 Docs/Validation/check_results.py \
  Docs/Validation/Economy_Expanded_2026-10-15/six_players.csv 30 6
```

CSV bruts : `four_players.csv` et `six_players.csv` dans ce répertoire. Le
journal et les traces détaillées du banc restent sous `/tmp` sur la machine de
recette.
