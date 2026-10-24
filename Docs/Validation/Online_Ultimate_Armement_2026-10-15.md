# Pouvoir Armement en multijoueur WebGL — 15 octobre 2026

## Parcours exécuté

Deux clients WebGL de la build Release courante ont rejoint le même lobby local
isolé. L’invité a lancé les dés, joué son tour puis rendu la main ; l’hôte a
ensuite soumis `ultimate.use` pour Armement en ciblant Opioid Paradise
(propriété Pharma de l’invité). Le serveur a consommé la commande et l’hôte a
publié l’état mis à jour.

## Résultat

Le snapshot serveur après traitement est en version 6, séquence 4, toujours au
tour de l’hôte. L’hôte a dépensé son énergie (100 → 0), le solde des deux joueurs
est resté à 18 000 $, et Opioid Paradise est restée propriété de l’invité avec
`hijackedByPlayerId = 0` et `hijackTurnsRemaining = 3`. Le journal d’activité
contient « active Cyber-sabotage sur Opioid Paradise ». La requête a donc bien
été exécutée par l’autorité Unity puis reflétée dans l’état réseau.

L’écran de l’invité est resté connecté pendant le tour de l’hôte et le serveur
servait le snapshot partagé. La capture tardive de l’écran invité n’a pas
conservé le toast temporaire du pouvoir ; l’affichage précis de l’effet sur le
plateau chez l’autre joueur reste à valider visuellement.

## Portée et suite

Cette preuve couvre un pouvoir ciblé, sa consommation d’énergie et sa mutation
d’état entre deux clients WebGL. La recette Pharma séparée confirme aussi la
réplication visible et la reprise après rechargement pendant un effet temporaire
(`Docs/Validation/Online_Ultimate_Pharma_2026-10-15.md`). Les six autres pouvoirs
et les refus de cible invalide/immunisée côté réseau restent à valider. La partie
humaine à quatre confirmée par l’utilisateur complète le test de salon, mais ne
remplace pas ces contrôles ciblés.

Le test a utilisé un serveur, des données et des ports loopback temporaires ;
il n’a touché ni au service de préversion ni aux règles du jeu. Aucun code
produit n’a été modifié pour cette recette.

## Reprise de l’hôte pendant le sabotage actif — 15 octobre 2026

Un contrôle supplémentaire a appliqué Armement depuis l’autorité Unity/WebGL
Release, puis rechargé cette page hôte pendant que le sabotage était actif.
Deux sièges étaient présents dans le lobby QA `TDEWRG` : l’hôte possédait les
deux propriétés Armement et l’invité la propriété Pharma Opioid Paradise
(case 8). Le joueur hôte avait 100 % d’énergie en phase `Planning`.

La commande `ultimate.use` a produit le sabotage attendu
(`hijackedByPlayerId = 0`, durée 3), dépensé l’énergie une seule fois (100 → 0)
et publié l’activité Cyber-sabotage. Après rechargement complet du client hôte,
le serveur a restauré la même version 3, la séquence 1, la durée restante 3,
le même propriétaire, les mêmes soldes (18 000 $ chacun) et la phase
`Planning`. Aucune exception JavaScript ni seconde dépense n’a été relevée. Le
bandeau d’activité et le plateau sont visibles après reprise.

Preuves : [snapshot après reprise](Online_Ultimate_Armement_2026-10-15/state_after_host_reconnect.json), [avant rechargement](Online_Ultimate_Armement_2026-10-15/before_reload.png) et [après rechargement](Online_Ultimate_Armement_2026-10-15/after_reload.png).

Le joueur adverse était un siège API QA, pas un second navigateur WebGL rendu.
Cette étape confirme la reprise de l’hôte pendant l’effet et la persistance du
snapshot ; elle ne confirme pas le rendu du sabotage sur l’écran d’un invité.

## Reconnexion de l’invité pendant un effet Armement actif — 15 octobre 2026

Dans un lobby QA isolé à deux clients WebGL Release (`B29WNJ`), l’hôte a
appliqué Cyber-sabotage à Opioid Paradise, détenue par l’invité. La commande a
été consommée une fois (version 4/séquence 1 → version 5/séquence 2), l’hôte a
dépensé 100 → 0 énergie et la cible a conservé son propriétaire avec le
saboteur actif pour trois tours. L’invité a rechargé sa page pendant l’effet ;
son client WebGL a rejoint le lobby et a reçu le snapshot version 5/séquence 2,
avec le sabotage et les trois tours restants. Pas de nouvelle dépense, nouvelle
commande ni exception JavaScript ; tour et phase Planning sont restés inchangés.

L’hôte autoritaire a conservé exactement 18 000 $ pour chacun des deux joueurs.
La vue renvoyée à l’invité a masqué le solde adverse selon Dark Board : le
représentant affiché est 20 000 $ pour l’hôte, tandis que le solde propre de
l’invité est 18 000 $. Ce nombre projeté n’est ni un paiement ni une mutation
du solde réel. Les deux captures montrent l’état après reprise ; les snapshots
serveur et invité sont conservés dans ce dossier.

Ce contrôle QA injecte le pouvoir par l’API dans une partie rendue, puis vérifie
la reconnexion réelle du client invité. Il confirme la persistance et la
réplication de l’effet Armement entre clients ; il ne remplace pas le test
humain du pouvoir. Les fichiers de preuve ne contiennent pas de jeton de
reconnexion.
