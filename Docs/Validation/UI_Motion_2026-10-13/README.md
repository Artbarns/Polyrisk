# PolyRisk UI visuelle — contrôle V21, 13 octobre 2026

Périmètre : aucune règle ni donnée économique modifiée.

Le motif historique du plateau déterminait la couleur par position. Une comparaison des 26 assets `PropertyData` avec leurs `FamilyData` a trouvé 17 propriétés dont la couleur de la case ne suivait pas la famille. `PolyRiskBoard` lit désormais la famille de chaque propriété pour la texture d'état du shader. Les teintes correspondent à la palette des glyphes UI existante. Les cases au repos utilisent 22 % de l'intensité d'origine ; le highlight de sélection conserve son intensité propre. Le shader, le mesh et le nombre de draw calls restent inchangés.

Build Unity WebGL V21 : Economy 106 + Mechanics 99 assertions PASS, `WEBGL PLAYER BUILD PASS`, sortie 0. Taille du dossier : 12 292 KiB. Le build V20 occupait environ la même taille. Contrôle navigateur local 430×900 : plateau au repos, puis sélection Marketplace avec une seule propriété en surbrillance, choix de Trump Tower et retour au formulaire avec titre et valeur affichés.

Captures : `board_rest_v21.png`, `board_selection_v21.png`. La préversion HTTPS sert l'index V21, identique au build, ainsi que les quatre ressources WebGL répondant 200. Le navigateur local a chargé et exécuté V21 ; le dernier contrôle du chargement public n'a pas abouti parce que le navigateur de recette s'est arrêté. La mesure de fluidité et netteté sur iPhone réel reste ouverte.

Le rendu final des icônes et du pourcentage sur chaque case attend la confrontation avec le mockup cible, indisponible dans cette session. Le chantier reste dans `tasks/todo.md`.

## Accessibilité V24

Le réglage « Réduire les animations » est visible sur l'accueil et dans le lobby, synchronisé entre les deux et conservé après rechargement. Il coupe les transitions déjà centralisées et l'arc des pions ; la durée de déplacement et les événements de fin de mouvement restent identiques. Le contrôle d'état dans le menu a été vérifié sur le build V22 : activation, rechargement, changement depuis le lobby, puis retour à l'accueil. Le style final sombre et l'état coché vert ont été vérifiés sur V24 en navigateur à 430 × 900. Capture : `reduced_motion_home_v24.png`.

V24 : Economy 106 + Mechanics 99 assertions PASS, `WEBGL PLAYER BUILD PASS`, sortie 0. Les animations futures de transaction et de compteurs devront respecter ce réglage ; cette couverture reste ouverte dans `tasks/todo.md`. La fluidité et la netteté sur iPhone 14 Pro Max réel ne sont pas encore mesurées.

Le dossier WebGL V24 occupe 12 292 KiB, comme V21. La préversion HTTPS sert l'index V24 identique au build et les quatre ressources WebGL répondent 200. Le menu public a été chargé dans le navigateur à 430 × 900 après rechargement. En contrôle paysage à 1280 × 720, le bouton « Create Online Game » chevauche la ligne de code/entrée de partie ; le correctif est inscrit dans `tasks/todo.md`.

## Accueil paysage — validation en cours

V25 a remplacé l'accueil par un `ScrollView` limité à la hauteur disponible : à 1280 × 720, la superposition du bouton de création et de la saisie du code avait disparu et les actions restaient visibles. La barre de défilement native apparaissait en blanc et occupait trop de place. V29 garde la zone défilante et applique un style sombre plus étroit. Economy 106 + Mechanics 99 assertions PASS ; `WEBGL PLAYER BUILD PASS`, sortie 0 ; taille 12 292 KiB. Le navigateur de recette s'est arrêté pendant le chargement V29 : l'apparence de la barre, le scroll vers « Connection » et les autres tailles d'écran ne sont pas encore confirmés. V29 n'est pas publiée.

Audit source du HUD debug : `BuildVerifiedWebGLPreview` appelle `BuildWebGLInternal(BuildOptions.None)`. Les trois scripts de raccourcis `DebugMarketplaceSetup`, `DebugUIShowcase` et `StressTestRunner` cachent leur `OnGUI` hors de l'éditeur ; le showcase est en plus limité par `UNITY_EDITOR || DEVELOPMENT_BUILD`. Le menu public V24 observé en navigateur ne montrait aucun overlay. La refonte visuelle du HUD reste ouverte.

V30 reprend pour la barre du menu les sélecteurs `.unity-scroller--vertical`, `#unity-tracker` et `#unity-dragger` déjà utilisés dans le style des enchères. Build WebGL et 205 assertions PASS, sortie 0, dossier 12 296 KiB. Le navigateur a brièvement retrouvé ses onglets, puis s'est arrêté pendant leur fermeture ; aucun contrôle visuel V30 n'a été possible. La préversion publique reste V24.

## Accueil V31 validé

Le navigateur a été récupéré après fermeture des anciens onglets WebGL. V30 a montré l'absence de superposition à 1280 × 720 et 900 × 430 ; sa barre verticale sombre fonctionne. À 1280 × 720, le foldout « Connection » s'ouvre et le champ « Lobby Server » est atteint par glissement. Une barre horizontale parasite apparue en portrait a été désactivée dans V31. V31 a été contrôlé à 430 × 900 (aucune barre horizontale) et 1280 × 720 (boutons séparés, scrollbar verticale conservée). Captures : `menu_landscape_v31.png`, `menu_portrait_v31.png`, `menu_public_portrait_v31.png`.

V31 : Economy 106 + Mechanics 99 assertions PASS, `WEBGL PLAYER BUILD PASS`, sortie 0. Le nouveau tunnel HTTPS `https://discussing-intimate-outdoors-exclusively.trycloudflare.com/?visual-v31` sert l'index V31 à l'octet près et ses quatre ressources WebGL répondent 200. Le navigateur a chargé le menu public à 430 × 900. Ce tunnel de test reste temporaire et dépend du Mac hôte.

## Typographie V35

L'USS applique les polices Inter déjà suivies dans le dépôt au menu et aux quatre segments du HUD. Un essai avec l'asset Outfit SDF a produit un titre incliné, même sans graisse synthétique ; V35 utilise Inter 700 pour le titre, Inter 600 pour les actions/libellés du menu, Inter 500 pour les labels HUD et Inter 700 pour leurs valeurs. La casse, les tailles et les contenus restent inchangés.

V35 : Economy 106 + Mechanics 99 assertions PASS, `WEBGL PLAYER BUILD PASS`, sortie 0. Dossier 12 424 KiB contre 12 292 KiB pour V31 (+132 KiB). Contrôle navigateur à 430 × 900 : accueil sans débordement, HUD en partie avec CAPITAL / NET WORTH / TURN / MARKET sur une ligne. Contrôle du titre en paysage 1280 × 720. Captures : `menu_inter_v35.png`, `hud_inter_v35.png`. Le tunnel HTTPS sert l'index V35 identique au build et ses quatre ressources répondent 200 ; le menu public a chargé en portrait. La comparaison au mockup cible et la recette sur iPhone réel restent ouvertes.

V37 : card inspecteur en portrait 430 × 900. Le badge famille/niveau est conditionné à une propriété réelle, donc absent de la case neutre et visible sur « Opioid Paradise » (`Pharma · Level 0`). L'action Acheter est pleine, Passer est en contour ; les statistiques affichées restent Rent / Level / Rent Yield, conformes aux données disponibles. Capture : `inspector_property_v37.png`. Economy 106 + Mechanics 99 assertions PASS, `WEBGL PLAYER BUILD PASS`, sortie 0. Le rendu Risk / ROI et la comparaison au mockup attendent des données/visuels fiables ; la réduction puis restauration du panneau en marketplace reste à contrôler.
Préversion HTTPS V37 : index et loader identiques au build, données/framework/WASM identiques après décompression HTTP, réponses 200 et accueil chargé en portrait. Le lien temporaire à jour figure dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.

V38 — enchère Marketplace, avant correction de l'état vide : Economy 106 + Mechanics 99 assertions PASS et build WebGL PASS, sortie 0. Dossier 12 428 KiB contre 12 424 KiB en V37. Salon local QA à deux sièges, fixture `InvitingPlayers` : notification lisible en 430 × 900, minuteur orange à 28 s (`auction_invitation_v38.png`). La capture nommée `auction_timer_critical_v38.png` montre en réalité le formulaire Submit Your Bid ; elle ne prouve pas l'état rouge à 2 s. Fixture `Reviewing` : avatar rond coloré pour le joueur, nom et offres propriété/cash alignées, boutons accessibles (`auction_review_v38.png`). Fixture sans offre : message encore brut, d'où la correction USS V39 (`auction_empty_before_v38.png`). Ces fixtures valident le rendu, pas une partie humaine complète ni la réception d'offres en direct.

V39 : Economy 106 + Mechanics 99 assertions PASS, build WebGL PASS, sortie 0. Dossier 12 424 KiB. Le message sans offre devient une carte sombre lisible (`auction_empty_v39.png`). Dans un salon QA de six sièges, le vendeur voit cinq lignes d'offres avec avatar et montant ; la barre permet d'accéder à toutes en portrait 430 × 900 (`auction_review_six_v39.png`). En paysage 900 × 430, les actions sont tronquées en bas (`auction_review_six_landscape_clipped_v39.png`) : V39 n'est pas publiée ; V40 cible une hauteur flexible de la liste et un pied d'action fixe.

V40 : Economy 106 + Mechanics 99 assertions PASS, build WebGL PASS, sortie 0. Dossier 12 424 KiB, identique à V39 à la résolution du KiB. Même salon QA à six sièges : à 900 × 430, la cinquième offre est atteinte par défilement tandis que Reject All et Seal the Deal restent entièrement visibles (`auction_review_six_landscape_v40.png`). Retour en 430 × 900 vérifié sans régression de mise en page. Aucun relevé Unity Profiler ni mesure sur iPhone physique à ce stade.
Préversion HTTPS V40 : index et loader identiques au build, données/framework/WASM identiques après décompression HTTP, cinq réponses 200 et accueil chargé à 430 × 900. Lien temporaire dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.

V41 — compteurs HUD : Economy 106 + Mechanics 99 assertions PASS, build WebGL PASS, sortie 0. Dossier 12 424 KiB, identique à V40 à la résolution du KiB. Achat local de Hydrogen Speedways à 1 500 $ : CAPITAL passe par 16,64 K$ puis se stabilise à 15,9 K$ ; NET WORTH reste à 17,4 K$ puisque la propriété remplace le cash (`money_purchase_v41_0.png`, `money_purchase_v41_1.png`). Avec « Réduire les animations » activé, achat de French Riviera à 1 000 $ : affichage direct de CAPITAL 17 K$ et NET WORTH 18 K$ (`money_reduced_v41.png`). Une fixture en ligne à deux sièges passe de 17 K$ / 18,3 K$ à 20 K$ / 21,3 K$ côté client ; les captures de cette mise à jour répliquée n'ont pas isolé une image intermédiaire. Pas de relevé Unity Profiler ou iPhone physique ; ne pas conclure à une absence de régression de cadence.

V42 — confirmation d'achat par glissière : contrôle UI Toolkit léger, sans dépendance, affiché uniquement pour une propriété achetable. Un déplacement inférieur à 90 % revient au départ sans transaction ; un déplacement complet réutilise l'action d'achat existante. En 430 × 900, French Riviera a conservé son solde après un glissement incomplet puis a été achetée après un glissement complet, avec message d'activité et compteur animé (`buy_slide_v42_after.png`). Avec « Réduire les animations », la glissière est remplacée par le bouton BUY classique ; l'achat d'Opioid Paradise a réussi et le capital a été mis à jour immédiatement (`buy_reduced_v42_after.png`). Entrée/Espace restent disponibles au clavier. Economy 106 + Mechanics 99 assertions PASS sur le build final ; la vibration reste à valider sur appareil réel.

V43 — focus de sélection Marketplace : le panneau déjà existant se réduit à l'entrée en mode de sélection et reprend son contenu au retour. Les propriétés que le joueur peut choisir continuent de pulser ; les accents des autres cases sont temporairement désaturés et abaissés à 38 % de leur intensité précédente. Le code sauvegarde puis restaure les pixels hors sélection, en laissant les cases admissibles libres de leurs mises à jour de pulse. L'effet réutilise la texture d'état du shader existant : pas de matériau ni de draw call supplémentaire. Les cases sont dans un mesh fusionné, donc elles ne peuvent pas être mises à l'échelle individuellement sans ajouter une géométrie dédiée ; cet effet reste à évaluer au regard du budget mobile. Economy 106 + Mechanics 99 assertions PASS, build WebGL PASS, sortie 0. L'index et les quatre ressources de la préversion HTTPS correspondent au build ; le menu WebGL charge en navigateur (`home_public_v43.png`). La sélection avec des propriétés possédées et le profilage GPU doivent encore être vérifiés sur iPhone réel.
Préversion HTTPS V41 : index, loader et trois ressources Unity correspondent au build, cinq réponses 200, accueil chargé à 430 × 900. Lien temporaire dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.

V44 — icônes de famille et rendement du plateau : `BoardTileOverlayWidget` réutilise `TileIconResolver` et la formule d'inspecteur `BaseRent / BasePrice × 100`. Les ancres existantes alimentent un overlay du document HUD ; il n'ajoute ni Canvas, ni mesh, matériau, donnée ou règle. Le placement se recalcule à 150 ms seulement après une modification de caméra, d'écran ou de géométrie UI. Les repères suivent les tuiles et restent lisibles à l'endroit comme sur les côtés verticaux. Le coût de rendu des éléments UI n'a pas encore été profilé.

Validation : Economy 106 + Mechanics 99 assertions PASS, build Unity WebGL PASS (sortie 0), dossier 12 432 KiB (+8 KiB vs V43). Contrôle navigateur 430×900 puis 900×430 : icônes teintées par famille et pourcentages visibles sur le plateau, inspecteur et HUD fonctionnels. Les Hubs dont `BaseRent` vaut zéro affichent 0 % de rendement locatif, selon les données présentes. L’index HTTPS correspond au build à l’octet près ; loader, framework, data et WASM correspondent après décompression. Préversion : `https://tom-hired-dover-impression.trycloudflare.com/?visual-v44`. Certaines cases sont encore coupées par le cadrage normal portrait ; le cadrage complet s’active en sélection Marketplace. Recette iPhone 14 Pro Max, mesure de cadence et chauffe restent à faire.

V45 — audit des accents lumineux et de la profondeur, sans changement de code : sur V44, l’éclairage au repos reste discret et les cases admissibles concentrent la mise en évidence en sélection Marketplace. `PlayerPawn.prefab` ainsi que les meshes du plateau autorisent déjà les ombres temps réel. Les profils URP Low et Very Low désactivent cependant les ombres ; Medium et au-dessus les prennent en charge. Ajouter une ombre factice à chaque pion coûterait des rendus supplémentaires et ferait doublon sur les profils compatibles. Décision : garder la solution native, puis vérifier la visibilité et la performance sur les niveaux réellement utilisés sur iPhone ; ne pas activer les ombres par défaut sans mesure.

Le build V44 a été observé dans le navigateur à 430 × 900 : HUD, accents de familles et rendement par tuile sont visibles, sans glow uniforme marqué. Ce contrôle confirme le rendu WebGL existant, mais ne mesure ni GPU/FPS ni le rendu d’ombres sur appareil physique.

## HUD Release — vérification responsive du 15 octobre 2026

Le build Release frais `/tmp/PolyRiskFinalCheckpoint_20261015` a été ouvert
dans Chrome 155 accéléré par Metal. Après entrée en partie locale, les quatre
indicateurs CAPITAL / NET WORTH / TURN / MARKET et le bouton de menu restent
sur une ligne en 430 × 900 et 932 × 430 ; les séparateurs sont visibles, les
valeurs suivent les couleurs positives/or patrimonial/neutre/état du marché,
et la police Inter est rendue. Aucune erreur JavaScript n’a été relevée.
Captures : `hud_final_release_430x900_2026-10-15.png` et
`hud_final_release_932x430_2026-10-15.png`.

Cette passe vérifie le build et ces deux viewports ; elle ne compare pas la
grille d’espacement au mockup absent de l’espace de travail et ne vaut pas
recette Safari/iPhone physique. Aucun code ni réglage d’affichage n’a été
modifié.

## Mouvement réduit global — 15 octobre 2026

Le réglage persistant marquait déjà chaque document UI via `UIUXRuntime` et les
effets pilotés par code (déplacement des pions, achat, compteurs et arrivée
d’offres) consultaient déjà ce réglage. Il manquait une couverture commune aux
transitions USS des panneaux et boutons. Une règle unique en fin de
`Theme.uss` force désormais leur durée et leur délai à zéro pour les racines
`ux-reduced-motion`, afin que les prochains effets CSS héritent aussi du réglage.
Les éléments et confirmations restent présents ; leurs transitions deviennent
instantanées.

Validation groupée dans l’Éditeur Unity 2022.3.62f3 : Economy 114/114,
Mechanics 99/99, build WebGL Release `BuildOptions.None` PASS. Le navigateur a
chargé index, style, loader, framework, WASM et données en HTTP 200 depuis la
sortie isolée `/tmp/PolyRiskReducedMotionFinal`. La préversion partagée n’a pas
été remplacée.

Contrôle interactif WebGL local : à 1048 × 720, le toggle a été activé, conservé
après rechargement, puis une partie locale a démarré ; les quatre indicateurs du
HUD, le menu de partie et les boutons d’action tenaient dans le viewport après
chargement à froid. À 430 × 900, l’accueil portrait affichait les champs, Join,
Play Local, Connection et le toggle toujours coché. Ce test d’émulation valide
l’activation, la persistance et la mise en page, mais n’a pas comparé une même
séquence avec les animations activées puis réduites, ni remplacé Safari sur
iPhone physique. Le retour d’un utilisateur sensible au mouvement reste ouvert.

## Marketplace V67 — arrivée d’une nouvelle offre observée en direct

Le 15 octobre, le build WebGL Release courant a été lancé contre une API de
recette isolée sur le port 5096, via un relais QA sur le port historique 5080
attendu par les pages localhost. Le scénario a trois sièges : le client Unity
hôte, un enchérisseur automatisé par le protocole et le vendeur. Un snapshot
de recette place le tender en `Bidding` avec une offre existante de 2 500 $.
Après restauration du client hôte, l’enchérisseur envoie une vraie commande
`marketplace.bid.submit` à 3 000 $. Le client Unity hôte met alors la fiche
spectateur à jour : l’activité « QA Bidder soumet une offre » apparaît et la
liste passe à deux offres sans reconstruire le panneau. Le snapshot autoritaire
confirme `Reviewing`, séquence 1 et les offres 2 500 $ / 3 000 $.

Cette recette valide la réception réseau, le rendu de la nouvelle ligne et le
rafraîchissement en direct du panneau. L'audit source confirme une transition
de 180 ms via `bid-entry--incoming` ; la classe n'est ajoutée que si « Réduire
les animations » est désactivé, et l'USS donne une durée de 0 ms aux lignes dans
l'état réduit. La comparaison visuelle de ces deux états et la mesure de la
transition restent à recetter en runtime. La recette Safari sur iPhone reste
également ouverte. L'erreur « Unknown Error » du premier essai venait du banc :
les pages localhost visaient le port 5080, alors que le serveur local était
démarré sur un autre port.


## Invitation Marketplace — test final en Release WebGL, 15 octobre 2026

Le fond à 97 % et le tri au-dessus du HUD ne suffisaient pas à masquer
complètement les badges ROI. `TopHUDWidget.SetBoardYieldOverlayVisible` masque
désormais l'overlay existant pendant une modale Marketplace ;
`Marketplace_UIManager` le rétablit à la fermeture ou à l'expiration de la
notification. Aucun Canvas ou shader supplémentaire, polling, ni changement
de règle/donnée n'est impliqué.

Build Unity 2022.3.62f3 : Economy 114/114, Mechanics 130/130, Release WebGL
`BuildOptions.None` PASS. Dans un lobby local neuf avec fixture d'invitation,
le titre et le vendeur sont lisibles à 19 s, le minuteur passe au rouge à 3 s,
puis les badges de rendement réapparaissent après fermeture automatique. Le
test valide cet état Marketplace en navigateur desktop local ; il ne remplace
pas la recette iPhone Safari ni le contrôle sur le lien public. La capture
historique `auction_timer_critical_v38.png` reste mal étiquetée et ne prouve
pas ce résultat récent.
