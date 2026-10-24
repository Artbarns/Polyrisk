# PASS 2F — Graphite Satin / Graphite Reflective Neon

Livraison pour validation artistique. Périmètre limité à PASS 2F.

## Décisions

Deux finitions dans un même `GraphiteSurface`, choisies dans `GraphiteTheme.asset` : `surfaceTreatment`, `graphiteSatin`, `reflectiveNeon`. B est le défaut demandé pour la comparaison. `BoardSurfaceLighting` applique la finition au démarrage, puis actualise seulement le footprint du pion actif quand il change. Aucun nouveau contrôleur de caméra.

L’éclairage WebGL Low désactivait la main light : les horizontales recevaient surtout le remplissage ambiant. Low autorise maintenant la directional light existante sans activer ses ombres. A et B utilisent le même éclairage neutre : key 0,55, fill RGB 0,32 / 0,35 / 0,39. Le remplissage est plus retenu, pour éviter de laver le graphite. L’orientation de la lumière existante reste inchangée. Aucun point light ajouté, aucune probe/SSR.

Les arêtes reçoivent une réponse orientée ; deux softboxes analytiques neutres donnent une variation satinée sans texture ni détail aléatoire. Le centre reste sombre et calme. Les couleurs fonctionnelles des catégories, propriétaires et joueurs ne sont pas remplacées.

B renforce les réflexions à proximité des bandes, avec portée bornée à 0,14 unité de board et décroissance courte. Le contact garde son canal indépendant de l’état persistant, avec un drapeau d’arrivée dans un canal auparavant inutilisé de la texture existante. Les durées et déclencheurs sont inchangés : 120 ms par contact, 240 ms pour la case d’arrivée, 220 ms pour le lock du pion. Les coins gardent leurs anneaux existants ; le reflet sous le pion les accompagne aussi. Aucun reset/sauvegarde/restauration des couleurs persistantes n’est effectué par le polish.

Le reflet de socle suit le child actif réel, y compris son offset multi-pions, au lieu de peindre une lumière au centre théorique de la case. Il est fini dans l’espace, sans mesh ni transparence. Reduced Motion supprime son renforcement pulsé d’arrivée ; la position et le contact restent lisibles. Aucun changement du glide, de ses callbacks ou du Magnetic Click.

Le bloom reste à **0,08**, seuil 1,15, scatter 0,2 ; exposition **+0,15**, contraste 6, saturation −8, ACES. Identiques pour A et B. Le fond unlit neutre est inchangé. Aucun réglage PlayerSettings / réseau / UI modifié.

## Paramètres retenus

Voir `theme-after.json` pour les valeurs réellement sérialisées et les logs `GRAPHITE_QA` pour les valeurs réellement appliquées dans le navigateur. Les profils agissent sur la matière et la lumière uniquement ; les paramètres géométriques du thème restent intacts.


| Paramètre | A | B |
|---|---:|---:|
| Socle metallic | 0.36 | 0.42 |
| Cases metallic | 0.27 | 0.34 |
| Socle smoothness | 0.47 | 0.53 |
| Cases smoothness | 0.41 | 0.48 |
| Studio neutre | 0.46 | 0.56 |
| Arêtes spéculaires | 0.035 | 0.045 |
| Emission néon (gain) | 0.82 | 1.06 |
| Réflexion néon | 0.06 | 0.52 |
| Falloff / unité board | 0.025 | 0.045 |
| Portée maximale / unité board | 0.09 | 0.14 |
| Réflexion contact | 0.035 | 0.24 |
| Réflexion arrivée | 0.075 | 0.42 |
| Gain contour contact | 0.9 | 1.04 |
| Emission pion (gain) | 0.88 | 1.1 |
| Anneau pion (gain) | 0.9 | 1.12 |
| Accent haut (gain) | 0.9 | 1.02 |
| Reflet socle pion | 0.008 | 0.045 |

Couleurs communes des profils, multiplicateurs RGB : socle 0,80 / 0,83 / 0,87 ; cases 0,88 / 0,91 / 0,95 ; corps du pion 0,23 / 0,28 / 0,33. Metallic pion A/B : 0,42 / 0,48 ; smoothness : 0,40 / 0,46. Séparation de fond : 0,012 / 0,016. Le centre utilise un studio neutre limité à 0,045, distinct des surfaces de cases.

## Coûts et validation technique

- Toujours quatre meshes de board et le même nombre de passes : GraphiteForward / ShadowCaster / DepthOnly, NeonUnlit / ShadowCaster. Les passes compilées ne signifient pas qu’elles sont toutes exécutées en Low.
- Aucun shader keyword A/B : les deux finitions partagent le même programme, piloté par des uniforms. Le build GLES3 conserve cinq variantes forward Graphite, deux shadow, une depth ; Neon deux programmes au total.
- Aucun nouveau rendu transparent, aucune lumière dynamique par case, aucune réflexion en cubemap ou screen space. Une key existante est désormais évaluée par pixel en Low, sans ombre.
- Aucune nouvelle texture GPU. La texture persistante 64×1 RGBAHalf et la texture contact 64×2 sont réutilisées. Un canal libre marque l’arrivée.
- Calculs fragment supplémentaires : réponse d’arête orientée, softboxes analytiques, décroissance de contact et footprint fini du pion actif. Coût ALU exact et temps GPU non mesurés ; ne pas présenter cela comme gratuit.
- Jusqu’à deux vecteurs par matériau de surface sont envoyés lorsque le pied actif change (trois matériaux). Au repos, pas d’upload répété si les valeurs sont identiques. Les paramètres de finition et les lights sont mis à jour lors du choix de profil, pas à chaque frame.
- Les pions gardent leurs meshes partagés, un matériau de signature par joueur et les deux PropertyBlocks déjà présents. Pas de duplication de matériaux ajoutée dans cette passe.
- SRP Batcher : compatibilité code 0 (« OK ») pour les deux shaders, après compilation effective par `Material.SetPass(0)` avec Metal dans l’Editor. Un premier contrôle sans GPU renvoyait « Not initialized » ; il ne constituait pas un échec du shader. La compilation préalable suit le contrôle de [ShaderInspector Unity 2022.3](https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Editor/Mono/Inspector/ShaderInspector.cs).

## Sources modifiées / créées

Modifiées :
- `Assets/Scripts/Board/BoardVisualTheme.cs` — profils A/B centralisés, éclairage commun.
- `Assets/Settings/GraphiteFoundation/GraphiteTheme.asset` — valeurs sérialisées.
- `Assets/Shaders/GraphiteSurface.shader` — matière et réflexions locales.
- `Assets/Shaders/NeonState.shader` — gain de contact paramétrable.
- `Assets/Scripts/Visuals/CompactPawnVisual.cs` — finition de matériaux/émissions seulement.
- `Assets/Scripts/Visuals/BoardContactFeedback.cs` — drapeau final dans un canal existant seulement.
- `Assets/Settings/Low_PipelineAsset.asset` — activation d’une main light, sans changement d’ombres.

Créées :
- `Assets/Scripts/Visuals/BoardSurfaceLighting.cs` et `.meta`.
- `Assets/Scripts/Debug/SurfaceLightingValidationDriver.cs` et `.meta` — QA opt-in, Editor/Development uniquement.
- `Assets/Scripts/Editor/SurfaceLightingValidation.cs` et `.meta` — contrôles et build.
- Ce dossier de preuves : audit, inventaires, diff de passe, paramètres, logs, galerie, médias et outils locaux.

## Tests et preuves

- **Compilation Unity 2022.3.62f3 / URP 14.0.12 / WebGL IL2CPP Development réussie** (`build.log`, `build-sha256.json`).
- **612 assertions existantes + 101 contrôles Editor passent** (`editor-checks.txt`). Compatibilité SRP Batcher des deux shaders : OK après compilation Metal.
- **95 contrôles A/B passent dans le WebGL final** : 40 indices uniques, raycast de chaque collider dans chaque variante, quatre meshes identiques, positions des pions, caméra, préférence Reduced Motion, pixels persistants et snapshot économique complet inchangés (`verification-browser.json`).
- **157 contrôles du banc de mouvement passent** : quatre coins, 1 à 6 occupants, chemins 2/2/6/12/6/2 cases, frames lentes, Reduced Motion, absence de traîne, callback unique, annulation réelle par ApplyState. **30 contacts**, 24 ticks et 6 locks ; contact final et lock ont exactement le même temps dans les logs (`variant-b-fixtures-timeline.json`). Ce banc utilise le prefab et PawnMovement réels ; ses premiers placements rapides sont des poses QA.
- Sélection réelle par clic vérifiée sur desktop et en portrait : l’inspecteur mobile affiche **Quantum AI Labs, case 31, loyer 450, rendement 10,0** (`selection-mobile.jpg`, `selection-mobile-browser.json`). Les 40 ancres sont présentes ; la projection stratégique garde ses débords latéraux validés en PASS 2D, sans nouvelle garantie que chaque collider entier soit visible à tout instant.
- Six captures comparatives natives, baseline / A / B en 430 × 830 et 1200 × 900. Profil **Low** pour A/B principaux ; état initial identique, quatre joueurs à GO / 18 000, sélection 0, cockpit compact. Les logs confirment la projection identique à la baseline : taille orthographique 18,20817 en portrait, 11,28090 sur desktop, angles 40° / 45°. Exposition +0,15 et bloom 0,08 inchangés. Les anciennes captures desktop Medium sont conservées en complément, explicitement nommées.
- Vidéo **B partie réelle, 24,993 s, 430 × 830**, avec une piste audio Unity : repos, dés réels donnant 4, glide, trois contacts intermédiaires, lock sur la case 4, retour au calme, puis tours automatiques des autres joueurs. La notification de taxe est celle du jeu ; aucun résultat de dés ni état économique n’a été fabriqué pour la capture.
- Vidéo **B banc, 22,519 s, 430 × 830**, audio réel, 157 contrôles. Audio extrait en WAV : 48 kHz stéréo, pic 1951/32768, aucun échantillon écrêté. L’audio reste discret, conformément à la passe précédente.
- Les vidéos sont remuxées avec index de navigation, **sans réencodage** : les empreintes de chaque image décodée et de l’audio sont identiques aux originaux conservés (`capture-originals/`, `media-validation.json`). Les durées finies, le déplacement à 8,95 s et les 12 images de la galerie sont vérifiés dans le navigateur. Le serveur fournit les réponses HTTP Range nécessaires à la navigation.
- Gros plans : case non sélectionnée et sélection (desktop Medium), contact et arrivée (vidéo portrait Low), tous recadrages natifs. Les coordonnées et sources figurent dans `crop-provenance.json`. Monolithe repos/actif : spécimen QA ×5 déclaré, desktop Medium, géométrie de production inchangée. Les captures contexte sont conservées ; aucun gros plan ne remplace la lecture native mobile.
- `verify_scope.py` : **593 fichiers préexistants inchangés octet pour octet**, sept fichiers modifiés et six nouveaux fichiers source/meta dans cette passe. Scène, géométrie, caméra, règles, économie, réseau, UI, dés et PawnMovement intacts ; contrats de mouvement/audio du monolithe également identiques.
- `verify_evidence.py` : contrôle croisé des logs de comparaison, des contacts, du lock, des mesures et de la sélection. Résultat PASS (`evidence-validation.json`). Aucune erreur console dans les deux fenêtres vidéo.

### Mesures de session

WebGL Development sur ce Mac / Intel Iris Plus ; fenêtres de repos indépendantes de six secondes, sans enregistrement vidéo. Compteurs Unity de la scène et de l’UI réunies ; maxima draw calls / batches / SetPass sur chaque fenêtre.

| Profil / traitement | Frames | Durée | Intervalle moyen | Maximum | Draw calls | Batches | SetPass | Mémoire Unity allouée / réservée |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Low / A | 149 | 6,008 s | 40,48 ms | 157 ms | 76 | 67 | 37 | 51,66 / 60,00 Mo |
| Low / B | 159 | 6,013 s | 37,97 ms | 217 ms | 76 | 67 | 37 | 51,67 / 60,01 Mo |
| Medium / B | 149 | 6,049 s | 40,50 ms | 203 ms | 80 | 71 | 39 | 51,66 / 60,01 Mo |

Ces fenêtres uniques ne démontrent pas une amélioration de cadence de B par rapport à A. Elles montrent le même maximum de draw calls pour A/B et quatre supplémentaires en Medium avec ses ombres existantes. La mémoire donnée est l’allocation Unity, pas toute la mémoire du navigateur ni la mémoire GPU. Une première fenêtre B perturbée par l’initialisation/capture atteignait 169,72 ms en moyenne et 656 ms au maximum ; elle reste dans les logs bruts, séparée des fenêtres de repos.

L’enregistrement WebGL ajoute son propre coût : 365 images distinctes dans la vidéo de partie (~14,7/s sur la durée vidéo), 312 dans le banc (~14/s). Le recorder demandait 30 images/s mais le résultat n’atteint pas cette cadence. Certains contacts de 120 ms ne disposent que de quelques images ; leurs durées n’ont pas été prolongées pour la vidéo. Aucun temps GPU, coût isolé de bloom ou budget 60 fps mobile n’est validé.

## Limites et choix artistique

Le premier banc navigateur utilisait des rayons globaux verticaux à GO : ils rencontraient les colliders des pions, donnant deux échecs de test attendus (A/B, case 0). Le banc final interroge chaque BoxCollider directement sans déplacer/désactiver les pions ; la sélection réelle par clic est testée séparément. Les captures initiales d’itération et les logs de précontrôle sont conservés comme historique QA, pas comme validation finale.

Les comparatifs finaux sont des captures du navigateur à dimensions fixes. Une tentative de lecture directe du canvas WebGL produisait une image noire (framebuffer non conservé) : ces images ont été remplacées par des screenshots réels. Le masque temporaire des outils QA n’altère pas le canvas Unity.

Le profil Editor Very High et son éclairage hors Play ne sont pas la référence artistique WebGL Low. La finition s’applique en Play ; les preuves visuelles ci-jointes proviennent du navigateur. Le passage à Medium dans WebGL vérifie ce profil dans le navigateur ; il ne valide ni Android ni iOS. Aucun téléphone physique disponible, aucune mesure GPU annoncée. Aucun build Release ni profilage Release effectué dans cette passe ; la mesure Development ne le remplace pas.

La calibration transitoire en cas de redimensionnement pendant le chargement reste celle de PASS 2D ; les comparatifs sont rechargés à dimensions fixes. La géométrie/caméra validées ne sont pas retouchées pour améliorer artificiellement les captures.

**Recommandation : B**, pour ses bandes plus lisibles, ses reflets courts et son contact d’arrivée plus présent, avec A comme option sobre. Les captures montrent surtout un centre plus profond et une meilleure séparation des accents ; la différence A/B reste volontairement modérée à taille mobile. Le pion reste petit et les arêtes lointaines occupent peu de pixels dans le cadrage conservé. Les petits contours peuvent présenter de l’aliasing : cette passe ne garantit pas une largeur minimale en pixels. Ces limites ne sont pas masquées par du bloom, un recadrage ou un agrandissement géométrique. La validation artistique reste attendue. Aucun développement au-delà de PASS 2F n’est autorisé ou commencé.

## Accès aux preuves

Galerie locale : http://127.0.0.1:9004/evidence/review.html

Le serveur est lié uniquement au loopback. En cas d’arrêt/reboot, double-cliquer `ouvrir-galerie.command` relance le serveur et ouvre la galerie sur ce Mac. Le prototype WebGL reste dans `Builds/ReflectiveNeon`.
