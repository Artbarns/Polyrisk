# Pouvoir Automobile en multijoueur WebGL — 15 octobre 2026

Deux clients WebGL Release indépendants ont rejoint le lobby QA local
`WMZ6CU`. La fixture attribuait à l’invité les trois propriétés Automobile
(13, 14 et 16), 100 points d’énergie et le premier tour en `Planning`. Il a
activé Automobile depuis le sélecteur d’ultime.

Le reçu serveur confirme `ultimate.use` en séquence 1 ; le snapshot version 2
confirme énergie 0 et `autoTurboReady = true`. La bannière « active Moteur
turbo » est visible chez l’invité. Depuis son interface, le joueur a ensuite
lancé les dés : le client lui a proposé deux jets (`6 + 4 = 10` et `2 + 5 = 7`)
avec les cases d’arrivée, puis il a retenu le jet 10. Le serveur a reçu
`turn.roll` et `dice.turbo` (séquences 2 et 3), a appliqué le mouvement jusqu’à
la case 10 (Audit fiscal) et a remis `autoTurboReady` à `false`. Le snapshot
version 5 confirme la fin du pouvoir et le passage du tour à l’hôte. Les deux
clients sont restés connectés ; l’hôte affiche le nouvel état d’attente.

Ce parcours vérifie le pouvoir en ligne et son enchaînement jouable complet,
pas seulement le drapeau de préparation. Test conduit avec un stockage et un
serveur isolés ; aucune règle, donnée économique ni source Player n’a été
modifiée.
