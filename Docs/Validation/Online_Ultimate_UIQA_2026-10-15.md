# Recette UI en ligne des ultimes — 15 octobre 2026

## Essai initial non concluant

## Essai

Deux clients WebGL Release ont rejoint le lobby QA local `MSUD74`. L’hôte a
démarré la partie ; le client invité a affiché son sélecteur de famille ultime
et a soumis Pharma depuis l’interface. Le reçu serveur porte le type
`ultimate.use`, séquence 1.

## Pourquoi ce n’est pas un PASS

La fixture de test a été installée après le démarrage, puis l’hôte a été
rechargé pour la reprendre. Le snapshot final (version 6, séquence acquittée 1)
montre pourtant le joueur invité en `ResolveTile`, sur la case 5 après un lancer
1 + 4 ; son énergie est toujours à 100 et l’immunité Pharma n’est pas active.
L’état du serveur ne correspond donc pas à l’effet demandé. Comme la fixture
modifiait le serveur pendant une partie déjà initialisée et que l’hôte a été
rechargé en cours d’essai, ce résultat ne permet pas d’attribuer un défaut au
jeu ni de valider l’ultime.

Cet essai est remplacé pour Pharma par la reprise déterministe ci-dessous ; il
reste conservé comme historique du premier essai.

## Reprise déterministe — Pharma depuis l’interface

La fixture a cette fois été chargée dans le stockage isolé avant que l’hôte
initialise l’autorité. Deux clients WebGL Release ont rejoint la partie. Depuis
le sélecteur affiché par l’hôte, Pharma a été activé alors que le joueur avait
les trois propriétés de famille et 100 points d’énergie.

Le snapshot autoritaire suivant (version 2, séquence 1) confirme `Planning`,
l’hôte toujours à sa position de départ, énergie 0, `hasImmunityCard = true`,
`immunityTurnsRemaining = 1` et les deux soldes inchangés à 18 000 $. L’invité a
vu la bannière « QA Seed Host active Protocole sanitaire ». La commande reçue
était `ultimate.use`. Le parcours interface → commande → autorité → réplication
visuelle est donc PASS pour Pharma.

Lors d’une capture ultérieure après rechargement de l’invité, le snapshot est
passé à la version 3 sans nouvelle séquence de commande : l’hôte avait lancé
4 + 4 et se trouvait sur la case 8, toujours avec son immunité. Cela ne prouve
pas que le rechargement a déclenché le lancer. Le code de `TurnManager` contient
un filet de sécurité qui quitte `Planning` après 60 secondes sans décision ; ce
comportement est cohérent avec l’avancement observé, mais le délai exact de ce
parcours n’a pas été chronométré. À vérifier séparément pour s’assurer que ce
filet ne surprend pas les joueurs pendant un tour en ligne.

## Suite

Recetter les six autres pouvoirs en ligne, les refus de cibles invalides ou
immunisées, et la reconnexion sous d’autres effets temporaires. L’essai a utilisé
un serveur isolé et des données temporaires ; aucun code Player ni règle de jeu
n’a été modifié.
