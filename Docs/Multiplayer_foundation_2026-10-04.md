# Fondation multijoueur — 4 octobre 2026

## Choix d'architecture

PolyRisk est un jeu de plateau au trafic réseau faible : les clients échangent des
commandes et des états de partie, pas des positions physiques à 60 Hz. Une API HTTP à
états versionnés est donc plus simple et moins coûteuse qu'une réplication de GameObjects.

Les options évaluées :

- Unity Multiplayer Services réduit le boilerplate mais le SDK Sessions actuel cible
  Unity 6, alors que PolyRisk est stabilisé sous Unity 2022.3.62f3 ;
- Photon Fusion supporte Unity 2022.3 mais introduit un compte, un App ID et une dépendance
  propriétaire avant le premier test ;
- Nakama est compatible Unity/WebGL et permet un serveur autoritaire, mais l'environnement
  local n'a pas Docker et son installation retarderait la tranche lobby ;
- un service ASP.NET Core minimal utilise l'outillage déjà installé, fonctionne avec
  WebGL via HTTPS et garde le protocole sous contrôle du projet.

La dernière option est retenue pour la fondation. L'interface côté Unity reste isolée afin
de pouvoir remplacer le transport sans modifier l'UI ni les règles.

## Première tranche livrée

- page d'accueil auto-initialisée, sans nouvelle scène ni câblage Inspector ;
- nom du joueur, création d'une partie, saisie du code et partie locale ;
- lobby privé jusqu'à six joueurs ;
- code de six caractères sans caractères ambigus ;
- état prêt/pas prêt, liste des joueurs et action de lancement pour l'hôte ;
- identifiants de reconnexion conservés localement ;
- polling léger d'un snapshot versionné ;
- transfert automatique du rôle d'hôte si l'hôte quitte le lobby ;
- refus du septième joueur et des arrivées après lancement ;
- mise en attente du moteur local derrière l'accueil pour éviter qu'une partie démarre
  avant le choix du mode.

## Deuxième tranche livrée : protocole de partie

- file générique de commandes authentifiées et ordonnées ;
- contrôle optimiste de version pour refuser une décision fondée sur un ancien état ;
- consommation des commandes et publication du nouvel état réservées à l'hôte ;
- snapshots récupérables après une reconnexion ;
- limites de 32 Kio par commande et 256 Kio par snapshot ;
- quatre opérations typées dans le client Unity : envoyer une commande, relever la file,
  publier un état et récupérer le dernier état.

## Troisième tranche en cours : premier tour réel

- création de deux à six sièges humains à partir de l'ordre du lobby, sans IA de
  remplacement ;
- moteur de règles actif uniquement chez l'hôte et réplique passive chez les invités ;
- HUD relié au siège local et boutons verrouillés pendant le tour d'un autre joueur ;
- commande `turn.roll` envoyée par le joueur actif, vérifiée et appliquée par l'hôte ;
- commandes `action.primary` et `action.secondary` pour résoudre ou passer une case ;
- commandes Audit (payer, tenter les doubles, rester, carte d'évasion) et ajustement
  Automobile −1/0/+1 ;
- commande unique `ultimate.use` pour les huit familles, avec index de cible pour
  Aerospace et Armement ;
- réplication du résultat des dés, de la phase, des soldes et des positions ;
- réplication des propriétaires, niveaux, hypothèques, visites, embargos et sabotages ;
- réplication de l'énergie, des cartes persistantes et des effets temporaires joueur ;
- affichage de la case d'arrivée sur les clients répliques.
- journal d'activité autoritaire inclus dans le snapshot : douze événements récents,
  identifiants ordonnés et restitution des nouveautés sur chaque client ;
- ticker UI Toolkit auto-expirant pour début de tour, dés, arrivée, achat, passage, Audit,
  faillite, famille complétée et activation d'un pouvoir. Une seule ligne est rendue afin
  de préserver la lisibilité du plateau sur mobile.
- commandes Marketplace autoritaires pour passer, appliquer les quatre sanctions,
  démarrer un appel d'offres, répondre hors tour, annuler et sceller le choix du vendeur ;
- état d'enchère inclus dans chaque snapshot : vendeur, propriété, phase, joueurs ayant
  passé, montants et propriétés proposées. Les réponses concurrentes envoyées depuis la
  même version restent ordonnées dans la file du serveur.
- carte Venture en attente répliquée avec un jeton de confirmation : la modale s’ouvre
  uniquement chez le siège actif et l’hôte applique ensuite l’effet ; l’OPA aléatoire,
  son prix, le paiement au vendeur et le transfert de propriété restent autoritaires.
- liquidation répliquée avec dette, créancier et jeton : seul le siège débiteur voit
  l’écran, tandis que ventes d’améliorations, hypothèques, ventes à la banque et abandon
  sont validés par l’hôte. Les snapshots intermédiaires rafraîchissent les actifs et le
  tour ne reprend qu’après règlement complet ou faillite définitive.
- investissements Bourse répliqués avec montant, durée totale et tours restants ; une
  commande dédiée choisit réellement 1/3/5 tours et 1×/5×/MAX Sgo, puis seul l’hôte
  calcule le rendement et le paiement ;
- cycle macro-économique, tours restants et état de marché intégrés au snapshot. Les
  transitions BOOM/CRASH/WAR et les résultats d’investissement alimentent le ticker.

Ce découpage réutilise le moteur de règles Unity déjà couvert par les régressions. L'hôte
est l'autorité de jeu pour la première version. Le serveur garantit l'identité, l'ordre et
la cohérence des versions, mais ne recalcule pas encore les règles économiques : cette
validation côté serveur sera évaluée une fois le schéma complet des commandes stabilisé.

## Validation

- build serveur .NET Release : réussi ;
- test HTTP bout en bout : six joueurs, version de lobby 13, règles du lobby validées,
  commande joueur consommée par l'hôte, snapshot v1 relu par un autre siège et commande
  obsolète refusée ;
- compilation Unity 2022.3.62f3 : réussie ;
- régression mécanique : 54/54 assertions ;
- build WebGL de développement : `WEBGL PLAYER BUILD PASS` ;
- recette WebGL portrait : accueil complet, bouton Join sans débordement, création d'un
  lobby, arrivée d'un second joueur, deux états prêts et lancement par l'hôte ;
- console navigateur après la recette : aucune erreur.
- recette du premier tour réseau dans WebGL : lobby à deux sièges, snapshot initial v1
  (`Planning`, positions 0/0), commande 1 envoyée depuis le bouton réel, jet 2+5,
  déplacement en case 7 et snapshot v2 (`ResolveTile`) relu avec les identifiants du
  second siège ; console navigateur sans erreur ;
- recette complète sur le build final : double 4, achat d'Opioid Paradise en case 8,
  nouveau lancer automatique, Venture résolue, puis snapshot v4/commande 3 avec joueur 1
  actif, solde hôte à 16 900 $ et propriété de la case 8 conservée ;
- commande `turn.roll` envoyée directement avec les identifiants du second siège : double
  1, position 2 et snapshot v5/commande 4, puis deux résolutions de case et fin effective
  de son tour au snapshot v7 ; aucune erreur dans la console navigateur ;
- découverte puis correction d'une compression verticale de l'accueil en paysage et
  de la largeur minimale implicite du champ code qui poussait JOIN hors de la carte ;
  build final vérifié en portrait 430×840 et paysage 1280×720, sans chevauchement ;
- régression Unity étendue au choix de rester en Audit juste après l'arrivée : 55/55
  assertions, ce qui élimine un blocage local découvert pendant le branchement réseau ;
- build serveur .NET Release final : zéro avertissement, zéro erreur ; smoke test final
  lobby + protocole : six joueurs, version lobby 13 et état de partie v1.
- build WebGL après ajout des quatre décisions Audit, de l'Assistance de Précision et des
  huit Ultimes : `WEBGL PLAYER BUILD PASS` ; compilation Unity : 55/55 assertions.
- validation du journal d'activité : ordre des identifiants, aller-retour JSON dans le
  snapshot, achat et déblocage de famille couverts ; régression Unity 59/59, build WebGL
  réussi, ticker vérifié visuellement en desktop 1280×720 et portrait 430×840.
- validation Marketplace : commandes sanctions et état complet d'appel d'offres couverts
  par sérialisation, effets économiques et feedback partagés conservés ; régression Unity
  62/62, build serveur .NET Release sans avertissement et build WebGL réussi ; accueil
  rechargé dans le navigateur sans erreur console.
- validation Venture/OPA : attente de confirmation sérialisée, transfert économique et
  feedback partagé couverts ; régression Unity 64/64 et compilation serveur inchangée.
- validation faillite : état en attente sérialisé, remboursement à 50 %, protection contre
  le double remboursement, transfert du cash restant et restitution des titres couverts ;
  régression Unity 70/70, build WebGL réussi et écran recetté en 430×840 et 1280×720. Dette,
  cash, état des actifs et confirmation en deux temps restent visibles sans chevauchement.
- validation investissements/cycles : plafonds économiques, commande et snapshot,
  remplacement d’état réplique et cycle CRASH couverts ; régression Unity 76/76. Le panneau
  Bourse a été recetté en 430×840 et 1280×720, placement 1×Sgo exécuté ($18K→$16K), build
  WebGL réussi et console navigateur sans erreur.
- durcissement serveur : stockage JSON atomique, restauration des lobbies/commandes/états,
  jetons de reconnexion hachés sur disque, validation JSON, allow-list du protocole,
  CORS local par défaut et configurable, limites de débit, taille HTTP bornée, en-têtes
  défensifs et journaux structurés. Build .NET Release sans avertissement ; smoke test à
  six joueurs, test CORS/rate limit et reprise authentifiée après deux redémarrages réussis.

## Prochaine sous-étape

Exécuter une recette de partie complète à deux puis six clients WebGL, en injectant latence,
reconnexion et coupure d’un invité puis de l’hôte. Inclure Marketplace, Venture/OPA,
faillite, Ultimes, investissements et changement de cycle. La mise en ligne devra fournir
le domaine CORS réel, un volume persistant, le hostname autorisé et la terminaison TLS.

### Préparation de la reprise autoritaire — 8 octobre 2026

Le snapshot capture désormais `consecutiveDoubles` et `players[].turnsStarted`.
L’application restaure la série de doubles dans le moteur de tour, et les compteurs
individuels dans les modèles joueurs. Les tests de règles vérifient le droit à une
relance après restauration d’un double et la fin de tour après un jet normal :
`MECHANICS REGRESSION PASS: 80 assertions`.

Cette préparation ne constitue pas encore une reconnexion fonctionnelle de l’hôte.
Le démarrage autoritaire reste à modifier pour charger l’état serveur avant la boucle.
Les callbacks de dette et les coroutines de mouvement/ajustement doivent être reconstruits
pour éviter une seconde facturation ou un état en attente permanente.


### Continuations de dette — préparation du 9 octobre 2026

`CompleteRentSettlement` et `CompleteGoSettlement` terminent un règlement déjà
encaissé sans répéter le paiement. `OnInsolvency` transporte un contexte explicite
jusqu’au snapshot : type rent/go, case du loyer, détourneur éventuel et solde du
créancier avant règlement. L’application autoritaire reconstruit la continuation
loyer en utilisant cette même fonction. Les anciens snapshots sans contexte ne
permettent pas de reconstruire la continuation : aucun succès de reconnexion
n’est revendiqué pour ces états.

La reprise du GO doit encore reconstruire la fin du mouvement, les durées des
bonus et l’arrivée sur la case. Le démarrage de l’hôte doit charger le snapshot
avant de reprendre la boucle ; les tests à deux/six clients restent nécessaires.
La version WebGL exposée au test iPhone n’est pas remplacée par ces modifications.

Vérification préalable à l’ajout du contexte : Unity Mechanics 80 assertions,
Economy 30 assertions, dont absence de second débit lors de la continuation,
conservation de la trésorerie avec détournement de 25 %, et fin du GO sans nouveau
revenu. Logs : `/tmp/polyrisk_debt_continuations.log` et
`/tmp/polyrisk_debt_economy.log`. Validation du contexte terminée : Mechanics 81 assertions et Economy 30
assertions, sessions Unity terminées avec code 0. Logs :
`/tmp/polyrisk_debt_context_mechanics.log` et
`/tmp/polyrisk_debt_context_economy.log`. La première session signale des erreurs
internes Burst dans les packages Unity (assemblage Smash / linker natif manquants),
malgré l’exécution réussie des tests C#. Cela ne valide pas un build natif/Burst ;
ce point devra être contrôlé lors de la prochaine compilation de cible.


### Reprise d’une dette DÉPART — 9 octobre 2026

`ResumeGoDebtSettlement` termine la dette puis utilise la même fin de déplacement
que le parcours normal : recharge d’énergie, décrément des bonus, Transit Express,
notification d’arrivée et passage à ResolveTile. Elle ne répète ni crédits/débits
DÉPART, ni inflation, ni compteur de tours de plateau. Le total de dés en attente
est effacé. Une seconde invocation après arrivée est rejetée par l’état de tour.
L’application d’un snapshot autoritaire reconstruit cette continuation pour une
dette de type go.

La boucle de tour attend tant qu’une dette réseau est en cours, pour ne pas
réexécuter une facture de loyer lors de la reprise d’un état ResolveTile. Le poll
de commandes reste indépendant et peut recevoir les décisions de liquidation.

Première régression Unity : Economy 34 assertions (`/tmp/polyrisk_go_resume.log`).
La version finale incluant remise à zéro des dés et garde de boucle passe Economy
34 assertions et Mechanics 81 assertions, sessions terminées avec code 0. Logs :
`/tmp/polyrisk_go_resume_final.log` et `/tmp/polyrisk_go_resume_mechanics.log`. Les erreurs internes Burst/Smash du clone persistent dans le
premier log ; les résultats portent sur les règles C#, pas sur un build de cible.
Le démarrage restauré de l’hôte et la recette réseau réelle restent ouverts.


### Démarrage autoritaire restauré — 10 octobre 2026

Tous les clients démarrent avec une boucle différée, y compris l’hôte.
`InitializeAuthority` lit d’abord l’état serveur. Une version positive est appliquée
avant de reprendre la boucle ; `StateVersion` et `lastAppliedCommandSequence` sont
repris, et les douze dernières activités sont conservées (l’UI n’en rejoue que trois).
Pour une version zéro, la boucle neuve démarre puis publie v1. En cas de réponse
perdue, le serveur est relu avant une nouvelle tentative de publication. Une erreur
réseau ne déclenche pas de partie neuve sans lecture préalable réussie.

Compilation / règles Unity : Mechanics 81 assertions, code 0
(`/tmp/polyrisk_authority_startup.log`). Régression ciblée de restauration : Economy 38 assertions, code 0
(`/tmp/polyrisk_authority_restore.log`), couvrant version/cash/position, séquence
acquittée, conservation des douze activités et rejet d’un état illisible.
Cette validation n’est pas une recette HTTP multi-client : les états AdjustDice,
MovingPawn hors dette et victoire, ainsi que les décisions de panneaux restaurées,
restent à traiter/vérifier. Aucun nouveau WebGL n’a remplacé le test mobile existant.


### Reprise du choix Automobile — 10 octobre 2026

Le parcours normal et l’état AdjustDice restauré partagent désormais
`WaitForAutoPrecisionChoice`. Après un choix restauré, HandleTurnState reprend
ExecuteMovement avec le total du snapshot. Un RollDice restauré qui contient déjà
un total reprend ce déplacement au lieu de lancer de nouveaux dés ; cela couvre
le choix acquitté juste avant une interruption. Le total est enregistré pour tout
jet accepté, même sans monopole Automobile, puis effacé à la fin du mouvement.

Régression Unity : Economy 41 assertions, code 0
(`/tmp/polyrisk_precision_resume.log`), dont attente du choix, confirmation sans
modification du total ou des doubles, rejet d’une seconde confirmation. La recette réseau et la
reprise de MovingPawn hors dette restent ouvertes. Le WebGL de test mobile est
conservé tel quel.

Régression des mécaniques après ce changement : Mechanics 81 assertions, code 0,
`/tmp/polyrisk_precision_mechanics.log`. Aucun échec C# ni exception relevés dans
les deux logs de ce changement. La couverture actuelle vérifie le choix conservé
(delta 0) en C# ; -1/+1, timeout et mouvement via clients restent à recetter.


### Destination et phase de mouvement — 10 octobre 2026

Le snapshot contient maintenant `movement` : départ, destination et indicateur des
économies DÉPART déjà appliquées. ExecuteMovement et MovingPawn restauré utilisent
`ApplyPendingMovementEconomics` : position, compteur de passage, inflation, revenus
et entretien. L’indicateur empêche de les répéter. Les bonus et l’arrivée restent
centralisés dans la fin de déplacement partagée. L’état de mouvement est effacé
à l’arrivée et au démarrage d’une nouvelle partie. La reconnexion replace le pion
sur sa destination plutôt que de rejouer une animation partiellement effectuée.

La reprise de MovingPawn sans dette est branchée sur cette continuation. Les
anciens snapshots MovingPawn sans destination ne permettent pas de reconstruire
le mouvement et restent en attente avec un diagnostic explicite. Une dette GO
ancienne pourvue de son contexte conserve la continuation de règlement décrite
plus haut. Les nouveaux champs sont additifs dans le schema 1.

Première régression C# : Economy 48 assertions, Mechanics 81 assertions, codes 0
(`/tmp/polyrisk_movement_resume.log`, `/tmp/polyrisk_movement_mechanics.log`).
Couverture : passage DÉPART une seule fois, copie indépendante de l’état,
sérialisation, validation des bornes de case et effacement à l’arrivée. La régression
complémentaire de la coroutine de reprise avec/sans passage GO passe Economy
49 assertions, code 0 (`/tmp/polyrisk_movement_resume_final.log`). Elle vérifie
l’arrivée, l’absence de second paiement GO, le nettoyage de l’état et l’absence
de salaire/inflation/décrément de bonus lorsque le mouvement ne passe pas par GO.
La recette réseau à deux/six clients et le build WebGL intégrant ces changements
restent nécessaires ; la version mobile de test n’est pas remplacée.


### Résultat de partie restauré — 11 octobre 2026

Le snapshot transporte `isGameOver`, `winnerPlayerId` et `victoryReason`.
DeclareVictory conserve ces données ; leur restauration force EndTurn, efface le
mouvement/dés en attente et arrête les coroutines. L’événement OnGameVictory est
émis uniquement si le résultat change, pour reconnecter l’écran existant sans le
réafficher à chaque poll. Une nouvelle partie nettoie le résultat précédent.

L’hôte ne reprend pas la boucle d’une partie terminée. Les commandes locales sont
ignorées, et les commandes réseau restent acquittées mais ne sont plus exécutées.
Un gagnant inconnu est rejeté avant l’application des modèles ; -1 est autorisé
pour le résultat sans survivant.

Régression C# après restauration des dépendances : Economy 55 assertions et
Mechanics 81 assertions, codes 0. Logs durables :
`/Users/arthurbarreau/.codex/validation/PolyRiskNetwork/victory_economy.log` et
`victory_mechanics.log`. Couverture : résultat/gagnant/raison, événement unique,
résultat sans survivant, rejet d’un gagnant inconnu et nettoyage au nouveau départ. L’affichage
réel de l’écran et le blocage des actions après reconnexion à deux/six clients
restent à recetter. Les anciens snapshots sans résultat ne peuvent pas prouver
que leur partie était terminée ; aucune reconstruction heuristique n’est faite.


Incident de validation du 12 octobre : les premières sessions victoire ne sont pas
exploitables. Le clone temporaire avait perdu PlayerModel.cs et plusieurs sources
du PackageCache (CS2001 puis erreurs de types en cascade). Les fichiers existent
dans le projet de travail. Assets et PackageCache du clone ont été resynchronisés
avant une nouvelle exécution ; les logs initiaux `/tmp/polyrisk_victory_restore.log`
et `/tmp/polyrisk_victory_mechanics.log` ne constituent pas une validation.

Le manifeste temporaire avait aussi été remplacé par une liste de packages par
défaut, sans URP. Le second essai échouait dans PolyRiskLightingSetup.cs, absent
jusqu’à la restauration complète d’Assets. Packages a ensuite été resynchronisé
avec le projet (URP 14.0.12 et dépendances MCP/éditeur), puis les régressions ont été
relancées dans les logs `polyrisk_victory_*_dependencies.log`. Unity ajoute certains
packages d’éditeur par défaut au manifeste du clone ; le manifeste source reste
inchangé. Ces incidents concernent le clone de validation et ne prouvent pas un
échec des modifications de victoire.


### Préparation du WebGL réseau — 12 octobre 2026

Le clone Unity est conservé dans
`/Users/arthurbarreau/.codex/validation/PolyRiskUnityReconnect`, avec un lien de
compatibilité depuis son ancien chemin /tmp. Le builder accepte
`POLYRISK_WEBGL_OUTPUT` ; sans cette variable, le chemin de sortie reste inchangé.
Le build réseau utilise le dossier durable `PolyRiskWebGLReconnect` et ne remplace
pas le WebGL mobile servi sur 8765/8767.

Le premier essai a été interrompu explicitement : les ProjectSettings du clone
avaient été réinitialisés (pipeline vide et template APPLICATION:Default).
ProjectSettings a été resynchronisé avec le projet, puis le build relancé avec
URP et PROJECT:PolyRiskResponsive. Log en cours :
`/Users/arthurbarreau/.codex/validation/PolyRiskNetwork/webgl_reconnect_configured.log`.
Aucun succès WebGL ni recette navigateur n’est encore confirmé pour ce build.

La vieille instance serveur 5080 accepte debug.cheat et échoue au smoke test
actuel. Le serveur reconstruit depuis les sources compile Release sans avertissement
ni erreur et tourne séparément sur 5091 avec un stockage dédié durable ; le smoke
HTTP à six joueurs passe (code de recette CEBVYW, state_version 1). La recette
Unity réseau doit donc utiliser 5091, et non l’ancien processus. Le WebGL ancien
reste accessible localement ; le fichier wasm répond HTTP 200.


### Pioche Venture et défausse — correctif préparé le 12 octobre 2026

L’audit des continuations a trouvé un état absent du snapshot : la pioche et la
défausse Venture. Le correctif source ajoute `ventureDeck` et copie les listes,
avec validation de la partition des vingt identifiants (1 à 20, sans duplication
ni perte). Un deck invalide est refusé avant modification des soldes joueurs.
Les snapshots anciens sans deck restent lisibles mais leur ordre de cartes passé
ne peut pas être retrouvé. Aucun succès de recette de deck n’est encore revendiqué.

Les régressions préparées vérifient l’ordre après cinq tirages, la sérialisation,
le refus d’un doublon sans altérer le deck, le remélange d’une pioche épuisée et
l’application via la restauration autoritaire. Elles doivent être synchronisées
au clone et exécutées après la compilation déjà en cours.

Le premier build réseau en cours est une base de recette des continuations,
produite avant ce correctif de deck. Il ne faut pas le présenter comme validant
les nouvelles sources Venture ; un second build sera nécessaire pour ce correctif.
Le serveur statique de recette tourne sur 8770 à 8775, mais seule la page HTML
partielle répond actuellement : le player WebAssembly n’est pas encore complet.


### Recette réelle et correctifs supplémentaires — 12 octobre 2026

La recette détaillée et ses preuves sont dans `Validation/Reconnect_2026-10-12`.
Les continuations de base compilent et la reconnexion Planning a été vérifiée à
deux navigateurs. La recette a trouvé la non-publication des changements autonomes.
Le correctif suit des générations de mutation via événements ; il publie uniquement
aux décisions stables ou dettes, sans sérialiser le plateau chaque frame. Une seule
publication reste en vol. Le résultat capturé est réessayé après échec/réponse
perdue, puis le serveur est relu pour confirmer un commit sans réexécuter le paiement.

Venture et mouvement ont désormais une présence explicite dans le DTO, car Unity
peut sérialiser un champ de classe nul comme un objet vide. Régression Economy
69 assertions et Mechanics 81, codes 0 ; logs `autonomous_checkpoint_lifecycle.log`,
`checkpoint_mechanics.log`, `checkpoint_source_dependencies.log` dans le dossier
`.codex/validation/PolyRiskNetwork`. Les dépendances du clone correspondent désormais
exactement au projet. Le build V2 réussit et produit un wasm de 51 Mo contre 65 Mo
pour le clone précédent avec packages supplémentaires.

Lobby polling passe à 5 s en jeu, 1 s avant lancement ; les snapshots restent à
1 s côté réplique et la queue à 0.25 s côté hôte. Le budget de base à six clients
sur une IP passe d’environ 900 à 612 requêtes/minute, avant commandes/publications,
ce qui laisse une marge sous le plafond de 900. Mesure réelle à six clients à faire.

La recette V2 confirme publication automatique et achat unique malgré réponse
perdue. Elle trouve aussi un reset de sélection du second joueur causé par l’ordre
des événements UI. Le correctif source et ses tests sont en cours, non intégrés V2.


Le correctif d’ordre des événements passe Economy 71 assertions (code 0,
`replica_selection_corrected.log`) après correction du nom Go_Start dans le test.
Un V3 distinct est lancé : sortie `PolyRiskWebGLReconnectV3`, log
`PolyRiskNetwork/webgl_reconnect_v3.log`. Il comprend aussi des lignes de lobby
compactes à 36 px et un viewport minimum de 84 px. Ces deux correctifs ne sont
pas encore vérifiés dans le navigateur. Les deux clients V2 de recette ont été
fermés après archivage des captures, pour éviter de conserver un client bloqué
et libérer de la mémoire avant le test à six.
