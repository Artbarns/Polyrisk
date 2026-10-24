# PolyRisk — PASS 2D · Close Strategic Board

20–21 octobre 2026 · Unity 2022.3.62f3 · URP 14.0.12 · IL2CPP WebGL Development.

Prototype limité au cadrage rapproché, aux dés intégrés et au suivi anticipatif. **Arrêt à PASS 2D : la validation artistique reste celle d'Arthur.**

## Preuves réelles

[Galerie vidéo](http://127.0.0.1:9001/evidence/review.html) · [Comparatif avant/après](http://127.0.0.1:9001/evidence/comparisons.html) · [Prototype jouable](http://127.0.0.1:9001/CloseStrategic/index.html). Le serveur est local, sur 127.0.0.1 uniquement. Les fichiers se consultent aussi directement dans ce dossier.

| Vidéo du canvas WebGL | Contenu |
|---|---|
| [Portrait 430 × 830 — partie](portrait-game.webm) | Vrai résultat du moteur, dés centraux, repli au départ réel, suivi et retour, inspection |
| [Portrait — banc visuel](portrait-fixtures.webm) | Pion aux 40 ancres au repos ; faces 1/6, 2/5, 3/4 et double 6 ; deux cases traversant le coin 10 ; long chemin par les quatre coins ; Reduced Motion et restauration |
| [Desktop 1200 × 900 — partie](desktop-game.webm) | Même intégration avec un vrai lancer dans une session locale fraîche |
| [Desktop — banc visuel](desktop-fixtures.webm) | Scénarios déterministes, visibilité et continuité aux quatre coins |

Les parties emploient les vrais dés du moteur. Le banc utilise une copie du pion existant, les vraies ancres / le vrai PawnMovement et des faits de dés construits uniquement pour présenter des valeurs déterministes ; ces faits ne sont jamais publiés comme actions économiques. Le parcours de 44 cases est un stress visuel, pas un jet de dés possible ni une simulation de la boucle économique. Le test ApplyState appelle la vraie méthode localement ; il faut recharger la page après ces fixtures avant de jouer une partie normale.

| Vue comparable | PASS 2C | PASS 2D |
|---|---|---|
| Portrait compact | [avant](before-portrait-overview.jpg) | [après](after-portrait-overview.jpg) |
| Portrait déployé | [avant](before-portrait-expanded-overview.jpg) | [après](after-portrait-expanded-overview.jpg) |
| Desktop compact | [avant](before-desktop-overview.jpg) | [après](after-desktop-overview.jpg) |

## Choix et intégration

[Audit initial](AUDIT.md). `BoardInteractionManager` conserve seul l'autorité caméra. Son fit utilise les 40 volumes de collider et l'enveloppe des pions existants placée aux 40 ancres. Cette enveloppe est normalisée à l'échelle maximale existante du pion actif (0,91), même si le premier cadrage précède son activation. Il ne cherche plus à contenir le socle complet. Une marge intérieure supplémentaire de deux pixels protège des limites flottantes exactes. Angle 40°, azimut 45°, orthographique, aucune rotation nouvelle.

En portrait, le size de repos passe de 20,253113 à 18,208172 : les dimensions projetées des cases gagnent 11,23 % et leur surface 23,72 %. En desktop, il passe de 11,567147 à 11,280901 : gain linéaire 2,54 %. Les 40 colliders et centres restent dans le viewport de référence aux deux dimensions. Le mesh du socle peut dépasser de 15,25 px sur chaque bord latéral du portrait ; aucune case interactive n'est retirée. Le gain est borné par la visibilité des pions, sans chercher un zoom arbitraire.

Le cockpit ne pilote toujours pas la caméra. Sa réserve compacte reste fixe à 236 unités UI ; ouverture, onglets, scroll et inspection n'entrent pas dans la clé de calibration. Le panneau déployé peut recouvrir la périphérie inférieure, comme en PASS 2C. Le repli automatique existant reste lié au début réel du mouvement visuel, pas au clic sur les dés ni à leur animation.

`BoardDicePresenter` remplace le rendu actif des grands dés UI de TopHUDWidget. Deux petits volumes à six faces réelles, chanfrein simple, pips et incrustations cyan/violet sont posés dans le centre libre. Ils utilisent trois meshes et trois matériaux URP/Lit partagés, six renderers sans collider, Rigidbody, texture, lumière ni réflexion temps réel ajoutés. La racine 3D est indépendante de l'échelle UI ; ces ressources sont réutilisées puis détruites avec le présentateur. Le graphite du plateau n'est pas modifié. La finition holographique est une variation discrète des incrustations, pas une simulation optique d'hologramme.

Les faces opposées totalisent sept. `FaceUp` oriente la face correspondant à chaque valeur confirmée vers le haut ; l'animation n'utilise aucun tirage aléatoire et ne décide d'aucun résultat. Le grand total, les rayons et l'ancien bouton Abréger ne sont plus montés. Les valeurs et le total restent présents dans l'historique métier existant. PulseDiceView et ses styles restent dans le dépôt comme ancien code de référence, sans chemin de rendu actif.

La préparation du lancer dure nominalement 0,94 s : rotation courte, petit rebond / settling, résultat stabilisé vers 80 % de la séquence. PlayerVisualManager attend uniquement la préparation déjà active pour cet acteur avant le dispatch original du pion. Le cockpit reste déployé pendant cette préparation. Au vrai dispatch, dés et cockpit reçoivent la notification existante, le suivi démarre et les dés disparaissent en 0,14 s. Un fait de dés reçu tardivement après son mouvement est ignoré visuellement ; il reste dans l'historique.

**Contrat de mouvement :** la lambda originale qui invoque OnPawnMovementFinished et PawnMovement sont identiques. La préparation ajoute du temps visuel avant le moteur d'animation ; aucun délai n'est ajouté après son callback et aucun résultat / paiement n'est déplacé par une nouvelle règle. TurnManager conserve son attente originale et son garde-fou de 10 s. La préparation est bornée à 1,1 s et exclue du mode SkipMovementAnimation. Les parties normales sont vérifiées séparément du stress de 44 cases, qui ne passe pas par cette attente métier.

Le suivi lit le chemin validé sans le modifier. Un point projeté 1,8 unité en avant suit le trajet, y compris ses angles. Le déplacement horizontal combine la progression du pion et cet espace d'anticipation avec amortissement exponentiel. Le pion garde sa liberté à l'écran ; il n'est pas verrouillé au centre. La verticale utilise uniquement les corrections de confort / visibilité. Le saut existant est couvert par une enveloppe fixe plutôt que suivi par la caméra.

À l'arrivée, pose et size sont figés pendant la micro-pause, puis retour vers le nouveau repos. La sélection / réaction d'arrivée existante est conservée, sans nouveau système d'états ni shader de case. Téléportation : aucun suivi par défaut. Restauration : annulation immédiate des travaux de présentation et retour au repos ; aucun nouveau message réseau. Reduced Motion : résultat statique, aucun zoom ni travelling, disparition sans contraction animée.

## Paramètres retenus

| Paramètre | Valeur |
|---|---|
| Angle / azimut | 40° / 45°, fixe |
| Overview ortho | Calculé sur cases + pions : 18,208172 portrait ; 11,280901 desktop |
| MovementZoomFactor | 1,14 par rapport au nouveau repos |
| FocusEntryDuration | 0,30 s, bornée à 0,28 s pour deux waypoints |
| Horizontal / vertical strength | 1,00 / 0,55 |
| HorizontalTravelShare | 0,50 |
| LookAheadDistance / strength | 1,80 unité monde / 0,55 |
| ComfortZoneWidth / height | 0,62 / 0,82 de la zone utile |
| CameraDamping | 0,16 s |
| ArrivalHold / return | 0,25 / 0,375 s |
| Dice duration / fade | 0,94 / 0,14 s |
| Die edge | 1,10 unité monde, indépendante du parent UI |
| Corps des dés | Base RGB 0,16 / 0,20 / 0,24 ; metallic 0,42 ; smoothness 0,48 |
| Pips | Base RGB 0,82 / 0,91 / 0,93 ; émission RGB 0,20 / 0,30 / 0,31 |
| Incrustation holographique | Bande 0,036 unité ; teinte cyan/violet localisée, émission ×0,28 |
| Reduced Motion | Option existante ; résultat immédiat, préparation sobre 0,24 s |

Les réglages caméra et dés sont sérialisés sur les composants existants / créés au runtime. Aucun grand asset de configuration ni dépendance supplémentaire.

## Fichiers et préservation du travail local

La comparaison est faite avec l'inventaire au début de PASS 2D, car le dépôt contenait déjà des changements locaux. [Périmètre vérifié](scope-validation.json), [avant](before-inventory.json), [après](after-inventory.json), [script de contrôle](verify_scope.py).

| Fichier modifié | Motif |
|---|---|
| `Assets/Scripts/Systems/BoardInteractionManager.cs` | Fit des cases / pions, marge, anticipation et réglage du zoom |
| `Assets/Scripts/UI/TopHUDWidget.cs` | Remplacement de la seule présentation des dés par BoardDicePresenter |
| `Assets/Scripts/Visuals/PlayerVisualManager.cs` | Préparation visuelle bornée avant le dispatch original ; invalidation sur restauration |
| `Assets/Scripts/Debug/StrategicCameraValidationDriver.cs` | Scénarios déterministes, dés, 40 positions, traces et contrôle après LateUpdate |
| `Assets/Scripts/Editor/MobileBoardPresenceValidation.cs` | 15 contrôles supplémentaires de faces / anticipation ; sortie de validation PASS 2D |

Créés : `Assets/Scripts/Visuals/BoardDicePresenter.cs` et son `.meta`. Les autres créations sont des outils / preuves de ce dossier et la page QA `Builds/CloseStrategic/capture.html` ; l'index jouable et le template du projet ne contiennent pas les outils de capture.

583 fichiers Assets/ProjectSettings préexistants restent identiques, notamment RulesEngine, économie, DiceSystem, TurnManager, PawnMovement, réseau, scène, 40 ancres / colliders, quatre meshes fusionnés, matériaux / shaders graphite, post-processing, cockpit UXML/USS et permissions. Le script compare aussi textuellement le callback de fin et le routage de raycast.

## Vérifications et limites

Les résultats chiffrés finaux figurent dans [evidence-analysis.json](evidence-analysis.json), les traces dans [browser-validation.json](browser-validation.json), le build dans [build.log](build.log). Les captures sont celles du canvas Unity, sans image générée ni maquette substituée à l'exécution.

Les 612 assertions existantes et 524 contrôles spécifiques passent avant le build : indices / raycasts des 40 cases, quatre meshes, invariants de projection, safe insets / DPR, callbacks vides / skip, faces réelles et anticipation au coin. Les anciens tests de PulseDicePose sont des régressions du code historique ; ils ne remplacent pas les nouveaux contrôles des dés intégrés.

Le build WebGL final réussit. Les deux bancs navigateur passent **76 contrôles chacun, soit 152 PASS et aucun FAIL**. Ils conservent les 40 positions de pion au repos dans chaque format. Les quatre clips décodent à leur résolution native, sans erreur média ; leurs journaux ne contiennent aucune erreur console enregistrée. Les lectures intermédiaires du journal sont fusionnées pour éviter la troncature du tampon navigateur.

| Mesure sous enregistrement | Portrait partie | Portrait banc | Desktop partie | Desktop banc |
|---|---|---|---|---|
| Échantillons caméra | 346 | 353 | 246 | 306 |
| Pion hors zone pendant mouvement / pause | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| Micro-pause observée | 263 ms | 300 / 326 ms | 282 ms | 334 / 256 ms |
| Retour observé | 392 ms | 421 / 404 ms | 474 ms | 406 / 405 ms |
| Trajet caméra horizontal / vertical échantillonné, unités | 2,57 / 0 | 18,34 / 0,09 | 1,97 / 0 | 17,65 / 9,22 |

Toutes les pauses échantillonnées gardent exactement la même pose et le même zoom. Angle 40° / azimut 45° constants dans les 1 251 échantillons. Le premier changement de travelling est observé dans les 0–105 ms du début ; le premier changement de zoom dans les 54–107 ms, à la résolution temporelle du journal. Ces chiffres ne constituent pas une mesure de latence tactile.

Les vrais jets donnent 2 + 3 en portrait et 5 + 2 en desktop. Les faces supérieures correspondent dans tous les échantillons de résultat visible. La caméra reste au repos pendant les dés ; le cockpit ne se réduit qu'au dispatch visuel, une fois dans chaque scénario concerné. Du début des dés au mouvement : 955 ms en portrait, 1 139 ms en desktop sous capture ; la lecture stabilisée dure respectivement 174 et 347 ms. La valeur nominale est 940 ms ; le scheduling des frames / événements reste perceptible sous instrumentation. Les pauses de 326 / 334 ms et le retour desktop de 474 ms dépassent aussi légèrement leurs plages cibles : ils sont conservés comme limites du prototype, sans prétendre à une cadence mobile validée.

La verticale du stress desktop est nécessaire à la visibilité aux coins dans son viewport plus large et moins haut. L'horizontale reste majoritaire ; elle est presque exclusive en portrait. Un mouvement d'action peut recadrer des cases / pions inactifs périphériques : la contrainte de visibilité des 40 cases s'applique au repos compact, celle du pion suivi au mouvement.

Captures clés : [dés portrait, vrai 2 + 3](portrait-game-frame-11820.jpg), [dés desktop, vrai 5 + 2](desktop-game-frame-4550.jpg), [suivi portrait](portrait-fixtures-frame-15000.jpg), [suivi desktop](desktop-fixtures-frame-18000.jpg). Ce sont des JPEG extraits des images décodées originales ; les trois vues comparatives « après » sont des captures fixes directes du build final, sans surimpression QA.

Repères approximatifs dans les clips : portrait partie 10,96 s début des dés, 11,74 s résultat, 11,91 s départ ; desktop partie 3,65 / 4,44 / 4,79 s. Le banc portrait démarre les deux cases au coin 10 vers 7,42 s, puis le parcours complet vers 9,25 s ; le banc desktop vers 9,45 et 11,24 s.

Les fixtures testent le volume réel de la copie de pion successivement aux 40 ancres, les faces réelles des dés, Reduced Motion, deux cases, le parcours complet des quatre coins, la pause sans second zoom, le retour, le repli unique, TeleportPlayer, ApplyState local, et l'ignorance d'une fin / d'un jet tardifs. Les parties avec vrais dés sont enregistrées dans des sessions fraîches distinctes des tests de restauration.

L'inspection directe de l'index 31 (Quantum AI Labs) et l'ouverture du cockpit sont revérifiées dans le build final. Projection et économie sont strictement identiques entre les trois relevés portrait de cette consultation. Les onglets Portefeuille / Flux GO et un geste de scroll ont été exercés dans l'itération précédente ; ils ne sont pas réexécutés après le dernier ajustement du seul cadrage des pions. Le contenu de test est court ; un portefeuille rempli avec long défilement n'est pas couvert.

Limites :

- Aucun téléphone physique, iOS / Android, multitouch, test transport réseau multi-client ou reconnexion réelle validé. ApplyState est exercé localement.
- Build Development avec traces et MediaRecorder. Pas de mesure GPU, frame time, mémoire ni draw calls. Le nombre de meshes / renderers indiqué décrit la structure, pas un profilage.
- Les traces sont échantillonnées au plus à 20 Hz. La visibilité échantillonnée n'est pas une preuve pour tous les frames possibles / toutes les plateformes.
- Les durées sont quantifiées par les frames ; les cibles 250 / 375 ms peuvent être dépassées sous capture. Les durées observées sont rapportées séparément.
- Un changement de joueur ou une restauration peut annuler la fin de séquence pour éviter un travelling tardif. Aucun callback économique n'attend la pause caméra.
- Le cockpit déployé recouvre encore une partie du plateau. Le fermer rend le parcours entier accessible ; son ouverture ne déclenche aucun recadrage.
- Les WebM peuvent afficher une durée initiale indéfinie dans certains lecteurs ; les images décodées, dimensions et instants sont contrôlés dans le navigateur.
- La première itération montrait des dés trop petits à cause de l'échelle UI et un contrôle d'anticipation évalué avant LateUpdate. Les preuves sont archivées dans `iteration-1/`. Une seconde itération a révélé deux coins desktop trop serrés avant la normalisation à la taille active ; elle est conservée dans `iteration-2/`. La racine contient les preuves finales après correction.

## Reproduction et suite

Build : définir `POLYRISK_WEBGL_OUTPUT` sur le chemin absolu `Builds/CloseStrategic`, puis lancer Unity 2022.3.62f3 en batch avec `-executeMethod PolyRisk.Editor.MobileBoardPresenceValidation.RunAndBuild`. Sans variable, le helper existant construit dans `/tmp/PolyRiskWebGLValidationFixed`.

Preview : `python3 Docs/Validation/CloseStrategic_2026-10-20/serve_webgl.py --build-root "$PWD/Builds" --port 9001`. La page QA est générée avec `make_capture_page.py`. Démarrer une partie locale fraîche, puis Record / Fixtures. Recharger après le banc de restauration avant de jouer.

Prochaine action : examiner les vidéos pour juger le rythme, l'anticipation et la finition des dés. **Arrêt après le prototype PASS 2D**, sans refonte des pions, du cockpit, des états de cases ou de l'économie.
