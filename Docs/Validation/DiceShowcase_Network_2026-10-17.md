# Dé central et transport d'événement en ligne — 17 octobre 2026

Périmètre : présentation du jet de dé, sans changement des règles, de l'économie, des données joueur ou du résultat autoritaire.

## Changements validés

La carte centrale affiche le joueur actif, les deux valeurs confirmées par le moteur et le déplacement correspondant. Les faces intermédiaires restent décoratives. Pendant cette séquence, l'inspecteur de tuile entier (Dock d'état, statistiques et boutons, y compris « PLAYER TURN ») est masqué pour dégager le plateau. Son état d'affichage avant le lancer est mémorisé et restauré à la fin; « SKIP », le réglage Réduire les animations et la désactivation du widget utilisent aussi le nettoyage de séquence existant. Aucun objet graphique ou Canvas supplémentaire n'est créé.

## Vérification

- Unity 2022.3.62f3 : Économie **114**, Mécaniques **141**, événements **12**, UI **252** — **519 assertions PASS**.
- Build WebGL Release (`BuildOptions.None`) : **PASS**, artefact isolé `/tmp/PolyRiskDicePanelFixWebGL_20261017` (~12 Mo compressés), non publié.
- Deux clients WebGL Chromium locaux : lobby, ready, démarrage, jet hôte et réception de l'événement sur le client invité. Le snapshot autoritaire contient la séquence 1, acteur 0, dés **2 et 6**, total **8**. Console : **0 erreur**.
- Visuellement, le bottom sheet est dégagé pendant le dé (`DiceShowcase_Network_2026-10-17/guest-dice-1s.png`) et réapparaît après (`guest-panel-restored-1_6s.png`).
- `git diff --check` : PASS.

## Limites et suite

La recette utilise deux navigateurs Chromium desktop en 1280×720, pas Safari ni un iPhone physique. Au retour de l'animation, la jonction Dock d'état/inspecteur manque encore de respiration visuelle; les badges de rendement restent également denses. Ces défauts sont conservés dans la todo UI/UX pour une correction dédiée. Les séquences autres que les dés, le roster multi-joueur dans l'interface, le profilage de performance et la recette réelle iPhone 14 Pro Max restent à faire. Aucun aperçu partagé ni tunnel public n'a été remplacé.
