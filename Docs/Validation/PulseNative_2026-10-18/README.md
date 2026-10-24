# Pulse — intégration native Unity / WebGL

Les fichiers runtime ont été appliqués au projet principal `PolyRisk`, après travail dans une copie isolée. Le rendu utilise UI Toolkit natif Unity 2022.3.62f3. Aucun site, iframe ou HTML superposé au jeu n’a été ajouté.

## Ce qui a été corrigé

- Échelle du HUD/cockpit en pixels CSS WebGL, indépendamment de la résolution physique du canvas ; safe areas conservées.
- Palette Pulse, hiérarchie propriété / loyer / rendement / risque, un seul bloc de synthèse GO, famille réelle x/n et portefeuille par cartes.
- Deux crans avec transition native de hauteur de 260 ms, interruption possible et état immédiat si les animations sont réduites. Les cartes ne sont reconstruites que lorsque le détail est affiché.
- Styles communs des boutons restaurés ; marges natives des contrôles neutralisées dans le cockpit ; actions contenues dans la largeur disponible.
- Panneau compact en portrait, détails côte à côte en paysage court, cockpit aligné à droite sur ordinateur. Les onglets restent défilables lorsque l’espace est limité.
- Estimation GO en lecture seule : salaire, dividendes de famille, entretien et taxes du modèle existant. Le règlement réel utilise la même projection ; les effets sont consommés uniquement au règlement. Le net est estimatif à état constant, hors événements futurs et loyers.
- Inspection d’une autre case pendant une résolution : retour explicite à la case d’arrivée avant l’action de jeu.
- Priorité du menu et des modales conservée au-dessus de la couche de jeu. Contrôle plein écran discret et accessible dans le template WebGL.

Les dés et les règles déjà présents dans le snapshot du builder ont été conservés. L’achat glissé, les célébrations de famille et les autres suggestions optionnelles n’ont pas été activés.

## Vérification

Build WebGL de production via `EconomyBatchValidation.BuildVerifiedWebGLPreview` dans Unity 2022.3.62f3. Les suites exécutent 120 assertions économie, 148 mécaniques, 14 événements et 303 UI : **585 assertions**. Le journal final est `unity-build.log`.

Contrôle du WebGL réel dans le navigateur : menu / partie locale, inspection depuis le plateau, deux crans, cartes de famille, onglets Portefeuille et Flux GO, lancer / arrivée / achat et mise à jour du capital. Formats contrôlés : 320×720, 430×830, 932×430 et 1280×720. Les captures livrées proviennent du canvas Unity, pas du mockup HTML.

Pas de mesure de fréquence d’images ni de test sur téléphone physique ou de partie réseau à deux clients dans cette passe.

## Fichiers et reprise builder

- `source-inventory.json` : les 15 fichiers source ciblés et leurs SHA-256. Les fichiers du projet principal sont identiques aux sources compilées.
- `changes-from-builder.patch` : différence avec le snapshot du builder au début de cette intervention, sans mélanger son travail antérieur.
- `baseline/` : copies avant intervention pour comparaison, sans réinitialisation du dépôt.
- `build-inventory.json` : identité du build livré dans `Builds/PulseNative` du projet principal.
- `before-portrait-compact.jpg` : défaut constaté avant correction ; `reference-compact.jpg` : référence HTML approuvée. Les captures `portrait-*`, `landscape-*` et `desktop-*` montrent le jeu livré.

Pour relancer l’aperçu, depuis la racine du projet principal :

```sh
python3 Docs/Validation/PulseNative_2026-10-18/serve_webgl.py --build-root Builds/PulseNative --port 8977
```

Ouvrir http://127.0.0.1:8977/. Le serveur applique le bon encodage aux fichiers gzip Unity et reste limité à la boucle locale.

Le builder doit reprendre ces fichiers natifs et comparer le même cran aux mêmes dimensions. Ne pas refaire une conversion du HTML ni écraser les autres changements locaux du projet.

## Preuves de données réelles

Achat contrôlé d’Opioid Paradise : capital stabilisé de 18 000 $ à 16 900 $, progression Pharma de 0/3 à 1/3 et carte Pharma visible dans le portefeuille. La carte de portefeuille ouvre bien cette famille. Après l’événement Bull Market (+20 % de Sgo), la synthèse GO passe de +2 000 $ à +2 400 $. Les autres cases restent inspectables et l’action de retour à la case courante fonctionne.

Le jeu existant possède un auto-lancer de sécurité après 60 secondes de Planning. Pendant une inspection longue, ce délai peut donc déclencher un changement de case ; il ne s’agit pas d’un clic sur la carte inspectée.

Captures natives du build livré :

- [Compact / achat](portrait-achat.jpg)
- [Inspecteur par cartes](portrait-inspecteur.jpg)
- [Portefeuille après achat](portrait-portefeuille.jpg)
- [Propriété acquise / 1 sur 3](portrait-propriete-acquise.jpg)
- [Flux GO](portrait-flux-go.jpg)
- [Petit écran 320 × 720](portrait-320.jpg)
- [Paysage 932 × 430](landscape-inspecteur.jpg)
- [Bureau 1280 × 720](desktop-inspecteur.jpg)
