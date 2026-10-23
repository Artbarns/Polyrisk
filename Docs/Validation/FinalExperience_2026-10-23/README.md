# PolyRisk — Final Experience

Travail commencé le 23 et poursuivi le **24 octobre 2026**. **Build Release et recette locale livrés ; publication HTTPS encore en attente d'accès à un hébergement persistant.** Aucun lien local ci-dessous ne constitue une URL publique ou une validation iPhone.

## Jouer et examiner

- Aperçu **local, Mac requis** : `http://127.0.0.1:9012/` — le vrai export Release servi par le serveur .NET, sans galerie ni outils QA.
- Galerie locale : `http://127.0.0.1:9006/evidence/review.html`.
- Vidéo réelle principale : [caméra, coin, arrivée et achat, avec audio](release-close-corner-audio.mp4), 34,76 s. Lancer naturel **5 + 4**, case 7 → 16 via le coin 10, puis achat de Ferrari Maranello.
- [Action du deuxième joueur, avec audio](release-multiplayer-audio.mp4), 34,72 s. Le premier enregistrement `release-multiplayer-game.mp4` est silencieux : la sortie WebAudio n'était pas encore initialisée. Il est conservé comme preuve, sans le présenter comme une prise sonore.
- Déploiement prêt : `Builds/FinalExperienceReleaseBundleFinal/`, 24 fichiers et manifeste SHA-256. Le conteneur n'a pas été construit ici : aucun moteur Docker disponible.

Sur cet aperçu loopback, déployer CONNECTION et saisir `http://127.0.0.1:9012` : le défaut localhost hérité est5080. Sur une origine publique non loopback, le code existant utilise l’origine HTTPS du jeu par défaut.

Le multijoueur garde le menu existant : **CREATE ONLINE GAME**, partage du code de six caractères, **JOIN**, chaque joueur **I'M READY**, puis **START GAME** par l'hôte. Deux clients WebGL distincts ont effectivement joué sur ce Mac. Capacité protocole testée : six clients. Aucun test entre appareils distants ou sur Safari iPhone n'est revendiqué.

## Caméra et exploration

`BoardInteractionManager` reste l'unique writer. Le calcul stratégique de PASS 2F.1 est conservé comme référence séparée ; sa taille est divisée par **1,40** sur chaque format, sans réduire silencieusement ce facteur.

| Format CSS | Référence PASS 2F.1 | Nouveau repos |
|---|---:|---:|
| 430 × 830 | 18,2081718445 | 13,00583744 |
| 1200 × 900 | 11,2809009552 | 8,0577864 |

Orthographique, élévation **40°**, azimut authored **45°**. Repos ancré sur le pion actif avec **0,75 unité** d'anticipation et bornes d'exploration. Zoom complémentaire de mouvement : **1,00** (ancien 1,14). Suivi existant : amortissements horizontal **80 ms**, vertical **120 ms**, coins **140 ms**, ancrage utile **0,50 / 0,52**, entrée **300 ms**, hold **250 ms**, retour **375 ms**. Le retour cible la nouvelle position du pion.

Le glissement souris/tactile utilise un seuil de **10 pixels UI**, un déplacement borné dans le plan caméra, et distingue le tap au relâchement. Il conserve l'exploration jusqu'à un événement pertinent de jeu. Un snapshot du même joueur à la même position laisse l'exploration intacte. Les contrôles UI continuent de bloquer les raycasts. Aucun bouton caméra ajouté.

Reduced Motion conserve la caméra fixe pendant l'interpolation, puis établit le nouveau repos à l'arrivée. La première recette a révélé un recentrage au premier frame ; ce défaut est corrigé et la recette finale passe. Aucun callback de `PawnMovement` changé. Le cockpit n'intervient jamais dans la clé de calibration : inspection et déploiement réel ont gardé exactement la même pose et la même taille.

## Rendu et netteté

B **Graphite Reflective Neon** est conservé. Après examen du nouveau cadrage, quatre coefficients seulement sont affinés :

| Paramètre B | PASS 2F.1 | Final |
|---|---:|---:|
| Smoothness support | 0,47 | **0,49** |
| Smoothness cases | 0,41 | **0,45** |
| Arête spéculaire | 0,040 | **0,052** |
| Émission néon | 0,88 | **0,92** |

A reste inchangé. Albedo, metallic, lumière, bloom, géométrie, pions, contact lumineux et son restent ceux de PASS 2F.1. Aucun shader, texture, lumière, probe temps réel ou passe ajouté. Les impulsions conservent attaque 32 ms, décroissance 145 ms, arrivée 215 ms et leurs horloges de lock. Les tests A/B vérifient la conservation des données et des états persistants.

Les contours sont nettement plus continus dans la capture au framebuffer **860 × 1660** pour **430 × 830 CSS**. Cette page QA force la résolution à ×2 sur Chromium avec un device DPR réel de 1 : **elle ne simule pas Safari ni le GPU d'un iPhone**. Le comparatif 2F.1/final à DPR2 utilise la même résolution et quatre pions à GO. Le gain de netteté ne doit pas être attribué uniquement aux coefficients de matière.

Le template détecte aussi l'iPadOS avec UA desktop, plafonne le DPR initial à **2** avec budget **2,4 Mpixels**, et réserve les gestes du canvas au jeu (`touch-action:none`). Les très grands viewports restent au moins à leur résolution CSS ; ce budget n'est donc pas un plafond absolu. URP renderScale **1**, SMAA High et Low par défaut conservés. `minimumBandPixels` reste **1,2 pixel framebuffer**, soit environ 0,6 pixel CSS à DPR2 : largeur géométrique et couverture en pixels ne sont pas confondues.

La Marketplace avait deux libellés dynamiques noirs sur fond sombre. Seules leurs couleurs sont corrigées ; [avant](marketplace-labels-before.png) / [après](marketplace-labels-after.png). Aucun changement de layout, de cockpit ou de commande.

## Preuves visuelles

- Portrait : [2F.1 DPR1](baseline-portrait.png) / [Release final DPR1](release-final-portrait.png).
- Netteté comparable : [2F.1 DPR2](baseline-portrait-dpr2.png) / [nouveau cadrage et finition DPR2](final-b-portrait-dpr2.png).
- Desktop : [2F.1 1200 × 900](baseline-desktop.png) / [Release final 1200 × 900](release-final-desktop.png).
- [Cockpit déployé](final-cockpit-expanded.png), [sélection après drag](mouse-selected-tile.png), [poses avant/après inspection et cockpit](cockpit-no-camera.json).
- Les pions à GO restent petits lorsqu'ils sont regroupés. L'anneau actif se distingue mieux que les silhouettes individuelles. Aucun nouveau redesign des pions n'a été entrepris.
- Le board est volontairement hors cadre par endroits. L'accès aux 40 cases est établi par la recette d'exploration, pas par une image qui montrerait tout simultanément.

## Tests exécutés

| Contrôle | Résultat réellement obtenu |
|---|---|
| Compilation C# et export IL2CPP WebGL Development puis Release | **PASS**, Unity 2022.3.62f3 / URP 14.0.12 |
| Régressions existantes | **612 assertions PASS** : économie143, mécanique148, événements17, UI304 |
| Géométrie de caméra et seuils | **31 contrôles Editor PASS** |
| Scène/rendu/40 indices et raycasts/4 meshes | **48 contrôles Editor PASS** |
| Nouvelle caméra runtime | **164 contrôles PASS**, zéro échec final ; le nombre inclut des échantillons de hold dépendants du framerate |
| Invariants A/B WebGL | **95 PASS** : état économique, ancres/colliders, meshes, pions/sons/locks, sélection persistante |
| Enveloppe lumineuse WebGL | **8 PASS** lors de la reprise sans build en arrière-plan. Une première prise sous compilation a manqué le pulse à l'échantillonnage ; elle est conservée dans les preuves |
| Template DPR/iPad/observateurs/gestes | **37 contrôles JavaScript PASS** |
| Interaction navigateur réelle | Drag souris conservé, tap sur case12, cockpit déployé sans changement de pose/taille |
| Backend .NET Release | 0 erreur, 0 avertissement ; smoke6, persistance/restart, reconnexion/transfert d'hôte **PASS** |
| Six clients protocole sur90s | **1170 requêtes authentifiées, toutes200**, budget configuré3600/min ; rejet429 et bornes testés séparément |
| Erreur réseau dans le Release | Adresse indisponible → message « Unknown Error » ; correction de l’adresse → salon créé sans rechargement. Libellé générique encore à améliorer |
| Deux WebGL Release | Quatre tours naturels, événements Venture, deux achats, synchronisation et deux rafraîchissements exécutés |
| Paiement et enchère préparés | États de reprise QA explicites, puis vrais moteur/UI et transport ; transferts et propriété vérifiés |
| Fichiers réels du Release servis par .NET | WASM/data/gzip/cache validés ; source, données et capture QA **404** |
| Vidéos | MP4 H.264/AAC décodables ; deux prises stéréo48kHz, aucun échantillon écrêté |

Les assertions historiques imposant une vue de toutes les cases et un zoom d'action1,14 n'ont pas été réécrites pour les faire passer : elles documentent les anciennes passes. Le nouveau gate vérifie les critères de cette demande.

## Recette multijoueur détaillée

Salon de test isolé **JZGNEM**, clients sur origines9006 et9011, API5097. Aucune session humaine partagée modifiée.

1. Les deux sessions rejoignent, passent READY et démarrent via l'UI. Premier lancer **4+3**, puis **6+1** : chacun atteint Venture7. Les cartes réellement tirées instaurent BOOM puis augmentent le Sgo à2100.
2. Le lancer **5+4** de l'hôte l'amène de7 à16 via10. Achat réel Ferrari Maranello **1700** : solde18000→16300, owner16=0.
3. Le lancer **2+4** du deuxième client l'amène de7 à13. Achat réel Hydrogen Speedways **1500** : solde18000→16500, owner13=1.
4. Le rafraîchissement de l'hôte restaure un **snapshot intégralement identique**, version11, sans double achat ni replay économique. Le deuxième client est également rafraîchi après le paiement.
5. **Scénario préparé distinct** : reprise sur la Ferrari possédée, avec BOOM naturel conservé. Le vrai moteur verse **221** du visiteur au propriétaire : soldes16300/16500→16521/16279. Événement typé de loyer et somme des capitaux vérifiés.
6. **Scénario préparé distinct** : reprise sur Marketplace20. Vente sélectionnée par raycast de la propriété13, participation de l'autre client, offre **1770**, sélection et acceptation par le vendeur. Soldes16521/16279→14751/18049, owner13 passe de1 à0. Aucun montant injecté dans la règle de transaction.

Les fixtures changent uniquement l'état initial du **salon QA** pour exercer la reprise. Elles ne constituent ni des lancers naturels, ni un test distant. Les soldes avant/après, versions, événements et conservation sont enregistrés dans les JSON ; `two-client-economy-validation.json` contient huit assertions réussies. Aucun token de reconnexion n'est publié dans ces preuves.

## Performance et limites de mesure

Mesures dans Chromium/IAB sur ce **Mac Intel Iris Plus**, WebGL **Development**, canvas430×830 DPR1, fenêtres de6s. Elles ne sont pas des mesures iPhone, Safari ou du Release de production. Voir `performance-summary.json` pour les données finales.

- Un premier créneau Low/B à GO : **23,18 ms moyens**, max45 ms, 72draw/65batches/37SetPass ; mémoire Unity allouée51,69MB, réservée60,04MB.
- Low/A mesuré plus tard en case7 : **27,56 ms**, max61 ms, 93draw/73batches/37SetPass. Le jeu a effectué son auto-skip de planning existant entre ces prises ; ce n'est pas un comparatif A/B strict avec GO.
- Low/B en case7, même cadrage que A : **26,52 ms**, max49 ms, 93draw/73batches/37SetPass ; allouée58,54MB, réservée66,89MB. Cette seule prise ne démontre pas un gain statistique face à A.
- Medium/B en case7 : **30,09 ms**, max60 ms, 97draw/77batches/39SetPass. Courte mesure après changement de qualité, sans isolation CPU/GPU.
- La référence2F.1 à GO donnait39,16 ms sur un autre créneau/session. La variabilité et la reprise du navigateur empêchent d'attribuer la différence au seul nouveau cadrage.

A et B utilisent les mêmes programmes, passes et textures : aucun gain significatif de structure n'est établi pour A. B reste la piste retenue, sans promesse de60fps. La mémoire Profiler ci-dessus ne représente pas l'intégralité du heap WASM, du navigateur et du GPU. Mémoire initiale WebGL256MB inchangée ; bundle Build compressé **12 603 141octets**. Coût du post-processing non isolé, chauffe/consommation/stabilité Safari après longue partie non mesurées.

## Hébergement et Git

Le serveur existant reste ASP.NETCore net10 avec requêtes HTTP JSON, autorité Unityhost, snapshots versionnés et JSON atomique. Pas de WebSocket : aucun ajout WSS nécessaire. `POLYRISK_WEBGL_ROOT` permet de servir le jeu sur la même origine ; PORT et budget de requêtes sont configurables, avec défaut900 conservé et3600 dans le conteneur. Une seule réplica et un volume `/data` sont requis.

Les anciennes URL Cloudflare dépendent du Mac. Aucun hébergement persistant autonome n'est connecté. Vercel est accessible mais n'a aucun projet existant disponible ; ses [conteneurs](https://vercel.com/docs/functions/runtimes/container) se mettent à l'échelle et s'arrêtent à l'inactivité. Héberger tel quel le store JSON local n'y garantit pas sa durabilité. La livraison n'impose pas une nouvelle architecture de persistence pour contourner cet accès manquant.

GitHub est connecté au compte Artbarns. Les six dépôts accessibles inspectés concernent d'autres projets, aucun PolyRisk. Le connecteur ne fournit pas la création/renommage de dépôt ; l'ouverture de la création via navigateur mène à une session non authentifiée. Aucun de ces dépôts n'a été renommé ou écrasé, aucun push/PR n'est revendiqué.

Branche locale : **release/final-experience-20261023**. Le checkpoint **0e7ccc1** conserve les fondations locales préexistantes jusqu'à2F.1, y compris leurs modifications économiques antérieures à cette demande. Ces changements ne sont pas attribués à cette passe. Les fichiers de travail ne sont pas réinitialisés ; main d'origine et les autres documents locaux restent conservés. Le commit **58b3283** contient uniquement la nouvelle livraison ciblée (20 fichiers). Un commit séparé conserve ce rapport et les preuves ; identifiants dans `git-release.json`.

**Accès encore nécessaires :** un dépôt privé vide PolyRisk autorisé dans GitHub (ou un dépôt cible explicitement nommé), et un projet d'hébergement persistant autorisé — Railway ou hébergeur existant. Après ces accès : construire le conteneur sur l'hébergeur, attacher le volume, configurer le domaine réel HTTPS et les origines, publier le bundle final, puis revalider le jeu et deux clients depuis l'URL publique. **Aucune URL de production n'est inventée.**

## Fichiers de cette passe

Sept fichiers préexistants modifiés par rapport au manifeste de départ :

- `Assets/Scripts/Systems/BoardInteractionManager.cs` — caméra et exploration.
- `Assets/Scripts/Board/BoardVisualTheme.cs` et `Assets/Settings/GraphiteFoundation/GraphiteTheme.asset` — quatre coefficients B.
- `Assets/WebGLTemplates/PolyRiskResponsive/index.html` et `TemplateData/style.css` — DPR, iPadOS, métadonnées et gestes.
- `Assets/UI/Styles/Theme.uss` — deux couleurs de libellés Marketplace.
- `Server/PolyRisk.LobbyServer/Program.cs` — bootstrap de livraison, sans changement des routes/DTO/règles.

Créés : `CloseStrategicCameraValidation.cs`, `CloseStrategicCameraValidationDriver.cs`, `FinalExperienceValidation.cs` et leurs meta ; `WebGLHosting.cs`, `webgl_hosting_test.py`, `polling_budget_test.py` ; `Server/Deployment/` et ce dossier de preuves. Les montages QA sont conditionnés Editor/Development et absents de l'auto-initialisation Release. Le recorder HTML est local et exclu du bundle.

**619 des626 fichiers préexistants sont identiques octet pour octet.** Les33 sources protégées contrôlées — règles, modèle, économie, mouvement et transport Unity — sont inchangées. Les écritures de sérialisation de matériaux faites par l'Editor ont été enregistrées puis les octets originaux restaurés. Les changements locaux antérieurs hors périmètre restent présents.

## Validation restante

Publication HTTPS autonome, construction Docker distante et test public entre appareils ; Safari iPhone physique, gestes tactiles réels, performances/chauffe/mémoire sur téléphone ; longue partie réseau distante. Les menus et cartes Marketplace héritent encore de styles anciens dans certaines vues : cette passe corrige le défaut de contraste observé, sans refonte générale. Les quatre pions regroupés à GO et les accents sous-pixel méritent encore le regard de l'utilisateur sur le téléphone cible.
