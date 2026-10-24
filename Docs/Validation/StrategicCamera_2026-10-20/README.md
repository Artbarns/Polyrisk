# PolyRisk — PASS 2C · prototype caméra stratégique

20 octobre 2026 · Unity 2022.3.62f3 · URP 14.0.12 · IL2CPP WebGL Development.

Le prototype remplace le focus manuel par trois états : `StrategicOverview`, `MovementFocus`, `ReturnToOverview`. La vue de consultation reste fixe ; chaque chemin visuel normal validé déclenche le rapprochement, un accompagnement en zone de confort, une pause sans nouveau zoom, puis le retour automatique. Aucun mécanisme financier n'attend la caméra. **Livraison arrêtée à PASS 2C ; validation artistique encore requise.**

## Preuves visuelles

Ouvrir [la galerie vidéo](review.html) ou [les comparatifs avant/après](comparisons.html). Avec le serveur local démarré : [galerie](http://127.0.0.1:9000/evidence/review.html), [prototype jouable](http://127.0.0.1:9000/StrategicCamera/index.html).

| Capture réelle du canvas | Scénario |
|---|---|
| [Portrait 430 × 830 — partie](portrait-game.webm) | Vrais dés, cockpit préalablement déployé, mouvement, arrivée, retour, inspection, cockpit |
| [Portrait — parcours déterministes](portrait-fixtures.webm) | Deux cases traversant le coin 10, 44 cases traversant les quatre coins, téléportation, application de snapshot, Reduced Motion |
| [Desktop 1200 × 900 — partie](desktop-game.webm) | Vrais dés, repli au départ visuel, focus, pause, retour et consultation |
| [Desktop — parcours déterministes](desktop-fixtures.webm) | Même banc de parcours / cas particuliers |

Les fixtures utilisent une copie du pion existant, les vraies ancres et le vrai `PawnMovement`. Leurs fins appellent uniquement l'observateur caméra ; elles n'émettent aucun faux callback global de gameplay. Après les fixtures, recharger la page avant de jouer : le vrai ApplyState du test active le contexte de réplication et le banc ne prétend pas restaurer une session locale jouable complète. Les clips de partie réelle sont capturés dans des sessions fraîches séparées.

Le parcours de 44 cases est un stress visuel volontaire, indépendant de la boucle économique et de son timeout de mouvement. Il ne représente pas un jet de dés possible.

| Comparaison de cadrage | Avant PASS 2B.2 | Après PASS 2C |
|---|---|---|
| Portrait compact | [capture](before-portrait-overview.jpg) | [capture](after-portrait-overview.jpg) |
| Portrait déployé | [capture](before-portrait-expanded-overview.jpg) | [capture](after-portrait-expanded-overview.jpg) |
| Desktop compact | [capture](before-desktop-overview.jpg) | [capture](after-desktop-overview.jpg) |

Les tailles et la projection des vues globales compactes restent identiques : plateau de 414 × 266,51 px en portrait et 786,01 × 506 px en desktop, élévation 40°, azimut 45°. Les fiches inspectées des captures déployées peuvent différer. Le changement principal se juge dans les vidéos, pas sur le rendu graphite inchangé.

## Décisions et intégration

[Audit ciblé](AUDIT.md). `BoardInteractionManager` conserve seul l'autorité caméra et le raycast. Sa calibration dépend des dimensions et de la safe area, jamais de la hauteur effective du cockpit ou de son contenu. La réserve compacte de référence est de 236 unités UI. Inspection, onglets et consultation ne lancent aucune interpolation.

`PlayerVisualManager` émet une notification locale immédiatement avant son appel existant à `MoveAlongPath`. Le callback original qui publie `OnPawnMovementFinished` est conservé à l'identique. La caméra observe la fin, vérifie le pion et sa destination ; elle ne possède ni n'invoque ce callback. Le cockpit écoute séparément le début visuel et se réduit une fois, immédiatement si nécessaire. Il reste compact ensuite. Les anciens replis au clic sur les dés / début de tour ont été retirés.

`OnlineGameController.ApplyState` émet une notification de présentation après succès ; la caméra annule son suivi et restaure immédiatement l'overview. Le résultat, les écritures du snapshot et le protocole sont inchangés. Téléportations : aucun nouveau travelling ; un saut inattendu pendant le suivi annule la séquence. Changement de joueur, destruction du pion et restauration invalident également les travaux restants.

Le suivi raisonne dans les axes de la caméra, pas dans les axes monde. Il suit le plan de déplacement, sans reproduire les sauts verticaux. Une enveloppe des renderers est calculée une fois au départ, avec 4 % de marge d'échelle et 0,60 unité de réserve verticale pour le saut existant. Cela évite d'utiliser uniquement le centre du pion pour protéger les bords. Aucun raycast supplémentaire de suivi, aucune allocation de géométrie par frame, aucune bibliothèque ajoutée.

## Paramètres retenus

| Paramètre | Valeur |
|---|---|
| Overview angle / orientation | 40° / azimut de scène conservé à 45° ; orthographique |
| Overview orthographic size | Calculé : 20,253113 en 430 × 830 ; 11,567147 en 1200 × 900 |
| MovementZoomFactor | 1,20 — cible size 16,877594 / 9,639289 |
| FocusEntryDuration | 0,20 s, bornée pour chemins courts |
| FollowHorizontalStrength | 1,00 |
| FollowVerticalCorrectionStrength | 0,55 |
| ComfortZoneWidth / Height | 0,62 / 0,82 de la zone utile |
| CameraDamping | 0,12 s, amortissement exponentiel |
| ArrivalHoldDuration | 0,25 s, reprise au premier frame suivant l'échéance |
| ReturnDuration | 0,375 s, interpolation smoothstep |
| ReducedMotionEnabled | Option existante UIUXRuntime, caméra toujours globale |

Les paramètres de mouvement sont sérialisés sur le contrôleur existant ; l'angle et le size global restent les invariants / résultats de calibration existants. Pas de nouvel asset de configuration.

## Fichiers et préservation du travail local

Comparaison avec l'inventaire pris **au début de cette passe**, pas avec HEAD (le dépôt contenait déjà beaucoup de changements). [Preuve de périmètre](scope-validation.json), [inventaire initial](before-inventory.json), [inventaire final](after-inventory.json).

| Fichier modifié | Raison |
|---|---|
| `Assets/Scripts/Systems/BoardInteractionManager.cs` | Trois états, fit stable, confort et protection du pion, annulation, retrait du focus manuel |
| `Assets/Scripts/Visuals/PlayerVisualManager.cs` | Notification locale avant le dispatch visuel, callback original intact |
| `Assets/Scripts/UI/ActionButtonsWidget.cs` | Repli unique au départ réel, retrait des anciens déclencheurs de repli |
| `Assets/Scripts/Multiplayer/OnlineGameController.cs` | Signal local après application réussie d'un snapshot |
| `Assets/UI/Layouts/TopHUD.uxml` | Suppression du bouton Approcher / Vue entière |
| `Assets/UI/Styles/Theme.uss` | Retrait des seuls styles du contrôle manuel |
| `Assets/Scripts/Editor/MobileBoardPresenceValidation.cs` | Régressions caméra / géométrie / callbacks et build |
| `Assets/Scripts/Debug/MobileBoardValidationProbe.cs` | Mesures du cadrage et état caméra, retrait de l'ancien test de focus manuel |

Créés : `Assets/Scripts/Debug/StrategicCameraValidationDriver.cs` et son `.meta`, banc opt-in limité à Editor / Development Build. Les autres créations sont les preuves et outils locaux de ce dossier ; `Builds/StrategicCamera/capture.html` est une page QA générée, distincte de l'index jouable et du template livré.

578 fichiers Assets/ProjectSettings préexistants restent identiques à l'inventaire de départ, notamment RulesEngine, économie, TurnManager, PawnMovement, scène, géométrie, 40 ancres/colliders, quatre meshes fusionnés, matériaux, shaders, post-processing et DTO réseau. Le [script de contrôle](verify_scope.py) vérifie aussi textuellement le callback, la logique réseau et le routage d'entrée / permissions préservés.

## Vérifications exécutées

- Compilation C# et build IL2CPP WebGL : résultat détaillé dans [build.log](build.log).
- Régressions existantes : économie 143, mécaniques 148, événements 17, assets UI 304 assertions.
- Validation spécifique : 509 contrôles, dont 40 indices uniques / raycasts directs et écran, quatre meshes, projection, corrections de confort, callbacks vide / skip, safe insets, DPR et fonctions de fit.
- Parties locales réelles WebGL aux deux dimensions, interactions par clic/tap, journaux caméra et vidéos du canvas.
- Banc déterministe aux deux dimensions : deux cases, long chemin / quatre coins, callback unique / destination, pause sans nouveau zoom, retour, repli, TeleportPlayer réel, ApplyState réel exécuté localement, fin tardive ignorée, Reduced Motion et restauration de la position logique originale.
- Inspection de Quantum AI Labs (index 31), ouverture/fermeture, onglets Portefeuille et Flux GO, geste de défilement dans le contenu : caméra globale conservée. Ce contenu court n'offre pas un long défilement représentatif d'un portefeuille rempli.

Résultats échantillonnés et durées observées : [evidence-analysis.json](evidence-analysis.json). Traces brutes : [browser-validation.json](browser-validation.json). Les échantillons sont plafonnés à 20 Hz et dépendent des frames rendus ; ils ne constituent pas une mesure de performance ni une preuve pour tous les frames possibles.

## Résultats du build final

1 121 assertions / contrôles Editor passent (612 existants + 509 spécifiques), puis compilation IL2CPP WebGL réussie. Les deux bancs WebGL passent chacun 18 vérifications, soit 36 PASS et aucun FAIL. Les quatre clips ne rapportent aucune erreur console.

Sur 398 échantillons de mouvement (21 partie portrait, 158 banc portrait, 72 partie desktop, 147 banc desktop), aucun volume de pion ne sort du viewport de référence. Les pauses échantillonnées gardent exactement la même position et le même size. Rotation observée constante 40° / 45°. La projection et l'économie restent exactement identiques entre les états d'inspection / onglets / cockpit mesurés.

La partie portrait observe une pause de 268 ms et un retour de 395 ms. Les bancs observent des pauses de 253–325 ms et des retours de 400–474 ms. Les cibles sont 250 / 375 ms, mais la reprise au prochain frame sous capture dépasse parfois les plages souhaitées (300 / 450 ms). **Le respect strict de ces plafonds sous charge n'est donc pas validé.** Aucun délai n'est ajouté au gameplay.

Le long parcours portrait représente 10,16 unités de déplacement horizontal dans le plan caméra contre 0,027 verticale : l'accompagnement est essentiellement latéral. En desktop, la largeur disponible absorbe presque tout le parcours latéral ; la protection des limites haut/bas impose davantage de correction verticale (2,02 horizontales / 10,26 verticales sur le stress de 44 cases). Ce comportement doit être évalué artistiquement sur le clip desktop.

Une séquence IA du clip desktop est réinitialisée avant son retour animé, lors d'une invalidation de séquence / nouvelle phase ; la trace la distingue d'une pause normale. La caméra privilégie alors le retour global immédiat pour éviter un travail tardif. Le journal desktop est partiel au début (à partir de 9,04 s de vidéo, tampon navigateur limité), tandis que la vidéo contient bien l'introduction complète. Les traces des deux bancs et de la partie portrait couvrent leurs clips depuis le départ.

Images décodées du build final : [travelling portrait à 9 s](portrait-fixtures-frame-9000.jpg), [coin inférieur desktop à 14 s](desktop-fixtures-frame-14000.jpg). [Empreintes de l'exécutable final](build-sha256.json).

## Limites et validation artistique

- Exécution WebGL sur navigateur desktop aux dimensions portrait ; aucun téléphone physique, iOS / Android, multitouch, mesure GPU, draw calls, mémoire ou benchmark de frame time validé.
- Restauration testée par la vraie méthode ApplyState localement. Aucune reconnexion transport réelle ni session multi-client validée dans cette passe.
- Les panneaux déployés peuvent recouvrir le bas du plateau : conséquence assumée de la caméra stable. Le cockpit compact redonne accès au parcours entier ; les actions restent présentes en bas.
- Les dés et feedbacks déjà présents peuvent temporairement recouvrir le pion. Ils sont conservés, hors périmètre caméra.
- Les WebM proviennent de MediaRecorder et peuvent afficher une durée initiale indéfinie selon le lecteur. Les dimensions / images décodées sont vérifiées dans le navigateur ; les journaux donnent les instants de séquence.
- La réserve de saut correspond au PawnMovement actuel. Un futur changement de silhouette ou d'amplitude devra être revérifié, sans refonte de pion dans cette passe.
- Le premier stress desktop a révélé une marge insuffisante du volume du pion près du coin inférieur. Les preuves de cette itération sont conservées dans `iteration-1/`. Les captures livrées à la racine sont celles de la correction finale.

Prochaine action : examiner les vidéos portrait et desktop pour valider le rythme et l'amplitude. **Arrêt après PASS 2C**, sans modernisation des pions, nouvelles animations de cases ni polish FX.

## Reproduction locale

Build : définir `POLYRISK_WEBGL_OUTPUT` sur le chemin absolu `Builds/StrategicCamera`, puis lancer Unity 2022.3.62f3 en batch avec `-executeMethod PolyRisk.Editor.MobileBoardPresenceValidation.RunAndBuild`. Sans cette variable, le helper existant construit dans `/tmp/PolyRiskWebGLValidationFixed`. Le build final a été copié de cette sortie vers le dossier servi et son nouveau wasm est vérifié par empreinte.

Preview : `python3 Docs/Validation/StrategicCamera_2026-10-20/serve_webgl.py --build-root "$PWD/Builds" --port 9000`. Le serveur écoute uniquement sur 127.0.0.1. Pour les fixtures / vidéos, générer la page QA avec `make_capture_page.py`, ouvrir `capture.html`, démarrer une partie locale, puis Record / Fixtures. Recharger la page avant une partie réelle après les fixtures de restauration.
