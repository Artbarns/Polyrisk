# PASS 2F.1 — Perceptual Polish & Camera Composition

Livraison limitée à PASS 2F.1. Validation artistique attendue.

## Audit et décisions

Voir `AUDIT.md`. Le chemin actif reste SampleScene / BoardInteractionManager / PlayerVisualManager / PawnMovement / CompactPawnVisual / BoardContactFeedback / GraphiteSurface + NeonState. Aucun nouveau writer de caméra.

Le suivi précédent utilisait la moitié du déplacement horizontal depuis le centre d’entrée et une grande zone de confort. La nouvelle cible suit le point au sol du pion autour d’un ancrage dans `BoardViewport`, puis ajoute une anticipation courte le long du chemin déjà validé. L’entrée conserve son temps, le vecteur d’anticipation est filtré aux coins et les deux axes ont des amortissements distincts. Le saut logique n’est pas suivi verticalement. Le garde de visibilité existant reste actif. Le retour, le hold, le zoom et la calibration au repos ne changent pas.

Le contact précédent démarrait au maximum, conservait ce pic sur les premiers 18 % de sa durée puis décroissait. Il utilise maintenant une attaque et une décroissance smoothstep continues. Un retrigger repart de l’intensité encore présente. La durée de décroissance des nouveaux contacts est bornée par leur cadence lorsqu’ils se suivent rapidement ; les arrivées gardent une enveloppe plus longue. Le canal transitoire reste séparé des pixels persistants. Aucun son, waypoint ou callback n’est retardé.

B partage désormais les coefficients principaux de matière neutre de A. Sa différence est concentrée sur le contact : reflet coloré plus lisible près du périmètre, réponse satinée locale et reflet de base légèrement renforcé. La portée reste bornée dans l’espace du plateau. Aucun changement de géométrie, de saturation globale ou de bloom.

## Paramètres retenus

Caméra (Inspector du contrôleur existant) : ancrage X **0,50**, Y **0,52** ; anticipation **0,75 unité monde** ; amortissement horizontal **80 ms**, vertical **120 ms** ; zone de confort **0,16 × 0,22** de la zone utile ; filtrage des coins **140 ms**. Zoom action **1,14**, entrée **300 ms**, hold **250 ms**, retour **375 ms**, orthographique **40° / 45°** conservés.

Contacts (`GraphiteTheme.contactLight`) : attaque **32 ms**, décroissance **145 ms**, durée **177 ms**, pic **1,15** ; arrivée décroissance **215 ms**, durée **247 ms**, pic **1,65** ; Reduced Motion **×0,45**. Couleur : mélange d’identité **0,70**. En cadence rapide, décroissance ordinaire `min(145 ms, max(55 ms, intervalle × 0,8))`. Aucun allongement de déplacement ou du Magnetic Click.

| Paramètre | A Satin | B Reflective Neon |
|---|---:|---:|
| Metallic socle / cases | 0,36 / 0,27 | 0,36 / 0,27 |
| Smoothness socle / cases | 0,47 / 0,41 | 0,47 / 0,41 |
| Studio neutre | 0,46 | 0,46 |
| Arête spéculaire | 0,035 | 0,040 |
| Emission néon au repos | 0,82 | 0,88 |
| Réflexion de bande au repos | 0,06 | 0,08 |
| Falloff bande / portée | 0,025 / 0,09 | 0,030 / 0,10 |
| Réflexion contact / arrivée | 0,035 / 0,075 | 0,78 / 1,10 |
| Falloff contact / portée | 0,025 / 0,09 | 0,065 / 0,22 |
| Contraste satiné actif | 0 | 0,14 |
| Gain contour de contact | 0,90 | 1,12 |
| Reflet de base du pion | 0,008 | 0,065 |
| Séparation du socle | 0,012 | 0,014 |

Les teintes de matière et couleurs fonctionnelles sont conservées. Les autres réglages du pion restent ceux de 2F, sans toucher à sa silhouette, ses animations ou ses sons. L’exposition reste +0,15 ; bloom 0,08 / seuil 1,15 / scatter 0,2 ; contraste 6, saturation −8, ACES. Low conserve sa directional existante sans ombres.

## Fichiers

Modifiés :
- `Assets/Scripts/Systems/BoardInteractionManager.cs` — composition en mouvement uniquement.
- `Assets/Scripts/Visuals/BoardContactFeedback.cs` — enveloppe continue et retriggers.
- `Assets/Scripts/Board/BoardVisualTheme.cs` — paramètres de contact et de matériau.
- `Assets/Settings/GraphiteFoundation/GraphiteTheme.asset` — valeurs réellement sérialisées.
- `Assets/Scripts/Visuals/BoardSurfaceLighting.cs` — transfert des nouveaux paramètres, même système partagé.
- `Assets/Shaders/GraphiteSurface.shader` — reflet et contraste local sous contact.

Créés : `Assets/Scripts/Debug/PerceptualPolishValidationDriver.cs`, `Assets/Scripts/Editor/PerceptualPolishValidation.cs`, leurs `.meta` et ce dossier de preuves.

## Validation et médias

Exécution commencée le 22 octobre et finalisée le **23 octobre 2026** ; le nom du dossier conserve la date de début. Les preuves sont des captures réelles du WebGL compilé ; aucun visuel généré.

- **Compilation finale Unity 2022.3.62f3 / URP 14.0.12 / WebGL IL2CPP Development réussie**, avec le build final et son empreinte dans `build.log` / `build-sha256.json`.
- **612 assertions existantes** passent : 143 économie, 148 mécanique, 17 événements, 304 assets UI. Elles couvrent notamment des contrats de snapshots/réseau disponibles localement ; elles ne remplacent pas un test multi-postes réel.
- **315 contrôles Editor** passent : 40 indices/raycasts, quatre meshes, valeurs de matière, géométrie/ancres, horloges conservées, attaque croissante/décroissance monotone et bornées, ancrage/zoom. Les deux shaders obtiennent SRP Batcher **code 0 OK après compilation Metal** (`editor-checks.txt`).
- **95 contrôles WebGL A/B** passent : colliders, meshes, caméra immobile lors du changement de finition, pixels persistants, snapshot économique complet, positions, locks, sons et préférence Reduced Motion (`runtime-invariants.json`).
- **8 contrôles d’enveloppe** passent dans le WebGL : texture conforme à la courbe au temps réellement échantillonné, retrigger continu, contacts rapprochés sans résidu, arrivée indépendante du callback, intensité Reduced Motion bornée et états persistants intacts. Le premier échantillon mesuré à 53 ms a une intensité 1,08496 pour un pic de 1,15 ; l’attaque de 32 ms n’est donc pas entièrement échantillonnée sur ce poste.
- **157 contrôles du monolithe passent pour A, puis 157 pour B** : courts/longs trajets, les quatre coins, six occupants, callback unique à la destination, clic et contact final sur la même horloge, absence de traîne, frames lentes, Reduced Motion et annulation locale par ApplyState. Les six parcours comparés produisent exactement les mêmes **30 indices de contact** que la référence 2F. Le test d’annulation qui suit a produit un contact intermédiaire supplémentaire en A avant l’application de l’état ; aucun callback final ni replay n’a suivi. Il est conservé dans les logs et distingué des six parcours comparés.
- **75 contrôles caméra passent** : dés de présentation QA sans mouvement de caméra, départ court, trajet de 44 cases passant les quatre coins, repli automatique du cockpit, hold fixe, retour, téléportation, restauration d’état et Reduced Motion (`after-b-camera-browser.json`). Le banc caméra historique utilise un clone dont la silhouette accompagne le root ; les contacts/Precision Glide sont évalués séparément par le banc monolithe et la partie réelle.
- **Sélection réelle par raycast** : case 31 / Quantum AI Labs, loyer 450, rendement 10,0 dans l’inspecteur. La pose caméra, son angle et sa taille restent strictement identiques avant/après inspection, puis après déploiement réel du cockpit (`selection-mobile.jpg`, `cockpit-expanded.jpg`, `inspection-cockpit-browser.json`, `cockpit-expanded-browser.json`).
- **Périmètre** : six fichiers existants modifiés, quatre nouveaux fichiers source/meta, **600 fichiers préexistants identiques octet pour octet**. Scène, meshes, 40 ancres/colliders, PawnMovement, CompactPawnVisual, PlayerVisualManager, TileVisuals, règles, économie, réseau, UI, dés et sons préservés. La calibration du repos, le hold, le retour et la sélection du contrôleur caméra sont conservés (`scope-validation.json`).

### Résultat du suivi

Mesures du point au sol dans la zone utile 414 × 444, en omettant les 450 ms d’entrée, le hold et Reduced Motion. La comparaison analytique projette le même plan de référence dans les traces caméra ; le nouveau banc fournit aussi une lecture directe indépendante.

| Prise | X utile min–max | Y utile min–max | Écart horizontal moyen à 50 % |
|---|---:|---:|---:|
| 2F — référence | 12,7–85,5 % | 21,3–80,4 % | 14,9 points |
| 2F.1 — A | 46,8–53,3 % | 49,9–52,6 % | 2,47 points |
| 2F.1 — B | 46,8–53,3 % | 49,8–52,6 % | 2,51 points |

La lecture directe B sur 67 échantillons confirme X **46,75–53,30 %**, Y **49,84–52,62 %**. La caméra accompagne le pion autour de l’ancrage plutôt que de le laisser parcourir une grande zone. Ces chiffres concernent les parcours enregistrés, pas tous les appareils ou toutes les cadences. L’entrée depuis un bord conserve une transition ; elle est exclue des bornes stabilisées. Un seul contact lumineux simultané au maximum est observé dans les parcours normaux B ; le banc de retrigger vérifie séparément les contacts rapprochés.

### Médias livrés

- Captures au repos 430 × 830 : `baseline-portrait.jpg`, `after-a-portrait.jpg`, `after-b-portrait.jpg`, quatre joueurs à GO et cockpit compact. Même angle, même cadrage stratégique, exposition +0,15 et bloom 0,08.
- Référence 2F : `baseline-contacts.webm` (22,52 s, source vérifiée de la passe précédente), `baseline-camera.webm` (24,08 s, nouvelle prise de l’ancien binaire). La trace de ce dernier a conservé 23 résultats dont le trajet long ; la vidéo reste complète. Les tests finaux 2F.1 ont des traces complètes.
- 2F.1 : `after-a-contacts.webm` (25,83 s), `after-b-contacts.webm` (26,13 s), `after-b-camera.webm` (28,09 s). La galerie aligne les départs ou contacts de même index à partir des horloges réelles (`comparison-data.json`) ; vitesse native ×1, aucun changement de cadence des animations.
- **Partie réelle B** : `partie-b-coin.webm` (39,12 s), dés **4 + 6**, déplacement de la case 9 à la case 19 en franchissant le coin 10, dix contacts, lock puis retour stratégique. Aucun résultat forcé. Le début de partie précédent a permis l’achat réel de Pfizer-BioTech dans ce jeu local de test ; aucun achat ou montant n’a été modifié dans le code. La galerie emploie `partie-b-coin-short.webm`, qui coupe seulement l’attente finale après 14,6 s, sans coupe interne ni réencodage. La prise intégrale reste accessible.
- Audio réel Unity : 48 kHz stéréo, pic 1832/32768, aucun échantillon écrêté (`partie-b-coin-audio.wav`). Aucun microphone.
- Images natives extraites : contact A/B, dés réels, contact réel, arrivée et retour. Elles ne remplacent pas les vidéos.
- Remux avec index pour la navigation : **toutes les images décodées et l’audio sont identiques aux prises originales**, contrôlés par empreintes (`media-validation.json`, `capture-originals/`). Les originaux complets sont conservés. Le clip court conserve le début de la prise, sans réencodage : ses 199 images et horodatages correspondent exactement au préfixe de la prise intégrale ; les 14,5 premières secondes audio ont aussi une empreinte identique.

### Mesures de performance

Même Mac / Intel Iris Plus, navigateur intégré, Low, 430 × 830, WebGL Development. Fenêtres de repos de six secondes ; pas de mesure CPU/GPU séparée. Draw calls, batches et SetPass sont les maxima de la fenêtre pour scène + UI. Les sommes mémoire sont les compteurs Unity, pas toute la mémoire du navigateur/GPU.

| Fenêtre | Frames | Intervalle moyen | Maximum | Draw calls / batches / SetPass |
|---|---:|---:|---:|---:|
| 2F B avant compilation | 155 | 38,95 ms | 198 ms | 76 / 67 / 37 |
| 2F.1 A stabilisé | 112 | 54,19 ms | 133 ms | 76 / 67 / 37 |
| 2F.1 B même session | 112 | 54,21 ms | 112 ms | 76 / 67 / 37 |
| 2F B contrôle ultérieur | 96 | 62,91 ms | 344 ms | 76 / 67 / 37 |
| 2F.1 B sans capture — contrôle final | 129 | 46,63 ms | 109 ms | 76 / 67 / 37 |
| 2F.1 B avec capture + traces — contrôle final | 121 | 50,20 ms | 69 ms | 76 / 67 / 37 |

Le contrôle final constate **+3,57 ms (~7,7 %) avec capture et traces**, sur deux fenêtres uniques. Il ne sépare ni le coût de l’encodeur, ni celui des traces QA, ni le CPU du GPU. L’allocation Unity est d’environ 51,62 Mo sans capture / 51,65 Mo avec capture, réserves proches de 60 Mo.

Les écarts entre les sessions 2F et 2F.1 sont trop variables pour attribuer une régression au shader : l’ancien binaire est lui aussi passé de 38,95 à 62,91 ms sur ce poste. **Aucune hausse de draw calls observée** ; aucune garantie d’absence de régression GPU. La première fenêtre A non stabilisée (61,80 ms) et deux fenêtres mesurées dans le menu (57,47 / 80,39 ms) restent archivées et ne sont pas utilisées comme mesures du plateau. Une fenêtre B intermédiaire de 54,13 ms est aussi conservée. La mesure capturée finale a été déclenchée automatiquement par le recorder QA afin de garantir son exécution et son scénario.

Les médias de banc contiennent environ **10–10,5 images distinctes/s**, contre environ **14 images/s pour la partie réelle** ; c’est le débit des fichiers vidéo, pas une mesure interchangeable avec le temps de frame Unity. Le détail de la courbe et la fluidité du travelling restent limités par l’échantillonnage de ces captures. Aucun budget 60 fps, temps GPU ou coût isolé du post-processing n’est revendiqué.

### Journal des corrections de validation

Le premier essai de compilation a relevé une référence erronée à la liste d’ancres dans le nouveau script QA ; elle a été corrigée vers BoardManager. Un test de premier échantillon supposait ensuite une frame plus courte que l’attaque : il a été corrigé pour comparer la texture au temps réellement écoulé. Ces erreurs concernaient le banc, pas les règles ou le déplacement. Les prises/tests finaux utilisent le dernier binaire compilé ; les premières traces sont conservées dans `iteration-1/`.

## Bilan artistique

**B est recommandée pour la suite** : elle garde une matière au repos proche de A, puis rend le périmètre touché et le reflet voisin plus perceptibles pendant l’action. Le suivi fait du pion le centre de lecture de la zone utile, avec une anticipation courte. Le centre du plateau reste sombre et les autres cases gardent leur calme.

La courbe lumineuse est continue et son extinction plus longue ; la montée peut encore apparaître en un seul échantillon sur les cadences basses observées. La netteté fine des néons et la fluidité à 60 fps ne sont pas validées par ces vidéos. Les limites ne sont pas compensées par du bloom, un changement de zoom ou une modification du pion. La validation artistique appartient à l’examen des captures et des vidéos, pas au seul résultat des tests.

## Coûts et limites

Trois paramètres de matériau supplémentaires, aucune texture, lumière, transparence, passe, mesh ou variante de shader supplémentaire. Le calcul de contact réutilise ses fetchs ; le lobe local et la modulation de smoothness ajoutent quelques opérations scalaires, sans mesure exacte d’instructions GPU. Les tableaux de pulses sont réutilisés ; la texture transitoire n’est actualisée que pendant une impulsion, y compris son extinction. La caméra utilise des vecteurs valeur et le chemin déjà présent, sans nouvelle allocation par frame dans son calcul de suivi.

Les mesures disponibles sont des intervalles de frames de session et des compteurs Unity WebGL Development sur ce Mac, pas un temps GPU ni un budget validé sur téléphone. Les bancs avec trace JSON et enregistrement ont un coût propre. Aucun Android/iOS physique, aucune simulation de réseau multi-postes et aucune conclusion de performance Release. La courbe de 32 ms ne peut pas avoir plusieurs échantillons d’attaque dans une frame de 40 ms ; le défaut binaire est retiré du signal, la visibilité finale reste dépendante du rythme de rendu/capture.

Les contrôles QA sont exclus du Release par compilation conditionnelle. Le banc de caméra historique utilise un clone de présentation et des faits de dés déclarés QA ; il ne change pas le résultat des dés d’une partie réelle. Le banc monolithe utilise le prefab réel et des chemins déterministes, restaure ses pixels persistants et teste localement ApplyState. Ces preuves sont distinguées de la vidéo de partie réelle.

## Accès et prochaine action

Galerie locale : http://127.0.0.1:9005/evidence/review.html. Le lanceur `ouvrir-galerie.command` relance le serveur loopback si nécessaire. Le prototype final est dans `Builds/PerceptualPolish`.

**Arrêt après PASS 2F.1.** Prochaine action : validation artistique des vidéos natives avant toute autre modification.
