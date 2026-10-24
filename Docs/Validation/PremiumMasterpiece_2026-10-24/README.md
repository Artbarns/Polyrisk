# PolyRisk — Graphite architectural, 24 octobre 2026

La proposition ajoute une lecture satinée des faces et des arêtes, relie les monolithes au matériau du plateau et fait recevoir les contacts lumineux aux quatre coins. Le bloom global est retiré : l'essai.045 ne modifiait aucun pixel de la zone de plateau au repos comparée, mais ajoutait16drawcalls et16SetPass. La différence A/B doit être appréciée dans les vraies images ; elle n'est pas déclarée artistiquement validée par la compilation.

Ouvrir **[la galerie publique](https://artbarns.github.io/Polyrisk/Docs/Validation/PremiumMasterpiece_2026-10-24/review.html)**, [la galerie portable](review.html) depuis ce dossier, ou [la galerie locale](http://127.0.0.1:9007/evidence/review.html) pendant que son serveur tourne. Les fichiers et vidéos restent accessibles dans le dossier si le lien local s'arrête. Ce lien loopback n'est pas un jeu publié pour des iPhone distants.

## Preuves visuelles finales

- Le sélecteur de la galerie donne dix comparaisons au même cadrage : graphite inactif, propriété de présentation, sélection, contact, arrivée, monolithe au repos/actif, métal, reflet et coin. Fichiers `reviewed-final-*-a/b.jpg`.
- [Portrait au repos B](final-rest-b.jpg), [cockpit déployé](final-cockpit-expanded-b.jpg), [propriétés acquises en partie réelle](final-owned-b.jpg), [A correspondante](final-owned-a.jpg).
- [A native430](final-native430-a.jpg) / [B native430](final-native430-b.jpg) vérifient aussi le framebuffer430×830. Les autres portraits sont des captures430×830CSS d'un framebuffer860×1660, deviceDPR réel1 sur Mac. Ils ne simulent ni le GPU ni Safari d'un iPhone.
- [Court B avec son](final-short-b.mp4), [long B avec coin](final-long-b.mp4), [même trajectoire longue A](final-long-a.mp4), [achat puis tours réels25s](final-live-turns-b.mp4). Vitesse réelle, audio du jeu, pas de nouvelles images ni d'interpolation vidéo. Les sourcesWebM sont conservées.
- Les trajectoires du banc empruntent GetPath/PawnMovement mais n'avancent aucun état métier. Leurs vidéos retirent uniquement la restauration QA finale après le retour au calme. Le clip des tours réels est intégral. Voir les fichiers `*-media.json` pour les durées, coupes et vérifications audio.
- Les images des pics sont figées pour comparer les surfaces ; leur intensité n'est pas une lumière permanente. Les couleurs des spécimens n'attribuent aucune propriété au modèle.

La baseline A est le traitement ReflectiveNeon du commit9242725, réappliqué dans le même player avec sa branche de shader, sa key, son ambiance et son bloom. La correction d'occlusion des identités UI s'applique aux deux côtés. Ce n'est pas une comparaison entre deux binaires indépendants. Les poses, hashes économiques et coefficients sont archivés dans [les readbacks](final-comparison-readbacks.json).

Les captures finales utilisent B/stage3 du dernier QA ; le Release utilise B/stage4 avec bloom0, identique visuellement. Les photos antérieures sans préfixe `reviewed-final-`, ainsi que stage4 àbloom.045, sont des diagnostics et essais archivés. Une collision de nom a remplacé l'ancienWebM `final-natural-b` : son ancienMP4 est conservé sous `iteration2-natural-b`, explicitement hors preuves finales. La nouvelle prise25s porte le nom unique `final-live-turns-b`.

## Décisions et paramètres

[Audit des causes et des itérations](AUDIT.md) · [valeurs exactes A/B](PARAMETERS.md) · [mode d'emploi du banc](CALIBRATION.md).

Une Directional douce à55°/−25°, remplissageTrilight et deux panneaux analytiques neutres révèlent les orientations sans ajouter de lumières. Le socle, les cases, les chanfreins virtuels et les monolithes ont des réponses distinctes. Les lobes de studio sont une approximation artistique ; ils ne sont pas une réflexion physique de l'environnement.

La source reste géométriquement fine. La réception locale est séparée, bornée à.16unité au repos et.28en contact, avec atténuation rapide. Les quatre inlays de coin reçoivent désormais la texture contact existante. Les pics HDR sont comprimés pour éviter les cadres saturés. Aucun bruit procédural, probe temps réel, nouvelle passe, nouveau mot-clé ou texture supplémentaire. Une couverture par dérivées protège certaines arêtes ; aucune garantie de largeur CSS universelle n'est revendiquée.

Géométrie4,5%, index40, meshes fusionnés, colliders, règles, économie, palette fonctionnelle, chemins, timing/callbacks/audio de déplacement et caméra1,40 restent inchangés. L'enveloppe32ms/145ms, arrivée215ms, est conservée. Les identités projetées sont maintenant bornées au viewport du board et occultées par le cockpit réel : elles ne flottent plus sur le HUD quand les cases sortent du cadrage.

## Fichiers

Modifiés :

1. `Assets/Scripts/Board/BoardVisualTheme.cs` — profil ArchitecturalGraphite et architecture lumineuse.
2. `Assets/Scripts/Visuals/BoardSurfaceLighting.cs` — application partagée, éclairage et calibration par couches.
3. `Assets/Scripts/Visuals/BoardContactFeedback.cs` — même texture liée au receveur des coins.
4. `Assets/Scripts/Visuals/CompactPawnVisual.cs` — finition des deux matériaux existants, sans refonte de mesh ni mouvement.
5. `Assets/Scripts/UI/BoardTileOverlayWidget.cs` — occlusion des identités derrière HUD/cockpit.
6. `Assets/Settings/GraphiteFoundation/GraphiteTheme.asset` — valeurs artistiques retenues.
7. `Assets/Shaders/GraphiteSurface.shader` — matériau architectural et réception locale ; branche baseline conservée.
8. `Assets/Shaders/NeonState.shader` — cœur de source et compression des pics, uniquement dans le nouveau traitement.

Créés : `Assets/Scripts/Debug/ArtCalibrationDriver.cs`, `Assets/Scripts/Editor/ArtCalibrationValidation.cs`, `Assets/Scripts/Editor/ArtCalibrationWindow.cs`, leurs.meta et ce dossier de preuves. Le banc runtime et son montage sont compilés hors Release nonDevelopment. Aucun boutonQA/recorder n'entre dans le paquet de déploiement.

## Validations exécutées

- Editor :612assertions économiques existantes,31contrôles statiques de caméra rapprochée,165contrôles de calibration/raycast/shaders passent. Les nombres et résultats sont dans [editor-checks.json](editor-checks.json).
- WebGLDevelopment : dernière recette de présentation126contrôles sans échec ; géométrie, état économique, colliders, caméra et identités. [Journal](final-short-browser.json).
- CaméraWebGL :176contrôles sans échec en état stable,40cases explorables, raycast/sélection, distinction tap/drag, court/long44pas/quatre coins, micro-pause et ReducedMotion. [Journal](final-camera-stable-dpr1.json).
- Enveloppe lumineuse : huit assertions passent lors de la recette isolée àDPR1, dont texture conforme à l'âge réel, retrigger, extinction, ReducedMotion et persistance des états. [Journal](final-envelope-dpr1.json), dernière exécution terminée.
- Chaque trajectoire courte/longueA/B a un callback, une destination exacte et un checksum économique inchangé. Les quatreMP4 finaux se décodent avec audio non écrêté ; cela ne certifie pas l'écoute sur haut-parleur d'iPhone.
- Serveur local :26contrôles de livraison same-origin, MIME/compression/cache/range, isolement des données et protocole six joueurs. Six sessions restaurées après redémarrage, commandes, hosthandoff et partages de loyers conservés. [Hosting](same-origin-hosting-test.log) · [reconnexion](six-player-reconnect-test.log). Ce sont des clients protocole, pas sixiPhone rendus.
- [Contrôle du périmètre](scope-validation.json) :639fichiers de référence, huit changements autorisés,59fichiers protégés inchangés, notamment règles, réseau, caméra et PawnMovement. Les changements locaux préexistants hors périmètre restent présents.
- Release nonDevelopment : compilation réussie,0erreur/0avertissement,430,99s,12 651 782octets. Le vrai build a été exécuté en navigateur à430×830 puis1200×900, avec captures. La recetteHTTP vérifie ses fichiers réels et le protocole six joueurs passe sur le serveur qui les livre. Voir `release-build-summary.json`, `release-http-validation.json`, `actual-release-protocol-test.log`, `release-portrait.jpg`, `release-desktop.jpg`. Le paquet et sonSHA256 sont dans `release-package.json`.

Échecs conservés : la première compilationQA avait des types de compteurs incorrects, corrigés. Un essai a cherché l'overlay dans le mauvais document UI ; un autre a lancé les checks avant la partie. Un test de caméra lancé en planning a croisé son expiration : deux assertions de hold ont échoué ; la recette complète repasse en décision stable. Un test d'enveloppe a été lancé en même temps que la caméra : son assertion d'immobilité était invalide. ÀDPR2 lent, son impulsion courte peut aussi être terminée avant l'échantillon. Les journaux restent archivés ; aucun résultat ancien en échec n'est transformé en réussite.

## Performance mesurée

Une seule prise6s par condition, Mac Intel IrisPlus, un playerDevelopment actif, qualitéLow, renderScale1, SMAAHigh, hors compilation/encodage/enregistrement. [Données complètes](performance-summary.json).

| Framebuffer | Traitement | Moyenne | Médiane | p95 | Drawcalls | SetPass |
|---|---|---:|---:|---:|---:|---:|
|430×830|A précédent|33,68ms|30ms|50ms|72|37|
|430×830|B final sans bloom|17,09ms|17ms|19ms|56|20|
|860×1660|B avec bloom.045, écarté|150,59ms|151ms|161ms|86|30|
|860×1660|B sans bloom|83,74ms|84ms|92ms|70|14|

Le bloom supprimé explique une part importante du gain ; ces prises ne mesurent pas isolément le coût ALU du shader. ÀDPR1, mémoireUnity allouée52,01→52,03Mo, réservée60,36→60,38Mo environ ; navigateur/GPU non inclus. Le compteurMainThread inclut des attentes : ce n'est pas un coûtCPU de calcul isolé. Pas de tempsGPU disponible. Le nombre51/2 de variantes indiqué par l'Editor est le readback d'import, pas le nombre de variantes effectivement livrées après stripping.

La priseA haute densité avait une condition de visibilité du navigateur différente ; elle n'est pas utilisée pour annoncer un gain temporel A/B. Le renduDPR2 reste lent sur ce Mac et les contacts courts peuvent manquer d'images intermédiaires. Aucun60fps mobile ni compatibilitéSafari physique n'est annoncé. RenderScale et politiqueDPR livrés restent ceux de la release précédente ; choisir une limite différente nécessite une recette sur appareils.

## Release et limites restantes

Le paquet same-origin réunit le client final et le backend.NET10 existant, une instance avec stockage persistant hors racine Web. Les tests locaux du protocole sont distincts de la validation graphique. Docker n'est pas disponible ici ; l'image de conteneur n'est donc pas construite/testée dans cette passe.

Mise à jour après publication par Arthur : le dépôt https://github.com/Artbarns/Polyrisk et le remote origin sont configurés. main local et distant correspondent au commit73dd655. La galerie GitHub Pages est publique enHTTPS et ses médias ont été vérifiés ; elle ne livre pas le jeu multijoueur. Aucun hébergement de serveur persistant n'est encore connecté, aucune partie distante ni validation sur iPhone physique n'est revendiquée. Voir [la reprise après synchronisation GitHub](GITHUB_RESYNC.md).

Prochaine action : apprécier les vrais comparatifs, puis valider les performances/Safari/tactile sur appareils et le multijoueur sur un hébergementHTTPS persistant. La finition artistique et la release publique ne sont pas déclarées terminées sur la seule base des tests locaux.

## Livraison et reprise de la prévisualisation

Le code artistique et le banc proviennent du commit **040d456** ; les documents et captures ont été enregistrés séparément. Arthur les a publiés sur `main` avec son import **73dd655**, qui ajoute également des archives et documents préexistants. Cet import ne modifie aucun fichier sous Assets, Server, Packages ou ProjectSettings par rapport au dernier point artistique9773edb.

La recette de reprise confirme les dix couples A/B à caméra, résolution, position logique et hash économique identiques : [contrôle A/B](ab-consistency-validation.json). Le serveur rouvert valide les 20 captures et les 4 vidéos avec requêtes partielles206 ; son proxy passe également le protocole six joueurs : [médias](reopened-gallery-validation.json) · [protocole](reopened-proxy-protocol.log).

Les 109 captures dont le contenu était JPEG ont désormais une extension `.jpg` correcte. Les octets et SHA256 n'ont pas changé : [table des renommages](capture-formats.json). Les journaux historiques conservent les anciens noms PNG ; cette table permet de les retrouver. Les images PNG extraites par FFmpeg restent PNG. Aucune image n'est générée ni retouchée.

Pour relancer la prévisualisation après arrêt du Mac, depuis la racine du dépôt :

```sh
python3 Docs/Validation/PremiumMasterpiece_2026-10-24/preview.py
```

Ce lanceur conserve le serveur 5097 et la galerie 9007 dans des processus détachés du terminal et vérifie leur réponse HTTP. Un second lancement réutilise les ports déjà prêts. Il ne crée aucun service de démarrage, aucune tâche récurrente et aucune modification du jeu. Les données et journaux du serveur sont dans `/tmp/polyrisk-premium-preview`, hors racine Web ; ce stockage de prévisualisation n'est pas une solution de production persistante. Les liens locaux fonctionnent sur ce Mac tant que les processus tournent.

- Galerie : http://127.0.0.1:9007/evidence/review.html
- Build Release local : http://127.0.0.1:5097/
- Paquet préparé : `Builds/PolyRisk-PremiumMasterpiece-20261024.zip` ; SHA256 `c4ee1cc85131296e1829b55bc2a3487e4d9f782d17ca5fa51304a81e5a92c0be`.

La reprise confirme GitHub main73dd655 et la galerie publique. Les14empreintes des sources artistiques, les24fichiers du bundle, leurs entrées actuelles et les25ressources de galerie correspondent. Aucun reset ni push forcé. L'accès à un hébergement avec stockage persistant reste nécessaire à la publication du jeu multijoueur ; le choix gratuit envisagé est Oracle Always Free, sous réserve de compte et de capacité disponible. Railway reste une alternative gérée payante, sans abonnement souscrit.
