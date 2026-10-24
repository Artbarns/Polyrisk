# Pulse v2 — HUD compact, synthèse horizontale, cockpit de dés persistant

## Demande appliquée

- HUD : hauteur de référence réduite de 192 à 134 unités logiques (environ −30 %), disposition et corps des informations conservés. Le format paysage court utilise 94 unités.
- Fiche compacte : hauteur de 236 unités ; une seule ligne financière avec Loyer, Rendement, GO, Coûts, Net estimé. Risque non modélisé indiqué discrètement dans les métadonnées, sans inventer de statistique. Les valeurs proviennent de la projection GO existante.
- Lancer : cockpit relevé à 340 unités (304 en paysage court), avec dés en volume 2,5D dessinés dans UI Toolkit. Le panneau n’est plus masqué par le lancer. Le rebond, la suspension de 200 ms au coin, le tac et le total en arrière-plan sont intégrés au cockpit ; aucune fenêtre flottante de dés ne le remplace.
- Deux hauteurs automatiques explicites : lancer/déplacement, puis fiche de case. Les événements de tour ne réinitialisent plus le cran et le contenu plusieurs fois. Les interactions manuelles restent possibles ; le cran étendu conserve ses cartes et ses onglets.
- Le clic dans le cockpit intercepte désormais le raycast du plateau. L’action Ultimate garde sa largeur normale ; les boutons de jeu et leurs commandes restent ceux du projet.

## Motion

La référence est le « Rebond vertical » de `PolyRisk_Builder_Handoff_2026-10-17/previews/dice-approved.html`. `PulseDicePose` reprend les temps clés : premier dé stabilisé à 520 ms, second sur l’arête à 640 ms, suspension jusqu’à 840 ms, contact à 935 ms, amortissement jusqu’à 1 060 ms. Le second dé pivote autour d’un point au sol, et les trois faces visibles ont une disposition physiquement cohérente (aucune paire de faces opposées adjacentes).

Le rappel générique du résultat dans le bandeau est masqué pendant le mode dés afin de laisser le tac révéler le total. Les notifications importantes et le journal restent disponibles. Les faces au repos sont décoratives ; elles n’annoncent pas de résultat.

La signature du double conserve la compression locale, le total vert, DOUBLE, l’impulsion et le son. Un double pénalisant utilise l’ambre. Les animations réduites affichent immédiatement le résultat ; passer la présentation ne déclenche pas un nouveau lancer. Aucune probabilité ni règle de jeu modifiée.

Le jeu existant attend la confirmation du résultat avant le début du mouvement des dés ; la séquence de présentation ne génère aucun résultat. Le filet de sécurité de Planning après 60 s reste celui du jeu.

## Vérification

Unity 2022.3.62f3, WebGL, `BuildVerifiedWebGLPreview` : **601 assertions** (135 économie / intégration UI, 148 mécaniques, 14 événements, 304 assets UI). Le journal final est `unity-build.log`.

Les nouvelles assertions couvrent notamment la conservation du panneau pendant le lancer, les poses de suspension / contact / repos, l’appui fixe du coin, les faces de dés, la largeur de l’action Ultimate et l’interception des clics sur le cockpit.

Recette du WebGL réel : partie locale, lancer confirmé, rebond / bascule / résultat, déplacement, arrivée, fiche compacte, inspecteur étendu et formats 430×830, 320×720, 932×430 et 1280×720. Les captures proviennent du canvas Unity. Le GIF assemble les images capturées avec leur intervalle réel ; il ne mesure pas la fréquence de rendu du jeu.

Aucun test sur téléphone physique ni mesure fps revendiqué. Les délais d’attente et règles du moteur, l’économie et le réseau existants sont conservés.

## Fichiers de reprise

- `source-inventory.json` : les 9 sources ciblées et leurs empreintes ; `changes-from-pulse-v1.patch` : la différence avec l’intégration précédente.
- `baseline/` : copies avant intervention. Le reste du travail local a été conservé.
- `build-inventory.json` : identité du build dans `Builds/PulseStableV2`.
- `property-compact.jpg` / `property-320.jpg` : fiche à un étage.
- `dice-ready.jpg` : cockpit de lancer relevé.
- `dice-cockpit.gif` : capture du lancer réel, jusqu’à l’arrivée sur la case.
- `inspector-320.jpg` / `landscape-inspector.jpg` / `desktop-inspector.jpg` : contrôles du cran étendu.

Pour relancer l’aperçu depuis la racine du projet principal :

```sh
python3 Docs/Validation/PulseStable_2026-10-18/serve_webgl.py --build-root Builds/PulseStableV2 --port 8977
```

Le serveur reste local et gère les fichiers gzip Unity. Le précédent build est conservé dans `Builds/PulseNative`.

API utilisées : [Painter2D, Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/UIElements.Painter2D.html). Ne pas réintroduire le masquage global de `Panel_TileDetail` pendant les dés ni une fenêtre de lancer autonome.
