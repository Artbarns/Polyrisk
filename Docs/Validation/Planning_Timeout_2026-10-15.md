# Filet de sécurité Planning — 15 octobre 2026

## Résultat

Contrôle manuel du build Unity WebGL Release généré depuis le source courant
dans `/tmp/PolyRiskEfficientCheckpoint_20261015`. Le build a été servi sur
loopback par `Server/serve_webgl.py`, avec les en-têtes de compression requis.
Une partie locale a été lancée, puis laissée sans action.

Après 45 secondes, l’écran était toujours en phase Planning avec `ROLL DICE` et
`PASS`. À 65 secondes, le pion avait avancé jusqu’à `Temu Sweatshop Terminal`
et la fiche affichait `BUY ($1K)` / `PASS`. Cela confirme que le filet finit
par sortir de Planning, lancer le déplacement et rendre la décision de tuile.
Le déclenchement est borné entre les deux observations (45–65 s), pas mesuré à
la frame exacte.

## Contrôle en ligne à deux clients

Deux navigateurs WebGL Release indépendants ont rejoint le lobby QA `Y5MPAB`,
passé Ready, puis démarré ensemble. Hôte et invité étaient encore en attente
après 45 secondes d’inactivité. À 65 secondes, l’hôte avait résolu son lancer
automatique sur Venture (tuile 7) et affichait `DRAW CARD` ; l’invité rendait
le même pion sur la tuile 7 et restait en attente du joueur actif.

L’état persistant du serveur confirme version 2, séquence appliquée 0,
`turnState=ResolveTile`, tour commencé 1, joueur actif 0 et positions 7/0.
Le filet a donc déclenché la progression locale de l’hôte et l’invité a reçu
l’état concordant, sans commande joueur injectée (`sequence 0`). L’intervalle
observé reste 45–65 secondes.

## Portée

Tests navigateur sur le build QA isolé, sans achat, publication ni changement
de code/règle. La vérification confirme un cas de timeout en ligne à deux
clients ; elle ne couvre pas tous les choix temporisés, les appareils mobiles
physiques ou les interruptions réseau longues.
