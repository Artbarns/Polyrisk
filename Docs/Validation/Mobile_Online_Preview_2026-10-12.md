# Préversion mobile en ligne — 12 octobre 2026

Lien de recette V44 (rétabli le 14 octobre) : https://chambers-vat-carry-history.trycloudflare.com/?visual-v44

Le lien Cloudflare précédent s’était arrêté avec Error 1033. Le serveur statique et l’API ont été relancés sur ports locaux 8900/5091, avec une base de recette isolée ; le nouveau tunnel quick est temporaire et dépend de ce Mac et de ses processus actifs.

Recette de reprise publique : page et fichiers Unity renvoient 200, y compris `application/wasm` avec `Content-Encoding: gzip`. `smoke_test.py` lancé sur l’origine HTTPS a validé 6 sièges, Ready/Start, rejet du 7e joueur et un aller-retour de commande/état (`M47FKR`, version de lobby 13, version d’état 1). Le build Unity dans le navigateur a créé un salon via « Create Online Game » (`4K4665`, un siège), puis le salon QA a été quitté pour revenir à l’accueil. Ce contrôle n’est pas une partie humaine entre amis ni une connexion depuis Safari/iPhone physique.

Le 14 octobre, `persistence_test.py` a également réussi contre un serveur de test isolé sur 5092 : les identifiants reconnect restent non persistés en clair, les commandes en attente et le snapshot de partie reviennent après redémarrage. Le relais public 8900 et l’API de préversion 5091 sont restés actifs pendant ce test.

Recette client WebGL à six sièges : le salon `BS6LUU` a atteint 6/6 ; cinq sièges QA ont été raccordés via l’API et déclarés prêts, le client Unity hôte est passé Ready puis Start Game. Le client a chargé les six pions, roulé puis acheté Temu Sweatshop Terminal. Snapshot serveur après achat : six joueurs, joueur 0 à 17 000 $, joueur courant 1 en Planning, deux tours commencés, version 3, séquence acquittée 2, aucune commande en attente. Le lobby UI permet d’atteindre les sièges 5 et 6 par défilement tout en gardant les commandes de préparation visibles. Les cinq clients QA n’étaient pas des instances WebGL distinctes : le prochain contrôle doit relier de vrais navigateurs/appareils pour jouer le tour du siège 2, puis poursuivre la partie.

Build V41 depuis le 14 octobre, serveur statique et relais API sur 8900, API de recette 5091. Le tunnel HTTPS de recette est temporaire et nécessite que ce Mac reste allumé. L'index et le loader correspondent octet pour octet au build ; les trois ressources Unity compressées correspondent après décompression HTTP. Les cinq URLs répondent 200 et le menu public a chargé à 430 × 900. La revue d'offres à six sièges et les compteurs HUD ont été contrôlés en local ; le lien public n'est pas une preuve de partie humaine réelle ni de cadence sur iPhone.
Le smoke test `smoke_test.py` sur cette nouvelle origine HTTPS passe avec six joueurs (salon A33BXU, version 13, état de partie version 1). Ce test de protocole ne remplace pas une partie humaine sur six appareils.
Tunnel temporaire : le Mac et les processus doivent rester allumés. Le lien
mobile historique sur 8765 reste distinct.

V44 du 14 octobre : glyphes de famille et rendement sur le board. Build WebGL
PASS ; Economy 106 + Mechanics 99 assertions PASS. Le menu et le board ont été
observés dans le navigateur à 430×900 et 900×430. Recharger avec `?visual-v44`.
La mesure sur iPhone 14 Pro Max reste à faire.

## Recette six clients WebGL indépendants — 14 octobre 2026

Dans le salon de recette `ZGUKEP`, six clients WebGL ouverts sur des origines
distinctes ont rejoint la même API : 6/6 Ready, puis démarrage par l’hôte.
L’hôte a roulé et résolu deux cases, dont la carte « HÉRITAGE OFFSHORE » ;
les clients ont vu le même plateau et la progression du tour, tandis que le HUD
affichait les valeurs propres au joueur local. Le joueur QA2 a ensuite acheté
Opioid Paradise pour 1 100 $ ; l’état autoritaire indiquait 16 900 $ et la
propriété de la case 8 pour ce joueur. L’action et la propriété étaient visibles
depuis les autres clients. Les décisions Buy/Pass n’étaient présentes que sur
le client dont c’était le tour.

Le parcours vendeur Marketplace conserve le widget réduit pendant la sélection
sur le plateau, puis rouvre le panneau après le choix avec le nom de la propriété
et son revenu. Les captures `Reconnect_2026-10-12/six_marketplace_pick_v3.png`
et `six_marketplace_selected_v3.png` montrent ces deux états. Le panneau de
sélection vendeur ne remplace pas la fiche inspecteur complète Rent/Level/Yield.

Les six vues étaient six interfaces WebGL distinctes, mais elles ont été
pilotées depuis le même poste et avec des joueurs QA. Cette preuve valide la
propagation des états et l’affichage à six clients dans le navigateur ; elle ne
remplace pas une partie humaine simultanée sur six téléphones/ordinateurs, ni la
recette Safari/iPhone et réseau réel. Le lien public V44 n’a pas été remplacé.

Pendant cette recette, une collision d’entrée a été identifiée : la touche
Espace déclenchait simultanément la confirmation du slider d’achat et le
raccourci de debug de résolution forcée du tour. Les raccourcis Espace et M de
`TurnManager.Update()` sont désormais compilés uniquement dans l’Éditeur ou un
build de développement/debug. La régression Unity a passé Economy 106 et
Mechanics 99 assertions ; le build Unity WebGL 2022.3.62f3 avec
`BuildOptions.None` a terminé PASS (code de sortie 0). Sortie isolée :
`/Users/arthurbarreau/.codex/validation/PolyRiskSixClientReleaseBuild` (WASM gzip
7,3 Mio, données gzip 4,7 Mio). Cela confirme la compilation du correctif ; ce
build n’a pas remplacé V44. Exécution de ce même build sur le serveur local isolé
`127.0.0.1:8910` : menu démarré, plateau initialisé à 40 cases, aucun overlay
debug visible et aucune erreur JavaScript navigateur. Unity a signalé le shader
diagnostic URP `Hidden/Universal/HDRDebugView` non supporté par le GPU du
navigateur ; le menu a rendu normalement. Cela reste un smoke visuel du menu, pas
une partie complète ni une mesure sur Safari/iPhone.

Préversion temporaire de ce build — 14 octobre :
https://behavior-alexandria-mhz-wiley.trycloudflare.com/?ui-v46-2026-10-14 .
Elle utilise le serveur API de recette 5091 et coexiste avec V44, qui n’a pas été
remplacée. Le chargement HTTPS distant a démarré Unity et le plateau de 40 cases
sans erreur JavaScript ; le smoke test distant a validé le lobby à six, Ready,
Start et une commande de jeu (`36YVMD`, lobby version 13, game state version 1).
Cette URL quick-tunnel et ces services sont temporaires : le Mac et les
processus de préversion doivent rester actifs.

Mise à jour V46 sur cette même URL après le build d’animation live : le dossier
de ressources a été fusionné sans supprimer les anciens fichiers hashés, puis
`index.html` changé en dernier. Rechargement distant confirmé sur le framework
`511e108b3e8610c9c3a887148d9ae49d` ; Unity a initialisé les 40 cases sans erreur
JavaScript. L’animation d’arrivée des offres reste à essayer pendant une partie.

Mise à jour V47 — inspecteur : le niveau n’est plus répété dans les statistiques
car il figure déjà dans le badge famille. Le rang présente Rent / Risk / ROI ;
Risk affiche `N/A` (aucune donnée par propriété) et le ROI utilise le rapport
BaseRent/BasePrice existant. Economy 106 + Mechanics 99 assertions PASS et build
Unity WebGL `BuildOptions.None` PASS. Le menu et le plateau chargent en local et
sur HTTPS sans erreur JavaScript; l’inspecteur lui-même reste à capturer et
contrôler en portrait/paysage ainsi que face au mockup.

Recette responsive du même build en viewport 430×900 puis 900×430 : les actions
Créer/Rejoindre/Local restent visibles ; en paysage court, les réglages bas
restent accessibles par défilement. Aucune erreur navigateur dans les deux
formats. C’est une émulation de taille de fenêtre, pas une mesure tactile ni de
densité réelle de l’iPhone.

Animation live des offres — 14 octobre : `Marketplace_UIManager` conserve la
bottom sheet spectateur montée lors des nouvelles offres pour éviter de rejouer
son entrée et de reconstruire le document entier. La liste est actualisée ; une
transition de 180 ms (fade + déplacement vertical de 8 px) s’applique uniquement
à un enchérisseur nouvellement vu. Le réglage « Réduire les animations » supprime
cet effet. Unity Economy 106 + Mechanics 99 assertions et build WebGL
`BuildOptions.None` PASS ; le client local a démarré le menu et le plateau de
40 cases sans erreur JavaScript. L’affichage événementiel de la nouvelle ligne
reste à contrôler en partie WebGL réelle à deux/six clients et avec le réglage
réduit activé.

Audit d’enchère classique : ce flux est séparé des offres de la Marketplace.
`ActionButtonsWidget` ne démarre `AuctionManager` que si une instance existe ;
la scène principale ne référence pas ce composant et le chemin courant passe
donc par `SkipOptionalAction`. En ligne, `action.secondary` saute aussi l’action.
Le gestionnaire dormant sait démarrer une enchère, accepter des mises dès 10 $
avec incréments de 10 $, et terminer seulement par un appel explicite à
`EndAuction` ; aucun écran ne s’abonne à ses événements et il n’a ni minuterie
ni clôture automatique. Le GDD ne définit pas le tour de parole ni la condition
de fin de cette enchère classique. Il faut finaliser la règle avant de rendre le
flux local et réseau fonctionnel.

Chaque joueur ouvre ce lien sur son propre appareil et choisit un pseudo unique.
Un joueur crée le salon et partage le code ; les autres le rejoignent. Tous
cliquent Ready, puis l’hôte Start Game. Jusqu’à six joueurs. Aucun réglage
d’adresse serveur nécessaire sur ce lien public : origine HTTPS automatique.

## Contrôles confirmés

- WebGL V11 : Economy 106, Mechanics 92 assertions Unity puis build PASS / sortie 0.
- Six clients WebGL : rejoindre/ready/démarrer, six lancers manuels, achats,
  loyer, passage direct Planning→Planning avec bouton actif.
- Rechargement des six clients à un point de décision : snapshot public complet
  strictement identique. Sixième client récupère automatiquement après timeout.
- Achat après reprise : un seul débit de 1000 $, séquence acquittée.
- Serveur HTTPS : smoke protocole à six joueurs PASS (QBHYM8).
- Navigateur public : chargement et création 4LV5EC sans saisir de serveur.
- Format portrait 430×900 : code, ligne joueur, boutons visibles. Ce contrôle
  navigateur ne remplace pas un essai sur Safari/iPhone réel.
- Précompression : principaux fichiers 64020488→18086975 octets, facteur 3,54.
  HEAD négociation gzip/identity/q=0, MIME wasm et SHA décompressé confirmés.

## Encore à vérifier / finir

Parties humaines, Safari réel, Marketplace V8 avec offres/fermeture, OPA,
pouvoirs de famille, liquidation et fin de partie en ligne, équilibrage final.
V9 corrige la hauteur du salon en paysage court : contrôle navigateur à 900×430,
sixième joueur accessible et bouton Quitter fonctionnel. Safari réel reste à confirmer.
Publication HTTPS V9 vérifiée (index identique, wasm 200 application/wasm gzip).
Les anciens fichiers V8 restent disponibles pour les chargements déjà commencés.

Retour utilisateur initial : premier essai sur iPhone 14 Pro Max fluide, boutons
et panneaux réactifs, y compris le widget Marketplace. Mise à jour après la
partie entre amis à quatre confirmée le 15 octobre : l’utilisateur rapporte
toujours une bonne réactivité des panneaux/boutons et un widget Marketplace
fluide, sans défaut précis signalé. La durée, Safari/iOS et les mécaniques rares
rencontrées ne sont pas consignés ; cela reste un essai exploratoire, pas la
recette approfondie du multijoueur ni un test humain à six.

Relancer le serveur statique avec --precompress pour préparer les copies gzip
d’un nouveau build, puis --api-upstream http://127.0.0.1:5091.

## Préversion Release fraîche — 15 octobre 2026

L’ancien Quick Tunnel `secretariat-result-tired-bridal.trycloudflare.com` ne
résolvait plus et ses services n’étaient plus à l’écoute. Une nouvelle
préversion isolée sert le build WebGL Release contenant le menu « Quitter la
partie » : https://does-specialists-remembered-athens.trycloudflare.com . Le
serveur lobby utilise un fichier de stockage temporaire séparé, sans réutiliser
les lobbies ou données de l’ancienne préversion. Cette URL Quick Tunnel cesse de
fonctionner si le tunnel ou les deux services locaux s’arrêtent.

Revalidation : build serveur Release `dotnet build` sans avertissement ni erreur ;
`smoke_test.py` distant via HTTPS PASS (6 joueurs, Ready/Start, commande/état,
rejet d’entrée invalide) ; `persistence_test.py` PASS ;
`six_player_reconnect_test.py` PASS (six sièges restaurés et transfert d’hôte).
Les assets page et WASM répondent HTTP 200. Le client Unity s’initialise dans le
navigateur et « Create Online Game » ouvre bien un lobby sur cette origine ; le
lobby QA a été quitté après vérification. Cela valide transport et amorçage du
salon, pas une nouvelle partie humaine à quatre ou six joueurs.

Tentative de recette UI à six onglets sur la même machine, avec six ports
distincts et stockage/API isolés : le build s’est chargé sur les six origines,
l’hôte a créé `TUP8WW` et le premier invité a rejoint (2/6). Le pilotage des
champs dans les onglets WebGL cachés est devenu intermittent, puis le contrôle
du navigateur a expiré sous la charge des six runtimes ; Ready/Start et le tour
ne sont donc pas validés par cette tentative. Aucun défaut de synchronisation
du jeu n’a été observé ni conclu. Le banc/API local a été arrêté et les onglets
QA fermés ; le tunnel amis (port 8930/API 5093) est resté actif.


## Mise à jour du 13 octobre — V11

Même lien HTTPS ; API5091 mise à jour et smoke six joueurs9H545G PASS,
commande dice.turbo incluse. Test serveur restart/persistence PASS sur stockage
isolé ; stockage public avant remplacement vide (backup conservé).
Turbo : choix humain et reprise validés avec deux navigateurs V10, pénalité
500 une fois au cinquième double. V11 rendus430×900/900×430 : contexte de
choix explicite, statistiques masquées, aucune superposition debug/plein écran.
Build sans Development. Les anciens assets restent disponibles, index changé
en dernier et égal au V11 via HTTPS200. Recharger avant nouvel essai.
Cela ne clôt pas la recette des autres familles/OPA et des parties humaines.

V12 : revue marketplace restaurée sans panneau de case superposé, offre mixte
acceptée sur deux navigateurs, cash et titres conservés après reconnexion.
Rendus portrait430×900 et paysage900×430 contrôlés ; index public V12 vérifié.

## Mise à jour visuelle du 13 octobre — V20

Le template WebGL ne force plus DPR 1 sur mobile. Il utilise la densité de
l'écran, plafonnée à 2 et à 2,4 millions de pixels de rendu : sur un viewport
430×932 à DPR natif 3, le buffer cible est 860×1864, soit 1,60 million de
pixels. La cause du flou global a été identifiée dans le template. La netteté
et la cadence sur iPhone 14 Pro Max réel restent à mesurer ; le navigateur de
recette disponible ici fonctionne à DPR 1 et ne peut confirmer ce point.

V20 : Economy 106 + Mechanics 99 assertions PASS, build Unity WebGL PASS
(sortie 0), taille du dossier identique à V19 (12 292 KiB). En portrait
430×900, la sélection Marketplace cadre le plateau complet, la case possédée
est atteignable et le panneau se réouvre avec le titre choisi. Captures :
`Reconnect_2026-10-12/visual_board_fit_v20.png` et
`Reconnect_2026-10-12/visual_board_selection_v20.png`.

L'index public a été remplacé en dernier, est identique au build V20 et
répond en HTTPS 200. Le menu public a été chargé dans le navigateur de recette
(`Reconnect_2026-10-12/visual_public_home_v20.png`). Utiliser le paramètre
`?visual-v20` pour contourner un ancien index mis en cache. Le tunnel reste
temporaire et dépend du Mac allumé.

## Mise à jour du plateau — V21

Les teintes des propriétés suivent maintenant leur famille réelle ; la lumière
des cases au repos est abaissée. Build Unity205 assertions PASS. En recette
locale 430×900, la case sélectionnable reste clairement visible et le choix
réouvre le formulaire avec la propriété affichée. Captures et limites :
`UI_Motion_2026-10-13/README.md`.

Index public V21 identique au build et quatre ressources WebGL en HTTPS 200.
Le dernier contrôle de chargement dans le navigateur public a été interrompu
par l'arrêt de l'outil de navigateur ; la recette locale V21 a réussi. Utiliser
`?visual-v21` pour contourner un ancien index en cache. Fluidité et netteté
sur iPhone réel restent à confirmer.

## Recette deux clients WebGL — 14 octobre 2026

Dans le build public V44, deux instances Unity WebGL avec stockages navigateur
indépendants ont rejoint le salon `KPM6A7`, lancé une partie et joué à tour de
rôle. L'hôte a acheté Pfizer-BioTech puis Hydrogen Speedways ; l'invité a acheté
Fentanyl Labs. Les activités, propriétaires et soldes ont été publiés dans le
même état serveur : Pfizer-BioTech et Hydrogen Speedways au siège hôte (15 300 $),
Fentanyl Labs au siège invité (16 700 $), version d'état 8, séquence 5 acquittée,
aucune commande en attente. Après rechargement de sa page, l'invité a retrouvé
la partie et son tour sans ressaisir le code.

Ce résultat valide le flux de base entre deux clients WebGL et la reprise après
rechargement dans ces navigateurs de recette. Les deux clients étaient pilotés
en QA sur le même poste : cela ne valide ni six personnes/appareils simultanés,
ni Safari/iPhone, ni une partie complète, ni les parcours Marketplace/OPA et les
pouvoirs de famille avec des joueurs humains. Le lien de préversion reste
temporaire et dépend du Mac hôte.

## Inspecteur UI V48 — 14 octobre 2026

L’inspecteur de propriété présente le badge famille/niveau et les métriques
Rent / Risk / ROI. Le loyer courant et le ROI locatif utilisent les valeurs
existantes ; faute de donnée de risque par propriété, Risk affiche `N/A`.
Les indicateurs comparent le loyer courant au loyer de base : hausse, baisse ou
égalité. Ils ne signalent pas une tendance historique ni une prévision.

Validation : Economy 106 et Mechanics 99 assertions PASS ; build Unity WebGL
`BuildOptions.None` PASS. Le client local V48 démarre, et une propriété
(Antarctica Datacenter, niveau 0) affiche Rent `$350`, Risk `N/A`, ROI `10,0 %`
avec flèches neutres. Le preview HTTPS charge le menu. Ce contrôle visuel a été
fait en 1280×720 ; les vues 430×900/900×430, les états de tendance hausse/baisse,
le contrôle face au mockup et le profilage iPhone restent à faire.

Preview temporaire :
https://behavior-alexandria-mhz-wiley.trycloudflare.com/?ui-v48-2026-10-14
Le tunnel dépend du Mac hôte et les essais entre amis sur appareils distincts
ne sont pas couverts par ce smoke test. Ressources WebGL V48 : framework
`52c8e17f51d094b699f52434979ee40e`, wasm `8679d082b2079b7bed3816a8ff649e01`,
data `6ee52df26daaef5d55d6c537f38ef456`.

Recette serveur Release isolée refaite le 14 octobre :
`smoke_test.py http://127.0.0.1:5093` PASS (`UFCXN4`, six joueurs ; septième et
join/start interdits aux sièges incorrects ; commande autoritaire et snapshot ;
JSON/token invalides rejetés ; succession d’hôte validée). `persistence_test.py`
PASS sur stockage temporaire séparé : jetons de reconnexion hachés, file et
état restaurés après deux redémarrages. Ce test API ne remplace pas les six
clients Unity/WebGL ni une partie humaine ; il n’a pas touché au stockage du
serveur de prévisualisation.

## Lisibilité inspecteur V49 — 14 octobre 2026

Le contrôle V48 en jeu a montré que les badges rendement projetés sur le plateau
restaient perceptibles dans le fond translucide de la fiche inspecteur. La carte
inspecteur utilise maintenant un fond opaque et son UIDocument est dessiné après
le HUD. En WebGL local, la fiche Boston Dynamics Factory reste nette : icône,
famille/niveau, Rent `$400`, Risk `N/A`, ROI `10,0 %` et actions visibles sans
badges de rendement derrière les statistiques. Le test a été fait en 1280 × 720.

Validation Unity 2022.3.62f3 : Economy 106 assertions + Mechanics 99 assertions,
build WebGL Release `BuildOptions.None` réussi. Le build de recette isolé est
dans `/tmp/PolyRiskWebGLValidationFixed`; il n’a pas remplacé ni republié le lien
HTTPS V48. Le rendu est aussi contrôlé en viewports simulés 430 × 900 et
900 × 430 : les trois métriques, `N/A`, flèches neutres et actions restent
lisibles, le plateau est visible en portrait et la fiche reste compacte en
paysage. Le layout paysage a été lu après stabilisation suivant le redimensionnement ;
la console navigateur ne rapporte aucune erreur ni aucun avertissement.
La simulation de viewport a ensuite été remise à la taille normale du navigateur.

Les flèches hausse/baisse, les captures exactes du mockup et les performances sur
iPhone réel restent à vérifier. L’entrée Marketplace et les états de vente sont
recettés sous V51 ci-dessous ; les nouveaux bid live à deux/six clients restent
un contrôle distinct.

## Inspecteur paysage compact V50 — 14 octobre 2026

Pour récupérer de la surface de plateau en orientation paysage, le panneau
inspecteur est plafonné à 60 % de la hauteur des viewports compacts (≤430 px),
avec une vignette de 48 px et des marges/stats resserrées. La variante CSS a été
choisie après un premier essai à 65 % : elle ne libérait qu’environ 30 px ; 60 %
libère environ 52 px par rapport à la limite responsive précédente de 72 %, sans
ajouter canvas, dépendance, animation ou logique de jeu.

Sur le build WebGL Release isolé, une partie locale affiche à 900 × 430 le nom,
famille/niveau, Rent, Risk, ROI, glissière `SLIDE TO BUY` et `PASS` sans coupure ;
la fiche occupe 257 px et laisse environ 82 px de plateau visibles au-dessus.
En 430 × 900, le plateau complet, les trois métriques et les deux actions sont
restés lisibles. Le viewport est revenu à 1280 × 720 et la console JavaScript ne
rapporte aucune erreur. Unity 2022.3.62f3 : Economy 106/106, Mechanics 99/99 et
`WEBGL PLAYER BUILD PASS` (`BuildOptions.None`). Build isolé dans
`/tmp/PolyRiskWebGLValidationFixed`, servi localement sur le port 8917 pour la
recette ; la préversion publique n’a pas été remplacée.

Cette recette ne remplace pas les mesures tactiles/performance sur iPhone réel.

## Marketplace en portrait/paysage V51 — 14 octobre 2026

Pour accéder au parcours Marketplace sans attendre un lancer aléatoire, un build
WebGL Development isolé utilise le composant QA déjà présent dans la scène : F1
place le joueur sur la case 20. Ce build sert uniquement à la recette ; il n’a
pas été publié et ses raccourcis ne sont pas dans le Release.

Dans un parcours local réel, le hub Marketplace s’ouvre depuis la case 20 ;
« Sell a Property » replie le panneau et libère le plateau ; le titre Temu
Sweatshop Terminal peut être sélectionné puis confirmé. L’écran « Call for
Tenders » affiche le nom, le loyer de base et « No offers yet ». En paysage
900 × 430, le bouton « Cancel Auction » était coupé sous le bas de l’écran. En
portrait 430 × 900, la ligne sélectionnée débordait horizontalement et rognait
le bouton de retrait.

Les vues ont été resserrées par USS/UXML seulement : en paysage, le footer de
l’attente tient entièrement dans le panneau ; en portrait, la ligne du titre
est plus compacte, le nom et le loyer restent lisibles, la scrollbar horizontale
parasite disparaît et le bouton de retrait reste entièrement visible. Le bouton
d’annulation a été activé en recette ; il annule l’appel et le tour reprend.
Les IA de cette partie QA ont refusé le titre à loyer nul, puis l’enchère s’est
annulée normalement ; aucun nouveau bid en direct n’a donc été produit.

Validation Unity 2022.3.62f3 : build QA `BuildOptions.Development` PASS ;
Economy 106/106, Mechanics 99/99, Release `BuildOptions.None` PASS. Le Release
local a été rechargé sans erreur JavaScript et sans étiquette « Development
Build ». Release isolé dans `/tmp/PolyRiskWebGLValidationFixed`, QA dans
`/tmp/PolyRiskMarketplaceQA` ; préversion HTTPS V48 non remplacée.

Reste à jouer le parcours avec deux à six clients humains pour vérifier les
offres live et animations réduites, puis mesurer gestes/performance sur iPhone
14 Pro Max. La comparaison face au mockup cible fourni reste ouverte faute de
captures visibles dans les fichiers de cette session.

## Offre Marketplace live à deux clients — V52, 14 octobre 2026

Deux navigateurs WebGL indépendants ont rejoint le salon privé `S2MRE5` depuis
le build Release de recette `PolyRiskSixClientReleaseBuild`. L’hôte a ouvert
un appel d’offres pour Temu Sweatshop Terminal ; l’autre client a envoyé une
offre de 5,04 k$. La liste « Deal List » de l’hôte a affiché en direct l’avatar,
le pseudo de l’enchérisseur et le montant. Le client enchérisseur avait
« Reduce Animations » activé ; le panneau du vendeur a reçu l’offre sans
erreur navigateur. Cela valide la livraison réseau d’une offre réelle dans un
salon à deux clients, mais ne prouve pas à lui seul le rendu de l’animation
chez un spectateur.

Point à reproduire : après l’envoi, l’écran de l’acheteur affichait encore
« Submit Your Bid » avec le curseur revenu à 0 $, alors que le vendeur avait
reçu 5,04 k$. Le code appelle `ShowBidderSpectator()` après l’envoi, mais le
passage effectif à la vue de suivi n’est donc pas confirmé par ce build. La
vérification du spectateur, de l’arrivée d’une deuxième offre et de l’effet
visuel de « Reduce Animations » restent ouvertes ; aucun changement de règle
ou de données n’a été fait pendant cette recette. Salon de QA privé, non
publié.

## Reprise du contrôle du spectateur — V53, 14 octobre 2026

Le correctif UI de réponse locale a été compilé en Development WebGL ; le banc
d’économie a passé 106 assertions. Pour isoler la réplication, un fixture QA
sur boucle locale a installé une vente d’Opioid Paradise dans un lobby privé à
deux clients (hôte et invité). Cette fois, l’invité vendeur a bien affiché
« Call for Tenders » et l’autre client a reçu l’invitation avec un compte à
rebours de 30 secondes. La réplication de l’invitation est donc observée avec
un état de partie autoritaire explicite ; le précédent essai via
`DebugMarketplaceSetup` n’était pas une préparation fiable.

Le parcours Skip reste non validé : après activation visuelle du bouton, le
client acheteur a affiché le formulaire d’offre, puis les activations suivantes
n’ont pas produit de passage stable en spectateur. La capture réseau de l’état
de partie conservait `InvitingPlayers` et une liste d’offres vide. Aucune offre
réelle n’a été soumise et aucune ligne n’a été vérifiée côté vendeur. Il faut
reprendre avec une interaction contrôlée, confirmer la réponse Skip dans l’état
répliqué, puis tester une offre réelle, sa réception par le vendeur et le
maintien du panneau spectateur lors de l’arrivée d’une nouvelle offre. V52–V53
restent ouverts.


## Symptôme Ready initial au lobby — V55, 14 octobre 2026 (requalifié en V58)

Reproduction WebGL Development dans un lobby privé à deux clients (`AKXHCE`).
L’hôte affichait Ready. Après activation du Ready côté invité, l’écran de
l’invité montrait `READY`, mais l’écran de l’hôte conservait `WAITING` et
`START GAME` restait désactivé. La lecture directe de l’état autoritaire du
lobby via le relais QA confirmait `QA Host V55: isReady=true` et
`QA Bidder V55: isReady=false` (version 13), y compris après le cycle local de
Ready et une reconnexion du client invité. Le parcours de démarrage à deux
clients est donc bloqué par un écart entre l’état présenté et l’état serveur.

À ce stade, le client invité utilisait une origine locale sans adresse serveur
explicitement configurée. L’observation montrait bien un état serveur faux, mais
la cause n’était pas encore isolée. La recette de suivi est consignée en V58.
Le test V55 n’a modifié ni règles, ni économie, ni données de jeu. Le Skip
Marketplace V52–V53 reste distinct et non validé.


## Périmètre serveur Ready et accès Unity — V56, 14 octobre 2026

Le test API `Server/PolyRisk.LobbyServer/smoke_test.py` exécuté contre le
serveur local sur 5091 passe : six sièges, préparation Ready de chaque joueur,
démarrage par l’hôte, rejet du 7e joueur, commande réseau et publication/lecture
d’état. Cela valide le contrat serveur Ready/Start, mais ne reproduit pas le
décalage du client WebGL constaté en V55.

Unity 2022.3.62f3 et le module WebGL sont installés via la CLI locale ; aucun
MCP Unity dédié n’est disponible dans cette session. La tentative de build
batch isolé n’a pas obtenu de verdict : aucun processus Editor n’a démarré et
la CLI est restée bloquée sur le broker de licence. Le statut de licence et
l’authentification CLI n’ont pas répondu, et aucun fichier de licence Unity
local n’est présent. Aucun résultat de compilation ou de régression Unity ne
doit donc être déduit de cette tentative.

La prochaine recette de source doit permettre à l’éditeur batch de démarrer
(licence/connexion Unity Hub). Le test runtime V58 ci-dessous s’est fait sur le
build WebGL Development déjà disponible ; il ne remplace donc pas une compilation
des sources actuelles.


## Diagnostic statique des offres IA Marketplace — V57, 14 octobre 2026

La cause des refus sur une propriété non améliorée est déterministe dans
`MarketplaceSystem.EvaluateBidValue` / `SimulateAIBidders` : le modèle projette
5 loyers, soustrait le prix de base puis n’enchérit que si le résultat est
strictement positif. Trump Tower (prix 900 $, loyer de base 90 $) donne une
valeur de -450 $ au niveau 0 (`5 × 90 − 900`) et 0 $ au niveau 1
(`5 × 2 × 90 − 900`). Les deux cas sont automatiquement passés avant même
d’appliquer la probabilité de refus du profil. Le niveau 2 donne 1 350 $ et
peut produire une offre, sous réserve des liquidités et du profil.

Sur les 26 propriétés sérialisées, 22 ont un loyer de base positif : sans
bonus de monopole, le test statique donne 26/26 valeurs négatives au niveau 0,
22/26 valeurs nulles et 4 négatives au niveau 1, puis 22/26 valeurs positives
au niveau 2. Quatre propriétés ont un loyer de base nul
(Temu Sweatshop Terminal, Amazon Prime Drone-Pad, SpaceX Uplink Hub et Silk
Road 3.0 Port) ; le rendement locatif projeté ne suffit donc jamais à les
valoriser. Un bonus de monopole ou Logistics peut toutefois changer certains
cas. Le refus précédent de l’IA sur Temu est donc cohérent avec le code, pas
une preuve d’un problème d’appel réseau.

Cette analyse révèle un biais probable de valorisation, mais ne décide pas
à elle seule d’une nouvelle stratégie d’enchères. L’horizon de 5 tours, les
prix offerts, la valeur des monopoles et l’effet sur la trésorerie doivent être
observés en partie. Conformément au jalon de 5–10 parties humaines de la TODO,
aucune règle, formule économique ou donnée n’a été modifiée ; la validation
par logs runtime et tests humains reste ouverte.


## Ready et démarrage à deux clients — V58, 14 octobre 2026

Retest du lobby `AKXHCE` avec deux clients WebGL Development (`8957` hôte,
`8958` invité) et un relais QA vers l’API locale `5091`. La première tentative
depuis le client invité a échoué car l’écran Connection de cette origine locale
gardait son défaut `http://127.0.0.1:5080`. Après configuration explicite vers le
relais `http://127.0.0.1:5096`, le client invité a rejoint correctement le
salon : 2/6 joueurs, hôte et invité distincts.

Le relais a confirmé que le POST `/ready` de l’invité portait son propre
`playerId`, demandait `isReady=true` et recevait en version 18 un snapshot avec
les deux joueurs `isReady=true`. Après convergence du polling, l’invité affichait
`READY`, l’hôte affichait aussi `READY` pour l’invité et son bouton `START GAME`
était actif. Le démarrage par l’hôte a ensuite fait passer les deux clients sur
la même partie (capital et valeur nette 18 000 $, tour 1/50, marché stable).

Une interaction clavier Space ajoutée juste après le premier test Ready l’a
retourné à `false` : le simple clic avait déjà envoyé `true`, puis Space a envoyé
`false`. Avec un seul clic et le temps de recevoir le snapshot, l’état s’est
synchronisé. Le symptôme V55 n’a donc pas été reproduit après configuration du
serveur; les éléments probants indiquent une confusion du banc de test (adresse
locale manquante et double activation), pas une anomalie Ready démontrée dans le
client. Ce contrôle porte sur le build Development préexistant, pas sur une
compilation Unity fraîche des sources actuelles. Aucune règle, donnée ou formule
économique n’a été modifiée. La vente Marketplace V52–V53 reste à valider.


## Marketplace Skip et banc multi-client — V59, 14 octobre 2026

Sur le build WebGL Development préexistant, un vendeur et un acheteur ont
rejoint le même relais local. Le vendeur a ouvert une vente Marketplace pour
French Riviera et a envoyé une invitation tender ; l’invitation acheteur
affichait bien le vendeur, la propriété et le compte à rebours. L’acheteur a
activé Skip. Le client a brièvement présenté le formulaire d’offre, puis le
serveur a enregistré le passage et l’écran est revenu au plateau. Comme c’était
le seul acheteur, l’appel d’offres s’est clos automatiquement : ce scénario ne
permet donc pas de conclure si Skip maintient durablement la feuille spectateur
pendant que d’autres joueurs enchérissent. Aucune seconde offre ni animation
d’arrivée n’a été observée dans cet essai.

Une nouvelle tentative avec quatre onglets sur le même build a laissé les
clients sur un écran noir après rechargement. Avec trois clients, le lobby
WebGL s’est ensuite formé et son compteur affichait 3/6 ; la troisième ligne
était partiellement masquée (détail ci-dessous). La partie n’a pas été lancée
pendant la mise en place du banc. Cela ne constitue pas une
reproduction d’un défaut du jeu. La suite doit reprendre ce scénario à trois ou
quatre joueurs (vendeur, un joueur qui passe et au moins un acheteur actif),
puis vérifier l’arrivée de plusieurs offres et comparer « Reduce Animations »
activé et désactivé. Le panneau vendeur affichait aussi un tiret à la place du
nom de propriété pendant l’attente ; ce défaut visuel reste à retester.

Sur l’écran de lobby à 3/6 joueurs, la troisième ligne était partiellement
masquée sous la zone fixe des boutons Ready/Leave, avec un contrôle de
défilement pour la liste. Ajouter une vérification d’accessibilité visuelle des
sièges et de l’absence de recouvrement jusqu’à six joueurs.

Les serveurs statiques/relais de test ouverts pour cette tentative ont été
arrêtés et les onglets QA fermés. La partie reste un test d’interface/réseau sur
un build Development préexistant : aucune règle, formule économique ou donnée
n’a été modifiée, et aucune validation de compilation Unity fraîche n’en
découle.


## Lobby compact paysage — V60, 14 octobre 2026

Le build Release WebGL produit par Unity 2022.3.62f3 a passé les 106
assertions économiques, les 99 assertions mécaniques et `WEBGL PLAYER BUILD
PASS`. Sur l’écran du lobby à 3/6 en paysage 1280×720, le roster était d’abord
rogné ; une tentative de flex-basis nul a comprimé la liste à zéro. Le correctif
final conserve une hauteur minimale pour trois rangées, compacte l’en-tête et
les contrôles en hauteur compacte et retire les libellés d’aide redondants.
Contrôle final : les trois noms, Ready, Leave Lobby et Reduce Animations sont
entièrement visibles, sans recouvrement avec le roster.

La nouvelle rangée Guest Three a été introduite dans ce salon de recette par
l’API locale après qu’un troisième client WebGL eut affiché « Unknown Error »
sur Join. L’affichage de trois joueurs est vérifié, mais le parcours Join du
troisième client ne l’est pas ; relever la réponse serveur si l’erreur se
reproduit. Les places 4–6, le défilement du roster et Start côté hôte restent à
contrôler. Lors des rechargements, Unity a présenté un écran noir pendant une
quinzaine de secondes avant de charger le menu ; la persistance et le démarrage
à froid demandent un nouvel essai. Aucun changement de règles, d’économie ou
de données de jeu.


## Reprise de la recette du lobby — V61, 14 octobre 2026

Le défaut « Unknown Error » du contrôle précédent était dû au banc de test :
le relais WebGL pointait vers un port différent de l’API locale par défaut
(5080). Avec le relais et une API Release isolée tous deux alignés sur 5080,
un second navigateur WebGL a rejoint réellement le salon `PB63HA`, et le
snapshot hôte a affiché 2/6. La recette protocole `smoke_test.py` a aussi passé
la création, six sièges, refus du septième, Ready obligatoire, lancement par
l’hôte et réplication initiale d’une commande (`89ZL42`, état version 1).

Pour contrôler le layout hôte à pleine capacité, quatre sièges QA ont ensuite
été ajoutés à `PB63HA` par l’API : le client WebGL a affiché 6/6 et son bouton
Ready a répondu. Le contrôle s’arrête avant Start : les cinq autres sièges
n’étaient pas tous pilotés comme clients WebGL indépendants et le bouton Start
est resté désactivé tant qu’ils n’étaient pas prêts. Le roster visible montre
les premières lignes et une barre de défilement, mais cette session n’a pas
permis de confirmer l’accès visuel complet aux sièges 4–6. La recette humaine
simultanée, le défilement sur iPhone/Safari et le démarrage à six restent donc
ouverts. Aucun changement de règles ou de données de jeu.


## Roster six joueurs et défilement — V62, 14 octobre 2026

Le build Release WebGL frais a passé les 106 assertions économiques, les 99
assertions mécaniques et `WEBGL PLAYER BUILD PASS` sous Unity 2022.3.62f3.
Après chargement du lobby `7GQEFP` à 6/6 dans le navigateur QA en 1280×720,
les trois premières rangées étaient visibles et le ScrollView affichait son
indicateur de défilement. Un glissement de la barre a fait apparaître les
sièges QA Four, QA Five et QA Six entièrement lisibles ; la dernière rangée
reste au-dessus des boutons Ready/Start, sans recouvrement. Le compactage
spécifique aux fenêtres paysage de faible hauteur est conditionné à la hauteur
logique Unity ; il ne s’est pas déclenché dans cette capture desktop et reste à
valider sur petit écran. Le build émet des avertissements de licence Unity CLI
(token/ULF absent), mais s’est terminé avec succès.

Le client hôte et le second client WebGL déjà validé proviennent du test V61 ;
les quatre autres sièges de `7GQEFP` restent des fixtures créées par API. Ce
contrôle prouve donc l’accès visuel aux six sièges via le roster et le join réel
à deux clients, pas le lancement d’une partie par six clients indépendants.
Start n’a pas été activé car les sièges fixture n’étaient pas tous prêts. Les
tests d’animation des rangées et la recette tactile sur iPhone restent ouverts.
Pas de changement des règles, des mécaniques ou de l’économie.


## Reprise protocole à six et transfert d’hôte — V63, 14 octobre 2026

`dotnet build --configuration Release` a réussi sans avertissement ni erreur.
Le smoke existant a validé la création, les six places, le refus du 7e joueur,
Ready obligatoire, le lancement par l’hôte et une commande répliquée
(`MTGZD9`). Le test de persistance existant à deux joueurs a réussi.

Le nouveau `six_player_reconnect_test.py` utilise six identifiants distincts :
chacun retrouve son lobby et le même snapshot de partie après arrêt/redémarrage
du serveur ; les cinq commandes en attente avant checkpoint sont acquittées et
absentes de la file restaurée. Une nouvelle commande est ensuite reçue et
publiée après le départ de l’hôte initial ; le nouveau snapshot de version 2
est identique chez les cinq sièges restants. Tous les trois tests passent.

Cette preuve porte sur HTTP, l’authentification, la file et la persistance. Elle
ne remplace pas six interfaces Unity/WebGL connectées, ne simule pas la latence
WAN et ne valide pas la reprise visuelle après coupure réseau. Pas de changement
des règles de jeu ou des données économiques.


## Marketplace en direct à trois clients et reprise UI — V64–V65, 14 octobre 2026

Trois clients WebGL distincts ont rejoint le lobby `2NHXS9` et démarré une même
partie. L’acheteur siège 0 a passé ; l’acheteur siège 2 a soumis une offre cash
réelle de 3 018 $ sur « Opioid Paradise ». Le snapshot autoritaire version 4
contient `Reviewing`, la réponse skip du siège 0 et l’offre du siège 2. Le
vendeur siège 1 a affiché sa Deal List avec `QA Live Bidder` et 3,02 k$.

Défaut observé sur le build Release précédent : les deux acheteurs gardaient le
formulaire « Submit Your Bid » alors que le vendeur examinait l’offre. Le code
UI ajoute maintenant une synchronisation au démarrage, refuse de rouvrir un
formulaire après une réponse ou en dehors des phases de mise, et resynchronise
la présentation si un client manque l’événement `Reviewing`. Il n’y a aucune
modification des règles, de l’économie ni du protocole.

Le build Release avant cette dernière resynchronisation a passé Economy 106,
Mechanics 99 et `WEBGL PLAYER BUILD PASS`. Le build Development incluant la
resynchronisation a aussi compilé (`WEBGL PLAYER BUILD PASS`). La vérification
visuelle après rechargement ne clôt toutefois pas le défaut : le siège vendeur
a retrouvé sa Deal List, le siège hôte a retrouvé le plateau sans panneau et
le troisième siège a affiché le lobby `PREPARING THE BOARD`. La vue spectateur
du joueur ayant passé et celle de l’enchérisseur restent à confirmer sur une
partie fraîche et un flux live ; vérifier aussi l’animation de nouvelle offre
et « Reduce Animations ». Le chemin de reprise visuelle propre au client hôte
reste à distinguer du passage en spectateur.

## Marketplace en direct à trois clients — V66, 14 octobre 2026

Rejeu sur trois clients WebGL Development frais, salon `EXQ83K`. Le siège 0
a ouvert la vente de « Hydrogen Speedways » depuis le panneau Marketplace. Le
siège 1 a passé ; le siège 2 a proposé 8 427 $ via le formulaire. Le snapshot
autoritaire version 8 confirme `turnState=ResolveTile`,
`auctionState=Reviewing`, l’offre du siège 2 et le skip du siège 1. Le vendeur
siège 0 voit sa Deal List avec l’offre correcte.

Avant correction, échec visuel confirmé sur ce flux actif : après l’entrée en `Reviewing`, le
siège 1 ayant passé conserve `Submit Your Bid` avec une offre à zéro et ses
boutons actifs ; le siège 2 ayant soumis son offre revient au plateau sans
panneau spectateur. Le défaut subsiste plusieurs secondes après l’état
autoritaire `Reviewing`. Après rechargement, le siège 1 revient au lobby
`PREPARING THE BOARD` au lieu de restaurer la partie ; le siège 2 retrouve le
plateau avec les activités de skip et d’offre, toujours sans fiche spectateur.
Le serveur et l’affichage du vendeur sont donc validés ; l’état local/réplication
UI des clients et la reprise de session restent à diagnostiquer.

Aucune règle ni donnée de jeu n’a été modifiée pendant ce test. « Reduce
Animations » était coché sur le siège 1 après rechargement, mais l’absence du
panneau spectateur empêchait de valider l’effet d’entrée et le réglage.

## Correction de la ressource spectateur Marketplace — V67, 14 octobre 2026

L’inspection de `SampleScene.unity` a trouvé que le champ Unity
`marketplace_BidderSpectator` pointait sur le GUID de `Marketplace_Bidder_Form`
au lieu de `Marketplace_Bidder_Spectator`. La scène référence maintenant le
bon asset visuel. C’est une cause directe plausible des vues erronées observées
en V66 ; la validation visuelle sur le flux corrigé reste nécessaire.

Validation Unity 2022.3.62f3 : Economy 106/106, Mechanics 99/99 et build WebGL
Release `BuildOptions.None` PASS. Build isolé :
`/tmp/PolyRiskWebGLV67SpectatorRef` (12.1 MB complet, données gzip 4.8 MB,
WASM gzip 7.4 MB). Aucune règle, formule ou donnée économique n’a été modifiée.
Au moment de la build, restaient à vérifier les deux panneaux invités, la reprise
après rechargement, l’animation d’offre et « Reduce Animations ». Les deux
premiers points sont validés dans le rejeu Development qui suit ; les deux
points motion/accessibilité restent ouverts. Le raccourci de préparation
Marketplace F1 n’est disponible qu’en Development.


### Rejeu de validation visuelle V67 Development — 14 octobre 2026

Après un premier essai interrompu avant la mise, un rejeu frais a abouti dans le
salon `X9CHZV` avec trois clients WebGL Development. Le vendeur siège 0 a mis
« Hydrogen Speedways » en appel d’offres ; le siège 1 a passé et le siège 2 a
soumis une offre réelle de 8 427 $. Le snapshot autoritaire version 5 confirme
`auctionState=Reviewing`, le skip du siège 1 et l’offre du siège 2. Le vendeur
voit la Deal List avec le montant et le nom du meilleur enchérisseur. Les deux
invités voient la fiche d’offre en revue ; le joueur qui a passé voit « You
passed on this auction » et l’enchérisseur voit « Your offer is in the
running ». Le formulaire de mise erroné et la vue nue observés en V66 ne sont
plus reproduits après correction.

Après rechargement du navigateur du joueur enchérisseur, le build Development
recrée sa session et restitue la Deal List, l’offre de 8 427 $ et « Your offer
is in the running ». Le retour à `PREPARING THE BOARD` n’est donc pas reproduit
pour ce client/état. Capture serveur publique expurgée des identifiants de
session : `Reconnect_2026-10-12/market_review_v67_spectator_fix.json`.

La compilation Release V67 reste validée par Unity : Economy 106/106, Mechanics
99/99, `WEBGL PLAYER BUILD PASS`. Le build Development a également passé
`ECONOMY REGRESSION PASS: 106 assertions` et `WEBGL PLAYER BUILD PASS`. Aucune
règle, mécanique ou donnée économique n’a été modifiée. L’animation d’entrée
d’une nouvelle offre et l’effet du réglage « Reduce Animations » sur cette
animation restent à vérifier par une deuxième offre pendant la revue ; la
recette sur iPhone réel reste ouverte.

## Préversion WebGL Release V67 — test entre amis, 14 octobre 2026

Lien HTTPS temporaire : https://secretariat-result-tired-bridal.trycloudflare.com

Le build Unity Release `/tmp/PolyRiskWebGLV67SpectatorRef` est servi en HTTPS
avec son API lobby .NET isolée. La page WebGL a chargé dans le navigateur ; le
salon hôte créé par l’interface est `DEMJM2` (1/6 sièges, hôte Arthur). Deux
clients QA distincts l’ont rejoint par le même code ; le roster hôte a affiché
3/6. Les deux sièges QA ont ensuite quitté et le salon est de nouveau à 1/6,
prêt à recevoir les deux amis. Aucun match n’a encore été lancé entre personnes
réelles.

Pour le test : garder l’onglet hôte ouvert, envoyer le lien et le code `DEMJM2` aux deux amis ; chacun choisit son nom, saisit le code puis appuie sur Join.
Quand les trois sont dans le lobby, l’hôte et les amis passent Ready ; l’hôte
appuie sur Start Game. Le tunnel Quick Tunnel et les serveurs tournent sur ce
Mac : l’ordinateur doit rester éveillé et connecté. L’URL peut changer si le
tunnel est relancé ; cette préversion est destinée au test temporaire, pas à
une publication permanente.

### Revalidation Unity de l’état courant — 14 octobre 2026

L’éditeur Unity 2022.3.62f3 a exécuté les régressions batch sur le source courant :
Economy 106/106 et Mechanics 99/99 PASS. Le build WebGL Release avec
`BuildOptions.None` a abouti (`WEBGL RELEASE BUILD PASS`) dans
`/tmp/PolyRiskWebGLRelease`, taille totale 12 432 KiB. L’index, le loader, le
framework, les données et le WASM répondent tous HTTP 200 ; le menu Release a
chargé dans le navigateur local sans overlay debug. Aucun fichier de règle ou
donnée de jeu n’a été modifié pour cette validation.

Test de rendu simulé séparé : Chromium/Playwright avec viewport 430 × 932,
user-agent iPhone et DPR 3 reçoit tous les assets (HTTP 200) et initialise un
canvas de 860 × 1 864 sans erreur JavaScript. SwiftShader headless est ensuite
resté à forte charge CPU et n’a pas rendu de capture exploitable ; cette
émulation confirme le calcul du buffer, pas la netteté, la cadence ou la chauffe
sur Safari/iPhone. Les services QA isolés ont été arrêtés après le contrôle.

Contrôle complémentaire avec accélération matérielle : Chrome 155 sous macOS,
émulation viewport iPhone 430 × 932/DPR 3, charge le build V67 public et rend le
menu complet sans erreur JavaScript. Le canvas reste à 860 × 1 864 ; capture
`Docs/Validation/UI_Motion_2026-10-13/webgl_chrome_iphone_emulation_v67_2026-10-14.png`.
Cette preuve couvre le rendu dans Chrome émulé, pas Safari ni les performances
sur l’iPhone physique.

Contrôle ciblé relancé sur le même source : `MechanicsRegression.Run` termine
avec 99/99. La couverture inclut la détection des huit familles complètes
(six à trois propriétés, deux à deux), le déclenchement du pouvoir de famille
à l’achat de la troisième propriété, la désactivation après perte d’un titre,
les effets passifs et ultimes, et une OPA abordable/inabordable avec transfert,
conservation du cash et des améliorations. Ce test est un test Unity local de
logique ; il ne prouve pas que chaque pouvoir a été joué dans une vraie partie
en ligne. Le transfert d’OPA en ligne après reconnexion dispose d’une preuve
distincte à deux clients dans la section V11 ci-dessus ; une recette humaine
des pouvoirs et une partie complète restent ouvertes.

La génération d’un projet Xcode iOS a été tentée avec Unity 2022.3.62f3, mais
échoue avant la production du projet : Burst ne trouve pas le SDK `iphoneos`.
`xcode-select -p` pointe vers `/Library/Developer/CommandLineTools`, et
`xcrun --sdk iphoneos --show-sdk-path` confirme que le SDK est absent ; aucun
`/Applications/Xcode*.app` n’est installé sur ce Mac. Il s’agit d’un prérequis
d’environnement, pas d’une erreur C# du jeu. La recette iPhone et le build
iOS restent donc non vérifiés ici.

À 19:35 UTC, recontrôle non intrusif de la préversion : page HTTPS publique
HTTP 200, API locale `GET /` répond `status: ok` sur le port 5093, et les
processus de service web (8930) et lobby (5093) écoutent toujours. Aucun client
n’a été ajouté au salon pendant ce contrôle.

Une tentative QA locale distincte a amené quatre clients à 4/6 dans un salon ;
le pilotage des boutons Ready depuis des onglets cachés n’a pas permis de garder
les quatre participants prêts, donc aucune partie ni animation d’offre n’a été
validée dans cet essai. Ce résultat est inconclusif sur le jeu. Le motion test
d’une seconde offre, l’essai avec animations réduites sur cette offre et la
recette sur appareil réel restent ouverts. Le service et le lien public
`DEMJM2` sont séparés de ce banc local.

## Menu « quitter la partie » — recontrôle de la préversion, 15 octobre 2026

Le serveur web public sert le build Release isolé `/tmp/PolyRiskWebGLQuitMenuFinal` ;
le lien HTTPS répond HTTP 200. Sans toucher au salon de recette, un serveur lobby
éphémère sur un port et un fichier de persistance temporaires a été utilisé pour
rejouer le parcours réseau avec deux sessions distinctes : création, rejoindre,
Ready des deux joueurs, démarrage par l’hôte, abandon confirmé par l’hôte, puis
lecture de l’état `abandoned` par les deux sessions authentifiées. Résultat :
`QUIT MATCH E2E PASS`. Le serveur éphémère et son stockage ont été supprimés à la
fin du test ; le service public et le tunnel sont restés actifs.

Cela confirme la propagation de l’abandon côté protocole et que le lien actuel
sert la version contenant le menu. Cela ne remplace pas le test humain du bouton
dans une partie complète ; la partie à quatre, la recette iPhone physique et les
retours de joueurs restent à faire.

## Dark Board — service Release mis à jour, 15 octobre 2026

Après validation locale, l’API Release a été redémarrée sur le port 5093 avec
le même fichier de persistance (aucun lobby actif avant le redémarrage). Le
service conserve son intégration same-origin derrière `serve_webgl.py` sur le
port 8930. La préversion HTTPS actuelle est
https://cooper-justin-seven-devel.trycloudflare.com/ . La page, le loader, le
framework, les données compressées et le WASM répondent HTTP 200.

Par ce lien, deux sessions ont créé et rejoint un lobby, passé Ready, démarré,
publié un snapshot de contrôle puis lu le même état : l’hôte voit les deux
montants exacts ; l’invité reçoit son montant exact et la tranche MOGUL pour le
cash adverse. Le salon QA a été abandonné après contrôle. Le tunnel Quick
Cloudflare est temporaire ; cette validation ne mesure pas Safari iPhone ni la
performance physique.

Le build a aussi été ouvert dans le navigateur Codex en viewports simulés
430 × 932 puis 932 × 430. Après chargement, le menu et ses commandes Create,
Join, Play Local et Reduce Animations sont restés visibles dans les deux
orientations ; aucun avertissement ou erreur console n’a été relevé. Il s’agit
d’un contrôle Chromium de mise en page, sans mesure du DPR 3, de la netteté,
des performances, des gestes tactiles ni de Safari/iPhone réel.
