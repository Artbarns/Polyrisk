# Prototype de dés — première passe UI

Date : 16 octobre 2026  
Périmètre : présentation seulement. Aucun changement au résultat du lancer, au déplacement, au tour ou à l'économie.

## Réalisation

`TopHUDWidget` monte une carte de lancer au milieu du plateau dans le document UI Toolkit déjà actif. Elle annonce le joueur, anime deux faces typographiques, puis révèle les valeurs et le total provenant de `GameEventKind.DiceRolled` après validation par `DiceSystem`. Les faces montrées pendant les 465 premières millisecondes sont des transitions décoratives et ne participent jamais au calcul. Le bouton SKIP coupe l'animation et conserve le résultat. `UIUXRuntime.ReducedMotion` saute l'anticipation et présente directement les valeurs exactes.

L'implémentation utilise des éléments UI et des transitions USS : aucune caméra ou RenderTexture dédiée, aucune particule, physique, nouvelle Canvas, bibliothèque ou texture. Elle ne bloque pas la progression du tour.

## Vérification

- Unity 2022.3.62f3, `RunAllEditorRegressions` : économie **114/114**, mécaniques **141/141**, événements **8/8**, UI **252/252** — total **515 assertions**.
- Build WebGL Release `BuildOptions.None` : **PASS**, environ **12 Mo compressés**, sortie isolée `/tmp/PolyRiskDiceShowcaseWebGL_20261016`.
- Chromium local : chargement WebGL, démarrage d'une partie locale, affichage central du lancer AI (2 + 2 = 4 STEPS), progression vers le dock de décision et inspecteur de propriété. Vérification visuelle capturée dans la session de travail.
- `git diff --check` : PASS.

## Limites / suite

Cette validation prouve la build et un parcours local Chromium, pas les performances ou l'aspect sur iPhone physique/Safari. Les faits typés ne sont pas encore transportés par le snapshot multijoueur : cette présentation est donc confirmée localement, mais l'apparition fiable chez tous les clients en ligne reste à implémenter. Le décompte des cases franchies, les doubles avec emphase distincte, le mouvement des pions lié à la séquence et les transferts visuels restent les prochaines sous-étapes. L'aperçu partagé existant n'a pas été remplacé.
