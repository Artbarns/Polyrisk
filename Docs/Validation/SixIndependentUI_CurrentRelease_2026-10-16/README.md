# Recette WebGL à six clients indépendants — 16 octobre 2026

## Résultat

PASS sur le Player WebGL Release courant (`BuildOptions.None`, Unity 2022.3.62f3), servi localement avec un lobby serveur Release et un stockage QA isolés. Six contextes Chrome distincts, chacun avec son stockage/session Unity, ont créé ou rejoint le même lobby par l’interface du jeu. Les six noms et états Ready apparaissaient sur le roster hôte; l’hôte a démarré la partie depuis l’UI.

Les six joueurs ont ensuite joué leur premier tour chacun depuis leur propre interface :

- Player hôte : lancer, résolution de case et Pass.
- Guest2 : lancer, inspecteur Pfizer-BioTech et Pass.
- Guest3 : lancer, inspecteur Trump Tower et Pass.
- Guest4 : lancer, inspecteur Fentanyl Labs et Pass.
- Guest5 : lancer et paiement automatique de 600 $ sur Taxe Douanière.
- Guest6 : lancer, pioche Venture #08 « Don de sang de stagiaire », puis confirmation UI; l’état lui attribue la carte Sortie d’Audit Fiscal.

L’état final revient à Player pour le tour suivant. Le serveur confirme **6/6 joueurs Ready**, partie démarrée, **12 commandes appliquées sur 12**, snapshot version **13**, phase `Planning`, positions Player=5, Guest2=9, Guest3=3, Guest4=11, Guest5=4, Guest6=7. Les activités et l’état détaillé, expurgés des identifiants et jetons privés, sont dans [`snapshot_summary.json`](snapshot_summary.json).

## Captures

- [Roster hôte à 6/6 Ready](host_lobby_6_ready.png)
- État final du plateau vu par l’hôte : [Player](host_final_state.png), [Guest2](guest2_final_state.png), [Guest3](guest3_final_state.png), [Guest4](guest4_final_state.png), [Guest5](guest5_final_state.png), [Guest6](guest6_final_state.png)
- [Carte Venture #08 affichée dans l’UI Guest6 avant confirmation](guest6_venture_card.png)

Les six captures finales montrent les mêmes positions, compteurs et tour actif. Les invités voient les commandes d’attente tandis que l’UI du joueur actif montre Roll Dice.

## Portée

La recette vérifie le parcours UI Create/Join/Ready/Start, la connexion simultanée de six instances WebGL indépendantes, les commandes de tour émises par chaque profil, la résolution de case, la carte Venture et la convergence visuelle du plateau. Les six clients tournaient dans des contextes séparés d’un seul Chrome sur le même Mac; ce n’est pas une recette sur six appareils/navigateurs physiques et cela ne remplace pas le test humain à six. Le profilage thermique/cadence Safari sur iPhone 14 Pro Max et le test humain à six restent ouverts.

Les essais antérieurs qui n’avaient que des sièges API restent documentés comme tels dans `LobbyRoster_2026-10-16.md`; cette recette UI plus récente les complète.
