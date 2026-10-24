# Reprise de la recette de clôture — 17 octobre 2026

## État vérifié sur les sources du dépôt

- Unity **2022.3.62f3**, `RunAllEditorRegressions` : économie **114**, mécaniques **141**, événements **14**, UI **262** — **531 assertions PASS**.
- Build WebGL Release (`BuildOptions.None`) : **PASS**, sortie isolée `/tmp/PolyRiskGoalAudit_20261017`, code 0.
- Payload gzip Unity + loader : **12 721 531 octets** (12 700 851 octets pour data/framework/WASM compressés et 20 680 pour le loader). Ce build n'a pas remplacé de preview partagé.
- Service lobby recompilé en Release : **0 avertissement, 0 erreur**. `smoke_test.py`, `persistence_test.py` et `six_player_reconnect_test.py` passent chacun depuis leur serveur et leurs données QA isolés; capacité six, refus du siège suivant, persistance, reprise des six sessions, transfert d'hôte et livraison de commande vérifiés.
- Parcours WebGL graphique exécuté dans le navigateur sur la build fraîche, en **414 × 720** : accueil → partie locale → lancer confirmé **4 + 1 = 5** → déplacement → fiche de la case Temu Sweatshop Terminal avec Buy/Pass disponibles. Cela confirme le chargement et un parcours local élémentaire de cette build; ce n'est ni une partie réseau, ni une mesure de cadence, ni Safari/iPhone physique.
- Parcours réseau de la **même build fraîche** ensuite exécuté entre deux clients WebGL sur deux origines locales (`8801` et `8802`) avec lobby Release isolé sur `5095` : Create → Join → Ready → Start; `turn.roll` hôte résout une taxe de **600 $**; l'invité reçoit le même tour/plateau et son historique indique le débit; le jet invité reçoit l'inspecteur French Riviera, puis `PropertyPassed` renvoie le tour à l'hôte. Vérification directe du stockage QA : état version **4**, séquence appliquée **3**, événements typés 1–4 = DiceRolled / TaxPaid(−600, acteur 0) / DiceRolled / PropertyPassed (acteur 1, tuile 6), solde hôte **17 400 $**, invité **18 000 $**, positions 4 et 6, tour actif joueur 0. Les deux interfaces ont montré l'état correspondant. Aucun loyer partagé ni achat en ligne n'a été forcé dans ce parcours.
- `git diff --check` : **PASS** après la génération. Les URP assets restaurés par le builder n'ont pas laissé d'erreur de whitespace.

## Écarts encore ouverts

La recette humaine à six appareils, Safari/iPhone physique, cadence/mémoire/chauffe mobiles, reprise réseau complète sur la build fraîche, équilibrage à partir de 5–10 parties humaines et livrables de publication restent à faire. Le contrôle visuel frais montre encore des badges ROI très denses sur les tuiles; les prochaines animations visuelles attendent le choix de l'utilisateur dans le laboratoire de previews séparé.

La session QA locale temporaire a été isolée sur `127.0.0.1:8799`; elle n'utilisait ni lobby de joueurs ni serveur partagé.

## Vérification complémentaire de création WebGL — 17 octobre

- Build `/tmp/PolyRiskGoalContinuation_20261017`, ouvert sur six origines locales (`8821`–`8826`) : les six pages ont chargé les ressources Unity du même build courant.
- Un serveur Release isolé a été démarré sur `127.0.0.1:5098`, avec fichier de données QA temporaire. Depuis le client WebGL hôte, après réglage de l'URL « Lobby Server » sur `http://127.0.0.1:5098`, le clic « Create Online Game » a créé un lobby (code `CTNZ3A` confirmé par le journal serveur). Le client a ensuite quitté le lobby depuis l'interface; la persistance QA a été vérifiée vide (`{"schemaVersion":1,"lobbies":[]}`). Les six pages et les deux processus QA ont été fermés/arrêtés.
- Le premier essai de contrôle avait laissé l’URL par défaut `http://127.0.0.1:5080` et créé un lobby temporaire sur le serveur local déjà actif; ce lobby a été quitté immédiatement. Aucun autre joueur n’a rejoint. Le serveur `5080` est resté actif et n’a pas été arrêté.
- Limite : cette reprise confirme la création via l’interface WebGL actuelle avec le bon serveur. Elle ne confirme toujours pas le parcours à six clients : la modification du champ « Connection » sur les clients invités n’a pas été suffisamment fiable dans l’automatisation, et aucun des cinq invités n’a rejoint le lobby isolé. Le protocole serveur à six et le précédent test WebGL à deux clients restent les preuves séparées citées ci-dessus.

## Nouvelle tentative à origines distinctes — résultat partiel

- Le serveur de test a été relancé avec une base vide sur `5098`; six noms d’hôte `*.127.0.0.1.nip.io:8831` résolvent vers la boucle locale. Le réglage réseau du client hôte, après saisie et validation visibles de `http://127.0.0.1:5098`, a créé le lobby `CRB4GX`; l’écran de salle affichait bien `1/6`. Le lobby a été quitté depuis le client et la base QA est revenue à zéro.
- Les six pages ont demandé les mêmes fichiers Unity; le contrôle d’écran a ensuite expiré avec plusieurs instances WebGL ouvertes. Une page invitée rouverte seule affichait un canevas noir et aucun invité n’a rejoint. Ce test ne permet donc ni de conclure à un défaut produit, ni de valider la jonction invitée sur cette build.
- Les tests serveur ont été relancés depuis les sources actuelles : `smoke_test.py` PASS (six joueurs/capacité/commandes), `persistence_test.py` PASS (sessions, queue et état restaurés), `six_player_reconnect_test.py` PASS (six sessions, événements typés de loyer partagé et parts bénéficiaires après reprise et transfert d’hôte).
- Les serveurs temporaires et les onglets QA ont été fermés. Le service persistant `5080` et les onglets utilisateur existants n’ont pas été touchés. Aucune ressource de jeu ni logique gameplay n’a été modifiée.

## Recette hôte sur lobby plein — résultat partiel

- Sur une nouvelle base QA, le client hôte a créé `NYU335`; cinq sièges nommés QA ont été joints et déclarés prêts par l’API. L’écran WebGL hôte a bien affiché **6/6**, les six noms/états Ready, puis a démarré la partie depuis le bouton Start.
- Le host a résolu son premier lancer (**1 + 5**, French Riviera) et a publié l’état au service : version d’état **2**, séquence de commandes **1**, six joueurs dans le snapshot, `turnState=ResolveTile`, événement `DiceRolled` typé.
- Le menu `Quit Game` a demandé confirmation « End Match » puis a ramené le host au titre; le serveur a marqué la partie abandonnée. La base QA temporaire a été supprimée après arrêt du serveur. Les cinq comptes QA étaient des sièges API, pas cinq clients Unity actifs : ce parcours valide l’affichage plein, le démarrage et l’écriture d’état côté host, mais pas la synchronisation visuelle de cinq invités WebGL.

## Commandes d’un siège invité consommées par l’hôte Unity — test mixte à six

- Un hôte WebGL Unity a créé le lobby isolé `J2E8PN`; cinq sièges QA ont rejoint et sont passés Ready via l’API. L’écran de l’hôte a affiché les six sièges, démarré la partie et affiché les cinq invités.
- Depuis l’interface hôte, le joueur 0 a lancé les dés (3+3, puis 4+1 après le double) et passé Fentanyl Labs. L’hôte Unity a publié le passage au siège 1 : état version **4**, séquence appliquée **3**, `currentPlayerId=1`, `turnState=Planning`.
- Le siège invité 1 a envoyé `turn.roll` directement au serveur (séquence **4**). Le contrôleur Unity hôte l’a consommé, résolu 5+1 vers French Riviera et publié l’état version **5**, `lastAppliedCommandSequence=4`, `currentPlayerId=1`, `turnState=ResolveTile`, position du siège 1 = **6**. L’interface hôte montrait « WAITING · QA SEAT 2 · AT FRENCH RIVIERA » et le pion invité sur cette case.
- Le même invité a envoyé `action.secondary` (Pass, séquence **5**). Unity l’a consommée et a publié la version **6**, séquence appliquée **5**, `currentPlayerId=2`, `turnState=Planning`; soldes inchangés pour les six joueurs. L’interface hôte est passée à « WAITING · QA SEAT 3 · ROLL OR USE A POWER ».
- Le host a quitté depuis le menu; le service a marqué le lobby abandonné. Serveur, serveur statique, onglet Unity QA et fichiers contenant les sessions temporaires ont été fermés/supprimés. Le service utilisateur `5080` et ses onglets n’ont pas été touchés.
- Ce test prouve le chemin commande d’un siège invité → transport lobby → consommation par `OnlineGameController` dans Unity → règle de jeu → publication d’état → affichage hôte, dans un lobby à six sièges. Il n’emploie qu’un client Unity actif et cinq sièges API : la synchronisation visuelle simultanée de six clients Unity/WebGL demeure à valider.
- Après fermeture de la recette, les trois tests serveur ont été relancés en parallèle sur la source courante : `smoke_test.py` PASS (six sièges et protocole), `persistence_test.py` PASS (hash credentials, queue et état restaurés), `six_player_reconnect_test.py` PASS (six sessions, loyer réparti typé, reconnexion/transfert d’hôte et livraison de commande). Les trois processus se sont terminés code 0.

## Nouvelle tentative six clients sur la build Release — non concluante

- Le build `/tmp/PolyRiskGoalContinuation_20261017` a été servi avec les en-têtes gzip attendus par `Server/serve_webgl.py`. Une première origine loopback a laissé l’URL de connexion par défaut `http://127.0.0.1:5080`; le code de lobby `UWZSNC` est apparu, puis l’hôte a utilisé **Leave Lobby** et est revenu au menu. Le service `5080` n’a pas été arrêté; son stockage configuré (`/tmp/PolyRiskTypedEventsQA/lobbies.json`) a été vérifié ensuite et contenait `{"schemaVersion":1,"lobbies":[]}`. Cette partie test a été abandonnée et n’est pas comptée comme recette QA isolée.
- Pour isoler la suite, un serveur QA `5099` et six origines `127.0.0.1.nip.io:8841–8846` ont été préparés. Les pages ont chargé; l’automatisation a expiré en capturant l’UI lorsque plusieurs instances Unity étaient ouvertes, et la création n’a pas été confirmée sur le service isolé (aucun fichier de session hôte n’a été produit). Aucune réussite six-clients supplémentaire n’est revendiquée.
- Les onglets QA et serveurs temporaires ont ensuite été fermés; le contrôle de navigateur confirme que seuls les onglets utilisateur `8123`, `8779` et Motion Lab `8796` restent ouverts. Le port `5080` demeure actif. Aucun défaut du jeu n’est déduit de cet essai interrompu; la preuve à six interfaces du 16 octobre et le test hôte/siège invité à six sièges cités ci-dessus restent les références.

## Diagnostic UI/API QA après l’essai interrompu

- Une origine QA unique (`host.127.0.0.1.nip.io:8851`) a ensuite chargé le menu du build Release. L’UI affichait cette origine comme serveur de lobby; un essai depuis l’onglet intégré a grisé **Create Online Game**, sans créer de fichier session ni de lobby sur `5102`.
- Le même proxy a été exercé indépendamment : une requête volontairement invalide a atteint le service isolé et retourné le `400` attendu; le préflight CORS de l’origine hôte a retourné `204` avec `Access-Control-Allow-Origin` exact. Les logs du service montrent ces deux requêtes de contrôle, mais aucune requête provenant du clic Unity. Cela borne le défaut observé à l’interaction/au runtime de cette recette intégrée; ce n’est pas une preuve suffisante d’un défaut produit ou d’un bon fonctionnement.
- Le serveur QA et l’onglet ont été arrêtés/fermés; son stockage est resté sans lobby, les ports `5102`/`8851` ne sont plus à l’écoute, et le service utilisateur `5080` demeure actif. Il faut reprendre cette recette dans un navigateur graphique réellement actif ou avec des traces réseau de navigateur avant toute conclusion sur Create/Join.

## Réessai avec Chrome/Playwright dédié — résultat non concluant

- Le même build Release a chargé et initialisé Unity `2022.3.62f3` sur Chrome/WebGL 2 (backend logiciel SwiftShader). Le menu et la partie locale réagissent bien aux clics.
- Sur six origines `host.127.0.0.1.nip.io:8921–8926`, l’adresse Lobby Server affichée par le client hôte correspondait à sa propre origine, et le proxy local `/api/lobbies` ainsi que son préflight répondent séparément. Pourtant, un clic sur Create depuis l’onglet intégré et depuis Playwright ne produit ni POST ni OPTIONS dans les journaux du proxy; aucun écran de lobby n’apparaît. Ce nouvel essai reproduit donc l’absence de trace sans permettre d’en attribuer la cause au jeu ou à l’automatisation.
- Le test direct du proxy a créé par inadvertance une salle de contrôle `VPQQJ9` sans client. Le serveur QA a été arrêté, son seul fichier de persistance isolé supprimé, puis les ports `5102` et `8921–8926` ont été vérifiés fermés. L’onglet de recette a été fermé. Le service utilisateur `5080` et ses onglets sont restés actifs et intacts.
- Le build chargeable et le parcours local sont confirmés sur cette machine. La création/jonction via l’interface de cette recette reste non démontrée; il faut instrumenter le runtime/browser ou reprendre avec un navigateur desktop attachable avant de conclure. Aucun code de jeu n’a été modifié pour cet essai.

## Cause des essais HTTP et création WebGL sous HTTPS — 17 octobre

- La console du navigateur sur l’origine locale HTTP a rendu la cause explicite : `Non-secure network connections disabled in Player Settings`, puis `InvalidOperationException: Insecure connection not allowed`. Unity rejetait la requête avant qu’elle n’atteigne le proxy; le bouton et le serveur n’étaient pas à l’origine de cette absence de POST. Le réglage HTTP reste inchangé.
- Via un tunnel HTTPS temporaire vers le build Release et le serveur QA isolé, le client WebGL a créé la salle `QZJKLE` depuis **Create Online Game**. L’écran hôte a affiché `1/6`, le serveur a répondu `200` à la création et aux snapshots de polling, puis **Leave Lobby** est revenu au titre avec une réponse `200`. Le stockage QA final est `{"schemaVersion":1,"lobbies":[]}`.
- Le second client n’a pas atteint le menu dans l’aperçu intégré (canvas noir); une tentative Playwright headless avec SwiftShader n’a pas non plus initialisé un second canvas Unity. Un POST de jonction vide a reçu `404` et n’a créé aucun siège. La jonction WebGL à six n’est donc pas revendiquée sur cette tentative; les tests précédents à six profils restent les preuves disponibles.
- Les deux tunnels temporaires, le serveur statique et le service lobby QA ont été arrêtés; les onglets QA ont été fermés. Seul le service utilisateur `5080` et ses trois onglets préexistants restent actifs. Aucun code du jeu n’a été modifié; `git diff --check` passe.

## Recette simultanée six WebGL sur HTTPS — 17 octobre

- La build Release fraîche a été servie via un tunnel HTTPS vers un lobby serveur Release QA isolé. Six contextes Chrome indépendants ont complété Create → Join ×5 → Ready ×6 → Start depuis l'interface WebGL. Lobby QA `YJNRZD`, six noms distincts, six joueurs Ready, état `Started`.
- Les six pages ont reçu des snapshots `started` (5 à 9 snapshots par page au moment de la capture) sans erreur console et ont affiché le même état initial : hôte au tour 1 avec Roll Dice/Pass, invités en attente du tour de l'hôte. Stockage serveur : 6 joueurs, `turnState=Planning`, `currentPlayerId=0`, `gameState.version=1`, séquence de commande 0.
- La recette est archivée dans `Docs/Validation/SixWebGL_UI_2026-10-17/README.md` avec les captures des six états clients et du roster six sièges. Le viewport de test était 960×540 pour garder les six canvases WebGL opérationnels sur le même Mac; cela ne remplace pas un test sur six appareils.
- Aucun code de jeu ni réglage Unity n'a été modifié. L'essai précédent à cinq clients avait fait apparaître une limite de capture/charge dans Chrome; la capture via CDP et la résolution réduite ont permis de mener celui-ci à terme.

## Revalidation fraîche Unity et serveur — 17 octobre, reprise

- Depuis la source actuellement présente dans le dépôt, Unity **2022.3.62f3** a exécuté `RunAllEditorRegressions` : Économie **114/114**, Mécaniques **148/148**, Événements **14/14**, UI **262/262** — **538 assertions PASS**.
- Build WebGL Release `BuildOptions.None` : **PASS**, code de sortie 0, sortie QA isolée `/tmp/PolyRiskFinalContinuation_20261017`. Payload compressé data/framework/WASM + loader : **12 721 531 octets**, soit **+12 octets** par rapport à la précédente mesure consignée. Aucun preview partagé n'a été remplacé.
- `smoke_test.py` : **PASS** à six joueurs, refus du septième, démarrage et commandes. `persistence_test.py` : **PASS**, identifiants, file de commandes et état restaurés après redémarrage. `six_player_reconnect_test.py` : **PASS**, reprise des six sessions, reçu de loyer partagé typé et parts bénéficiaires après reconnexion/transfert d'hôte, livraison de commande vérifiée. Chaque script a lancé son propre serveur et son stockage temporaires; aucun n'a utilisé l'instance déjà active sur `127.0.0.1:5080`.
- `git diff --check` : **PASS**. Aucun fichier source runtime n'a été modifié par cette revalidation. Le journal Unity est `/tmp/PolyRiskFinalContinuation_20261017.log`; il affiche un avertissement d'authentification de licence au démarrage mais termine avec le passage des quatre suites et `WEBGL PLAYER BUILD PASS`.
- Ces contrôles renforcent la preuve de compilation et de protocole; ils ne remplacent pas la synchronisation visuelle en partie complète sur six clients, Safari/iPhone physique, le profilage mobile, les tests humains ou la validation des écrans Venture/faillite/victoire.

## Tentative de recette visuelle des écrans de fin — non concluante

- Build WebGL Development temporaire `BuildOptions.Development` : compilation **PASS**. Le navigateur graphique isolé a chargé le menu, mais les raccourcis QA F5/F6/F7 n'ont pas affiché Venture, faillite ou victoire depuis cet état. Aucune capture de ces écrans n'est comptée comme preuve; ce résultat ne signale pas de défaut du produit.
- Le serveur WebGL de recette et le profil Chrome temporaire ont été arrêtés et supprimés. Aucun client n'a été connecté à l'instance lobby utilisateur `127.0.0.1:5080`, qui est restée active. Les captures temporaires n'ont pas été conservées.
- Prochaine tentative : entrer dans une partie locale déterministe avant d'appeler les raccourcis de showcase, ou fournir une fixture Editor/PlayMode reproductible; vérifier les trois vues, puis consigner les captures Release séparément des captures Development.
