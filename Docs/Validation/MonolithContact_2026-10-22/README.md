# PASS 2E.1 — Pawn Visibility / Tile Contact / Arrival Sync

PASS 2E.1 livrée pour validation artistique. Arrêt après cette passe.

## Intégration

Le prefab et les roots de mouvement restent ceux de PASS 2E. La silhouette garde son emprise, sa hauteur, ses offsets multi-pions et ses colliders. La couronne porte toujours le code de 1 à 6 points et reçoit un liseré lumineux supplémentaire. La fente verticale de la face caméra passe de 0,07 à 0,26 unité normalisée. Corps un peu plus clair, couronne métallique légèrement plus large, anneau mieux exposé.

`BoardContactFeedback` ajoute une couche transitoire indépendante de la texture des catégories / highlights. Une texture RGBAHalf 64×2 (1 Kio de données brutes GPU, hors objet/driver et copie CPU) transporte couleur, enveloppe et style. Aucune géométrie supplémentaire sur le board, aucune lumière/particule. GraphiteSurface dessine un contour analytique avec couverture d’environ 1,1 pixel ; NeonState ajoute la réponse aux rainures existantes, notamment aux quatre coins. L’intérieur ne reçoit qu’une diffusion de bord sur 0,085 unité maximum. Le canal persistant n’est jamais écrit, sauvegardé ou restauré par le contact.

La fin visuelle de PawnMovement est exposée via `IsVisuallySettled`, placé juste avant le callback original. Le calcul du chemin, les durées et le rebond historique ne changent pas. Le child du monolithe se pose à l’ancre finale, attend cette fin, puis déclenche dans le même LateUpdate : lock, clic et contour final. BoardInteractionManager conserve ses déclencheurs et reste le seul auteur de la caméra. Aucun offset arbitraire à régler : l’alignement suit la même frame de fin, même quand la cadence varie.

## Paramètres retenus

| Paramètre | PASS 2E | PASS 2E.1 |
|---|---:|---:|
| Body RGB | .18 / .22 / .27 | .24 / .29 / .34 |
| Arêtes RGB | .30 / .36 / .41 | .42 / .48 / .54 |
| Metallic / smoothness corps | .48 / .44 | inchangé |
| Hauteur / largeur de base | .62 / .30 × largeur courte case | inchangé |
| Largeur fente normalisée | .07 | .26 |
| Couronne demi-largeur / profondeur | .30 / .275 | .335 / .305 |
| Anneau rayon extérieur / intérieur normalisé | .397 / .369 | .397 / .345 |
| Pawn emission base | .50 | .65 |
| Active emission multiplier | 1.65 | 1.85 |
| Moving multiplier | 1 | 1.25 |
| Idle emission facteur | .48 | .65 |
| Base ring intensity / active multiplier | commun | 1.15 / 1.20 |
| Top accent intensity | commun | .80 |
| Pawn arrival intensity / duration | .85 / .22 s | 1.15 / .22 s |
| Final click volume / pitch | .18 / .88 | .26 / .88 |
| Contact duration / intensity | absent | .12 s / 1.15 |
| Contact outline / inner glow | absent | 1 / .075 |
| Contact player-color blend | absent | .55 |
| Tile arrival duration / intensity | absent | .24 s / 1.65 |
| Reduced Motion contact intensity | absent | × .45 |

Réglages sur `CompactPawnVisual.tuning` et `BoardContactFeedback.tuning` ; les defaults s’appliquent au spawn runtime. La couverture de contour 1,1 pixel et la portée intérieure sont locales au shader. Le feedback démarre à pleine intensité puis décroît, pour conserver au moins une image de contact lorsque l’intervalle de rendu dépasse 120 ms. Aucun pulse de repos ; Reduced Motion supprime lift, impulsions du pion et expansion du ring, conserve la silhouette et réduit le feedback de case.

## Fichiers

Modifiés :
- Assets/Scripts/Visuals/CompactPawnVisual.cs
- Assets/Scripts/Visuals/PawnMovement.cs — horloge de présentation en lecture seule uniquement
- Assets/Shaders/GraphiteSurface.shader — composition du contact uniquement
- Assets/Shaders/NeonState.shader — composition du contact uniquement
- Assets/Scripts/Debug/MonolithValidationDriver.cs — banc opt-in Development/Editor
- Assets/Scripts/Editor/MonolithValidation.cs — contrôles et build

Créés runtime : Assets/Scripts/Visuals/BoardContactFeedback.cs et son .meta.

Dossier de preuves : audit, inventaires avant/après, diff limité à cette passe, log de build, scripts de capture/serveur/analyse, médias et galerie. Les outils QA ne modifient ni le template public ni les commandes de la partie.

## Tests et preuves

- **Build WebGL IL2CPP Development : réussi**, Unity 2022.3.62f3 / URP 14.0.12 ; log `build.log`, empreintes des quatre fichiers réellement référencés par `index.html` dans `build-sha256.json`.
- **612 assertions existantes passent** : économie 143, mécaniques 148, événements 17, UI assets 304.
- **94 contrôles Editor passent** : 40 indices et raycasts verticaux, quatre meshes fusionnés, six positions de cohabitation, emprise réelle du ring actif, callbacks vide/Skip une fois, ressources audio et plages de paramètres.
- **157 contrôles WebGL du banc passent**, zéro FAIL / erreur console dans la session. 40 positions natives visibles, 1/2/3/4/6 occupants, repos stable, émission désactivée, parcours 2/2/6/12/6/2 cases, coins 10/0/20/30, cadence volontairement ralentie (>100 ms entre frames), Reduced Motion, ApplyState local réel qui annule une ancienne présentation sans callback tardif.
- **30 contacts de case / 24 ticks / 6 locks** dans le banc. Chaque contact possède une image instrumentée active, correspond au même timestamp que le clic et préserve les 40 pixels de l’état persistant, y compris un accent de sélection simultané. Aucun contour résiduel après extinction.
- **Arrivée : écart lock ↔ début de hold = 0 ms sur l’horloge Unity**, sur cinq parcours normaux du banc et un déplacement de sept cases en partie réelle. Reduced Motion ne déclenche pas le travelling/hold, conformément à l’existant. La position réelle atteint toujours l’ancre avant le callback historique : seul le feedback de lock final est coordonné à la fin visuelle, sans accélérer le moteur ni le rebond.
- **Caméra inchangée** : source byte-identique, angles 40°/45° dans tous les échantillons enregistrés ; overview 18.2081718445 en portrait, 11.2809009552 en desktop, comme PASS 2E. Positions XZ du child = root + offset partagé dans tous les échantillons.
- **Sélection manuelle réelle** de Quantum AI Labs en portrait : inspecteur correct (`portrait-selection.jpg`). 40 cases/ancres/colliders confirmées par BOARD_QA. Économie initiale des quatre joueurs identique avant/après sur les vues comparées.
- **Scope** : 590 fichiers Assets/ProjectSettings préexistants byte-identiques ; six sources ciblées modifiées, deux nouveaux fichiers (.cs/.meta). Le vérificateur enlève uniquement l’horloge de lecture seule de PawnMovement et retrouve exactement le code d’origine, callbacks compris. Géométrie, caméra, règles, économie, UI, réseau, dés, scène et prefab inchangés. Unity a sérialisé une entrée vide `_ContactTex` dans les deux matériaux au build ; cette seule entrée a été retirée après vérification SHA-256 pour retrouver leurs octets initiaux.

### Médias réels

- `portrait-game.webm` : partie réelle 430×830, environ 25 s, dés puis sept cases et lock ; six passages + arrivée. Le clip conserve la fiche compacte et les commandes originales.
- `portrait-fixtures.webm` : banc visuel 430×830, environ 27,8 s, six parcours, quatre coins, cohabitation et Reduced Motion. Début = 40 poses rapides de vérification, pas une animation de jeu.
- `portrait-fixtures-audio.wav` : extraction non amplifiée de la piste WebAudio du même clip, 48 kHz stéréo, 27,78 s ; **30 groupes d’attaque pour 30 contacts**, pic −24,68 dBFS, aucun échantillon clippé. Proximité des attaques enregistrées avec les événements source : 12 à 55,4 ms ; cela inclut buffers/encodage et ne mesure pas la latence d’un haut-parleur physique.
- Avant/après natifs : `before-portrait-overview.jpg` / `after-portrait-overview.jpg` et `before-desktop-overview.jpg` / `after-desktop-overview.jpg` ; même scène de départ à quatre joueurs, même cadrage et économie initiale.
- `crossing.jpg` : frame réelle du passage (extraction vidéo vers 2,65 s) ; `arrival.jpg` : frame du lock (vers 4,95 s). La capture vidéo est à cadence variable : les instants du conteneur peuvent différer légèrement de l’horloge Unity.
- `specimen.jpg`, `specimen-context.jpg` : gros plan QA ×5 et son contexte complet. Aucune modification de la caméra ni de l’échelle de jeu.

Les conteneurs WebM ont été réindexés sans réencodage (`-c copy`) pour fournir durée finie et recherche temporelle. Les originaux sont conservés dans `capture-originals/` ; les empreintes des images décodées et de l’audio sont identiques avant/après (`media-validation.json`). Le serveur local gère les requêtes Range pour la lecture et la recherche.

La galerie `review.html` réunit les vidéos, audio, comparatifs et extraction d’images natives. Les données brutes restent dans `browser-validation.json`, l’analyse filtrée par marqueur de chaque captation dans `evidence-analysis.json`.

## Limites et validation artistique

Pas de téléphone physique ni de profilage GPU / mémoire / draw calls réalisé. La couronne ajoute un renderer au pion (quatre renderers / cinq sous-meshes au total par pion) ; les meshes restent partagés, un matériau de signature par pion, deux PropertyBlocks persistants. Le canal de contact ajoute un échantillonnage de texture aux shaders concernés et des uploads courts pendant les contacts. Ces coûts ne constituent pas une mesure de performance.

Une calibration transitoire incorrecte a été observée lors d’un redimensionnement pendant le chargement : viewport initial trop réduit, corrigé par rechargement aux dimensions fixes. Ce comportement préexistant du cadrage est documenté et non retouché dans cette passe. Les comparaisons sont prises après rechargement à format fixe.

La visibilité doit être jugée sur les images natives 430×830 ; le spécimen ×5 est uniquement un gros plan de matière déclaré. La sélection existante peut encore remplacer une teinte dans les anciens flux : cette passe assure l’indépendance du contact sans refondre la composition historique. La luminosité et le punch final restent soumis à validation artistique. Le contour se lit clairement sur les frames de contact ; la silhouette reste volontairement compacte et les points de code de la tête restent très petits en portrait. Les 120/240 ms sont des durées d’enveloppe : sous faible cadence le flash est quantifié au rendu, parfois sur une seule image, et ne constitue pas une garantie temporelle sur téléphone. Aucun PASS 2F autorisé/démarré.
