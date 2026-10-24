# PolyRisk — audit UI et motion, 13 octobre 2026

Périmètre : présentation Unity/WebGL uniquement. Les références visuelles citées dans la demande (deux captures et le mockup cible) ne figurent pas dans les pièces jointes accessibles à cette session. Ce relevé s'appuie donc sur les captures de validation du dépôt, les UXML/USS et le template WebGL. La fidélité au mockup reste à comparer directement dès que ces trois images seront accessibles.

## Priorité 0 — netteté WebGL

- **Écart initial identifié puis corrigé :** le template forçait le ratio de pixels à 1, ce qui floutait l'image sur les écrans haute densité. Il plafonne maintenant le ratio mobile à `min(DPR, 2, sqrt(2 400 000 / pixels CSS))` dans `Assets/WebGLTemplates/PolyRiskResponsive/index.html`. Cela augmente la définition tout en bornant la taille du framebuffer; à 430 × 932 et DPR 3, le test donne 860 × 1864, soit environ 1,6 million de pixels.
- **Vérification faite :** l'émulation Chromium accélérée par Metal a contrôlé le canvas en portrait 430 × 932 et paysage 932 × 430 à DPR 3, l'accueil et l'entrée dans une partie locale, sans erreur JavaScript (`Docs/Validation/Mobile_WebGL_2026-10-15/README.md`). Le contrôle confirme la règle CSS/WebGL sur ce banc, pas la netteté perçue ni les performances sur Safari/iPhone réel.
- **À mesurer sur appareil :** netteté, cadence soutenue, mémoire et chauffe dans Safari sur iPhone 14 Pro Max, puis sur un appareil Android représentatif. Le ratio plafonné à 2 sous-échantillonne encore un écran DPR 3; ne l'augmenter qu'après un profil réel.
- **Autres facteurs :** `Assets/UI/MainPanelSettings.asset` utilise une résolution de référence 553 × 549 avec `m_Match: 0.265`; les icônes importées sont bilinéaires et compressées. Ils peuvent affecter la finesse d'éléments particuliers, mais le flou global initial venait du ratio 1.

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

### État actuel du champ Risk — 16 octobre 2026

L'inspecteur affiche maintenant la rangée Rent / Risk / ROI. La propriété `PropertyData` ne contient aucun attribut ni calcul de risque par propriété ; le GDD n'attribue des profils chiffrés qu'au Casino. `ActionButtonsWidget` présente donc `N/A` pour une propriété, ce qui reflète les données disponibles au lieu d'inventer un indicateur. Le conserver tant que le modèle de risque immobilier n'est pas défini ; la validation de l'écran peut porter sur l'alignement/contraste du `N/A`, pas sur une valeur métier inexistante.

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

1. ~~Corriger la résolution WebGL~~; mesurer netteté et performances sur appareils réels.
2. Plateau et cadrage/sélection.
3. HUD et card inspecteur.
4. Enchère et marketplace.
5. Motion signature, réglage d'accessibilité, profilage et recette mobile/ordinateur.

Chaque système fait l'objet d'une validation visuelle et d'un commit limité aux fichiers concernés. Les captures cibles manquantes rendent la comparaison pixel à pixel impossible pour l'instant.

## Reprise de l’audit source — 15 octobre 2026

Avant d’ajouter un composant ou une bibliothèque, l’audit du code courant
confirme que le minuteur d’invitation Marketplace décrémente déjà en temps
réel, passe au style rouge à cinq secondes ou moins, et utilise 30 secondes en
ligne. Les rangées d’offre définissent un avatar circulaire (`36 × 36`, rayon
18 px) ; `Marketplace_UIManager` applique la couleur joueur selon son siège.
Le mouvement d’entrée USS existe aussi et s’efface si « Réduire les animations »
est actif. Les preuves runtime antérieures ferment déjà plusieurs contrôles :
V39–V40 ont montré les cinq offres, l’état vide
et les actions visibles à six sièges en portrait et paysage ; V67 a confirmé la
réception d’une nouvelle offre dans le build WebGL, l’ajout de sa ligne et
l’activité du vendeur. Références :
`Docs/Validation/UI_Motion_2026-10-13/README.md` et
`Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.

La preuve visuelle historique du seuil rouge est à refaire : le fichier nommé
`auction_timer_critical_v38.png` montre le formulaire Submit Your Bid, alors
que seule `auction_invitation_v38.png` montre le compte à rebours (22 s,
orange). Le code applique bien la classe critique à ≤5 s, mais le rendu rouge
n’est pas vérifié par les captures actuellement présentes.

Gain/limite : réutiliser ce flux évite un second timer, un Canvas supplémentaire
et une dépendance d’animation. Le reliquat runtime précis est de recapturer le
seuil rouge à ≤5 s et d’isoler la transition entrante de 180 ms, puis de rejouer
cette transition avec « Réduire les animations » actif ; le test V52 avec ce
réglage ne l’isole pas chez le spectateur. Safari/iPhone, profilage et
comparaison au mockup cible restent ouverts. Le flux `AuctionManager.StartAuction` demeure un
parcours gameplay distinct, dont les règles de clôture ne sont pas définies ;
il n’est pas modifié dans la passe UI.

## Invitation Marketplace — superposition confirmée en WebGL, 15 octobre 2026

Le premier essai a montré que le tri du document et un fond plus opaque ne
suffisaient pas à empêcher les badges de rendement d'apparaître sous
« Auction Started ». Le correctif final masque l'overlay unique des rendements
pendant les états modaux Marketplace et le rétablit à leur fermeture. Il
réutilise les documents existants : aucun Canvas, shader, polling ou changement
de logique de partie n'a été ajouté. Le fond de la notification reste sombre et
à 97 % opaque.

Le build Unity 2022.3.62f3 a passé Economy 114/114, Mechanics 130/130 et
`WEBGL PLAYER BUILD PASS`. Dans le Release local, le titre et le vendeur sont
lisibles sans badge superposé à 19 s ; le minuteur est rouge à 3 s ; après
expiration automatique, les rendements réapparaissent sur le plateau. La
recette a utilisé un lobby neuf et une fixture Marketplace, pas une partie
humaine. La préversion publique n'a pas été remplacée. Safari/iPhone, le
profilage et la comparaison au mockup restent ouverts.
