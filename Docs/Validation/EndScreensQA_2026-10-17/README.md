# Recette visuelle des écrans de fin — 17 octobre 2026

## Résultat

Les vues Development WebGL de Venture, faillite et victoire ont été atteintes après le démarrage de la partie locale. Les trois captures propres sont conservées ci-dessous. Le navigateur n’a signalé aucune erreur JavaScript durant cette recette.

| Écran | Observation | Capture |
|---|---|---|
| Venture #20 · Effondrement global | Carte et texte anglais s’affichent. Le voile violet est assez transparent pour laisser apparaître les badges de rendement et le texte du plateau derrière le titre et la description ; cela nuit à la lecture. Un seul exemple de carte a été testé. | [venture-chaos-20.png](venture-chaos-20.png) |
| Insolvency | Dette, cash, absence d’actifs liquidables et bouton Give Up s’affichent. Le scénario est forcé avec un joueur de départ sans propriété ; les lignes de liquidation ne sont donc pas couvertes par cette capture. | [bankruptcy.png](bankruptcy.png) |
| Hyper-Mogul | Gagnant, condition, classement et New Game s’affichent. La capture utilise la configuration locale par défaut de deux joueurs, pas six. | [victory.png](victory.png) |

État local avant les vues forcées : [local-game.png](local-game.png).

## Méthode et portée

- Unity 2022.3.62f3, build WebGL `BuildOptions.Development` compilée depuis le dépôt courant dans `/tmp/PolyRiskEndScreensQA_20261017`.
- Chrome for Testing sur Mac, viewport 960 × 720 CSS, backend WebGL via SwiftShader.
- Partie locale démarrée via Play Local, puis raccourcis Development F5 (Venture), F6 (Bankruptcy), F7 (Victory).
- Serveur local isolé sur `127.0.0.1:8951`. Aucun client n’a rejoint le lobby partagé et le serveur utilisateur 5080 n’a pas été sollicité.
- Les captures sont Development desktop : elles ne prouvent pas le rendu Release, le parcours réel de déclenchement, six joueurs, Safari/iPhone, tactile, mouvement réduit, performance ou régression visuelle.

## Suite requise

Garder la tâche ouverte : clarifier le fond de Venture avant toute modification visuelle, puis répéter le rendu en Release WebGL portrait/paysage, vérifier les états de liquidation avec actifs et tester les chemins réels d’apparition. L’intégration UI reste subordonnée au choix de direction visuelle d’Arthur.
