# Pouvoir Pharma en multijoueur WebGL — 15 octobre 2026

## Parcours exécuté

Deux clients WebGL Release ont rejoint un lobby local isolé. L’hôte détenait
les trois propriétés Pharma et avait 100 points d’énergie. Après la reprise du
tour par l’hôte, il a envoyé `ultimate.use` pour Pharma. L’invité a affiché
l’annonce « QA Reduced active Protocole sanitaire » pendant que le serveur
appliquait l’effet.

## Résultat

Le serveur a accepté la commande (séquence 1). Le joueur hôte a dépensé son
énergie (100 → 0), reçu `hasImmunityCard = true` et
`immunityTurnsRemaining = 1`. Les deux soldes sont restés à 18 000 $. La
propriété Pharma est restée au joueur hôte et l’activité serveur annonce
« active Protocole sanitaire ».

Après rechargement du client invité, celui-ci a retrouvé le plateau et le HUD
de la partie. Le snapshot capturé après sa reconnexion était à la version 4,
séquence 1, conservait le joueur courant, les trois propriétés Pharma, l’énergie
à zéro et l’immunité à un tour. Le rechargement n’a donc ni annulé l’effet, ni
consommé une seconde fois l’énergie.

## Portée

Ce contrôle prouve l’exécution autoritaire du pouvoir Pharma, son annonce
visuelle côté invité et la reprise du client pendant l’effet. Il a utilisé deux
navigateurs WebGL, un serveur et des données temporaires sur loopback ; aucune
règle ni source de jeu n’a été modifiée. Les autres ultimes, les refus de
cibles invalides/immunisées et la reprise sous d’autres effets temporaires
restent à tester.
