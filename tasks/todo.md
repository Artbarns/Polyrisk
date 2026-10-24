# Objectif global d’achèvement PolyRisk

> Achever PolyRisk en construisant le multijoueur en ligne jusqu’à six joueurs, puis terminer la recette mobile/WebGL, les tests humains, l’équilibrage et les finitions de publication.

Cette phrase est l’objectif de référence du projet. Le suivi détaillé ci-dessous documente le travail, mais ne remplace pas les jalons de clôture. L’objectif Codex associé est actif ; son état et cette todo restent deux suivis distincts.

> **Point de reprise — 16 octobre 2026 :** session mise en pause à la demande d’Arthur. La recette WebGL Release à six clients indépendants via l’interface est passée (les six joueurs ont joué un tour et les états convergent) ; rapport et captures : `Docs/Validation/SixIndependentUI_CurrentRelease_2026-10-16/README.md`. Serveur local, prévisualiseur et tunnel HTTPS arrêtés à la pause ; les changements non commités sont conservés. Reprendre par les tests sur appareils réels (Safari/iPhone, interactions tactiles et performance), puis les mécaniques/équilibrage et la publication listées ci-dessous. Les six navigateurs venaient du même Mac : cela ne valide pas six appareils physiques.

> **Reprise de la recette — 17 octobre 2026 :** dépôt courant revalidé dans Unity 2022.3.62f3 : 538 assertions Editor PASS (dont commande hôte Unity `action.primary` et partage de loyer 25/75), build WebGL Release fraîche PASS (sortie QA isolée) ; serveur lobby recompilé puis smoke, persistance/redémarrage et reconnexion/transfert d’hôte six joueurs PASS. La build fraîche a aussi été chargée dans Chromium à 414 × 720 et jouée localement jusqu’à une arrivée et sa fiche propriété. Cette vérification ne remplace pas Safari/iPhone physique, la partie à six appareils, le profilage mobile ou le playtest humain. Détails : `Docs/Validation/GoalContinuation_2026-10-17.md` et `Docs/Validation/SplitRentTypedReceipt_2026-10-17.md`.

> Complément : la même build Release fraîche passe aussi Create/Join/Ready/Start et un flux à deux clients WebGL sur deux origines isolées. Le snapshot et les deux interfaces concordent après lancer, taxe de 600 $, arrivée French Riviera, passe et changement de joueur; les événements `DiceRolled`, `TaxPaid` et `PropertyPassed` et le solde 17 400 $ de l’hôte sont confirmés. Le split 25/75 a aussi été vérifié sur trois clients Unity WebGL : le payeur transfère 110 $ en deux faits corrélés de 27,50 $ et 82,50 $, conservés dans le snapshot et visibles dans les reçus du payeur/propriétaire. Le Dock arrondit la part locale à 83 $ et n’explique pas tout le partage : finition visuelle encore ouverte. Voir `Docs/Validation/SplitRentTypedReceipt_2026-10-17.md` et `Docs/Validation/SplitRentWebGL_2026-10-17/README.md`.

- [x] Base du multijoueur en ligne jusqu’à six sièges ; smoke test six joueurs et démarrage protocole PASS. Six clients WebGL indépendants ont déjà joué une partie synchronisée (`Docs/Validation/Mobile_Online_Preview_2026-10-12.md`) ; recette API Release isolée refaite le 14 octobre (salon `UFCXN4`, 6 sièges, refus du 7e, commandes/état, transfert d’hôte). Les six navigateurs QA étaient pilotés depuis le même poste ; cela ne remplace pas une partie humaine sur six appareils.
- [x] Serveur lobby revalidé sur la source actuelle le 16 octobre : build Release à 0 avertissement/0 erreur ; smoke six sièges/protocole, persistance après redémarrage et reprise/handoff des six sessions PASS (codes 0), chacun sur son serveur/données QA temporaires. Cette recette valide le service HTTP, pas six clients Unity ni une partie physique. Preuve : `Docs/Validation/SixPlayer_2026-10-16.md` ; rejeu en parallèle plus tard dans la même continuation PASS (smoke EV9FRQ, persistance, six sessions/handoff), sans toucher au preview partagé.
- [x] Harnais serveur simplifié pour la recette : `python3 smoke_test.py` démarre désormais un serveur et un stockage QA temporaires sur port libre ; protocole six sièges, persistance et reprise/handoff rejoués en parallèle après build Release, tous PASS. Une URL explicite reste disponible pour les tests d’un service existant. `Server/PolyRisk.LobbyServer/README.md`.
- [x] Partie entre amis à quatre jouée et confirmée par l’utilisateur ; boutons et panneaux jugés réactifs, aucun incident précis signalé. Les mécaniques non rencontrées pendant cette partie restent couvertes séparément par recette QA.
- [x] Relancer la préversion publique V44 ; le tunnel HTTPS et les services de recette répondent à nouveau. Le lien est temporaire et dépend du Mac hôte.
- [x] Préversion amis V67 (14 octobre) : build WebGL Release avec correctif de l’écran spectateur, service lobby .NET isolé et tunnel HTTPS ; lien public ouvert et création/rejoindre vérifiés avec deux clients WebGL invités (`3/6`). Lobby hôte prêt (`DEMJM2`). La partie humaine à quatre est confirmée ; l’utilisateur rapporte un test sur iPhone 14 Pro Max avec panneaux, boutons et Marketplace réactifs, sans défaut précis signalé. Durée, navigateur et mécaniques rares rencontrées restent inconnus. Fiche : `Docs/Validation/Playtest_Amis_2026-10-14.md`.
- [x] Régressions de la source Unity courante exécutées en batch le 14 octobre : Economy 106 et Mechanics 99 assertions, code de sortie 0. Détails dans `Docs/Validation/Unity_Batch_2026-10-14.md`.
- [x] Revalidation de la source le 15 octobre dans Unity 2022.3.62f3 : Economy 114, Mechanics 106 et build WebGL Release `BuildOptions.None` PASS dans un seul lancement batch. Sortie isolée `/tmp/PolyRiskPostPlaytestReview`, 12 MiB ; non publiée. La suite mécanique a ensuite été étendue à 110 assertions par des tests Editor seuls (enchère banque/tender) ; aucune source Player n’a changé. Détails dans `Docs/Validation/Unity_Batch_2026-10-14.md` et `Docs/Validation/Mechanics_Family_OPA_2026-10-15.md`.
- [x] Revalidation fraîche du source courant le 15 octobre : Economy **114/114**, Mechanics **117/117**, compilation WebGL Release (`BuildOptions.None`) PASS dans Unity 2022.3.62f3. L’artefact `/tmp/PolyRiskReleaseQA_2026-10-15` (12 MiB) a chargé en navigateur local et servi le loader, framework, WASM et données compressés en HTTP 200 ; non publié et sans lobby en ligne. Détail dans `Docs/Validation/Mechanics_Family_OPA_2026-10-15.md`.
- [x] Rejeu ciblé le 16 octobre : MarketplaceSystem.ExecuteSanction(ForceAudit) est appliqué, capturé par le snapshot de production, effacé puis restauré avec IsInAudit, 3 tours et case 10 ; l’immunité Pharma bloque aussi ce coût, ce déplacement et cette activité. MechanicsRegression passe **137/137** sous Unity 2022.3.62f3 ; ApplyCommand couvre marketplace.sanction/audit avec et sans immunité. Aucun transport HTTP n’est exercé et aucune source Player n’a changé. Cela ne remplace pas le test réseau à deux clients. Docs/Validation/ForceAudit_Snapshot_2026-10-16.md.
- [x] Réseau revalidé le 16 octobre : smoke lobby/protocole à six joueurs (lobby v13, snapshot v1), persistance après redémarrage (credentials hachés, queue, snapshot) et reconnexion six sièges (état, transfert d’hôte, commande) terminent tous avec code 0. Cela confirme le transport et la résilience du serveur, pas la sanction exécutée par Unity entre deux clients. `Docs/Validation/MarketplaceSanction_Transport_2026-10-16.md`.
- [x] Correctif de layout du sélecteur Marketplace : liste rétractable, feuille à 96 % et barre de confirmation verticale avec bouton pleine largeur (48 px minimum), sans toucher aux règles. Revalidation source courant : Unity 2022.3.62f3, Economy 114/114, Mechanics 137/137, WebGL Release `BuildOptions.None` PASS (code 0), `git diff --check` PASS. Sortie isolée `/tmp/PolyRiskEfficientFinal_20261016` ; détails et tailles dans `Docs/Validation/Unity_CurrentSource_2026-10-16.md`. La barre du dernier build et la commande n’ont pas encore été confirmées à l’écran. Parcours précédent et écart de payload : `Docs/Validation/MarketplaceSanction_WebGL_2026-10-16.md`.
- [x] Recette audit ciblé à deux clients WebGL, 992×720 : hôte −2 000 $, invité −900 $ (5 % de 18 000 $), snapshot version 5/séquence 1 → version 6/séquence 2, tour et activité répliqués, 0 erreur console. Release finale : défilement vertical, piste cliquable, aucune barre horizontale ni flèches natives ; les quatre sanctions restent accessibles. Unity 2022.3.62f3 : 519 assertions PASS, Release WebGL PASS, payload compressé principal + loader 12 700 348 octets. Limites : piste encore gris clair, LOG non ouvert, Safari/tactile physique à vérifier. Aucun changement aux règles. Captures et portée : `Docs/Validation/MarketplaceSanction_WebGL_2026-10-17/README.md` ; échec historique : `Docs/Validation/MarketplaceSanction_WebGL_2026-10-16.md`.
- [x] Checkpoints Unity du 15 octobre : HUD inspecteur et caméra ont été revalidés après le test amis. Economy **114/114**, Mechanics **130/130**, Release WebGL `BuildOptions.None` **PASS** (~12 MiB), artefact `/tmp/PolyRiskPortraitFitQA_20261015`. Chrome/Metal confirme le cadrage Planning en 932×430, les 40 cases dans le champ en 430×900, l’inspecteur après lancer/rotation, et un achat abouti en glissant la poignée dans le paysage court. Aucune erreur JavaScript ; la glissière répond dans sa zone visible. La marge de sécurité reste à vérifier sur Safari/iPhone physique. `Docs/Validation/Unity_FinalCheckpoint_2026-10-15.md`.
- [x] Revalidation pendant la continuation du 15 octobre : un batch Unity frais sur le source courant a passé `EconomyRegression` **114/114**, `MechanicsRegression` **130/130** et le build WebGL Release `BuildOptions.None` ; sortie isolée `/tmp/PolyRiskEfficientCheckpoint_20261015` (~12 MiB), non publiée, arrêt batch normal. Journal : `/tmp/PolyRiskEfficientCheckpoint_20261015.log`.
- [x] Une capacité ciblée, Cyber-sabotage Armement, a été exécutée entre deux clients WebGL Release : tour invité, retour à l’hôte, consommation des 100 points d’énergie, effet de sabotage trois tours sur la propriété Pharma adverse, journal serveur et état partagé PASS. La capture invitée est trop tardive pour prouver l’animation/toast visuel. `Docs/Validation/Online_Ultimate_Armement_2026-10-15.md`.
- [x] Pouvoir Pharma en ligne : annonce Protocole sanitaire visible chez l’invité ; snapshot confirme énergie 100 → 0 et immunité 1 tour. Après rechargement, l’invité reprend le plateau et l’effet persiste sans seconde dépense d’énergie. `Docs/Validation/Online_Ultimate_Pharma_2026-10-15.md`.
- [x] Statut persistant `PHARMA IMMUNITY · 1 TURN` recetté en WebGL : visible après activation/reconnexion ; vrai lancer 3+1 depuis la case 39 franchit Départ, expire l’effet (v2/s1) et masque la pastille ; l’invité restauré voit le même plateau. Unity Economy 114/114, Mechanics 130/130, build Release 12 MiB PASS. `Docs/Validation/Pharma_Immunity_Status_2026-10-16.md`.
- [x] Le premier essai du sélecteur invité reste non concluant (fixture installée après démarrage) et est conservé comme historique. Essai déterministe de remplacement : activation Pharma depuis l’interface WebGL de l’hôte, commande `ultimate.use`, énergie 100 → 0, immunité 1 tour, fonds inchangés, annonce visible chez l’invité et snapshot autoritaire version 2/sequence 1 : PASS. Une capture ultérieure montre un lancer 4 + 4 sans nouvelle commande ; cohérent avec le filet de sécurité de `Planning` à 60 s, mais le délai exact n’a pas été mesuré. `Docs/Validation/Online_Ultimate_UIQA_2026-10-15.md`.
- [x] La suite Editor passe maintenant à **123/123** : elle vérifie la capture/restauration des effets en ligne et les refus Aerospace (destination invalide) et Armement (cible Aerospace protégée), sans mouvement, sabotage ni dépense d’énergie lors d’un refus. Changements confinés au banc Editor ; aucune règle ni source Player n’a changé. `Docs/Validation/Mechanics_Family_OPA_2026-10-15.md`.
- [x] Rejeu frais le 15 octobre de `MechanicsRegression.Run` sur l’état courant du projet : **123/123 PASS**, batch Unity terminé normalement (`/tmp/PolyRiskOnlineUltimateCoverage.log`). Cela revalide les pouvoirs et snapshots locaux ; ne compte pas comme test d’application des commandes entre deux clients.
- [x] Pouvoir Tech exécuté depuis le sélecteur hôte sur deux clients WebGL Release : séquence 1/version 2, énergie 100 → 0, upgrades gratuits des propriétés 27 et 28, fonds inchangés, activité et snapshot autoritaires confirmés. La capture invitée n’a pas confirmé visuellement les niveaux ni conservé la bannière temporaire. `Docs/Validation/Online_Ultimate_Tech_2026-10-15.md`.
- [x] Revalidation combinée fraîche le 15 octobre : `EconomyRegression` **114/114**, `MechanicsRegression` **123/123**, build WebGL Release `BuildOptions.None` PASS (≈12 MiB) et accueil/lobby visibles au navigateur. Build QA isolé, non publié. `Docs/Validation/Mechanics_Family_OPA_2026-10-15.md`.
- [x] Rejeu frais additionnel `MechanicsRegression.Run` dans Unity 2022.3.62f3 : **126/126 PASS**, code 0. Confirme capture/restauration `ApplyState` des effets Pharma/Automobile/Oil/Tech/Armement et refus Aerospace/Armement. Cela reste la vérification locale du chemin snapshot, sans remplacer une reconnexion réelle à deux clients. Journal : `/tmp/PolyRiskMechanicsContinuation.log`.
- [x] Après la partie humaine à quatre, validation groupée Unity 2022.3.62f3 en un lancement : `EconomyRegression` **114/114**, `MechanicsRegression` **128/128**, build WebGL Release `BuildOptions.None` PASS (≈12 MiB dans `/tmp/PolyRiskPostFriendRelease`), code 0. Le gestionnaire autoritaire `ApplyCommand` rejette Aerospace vers cible invalide et Armement contre Aerospace sans dépense, effet ni mouvement. Contrôle Editor sans transport entre clients ; une recette WebGL réseau de ces refus reste à faire. Journal : `/tmp/PolyRiskPostFriendRelease.log` ; détails dans `Docs/Validation/Mechanics_Family_OPA_2026-10-15.md`.
- [x] Conformité des cycles macro au GDD : le CRASH applique désormais `×0,6` au salaire Sgo et aux dividendes en plus des loyers, sans réduire le Burn Rate ; les plafonds boursiers dépendent de la durée comme spécifié. Régressions Economy 114/114 et Mechanics 126/126, build WebGL Release PASS, lot corrigé 30 parties à 4 joueurs et échantillon apparié de 5 graines à 6 joueurs : zéro anomalie et aucun gagnant à 10 M$. L’échantillon six joueurs reste trop petit pour conclure sur l’équilibrage ; résultats détaillés dans `Docs/Validation/Economy_Cycle_Conformance_2026-10-15.md`.
- [ ] Recette finale sur appareils mobiles et WebGL, notamment iPhone 14 Pro Max, performances, netteté, orientation et reprise de partie. L’utilisateur confirme une partie à quatre sans défaut précis signalé ; navigateur iOS, durée, performance mesurée et parcours approfondi restent à consigner. Le plafond DPR 2 du template est revalidé sur le Release courant avec User-Agent iPhone et DPR3 : buffers 860×1864 en 430×932 et 1864×860 en 932×430, HTTP 200, zéro erreur JS (`Docs/Validation/Mobile_WebGL_2026-10-15/README.md`). Chromium desktop émule le téléphone ; Safari/appareil physique et profil cadence/chauffe restent ouverts. Build iOS non vérifiable sur le Mac actuel : Xcode et le SDK iPhoneOS sont absents (`xcode-select` ne voit que Command Line Tools) Recette approfondie du Release courant le 17 oct. : UA iPhone, touch, DPR3; canvas bien plafonné à 860×1864 portrait / 1864×860 paysage, taps démarrage/lancer et réseau sans erreur. À l’arrivée, le paysage chevauche le Dock avec la fiche et tasse les ROI contre le roster ; portrait lisible. Captures : `Docs/Validation/ReleaseMobileCurrent_2026-10-17/README.md`.
- [x] Sur la build Release de recette précédente, lancer une partie locale dans Chromium à 430×932, atteindre un inspecteur Pharma et accepter tactilement « Passe » ; capture de l’inspecteur et limites notées dans `Docs/Validation/ReleaseAudit_2026-10-16.md`. La recette Venture n’a pas été atteinte.
- [x] Flux d’activité harmonisé avec l’interface anglaise : le HUD traduit à l’affichage les messages français actuels et les entrées historiques des snapshots ; les noms de secours utilisent « Player »/« Game ». Les 42 formes d’activité testées, montant et nom de propriété conservés, passent dans UIAssetRegression ; build WebGL Release frais PASS. `Docs/Validation/ActivityFeedLocalization_2026-10-16.md`.
- [ ] Capturer et recetter en WebGL les textes/rendus de la carte Venture et des écrans Bankruptcy/Victory, y compris un aperçu anglais du bandeau après le correctif ; Safari/iPhone reste ouvert. Première recette isolée Development WebGL 960×720 le 17 oct. : lancement local puis F5/F6/F7, captures des trois vues, zéro erreur JS. Venture #20 laisse les badges et le plateau transparaître derrière le texte, défaut de lisibilité à corriger après choix visuel. Banque testé avec joueur sans actifs et victoire avec 2 joueurs seulement ; Release/mobile, autres cartes, états de liquidation et déclenchements réels restent ouverts. `Docs/Validation/EndScreensQA_2026-10-17/README.md`.
- [x] Ultimes Immobilier et Luxe recettés depuis l’interface invitée sur deux clients WebGL Release : commande réseau, reçu/snapshot autoritaires, dépense d’énergie, effet mathématique et soldes répliqués vérifiés. Preuves : `Docs/Validation/Online_Ultimate_RealEstate_2026-10-15.md` et `Docs/Validation/Online_Ultimate_Luxe_2026-10-15.md`.
- [x] Ultime Oil vérifié en ligne : boost +80 % enregistré et répliqué pour un tour, énergie consommée et activité serveur confirmés. L’ultime Automobile puis les deux jets et la sélection de la meilleure issue ont été joués depuis l’interface, avec reçus/snapshots `ultimate.use`, `turn.roll`, `dice.turbo` et consommation du bonus vérifiés. Preuves : `Docs/Validation/Online_Ultimate_Oil_2026-10-15.md` et `Docs/Validation/Online_Ultimate_Automobile_2026-10-15.md`.
- [x] Ultime Aerospace recetté depuis l’interface WebGL à deux clients : NASA Moonbase (case 33) sélectionnée, position autoritaire 33, énergie 100 → 0, activité unique « Saut quantique ». L’invité a vu la bannière et l’a retrouvée après rechargement ; snapshots avant/après et progression ultérieure du filet Planning consignés. `Docs/Validation/Online_Ultimate_Aerospace_2026-10-15.md`.
- [x] Refus d’Armement sur Aerospace validé entre deux profils WebGL rendus : version 3/séquence 1 reçue par l’invité, propriété intacte, aucune dépense ni activité, zéro exception JavaScript. Commande injectée via API QA ; le contrôle ne vaut pas partie humaine. Captures/snapshot dans `Docs/Validation/Online_Ultimate_Refusals_2026-10-15.md`.
- [x] Reconnexion invitée pendant un Cyber-sabotage Armement actif : deux clients WebGL Release, effet restauré à trois tours sur le client invité après rechargement, séquence non rejouée, énergie dépensée une fois, tour/phase préservés, zéro exception. Le snapshot hôte conserve les deux soldes exacts à 18 000 $ ; Dark Board projette le solde adverse à 20 000 $ chez l’invité. `Docs/Validation/Online_Ultimate_Armement_2026-10-15.md`.
- [x] Filet de sécurité `Planning` contrôlé dans le WebGL Release courant, d’abord en local puis en ligne à deux clients : sans input à 45 s ; à 65 s, hôte sur Venture tuile 7 avec Draw Card, invité sur la même tuile en attente. Snapshot persistant : version 2, séquence 0, `ResolveTile`, tour 1, joueur actif 0, positions 7/0. Cela confirme l’auto-skip et sa réplication ; seuil borné entre deux observations, pas à la frame exacte. `Docs/Validation/Planning_Timeout_2026-10-15.md`.
- [ ] Terminer la recette réseau des refus et de la reconnexion d’effets temporaires autres que Pharma. Refus Aerospace invalide et Armement contre NASA Moonbase : aucune mutation par l’autorité WebGL Release (`Docs/Validation/Online_Ultimate_Refusals_2026-10-15.md`). L’hôte rétablit Armement actif après reload sans seconde dépense ; Tech garde ses niveaux après reconnexion invitée ; Aerospace restaure la bannière après reconnexion invitée. Automobile Moteur turbo : snapshot avant/après reload maintient `autoTurboReady` (v3/s1), puis un second parcours a rechargé l’invité après `turn.roll` et montré le panneau « Choisissez votre jet » restauré (`ChooseTurbo`, v4/s2). Reçus, snapshots et compte rendu : `Docs/Validation/Online_Ultimate_Automobile_2026-10-16.md`. Le rendu de la cible est maintenant vérifié chez l’invité : la puce de rendement est cerclée en violet et la fiche affiche « CYBER-SABOTAGE · 3 TURNS » sur Opioid Paradise ; état miroir version 2/séquence 0. Build Unity Release, Economy 114 et Mechanics 130 PASS. `Docs/Validation/Armement_Target_Visual_2026-10-16.md`. Nouveau WebGL Release recetté à deux clients après reload complet : l’invité voit sur Greenwashing Solutions le badge Oil +80 % / 1 tour, Rent 778 $ et ROI 43,2 % ; état serveur v2/s0, invité actif et trois propriétés Oil conservés. Le calcul Rent/ROI utilise `RulesEngine.CalculateRent`. Release, Economy 114 et Mechanics 130 PASS (`Docs/Validation/Oil_Boost_Inspector_2026-10-16.md`). Restent les autres effets temporaires et la recette humaine. Le filet Planning a été validé en ligne entre 45 et 65 s (`Docs/Validation/Planning_Timeout_2026-10-15.md`).
- [ ] Équilibrage final de l’économie et validation du plaisir de jeu avec des joueurs humains. La partie à quatre sur iPhone 14 Pro Max est confirmée, avec panneaux/boutons réactifs, mais durée, issue, mécaniques rencontrées et rythme restent inconnus. La fiche consigne maintenant ces points et des notes comparables par siège ; recueillir 5 à 10 parties humaines avant tout ajustement. Le lot Unity courant de 100 graines appariées 20261106–20261205 termine sans anomalie : 0/100 victoires à 10 M$ à quatre joueurs; 1/100 à six joueurs, avec 11,19 M$ de cash final (graine 20261197). Médiane du cash gagnant : 458 381 $ à quatre, 1 219 417 $ à six. Cela confirme que le seuil est atteignable mais rare dans cet échantillon; aucune modification économique n’est justifiée sans retours humains. Résultats et CSV : `Docs/Validation/Economy_Paired100_2026-10-16/`.
- [x] Dark Board réseau : les réponses `game/state/get` filtrent maintenant les soldes adverses pour les invités en projetant le cash dans la tranche LOW/MID/HIGH/MOGUL ; l’hôte autoritaire conserve l’état exact et le serveur le remet intégralement au nouvel hôte après transfert. Tests API six sièges/reprise vérifient les seuils de chaque tranche, la projection par siège, la persistance et le handoff ; smoke test lobby/protocole et test de persistance passent aussi. Le serveur corrigé est en préversion HTTPS et son flux create/join/start/snapshot a passé le test à deux sièges. Limite hôte et lien temporaire dans `Docs/Validation/DarkBoard_Snapshot_2026-10-15.md`.
- [ ] Finir l’UI/UX et les interactions visuelles, contrôler les panneaux et enchaînements face au mockup, profiler et préparer la publication. Marketplace : avatars ronds, état vide et cinq offres/actions à six joueurs en 430×900 et 900×430 validés ; arrivée d’offre réelle rafraîchie en direct dans WebGL ; invitation confirmée lisible sans badges ROI superposés, minuteur rouge à 3 s et badges restaurés après expiration (Release Economy 114, Mechanics 130 PASS). Recette runtime comparative sur le même Release et la même origine, 15 oct. : préférence activée puis désactivée depuis le menu ; les deux états sont visibles et persistent après démarrage/rechargement. Une offre entrante de 3 000 $ en mode réduit et de 3 200 $ en mode normal est apparue dans la Deal List et le feed. Les autorités confirment respectivement version 3/séquence 1, deux offres et passage à Reviewing. La fixture doit placer le vendeur sur la case Marketplace 20. Le code ajoute la classe d’entrée et sa transition de 180 ms en mode normal, et omet cette classe avec transitions à 0 ms en mode réduit. Les captures ont montré l’état final, pas une image intermédiaire de ces 180 ms ; le déclencheur, le résultat de chaque préférence et les deux parcours réseau sont confirmés. Correctif d’accès au lobby : le champ de code et `JOIN` sont empilés à toutes les largeurs. Deux navigateurs WebGL indépendants ont créé/rejoint `B87TR2` avec la souris ; l’hôte affichait les deux joueurs. Unity Release courant : Economy 114/114, Mechanics 130/130, build PASS. Safari/iPhone réel reste à vérifier. `Docs/Validation/Menu_Join_Responsive_2026-10-15.md`. Comparaison au mockup toujours impossible faute d’images accessibles ; profilage et Safari/iPhone réel restent ouverts. Correctif WebGL Release 17 oct. : l’axe horizontal et les flèches natives sont supprimés, le curseur vertical est violet, cliquable et révèle les quatre choix à 992×720. La piste de fond reste gris clair ; le tactile Safari/iPhone et son apparence restent à vérifier. `Docs/Validation/MarketplaceSanction_WebGL_2026-10-17/README.md`. `Docs/UI_Motion_Audit_2026-10-13.md`.
- [x] Menu de partie : bouton discret dans le HUD, reprise, confirmation de sortie locale et abandon synchronisé pour tous les joueurs en ligne ; retour à l’accueil avec session réseau nettoyée. Release WebGL servie depuis `/tmp/PolyRiskWebGLQuitMenuFinal` sur la préversion HTTPS ; recontrôle du lien HTTP 200 et test isolé à deux joueurs (création, Ready, démarrage, abandon hôte, état abandonné visible pour les deux sessions) PASS le 15 octobre 2026. La recette complète avec des amis reste ouverte.
- [ ] Construire et vérifier les livrables de publication après clôture des jalons précédents.
- [x] Inventaire de recette des écrans UI : accueil/lobby, HUD, plateau, inspecteur, flux Marketplace, Venture, faillite, victoire et menu de sortie associés aux preuves existantes ; gaps physiques, mockup et vues de fin isolés sans confondre build, tests de règles et rendu. `Docs/Validation/UI_Panel_Inventory_2026-10-16.md`.
- [x] Mouvement du pion : arc existant complété par un rebond discret à chaque arrivée (effets intermédiaires superposés au segment suivant ; +80 ms max à l’arrivée finale), sans objet/draw call/librairie, désactivé avec « Réduire les animations ». Unity Release : Économie 114/114, Mécaniques 140/140, build PASS ; payload +436 octets. La capture du mouvement WebGL reste à faire. `Docs/Validation/PawnBounce_2026-10-16.md`.
- [x] Garde UI Toolkit : nouvelle suite Editor charge et clone les 17 écrans/composants UXML et vérifie les actions/champs critiques (**194 assertions PASS**). Appelée par la recette Unity intégrée. Cela protège la structure des panneaux, pas leur placement, comportement ou apparence. `Assets/Scripts/Editor/UIAssetRegression.cs`, `Docs/Validation/UI_Panel_Inventory_2026-10-16.md`.
- [x] Recette Editor groupée sans build Player : `RunAllEditorRegressions` passe Économie 114/114, Mécaniques 140/140 et UI 196/196 (**450 assertions**, Unity batch terminé avec succès). `BuildVerifiedWebGLPreview` appelle le même groupe avant le build, ce qui évite de relancer le link WASM pour des contrôles Editor seuls.
- [x] Mouvement réduit transversal : carte Venture sans rotation/zoom d’entrée mais avec son code couleur positif/négatif/chaos; pulse des cases sélectionnables remplacé par un surlignage statique. Régressions groupées 450/450 et WebGL Release frais PASS (`BuildOptions.None`), payload compressé total 12 680 967 octets avec loader ; aucune publication. Le comparatif visuel navigateur/appareil réel reste ouvert. `Docs/Validation/ReducedMotionVenture_2026-10-16.md`.
- [ ] Recetter le preview public : l’ancienne URL de l’onglet renvoie NXDOMAIN et son canvas montrait « Unknown Error » en lobby 1/6. Le tunnel déjà lancé a été récupéré sans le redémarrer : `https://cooper-justin-seven-devel.trycloudflare.com/` répond 200 et sert index, WASM, data et framework, mais pointe vers `/tmp/PolyRiskWebGLQuitMenuFinal` (préversion du 14 oct.), pas le Release du 16 oct. Build frais isolé : 450 assertions avant build, WebGL PASS; le groupe courant est à 457 assertions; protocole six joueurs PASS via relais local. CUA n’a pu attacher ni l’onglet local, ni le nouvel onglet HTTPS ; Chrome headless rend aussi noir l’ancien build témoin sous SwiftShader, donc pas de conclusion visuelle. Aucun tunnel créé/remplacé, aucune commande ni lobby existant touché. Refaire le parcours sur navigateur graphique et décider séparément si le tunnel peut être repointé vers la build fraîche. `Docs/Validation/PreviewStatus_2026-10-16.md`, `Docs/Validation/ReleaseAudit_2026-10-16.md`.

- [ ] **Chantier UI/UX — lisibilité, lancer et ambiance (retours de test humain)** : plusieurs joueurs ne comprennent pas assez vite ce qui se passe à l’écran. Le prototype de dés est fait et validé à deux clients; le panneau inspecteur est dégagé pendant le lancer. Restent à rendre immédiatement visibles le joueur actif, l’étape du tour et l’action attendue; mieux faire ressortir le pion et évaluer un suivi caméra doux; corriger la taille, le contraste et le placement des pourcentages de rendement; retravailler profondeur/lumière sans glow uniforme. La capture montre que la jonction Dock/inspecteur reste trop dense et demande une hiérarchie/respiration dédiée. Présentation/motion uniquement; garder « Réduire les animations ». Vérifier les formats mobile/desktop, profiler et recetter sur appareil réel.
- [x] Dé central vérifié en ligne : carte UITK animée affichant le joueur, les deux valeurs autoritaires et le total, avec SKIP et Réduire les animations. Pendant l’animation, l’inspecteur, le Dock et ses boutons sont masqués puis restaurés; les faits typés transitent dans le snapshot et arrivent sur le client invité. Tests Unity : 114 économie + 141 mécaniques + 12 événements + 252 UI (**519 assertions**); build WebGL Release `BuildOptions.None` PASS (~12 Mo); recette deux clients Chromium, dés 2+6, événement reçu, 0 erreur console. Captures et détails : `Docs/Validation/DiceShowcase_Network_2026-10-17.md`. Safari/iPhone physique non testé.
- [x] Audit UI/UX étendu et catalogue des événements avec chorégraphies, décisions UX, efforts/risques/coûts perf : `Docs/Validation/VisualUX_MasterPlan_2026-10-16.md`. Constat central : flux UI encore textuel; reçus typés et leur réplication sont prérequis aux animations argent/roster.
- [x] Première fondation non visuelle : faits `GameEvent` immuables, corrélation, parts bénéficiaires, Presenter priorisé/skippable, historique borné, mouvement réduit et tests dédiés. Producers typés ajoutés après confirmation pour dés/achat/transferts; suite Unity batch 114 économie + 141 mécaniques + 8 événements + 252 UI PASS.
- [ ] Terminer l’intégration UI de cette fondation : le transport additif, le watermark/dédoublonnage, la carte de dés et le roster compact sont faits. Le Dock consomme maintenant achat, transferts, passe, taxe et amélioration confirmés via le Presenter priorisé. Il reste à couvrir les reçus pouvoirs/OPA, enchères, Venture et faillite, vérifier loyer à deux clients, 2–6 clients et portrait/paysage, profiler et recetter sur iPhone réel. La préversion partagée n’a pas été remplacée.
  - [x] Dock branché au Presenter : achat confirmé et transferts affichés depuis les faits autoritaires (acteur, montant, bénéficiaires, motif, tuile), historique anti-doublon, animation skippable et retour compact pendant une décision sur tuile. Passe, taxe et amélioration émettent aussi un reçu après confirmation. Unity 2022.3.62f3 : Economy 114, Mechanics 141, événements 14, UI 262 (**531 assertions**); Release WebGL `BuildOptions.None` PASS (~12 MiB). Navigateur local : achat confirmé et passe réel affichés dans le Dock. Le Presenter fusionne aussi un loyer réparti; formatter et séquence couverts par tests. Taxes/améliorations runtime, transfert entre joueurs en réseau, OPA, Safari/iPhone et profilage restent à recetter. `Docs/Validation/TypedGameEventDock_2026-10-17.md`.
  - [x] Roster responsive : une pastille par joueur avec couleur cohérente avec le pion, initiale/nom compact, niveau de richesse privacy-safe, contour du joueur actif et état faillite. Rafraîchi au changement de tour/solde, sans polling ni dépendance. Testé en navigateur WebGL local à 4 joueurs; la disposition 6/6 et l’iPhone physique restent à recetter. `Docs/Validation/PlayerRoster_2026-10-17.md`.
  - [x] Première passe 16 oct. : faces de dés animées dans le feed avec priorité au message de jet sur petit écran ; pion actif agrandi/éclairci sans changer le collider monde ; badges de rendement recentrés et lisibles ; bloom, vignette, reflets temps réel réduits. Build WebGL et tests Editor passent (114 économie, 141 mécaniques, 246 UI). Recette portrait locale 332×736 : jet 4+1 visible avant l’inspecteur d’arrivée. Voir `Docs/Validation/BoardFeedback_UX_2026-10-16.md`.
  - [ ] Compléter la revue écran par écran face aux mockups, mesurer portrait/paysage, capturer les frames d’animation et profiler WebGL/iPhone réel ; recueillir la validation visuelle d’Arthur.

# Journal de travail et tâches détaillées

## Banc de stress IA ×1000 — tâche historique

Stress test IA x1000 parties. Plan complet :
[wondrous-discovering-teacup.md](/Users/arthurbarreau/.claude/plans/wondrous-discovering-teacup.md)

## Plan
- [x] `Assets/Scripts/Debug/StressTestRunner.cs` — driver complet (batch coroutine,
      séquence de reset, capture par partie, CSV + résumé)
- [x] `Assets/Scripts/Systems/GameBootstrapper.cs` — `ForceAllAI` (2 lignes, additif,
      comportement par défaut inchangé)

## Review

**2 bugs pré-existants trouvés et vérifiés en lisant le code (pas des suppositions)** :
1. `GameBootstrapper.cs:99` — le joueur 0 était toujours humain, bloquant toute partie
   100% IA. Corrigé via `ForceAllAI` (flag statique, off par défaut).
2. `FinanceSystem.cs:261` (victoire à 10M$) ne positionne jamais `isGameOver` dans
   `TurnManager` — la coroutine de tour continuerait indéfiniment après ce type de
   victoire précis. Géré côté harness : `StopAllCoroutines()` appelé dans TOUS les cas
   de reset, pas seulement en cas d'anomalie.
3. Bug latent supplémentaire trouvé (pas corrigé, hors scope — juste détecté) : si les
   4 joueurs font faillite au même tour, aucun gagnant n'est jamais désigné et le jeu
   boucle silencieusement. Le harness s'abonne à `OnPlayerBankruptcy` et détecte ce cas
   immédiatement (`ANOMALY_ALL_BANKRUPT`) plutôt que d'attendre le plafond de 90s.

**Ce qui a changé** :
- Nouveau : `StressTestRunner.cs` — `MonoBehaviour` de debug (même patron que
  `DebugMarketplaceSetup.cs`), touche F9. Accélère via `Time.timeScale` (aucun bypass de
  la logique de jeu). Reset entre parties : `StopAllCoroutines` + destruction des 6
  singletons `DontDestroyOnLoad` + `SceneManager.LoadScene`. Garde-fou 90s réelles par
  partie (`Time.realtimeSinceStartup`, indépendant de `timeScale`). Résultats écrits
  dans `StressTestResults/` (hors `Assets/`) : un CSV par partie + un résumé texte.
- Modifié : `GameBootstrapper.cs` — `ForceAllAI` statique, 2 lignes.

**Pas vérifié** : aucun accès Unity dans cette session — ni compilation, ni exécution.
Étapes manuelles pour Arthur (détaillées dans le plan) :
1. Ajouter le script sur un GameObject vide dans la scène.
2. **Smoke test d'abord** : 5 parties, `timeScale=10`. Entrer en Play, appuyer F9.
3. Vérifier le CSV (5 lignes, balances plausibles, zéro anomalie) avant de monter à 1000.
4. Lire le résumé pour la vraie question du backlog : l'économie reste-t-elle stable ?

## Itération 2 — bugs trouvés en testant réellement

Retours d'Arthur (premier vrai test en jeu) :
- [x] **Bug dans `StressTestRunner.cs`** : avec `numberOfGames=1`, Arthur devait quand
  même cliquer Buy/Roll Dice lui-même. Cause : `GameBootstrapper.Start()` s'exécute
  automatiquement à l'entrée en Play Mode, AVANT que F9 ne soit pressé — la partie déjà
  en cours a donc été bootstrappée avec `ForceAllAI` encore à `false`. Fix : reset
  initial (`DoReset()`) avant la boucle de `RunBatch()`, pour que même la partie 0
  tourne avec `ForceAllAI=true`.
- [x] **Bug Marketplace confirmé et corrigé** (`Marketplace_UIManager.cs`,
  `HandleAuctionStateChanged`, cas `Closing`) : ne fermait le panneau que si l'IA était
  vendeuse — supposait à tort que `CloseMarketplace()` était toujours appelé ailleurs
  pour un vendeur humain (vrai seulement pour le cas "deal scellé", pas pour "tender
  annulé, aucune enchère"). Le bottom sheet restait bloqué indéfiniment après un Reject
  All sans offres. Fix : appel inconditionnel, sans risque (double appel déjà idempotent
  ailleurs dans le code).
- [ ] **Offres IA Marketplace** — diagnostic statique V57 : le modèle projette 5 loyers puis soustrait encore le prix de base. Sur 22/26 propriétés à loyer positif, la valeur est négative au niveau 0 (ex. Trump Tower : −450 $) et nulle au niveau 1 (0 $), ce qui force un Skip quel que soit le profil ; seules 22/26 deviennent positives au niveau 2, hors bonus famille. Cela explique les refus observés sans prouver que le comportement est amusant ou souhaité. Mesurer les offres/niveaux en partie et faire les 5–10 parties humaines prévues avant tout changement économique ; détail dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.
- [x] **Bug racine du blocage systématique trouvé grâce au stress test** — 7/7 parties
  timeout à 90s, `EndedOnLap` quasi nul à chaque fois. Logs Console (3 runs identiques)
  montrent le blocage exactement après "Paid fine immediately after landing. Turn Ends."
  Cause (`TurnManager.cs:441-445`, `AuditPayFine()`) : appelle `CompleteTileResolution()`
  qui exige `currentState == ResolveTile`, mais l'état réel à ce moment est
  `WaitingForAuditDecision` — l'appel était un no-op silencieux, la partie restait
  bloquée indéfiniment dans cet état. La branche voisine ("IA ne peut pas payer") avait
  déjà le bon correctif (`currentState = TurnState.EndTurn;` direct) — jamais appliqué
  à la branche "paiement réussi", qui est celle qui plantait à chaque fois. Fix : même
  correctif appliqué. Vérifié que l'autre branche audit (`ExecuteMovementAfterAuditEscape`)
  n'a pas le même problème — elle passe par le flow normal `MovingPawn → ResolveTile`.

**Pas vérifié** : toujours aucun accès Unity — le fix est validé par traçage précis de la
séquence d'états (pas une supposition, le bug est 100% reproductible dans les logs
fournis), mais pas par un test réel après correction.

## Itération 3 — plus de blocage, juste un mauvais calibrage

Après le fix audit, tous les batches (10 parties sur 2 runs) montrent encore 100%
`ANOMALY_TIMEOUT` à 90s. Mais Arthur confirme qu'une partie laissée tourner va bien
jusqu'à la victoire du dernier joueur, sans blocage — juste lentement. Conclusion :
plus de vrai deadlock, seulement un plafond de 90s trop juste pour la durée réelle
d'une partie complète à `timeScale=10`.

- [x] `StressTestRunner.cs` — defaults remontés : `timeScale` 10→30, `perGameTimeoutRealSeconds`
      90→240. Pas encore reconfirmé par un run réel après ce changement.

**Pas vérifié** : aucun accès Unity — à confirmer par Arthur que ces nouvelles valeurs
donnent enfin des parties `Normal` plutôt que 100% anomalies.

## Itération 4 — fix du Min() pour le timeout tour 50

- [x] `TurnManager.cs:921` — `players.Min(p => p.LapsCompleted)` incluait les joueurs en
      faillite (compteur figé dès l'élimination) → le timeout tour 50 pouvait ne jamais
      se déclencher. Exclu les faillis du calcul (`Where(!IsBankrupt)` avant le `Min`),
      garde-fou si la liste filtrée est vide.
- Clarifié à Arthur (pas un bug) : le TopHUD affiche `Player0.LapsCompleted + 1`
  spécifiquement, pas un compteur global — reste visuellement figé si Player 0 est
  éliminé, même partie/fix qui fonctionnent bien en interne. Pas changé sauf si demandé.
- Clarifié à Arthur (pas un bug) : le harness enchaîne automatiquement les parties du
  batch (`numberOfGames`) — "ça recommence de zéro" tout seul est normal si
  `numberOfGames > 1`.

**Pas vérifié** : aucun accès Unity — fix du Min() basé sur lecture de code, pas testé.

## Itération 5 — perf réglée (reflection probe + stack trace), mais nouveau deadlock trouvé à l'échelle

Run de 5 parties (`timeScale=100`) : 0 anomalie, 8-50s réelles/partie. Les deux fixes de
perf de l'itération précédente tiennent la route. Mais un run suivant de 20 parties (même
config) est retombé à **17/20 `ANOMALY_TIMEOUT` (85%)**, toutes bloquées très tôt
(`EndedOnLap` 0-7), sans erreur console, sans faillite en cascade. Diagnostiqué à partir
d'un export console complet (70k lignes) fourni par Arthur — pas une supposition :

- [x] **Race condition trouvée et corrigée** (`TurnManager.cs`, `ExecuteMovement()`) :
  `GameEvents.OnPlayerPathCalculated?.Invoke(...)` était appelé **avant** la souscription
  à `OnPawnMovementFinished`. Si `BoardManager.GetPath()` retourne un chemin vide (arrive
  dès qu'une entrée `visualTiles[i]` est `null` sur le trajet — cf. `BoardManager.cs:131-140`),
  `PawnMovement.MoveAlongPath` termine de façon **synchrone** et déclenche
  `OnPawnMovementFinished` immédiatement — avant que `TurnManager` ne soit abonné. Le
  `yield return new WaitUntil(() => movementFinished)` attendait alors un event qui ne se
  reproduirait jamais : blocage permanent, sans aucune erreur. Confirmé par les logs :
  toutes les parties bloquées s'arrêtent net juste après un jet de dé / au tout début de
  `ExecuteMovement`, jamais ailleurs.
  Fix : souscription déplacée avant l'invocation (élimine la race), + garde-fou 10s
  réelles (`Time.realtimeSinceStartup`, indépendant du `timeScale`) en filet de sécurité,
  même pattern que le safety-net déjà existant sur la phase Planning humaine (qui, lui,
  anticipait déjà ce genre de blocage — juste jamais appliqué à `ExecuteMovement`).

**Pas vérifié** : aucun accès Unity dans cette session — fix basé sur traçage précis du
code (source du bug identifiée avec certitude), pas retesté après correction. Prochaine
étape pour Arthur : relancer un batch de 20+ parties et confirmer que le taux d'anomalie
retombe à 0 (ou au pire au bug latent connu `ANOMALY_ALL_BANKRUPT`, toujours pas corrigé
par choix — juste détecté par le harness).

## Itération 6 — vrai goulot trouvé : ce n'était pas un deadlock de code

Le fix de l'itération 5 n'a quasi rien changé (70-85% `ANOMALY_TIMEOUT` persistant sur
des runs de 20-30 parties). Diagnostiqué avec des logs `[DIAG]` temporaires (retirés
depuis) tracés dans `ExecuteMovement` + un heartbeat mesurant `Time.frameCount` :

- [x] **Vrai diagnostic** : l'Editor Unity ne tournait qu'à **1-4 frames par ~2s réelles**
  pendant tout le batch — pas un deadlock logique, un vrai ralentissement moteur.
  Cause la plus probable : la fenêtre Console qui dégrade très mal au-delà de dizaines de
  milliers de lignes (70k-120k lignes mesurées par run, avec 150+ points de log/tour).
- [x] **Fuite trouvée en même temps** (`GameEvents.OnPawnMovementFinished` : 1→2→...→6
  abonnés sur un seul batch) : `Destroy()` est différé en fin de frame, et un seul
  `yield return null` dans `DoReset()` ne suffit pas toujours à laisser `OnDisable()`
  désabonner avant le `SceneManager.LoadScene()` suivant, surtout sous le ralentissement
  ci-dessus. Fix : tous les `GameEvents` pertinents nettoyés explicitement dans `DoReset()`
  (sauf `OnGameVictory`/`OnPlayerBankruptcy`, dont `RunBatch` reste abonné tout le batch).
- [x] **Fix perf #1** : `Debug.unityLogger.filterLogType = LogType.Warning` pendant toute
  la boucle du batch (restauré après) — supprime le bavardage de routine sans toucher aux
  150+ call sites. Errors/Exceptions passent toujours (`ErrorCount` intact).
- [x] **Fix perf #2, le vrai morceau manquant** : Arthur a remarqué "les pions avancent
  d'une case en une case" — l'animation de hop (`PawnMovement.MoveRoutine`) impose au
  moins 1 frame réelle par case traversée, indépendamment de `Time.timeScale` (qui
  n'accélère que le temps de jeu, pas le nombre de frames réelles). `GameBootstrapper.SkipMovementAnimation`
  ajouté : téléportation directe pendant le stress test. Fonctionne correctement
  seulement grâce au fix subscribe-avant-invoke de l'itération 5 (la téléportation
  déclenche `OnPawnMovementFinished` de façon synchrone à chaque coup, sur tous les coups
  désormais, pas juste les cas limites).
- [x] **Faux problème résolu** : après ces fixes, Console totalement vide même en pressant
  F9 (mais jeu visiblement rapide). Cause : `Debug.unityLogger.filterLogType` restait
  "collé" en `Warning` d'une session Play interrompue à l'autre (`Reload Domain`
  désactivé). Un forçage `filterLogType = LogType.Log` + stack traces `ScriptOnly` en
  tout début de `RunBatch()` corrige ça de façon auto-réparante, quel que soit l'état
  laissé par un run précédent. Confirmé par un `Debug.LogError` inconditionnel par frame
  (retiré une fois confirmé) — les logs sont revenus dès la recompilation suivante.
- [x] **Bug trouvé au passage, corrigé** (`RulesEngine.cs`, `ExecutePrimaryAction`) : le
  cas `owner == null` + `TryBuyProperty` en échec (cash insuffisant) n'avait pas de
  `else` — tombait jusqu'au garde-fou générique en bas de la méthode, loggant
  `"Unhandled tile type Property"` (message trompeur, laisse croire à un case non géré).
  Comportement inchangé (la case reste juste sautée), mais le log dit maintenant
  vraiment ce qui se passe — utile pour la question de fond sur l'équilibrage (fréquence
  de ce skip = signal direct de joueurs à court de cash).

**Résultat** : le batch x100 tourne maintenant à vitesse normale avec logs propres.
Prochaine étape pour Arthur : laisser tourner le vrai run x100 (ou plus) et analyser les
résultats CSV — c'est la première fois que la question de fond du backlog (l'économie
reste-t-elle stable au tour 50 ?) peut vraiment être testée à l'échelle.

**Pas vérifié** : aucun accès Unity dans cette session — tous les fixes validés par
traçage précis de code + logs fournis par Arthur, pas par exécution directe de ma part.

## Itération 7 — le vrai run x100 (10h30, 99% timeout) révèle le bug de fond

Le run x100 réel a tourné 10h30 (17h24 → 03h48) : **99% ANOMALY_TIMEOUT**, 1 seule victoire
propre ($10M en 214s). Mais le CSV révèle deux populations très différentes :
- 6 parties ont réellement progressé (29-45 tours, Sgo $75K-$166K, soldes en millions) —
  preuve que les fixes de perf de l'itération 6 fonctionnent vraiment une fois lancés.
- 69/100 finissent à `EndedOnLap=0` avec Sgo quasi jamais monté, et plusieurs parties ont
  dépassé le plafond de 240s réelles jusqu'à **1845s (30 min)** — signe d'un vrai blocage,
  pas juste un manque de vitesse.

Le premier export console (10h30 de session) ne montrait presque rien d'utile — hypothèse :
buffer Console limité en lignes, ne gardait que la toute fin de la session. Un second run
ciblé de 10 parties (~qq dizaines de minutes, export console immédiat) a confirmé cette
hypothèse ET changé le diagnostic : sur ce run, **aucune** partie n'est restée bloquée à
`EndedOnLap` bas — les 10 ont progressé normalement (14-33 tours). Donc le blocage précoce
observé sur le run de 10h30 n'est probablement pas systématique, mais quelque chose s'est
quand même révélé dans les logs de ce run de 10 : **le vrai bug de fond**.

- [x] **Bug trouvé et corrigé** (`TransactionManager.cs`, `ProcessBankruptcyCash`) : le
  message `"Player X cannot afford $130000 rent. Triggering Bankruptcy flow."` se répétait
  **24 à 39 fois à l'identique** pour un même joueur dans une seule partie (confirmé par
  grep sur l'export console). Cause : `ProcessBankruptcyCash` — le filet de secours appelé
  par `RulesEngine` quand une IA insolvable ne peut toujours pas payer après le flow normal
  de liquidation (`GameEvents.OnInsolvency` → `BankruptcyScreenController.ProcessAILiquidation`)
  — transférait l'argent restant mais **ne posait jamais `IsBankrupt = true`**, contrairement
  à `ProcessAILiquidation` qui le fait correctement. Si le flow de liquidation normal ne se
  déclenche pas à temps pour une raison quelconque (même catégorie de risque que la fuite
  d'abonnement déjà trouvée sur `OnPawnMovementFinished` à l'itération 5 — souscription à
  `OnInsolvency` pas encore réarmée juste après un rechargement de scène), le joueur restait
  bloqué à $0 sans jamais être formellement en faillite : il continuait à jouer
  indéfiniment, retombant sur le même loyer impayable à chaque tour.
  **Impact direct sur le taux d'anomalie** : `TurnManager.cs:917-920` (condition de victoire
  "Last Player Standing") compte les joueurs non-bankrupt et déclare un gagnant dès qu'il
  n'en reste qu'un — ce bug bloquait donc directement la condition de victoire naturelle du
  jeu, forçant systématiquement le passage par le timeout de 240s peu importe la qualité du
  reste. Fix : `ProcessBankruptcyCash` pose maintenant `IsBankrupt` et déclenche
  `OnPlayerBankruptcy` lui-même, de façon autonome, peu importe ce qui s'est passé avant.

**Pas vérifié** : aucun accès Unity dans cette session — fix basé sur traçage précis de
code + comptage exact des répétitions dans les logs fournis par Arthur, pas retesté après
correction. Prochaine étape pour Arthur : relancer un batch (10-30 parties suffisent pour
valider avant de remonter à 100+) et vérifier que le taux de `LAST PLAYER STANDING` /
`REACHED $10M` remonte franchement au-dessus de 1%.

## Itération 8 — le fix marche (96.7% LAST PLAYER STANDING), le vrai sujet est l'équilibrage

Batch de 30 parties après le fix de l'itération 7 : **96.7% `LAST PLAYER STANDING`** (contre
99% d'anomalie avant), 1 seule `ANOMALY_TIMEOUT`. Le fix de faillite marche exactement comme
prévu. Mais les données propres révèlent le vrai sujet : parties très courtes (`Tour de fin`
moyen = 0.4), **29/30 parties se terminent avec exactement 3 joueurs sur 4 en faillite** —
pattern quasi uniforme. Winner rate par profil sur cet échantillon plus large : Negotiator
52%, Rentier 31%, Predator 17% (correction d'une lecture précédente sur trop petit
échantillon qui suggérait Rentier "ne gagne jamais").

- [x] **Diagnostic `[BANKRUPTCY-TRACE]` posé** (`RulesEngine.cs`, avant `OnInsolvency.Invoke`)
  pour tester l'hypothèse "IA achète sans réserve de trésorerie" proposée par une revue
  externe. Run de 30 parties → 143 événements capturés.
- [x] **Analyse quantifiée des 79 vrais déclenchements** (après dédoublonnage, voir bug
  ci-dessous) :
  - 71% des loyers tueurs viennent de tuiles **niveau 4-5** (quasi/full upgrade), 10%
    seulement de niveau 0 — le vrai moteur n'est pas "achète sans réserve" mais
    **"upgrade trop vite jusqu'au niveau 5 (×200)"**.
  - 94% des morts arrivent en 0-2 tours de plateau complétés.
  - Loyer médian tueur $19 200, mais 30% des morts viennent d'un loyer ≥$100K (jusqu'à
    $260K) — aucune réserve de cash raisonnable à ce stade de l'économie (Sgo encore
    ~$2-5K) ne protège contre un pic pareil.
  - Tuiles les plus mortelles : Lockheed Martin, CyberTruck Factory, NASA Moonbase,
    SpaceX, Antarctica Datacenter — toutes familles Aerospace/Tech/Auto (les plus chères
    du plateau).
- [x] **Bug trouvé au passage, corrigé** : 45% des événements tracés étaient des doublons
  exacts (même tuile/loyer, `cash_before=0`) — preuve d'un **double-dispatch IA**.
  Root cause (`TurnManager.cs`, case `ResolveTile`) : le bloc Auto-Pay Rent/Tax (qui
  appelle `rulesEngine.ExecutePrimaryAction` directement et résout déjà la tuile via
  `CompleteTileResolution`) était suivi, sans condition, par `aiManager.RequestAIDecision`
  pour toute IA — qui rappelle `ExecutePrimaryAction` une 2e fois sur la même tuile. Fix :
  la délégation IA ne se déclenche plus que si `currentState` est encore `ResolveTile`
  (i.e. la tuile a vraiment encore besoin d'une décision : case libre, Marketplace,
  Venture, Casino, Stock Market — les seuls cas que `TurnManager` ne pré-résout pas).
- [x] **Vérification demandée par la revue externe faite, RAS** : le tableau
  `rentMultipliers = {1,2,5,12,50,200}` (6 entrées, indices 0-5) est indexé exactement par
  `property.CurrentLevel` (qui va aussi de 0 à 5) — pas d'off-by-one, le ×200 ne s'applique
  bien qu'au niveau 5.
- [x] **3 logs d'instrumentation ajoutés** (tous en `LogWarning` pour survivre au filtre du
  stress test) pour la suite de l'investigation demandée par la revue externe :
  `[UPGRADE]` (`UpgradeSystem.TryUpgradeProperty` — joueur, niveau avant/après, coût,
  cash avant/après), `[ACQUISITION]` (les 4 vrais points de transfert de propriété trouvés
  par grep : `RulesEngine.TryBuyProperty`=Landing, `AuctionManager.EndAuction`=Auction —
  **jamais atteint en 100% IA, réservé au flow humain via `ActionButtonsWidget`** —,
  `MarketplaceSystem.ExecuteTrade`=Marketplace-Trade et `SealTenderDeal`=Marketplace-Tender
  / -Tender-Swap), `[RENT-CALC]` (niveau, loyer de base, multiplicateur appliqué, monopole,
  loyer final).

**Pas vérifié** : aucun accès Unity dans cette session. Prochaine étape pour Arthur :
relancer un batch de 20-30 parties, exporter la console, et transmettre pour l'étape 3 de
la revue externe (analyse forensique des 5 kills les plus chers : comment le tueur a
acquis la propriété, comment il a financé l'upgrade, à quel tour).

## 2026-09-26 — fiabilisation moteur et banc de test (autorisé par Arthur)

- [x] Paiement obligatoire au GO et liquidation sans dépendance UI pour les IA.
- [x] Arrêt centralisé des victoires et protection zéro survivant.
- [x] Valeur nette partagée conforme au GDD.
- [x] État neuf entre parties, contrôleurs et abonnements préservés correctement.
- [x] CSV 2–6 joueurs, compteurs explicites, graine et restauration du mode test.
- [x] Compilation Unity, régressions ciblées, puis smoke test 5 parties et batch 30 si propre.

Validation terminée le 1er octobre dans Unity 2022.3.62f3 : 27 assertions ciblées,
30/30 parties à quatre joueurs et 5/5 parties à six joueurs, sans anomalie ni erreur
capturée et avec invariants CSV validés. Le run de diagnostic à quatre joueurs a détecté
puis permis de corriger les revenus post-faillite liés au sabotage. Voir
`Docs/Validation_economie.md` pour les résultats, la portée et les dettes hors périmètre
encore ouvertes. Ces validations ne constituent pas une recette visuelle ou réseau.

## 2026-10-02 — familles, OPA et sanctions

- [x] Énergie Tier 3 corrigée à `100/3` et testée sur trois passages GO.
- [x] Huit passifs et huit ultimes couverts par une régression Unity ciblée.
- [x] OPA hostile alignée sur le GDD, avec transfert de propriété, paiement au vendeur,
  conservation des upgrades et absence de création monétaire.
- [x] Embargo aligné à `5 × Sgo`; Audit, Strike, Embargo et Sabotage testés avec leurs
  immunités et leurs refus sans fonds.
- [x] Durée du Cyber-Sabotage Armement unifiée sur trois tours du propriétaire.
- [x] Validation finale : 54/54 assertions mécaniques, 27/27 assertions économiques,
  30/30 parties à quatre joueurs et 5/5 à six joueurs, sans anomalie.
- [x] Ajouter l'étape de choix après le jet pour Assistance de Précision, côté humain et IA.
- [x] Ajouter les pickers humains : ultime disponible, destination Aerospace et cible Armement.
- [x] Étendre le batch six joueurs à 30 graines (14 octobre 2026) : 30 parties, zéro anomalie, aucun `ErrorCount`, une seule victoire à 10 M$ ; résultats et CSV validé dans `Docs/Validation/Economy_SixPlayer_2026-10-14/`. Le gain final max observé est 13,54 M$ et le pic de cash 14,28 M$.

Résultats et pré-évaluation : `Docs/Validation_mecaniques_2026-10-02.md`.

## 2026-10-02 — UI Marketplace et préparation WebGL

- [x] Réduire dynamiquement le panneau Marketplace pendant le choix d'une propriété.
- [x] Restaurer le panneau avec le nom, le revenu et l'action de confirmation après le clic.
- [x] Installer le module Unity WebGL 2022.3.62f3 et produire un build de développement.
- [x] Corriger le crash URP au premier rendu : textures de dithering absentes des profils
  de qualité et reflection probe temps réel désactivée dans le navigateur.
- [x] Passer WebGL sur le profil Low et réserver 256 Mo de mémoire initiale.
- [x] Valider dans le navigateur : démarrage, HUD, plateau et parcours Marketplace complet.
- [x] Auditer les 16 écrans UI Toolkit et le panneau historique : tailles tactiles,
  focus, états vides, fermeture, confirmations destructives, listes et transitions.
- [x] Rendre le template WebGL responsive et valider dans le navigateur les formats
  téléphone paysage, téléphone portrait et desktop.
- [x] Recetter en WebGL portrait les écrans rares : Venture longue, faillite sans actif
  avec confirmation en deux temps, et victoire avec classement complet.
- [x] Produire et démarrer un build WebGL Release compressé de 12 Mo, puis fiabiliser les
  mises à jour navigateur avec des noms de fichiers fondés sur leur hash.
- [x] Vérifier le pipeline iOS sur cette machine : le module Unity est présent, mais
  l'export Release échoue à Burst AOT car le SDK `iphoneos` est introuvable (`xcrun`),
  `xcode-select` pointe vers les seuls Command Line Tools. L'export partiel temporaire a
  été supprimé lors du contrôle précédent. Nouvel essai le 16 oct. : Unity a bien créé
  `Unity-iPhone.xcodeproj` sous `/tmp/PolyRiskIOSExport_20261016`, puis Burst AOT a échoué
  pour la même absence du SDK `iphoneos`; sortie partielle non compilable, aucun `.ipa`.
  Détails : `Docs/Validation/iOS_Export_2026-10-16.md`.
- [ ] Installer Xcode complet, sélectionner son répertoire développeur, relancer l'export
  Unity iOS, compiler dans Xcode et installer le build sur un appareil.
- [ ] Installer Unity Android Build Support avec SDK, NDK et OpenJDK, puis produire et
  démarrer un export Gradle Release. Vérification 16 oct. : Unity Hub CLI voit Android,
  SDK/NDK et OpenJDK comme disponibles; simulation d’installation : 2,6 Go de téléchargement,
  ~7 Go sur disque (99 Go libres). Installation arrêtée avant téléchargement car Unity
  requiert l’acceptation explicite des licences des modules; autorisation demandée à Arthur.
- [ ] Terminer la recette sur iPhone/iPad et Android physiques : encoche, barre d'accueil,
  densité réelle, chauffe, mémoire, gestes tactiles et textes exceptionnellement longs.
- [x] Concevoir et implémenter le multijoueur en ligne de deux à six joueurs : lobby,
  reconnexion, autorité, réplication des commandes et mécaniques synchronisées. La recette
  technique est décrite ci-dessous ; la partie humaine sur appareils réels et les essais
  de latence/déconnexion restent des jalons ouverts, pas une conception à commencer.

Preuves et limites : `Docs/Validation_ui_webgl_2026-10-02.md` et
`Docs/Validation_device_readiness_2026-10-04.md`.

## 2026-10-04 — multijoueur, tranche 1 : accueil et lobby

- [x] Évaluer Unity MPS, Photon, Nakama et un protocole HTTP adapté au tour par tour.
- [x] Ajouter un service de lobby ASP.NET Core sans dépendance externe ni compte fournisseur.
- [x] Créer et rejoindre une partie par code privé de six caractères.
- [x] Gérer deux à six joueurs, prêt/pas prêt, lancement par l'hôte et transfert d'hôte.
- [x] Ajouter les identifiants de reconnexion et les snapshots versionnés.
- [x] Ajouter l'accueil et le lobby UI Toolkit auto-initialisés dans Unity.
- [x] Mettre le moteur local en attente derrière le choix du mode de jeu.
- [x] Valider le serveur par un test bout en bout à six joueurs.
- [x] Recompiler Unity et conserver 54/54 assertions mécaniques.
- [x] Produire le build WebGL, corriger le responsive portrait et recetter le parcours
  hôte + second joueur + prêt + lancement sans erreur navigateur.
- [x] Définir et tester le protocole commandes/snapshots de la partie, puis ajouter les
  opérations typées correspondantes au client Unity.
- [x] Initialiser deux à six sièges humains et publier le snapshot initial autoritaire.
- [x] Synchroniser le lancer et le déplacement du joueur actif dans le premier tour.
- [x] Synchroniser la décision de case, l'achat, le passage et la fin du premier tour
  minimal, puis confirmer un tour déclenché par le second siège.
- [x] Synchroniser les quatre décisions Audit et l'Assistance de Précision −1/0/+1 ;
  corriger et tester le blocage « rester en Audit » après l'arrivée.
- [x] Synchroniser l'énergie, les cartes persistantes et les huit Ultimes, y compris les
  cibles Aerospace et Armement ; recette deux clients encore à intégrer au test complet.
- [x] Ajouter un journal d'activité autoritaire et léger : tour, dés, arrivée, achat,
  famille complétée, passage, Audit, faillite et activation de pouvoir ; conserver les
  douze derniers événements dans le snapshot et afficher un ticker animé unique.
- [x] Valider le ticker en WebGL desktop et portrait sans masquer le plateau ni le panneau
  de case ; régression Unity portée à 59 assertions et build WebGL réussi.
- [x] Synchroniser la Marketplace : passage, quatre sanctions, appel d'offres, réponses
  simultanées jusqu'à cinq enchérisseurs, offres cash/propriétés, annulation et choix du
  vendeur ; répliquer l'état complet pour la reconnexion et publier le feedback partagé.
- [x] Recompiler Unity avec 62 assertions, le serveur .NET Release sans avertissement et
  produire le WebGL de développement actualisé (`WEBGL PLAYER BUILD PASS`).
- [x] Répliquer la carte Venture en attente et sa confirmation sur le siège actif ; corriger
  l’avancement prématuré du tour avant application de l’effet.
- [x] Synchroniser l’OPA hostile : tirage et prix autoritaires, transfert cash/propriété,
  conservation des upgrades et feedback partagé ; régression portée à 64 assertions.
- [x] Synchroniser le flux multi-écran de faillite : dette, créancier et jeton dans le
  snapshot ; commandes autoritaires pour vendre les upgrades, hypothéquer, vendre le titre
  ou abandonner ; écran limité au siège débiteur et reprise du tour seulement après paiement
  ou élimination. Régression portée à 70 assertions ; panneau recetté en WebGL portrait et
  paysage, confirmation en deux temps et fermeture sans erreur navigateur.
- [x] Synchroniser les investissements et cycles économiques : commande dédiée autoritaire,
  placements actifs et marché dans le snapshot, rendements calculés par l’hôte, feedback
  partagé et panneau de choix compact. Régression 76/76 ; WebGL recetté en 430×840 et
  1280×720 sans chevauchement ni erreur navigateur.
- [x] Ajouter la persistance serveur atomique, les jetons hachés, la validation JSON et
  l’allow-list de commandes, CORS restreint, rate limiting, journaux structurés et option
  d’obligation HTTPS. Build .NET sans avertissement ; smoke test six joueurs, test de
  sécurité et reprise après deux redémarrages réussis. Le certificat/TLS réel sera fourni
  par l’hébergement de production.
- [ ] Terminer la recette sous réseau dégradé : trois HTTP 503 pré-commit suivis d’un seul clic de reprise sont validés avec six interfaces Chrome/WebGL accélérées ; bandeau visible, zéro commande avant reprise, une séquence après reprise, file vide et convergence chez les six clients. La perte post-commit pendant un lancer est maintenant récupérée par l’hôte WebGL Release (version 2/séquence 1, un seul lancer et arrivée) avec un second siège prêt via l’API QA ; l’achat restauré après reconnexion reste prouvé à deux clients rendus. Idempotence/convergence post-commit à six restent prouvées côté API. Restent les délais longs, la reconnexion/déconnexion en cours de tour sur six rendus, un invité WebGL simultané, Safari/iPhone et réseau mobile. `Docs/Validation/NetworkResilience_2026-10-15.md` et ses captures.
- [x] V63 couche serveur : build .NET Release sans avertissement ; smoke six sièges, test de redémarrage existant à deux sièges et nouveau test à six sièges PASS. Les six sessions récupèrent le même snapshot après redémarrage ; une commande après reprise est consommée/publiée par le nouvel hôte après départ de l’hôte initial. C’est une preuve HTTP/persistance, pas la recette Unity de six navigateurs. Détails dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md` et `Server/PolyRisk.LobbyServer/six_player_reconnect_test.py`.
- [x] V68 résilience d’action : une commande invité réessaie jusqu’à 3 fois le même identifiant sur timeout/429/5xx ; reçu idempotent du serveur empêche l’action double et est persisté. Build serveur Release 0/0, smoke 6 sièges, persistance, transfert d’hôte, Unity Economy 114 + Mechanics 110 et build WebGL Release `BuildOptions.None` PASS. Build Release rejoué depuis l’interface Unity/WebGL après échec pré-commit simulé : état version 2, séquence 1, action visible une fois. Statut API 503 testé séparément ; détails dans `Docs/Validation/NetworkResilience_2026-10-15.md`.

Architecture et limites : `Docs/Multiplayer_foundation_2026-10-04.md`.

## À reprendre plus tard — validation du fun et équilibrage final

- [ ] Compléter **5 à 10 parties humaines** à 3–6 joueurs avant de modifier à nouveau l’économie ; une partie à quatre est terminée et son retour partiel consigné. Utiliser `Docs/Validation/Playtest_Amis_2026-10-14.md` pour relever durée, appareil/navigateur, issue, notes de plaisir/rythme/équité et résultats par siège.
- [ ] Mesurer la durée réelle, le temps d'attente entre deux décisions, la fréquence des
  choix intéressants et l'envie de rejouer.
- [ ] Relever les éliminations jugées injustes, particulièrement celles causées par les
  multiplicateurs de loyer niveau 4 (`×50`) et niveau 5 (`×200`).
- [ ] Vérifier en situation réelle que les pouvoirs de famille, OPA, sanctions et montants
  économiques sont compris sans explication extérieure.
- [x] Étendre le batch six joueurs : 30 graines supplémentaires. 29 gagnants terminent
  sous 10 M$, un franchit le seuil et finit à 13,54 M$ ; pic de cash observé 14,28 M$.
  Cette simulation IA mesure la mécanique actuelle, pas l’équilibrage perçu par des humains.
- [x] Rejouer 30 parties à six joueurs après les corrections de cycle, sur les graines
  20261016–20261045 : 30/30 sans anomalie, aucune victoire à 10 M$, 18 fins par
  élimination et 12 classements à la limite. Comparaison agrégée quatre/six joueurs
  et CSV conservés dans `Docs/Validation/Economy_PairedSeeds_2026-10-15/` ; cela ne remplace
  pas les 5–10 parties humaines à réaliser avant tout réglage économique.
- [x] Élargir la mesure à 100 graines communes (20261106–20261205) pour 4 et 6 joueurs :
  200/200 parties validées, zéro anomalie/erreur. Le seuil de 10 M$ n’est pas atteint
  à quatre joueurs et l’est une fois à six (11,19 M$, graine 20261197). Les 63 premières
  simulations six joueurs ont été reprises après la limite de sécurité du runner, puis
  réunies aux 37 restantes; contrôle CSV final PASS. Aucune règle économique changée.
  `Docs/Validation/Economy_Paired100_2026-10-16/`.
- [ ] Décider seulement après ces tests s'il faut adoucir les niveaux 4–5, accélérer la
  progression vers 10 M$ ou conserver les valeurs actuelles.

État mémorisé : le moteur économique est cohérent et ses régressions passent, mais
« économie parfaitement équilibrée » et « jeu confirmé fun » ne sont pas encore validés.

## Notifications d’activité — reprise du 8 octobre 2026

- [x] Ajouter des événements pour les loyers réglés, améliorations, taxes payées et résultats du casino.
- [x] Prioriser les événements importants dans le HUD, avec deux messages maximum sur écran large.
- [x] Raccorder les styles et la capacité mobile à la classe existante `ux-narrow`, et réduire la pile lors du redimensionnement.
- [x] Recetter le build final en 1280×720 et 430×840 : achat et passage côte à côte sur desktop ; passage conservé lors du tour suivant sur mobile ; aucune erreur navigateur.
- [x] Cas limites UI — noms du lobby : recontrôle du build WebGL courant avec viewport Playwright explicitement calibré à 1280×720 (DPR 1, canvas 1280×720), six sièges et cinq invités aux noms de 18 caractères. Les six rangées, états WAITING, boutons et marges sont visibles dans la capture pleine taille ; aucun nom tronqué. Recette consignée dans `Docs/Validation/Lobby_LongNames_2026-10-15.md`.
- [x] Cas limites UI — rotation du feed avec deux notifications actives : en 1280×720, deux cartes coexistent et une notification longue se répartit sur deux lignes sans ellipse. En passant à 430×840, le feed réduit sa capacité à une ligne comme prévu; l’événement prioritaire reste lisible et le panneau d’action demeure dégagé. Console sans erreur. Recette `Docs/Validation/Activity_Rotation_2026-10-15.md`.
- [x] Vérifier la visibilité effective des achats sur deux clients WebGL indépendants ; synchronisation des trois achats et reprise de l'invité après rechargement consignées dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.
- [x] Vérifier la visibilité des achats et d’une décision de case sur six interfaces WebGL indépendantes ; recette navigateur synchronisée consignée dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.
- [ ] Vérifier la visibilité des achats/passages avec six personnes sur leurs appareils, notamment Safari/iPhone ; le contrôle WebGL à six interfaces était piloté depuis le même poste.

Les builds précédents ont réussi et la régression mécanique précédente compte 76 assertions.
Cela ne prouve pas la lisibilité de la dernière disposition : le premier essai a montré
une seconde ligne masquée par le panneau, et le seuil mobile utilisait une classe absente.
Le binaire WebGL antérieur au dernier raccordement responsive ne constitue pas sa validation.

Validation du build final des notifications : `Build Finished, Result: Success` et
`WEBGL PLAYER BUILD PASS` dans `/tmp/polyrisk_activity_priority_webgl.log`.
Captures et périmètre exact : `Docs/Validation/Activity_2026-10-08/README.md`.

## Reconnexion de l’hôte — état de restauration

- [x] Répliquer le compteur de doubles consécutifs et les tours commencés par joueur.
- [x] Restaurer les doubles dans TurnManager/DiceSystem : régression Unity 80/80, dont relance après double et fin de tour après jet normal (`/tmp/polyrisk_reconnect_counters.log`).
- [x] Implémenter le démarrage sans boucle, la lecture serveur et la reprise de version/queue/historique ; compilation Unity validée.
- [ ] Recetter ce démarrage par HTTP avec réponse de publication perdue, état serveur invalide et reconnexion réelle de deux/six clients.
- [x] Extraire les fins de règlement loyer/GO ; conserver dans le snapshot le type de dette, la case, le détourneur et le solde du créancier avant règlement. Reconstruction de la continuation loyer à l’application autoritaire (recette réseau encore à faire).
- [x] Brancher les continuations loyer et DÉPART après application autoritaire, partager la fin du mouvement, bloquer la réexécution des cases pendant une dette. Validation C# partielle ; recette réseau à faire.
- [ ] Valider ces continuations avec un hôte réellement reconnecté, y compris paiement, faillite, victoire et Transit Express.
- [x] Brancher AdjustDice restauré sur l’attente de choix partagée, puis le déplacement avec le total sauvegardé ; RollDice avec total reprend sans relancer.
- [x] Persister départ/destination/phase GO et brancher MovingPawn hors dette sur la continuation partagée.
- [ ] Recetter le mouvement restauré avec deux/six clients, passage GO, insolvabilité, Transit Express et animation.
- [ ] Recetter Ajustement -1/0/+1 après reconnexion réelle, expiration et état immédiatement après confirmation.
- [x] Persister le résultat de partie et restaurer le gagnant/raison sans redémarrer les tours ; garde des commandes après victoire.
- [ ] Recetter l’écran de victoire et le rejet d’actions après reconnexion avec deux/six clients.
- [ ] Recetter la reconnexion de l’hôte sur partie neuve, décision de case, Venture, Marketplace et liquidation avec deux puis six clients.

- [x] Transit Express : aligner le moteur sur la règle GDD optionnelle. À la possession des quatre Hubs, l'humain choisit de voyager ou de rester ; commande autoritaire `transit.choose`, choix IA conservé, expiration à 60 s en faveur de rester. Régressions Unity (117 mécaniques), smoke serveur, build WebGL Release et client navigateur vérifiés sur les deux choix PASS ; rechargement de l'hôte pendant chaque attente a restauré les deux boutons. Après « Use », état serveur : position 5, séquence 1, solde inchangé à 18 000 $ ; après « Stay », position 5, séquence 2, solde toujours 18 000 $. Un lancement initial a expiré après génération; un build Release suivant sur les mêmes sources a terminé proprement. Détails dans `Docs/Validation/Transit_Choice_2026-10-15.md`.

Le WebGL actuellement servi à l’utilisateur conserve la version des notifications validée.
Le changement des compteurs a compilé et passé les tests Unity ; il sera intégré au prochain
build réseau après la mise en place du démarrage restauré. Les champs sont additifs dans
le snapshot schema 1 : les anciens snapshots sans ces données fournissent zéro, et leur
série de doubles historique ne peut pas être retrouvée.


## Retour humain mobile reçu le 12 octobre 2026

- [x] Premier essai WebGL sur iPhone 14 Pro Max : démarrage, boutons, panneaux et widget Marketplace jugés réactifs par l’utilisateur.
- [x] Premier test en ligne avec quatre amis effectué ; panneaux et boutons jugés réactifs, aucun défaut précis rapporté.
- [ ] Recette approfondie sur iPhone/Safari et partie humaine à six appareils ; relever la durée, les mécaniques rencontrées, la cadence et les corrections finales.


## État Venture après reconnexion — 12 octobre 2026

- [x] Préparer la capture/restauration de pioche et défausse avec validation des vingt cartes.
- [ ] Exécuter les régressions de ce correctif dans Unity après le build actif.
- [ ] Intégrer le correctif au build suivant et recetter les tirages après reconnexion réelle.


## Recette navigateur du 12 octobre 2026 — portée précise

- [x] Deux clients réels : création/code/ready/lancement et reconnexion de l’hôte en Planning avec snapshot identique.
- [x] Corriger la non-publication des mouvements/effets autonomes ; V2 confirme une arrivée sans commande publiée.
- [x] V2 : réponse de publication perdue après achat, débit de 1100 une seule fois, propriétaire unique, séquence acquittée 1.
- [x] Venture/présence des états, génération de mutation et publication sérialisée : Economy 69, Mechanics 81 assertions.
- [x] Régression du correctif de sélection : Economy 71 assertions ; ordre nouveau tour puis sélection, sans reset durant le même tour.
- [ ] Vérifier ce correctif dans le build V3, puis poursuivre les commandes du second joueur et la recette à six.
- [x] V9 : vérifier la liste de six joueurs en paysage 900×430, actions visibles et Quitter fonctionnel ; publier sur le lien mobile existant.
- [x] Moteur Turbo — la régression vérifie deux jets candidats sans mutation, puis un seul événement/compte de doubles pour le résultat retenu (y compris le cinquième double) ; le joueur humain choisit explicitement son jet, et le parcours WebGL en ligne a transmis le choix puis déplacé le pion une seule fois. `Assets/Scripts/Editor/MechanicsRegression.cs`, `Docs/Validation/Online_Ultimate_Automobile_2026-10-15.md`.
- [ ] Continuer la recette complète à deux puis six clients, mobile, tests humains et corrections de publication.

- [x] V8 : six clients WebGL, six lancers manuels, achats/loyer, rechargement des six et achat après reprise validés (voir preuves Reconnect_2026-10-12).
- [ ] Confirmer V9 sur Safari/iPhone réel et organiser la recette approfondie entre amis.

- [x] Moteur Turbo : séparer candidats sans effets et validation du résultat retenu ; Mechanics Unity 85 assertions PASS.
- [x] Moteur Turbo — choix humain, boutons, état/snapshot, commande réseau et reprise du panneau de choix après rechargement de l’invité vérifiés ; la régression confirme qu’un seul résultat produit l’effet de dés. Le parcours en ligne complet est consigné dans `Docs/Validation/Online_Ultimate_Automobile_2026-10-15.md`, la reprise du panneau dans `Docs/Validation/Online_Ultimate_Automobile_2026-10-16.md`, et les assertions dans `Assets/Scripts/Editor/MechanicsRegression.cs`.

- [x] Choix humain Turbo intégré au moteur, boutons, snapshot et commande ; Mechanics Unity 91 assertions PASS, serveur compile.
- [x] V10 : recette visuelle des deux choix Turbo, précision ensuite, pénalité et reconnexion réelle ; déployer API compatible après validation. — confirmé par les preuves V10/V11 ci-dessous.

- [x] V10 navigateur autorité : choix Turbo restauré depuis snapshot, jet inférieur choisi sans fausse amende, +1 ensuite et arrivée case6 ; v4 seq2.
- [x] Turbo : cinquième double retenu, invité navigateur, dimensions mobiles et publication API/build compatible. — confirmé par les preuves V10/V11 ci-dessous.

- [x] V10 navigateur : cinquième double choisi = amende500 une fois, aucun déplacement, tour suivant ; rechargement conserve acquittement seq3.

- [x] Turbo invité deux navigateurs : deux jets restaurés, choix natif transmis à hôte v3 seq1 AdjustDice total5 doubles0.
- [x] Précision invité après recharge : premier essai arrivé case5 au lieu6 (délai possible) ; retester et vérifier boutons face au timeout avant publication. — confirmé par les preuves V10/V11 ci-dessous.

- [x] Précision invité après reprise : essai chronométré réussi, v8 seq3 position6 (contrôle à53,4s).
- [x] UI Turbo portrait : titre/contextualisation de choix, bouton plein écran empiétant sur JET2, overlays debug à retirer pour livraison. — confirmé par les preuves V10/V11 ci-dessous.

- [x] Préparer V11 : contexte Turbo/précision, statistiques masquées pendant choix, overlays debug éditeur uniquement, plein écran hors formats courts.
- [x] Confirmer build V11 et rendu portrait/paysage, puis publication API/build compatible. — confirmé par les preuves V10/V11 ci-dessous.

- [x] V11 : Economy106/Mechanics92/build PASS, rendu Turbo portrait/paysage corrigé et publication même lien ; API5091 compatible, smoke public6 PASS.
- [ ] Poursuivre la recette en ligne des offres, OPA et ultimes : les effets principaux des huit pouvoirs ont des preuves ciblées, mais les retours visuels invités et la reprise après reconnexion ne sont pas uniformément prouvés. Rejeu frais Unity batch des refus `ApplyCommand` : 128/128 PASS (`/tmp/PolyRiskPowerMechanicsQA.log`). Contrôles Release WebGL : Armement est refusé sur la propriété Aerospace sans mutation, et le sabotage Armement appliqué survit au rechargement complet de l’hôte avec sa séquence/durée et sans seconde dépense (`Docs/Validation/Online_Ultimate_Refusals_2026-10-15.md`, `Docs/Validation/Online_Ultimate_Armement_2026-10-15.md`). Le refus Armement/Aerospace est maintenant vérifié avec deux clients WebGL rendus ; le test de reprise Armement valide le rechargement de l’hôte. Le rendu de la cible Armement est maintenant confirmé côté invité (case cerclée violet + badge de durée, `Docs/Validation/Armement_Target_Visual_2026-10-16.md`). Après reconnexion des deux clients, l’inspecteur invité montre le badge Oil +80 % / 1 tour, Rent 778 $ et ROI 43,2 % sur Greenwashing Solutions (serveur v2/s0, `Docs/Validation/Oil_Boost_Inspector_2026-10-16.md`).. Restent les retours visuels et la reprise des autres pouvoirs. Après la partie à quatre, demander aux testeurs ce qu’ils ont compris/rencontré pour guider l’équilibrage (`Docs/Validation/Online_Ultimate_Armement_2026-10-15.md`, `Docs/Validation/Online_Ultimate_Pharma_2026-10-15.md`, `Docs/Validation/Online_Ultimate_Tech_2026-10-15.md`).

- [x] OPA V11 deux navigateurs : carte restaurée invité, confirmation réseau, paiement1550 au vendeur, propriété/niveau/upgrades transférés sans création de cash.
- [x] OPA en réseau : après reprise, soldes et séquence inchangés sans second transfert ; cible inabordable sans débit ni changement de propriétaire ; offre mixte acceptée et conservée après reconnexion (V11/V12, preuves dans `Reconnect_2026-10-12/README.md`).

- [x] OPA après recharge invité : soldes et séquence4 inchangés, carte non répétée.

- [x] OPA inabordable réseau : aucun débit/transfert, carte acquittée seq5.
- [x] Identifier et corriger source de superposition après reprise marketplace Reviewing/Spectator : masquer panneau case.
- [x] Rebuild/recette du correctif et accepter offre mixte cash2500 + tile3 contre tile1 ; deal et reconnexion vérifiés sur deux navigateurs (V12, preuves dans `Reconnect_2026-10-12/README.md`).

- [x] V12 : offre mixte cash2500 + tile3 acceptée contre tile1, cash conservé/upgrades conservés ; reload deux clients sans second transfert, actions suivantes visibles.
- [x] Terminer rendu mobile de revue marketplace et publierV12 ; preuves portrait/paysage ci-dessous.

- [x] V12 revue marketplace : portrait/paysage contrôlés, vente mixte et reprise validées, publiée sur même lien HTTPS.

- [x] V12 parcours natif mise en vente : panneau dynamique, titre8 sélectionné et commande reçue ; invitation expire après5s.
- [ ] V13 : vérifier délai online30s, pseudo et bouton acceptation désactivé avant sélection ; reprendre soumission native jusqu’au deal.

- [x] Mode Aggressive Growth du GDD implémenté : horloge UTC au démarrage, seuil strict >90 min, +$5 000/GO avec multiplicateur de cycle, bouclier30×Sgo, timestamp conservé dans les snapshots en ligne/reprise d’autorité. Economy114 + Mechanics99 et build WebGL Release PASS ; détail `Docs/Validation/Unity_Aggressive_Growth_2026-10-14.md`. La victoire reste régie par les conditions déjà spécifiées (10 M$, dernier survivant ou 50 tours).

- [x] V13 build/test198 PASS et invitation avec pseudo/délai30 observée.
- [x] Identifier et corriger source de panneau case devant invitation : masquer à ShowAuctionNotification.
- [ ] V14 recette Participate/formulaire/soumission/deal ; V13 non publié.

- [ ] Marketplace vendeur après reprise InvitingPlayers : vérifier ShowWaitingForBids (pas de masquage explicite du panneau case) ; fixture market_waiting_fixture.json préparée.

- [x] Confirmer défaut reprise vendeur attente : panneau case devant offres ; correction source centralisée SwitchView préparée pourV15.
- [ ] Copier correctif au clone après buildV14, compilerV15 et vérifier attente vendeur/invitation/vente.

- [x] V14 invitation accessible et Participate ouvre formulaire ; superposition corrigée.
- [ ] Formulaire offre : actions hors écran1280×720 ; correction source ScrollView/footer séparé préparée pourV16, à compiler/recetter.

- [x] V14 soumission cash native mobile : offre2908 reçue et acceptée sur deux clients, soldes15092/20908 et titre8 transféré.
- [ ] V16 : vérifier formulaire desktop/mobile corrigé, offre avec propriétés proposée manuellement et attente vendeur restaurée.

- [x] V16 : actions formulaire desktop visibles, scroll fonctionnel et attente vendeur restaurée sans superposition.
- [x] Identifier/corriger source de sélection des titres du vendeur dans offre acheteur (3 chemins) ; ajouter3 régressions de propriété.
- [ ] V17 : vérifier nouvelles régressions, sélection titre acheteur, offre mixte manuelle et styles de défilement avant publication.

- [x] V17 Economy106/Mechanics95 : trois tests de propriété acheteur/vendeur PASS.
- [ ] V18 préparée : redéployer brouillon après chaque titre choisi, consigne de sélection, conserver brouillon sur réponse autre acheteur ; nouvelle régression à exécuter.

- [x] V17/V18 : offre mixte soumise par UI acheteur avec title3, puis acceptée par vendeur ; cash et deux transferts vérifiés.
- [x] V18 retour au brouillon après choix du titre : cash conservé, titre et total affichés, tests202/build PASS.
- [ ] V18 : terminer contrôle mobile/paysage (tab32), reprise post-deal et publication même lien.

- [x] V18 offre mixte native : cash2721 + title3 reçus/acceptés ; soldes15279/20721 et échange des titres corrects.
- [x] V18 formulaire mobile entier visible ; sélection réduit le panneau et affiche la consigne.
- [x] V19/V20 : cadrage plateau pendant sélection mobile ; plateau complet visible en 430×900, propriété ciblée sélectionnée et panneau réouvert avec le titre. Preuves `Docs/Validation/Reconnect_2026-10-12/visual_board_fit_v20.png` et `visual_board_selection_v20.png`.

- [x] V19 Economy106/Mechanics99 : 205 assertions PASS, dont cadrage portrait/landscape/vertical.
- [x] V20 : build Unity terminé avec Economy106 + Mechanics99 assertions PASS ; préversion publiée sur le lien HTTPS existant avec index V20 vérifié.

## Chantier UI/UX visuel et motion — avant fin du jeu (13 octobre 2026)

Périmètre strict : présentation et interactions uniquement ; aucune règle, donnée économique ou mécanique à modifier. Audit initial : `Docs/UI_Motion_Audit_2026-10-13.md`. La comparaison fidèle au mockup attend les deux captures actuelles et le mockup cible, dont les chemins/images ne sont pas encore accessibles dans cette session.

- [x] Identifier la cause principale du flou WebGL mobile : template forçait `devicePixelRatio = 1` ; V20 adapte la densité avec plafond 2 et budget 2,4 millions de pixels. Build et calcul vérifiés.
- [ ] Mesurer la netteté, la cadence et la chauffe sur iPhone 14 Pro Max réel avec V20 ; ajuster le plafond si nécessaire. Nouvelle vérification intermédiaire sur le Release courant : Chrome 155 accéléré en émulation iPhone, 430×932/DPR3 et 932×430/DPR3, canvas 860×1864 puis 1864×860, accueil sans erreur JS ; captures et limites dans `Docs/Validation/Mobile_WebGL_2026-10-15/README.md`. Ce contrôle ne remplace pas Safari/appareil physique.
- [x] V44 + compactage ROI paysage (16 oct.) : overlay toujours fondé sur `TileIconResolver`, Rent/Price et ancres projetées ; dimensions/icône/texte réduits seulement quand le viewport est paysage compact (≥1,35). Aucun mesh, matériau, Canvas ou changement de données/règles. Unity 2022.3.62f3 : 114 économie + 140 mécaniques + 246 UI, Release WebGL `BuildOptions.None` PASS ; rendu vérifié dans Chromium à 1280×720, 932×430 et portrait 430×932. Voir `Docs/Validation/BoardBadges_2026-10-16/README.md`. Lisibilité physique, profilage iPhone/Safari et chauffe restent à mesurer.
- [x] Plateau : icônes sectorielles, rendement de base et couleur de famille issue des données affichés sur les propriétés ; les cases spéciales gardent leur glyphe dédié. La recette du cadrage complet en sélection Marketplace et l’observation des zones visibles restent distinctes.
- [x] Plateau V21 : couleur des propriétés liée à leur vraie famille (17/26 écarts de l'ancien motif corrigés) ; lumière au repos abaissée, sélection Marketplace toujours visible et cliquable en portrait. Build 205 assertions PASS ; capture locale conservée.
- [x] Plateau — intensité au repos réduite en V21, cases admissibles seules en pulse et autres accents atténués pendant la sélection Marketplace en V43 ; aucune lueur forte permanente visible sur le build V44.
- [x] V45 audit de profondeur : le prefab du pion et les meshes de plateau autorisent déjà le shadow casting ; aucune géométrie ni draw call supplémentaire nécessaire sur les profils qui rendent les ombres.
- [ ] Ombres visuellement garanties sur tous les profils : Low/Very Low désactivent les ombres URP ; comparer les profils et mesurer l’impact mobile avant de changer le preset ou d’ajouter une ombre factice.
- [x] Garde de layout de la confirmation de sanction Marketplace : la suite Editor vérifie que la liste de cibles peut se contracter, que la barre de confirmation reste verticale/non-rétractable et que le bouton prend toute la largeur avec 48 px minimum. `RunAllEditorRegressions` : économie 114/114, mécaniques 140/140, UI 203/203 (**457 assertions**). Le contrôle source complète la recette navigateur : le clic, le débit et la synchronisation restent à confirmer sur un onglet graphique. `Docs/Validation/ReleaseAudit_2026-10-16.md`.
- [ ] HUD : ligne CAPITAL/NET WORTH/TURN/MARKET, Inter, couleurs sémantiques et séparateurs discrets revérifiés dans le Release courant à 430×900 et 932×430, sans erreur navigateur. Il reste à comparer précisément la grille 8 pt et l’échelle typographique au mockup cible, puis à recetter Safari/iPhone physique. Captures `Docs/Validation/UI_Motion_2026-10-13/hud_final_release_430x900_2026-10-15.png` et `hud_final_release_932x430_2026-10-15.png`.
- [x] Typographie V35 : Inter existant appliqué au menu et aux libellés/valeurs du HUD ; titre droit, gras et lisible, quatre indicateurs HUD sans débordement à 430 × 900. Build WebGL et 205 assertions PASS, +132 Kio sur V31, HTTPS chargé en navigateur. La comparaison au mockup et la typographie des autres panneaux restent ouvertes.
- [x] Overlays debug release : le build WebGL public utilise `BuildOptions.None` ; F1/F2, F5–F8 et F9 n'affichent leur OnGUI que dans l'éditeur, et les raccourcis de showcase sont compilés seulement pour Editor/Development. V24 publique ne montrait aucun overlay.
- [x] Raccourcis de debug Espace/M limités aux builds Editor/Development/Debug après détection d’un conflit avec la glissière d’achat ; Economy 106 + Mechanics 99 assertions, build WebGL Unity `BuildOptions.None` PASS et préversion temporaire distincte publiée pour test mobile.
- [x] Smoke responsive du build isolé après correctif : menu en 430×900 et 900×430, actions principales visibles, options basses accessibles par scroll en paysage, aucune erreur navigateur. L’iPhone physique reste à mesurer.
- [x] Accueil V31 : ScrollView vertical et barre sombre, sans superposition en 1280 × 720 et 900 × 430 ; ouverture de « Connection » et champ serveur atteints par défilement. La barre horizontale parasite a été supprimée en 430 × 900. Build WebGL + 205 assertions PASS ; nouveau lien HTTPS chargé en navigateur.
- [x] Champs de saisie WebGL : défocalisation explicite du nom/code/serveur en quittant le formulaire ou en entrant en partie. Build Release, 114 tests économie + 99 mécaniques PASS ; test local à deux navigateurs, Ready/démarrage/reconnexion PASS. Le build public des amis reste V67.
- [x] Menu « Quitter la partie » : bouton dans le HUD, reprise/confirmation avec texte distinct local vs en ligne ; l’abandon en ligne termine la session partagée et renvoie chaque client à l’accueil. Vérifié sur build WebGL Release local : parcours local complet, puis partie à deux navigateurs (lobby AF4U7Q, Ready, démarrage, abandon hôte et retour des deux clients). Serveur API isolé ; le preview public V67 n’a pas été modifié.
- [x] Modal Quit responsive (15 oct.) : sur le WebGL Release final à 332×763, correction du titre tronqué ; Quit et Cancel sont visibles, Cancel revient au menu et la sortie locale revient à l’accueil. USS ciblé sous 520 px, sans coût runtime. Build Unity 2022.3.62f3, Economy 114/114 + Mechanics 128/128, Release PASS (~12 MiB). Safari physique reste à vérifier. `Docs/Validation/QuitModal_Narrow_2026-10-15.md`.
- [x] Préversion entre amis du 15 oct. rétablie après extinction de l’ancien tunnel : nouveau build Release avec le menu de sortie servi en HTTPS, création de lobby depuis le client et smoke protocole distant 6/6 PASS ; build serveur, persistance après redémarrage et reprise/transfert d’hôte à six PASS. Fiche de test mise à jour. Cette preuve ne remplace pas la partie humaine à quatre/six ni l’essai Safari/iPhone.
- [ ] Confirmer sur iPhone/Safari physique que le clavier virtuel et sa barre de saisie disparaissent après création/rejointure et démarrage d’une partie.
- [x] Marketplace mobile : le panneau se réduit à l’entrée en sélection sur le plateau puis se rouvre après le choix, avec la propriété et son revenu visibles ; comportement confirmé dans `Reconnect_2026-10-12/six_marketplace_pick_v3.png` et `six_marketplace_selected_v3.png`.
- [x] Inspecteur V48 : badge famille/niveau; trois cellules Rent / Risk / ROI. Rent et ROI utilisent les données déjà présentes; `Risk` affiche `N/A` parce qu’aucun score par propriété n’est modélisé. Les flèches comparent le loyer courant au loyer de base (hausse, baisse ou égalité), sans prétendre représenter une tendance historique. Economy 106 + Mechanics 99 assertions et build WebGL `BuildOptions.None` PASS ; le client local montre bien les trois métriques sur une propriété, et le preview HTTPS charge le menu.
- [x] V49 correctif de superposition : la carte inspecteur est opaque et son UIDocument passe devant le HUD afin que les badges rendement du plateau ne traversent plus le texte de la fiche. Vérifié sur le build WebGL local en 1280 × 720, 430 × 900 et 900 × 430 ; Economy 106 + Mechanics 99 assertions et build Release `BuildOptions.None` PASS. Les vues Marketplace ont ensuite été contrôlées en runtime sous V51 ; le profil iPhone reste ouvert.
- [x] V50 inspecteur paysage compact : fiche limitée à 60 % de la hauteur et icône réduite à 48 px sur viewport ≤430 px ; à 900 × 430, les trois métriques et la glissière/pass restent visibles avec 82 px de plateau dégagé. En 430 × 900, plateau complet et actions restent lisibles. Economy 106 + Mechanics 99, build Release PASS et zéro erreur JavaScript. Préversion locale isolée, non publiée ; appareil physique reste à tester.
- [x] V70 fiche inspecteur en paysage large : à 1280×800, hauteur plafonnée à 50 %, bord haut décalé d’environ 72 px vers le bas ; famille, Rent/Risk/ROI et Buy/Pass restent visibles. Détection sans polling, aucun Canvas/mesh/dépendance supplémentaire. Unity 2022.3.62f3 : Economy 114, Mechanics 110, Release `BuildOptions.None` PASS (12 MiB), navigateur sans erreur JavaScript. Avant/après : `Docs/Validation/Inspector_Landscape_2026-10-15/` ; mesures physiques et mockup restent à valider.
- [x] V78 inspecteur en paysage court : le contrôle frais 932×430 avait révélé un chevauchement malgré l’ancienne validation 900×430. La fiche passe à 46 % maximum avec espacement compact. Nouveau build Unity Release, Economy 114 + Mechanics 128 PASS ; navigateur a démarré, affiché Rent/Risk/ROI et slider/Pass sans couvrir le plateau, puis validé l’achat à 900 $ une seule fois. Smoke réseau sur deux clients distincts : lobby/Ready/Start, Venture confirmée et tour invité synchronisés ; $17,4 k$ affichés des deux côtés après taxe. Détail : `Docs/Validation/Inspector_ShortLandscape_2026-10-15.md`. Safari/iPhone physique et recette humaine à six restent ouverts.
- [x] Inspecteur compact et cadre Planning (15 oct.) : la fiche vide est masquée pendant Planning, les actions persistent, et le plateau se recadre dans la zone libre HUD/dock à 932×430 ; au premier lancer, le cadre normal revient et la fiche de propriété apparaît. Rebuild Unity actuel : Economy 114, Mechanics 130 et Release `BuildOptions.None` PASS ; Chrome/Metal zéro erreur JS. Détails et captures dans `Docs/Validation/Inspector_Compact_2026-10-15.md`, `Inspector_Landscape_2026-10-15/` et `Unity_FinalCheckpoint_2026-10-15.md`. Restent lisibilité/mockup et appareil réel.
- [x] Badges rendement adaptatifs : libellé ROI auparavant fixe à 7 px, maintenant ajusté entre 8 et 11 px selon l’espace de la case ; padding réduit pour garder l’icône et le taux dans les rangées compactes. Contrôlé sur WebGL Release frais à 667×375 et 430×900. Cadrage portrait calculé sur les 40 cases : aucun repère latéral coupé à 430×900 ; l’inspecteur et ses métriques s’affichent après lancer. En 932×430, Chrome confirme que la glissière d’achat reste tactile jusqu’à la validation malgré son placement bas ; marge safe-area à confirmer en Safari réel, aucun correctif visuel spéculatif posé. Aucune erreur JavaScript. Release 12 MiB, Economy 114/114, Mechanics 130/130 PASS. La recette physique et la fidélité exacte au mockup restent ouvertes. `Docs/Validation/Unity_FinalCheckpoint_2026-10-15.md`.
- [x] V51 recette runtime Marketplace : hub, repli du panneau avant sélection de titre, écran de confirmation et attente `No offers yet` vérifiés en portrait/paysage. Corrige le bouton Cancel Auction coupé en paysage et la ligne sélectionnée qui débordait horizontalement en portrait ; cibles tactiles conservées. L’annulation retourne au tour. QA via build Development séparé, puis Economy 106 + Mechanics 99 et build Release `BuildOptions.None` PASS ; menu Release chargé sans erreur JS et sans label Development. Non publié.
- [x] Inspecteur V37 : famille/niveau affichés seulement sur une vraie propriété ; action principale pleine et secondaire en contour, lisibles en portrait 430 × 900. Unity WebGL et 205 assertions PASS ; capture `Docs/Validation/UI_Motion_2026-10-13/inspector_property_v37.png`.
- [x] Recetter l’inspecteur V49 à 430×900 et 900×430 : Rent/Risk/ROI, `N/A`, flèches neutres et actions Buy/Pass restent lisibles ; le plateau est visible en portrait et le panneau compact en paysage. Le changement d’orientation a été contrôlé après stabilisation du layout ; console sans erreur ni avertissement.
- [ ] Vérifier les tendances hausse/baisse, comparer fidèlement au mockup et profiler sur iPhone réel ; la capture cible n’est pas accessible dans cette session.
- [x] Enchères : avatars par joueur, offres alignées et état vide stylé ; cinq offres accessibles jusqu’à six joueurs en 430×900 et 900×430. Seuil rouge ≤5 s confirmé en Release WebGL à 3 s le 15 oct. ; l’ancienne capture V38 reste mal étiquetée.
- [x] Enchère Marketplace V38–V40 : avatar rond coloré par siège, nom/offres alignés et état vide en carte ; cinq offres accessibles dans un salon de six joueurs en 430 × 900 et 900 × 430, actions visibles en paysage. 205 assertions et build WebGL PASS ; captures dans `Docs/Validation/UI_Motion_2026-10-13/`. La capture historique du seuil rouge est mal étiquetée et ne valide pas cette couleur. L’animation en direct est ajoutée en V46 ci-dessous.
- [x] Notification Marketplace : le premier essai (opacité 97 % + tri au-dessus du HUD) laissait encore passer les badges ROI. Correctif final : masquer l’overlay rendement pendant la modale Marketplace, restaurer à la fermeture/expiration. Vérifié en Release WebGL dans un lobby neuf : titre/vendeur clairs à 19 s, minuteur rouge à 3 s, badges restaurés après fermeture automatique. Economy 114 + Mechanics 130, Release PASS. Détails dans `Docs/UI_Motion_Audit_2026-10-13.md` et `Docs/Validation/UI_Motion_2026-10-13/README.md`.
- [x] Animation live V46 : le spectateur conserve sa bottom sheet montée pendant les mises; seules les nouvelles lignes glissent/fadent, avec effet désactivé si « Réduire les animations » est actif. 106 assertions économie + 99 mécaniques, build WebGL `BuildOptions.None` PASS et client lance le plateau de 40 cases sans erreur JavaScript.
- [x] V52–V53 : en WebGL à deux clients, le vendeur reçoit une offre réelle de 5,04 k$ dans sa Deal List et l’invitation tender est répliquée avec son compte à rebours de 30 s. Sources et limites dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.
- [x] V64 — Trois clients WebGL indépendants ont lancé une partie ; l’acheteur 0 a passé, l’acheteur 2 a soumis une offre réelle de 3 018 $ et le vendeur a reçu sa Deal List. Le serveur a enregistré `Reviewing`, le skip et l’offre dans le même état.
- [x] V65–V67 — Correction du GUID de `marketplace_BidderSpectator` dans `SampleScene.unity`, puis preuve en ligne WebGL Development à trois clients : le vendeur voit son offre de 8 427 $, les deux acheteurs affichent la fiche de revue, et le bidder conserve l’offre/status après rechargement. Snapshot public : `Docs/Validation/Reconnect_2026-10-12/market_review_v67_spectator_fix.json`. Unity 2022.3.62f3 : Economy 106/106, Mechanics 99/99, Release `BuildOptions.None` et Development PASS. Voir `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.
- [x] Revalidation Unity du source courant (14 oct.) : Economy 106/106 et Mechanics 99/99 PASS ; build WebGL Release `BuildOptions.None` PASS (`/tmp/PolyRiskWebGLRelease`, 12 432 KiB), ressources HTTP 200 et accueil chargé sans overlay debug. La mise en revue en direct avec une seconde offre et « Reduce Animations » reste à valider ; recette appareil réel ouverte.
- [x] Motion V67 Marketplace — rafraîchissement en direct observé dans le build WebGL local : avec un tender Bidding restauré, l’arrivée de l’offre de QA Bidder fait passer la revue à 2 offres, affiche l’activité et conserve le spectateur dans sa fiche. Le snapshot serveur confirme `Reviewing`, séquence 1 et offres 2 500 $ / 3 000 $. Les détails sont dans `Docs/Validation/UI_Motion_2026-10-13/README.md`.
- [ ] Motion V67 — audit source confirmé : entrée 180 ms et classe d’arrivée omise si « Réduire les animations » est actif (USS 0 ms également). Il reste la comparaison visuelle runtime activé/désactivé et la mesure de la transition ; profil iPhone Safari réel requis. Détails dans `Docs/Validation/UI_Motion_2026-10-13/README.md`.
- [x] Lobby V59–V62 : le build Release frais confirme l’affichage 3/6 et ses contrôles. Le parcours Join échoue si le banc local relaie vers un port différent du défaut WebGL 5080 ; après alignement des ports, un second client WebGL rejoint réellement et passe Ready. L’UI hôte affiche 6/6 après quatre sièges QA ajoutés par l’API ; en 1280×720, le scrollbar du roster permet d’atteindre et lire les sièges 4–6, sans chevaucher Ready/Start/Leave. Economy 106 + Mechanics 99 assertions et build Release WebGL PASS. Cela ne constitue pas une recette à six clients indépendants : Ready et démarrage à six restent à confirmer avec des joueurs connectés séparément. L’ancien « Unknown Error » venait du banc mal configuré.
- [x] Protocole six joueurs (15 oct.) : banc API isolé, six sessions distinctes, plafond vérifié (septième refusé), Ready des six, Start hôte, commandes/état autoritaires, reconnexion des six après redémarrage et transfert d’hôte passent. Preuves et limites dans `Docs/Validation/SixPlayer_2026-10-15.md`. Ne valide pas six clients Unity rendus simultanément ni la recette visuelle mobile.
- [x] WebGL 6 sièges en mode mixte (15 oct.) : lobby 6/6 et Ready visibles dans le client hôte ; Start depuis l’UI (HTTP 200), lancer du dé puis arrivée Venture #07, version 3/séquence appliquée 1. Deux invités WebGL rendus ont affiché la même tuile et WAIT ; trois autres sièges étaient pilotés par l’API. Détails et captures dans `Docs/Validation/SixPlayer_WebGL_2026-10-15/README.md`.
- [x] Pouvoirs de famille et OPA (15 oct.) : suite Unity fraîche 117 assertions ; 3e titre par achat/OPA/Marketplace/enchère banque/tender complète le monopole, énergie disponible au GO et notification de déblocage présente sur ces routes. OPA abordable/inabordable et transfert sans création monétaire déjà vérifiés en Unity et réseau, dont reprise sans second transfert. Aucun réglage économique changé ; preuves dans `Docs/Validation/Mechanics_Family_OPA_2026-10-15.md`.
- [x] Recette WebGL à six profils indépendants : les six sièges ont rejoint et passé Ready par leurs interfaces ; l’hôte a démarré et lancé les dés ; cinq invités WebGL simultanés ont rendu la même arrivée et WAIT. Snapshot autoritaire : tour 1, case 8, dés 3 + 5, séquence 1 ; six pages sans erreur JavaScript. Captures et limite appareil dans `Docs/Validation/SixPlayer_WebGL_2026-10-15/README.md`.
- [ ] Recette humaine à six joueurs sur appareils/navigateurs distincts et mesure de cadence/chauffe ; le test humain confirmé à ce jour reste celui à quatre amis.
- [x] Roster compact V59 (implémentation historique) : recette 900×430 à 6 sièges en grille 2×3 et rotation 430×900 PASS ; détails `Docs/Validation/CompactRoster_2026-10-15.md`. La recette locale du 16 oct. a révélé que le ScrollView ne rendait pas ses lignes à 992×720 ; son remplacement par le libellé dynamique est noté ci-dessous.
- [x] Roster du lobby Release vérifié en 6/6 aux viewports 992×720, 900×430 et 430×932 ; six noms/états et Ready/Start/Leave/Reduce Animations restent visibles. Les espaces insécables gardent chaque nom composé entier ; les salons en paysage sont compacts dès l’hôte seul. Release Unity PASS : Economy 114, Mechanics 140. Captures et limites : `Docs/Validation/LobbyRoster_2026-10-16.md`.
- [ ] Recette intermédiaire plus large, cibles tactiles, Safari et performances sur iPhone physique ; le rendu automatisé ne remplace pas cette validation appareil.
- [x] V58 : le symptôme Ready V55 n’est pas reproduit après configuration explicite du serveur pour le client local. Deux clients distincts ont rejoint le même lobby, le POST Ready de l’invité porte le bon ID, les deux snapshots/UI convergent à Ready, Start devient actif et les deux clients reçoivent le démarrage. Attention : clic puis Espace déclenche deux inversions ; laisser le snapshot revenir après un seul clic. Test sur build Development préexistant ; la compilation Unity fraîche reste bloquée par la licence CLI. Détails V58 dans `Docs/Validation/Mobile_Online_Preview_2026-10-12.md`.
- [x] Audit du parcours d’enchère classique terminé : il est distinct de la vente Marketplace et aucun écran ne s’abonne aux événements `AuctionManager`. En local, Pass déclenche `StartAuction` puis termine la résolution ; en ligne, `action.secondary` fait seulement `SkipOptionalAction` (le contrôleur le décrit comme une évolution multi-étape ultérieure). L’absence d’enchère fonctionnelle à cet endroit requiert une tranche gameplay/réseau distincte, hors de la passe UI sans changement de règles.
- [ ] Enchère classique après refus d’achat : relecture du GDD et de la source le 16 oct. confirme le gap. `GDD_Board.md` nomme seulement Marketplace comme « Enchères / Sanctions » ; `GDD_Economy.md` ne spécifie que l’enchère des propriétés après faillite, dans les trois tours. Aucun ordre de parole, règle de passe/clôture ou traitement d’une enchère sans offre n’est défini. La scène principale n’instancie pas `AuctionManager`; l’action locale retombe sur Passer et la commande en ligne saute l’action. Aucun écran n’écoute le gestionnaire dormant. Hors passe UI-only : ne pas inventer la règle; clarifier séparément avant une implémentation locale et autoritaire en ligne. `Docs/UI_Motion_Audit_2026-10-13.md`.
- [x] V43 : sélection Marketplace avec pulse des cases admissibles et accents des autres cases atténués/désaturés ; restauration de l'état visuel au retour. Réutilise le shader/texture d'état sans draw call ajouté. Build WebGL + 205 assertions PASS ; lien V43 servi en HTTPS. L'échelle individuelle des tuiles est limitée par le mesh fusionné.
- [ ] Mesurer l'effet de sélection sur iPhone 14 Pro Max réel ; évaluer si un indicateur d'échelle mérite une géométrie dédiée. Haptique seulement si disponible sur appareil réel.
- [x] Achat V42 : glissière avec résistance initiale, seuil de validation à 90 %, retour sans achat si relâchée trop tôt, clavier Entrée/Espace et bouton classique avec « Réduire les animations ». Achat local contrôlé en WebGL 430 × 900 ; 205 assertions PASS.
- [x] HUD V41 : CAPITAL et NET WORTH interpolés par un animateur commun limité à 20 mises à jour/s, cible finale exacte, nouvelles cibles prises en compte en cours d'animation ; affichage immédiat avec « Réduire les animations ». Achat local contrôlé en WebGL à 430 × 900, 205 assertions PASS, taille du build stable. La glissière d'achat, l'haptique et le profilage réel restent ouverts.
- [x] Accessibilité V24 : toggle persistant « Réduire les animations » sur accueil et lobby, état synchronisé, arc des pions désactivé sans changer le temps de déplacement. WebGL 205 assertions PASS, contrôle navigateur en portrait.
- [x] Réduire les animations : le réglage persistant coupe maintenant globalement toutes les transitions USS dans les documents UI marqués, en plus des gardes dédiées au slider d’achat, au compteur d’argent, aux offres et aux animations de pion. Le contrôle Unity garantit aussi le surlignage statique des cases sélectionnées sans coroutine de pulse. Vérification du 16 oct. : 450 assertions PASS (114 économie, 140 mécaniques, 196 UI).
- [x] Partie terminée avec quatre amis, panneaux et boutons jugés réactifs par l’utilisateur (15 oct.) ; aucun incident précis rapporté. Durée, modèles d’appareils et navigateurs restent à consigner.
- [ ] Recueillir la durée, appareils/navigateurs et incidents éventuels de cette partie afin de prioriser les fixes finaux ; aucun incident précis n’a été signalé jusqu’ici.
- [x] Contrôle WebGL interactif du réglage : activation sur l’accueil, état coché conservé après rechargement, puis démarrage d’une partie locale ; menu portrait 430 × 900 et HUD/commandes à 1048 × 720 visibles après chargement à froid.
- [ ] Comparer les effets avec le réglage activé/désactivé et recueillir le retour d’un utilisateur sensible au mouvement ; Safari/iPhone physique reste à vérifier.
- [ ] Profiler avant/après sur WebGL mobile et desktop, mesurer le temps de chargement et terminer la recette visuelle écran par écran face au mockup. Payload Release compressé mesuré à 12,09 MiB, soit +10 octets après le correctif de retour à la ligne; le profilage cadence/mémoire et la comparaison au mockup restent ouverts. Un commit logique par système.
- [x] V71 HUD réseau : après trois échecs d’envoi, l’erreur en ligne apparaît comme activité prioritaire orange dans le HUD ; le message demande de rejouer la même action. La commande cliente garde son `clientCommandId` et son payload jusqu’à l’acquittement/changement de version, assurant l’idempotence du rejeu. Styles inclus ; build WebGL Release frais PASS (Economy114, Mechanics110, 12 MiB).
- [x] V76 réseau : le retour persistant a été déplacé dans un bandeau dédié au-dessus du plateau. Build Unity WebGL Release PASS, Economy114 + Mechanics110, `git diff --check` PASS.
- [x] V76 rendu : après trois réponses 503 avant commit sur « Draw Card », le bandeau apparaît ; le retry réussit avec une seule séquence `action.primary` et une seule activité Venture. La capture V76 a révélé que le bandeau restait affiché après l’application de l’action.
- [x] V77 : effacement relié à la confirmation de l’état autoritaire ; Release Unity PASS (Economy114, Mechanics110, 12 MiB). Le même build a montré le bandeau après trois 503 et a appliqué un unique `turn.roll` au retry. Un second scénario a glissé « Slide to Buy » et constaté un unique achat de 1 300 $ après un 503.
- [x] V77 récupération en même session (15 oct.) : trois réponses 503 avant commit, séquence serveur inchangée, puis reprise depuis l’interface ; une seule commande `turn.roll` commise (séquence 1) et bandeau visuellement absent après l’acquittement. Proxy et lobby isolés, preview publique intacte. Preuves : `Docs/Validation/NetworkHud_2026-10-15/README.md` et `error_after_three_failures_v77.png`.

- [x] Roster lobby, correction ciblée (16 oct.) : la recette WebGL locale à deux clients a révélé un ScrollView vide en 992×720. Remplacé par un libellé compact dynamique ; les deux noms/rôles/états et toutes les actions restent dans la carte. READY invité synchronisé au client hôte. Build Development courant et `git diff --check` PASS. Détails/limites : `Docs/Validation/LobbyRoster_2026-10-16.md`.
- [x] Release optimisé après les correctifs roster : Unity 2022.3.62f3 PASS (Economy 114/114, Mechanics 140/140) ; roster six joueurs contrôlé en Chromium WebGL à trois viewports. Payload compressé total 12 681 684 octets avec loader. Voir `Docs/Validation/LobbyRoster_2026-10-16.md`.
- [ ] Vérifier les viewports intermédiaires et les interactions tactiles sur appareil réel; Safari/iPhone et profilage mobile restent ouverts.
- [x] Audit Release courant (16 oct.) : 114 économie + 140 mécaniques + 203 UI = 457 assertions Editor PASS ; build Release fraîchement générée et menu rendu dans Chrome graphique à 430×904 CSS/DPR 2. L’essai tactile Create a échoué sur l’URL de serveur local par défaut (5080 au lieu du serveur QA isolé 5094), et l’ouverture tactile du Foldout Connection n’a pas été confirmée. Aucun lobby neuf n’a été créé. Détails/capture : `Docs/Validation/ReleaseAudit_2026-10-16.md`, `Docs/Validation/Mobile_Release_2026-10-16/menu_430x904.png`. À reprendre avec un serveur configuré correctement avant la recette d’interaction de cette build.
- [x] Complément QA Release mobile : origine unique locale configurée et visible dans Connection ; accueil et plateau rendus en 430×932/DPR2, « Play Local » puis un lancer résolu ont vérifié le HUD, le board complet et les commandes tactiles sur la build courante. À la date de cet audit, création/jointure restaient à confirmer ; la recette UI complète à deux clients a depuis passé sur le Release courant (voir `Docs/Validation/LobbyUI_CurrentRelease_2026-10-16/README.md`). Capture : `Docs/Validation/Mobile_Release_2026-10-16/board_after_local_turn_430x932.png`.
- [x] Serveur multijoueur courant : smoke test isolé six joueurs PASS après arrêt du banc UI ; capacité 6, refus du 7e, Ready/Start, idempotence, publication d’état et validation d’entrées. Interface create/join de cette build reste à confirmer.
- [x] Revalidation serveur après build Release fraîche (16 oct.) : compilation zéro warning/erreur, smoke six joueurs/protocole PASS, persistence/restart PASS et reprise de six sessions + transfert d’hôte PASS. À la date de ce contrôle, aucune requête POST n’avait été observée dans le banc CDP ; un banc recalibré a ensuite validé Create/Join/Ready/Start et `turn.roll` à deux clients (voir `Docs/Validation/LobbyUI_CurrentRelease_2026-10-16/README.md`).
- [x] Release courante (16 oct.) : Create → Join → Ready → Start → lancer de dés via l'UI de deux profils WebGL indépendants. Lobby `THZW6R`, deux sièges Ready, début de partie et `turn.roll` séquence 1 répliqués ; hôte et invité affichent l'état correspondant. Un HTTP 400 initial était dû au nom par défaut dupliqué « Player », puis le Join a réussi avec `GuestQA`. Captures et portée : `Docs/Validation/LobbyUI_CurrentRelease_2026-10-16/README.md`. Preview publique intacte.
- [x] Six clients WebGL indépendants sur le Release courant (16 oct.) : six BrowserContexts distincts ont rejoint/Ready depuis l’UI, l’hôte a démarré; chacun a joué son tour via son propre client, commandes 1–12 appliquées, Guest6 a tiré et confirmé Venture #08, état final synchronisé dans les six captures et tour revenu à l’hôte. Même Chrome/Mac, pas six appareils. Captures, snapshot expurgé et limites : `Docs/Validation/SixIndependentUI_CurrentRelease_2026-10-16/README.md`. Le test humain six appareils, Safari/iPhone physique, performance et publication restent ouverts.

## Feedback global et motion design — audit du 16 octobre 2026

- [x] Cartographier les temps forts, les activités publiées, les écrans déjà animés et les lacunes visuelles sans toucher aux règles ni aux données : `Docs/Validation/Gameplay_Feedback_Audit_2026-10-16.md`.
- [ ] Prototyper/recetter la séquence additive de lancer de dés (anticipation courte → résultat autoritaire → déplacement existant → arrivée/inspecteur), y compris réponse réseau tardive et mouvement réduit.
- [ ] Définir et valider la grammaire de feedback action / attente / résultat / conséquence; prioriser achats, transactions, pouvoirs, Marketplace/OPA et insolvabilité sans empiler les notifications.
- [ ] Exécuter la recette runtime de tous les panneaux et états listés dans l’audit, en portrait/paysage et avec deux puis six clients lorsque le parcours dépend du réseau.
- [ ] Terminer la comparaison au mockup, profilage WebGL mobile, test Safari/iPhone physique et contrôle du réglage de mouvement réduit avant de déclarer le polish clos.

## Lisibilité des états de partie — audit et recherche GitHub (16 octobre 2026)

- [x] Vérifier le HUD/feed et les données d’activité pour voir comment l’interface communique qui agit, qui reçoit/paye et si l’action est confirmée; documenter l’écart et le concept « Dock de partie » : `Docs/Validation/Game_State_Dock_GitHub_2026-10-16.md`.
- [x] Évaluer des repos GitHub ciblés UI Toolkit, notification prioritaire, animation et design system; consigner les idées réutilisables et les limites Unity/WebGL. Aucun package ajouté.
- [x] Prototyper le Dock compact ancré aux commandes, son historique consultable et les variantes « à toi / tour adverse » ; parcours d’éditeur et recette WebGL Release locale : `Docs/Validation/GameStateDock_2026-10-16.md`. La validation portrait/paysage sur appareils reste ouverte.
- [x] Le partage de loyer est vérifié par régression Unity Editor, transport autoritaire vers trois clients Unity WebGL et rendu du reçu sur les écrans payeur/propriétaire : 27,50 $ / 82,50 $, une corrélation commune et bénéficiaires conservés. `Docs/Validation/SplitRentWebGL_2026-10-17/README.md`.
- [ ] Améliorer le reçu visuel du loyer réparti : afficher le total payé et toutes les parts/bénéficiaires avec un arrondi cohérent. Le Dock courant n’affiche que la part locale arrondie à 83 $ dans les captures observées, malgré les deux transferts exacts dans l’état réseau. Concevoir en preview avant toute intégration.
- [ ] Valider le flux tour → dés → arrivée → achat/passe et loyer, l’historique/priorités, les erreurs réseau, le mouvement réduit et les performances WebGL. À deux clients, le flux dés/taxe/arrivée/passe est déjà prouvé; à six sièges, la recette UI indépendante est documentée dans `Docs/Validation/SixIndependentUI_CurrentRelease_2026-10-16/README.md`. Nouveau contrôle mixte : dans un lobby 6/6, l’hôte WebGL Unity a consommé `turn.roll` puis `action.secondary` envoyés par le siège invité 2, publié les versions 5 et 6, et affiché l’arrivée du pion invité puis l’attente du siège 3 (`Docs/Validation/GoalContinuation_2026-10-17.md`). Les cinq invités étaient pilotés par API pour ce nouveau test. Restent le loyer détourné Unity/WebGL entre clients, les erreurs réseau et le mouvement réduit dans un parcours complet, la recette humaine à six, le profilage et Safari/iPhone physique; la recette n’est donc pas limitée à du local, mais ces critères ne sont pas clos.
