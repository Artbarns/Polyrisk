# PolyRisk — audit UI et motion, 13 octobre 2026

Périmètre : présentation Unity/WebGL uniquement. Les références visuelles citées dans la demande (deux captures et le mockup cible) ne figurent pas dans les pièces jointes accessibles à cette session. Ce relevé s'appuie donc sur les captures de validation du dépôt, les UXML/USS et le template WebGL. La fidélité au mockup reste à comparer directement dès que ces trois images seront accessibles.

## Priorité 0 — netteté WebGL

- **Constat certain :** le template `Assets/WebGLTemplates/PolyRiskResponsive/index.html` force `config.devicePixelRatio = 1` sur iPhone, iPad et Android. Unity synchronise par défaut le buffer WebGL avec la taille CSS du canvas multipliée par ce ratio. Sur un appareil haute densité, le buffer est donc agrandi à l'affichage, ce qui floute toute la scène, y compris le texte.
- **Autres facteurs à contrôler :** `Assets/UI/MainPanelSettings.asset` utilise une résolution de référence 553 × 549 avec `m_Match: 0.265`; les icônes importées sont bilinéaires et compressées. Ces réglages peuvent affecter la taille ou la finesse d'éléments particuliers, mais ne justifient pas à eux seuls le flou global.
- **Critère de sortie :** contrôler dans le navigateur la taille CSS du canvas, ses dimensions de rendu et le ratio retenu; comparer la netteté et le temps de rendu avant/après sur téléphone et ordinateur. Aucun embellissement du board avant cette vérification.

## Plateau

- Le plateau est produit par `BoardGenerator`/`PolyRiskBoard`; l'audit de sa représentation réelle reste à terminer. Les captures de validation montrent des cases surtout typographiques, loin de l'icône sectorielle, du rendement et du code couleur de famille demandés.
- Distinguer visuellement la case d'arrivée et les seules cases sélectionnables. Vérifier les différentes phases avant de retirer tout halo permanent. Ajouter de la profondeur légère aux pions/bâtiments sans bloom lourd.
- Le mode portrait a déjà révélé une case sélectionnable hors cadre dans la capture V18; le cadrage correctif V19 doit être validé dans le navigateur.

## HUD

- `TopHUD.uxml` dispose déjà des quatre groupes Capital / Net Worth / Turn / Market, avec séparateurs et couleurs. La mise en page, le contraste, le corps des chiffres et les espacements restent à confronter au mockup aux largeurs mobiles.
- Les raccourcis et overlays de débogage ont des gardes de build, mais il faut vérifier le rendu release effectif; aucun libellé F1–F9 ou stress test ne doit apparaître.

## Card inspecteur

- `TileDetail_BottomSheet.uxml` dispose déjà d'un emplacement d'icône, d'un libellé famille/niveau, de trois statistiques et de classes primaire/secondaire. L'implémentation affiche Rent / Level / Rent Yield, alors que la cible demande Rent / Risk / ROI : toute donnée de risque absente doit être identifiée dans les données existantes avant affichage, sans inventer de chiffre ni modifier les règles.
- Renforcer la hiérarchie de boutons et le badge famille; vérifier que les stats et les actions restent lisibles quand le panneau se réduit pour sélectionner une case.

## Enchère et marketplace

- `AuctionItem_Component.uxml` contient déjà une ligne avec avatar, nom et offre, mais les données, la disposition, l'état vide, le minuteur et l'animation d'arrivée doivent être validés dans l'écran de jeu réel. La capture de recette V14 montre le formulaire mobile; elle ne prouve pas une liste d'offres complète à six joueurs.
- Les panneaux d'invitation, formulaire, attente et revue sont distincts. Leur cohérence de typographie, couleurs, états vides et transitions doit être traitée écran par écran.
- **Audit ciblé V38 :** `Marketplace_Notification.uxml` avait un compte à rebours orange fixe, sans état critique ; `AuctionItem_Component.uxml` avait un avatar carré de couleur de secours, tandis que les six couleurs de joueur existent déjà dans `Marketplace_UIManager`. La ligne d'offre se crée pour les vues revue/spectateur ; l'attente vendeur actualise seulement le nombre d'offres. Réutiliser ces composants et la palette existante évite un nouveau Canvas et une bibliothèque d'animation (gain de maintenance et de chargement) ; la limite est que les offres restent masquées jusqu'à la revue, ce qui respecte le comportement actuel. Une animation d'entrée en temps réel ne peut être affirmée tant que la liste n'est pas alimentée pendant l'attente.
- **Écart distinct à lever avant la fin :** `AuctionManager` expose `OnAuctionStarted`, `OnNewHighestBid` et `OnAuctionEnded`, mais aucun contrôleur UI ne s'y abonne actuellement. L'écran de revue Marketplace n'est donc pas une preuve de l'enchère déclenchée par la case/propriété via `AuctionManager.StartAuction`. Confirmer d'abord le parcours prévu et son état en WebGL ; sa réalisation dépasse la simple passe visuelle si la logique de tour/enchère doit évoluer.
- **Recette V39 :** une vente à six sièges peut afficher cinq offres. À 430 × 900, les cinq lignes se parcourent par glissement de la barre ; à 900 × 430, la liste déborde et les boutons du bas sont coupés. Le panneau de revue doit donner la hauteur restante à la liste défilante et réserver la zone d'action. Correctif ciblé V40, à valider en paysage avant diffusion.

## Motion et accessibilité

- Vérifier l'existant avant d'ajouter des dépendances : glissière d'achat, surbrillance de sélection, trajet des pions, compteurs d'argent. Ajouter un réglage « Réduire les animations » qui désactive les déplacements/ressorts décoratifs tout en conservant le retour d'état nécessaire.
- Toute nouvelle animation doit être évaluée sur WebGL mobile : coût de rendu, taille de build, lisibilité tactile et navigation clavier. Les bibliothèques tierces restent facultatives; la priorité est une implémentation légère et compatible Unity 2022.3.
- **Audit ciblé après V40 :** `PawnMovement` anime déjà chaque segment avec un arc et le supprime en mode « Réduire les animations ». `TopHUDWidget` écrit directement CAPITAL et NET WORTH à chaque événement sans interpolation ; un animateur commun, limité à environ 20 mises à jour/s et interrompu si la cible change, évite deux coroutines ou une bibliothèque supplémentaire. Avantage : peu de code et aucun ajout au build ; limite : l'effet reste sobre et doit être mesuré dans le HUD WebGL. `ActionButtonsWidget` déclenche encore l'achat depuis un bouton, sans glissière ; cette interaction exige une validation tactile et clavier séparée avant remplacement.

## Ordre de réalisation

1. Corriger et mesurer la résolution WebGL.
2. Plateau et cadrage/sélection.
3. HUD et card inspecteur.
4. Enchère et marketplace.
5. Motion signature, réglage d'accessibilité, profilage et recette mobile/ordinateur.

Chaque système fait l'objet d'une validation visuelle et d'un commit limité aux fichiers concernés. Les captures cibles manquantes rendent la comparaison pixel à pixel impossible pour l'instant.
