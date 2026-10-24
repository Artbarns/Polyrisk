# Lisibilité des noms du lobby — 15 octobre 2026

## Résultat

Le lobby WebGL reste lisible à six sièges avec cinq noms de 18 caractères (`LongPlayerName0001` à `LongPlayerName0005`) et un hôte de même longueur. Les six entrées sont visibles dans la disposition en deux colonnes; aucun nom ni état « WAITING » n'est tronqué, et les actions Ready/Start restent visibles.

## Environnement

- Build WebGL Release courant, servi localement sur le loopback.
- Viewport Playwright fixé à 1280 × 720 CSS; `window.innerWidth/innerHeight` et le canvas mesurent 1280 × 720, DPR 1.
- Lobby local à six places; l'hôte UI a créé le salon et cinq invités QA y ont été ajoutés par API.

## Périmètre

Contrôle visuel du panneau de lobby à cette résolution seulement. Les cinq invités ont été créés via l'API de recette; cela ne valide pas six clients ou appareils indépendants, ni le retour sur Safari/iPhone.
