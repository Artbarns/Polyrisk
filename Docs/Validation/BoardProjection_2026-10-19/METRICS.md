# Mesures utilisées

Les mesures finales viennent de `MobileBoardValidationProbe` dans le build WebGL exécuté, puis du journal console `BOARD_QA`. Elles sont en pixels de rendu ; le navigateur et le canvas sont vérifiés à 430 × 830, DPR 1 pour cette comparaison. Leurs équivalents CSS sont donc identiques.

- **Rectangle du plateau** : min/max des sommets réels des quatre meshes fusionnés, transformés puis projetés par `Camera.WorldToScreenPoint`. Inclut le socle et les contours ; exclut les pions et la UI. Ce n’est pas une estimation à partir de l’image.
- **Hauteur apparente** : hauteur de ce rectangle. Le rapport H/L tient compte des faibles reliefs réels ; la relation `sin(élévation)` explique le plan horizontal idéal.
- **Petit côté projeté minimal** : minimum, sur les 40 empreintes XZ des colliders, des deux longueurs d’arêtes projetées. Les colliders correspondent aux dimensions géométriques des cases.
- **Épaisseur apparente minimale** : minimum des hauteurs perpendiculaires du parallélogramme de chaque empreinte projetée, soit `aire / longueur de la plus grande arête`. C’est une mesure plus pertinente de la minceur de la cible qu’une boîte englobante alignée à l’écran.
- **Aire minimale** : minimum des aires projetées des 40 empreintes. Les arrondis visuels sont négligés dans cette mesure ; le contrat de sélection est rectangulaire et reste inchangé.
- **Visibilité** : 40 centres projetés dans la zone libre et 40 colliders dont les huit coins y sont intégralement contenus. La zone libre est mesurée depuis les `worldBound` effectifs du HUD/cockpit et leurs safe areas, après les marges.
- **État identique** : les trois captures principales doivent conserver quatre joueurs au GO, 18 000 $ chacun, aucune propriété détenue, même sélection initiale, cockpit compact. Les captures d’inspection sont des preuves séparées et ne remplacent pas la comparaison contrôlée.

Les glyphes d’identité restent à 10 px CSS dans cette passe. Les mesures géométriques ne prouvent pas une lecture confortable ou un taux de réussite au doigt ; ces points demandent une validation artistique puis un essai tactile sur téléphone réel.
