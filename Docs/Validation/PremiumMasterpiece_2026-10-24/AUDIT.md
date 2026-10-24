# Premium Graphite — audit ciblé, 24 octobre 2026

Référence exacte : commit9242725, rendu ReleaseFinalExperience avec caméra permanente1,40. Pas de remise à zéro des passes précédentes. Baseline sources639fichiers et originaux enregistrés avant intervention.

## Causes observées et vérifiées

- Les faces supérieures sont planes. La contribution de studio précédente utilise des panneaux étroits hors de la direction réfléchie au voisinage deGO ; augmenter seulement la smoothness ne change pas leur profil analytique. L'ancien lobe « Polish » n'utilise pas la smoothness pour sa largeur.
- La normale virtuelle ancienne occupe moins d'un pixel ; la définition des arêtes perd beaucoup à430×830 DPR1. La couverture1,2pixel framebuffer devient0,6pixelCSS àDPR2.
- Les cases0/10/20/30 sont des inlays dans Board_Dark. Ce matériau ne recevait pas la texture de contact et son masque TileSurface était0. Le contour néon réagissait, la surface ne recevait pas la réflexion de contact.
- Les corps de monolithes utilisaient URP/Lit sans contribution studio, contrairement au plateau. Aucune reconstruction géométrique nécessaire pour les relier matériellement.
- URP14 gère déjà correctement la direction de vue orthographique : aucun correctif de caméra requis. Le problème est l'architecture des lobes et des receveurs, pas un supposé bug de perspective.

## Intervention minimale choisie

Conserver les quatre meshes et toutes leurs données, dériver les indices de coins dans le shader sur le seul mesh de leurs inlays, lier la même texture contact au matériau dark. Nouveau mode architectural dans le shader partagé, branche baseline précédente conservée. Panneaux neutres larges, lobe de monolithe angulaire, variation de surface liée aux normales et chanfrein virtuel borné/couvert par dérivées. Pas de texture de bruit, de lumière supplémentaire, de probe temps réel, de nouvelle passe ou de nouveau mot-clé.

Éclairage : une Directional, remplissage Trilight, exposition/colorimétrie existantes ; bloom final désactivé après comparaison. Les valeurs sont des points de départ à comparer, pas une preuve artistique.

## Calibration

Outil DEV/Editor dans la vraie scène, cinq couches : neutre, architecture, sources, réponse locale, finition optique. Neuf types d'échantillons et coins supplémentaires, sans modifier les propriétaires, capitaux, règles ou logique réseau. Les pics figés sont explicitement des spécimens ; ils ne prouvent pas la qualité temporelle. Les trajectoires courtes/longues/coin utilisent PawnMovement et GetPath, avec callback QA distinct puis restauration visuelle ; elles ne constituent pas des lancers naturels.

Même zoom, caméra, résolution, état et couleurs entre A/B. Les gros plans restent complémentaires. MesuresWebGL séparées des prises vidéo et sans constructionUnity concurrente.

## Préservé

BoardInteractionManager, PawnMovement, règles, calculs, sources réseau, structure UI et colliders. Épaisseur4,5%, topologie40, angles40/45, zoom1,40, micro-pause250ms, callbacks et ReducedMotion ne sont pas retouchés. Les changements locaux hors périmètre restent présents.

## Limites de départ

Pas d'iPhone physique, de Safari iOS, de tempsGPU isolé ni d'hébergement public autonome accessible. Les six dépôtsGitHub accessibles ne contiennent pas PolyRisk ; le dépôt cible et l'hébergement persistant restent nécessaires. La préparation de release avance indépendamment ; publication et validation artistique ne seront pas confondues.

Correction d'un libellé historique : GraphitePost.asset utilise Tonemapping.mode=1, soit **Neutral** dans URP14, pas ACES. Exposition+.15, contraste6 et saturation−8 sont conservés ; aucune modification d'asset post-processing. Les profils de calibration agissent sur une copie runtime du volume.

## Correction observée pendant la recette

Les identités projetées se limitaient à l'écran entier. Avec le cadrage proche en case11, des marqueurs des cases lointaines se dessinaient sur les montants du HUD. Correction limitée à BoardTileOverlayWidget : conserver les40 éléments, afficher seulement ceux dont le rectangle complet appartient à BoardViewport, et placer leur couche derrière les commandes du même document. Le cockpit déployé les occulte naturellement, sans piloter la caméra. Même correction pour A/B ; données des propriétés et raycasts inchangés. Les captures antérieures à ce correctif sont archivées comme diagnostic, pas utilisées pour représenter l'UI finale.

## Deuxième itération Reflective Neon

La première réception sur les coins cumulait les deux rainures réelles, une ligne analytique et un reflet entre les deux ; le pic figé formait un cadre rose trop épais. La nouvelle branche supprime cette ligne supplémentaire sur les coins et reçoit la lumière vers l'intérieur depuis la rainure. Les cases normales ne gardent qu'une aide de couverture réduite et une réponse satinée bornée. Les pics HDR de source sont comprimés dans le shader, sans modifier la couleur de catégorie ou de propriétaire. La sélection seule reçoit une aide analytique de couverture, jamais tous les contours au repos.
