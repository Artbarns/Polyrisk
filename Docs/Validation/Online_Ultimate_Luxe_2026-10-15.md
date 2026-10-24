# Pouvoir Luxe en multijoueur WebGL — 15 octobre 2026

Deux clients WebGL Release indépendants ont rejoint le lobby QA local
`8WUKEU`. Avant l’initialisation de l’autorité, la fixture attribuait au joueur
invité les trois propriétés Luxe (tuiles 23, 24 et 26), avec 100 points
d’énergie, 18 000 $ et la phase `Planning`. Depuis l’interface invitée, il a
sélectionné Luxe dans le sélecteur d’ultime.

Le serveur a accepté `ultimate.use` (séquence 1) et publié l’état en version 2.
L’activité autoritaire est « active Taxe somptuaire » ; l’énergie de l’invité
passe de 100 à 0. Son solde passe à 18 180 $ et celui de l’hôte à 17 820 $,
soit un transfert de 1 % des 18 000 $ de l’hôte. Les trois propriétés restent
à l’invité. Les deux clients WebGL affichent les soldes correspondants et
restent connectés, l’hôte en attente.

Le test a utilisé un serveur et un stockage isolés sur loopback. La bannière
d’activité temporaire n’était plus visible à la capture. Aucune règle, donnée
économique ni source Player n’a été modifiée.
