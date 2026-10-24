# Audit ciblé — PASS 2C

`SampleScene.unity` utilise `PolyRiskBoard` et ses quatre meshes fusionnés / 40 ancres, `BoardManager`, `PlayerVisualManager`, `PawnMovement`, `TurnManager`, `ActionButtonsWidget` et la caméra principale orthographique. Le défaut 40° est appliqué par `BoardInteractionManager`. Il reste le seul composant de cadrage.

Le chemin normal est `TurnManager.ExecuteMovement` → phase `MovingPawn` → abonnement préalable à `OnPawnMovementFinished` → `OnPlayerPathCalculated` → `PlayerVisualManager.HandlePlayerPath` → `PawnMovement.MoveAlongPath` → callback existant → résolution du mouvement et de l’économie. Ce callback n’est ni remplacé ni retardé. La caméra écoute une notification émise au même dispatch visuel et le callback global existant ; elle vérifie le pion et sa destination avant de reconnaître une arrivée.

`TeleportPlayer` écrit directement la position logique et visuelle sans passer par ce dispatch de chemin. La reprise d’un mouvement en attente emploie également la téléportation. Le nouveau suivi ne démarre donc pas pour ces cas. `OnlineGameController.ApplyState` signale une application réussie à la présentation ; cette notification locale ne change ni DTO, ni protocole, ni logique du snapshot. Elle restaure le cadrage instantanément et annule tout suivi/pause/retour restant.

PASS 2B.2 contenait un bouton Approcher/Vue entière, des données d’inspection manuelle et un fit dépendant du panneau. Le bouton, les styles et cette logique sont retirés. L’inspection conserve exactement son routage de raycast et ses permissions.

Fichiers d’intégration : `BoardInteractionManager.cs`, `PlayerVisualManager.cs` (notification avant le mouvement, callback original intact), `ActionButtonsWidget.cs` (repli au dispatch visuel et retrait des anciens replis au clic/début de tour), `OnlineGameController.cs` (notification locale après succès), `TopHUD.uxml`, `Theme.uss`. `PawnMovement.cs`, `TurnManager.cs`, `RulesEngine.cs`, les meshes, les ancres, les matériaux et la scène ne sont pas édités. Les scripts de validation sont adaptés séparément.

Le cockpit reçoit la notification de mouvement indépendamment de la caméra. S’il est déployé, une seule réduction immédiate est appliquée ; sinon le handler ne fait rien. Les ouvertures/fermetures manuelles gardent leur animation existante. Aucune hauteur UI ne modifie les propriétés caméra : le fit réserve toujours la géométrie du cockpit compact de référence. Un panneau déployé peut donc recouvrir la périphérie du board, conformément à la nouvelle priorité de stabilité.

Risques examinés : callback synchrone du mode animation ignorée (exclu du suivi), événement global de fin sans identifiant (garde destination/pion), retour prématuré à une nouvelle phase sans arrivée (annulation), remplacement/restauration pendant un mouvement (retour instantané, fin tardive ignorée), sauts verticaux du pion (suivi sur le plan de parcours), nouveau tour avant la fin de pause (annulation plutôt que travelling retardé). Aucun délai caméra n’est injecté dans le moteur de jeu.

Les scénarios déterministes emploient une copie visuelle du pion existant et les vrais waypoints / le vrai PawnMovement ; ils ne prétendent pas être des jets de dés réels. Un clip de partie locale complète séparément ce banc d’essai. Aucun test multi-client ni téléphone physique n’est implicitement couvert par ces fixtures.

Correction après stress desktop : la protection utilise désormais une enveloppe projetée des renderers du pion, mise en cache au départ, plus une réserve de saut fixe. Le mouvement caméra reste lié au plan de parcours. Le build final est identifié dans build-sha256.json.
