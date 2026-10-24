# Résilience du protocole réseau — 15 octobre 2026

## Résultats exécutés sur la source actuelle

Le serveur ASP.NET Core a été recompilé en Release : zéro avertissement et zéro
erreur. Les trois scénarios existants passent sur des instances locales
isolées :

- smoke du protocole avec six joueurs : plafond à six, refus du septième,
  Ready/Start, commande autoritaire, snapshot et validations négatives ;
- redémarrage du serveur : identifiants hachés, queue et état restaurés ;
- reprise six joueurs : six sessions récupérées après redémarrage, transfert
  d’hôte et livraison d’une commande après la reprise.

## Nouvelle commande et nouvelle tentative

Après audit du parcours invité, une commande dont la réponse échouait avant
publication laissait le verrou local actif : le joueur ne pouvait pas refaire
son action dans la même version. Le client réutilise désormais le même
identifiant de commande lors de trois tentatives réseau (timeouts, 429 et
erreurs 5xx incluses), puis libère le verrou si elles échouent. Le serveur
reconnaît un identifiant déjà commité et renvoie la même séquence, sans ajouter
une seconde commande ; réutiliser cet identifiant avec un contenu différent
est rejeté. Les reçus compacts sont persistés et plafonnés à 512 par salon.

Vérifications exécutées : build serveur Release (0 avertissement/erreur),
smoke isolé six joueurs (rejeu renvoie la même séquence et contenu altéré
rejeté), persistance après redémarrage, reprise/transfert d’hôte à six,
Economy 114/114, Mechanics 110/110 et build Unity WebGL Release `BuildOptions.None`
PASS. L’artefact local isolé fait 12 MiB.

## Rejeu d’une commande après panne avant commit

Avec ce build Release, l’hôte a créé le salon depuis l’interface, puis un invité
piloté par l’API a rejoint, passé Ready et lancé la partie. Au clic « Roll
Dice », le relais QA a rejeté la première requête avant de la transmettre au
serveur. La réponse synthétique n’incluait pas l’en-tête CORS, donc le navigateur
l’a remontée à Unity comme erreur de connexion (code HTTP inaccessible), un cas
que le client traite comme réessayable. La seconde tentative a reçu 200 ; le
client a lancé le dé et affiché French Riviera. L’état serveur confirme la
version 2, la séquence appliquée 1, la position 6 et l’activité « arrive sur
French Riviera » : une seule commande a été commise après l’échec initial.
Capture : [état du client après reprise](NetworkRetry_PreCommit_WebGL_2026-10-15.png).

Le test API distinct a reçu le 503 synthétique explicitement, vérifié que la
queue serveur était vide, puis rejoué la même commande : la réponse et le
rejeu ont conservé la séquence 1, avec une seule entrée dans la queue. Cela
valide le dédoublonnage serveur et la reprise du client hôte en WebGL sur une
erreur de transport. L’invité était piloté par l’API, sans UI Unity. Le cas où
le client lit un vrai statut 503, les pertes/délais en plein tour avec deux
clients rendus et la reprise à six restent à tester.

## Trois refus HTTP 503 puis reprise dans deux clients WebGL rendus

Recette exécutée le 15 octobre sur un serveur isolé et deux onglets WebGL
locaux (hôte et invité). Le relais QA a renvoyé trois vrais HTTP 503 avant
transmission, avec CORS limité aux origines loopback, pour que le navigateur
ne masque pas le statut. L’hôte a affiché « No server confirmation. Retry the
same action to resync. » ; avant la reprise, l’état serveur restait à la
version 1, séquence 0, avec zéro commande acceptée.

Un seul clic supplémentaire sur « Roll Dice » a été envoyé. La séquence est
passée à 1 et le serveur n’a conservé qu’un reçu. Le journal partagé apparaît
sur les deux clients. Le jet 2+2 a déclenché le relancer prévu par la règle des
doubles (4+3), et le joueur a ensuite payé 600 $ à la Taxe Douanière avant
d’arriver sur Fentanyl Labs ; son solde serveur est de 17 400 $, celui du
second joueur reste 18 000 $. Ce second jet fait partie de la résolution
normale du double dans cette commande, pas d’un doublon de requête.

Résultat : refus pré-commit, erreur 503 visible, action déverrouillée, nouvelle
tentative manuelle et réplication à deux clients PASS ; une seule commande
acceptée. La réponse HTTP 503 du relais QA a été rendue lisible par un petit
changement au banc local uniquement ; aucun code de jeu, règle ou donnée
économique n’a été modifié. Il reste à simuler une déconnexion en cours de tour
et la reprise, puis à répéter la recette avec six clients WebGL rendus.

## Réponse de publication perdue après commit

Un relais QA local a supprimé une réponse HTTP de publication **après** que le
serveur a écrit le snapshot. Dans une partie à deux sièges, un invité a envoyé
`turn.roll`, l’hôte l’a consommée, puis le relais a coupé la réponse du
snapshot. La relecture directe sur le serveur a retrouvé la version 1, la
séquence appliquée 1 et le payload exact ; la commande avait été retirée de la
queue. Une répétition avec l’ancienne version a été rejetée comme obsolète.

Cela correspond au chemin de récupération de
`OnlineGameController.PublishCapturedState` : après échec de publication, il
relit l’état et accepte le commit si version, séquence et payload correspondent,
sans réexécuter l’action. Résultat du banc :

`LOST PUBLICATION RESPONSE RECOVERY PASS: commit survived dropped response; state readback matches payload/version/sequence; command was not replayed; duplicate publish rejected as stale.`

Cette preuve couvre le serveur et le scénario HTTP simulé ; elle ne remplace pas
l’observation du client Unity/WebGL pendant cette coupure. Les délais réseau,
la perte d’une commande avant commit, la déconnexion en cours de tour depuis un
client rendu, puis la reprise sans action double restent à vérifier en WebGL.

## Récupération dans le client WebGL

Sur le build WebGL Release isolé `/tmp/PolyRiskCompactRosterFix`, l’hôte a créé
le lobby `XPTVUM` depuis le menu Unity. Un second siège QA l’a rejoint et les
deux joueurs sont passés Ready. Le relais local a coupé la réponse de la
publication du snapshot initial après son commit. Le client hôte a relu l’état
et a atteint le plateau au tour `1/50`, avec `ROLL DICE` affiché. Le serveur
confirme une seule publication (version 1, séquence 0) et un snapshot à deux
joueurs ; le drapeau de coupure avait été consommé.

Ce test démontre la récupération de l’hôte Unity/WebGL après perte de réponse
au démarrage et évite une double publication initiale. Le second siège était
piloté par l’API QA et n’avait pas d’interface Unity. Un tour complet sous
coupure, une commande perdue avant commit, deux clients Unity rendus et la
déconnexion/reprise en cours de tour restent à tester.

## Reconnexion de l’hôte avec une décision de propriété en attente

Sur le build WebGL Release servi localement, deux navigateurs indépendants ont
rejoint le même salon. Après le premier lancer, le serveur était à la version
2 / séquence 1 : l’hôte était à la case 6 (French Riviera), à 18 000 $, avec
la décision d’achat en attente. J’ai rechargé seulement son onglet. Le client
a repris automatiquement le même lobby et a restauré la carte French Riviera
et son bouton « Slide to Buy » ; l’invité est resté en état d’attente sur la
même case et le même événement.

L’achat confirmé après reconnexion a avancé le serveur une seule fois (version
3 / séquence 2) : French Riviera appartient à l’hôte, son solde est 17 000 $,
la queue est vide, deux reçus couvrent les deux actions du tour, et le journal
contient exactement un événement d’achat. Le tour a repris normalement chez
l’invité. Résultat : coupure/rechargement du navigateur hôte en pleine décision,
restauration de l’action en attente et reprise sans double achat PASS à deux
clients rendus. Cette manipulation simule un rechargement navigateur ; elle
ne mesure pas la durée d’une vraie coupure réseau.

Il reste à combiner les perturbations réseau avec six clients WebGL rendus et
à mesurer la reprise après coupure prolongée / reconnexion mobile réelle.

## Révalidation six sièges et idempotence après le test entre amis

Le 15 octobre, un serveur local QA séparé a accueilli six identités distinctes.
Chaque joueur a rejoint et passé Ready, l’hôte a démarré la partie, puis deux
soumissions identiques de `turn.roll` avec le même identifiant de commande ont
renvoyé la séquence 1. Le poll de l’hôte ne contient qu’une commande ; après
publication de l’état version 1, les six sessions lisent cette même version.
Le test a utilisé une base temporaire vide et n’a pas touché au service ni au
salon de préversion. C’est une preuve protocole/API, en complément des six
clients WebGL ayant déjà chargé et joué normalement dans
`SixPlayer_WebGL_2026-10-15/README.md` ; la perturbation réseau simultanée avec
six interfaces rendues et la coupure prolongée restent à effectuer.

Le banc intégré affichait le menu Unity dans un viewport de 992 × 720 px, où
la commande Rejoindre était hors cadre ; cela confirme la limite de cette
fenêtre de test, pas une panne du service. Le serveur QA et les deux onglets
créés pour cette tentative ont été arrêtés/fermés après le test.

## Perte de réponse après commit avec six sessions

Sur un serveur Release isolé et une base vide, six sessions ont rejoint,
passé Ready et démarré. Une commande `turn.roll` d’un invité a été acceptée ;
sa première réponse a été volontairement ignorée côté banc. Le rejeu exact
avec le même `clientCommandId` a renvoyé la séquence 1. Le poll autoritaire
contient une seule commande, et après publication du snapshot version 1, les
six sessions lisent cette version. Résultat : dédoublonnage et convergence
après perte de réponse PASS au niveau HTTP/API à six sessions. Ce scénario
n’injecte pas la perte dans le navigateur Unity ; les perturbations avec
six interfaces rendues et une coupure prolongée/mobile restent ouvertes.

## Tentative de recette réseau à six clients WebGL

Le 15 octobre, six pages WebGL ont été ouvertes sur six origines locales
indépendantes (un hôte et cinq invités). Le chargement WebGL est passé, mais
la recette combinée avec injection de panne n’a pas pu commencer : la fenêtre
Codex In-app Browser fournissait un viewport de 995 × 720 px et la rangée
« code + rejoindre » débordait horizontalement, laissant le bouton Join hors
de la zone cliquable visible. Le salon QA n’a donc accueilli que l’hôte ; aucun
résultat à six clients n’est revendiqué. Les onglets et les serveurs locaux ont
été fermés. Il faut relancer cette recette dans un navigateur desktop plein
format ou depuis le setup six clients validé précédemment, avec le relais de
faute activé.

Un essai complémentaire en Chromium headless avec accélération GPU désactivée
n’a pas atteint le lobby : seuls deux clients avaient fini l’initialisation
avant que le troisième reste bloqué au chargement pendant plus de trois
minutes ; le processus SwiftShader montait à plus de 300 % CPU. Aucun clic de
jeu ni aucune commande réseau n’a été envoyé par cet essai. Ce comportement
mesure la limite du banc logiciel local, pas la performance WebGL accélérée du
jeu ; je ne le compte ni comme échec du jeu ni comme recette à six.

Une reprise du banc headless le 15 octobre a chargé les écrans d’accueil, mais
le focus clavier/souris entre les deux champs Unity n’était pas fiable : les
tentatives de saisie ont envoyé un code vide ou partiel et aucun invité n’a
rejoint le salon QA. Aucun lancer, commande de jeu ni drapeau de perte de
réponse n’a été déclenché. Le résultat est inconclusif pour le jeu et confirme
que ce scénario doit rester dans le navigateur accéléré qui a déjà validé les
six clients et les trois refus 503.

Contrôle complémentaire en Chrome 155 accéléré par Intel Metal : le formulaire
Unity hôte a créé un salon, puis un second client WebGL a rejoint depuis son
propre onglet ; l’UI invitée a affiché le code et `2/6 PLAYERS`, et l’API a
répondu 200. L’onglet hôte avait été fermé avant cette vérification, donc les
deux clients n’étaient pas simultanément actifs et la partie n’a pas démarré.
La reprise combinée à six n’a pas été atteinte : le focus automatisé des champs
et la soumission Join n’ont pas été assez stables pour conserver un nom unique
dans chaque contexte. Aucun lancer ni perte de publication n’a été injecté.
Le contrôle WebKit demandé pour l’émulation Safari n’a pas démarré, car le
binaire Playwright WebKit n’est pas installé ; aucun navigateur n’a été ajouté.
Les services et données de ce banc sont isolés et arrêtés.

Aucun code de jeu, règle ou donnée économique n’a été modifié par cette recette.

Une dernière tentative de compléter la perte post-commit avec six vues rendues
a été faite sur un nouveau serveur, des données vides et le même build Release.
Chrome Metal a chargé les pages ; le focus Unity UI Toolkit est toutefois resté
sur le champ du code quand l’automatisation a voulu saisir le pseudo. Le serveur
a donc créé des salons QA à un siège au lieu de réunir les six clients. Aucun
joueur n’a passé Ready, aucune partie n’a démarré et le drapeau de perte de
publication n’a pas été posé. Cette tentative confirme une limite du banc de
saisie, sans résultat de jeu exploitable. Les services et ports QA ont été
arrêtés ; aucune modification du build n’a été faite.

## Trois 503 puis reprise avec six interfaces WebGL accélérées

Recette locale sur le build Release `/tmp/PolyRiskFinalCheckpoint_20261015`,
Chrome 155 sur six contextes isolés de 1280 × 800, six origines WebGL distinctes
et un relais de panne local. Le salon `LE59FY` a atteint 6/6 ; tous les joueurs
ont passé Ready et l’hôte a démarré. Les six pages ont chargé et le jeu a
affiché les tours synchronisés sans exception JavaScript.

Au premier clic `ROLL DICE`, le relais a renvoyé trois réponses HTTP 503 avant
transmission. L’hôte a affiché « No server confirmation. Retry the same action
to resync. » et le bouton est resté disponible. Avant la reprise, le serveur
avait toujours `lastAppliedCommandSequence=0` et aucune commande en queue.
Un clic supplémentaire sur le même bouton a réussi : séquence appliquée 1,
une seule commande acceptée, file vide. L’hôte a obtenu 2 + 6 et est arrivé sur
Opioid Paradise ; les cinq invités affichaient la même case et `WAIT` pendant
que l’hôte voyait l’action d’achat. État autoritaire : version 2, séquence 1,
`ResolveTile`, un lancer et un événement d’arrivée.

Captures du salon, du bandeau d’erreur et de la convergence hôte/invité, plus
un état assaini sans jetons, dans `Docs/Validation/NetworkResilience_2026-10-15/`.
Le serveur, le relais, les origines et le stockage étaient QA locaux et ont été
arrêtés après le run ; aucun appel n’a touché la préversion. Cette recette
ferme la reprise pré-commit à six clients rendus sur desktop. Elle ne mesure pas
une longue coupure, six appareils physiques, Safari/iPhone, la latence sur un
réseau mobile ni la chauffe du téléphone.

## Réponse post-commit perdue depuis le WebGL Release en cours de tour

Le build Release actuel `/tmp/PolyRiskPortraitFitQA_20261015` a été lancé dans
Chrome avec un hôte WebGL et un siège invité Ready créé par l’API QA. Le relais
local a supprimé la prochaine réponse `game/state` **après** le commit serveur.
Depuis l’interface Unity de l’hôte, un clic sur `ROLL DICE` a produit 6 + 5,
une arrivée sur Fentanyl Labs et l’inspecteur de propriété est resté utilisable.
Le marqueur de panne a été consommé, puis l’hôte a relu l’état autoritaire.

Le snapshot relu est à la version 2 / séquence 1, avec un seul lancer, une
position 11, et exactement trois activités distinctes : début du tour, lancer,
arrivée. Aucun achat n’a eu lieu. L’écran WebGL montre la même arrivée et la
fiche de Fentanyl Labs. Capture assainie sans jeton :
`webgl_host_lost_publication_state.json`.

Résultat : récupération d’une publication déjà commise depuis l’interface
WebGL, sans répétition de l’action : PASS, un hôte rendu et un second siège
protocolaire. Ce contrôle ferme le trou « publication perdue au milieu d’un
tour » côté rendu hôte ; les six rendus simultanés, un invité WebGL rendu et la
coupure prolongée/mobile restent à recetter. La tentative avec deux canvases
rendus a échoué au chargement du second canvas et n’a pas été comptée.
