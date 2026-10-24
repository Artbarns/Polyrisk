# Recette WebGL simultanée à six clients — 17 octobre 2026

## Résultat

PASS pour Create → Join ×5 → Ready ×6 → Start, depuis six instances indépendantes du même build WebGL Release courant, ouvertes dans six contextes Chrome distincts. Le lobby de QA isolé `YJNRZD` a affiché les six noms et l'état Ready, puis a démarré depuis l'interface de l'hôte.

Après Start, les six clients ont reçu des snapshots `started` (compteurs observés : Host 7, Guest2 9, Guest3 5, Guest4 6, Guest5 8, Guest6 6) et ont affiché le même plateau au tour 1. L'hôte avait Roll Dice et Pass disponibles; les invités affichaient l'attente du tour de Host. Aucun des six clients n'a remonté d'erreur console. Le snapshot enregistré indique six joueurs, `turnState=Planning`, `currentPlayerId=0`, `gameState.version=1`, `latestCommandSequence=0`.

## Environnement et portée

- Build WebGL Release déjà produit par la recette de clôture, servi sous HTTPS par un tunnel temporaire.
- Service lobby Release distinct avec persistance QA sous `/tmp/PolyRiskSixClientQA_20261017/lobbies.json`.
- Six contextes Chrome distincts sur le même Mac, viewport CSS 960×540 (réduit pour limiter la charge de rendu simultanée). Cela ne simule pas six appareils physiques.
- Cette recette prouve la création/jonction, la préparation, le démarrage et l'affichage convergent du premier état par six clients WebGL. Elle ne rejoue pas ici une commande de tour après le démarrage; les tests à six clients avec six premiers tours sont consignés dans `../SixIndependentUI_CurrentRelease_2026-10-16/README.md`, et le chemin de commande/synchronisation sur la build fraîche à deux clients dans `../GoalContinuation_2026-10-17.md`.

## Captures

- [Roster six sièges dans le lobby](six-seat-lobby.png)
- [Hôte au tour 1](host-started.png)
- [Guest2](guest2-started.png), [Guest3](guest3-started.png), [Guest4](guest4-started.png), [Guest5](guest5-started.png), [Guest6](guest6-started.png)

Les jetons temporaires de session ne sont pas inclus.
