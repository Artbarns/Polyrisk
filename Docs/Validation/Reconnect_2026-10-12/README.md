# Recette de reconnexion WebGL — 12 octobre 2026

## Preuves confirmées

Build initial : `/Users/arthurbarreau/.codex/validation/PolyRiskWebGLReconnect`.
Deux clients réels, création/code/ready/lancement. Rechargement de l’hôte en
Planning : retour du plateau, absence d’erreur navigateur et snapshot serveur
strictement identique (version 1, séquence 0, positions/soldes complets identiques).
Voir les deux captures host_planning et les JSON two_clients_initial / after_host_reload.

Le clic suivant a acheté Opioid Paradise après expiration automatique du Planning,
plutôt que déclencher un lancer manuel : ne pas compter cela comme recette du bouton
Roll Dice. L’achat est acquitté une fois (séquence 1, solde 16900). Le build initial
laissait ensuite les changements automatiques hors snapshot ; les logs filtrés
`autonomous_desync_baseline.json` en conservent la preuve.

Build V2 : `/Users/arthurbarreau/.codex/validation/PolyRiskWebGLReconnectV2`.
Création/code/ready/lancement à deux clients via le relais QA 5093. Le relais a
perdu volontairement la première réponse après commit : la partie démarre tout
de même avec version 1 / séquence 0. Après expiration du Planning, version 2 /
séquence 0 publie l’arrivée case 8 sans commande utilisateur. Le snapshot contient
la pioche Venture initiale (20 cartes) et sa défausse vide, présence explicite vraie.

Une seconde réponse est perdue après le commit de l’achat. Version 3 / séquence 1,
solde 18000 → 16900 (un seul débit de 1100), propriétaire de la case 8 = joueur 0,
tour suivant joueur 1. Les exports v2_* enregistrent ces faits.

## Échec V2 et validation V3

Le joueur 1 reçoit sa destination (French Riviera, case 6, version 4, séquence 1),
mais le bouton demeure ROLLING. La commande du second joueur n’est donc pas validée.
L’ordre OnTileSelected puis OnTurnStarted efface la sélection répliquée. Le correctif
source émet le nouveau tour avant la sélection et ne réinitialise pas les pickers
pour chaque mise à jour dans le même tour. Sa régression corrigée passe Economy 71 assertions (log replica_selection_corrected.log).
Le build V3 est compilé et testé à deux clients réels (room QA 8VA9KH).
La pioche de l’hôte conserve exactement le snapshot lors du rechargement : version 3,
séquence 2, carte 20/token 1, pioche 19 et défausse [20]. Le dialogue revient puis
la confirmation active CRASH et passe au second joueur.

Le second joueur reçoit une carte Venture #15 et peut la confirmer. Son clic est
acquitté : version 7 / séquence 5, carte en attente effacée, doubleBurnRateNextGo=true
pour joueur 1, retour au Planning du joueur 0. Voir guest_venture_v3.png et
v3_after_guest_confirm.json. La commande de pioche était séquence 4 après arrivée
Venture ; le clic à cet instant ne prouve pas un lancer manuel du second joueur.
Le blocage ROLLING de V2 n’empêche plus ce parcours Venture en V3.

Le lobby V3 affiche deux lignes lisibles à 1280×720 sans recouvrir les actions
(lobby_two_rows_v3.png). La liste à six joueurs reste à vérifier. La photo lobby_guest_before montre l’ancien chevauchement.

## Limites

Aucun succès général de multijoueur/reconnexion à six clients n’est revendiqué.
Restent : lancer manuel et achat du second joueur, pouvoirs/OPA/Marketplace,
liquidation GO/loyer, réponses perdues supplémentaires, reconnexions après décisions,
fin de partie, recette mobile et humaine. Les snapshots exportés n’incluent pas
les credentials/hashes ni la queue privée. Le build mobile précédent reste distinct.

## Préparation de la recette à six

Six clients V3 ouverts sur les ports 8792–8797 ; lobby WVK7M9 créé par
QA_SixHost. Les invités ne sont pas encore tous connectés et aucun succès à
six joueurs n’est revendiqué. La saisie différée de l’outil a dupliqué l’adresse
API d’un invité. Cela révèle un défaut réel : UnityWebRequest lève FormatException
après SetBusy(true), laissant Create/Join désactivés même après correction du champ.
Voir invalid_api_busy_v3.png. La source valide désormais l’URL HTTP/HTTPS avant
SetBusy : URL absolue, hôte non vide, sans credentials/query/fragment.
Les tests Unity et le prochain build doivent confirmer ce correctif avant publication.

Suite : Unity EconomyRegression passe 82 assertions, sortie 0
(log lobby_url_recovery_regression.log). Les deux dernières vérifient le chemin
Post réel via son IEnumerator : aucune requête ni état busy bloqué, erreur UI
et callback cohérents. Build V4 lancé séparément avec ce correctif (log
webgl_reconnect_v4.log). Le lobby WVK7M9 a trois clients WebGL connectés :
QA_SixHost, QA_Six2 (ready), QA_Six3. Les ports 8795–8797 restent au menu.

Le défilement de la liste à trois clients revient en haut pendant la recette.
RenderLobby recrée toutes les lignes à chaque poll. Correctif source ajouté :
conserver les lignes si leurs champs affichés sont identiques, et restaurer
scrollOffset après une modification des membres/ready. Trois assertions ajoutées
pour la détection des changements ; elles ne sont pas encore exécutées. Ce correctif
est postérieur au lancement V4, donc absent de ce build et du clone pour le moment.

## Partie WebGL à six démarrée

Six clients réels rejoignent WVK7M9, ready puis start. Noms uniques requis :
les invités 5/6 portant initialement Player sont rejetés explicitement, puis
rejoignent avec QA_Six5/QA_Six6. L’export six_after_host_roll_v3 montre version 2,
séquence 1, six soldes initiaux, hôte case 9. Achat hôte : version 3/séquence 2,
16800 $, tour joueur 1. Lancer manuel joueur 1 : version 4/séquence 3, case 10,
tour joueur 2. Le troisième et quatrième lancers expirent automatiquement ;
ne pas les compter comme commandes manuelles. Version 5/séquence 3 : joueur 2
case 9 paie 120 $, hôte 16920 $, débiteur 17880 $, tour joueur 3. Puis version 6
arrivée joueur 3 case 10, tour joueur 4.

Défaut confirmé par inspection source et UI : OnTurnStarted remet WAIT pour le
joueur humain. Si deux snapshots successifs sont Planning (les phases transitoires
sont côté autorité), Update ne voit pas de changement d’état et ne rafraîchit pas
ROLL DICE. Fix source : OnTurnStarted appelle UpdateButtonsState immédiatement.
Une assertion UI Toolkit réelle a été ajoutée pour Planning→Planning ; pas encore
exécutée. Absent des builds V3/V4/V5 actuellement lancés.

V4 : build réussi / sortie 0. Tests de liste : Economy 85 assertions / sortie 0
(log lobby_rows_regression.log). V5 avec correctif liste compile séparément
(log webgl_reconnect_v5.log). Ces tests ne valident pas encore le défilement réel V5.

Correctif Planning→Planning : Economy 86 assertions / sortie 0, log
planning_button_regression_enabled.log. Le test active explicitement OnEnable car
il s’exécute hors mode Play, puis publie OnTurnStarted et vérifie le texte et
l’activation du vrai Button UI Toolkit. Le premier montage sans OnEnable échouait
à déclencher le widget. V5 build terminé / sortie 0 (n’inclut pas ce correctif).

Passerelle même origine préparée : serve_webgl.py --api-upstream local, client
WebGL public prend par défaut son origine. Smoke API six joueurs via 8800 PASS
(room M6G8AY), plus page statique 200, hors route API 404, corps >384 KiB 413.
Trois assertions de choix d’origine ajoutées ; validation Unity en cours dans
same_origin_regression.log. Aucun nouveau lien public fourni à ce stade.

Suite : Economy 89 assertions / sortie 0 (same_origin_regression.log). V6
compile avec les correctifs adresse/liste/tour et le défaut d’origine publique.
Achat du sixième joueur confirmé : version 9/séquence 4, joueur 5 à 16900 $,
propriété 8 acquise, second double puis arrivée Marketplace case 20.
Sélection de vente : panneau réduit, propriété 8 cliquée, retour détails
Opioid Paradise ; voir six_marketplace_pick_v3 et six_marketplace_selected_v3.
Vente lancée puis tous les invités passent ; version 15/séquence 10, Closing,
aucune offre. Aucun échange avec offre acceptée n’est validé par ce parcours.

Défaut UI après fermeture : panneau d’actions absent alors que le jeu poursuit
ResolveTile après le double. Marketplace désactive/réactive le GameObject
ActionButtonsWidget et son UIDocument, invalidant ses références UI et ses
abonnements pendant les événements. Correctif source postérieur au build V6 :
masquer root.style.display, garder le composant actif et rafraîchir les actions
à la fermeture. Deux assertions ajoutées, encore non exécutées. Le libellé
Income / turn de la propriété sélectionnée devient Rent / visit.

Marketplace presentation : Economy 91 assertions / sortie 0, log marketplace_presentation_regression.log. V7 lancé avec les correctifs compilés. La méthode garde le widget actif et rafraîchit ses actions après fermeture ; la recette WebGL réelle de ce correctif reste à faire. Passerelle : HEAD wasm confirmé application/wasm.

## Rechargement V7 et reprise de session

V7 build PASS / sortie 0. Les serveurs 8790–8797 servent V7 avec relais API
5091. Rechargement des six onglets. État serveur version 15/séquence 10 conservé,
six positions/soldes inchangés (six_v7_reloaded.json). L’hôte retrouve le plateau
Antarctica Datacenter. Le sixième client reste au menu, adresse enregistrée 5091
correcte, sans erreur console hors avertissement de synchronisation IDB.
La reprise complète à six et le retour Marketplace V7 ne sont donc pas validés.
La cause exacte de la perte de reprise sur ce client n’est pas établie.

Défaut de source corrigé : ResumeSavedSession effaçait les identifiants sur
toute erreur. Les statuts temporaires 0/408/429/5xx gardent désormais la session
et déclenchent un réessai à 5 s ; les rejets 400/401/403/404/410 invalident
la session conformément aux réponses du serveur. Un numéro de génération
annule la reprise ancienne quand Create/Join/Leave démarre une autre action,
et empêche une réponse tardive de remplacer la nouvelle session. Economy
103 assertions / sortie 0 (resume_failure_regression.log) confirme les classes
de statuts. Trois assertions de coroutine/réessai/annulation ajoutées ensuite.

V8 lancé via EconomyBatchValidation.BuildVerifiedWebGL : tests puis build dans
la même session Unity, arrêt sur exception de test. Log webgl_reconnect_v8_verified.log.
Cette correction ne récupère pas des identifiants déjà effacés par un ancien build.

## Reprise temporaire V8 confirmée

V8 tests 106 assertions puis build PASS / sortie 0. Sixième client au menu V7 :
adresse invalide affiche une erreur tout en gardant Create/Join disponibles
(invalid_address_recoverable_v7.png). Adresse corrigée → création M23EXU.

Serveur API QA 5091 brièvement suspendu (PID 15387, état Ts confirmé), secours
SIGCONT automatique armé à 45 s. Client 8797 rechargé sur V8 : Request timeout
visible (resume_timeout_v8.png). API rétablie explicitement, HTTP 200 confirmé.
Le client retrouve automatiquement M23EXU avec le même hôte QA_Six6, sans
ressaisir code/identifiants : resume_recovered_v8.png. Cela valide la reprise
du lobby après timeout, pas encore celle d’une partie complète à six.

Ancienne partie WVK7M9 : onglets 12–15 fermés pour réduire la charge. Nouveau
salon V8 utilise 8797 (hôte) et 8796 (invité en cours de connexion). Quatre
origines supplémentaires 8810–8813 préparées.

Finition source postérieure à V8 : erreur du menu pickingMode Ignore, effacée
quand nom/code/adresse change. Elle évite que le toast couvre/intercepte le
champ à corriger. Ce changement visuel n’est pas encore inclus dans un build.

## Recette V8 à six clients — parcours confirmé

M23EXU : les six clients V8 ont rejoint et envoyé un lancer manuel. Achats des
joueurs 0/1/2/3 acquittés (versions 3/5/7/9), tax/double du joueur 2, loyer de
100 $ joueur 4 → joueur 3. Passage direct Planning→Planning : joueur 5 affiche
ROLL DICE puis commande 10 arrive case 5, ResolveTile. Voir
planning_to_planning_sixth_v8.png et v8_after_sixth_roll.json.

Défilement réel avec la barre du lobby confirmé jusqu’aux joueurs 5/6 ; position
conservée pendant les polls et les changements ready. La tentative molette CUA
n’a pas fait défiler le canvas et n’est pas comptée comme validation tactile.
Voir lobby_six_ready_scrolled_v8.png.

Rechargement de six clients pendant décision d’achat du terminal case 5 :
les snapshots publics avant/après sont strictement identiques (version 11,
séquence 10, soldes, positions, cartes, propriétés, tours et continuation). Les
six retrouvent le plateau. Le sixième affiche d’abord Request timeout, puis
récupère automatiquement sa décision BUY sans ressaisie ; voir
sixth_decision_after_reload_v8.png. Clic BUY après reprise → version 12, séquence
11, joueur 5 18000→17000, retour joueur 0 Planning.

Cela confirme ce parcours à six et la reprise de ce point de décision. Restent
la Marketplace V8 avec offres/fermeture, ultimates/OPA/dettes en ligne, fin de
partie, geste tactile de liste, nouveaux essais mobiles et partie humaine.


## Publication V9 et retour mobile

V9 : 106 assertions Unity et build réussis. Salon paysage 900×430 : sixième
ligne accessible, actions visibles et Quitter revient à l’accueil ; preuve
`lobby_six_landscape_v9.png`. Même lien public mis à jour : index HTTPS identique
au V9, wasm HTTP 200 application/wasm gzip. Fichiers V8 conservés pour les
chargements en cours. Le retour humain confirme uniquement le premier essai
exploratoire iPhone 14 Pro Max ; recette multijoueur approfondie encore attendue.


## Moteur Turbo — séparation tirage/validation

DiceSystem.GenerateRoll ne modifie ni compteur ni événements ; CommitRoll valide
les deux valeurs puis publie et compte un seul résultat. TurnManager prépare deux
candidats Turbo et valide seulement celui retenu. MechanicsRegression Unity :
85 assertions, sortie 0, journal turbo_commit_mechanics.log. Quatre nouveaux
contrôles : candidats sans effet, cinquième double, remise à zéro sur non-double,
rejet des valeurs invalides sans mutation. Analyse combinatoire conservée dans
auto_turbo_counter_analysis.json.

Étape incomplète : le choix humain des deux résultats doit encore remplacer le
choix automatique du total supérieur. Ajouter un état de choix persistant, deux
paires dans le snapshot, commande réseau autorisée et boutons avec destinations ;
restaurer ce choix après reconnexion avant tout CommitRoll, puis vérifier la
précision automobile et la pénalité. Ce correctif source ne figure pas dans V9.


## Choix humain Turbo — intégration source

ChooseTurbo conserve les deux paires ; la commande dice.turbo valide 0/1 une
seule fois, puis ResolveDice poursuit la précision et le déplacement. Les deux
boutons affichent les jets et indices de destination. Délai de 60 s : premier
jet conservé. Snapshot additif et validation des candidats au chargement.
MechanicsRegression : 91 assertions PASS, Unity sortie 0 ; six contrôles ajoutés
sur restauration, jet inférieur choisi, double abandonné, clic répété, JSON et
valeurs invalides. Serveur Release compilé sans erreur ni avertissement.
La recette visuelle WebGL et les commandes réelles après reconnexion restent
à réaliser ; API publique 5091 conserve son processus précédent jusqu’à cette
validation, donc la nouvelle commande n’y est pas encore disponible.


V10 première tentative arrêtée par Economy : restauration d’un ancien snapshot
rejetée car JsonUtility produit des tableaux vides pour les jets absents.
Correction : tableaux vides acceptés hors ChooseTurbo, rejetés pendant le choix ;
validation avant mutation des joueurs. Deuxième tentative : Economy 106 PASS,
build encore en cours (session 4427, webgl_reconnect_v10_verified_retry.log).
Serveur QA isolé 5095 : smoke six joueurs M8ZG9C PASS, incluant réception
et préservation de dice.turbo choice=1. Cela valide le transport uniquement.
Fixture turbo_choice_fixture.json préparée : trois titres Automobile, streak 4,
jets 6+6 et 2+3, précision disponible. Les lobbyPlayerId doivent être associés
au salon QA au moment de l’injection locale ; ce scénario n’a pas encore été
exécuté dans le navigateur.


## Turbo V10 — navigateur autorité, salon MYA57L

V10 build terminé sortie 0. Serveur QA 5095, relais local 5096, WebGL 8930
(session 61261), navigateur tab24 conservé. Option explicite --qa-session-file
du relais : uniquement sessions créées via ce relais loopback, fichier 0600
sous .codex/validation, aucun token exporté aux preuves ou journal. Public intact.

Fixture publiée à v2 puis rechargement natif : deux jets 6+6→12 et 2+3→5
affichés ; preuve turbo_choices_restored_v10.png. Clic natif JET2 : snapshot
v3 seq1 AdjustDice, total5, doubles0, cash18000 ; double abandonné sans amende.
Clic natif +1 : v4 seq2 ResolveTile, position6 French Riviera. Boutons BUY/PASS
visibles. Preuves JSON turbo_after_lower_choice_v10 et turbo_after_precision_v10.
Ce test utilise un client autorité réel + invité REST de fixture, pas deux
navigateurs. Encore requis : cinquième double choisi, rechargement pendant
ResolveDice/précision, invité navigateur et dimensions mobiles avant publication.


## V10 cinquième double retenu

MYA57L : fixture réinjectée v5, rechargement natif puis JET1 6+6 choisi.
Snapshot v7 seq3 : cash17500 (amende500 une fois), position0, doubles0,
acteur1 Planning. Rechargement suivant conserve v7 seq3 et cash17500.
Preuves turbo_fifth_double_v10.json et turbo_penalty_reloaded_v10.png.
Deux origines V10 préparées 8931/8932 (serveur session60273), tabs25/26
conservés, globals turboHost2/turboGuest2. Accueil vierge, réglage API encore
5080 : choisir origine propre 8931 ou8932 avant Create/Join. Relais5096
capture uniquement la dernière session créée dans turbo_host_session.json ;
MYA57L token encore présent jusqu’à création suivante. Étape suivante : vrai
salon avec ces deux clients, puis choisir Turbo depuis invité et reload.


## V10 deux navigateurs — AGMS9R

Create/Join/Ready/Start natifs tabs25/26, origines8931/8932. Fixture v2
acteur1 ChooseTurbo puis recharge des deux : hôte WAIT, invité deux jets
restaurés (turbo_guest_restored_v10.png). Choix natif invité JET2 : v3 seq1
AdjustDice total5 doubles0 ; preuve turbo_guest_choice_v10.json. Recharge
invité : boutons de précision restaurés. Clic +1 tardif ne donne PAS case6 :
v5 seq2 acteur0 Planning, invité position5, delta0. Activités montrent arrivée
Temu puis passe. Hypothèse délai60s expiré pendant chargement/observations,
à confirmer ; aucune revendication de précision après reprise réussie.
Preuve turbo_guest_precision_timeout_v10.json. Retester avec chronométrage
et vérifier synchronisation des boutons quand l’autorité avance.


## Précision invité après reprise — essai chronométré

AGMS9R : injecter AdjustDice v7 puis reload hôte/invité ; boutons restaurés,
clic natif +1 donne v8 seq3 ResolveTile position6. Contrôle serveur à53,4s
après injection (pas chronométrage précis du clic). Preuve
turbo_guest_precision_timed_v10.json. Le précédent échec peut correspondre
au timeout60s ; sa cause exacte n’est pas prouvée.

Portrait430×900 : deux choix visibles (turbo_guest_portrait_v10.png), mais
le contrôle plein écran du template empiète sur JET2 et les overlays debug
couvrent le HUD. Le panneau conserve également French Riviera pendant le
choix ; contextualiser cette présentation avant publication. Vue reset.


## V11 présentation Turbo — source

ActionButtonsWidget contextualise ChooseTurbo/AdjustDice avec titre et consigne,
masque les statistiques durant la décision et restaure l’inspection ensuite.
OnGUI des trois outils debug limité à l’éditeur. Template : plein écran déplacé
en haut sur grand écran et masqué largeur<=600 ou hauteur<=500.
BuildVerifiedWebGLPreview exécute Economy puis Mechanics avant build sans
Development. V11 lancé session73533, webgl_reconnect_v11_preview.log ;
compilation et rendu non encore confirmés. API publique et V9 intacts.


## V11 publié le13 octobre

Economy106 + Mechanics92 + build PASS sortie0. Rendus navigateur portrait
430×900 et paysage900×430 propres : turbo_portrait_v11.png et
turbo_landscape_v11.png. Serveur isolé persistence/restart PASS. API5091
remplacée (session86786) avec stockage sauvegardé, qui contenait0 salons.
Smoke public6 joueurs9H545G PASS incluant dice.turbo. Assets fusionnés puis
index atomique, HTTPS200 index identiqueV11. Serveur8900 garde même
PolyRiskPublicPreview. Tab27 publicV11 ouvert pour contrôle final.

Public V11 navigateur : accueil chargé, Create natif donne B2HWGN sans réglage
serveur. Preuve public_lobby_v11.png ; salon QA quitté ensuite.


## OPA V11 — deux clients, invité

AGMS9R fixture v15 ResolveTile invité1 sur Venture7, carte19 token1001
en attente, unique cible tile1 détenue hôte0 : base800, upgrades payés1000,
niveau2. Rechargement des deux clients : carte OPA restaurée chez invité,
preuve opa_guest_restored_v11.png. Confirmer natif invité donne v16 seq4 :
hôte19550, invité16450 (cash total36000 conservé), propriétaire1, niveau2
et coût upgrades1000 conservés, carte effacée. Prix1550=max(1200,1550).
Preuves opa_fixture_v11.json et opa_guest_confirmed_v11.json. Ce contrôle
valide le transfert via la commande réseau et l’autorité réelle, pas toute
la pioche aléatoire ni une partie complète.

Après reload invité : cash16450 visible, carte absente ; serveur v17 seq4
et deux soldes inchangés. Aucun second transfert au rechargement.


## OPA inabordable et revue marketplace

AGMS9R fixture v18 carte19 invité cash1000, cible1550. Confirm natif :
v19 seq5, soldes18000/1000 inchangés, propriété reste hôte0, carte effacée.
Preuve opa_unaffordable_v11.json.

Marketplace fixture v20 Reviewing seller1 tile1, offre buyer0 cash2500 +
tile3, soldes18000 chacun. Après reload deux clients, panneau de case devant
Review bloque la liste ; preuve market_review_obscured_v11.png. Open Market
réouvre Hub, ne constitue pas acceptation. Code ShowTenderReview et
ShowBidderSpectator ne masquait pas ActionButtonsWidget contrairement à
ShowBidderForm ; correction source ajoutée aux deux présentations. Nouveau
build et recette acceptation/reconnexion encore requis, aucune vente validée.


V12 en cours : Economy106 et Mechanics92 PASS, liaison WebGL confirmée
active. Outil qa_fixture.py ajouté et exécuté sur5095 : capture puis
réinstallation Reviewing v21 avec association automatique des sièges.
Preuves market_before_v12.json et market_review_prepared_v12.json.
Build session33612/webgl_reconnect_v12_preview.log encore non terminal
au dernier contrôle ; ne pas relancer avant inspection.


## V12 vente mixte exécutée

Economy106/Mechanics92/build PASS sortie0. Serveur local8931/8932 V12
session82819. FixtureReview v22, reload25/26 : offre accessible sans panneau
de case. Sélection native puis Seal vendeur invité : v23 seq6, cash
acheteur15500/vendeur20500, total36000 conservé, tile1→0 et tile3→1,
niveau2 et coût upgrades1000 du titre vendu conservés. Reload des deux :
players/properties et seq6 strictement identiques ; ROLL DICE/PASS visibles
chez hôte au tour suivant. Preuves market_review_selected_v12.png,
market_sealed_v12.json et market_sealed_reloaded_v12.json.
Nouvelle fixture v24 pour recette portrait : tab28 marketMobile origine8932
430×900 en chargement. Cap viewport agit sur nouveau tab sélectionné ;
anciens25/26 restaient1280×720 malgré override. Ne pas attribuer cela au jeu.
Encore : vérifier portrait/paysage et publierV12 après ce contrôle.

Portrait430×900 tab28 : liste, offre cash/propriété et deux actions visibles,
pas de panneau de case superposé ; market_review_portrait_v12.png.

Paysage900×430 : offre et boutons accessibles, preuve market_review_landscape_v12.png.
V12 publiée même lien, index HTTPS200 identique, assets anciens conservés.


## Parcours de soumission V12 et délai

AGMS9R fixtureNone v25 acteur1 Marketplace20, titre8 au vendeur. UI native :
OpenMarket/SellProperty, panneau réduit et tile8 cyan, clic703372 → Opioid
Paradise sélectionné, panneau remonte et ConfirmSelection. Preuve
market_property_selected_v12.png. Serveur reçoit tender.begin seq7, puis
acheteur passe automatiquement seq8 en5s ; v27 Closing, acteur0Planning.
Preuve market_tender_started_v12.json. Aucune offre manuelle soumise dans
ce parcours, contrairement à l’acceptation de fixture déjà validée.

V13 source : délai d’invitation online30s (local5s conservé), pseudonymes
de salon dans notification et revue, bouton Seal désactivé avant sélection.
Délai maximal supplémentaire par invitation25s, sans changement des montants
ni de la règle de transfert. Build lancé session/log webgl_reconnect_v13_preview.log ;
recette de soumission native encore requise.

V13 : Economy106/Mechanics92 PASS, liaison WebGL active dernier contrôle.
Analyse délai : invitations parallèles, surcoût maximal25s par tender,
indépendant du nombre d’acheteurs ; exemple40 appels +1000s maximum,
pas une durée observée. Interaction avec arrêt90min à mesurer humainement.
Preuve analytique market_invitation_impact.json ; build session39486.


## V13 invitation — nouveau défaut confirmé

Economy106/Mechanics92/build PASS sortie0. V13 local8931/8932 session12808,
clients29 hôte /30 invité, globals turboHost2/turboGuest2. FixtureNone v29
puis parcours natif Open/Sell/select tile8/Confirm. Invitation hôte affichée
avec pseudo et compteur23s (délai30), mais panneau case la recouvre :
preuve market_invitation_obscured_v13.png. Participation non validée.
Correction source : ShowAuctionNotification masque ActionButtonsWidget
comme les écrans formulaire/revue. V14 doit compiler et permettre le clic
Participate avant d’affirmer la soumission complète. Lien public resteV12.


V13 fixtureInviting v32 : reload29/30, attente vendeur derrière panneau
case confirmée (market_waiting_obscured_v13.png). Correction source
centralisée SwitchView : masquer ActionButtonsWidget avant chaque nouvelle
vue marketplace, plutôt que compter sur le passage initial via OpenMarket.
Source repo préparée pourV15, pas encore copiée au clone pendant buildV14.
V14 session48588 reste active ; inspecter son statut avant nouvelle compilation.


## V14 invitation accessible ; formulaire à corriger

V14 Economy106/Mechanics92/build PASS sortie0. Local8931/8932 V14
session40068, fixtureInviting v36. Native acheteur : invitation visible30s
etpseudo sans panneau superposé, clicParticipate ouvre formulaire. Preuves
market_invitation_clear_v14.png et market_bid_form_clipped_v14.png.
Formulaire1280×720 coupe Skip/Submit sous la fenêtre, confirmé après animation.
Correction source asset : détails dans ScrollView, footer d’actions séparé
sans rétrécissement, panneau expanded82% hauteur/max92%. XML parsé.
Non copié au clone pendant V15 (session19127, correction SwitchView).
Étape suivante : attendreV15terminal, copier UXML+Theme et compilerV16 ;
valider formulaire desktop/mobile, soumission native, attente vendeur.


## Offre cash soumise manuellement V14

Client acheteur31 origine8931 en portrait430×900 (hôte29 fermé),
vendeur30 origine8932. Invitation restaurée puis Participate natif ;
formulaire portrait complet, slider réglé par drag, UI2,91K, montant réel2908.
Submit natif : v37 seq12 Reviewing, bidder0 cash2908, aucun titre proposé.
Vendeur : pseudo Player affiché, Seal désactivé avant sélection puis activé
après clic ligne. Seal natif : v38 seq13, cash15092/20908, total36000,
titre8 transféré à0. Preuves market_bid_cash_mobile_v14.png,
market_bid_submitted_v14.json et market_manual_deal_v14.json.
Cela valide participation, slider, soumission et acceptation sur deux clients,
avec une invitation installée par fixture ; début de vente natif validé
séparément dans V12/V13. Formulaire desktop reste coupé avant correctif asset.
V15 session19127 encore en cours au dernier contrôle ; copier UXML/Theme
au clone seulement après sortie terminale puis construireV16.

V15 terminé build PASS sortie0. UXML formulaire et Theme copiés au clone ;
V16 lancé session48493,
log webgl_reconnect_v16_preview.log, sortie PolyRiskWebGLReconnectV16.


## V16 formulaire + propriétaire de sélection

Economy106/Mechanics92/build PASS sortie0 (session48493 terminée).
Local8931/8932 V16 session70379, fixtureInviting v42. Reprise attente
vendeur sans panneau case (market_waiting_clear_v16.png), invitation
accessible, Participate ouvre formulaire desktop avec actions fixes.
Défilement réel par drag du scrollbar atteint sélection de titres.
Barre horizontale et scrollers Unity par défaut restent visuellement lourds.

Sélection bidder : cyan sur titre8 du vendeur, alors que buyer0 détient
titre3. Source confirme trois usages erronés CurrentPlayer : HighlightOwned,
IsTileEligible et traitement BidProperty. Correction V17 : GetLocalPlayer
pour contexte BidProperty ; autres contextes restent CurrentPlayer.
Trois régressions ajoutées : accepter titre acheteur, rejeter titre vendeur,
conserver règles du vendeur. Preuve market_bid_wrong_owner_highlight_v16.png.
UXML Vertical/Hidden horizontal, scroller vertical stylé et contenu borné.
V17 lancé session15404, log webgl_reconnect_v17_preview.log ; tests/build
et offre en propriétés encore non confirmés. Lien public resteV12.


## V17 régressions et prochaine correction de brouillon

Economy106/Mechanics95 PASS, trois contrôles de propriété nouveaux passent.
V17 session15404 toujours en optimisation au dernier contrôle. Source repo
(non copiée au clone pendant ce build) prépareV18 : après chaque titre choisi,
formulaire redéployé/modal rétabli ; pendant choix, détails/actions masqués
et consigne dédiée visible. Une réponse d’autre acheteur ne réouvre pas
l’invitation devant un brouillon existant. Bidding restaure attente vendeur
plutôt que tenter son formulaire acheteur. Nouvelle régression de brouillon
préparée, non encore exécutée.
AprèsV17terminal copier Scripts + Bidder_Form + Theme et compilerV18, puis
recetter titre3 acheteur (pas titre8 vendeur), montant et offre mixte native.


## V17/V18 offre mixte native

V17 terminé build PASS sortie0. Bidding fixture v47 (phase directe, pas
invitations) : slider2702, sélection3 surlignee chez acheteur0, Submit natif
v48 seq17 propose cash2702 + title3. Vendeur sélection/Seal natifs :
v49 seq18, soldes15298/20702, title8→0 et title3→1. Preuves
market_mixed_bid_submitted_v17.json, market_mixed_manual_review_v17.png,
market_mixed_manual_deal_v17.json.

V18 première tentative compiler error CS0117 : enum BidderForm manquant.
Corrigé en ajoutant cet état privé et en le posant à ShowBidderForm.
Deuxième tentative log webgl_reconnect_v18_preview_retry.log : Economy106,
Mechanics96 et build PASS sortie0. Local V18 session90960. FixtureBidding
v50 : sélectionner3 redéploie formulaire et conserve cash2721 ; défilement
montre title3 valeur900, total3621 (UI3,62K). Submit v51 seq19, Seal natif
v52 seq20, soldes15279/20721, title8→0/title3→1. Preuves
market_mixed_draft_returned_v18.png, market_mixed_bid_submitted_v18.json,
market_mixed_manual_deal_v18.json.

Portrait V18 : nouveau client autorité32 remplace31, origine8931, viewport
430×900, fixtureBidding v53. Chargement en cours au dernier contrôle.
Client vendeur30 conservé. PublierV18 après contrôle responsive.


V18 portrait430×900 : formulaire complet, actions visibles, preuve
market_bid_form_portrait_v18.png. Choix3D réduit panneau/consigne visible
mais board déborde horizontalement et titre3 presque hors écran :
market_mobile_board_clipped_v18.png. V18 non publié en attente de ce contrôle.

V19 source : BoardInteractionManager ajuste orthographicSize pendant un
picker, selon rayons des40 centres projetés dans repère caméra + marge0,75,
ratio réel de viewport ; conserve taille suffisante et restaure zoom11
hors sélection. Recalcul uniquement sur changement de mode/dimensions,
pas de boucle de framing par frame. Trois tests mathématiques ajoutés.
BuildV19 session29376/log webgl_reconnect_v19_preview.log lancé ; ni
tests ni rendu encore confirmés. Deux clients32 hôte/30 vendeur conservés,
API locale5095, V18 encore servi8931/8932 session90960.


V19 : Economy106/Mechanics99 PASS (205 total). Trois tests de cadrage
portrait/landscape/vertical PASS. Build encore actif session29376,
log webgl_reconnect_v19_preview.log. Rendu et sélection mobile non prouvés.
