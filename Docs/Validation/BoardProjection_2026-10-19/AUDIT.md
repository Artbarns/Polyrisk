# PASS 2B.1 — audit de projection

La scène exécutée reste `SampleScene` → `PolyRiskBoard` / `BoardManager`, avec `BoardInteractionManager` comme unique propriétaire de la caméra orthographique. Les quatre meshes fusionnés, les 40 ancres et les colliders restent inchangés. La sélection traverse le même filtre UI puis le même raycast et les mêmes permissions. L’inspection reste dans `ActionButtonsWidget`.

## Cause géométrique

La rotation de scène donne une élévation de **28,15999°**, un azimut de **45°**, sans roulis. Pour un carré horizontal vu orthographiquement suivant sa diagonale : `hauteur / largeur = sin(élévation)`. Le rapport vaut 0,472 à l’angle actuel, 0,643 à 40°, 0,788 à 52°. Modifier seulement `orthographicSize` conserve ce rapport : le plateau dépasserait latéralement avant d’occuper davantage la hauteur libre.

La vue A conserve exactement la rotation de scène. B et C sont uniquement des aperçus de développement, sélectionnés une fois au chargement via `?boardView=B` / `?boardView=C`. Aucun angle nouveau n’est enregistré dans la scène. Sans paramètre, A reste actif. Aucun mouvement de rotation automatique et aucun second contrôleur caméra.

Le cadrage utilise désormais l’enveloppe projetée des sommets des quatre meshes, les colliders et la réserve verticale du pion existant à chacune des 40 ancres. Cela retire la réserve sur les coins vides d’une AABB globale, sans réduire les marges ni rogner le plateau.

## Composition verticale

À 430 × 830 sans encoche : HUD 134 px, cockpit compact 236 px. Ils utilisent ensemble 370 px, soit 44,6 % de la hauteur. L’intervalle brut entre les deux mesure 460 px ; les marges de 12 px de chaque côté laissent une zone utile de **406 × 436 px**. Le HUD et le cockpit sont strictement identiques entre A/B/C. Le vide vertical ne vient donc pas principalement de la hauteur du cockpit compact : la projection actuelle le crée.

Le cockpit déployé reste un cran explicite, plafonné à 52 % en portrait. Le fit respecte ses dimensions réelles et restaure la vue compacte à sa fermeture. Les marges, safe areas natives/CSS et l’hôte central des dés/événements sont conservés. Pas de réduction du HUD ou des boutons à l’aveugle pendant cette comparaison.

## Zoom complémentaire proposé, non implémenté

Première étape proposée : commandes tactiles explicites `−`, `+`, `Vue entière`, cibles d’au moins 44 px CSS, hors du trajet et des actions du cockpit. Facteur borné **1,00 à 1,60**, pas 0,15. Le premier agrandissement vise la case précédemment sélectionnée ; chaque déplacement de caméra exige une commande de zoom explicite. Une nouvelle sélection ne recadre pas automatiquement. `Vue entière` restaure immédiatement le fit correspondant au cockpit présent. Un déploiement volontaire du cockpit restaure la vue complète pour éviter une zone inaccessible, puis reste stable pendant la décision.

Pas de glissement ni de pincement dans cette livraison. Ces boutons ne créeraient aucun conflit de tap et utiliseraient le même contrôleur caméra. Un éventuel pincement ultérieur exigerait une arbitrage tap/geste (annulation dès le second contact ou déplacement > 8 px CSS, sélection seulement au relâchement) : le système actuel sélectionne à l’appui. Ce changement d’entrée doit être validé avant implémentation. Le zoom est un complément ; aucune vue complète de 40 cases à 430 px de large ne peut transformer toutes les cases en cibles tactiles de 44 px.

## Informations conservées

Aucun pourcentage permanent ni bascule Analyse. Les identités, le propriétaire, la position active et la sélection restent dans une couche non interactive. Le tap sur une case ouvre les données de l’inspecteur et conserve les permissions existantes. Le rendement reste calculé par sa présentation actuelle dans l’inspecteur ; aucune formule économique n’est déplacée. Le risque individuel n’étant pas modélisé dans le jeu actuel, l’inspecteur conserve son indication « Risque — » plutôt qu’inventer une valeur.
