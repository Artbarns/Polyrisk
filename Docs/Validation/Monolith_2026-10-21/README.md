# PolyRisk — PASS 2E

21 octobre 2026 · Unity 2022.3.62f3 · URP 14.0.12 · WebGL IL2CPP Development.

Prototype limité aux pions Compact Tech Monolith, Precision Glide et Magnetic Click / Lock. Arrêt après PASS 2E pour validation artistique.

## Preuves

[Galerie et son WebGL](http://127.0.0.1:9002/evidence/review.html) · [Prototype](http://127.0.0.1:9002/Monolith/index.html). Les médias sont conservés dans ce dossier ; le serveur ne publie que sur 127.0.0.1.

Les clips « partie » utilisent le moteur réel. Le banc visuel instancie le prefab de production et utilise le vrai PawnMovement, sur des parcours déterministes sans actions économiques. Les manipulations ApplyState sont locales ; recharger après ce banc avant de jouer normalement. Le spécimen agrandi est explicitement une exposition QA de la même géométrie et des mêmes matériaux, pas la taille réelle de jeu.

## Architecture et décisions

[Audit avant modification](AUDIT.md). PlayerVisualManager monte un seul CompactPawnVisual sur chaque prefab existant. Le MeshRenderer primitif est désactivé ; sa racine, son CapsuleCollider et PawnMovement restent présents. Le renderer désactivé conserve l'enveloppe de référence lue par le cadrage de PASS 2D. Aucun nouveau contrôleur caméra.

CompactPawnVisual ne modifie ni la racine, ni les waypoints, ni le modèle joueur. Il observe la position XZ réellement atteinte à chaque snap de PawnMovement. L'enfant neutralise visuellement l'arc et la déformation de l'ancien saut, présente un glide très bas et une orientation fixe vers la caméra. Les durées, la progression, les snaps, les pauses historiques et les callbacks sont conservés dans PawnMovement, byte pour byte. Le mouvement n'est pas reconstruit.

Le franchissement final produit uniquement le lock, sans ajouter un tick concurrent. L'impulsion de 220 ms commence à la destination réelle et se superpose partiellement à la pause caméra : le callback conserve le rebond historique de 80 ms sur la racine invisible. Aucun délai nouveau après ce callback, aucune conséquence économique en attente des FX.

Le monolithe utilise une coque facettée, une base octogonale, une fente colorée et un anneau fin. Six codes de sommet complètent les six couleurs originales. Les codes de sommet sont destinés à l'inspection rapprochée ; leur lisibilité à quelques pixels en portrait doit être jugée avec les médias. Aucun éclairage ponctuel, particule, texture ni shader nouveau.

Les états sont combinables : activité, déplacement, arrivée et cohabitation sont indépendants. Au repos, aucun flottement / rotation / pulse répété. Une impulsion unique accompagne l'activation. Reduced Motion supprime les pulses décoratifs, l'élévation et les transitions d'offset ; le son n'est jamais nécessaire à la compréhension. GlowEnabled permet de vérifier la silhouette sans émission.

Les offsets restent sur l'enfant. Les occupants sont reconnus d'après leurs racines effectivement coïncidentes, et non les positions logiques de destination déjà avancées par le moteur. Six slots déterministes par identifiant couvrent le roster actuel de six joueurs ; un occupant seul revient au centre en douceur. La restauration réapplique immédiatement la disposition sans contact sonore. Sur PresentationStateApplied, PlayerVisualManager arrête également un ancien coroutine de mouvement visuel, afin qu'il ne déplace plus un pion après la pose répliquée ni ne déclenche un callback tardif. Aucun message ou protocole réseau modifié.

Audio : réutilisation des assets existants `Resources/PolyRiskAudio/contact_light.wav` et `contact_tac.wav`, mono 48 kHz / 100 ms. Ce ne sont pas de nouveaux sons définitifs. Une AudioSource persistante par pion, volume contrôlé, trois variations déterministes proches. Le lock est plus grave. Pas de timer de clic indépendant, pas de file de rattrapage ; garde sonore de 65 ms et remplacement de la voix précédente. L'ordre des contacts reste piloté par les snaps réels. Les pistes enregistrées utilisent la sortie WebAudio du jeu, sans microphone.

## Paramètres

| Paramètre | Valeur initiale |
|---|---|
| Corps | RGB 0,18 / 0,22 / 0,27 ; metallic 0,48 ; smoothness 0,44 |
| Arêtes / base | RGB 0,30 / 0,36 / 0,41 ; metallic 0,62 ; smoothness 0,55 |
| Hauteur / base | 0,62 / 0,30 de la petite largeur du collider de case standard |
| Renfort actif | Échelle visuelle ×1,06 ; racine historique inchangée |
| Émission | 0,50 ; actif ×1,65 ; inactif ×0,48 |
| Glide | 0,04 de la petite largeur ; aucun hop visible |
| Step pulse | 90 ms ; intensité 0,55 |
| Lock | 220 ms ; intensité 0,85 ; anneau jusqu'à ×1,15 |
| Impulsion de tour | 180 ms, une fois |
| Espacement partagé | Rayon 0,32 de la petite largeur ; transition 160 ms |
| FX | fxIntensity, GlowEnabled indépendants |
| Audio | Passage 0,22 ; final 0,18 sur un asset deux fois plus fort ; pitch ±0,018 / final 0,88 ; AudioEnabled indépendant |

La hauteur initiale de 50 % était trop discrète dans le rendu portrait. Elle est portée à 62 % après inspection réelle : environ 0,57 unité monde pour la case standard de 0,92 unité, tout en restant nettement plus petite que la primitive précédente. Le diamètre de base est normalisé par sa circonférence octogonale pour garder six occupants et leur anneau dans le footprint réel.

Les paramètres sont regroupés dans le petit Tuning sérialisable de CompactPawnVisual. Le composant est monté au runtime : l’inspecteur Play permet les réglages de prototype ; les défauts persistants restent dans ce script, sans nouvel asset de thème. Les dimensions sont évaluées par rapport au vrai collider standard dans la scène. Les matériaux graphite communs sont partagés et créés une fois ; le premier Tuning initialise ces matériaux communs. Les réglages de dimensions / FX / audio sont lus au runtime. Aucun ScriptableObject ou package supplémentaire.

## Fichiers

Modifié : `Assets/Scripts/Visuals/PlayerVisualManager.cs` — montage du visuel, activité, arrêt de l'ancienne animation après restauration.

Créés, chacun avec son `.meta` :

- `Assets/Scripts/Visuals/CompactPawnVisual.cs` — géométrie, matériaux, états, compensation du hop, contacts et offsets.
- `Assets/Scripts/Debug/MonolithValidationDriver.cs` — banc opt-in Development et traces ; aucun scénario en Release.
- `Assets/Scripts/Editor/MonolithValidation.cs` — régressions, indices / raycasts, slots et build.

Les autres créations sont les outils / preuves de ce dossier et le build. Aucun changement du prefab, de la scène, du moteur de mouvement, de la caméra, des règles, de l'économie, des permissions, du réseau, du board, des matériaux / shaders graphite ou de l'UI. La comparaison se fait avec l'inventaire au début de cette passe, car de nombreux changements locaux existaient déjà : [périmètre](scope-validation.json), [diff limité à PASS 2E](pass-2e-only.diff).

## Validation et limites

Les 612 assertions de régression existantes passent (économie 143, mécanique 148, événements 17, UI 304), ainsi que 89 contrôles Editor ciblés. Le build WebGL final réussit. Le banc navigateur passe **108 contrôles, aucun FAIL**, dont le volume réellement rendu aux 40 ancres, les cohabitations 1 / 2 / 3 / 4 / 6, les callbacks uniques à destination, les trajets de 2 / 6 / 12 cases et les quatre coins, les frames lentes, Reduced Motion, une téléportation de pose et le vrai ApplyState appelé localement. Le test de restauration vérifie aussi l'arrêt du coroutine du pion de production après la pose répliquée. Aucun transport réseau n'est exercé.

[Journal](browser-validation.json), [analyse reproductible](evidence-analysis.json), [contrôles Editor](editor-checks.txt), [build](build.log), [décodage médias](media-validation.json). Les contacts et événements caméra conservés après la fin d'une prise sont exclus des statistiques de cette vidéo.

| Preuve | Résultat |
|---|---|
| [Portrait — banc visuel avec audio](portrait-fixtures.webm) | 430 × 830 ; 108 PASS ; 23 ticks et 5 locks, 28 contacts |
| [Portrait — partie réelle](portrait-game.webm) | 430 × 830 ; premier parcours de 9 cases, puis début du tour bleu ; clic / inspection de l'index 31 et cockpit déployé avant le lancer |
| [Desktop — partie réelle](desktop-game.webm) | 1200 × 900 ; parcours réel de 6 cases, arrivée et retour caméra |
| [Audio seul, extrait sans normalisation](portrait-fixtures-audio.wav) | 21,78 s ; stéréo 48 kHz ; pic −28,04 dBFS ; aucun échantillon saturé |
| [Quatre occupants](portrait-fixtures-frame-4400.jpg) | Image native du banc ; voir également les deux vues de référence au départ GO |
| [Sans émission](portrait-fixtures-frame-6150.jpg) | Pion testé sans glow ; géométrie conservée |
| [Glide et suivi](portrait-fixtures-frame-13000.jpg) | Image native pendant le parcours de douze cases |
| [Gros plan](specimen.jpg), [contexte](specimen-context.jpg) | Même rendu, exposition QA ×5 déclarée ; caméra non recadrée |
| Avant / après | [Portrait avant](before-portrait-overview.jpg), [après](after-portrait-overview.jpg) ; [desktop avant](before-desktop-overview.jpg), [après](after-desktop-overview.jpg) |

Les trois vidéos décodent avec une vraie piste Opus non silencieuse. Le WAV provient de cette prise WebAudio via FFmpeg 7.1, sans amplification / remplacement / ajout de clics. Les 28 groupes d'attaque forte correspondent aux 28 contacts émis ; distance temporelle à l'événement journalisé de 8,5 à 35,4 ms dans le conteneur enregistré. Cette corrélation n'est pas une mesure de latence acoustique d'un téléphone. Le navigateur décode aussi le WAV et confirme des échantillons non nuls.

Les 810 échantillons caméra / visuel conservent 40° / 45° et la relation exacte XZ visuel = racine + offset. Les ortho de repos restent **18,208172** en portrait et **11,280901** en desktop, identiques à PASS 2D. Le glide observé plafonne à 0,0428 unité, sol de présentation compris ; Reduced Motion à 0,006. L'injection de frames lentes obtient un intervalle minimal effectivement observé de 139 ms après son premier frame d'amorçage ; six franchissements, six contacts au plus et un seul lock, sans rattrapage sonore.

**Limite de synchronisation conservée :** le lock se déclenche au vrai dernier waypoint. La pause caméra commence sur le callback historique de PawnMovement, après sa séquence finale invisible. Dans ce banc, son début se situe 176–316 ms après le lock ; dans les parties enregistrées, 272 / 275 ms. L'impulsion de 220 ms peut donc finir avant la pause caméra sous capture. Le calage strict de ces deux horloges n'est pas déclaré validé : les contrats et les déclencheurs de PASS 2D sont conservés. Aucun second effet ou délai métier n'est ajouté pour masquer cet écart.

À valider artistiquement : présence réelle des pions en portrait, lisibilité des joueurs inactifs / codes de sommet à cette échelle, volume et fatigue des clics après plusieurs dizaines de tours, raccord lock / pause caméra. Les codes de sommet restent petits dans la vue globale ; le gros plan ne prouve pas leur lisibilité mobile.

Structure de rendu : coque commune à deux sous-meshes, six petits meshes de signature, anneau commun ; trois renderers actifs / quatre passes matériau par pion. Deux matériaux communs et un matériau de signature par pion. Pas de MaterialPropertyBlock : les matériaux de signature sont peu nombreux et réutilisés, avec mises à jour d'émission uniquement pendant une impulsion / changement d'état. C'est compatible avec l'usage URP/Lit / SRP Batcher choisi ; ce décompte n'est pas une mesure de draw calls. Pas d'allocations de géométrie / source / matériau à chaque case.

Limites à conserver dans la livraison : viewport mobile sur navigateur desktop, aucun téléphone physique / iOS / Android, pas de mesure GPU / mémoire / cadence Release sur appareil. Le banc avec ralentissement artificiel est une injection de frames lentes, pas une mesure de performance normale. ApplyState local ne valide pas le transport multiclient / une reconnexion réelle. Les noms de sous-états décrivent la présentation, jamais une autorité métier. Les flashes de 90 ms peuvent être partiellement ou entièrement sautés à très faible cadence ; le son et la pose restent liés aux franchissements réels. Le moteur actuel expose au moins un frame à chaque waypoint ; aucun multi-franchissement dans le même frame n’a été inventé en réécrivant ce moteur. Les WebM peuvent avoir une durée initialement Infinity selon le lecteur ; dimensions et images doivent être contrôlées au décodage.

La première itération révélait une convergence résiduelle des offsets et un contrôle de frames lentes incluant le premier frame non injecté. Les corrections sont dans la racine finale ; `iteration-1/` conserve le diagnostic initial. `iteration-2/` conserve une prise audio silencieuse due au tee de capture après déconnexion / reconnexion des gains Unity. Le recorder final réattache ce tee à chaque connexion et garde une horloge audio silencieuse ; cette plomberie QA ne fait pas partie du template jouable. Aucun audio artificiel audible n’est ajouté.

## Reproduction et arrêt

Définir POLYRISK_WEBGL_OUTPUT sur le chemin absolu `Builds/Monolith`, lancer Unity 2022.3.62f3 en batch avec `-executeMethod PolyRisk.Editor.MonolithValidation.RunAndBuild`, puis `python3 Docs/Validation/Monolith_2026-10-21/make_capture.py`. Serveur : `python3 Docs/Validation/Monolith_2026-10-21/serve_webgl.py --build-root "$PWD/Builds" --port 9002`.

Prochaine action : juger la silhouette à l'échelle réelle, la cadence / le volume des contacts et le lock dans les vidéos. **Arrêt à PASS 2E ; aucun PASS 2F engagé.**
