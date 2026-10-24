# Roster joueur compact — 17 octobre 2026

## Livré

Le HUD de partie affiche désormais une puce par joueur. Chaque puce reprend la couleur du pion, une initiale, un nom raccourci, le niveau de richesse `LOW / MID / HIGH / MOGUL` et l’état `OUT` si le joueur est éliminé. Le joueur actif reçoit un contour or. Les montants exacts des autres joueurs restent masqués.

Le composant se met à jour aux changements de tour, de solde et aux événements typés. Il n’utilise ni boucle par frame ni nouveau package. Le roster reste masqué tant qu’aucune partie avec au moins deux joueurs n’est active. Les noms locaux sans nom de lobby deviennent `P1`, `P2`, etc. afin de rester distincts sur petit écran. La palette des pions est partagée avec le HUD pour empêcher une divergence de couleur.

## Validation

- Unity 2022.3.62f3 : économie 114/114, mécaniques 141/141, événements 12/12, UI 256/256 (**523 assertions**).
- Build WebGL Release (`BuildOptions.None`) : succès; payload compressé d’environ **12 Mo**.
- Aperçu local chargé dans le navigateur à 315 × 707; une partie locale montre quatre joueurs, leurs niveaux `MID` et le contour du joueur actif.
- `git diff --check` : PASS.

La capture du navigateur confirme le layout pour quatre sièges en petit viewport. Six sièges, Safari/iPhone physique, performance/mémoire sur appareil et test en ligne multi-clients restent à valider. La préversion publique n’a pas été modifiée.

## Fichiers

- `Assets/UI/Layouts/TopHUD.uxml`
- `Assets/UI/Styles/Theme.uss`
- `Assets/Scripts/UI/TopHUDWidget.cs`
- `Assets/Scripts/Visuals/PlayerVisualManager.cs`
- `Assets/Scripts/Editor/UIAssetRegression.cs`
