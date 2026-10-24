# PASS 2D — audit ciblé avant modification

SampleScene conserve PolyRiskBoard, quatre meshes fusionnés, les 40 ancres / colliders, PlayerVisualManager, TurnManager et la caméra orthographique à 40°/45° pilotée uniquement par BoardInteractionManager. PASS 2C cadre encore les meshes complets, socle compris ; son accompagnement attend essentiellement une sortie de la zone de confort.

Les dés visibles actuels ne sont pas des objets physiques : TopHUDWidget reçoit un GameEvent DiceRolled confirmé via le GameEventPresenter chronologique du cockpit, puis anime deux PulseDiceView en Painter2D. DiceSystem.CommitRoll publie les valeurs réelles synchroniquement avant ExecuteMovement. L'événement de présentation n'a aucune responsabilité sur le résultat. Le grand total / grosses faces UI persistent environ 1,9 s, alors que PlayerVisualManager dispatch immédiatement le chemin ; d'où l'occlusion du départ.

Modifications minimales prévues : BoardInteractionManager (fit des cases et enveloppe des pions, anticipation continue de chemin) ; TopHUDWidget (délégation du seul rendu des dés) ; nouveau BoardDicePresenter (petits meshes temporaires sans colliders, matériaux propres aux dés, réception du GameEvent réel) ; PlayerVisualManager (courte préparation visuelle des dés avant le dispatch original, callback inchangé). Les scripts QA sont adaptés séparément. TurnManager, DiceSystem, RulesEngine, réseau, PawnMovement, géométrie / matériaux / shaders du board, UI et permissions restent intacts.

Le lead-in des dés appartient au séquenceur visuel : 0,9 s maximum nominal, inclus dans le temps visuel déjà attendu par TurnManager. Aucun délai après le callback, aucun résultat recalculé, aucun nouvel appel de gameplay. Le timeout préexistant de 10 s est une dépendance à vérifier ; la préparation est exclue du mode SkipMovementAnimation et bornée. Téléportations et restauration invalident les travaux de présentation en attente. Reduced Motion montre directement le résultat, sans rotation ni caméra animée.

Risques : lisibilité réelle des pips à 430 px, cadrage des pions sur les coins, découpe du socle autorisée mais jamais des cases, anticipation aux changements de côté, durée de résultat avant disparition, restauration pendant la préparation et ordre des événements en présence d'un reçu précédent. Le banc visuel de 44 cases reste distinct d'un mouvement normal de partie.

Ordre : 1. fit cases/pions ; 2. dés intégrés ; 3. anticipation ; 4. compilation / régressions ; 5. exécution WebGL, vidéos portrait et desktop, comparatifs ; 6. arrêt pour validation artistique.

Correction après le banc desktop : l’enveloppe des renderers est portée à l’échelle maximale existante du pion actif (0,91), même si le premier fit précède l’activation du joueur. Aucun changement de silhouette ou de scale effectif du pion ; seul le fit anticipe ce maximum.
