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
