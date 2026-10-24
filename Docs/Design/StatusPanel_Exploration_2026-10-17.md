# Panneau d'état — premières directions

17 octobre 2026. Exploration demandée par Arthur ; aucune direction retenue à ce stade et aucune modification runtime.

## Diagnostic observé dans les sources

`Assets/UI/Styles/Theme.uss`, sélecteurs `.game-state-dock*` : le dock impose une ligne principale de 42 px, une copie de 36 px, puis deux labels de 16 px. Le titre et le message sont à 10 px ; achats et transferts descendent à 9 px. Les chaînes sont tronquées avec ellipsis. Cela pénalise les informations riches (acteur, propriété, montant, bénéficiaires).

`Assets/Scripts/UI/ActionButtonsWidget.cs`, `UpdateGameStateDock` : le titre suit le joueur et l'état actifs ; le message peut provenir du dernier événement. Des faits et phases de moments différents peuvent ainsi être juxtaposés. Ce n'est pas la preuve d'un bug runtime, mais une source de confusion à traiter dans la conception.

`GameEventTextFormatter`, `GameEvent`, `GameEventPresenter` fournissent déjà des acteurs, montants confirmés, tuiles, destinataires / parts et une file de présentation. Conserver cette source de vérité et éviter une deuxième narration concurrente des dés. Ne pas inférer une destination définitive avant les choix Turbo / Précision, ni afficher un achat comme acquis avant confirmation.

## Référence vérifiée

Board Game Arena, [Guidelines UX](https://en.doc.boardgamearena.com/images/5/57/Guidelines_UX_new_compressed.pdf), sections D.2 / D.3 : préciser l'acteur et ce qui change, conserver un récit dans des entrées structurées, regrouper les petits événements, utiliser l'animation pour expliquer sans ralentir le jeu. Document public vérifié ; ce sont des recommandations de conception, pas des assets ou du code à copier. Aucune licence de réutilisation des visuels n'a été présumée.

[boardgame.io](https://boardgame.io/) a également été consulté comme référence de moteur pour jeux au tour par tour ; pas de migration recommandée ni de modèle visuel repris sans vérification.

## Pistes

1. Carte de tour persistante : acteur / phase distincts d'un reçu structuré, propriété lisible, montant et contexte du déplacement. Recommandation initiale pour mobile.
2. Fil latéral : étapes du tour et événement courant, plateau à gauche. En portrait, fil placé sous le plateau. Plus d'historique immédiat, plus coûteux en place.
3. Signal sur le plateau : annonce typographique courte au-dessus de la zone de jeu, information financière mise en avant ; rappel consultable dans l'historique. Plus immersif, à contrôler face aux animations du plateau.
4. Effet qui me concerne : hiérarchie renforcée pour « Vous recevez +315 $ », tout en gardant total payé et autres bénéficiaires.
5. Trace durable après le beat : l'animation finit, l'information reste disponible. Ne pas remplacer la lecture par un toast éphémère.
6. Rapprochement narratif du tour : résultat → destination → action confirmée ; mettre à jour la même surface plutôt que faire surgir plusieurs bandeaux.

## Aperçu

Fragment interactif de la conversation :
`/Users/arthurbarreau/.codex/visualizations/2026/10/17/01a1498f-6c87-7e31-b929-4d90b504495c/panneau-etat-directions.html`

Trois variantes, données fictives explicitement marquées. Séquence d'exemple : Ava fait 8, se déplace, achète French Riviera ; Noah fait 5, arrive à Silicon Valley puis règle un loyer de 420 $, réparti entre vous (315 $) et Ava (105 $). Ces chiffres illustrent le rendu ; ils ne prétendent pas refléter le prix ou les règles de ces propriétés.

Transitions initiales proposées : changement de contenu sur 220 ms, déplacement du pion schématique sur 550 ms, confirmation de montant sur 240 ms. Aucun mouvement continu ou tremblement ; préférences de mouvement réduit respectées. Les commandes de simulation et les acteurs fictifs ne font pas partie de la future UI publiée.

## Points à valider avant handoff d'intégration

- Choix de la configuration puis raffinement dans le contexte réel du plateau et des actions locales.
- Plusieurs adversaires, noms longs, montants élevés, loyers répartis et événements rapprochés.
- Conserver distinction entre action en cours, dernier reçu confirmé et historique.
- Pas de masquage des dés, du pion, des décisions locales ou des commandes de jeu.
- UI Toolkit / Unity 2022.3, aucune dépendance nouvelle nécessaire pour les versions carte / fil.
- Annulation, skip, reconnexion, mode réduit, portrait / paysage et mesures WebGL à vérifier lors de l'intégration.
