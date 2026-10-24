# Pouvoir Oil en multijoueur WebGL — 15 octobre 2026

Deux clients WebGL Release indépendants ont rejoint le lobby QA local
`2B4E3T`. La fixture, posée avant le démarrage de l’autorité, attribuait les
trois propriétés Oil & Gas (18, 19 et 21) à l’invité et lui donnait 100 points
d’énergie en phase `Planning`. Il a activé Oil depuis le sélecteur d’ultime.

Le serveur a accepté `ultimate.use` (séquence 1), publié la version 2 et
enregistré « Boom énergétique ». L’énergie passe de 100 à 0, le booléen de
boost de loyers vaut `oilGasBoostTurnsRemaining = 1`, et les soldes restent à
18 000 $. Les trois propriétés restent à l’invité. La régression Unity
`MechanicsRegression` vérifie le calcul correspondant : loyer de famille à
432 $ après le pouvoir (multiplicateur complet ×4,32).

L’état est répliqué aux deux clients, sans perte de connexion. La bannière
temporaire n’a pas été capturée. Serveur et stockage QA étaient isolés sur
loopback ; aucune règle, donnée économique ni source Player n’a été modifiée.
