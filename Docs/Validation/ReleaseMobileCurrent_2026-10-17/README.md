# Recette WebGL Release en emulation iPhone — 17 octobre 2026

## Résultat

Le Release courant `/tmp/PolyRiskFinalContinuation_20261017` charge en Chromium avec User-Agent iPhone, viewport 430×932/DPR3 et 932×430/DPR3. Le template limite le framebuffer à **860×1864** en portrait et **1864×860** en paysage. Création de partie locale et lancer par tap fonctionnent dans les deux orientations, sans erreur console ni réponse HTTP en échec.

Le rendu n’est pas prêt à clôturer en paysage : après le lancer, l’inspecteur de la case Opiod Paradise se superpose au Dock (titre, reçu et statistiques se chevauchent) et les badges ROI encombrent la rangée des joueurs/les cases. En portrait, le même parcours arrive sur Trump Tower et la fiche reste distincte du Dock.

| Cas | Capture |
|---|---|
| Portrait 430×932 après tap sur Play Local puis Roll Dice | [portrait-arrival.png](portrait-arrival.png) |
| Paysage 932×430 après tap sur Play Local puis Roll Dice | [landscape-arrival-overlap.png](landscape-arrival-overlap.png) |
| Menu paysage après tap sur la piste verticale, Play Local visible | [landscape-menu-scrolled.png](landscape-menu-scrolled.png) |

## Méthode et portée

- Build WebGL Release déjà compilé depuis le source courant le 17 octobre; il n’a pas été modifié pendant ce test.
- Chromium sur macOS, moteur ANGLE/Metal Intel Iris Plus, Playwright mobile emulation, User-Agent iPhone OS 18.6, `isMobile`, touch activé, DPR 3.
- Partie locale non réseau; tap simulé via Playwright. Vérification d’orientation, de framebuffer, de chargement, d’interaction et de collisions visibles.
- Aucune erreur console, erreur page ou réponse HTTP ≥400 observée.
- Cette recette ne prouve pas Safari iOS, comportement/clavier réel, cadence/chauffe d’un iPhone, ni compatibilité tactile physique. Le lobby partagé n’a pas été sollicité.

## Suite

Garder la recette globale ouverte. Traiter le conflit Dock/inspecteur et la densité ROI/roster dans un aperçu visuel choisi par Arthur avant toute intégration; puis refaire le même scénario en Release WebGL paysage et Safari/iPhone physique.
