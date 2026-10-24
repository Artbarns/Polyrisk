# Plateau et feedback du tour — passe UX, 16 octobre 2026

## Plan et audit avant modification

| Retour des joueurs | Constat dans la source Unity | Action de présentation |
| --- | --- | --- |
| On ne comprend pas assez le lancer | `DiceSystem.CommitRoll` publie déjà le résultat exact dans le feed, mais il est rendu comme une ligne de texte uniforme. | Rendre les deux valeurs comme des faces de dé et le total comme nombre de cases, avec une entrée animée brève. Garder le résultat autoritaire et le timing du tour inchangés. |
| Les pions se voient mal | Le prefab utilise une sphère Unity à l’échelle 0,5 et une palette saturée ; aucune distinction de joueur actif n’est synchronisée avec `OnTurnStarted`. | Agrandir modérément le mesh, éclaircir le pion actif et lui donner une légère émission ; maintenir la taille monde du collider pour ne pas changer la cible tactile. |
| Les rendements sont laids/flottants | 40 chips écran de 16–38 px mêlent icône et pourcentage, avec une taille typographique pouvant tomber à 6,5 px et un décalage vertical de projection. | Recentrer les badges près de la surface, garder icône + rendement en ligne, et relever leurs tailles minimales pour mobile. |
| La lumière paraît peu immersive | Bloom du profil à 1,8 / seuil 0,4, contraste 18, saturation 12, vignette 0,32 ; la sonde de réflexion scène est recalculée à chaque frame. | Adoucir bloom et vignette, réduire la saturation, neutraliser la profondeur de champ et ne capturer la sonde qu’au chargement. Rester dans URP, sans package ni post-effet nouveau. |
| Suivi caméra proposé | La caméra montre le plateau entier ; son centrage sur un pion de bord masquerait des cases et déplacerait le contexte tactile. | Ne pas ajouter de suivi caméra à cette passe. Tester d’abord le repère direct du pion actif ; reconsidérer une caméra seulement si le cadre peut rester complet sur les formats ciblés. |

## Comparaison d’efficacité

Réécrire l’UI ou ajouter un système de tween donnerait plus de contrôle, mais toucherait plus d’écrans, ajouterait du poids au build et imposerait une vérification WebGL/iOS d’une dépendance. La voie la plus efficace est d’améliorer le feed et les widgets existants, et d’utiliser les transitions UI Toolkit déjà chargées. Le compromis est une animation de dés courte après le résultat réel, pas une longue simulation physique avant le jet. Aucun événement de jeu, règle, donnée économique ou commande réseau n’est changé.

## Réalisation

- Résultat de dés rendu comme deux faces lisibles et le total du déplacement dans le feed; la préférence de mouvement réduit coupe leurs transitions.
- Pion actif agrandi et légèrement éclairci, les autres gardant une palette joueur lisible. Les dimensions du collider sont compensées pour préserver sa taille monde existante.
- Badges ROI agrandis, en ligne, légèrement abaissés sur le plan de la tuile, sans défilement caméra ni panneau supplémentaire.
- Post-traitement et sonde de réflexion ramenés à une configuration plus sobre, moins floue et moins coûteuse par frame.

## Validation effectuée

- Unity 2022.3.62f3 : Economy 114/114, Mechanics 141/141, UI 246/246 sur 17 écrans/composants.
- Build WebGL Release `BuildOptions.None` terminé avec `WEBGL PLAYER BUILD PASS`; sortie isolée `/tmp/PolyRiskVisualUX_20261016` (12 MiB).
- Contrôle visuel WebGL local en portrait (viewport du navigateur 332×736) : menu local, plateau, HUD, badge yield, jet réel (`4 + 1`, `5 STEPS`), déplacement et bottom sheet de propriété. Le résultat de dé reste visible quand l’arrivée sur la case le suit immédiatement.
- Console du navigateur : la page locale a affiché `Unknown Error` pour le réseau lobby, attendu car ce preview n’était relié à aucun serveur de partie ; la partie locale, le plateau et les interactions testées ont fonctionné.
- Le mouvement réduit a une transition d’entrée à 0 ms via la règle `.ux-reduced-motion .activity-die`; les tests structurels et le build compilent cette règle.

Limites : le screenshot ne capture pas la frame intermédiaire de l’animation; le rendu de la transition est vérifié par son état final et sa configuration CSS. Pas de mesure Profiler avant/après ni d’essai sur iPhone physique cette session. Les retours d’Arthur restent requis avant de déclarer le polish complet.
