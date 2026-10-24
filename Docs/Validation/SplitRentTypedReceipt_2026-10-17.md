# Reçu typé de loyer détourné — 17 octobre 2026

## Vérification ajoutée

La régression appelle le `RulesEngine` courant sur une propriété réellement détournée par Cyber-sabotage. Elle confirme que le joueur visiteur paie exactement le loyer calculé, que le saboteur reçoit 25 % et le propriétaire 75 %, et que la durée du sabotage ne baisse pas à chaque arrivée.

Le test capture les deux `TransferConfirmed` émis par `TransactionManager` et vérifie pour chacun l’acteur, le bénéficiaire (`TargetId` et `Splits`), le montant signé, la tuile, le motif `Rent`, ainsi que la corrélation commune. Un `GameEventPresenter` connecté au bus réel coalesce ensuite ces deux faits en un seul reçu Dock avec le montant total et les deux parts. Aucun montant n’est recomputé dans l’interface; aucun code Player, règle, économie ou protocole n’a été modifié dans cette passe.

## Validation

- Unity 2022.3.62f3 : Économie **114/114**, Mécaniques **148/148**, Événements **14/14**, UI **262/262** — **538 assertions PASS**.
- Build WebGL Release (`BuildOptions.None`) : **PASS**, sortie isolée `/tmp/PolyRiskRentReceiptQA_Final_20261017`, processus code 0.
- `git diff --check` : **PASS**.

## Commande hôte Unity — ajout du 17 octobre

La fixture Editor câble désormais le `RulesEngine` au `TurnManager`, comme dans le chemin de production. Elle exécute `action.primary` par `OnlineGameController.ApplyCommand` avec un siège visiteur actif sur la propriété détournée. Résultat observé : commande acceptée, visiteur débité de **2 000**, hijacker crédité de **500**, propriétaire crédité de **1 500**, durée du sabotage inchangée à 2 tours. Les deux reçus `TransferConfirmed` portent les bénéficiaires et montants attendus, avec une corrélation commune. Les quatre groupes de régressions Editor passent, dont **148 assertions mécaniques**.

Cela ferme le trou de couverture sur la production du partage par la commande hôte Unity en Editor. Cela ne prouve pas encore le transport de cette action depuis un autre navigateur WebGL ni le rendu du reçu sur son écran; ce parcours multi-client reste ouvert.

## Transport serveur à trois sièges

Une recette API isolée a créé un lobby de trois sièges, fait rejoindre et confirmer les trois joueurs, démarré la partie, puis publié au snapshot deux événements `TransferConfirmed` correspondant au partage 25/75. Les deux sessions invitées ont relu le snapshot et conservé exactement les IDs d’événement et de corrélation, payeur, bénéficiaires, tuile 6, motif `Rent`, montants signés (−25/−75) et parts positives (25/75). La version du snapshot est passée à 1. Les trois sièges ont quitté le lobby ensuite; le stockage QA a été vérifié vide. Le serveur utilisé écoutait uniquement sur `127.0.0.1:5097`, avec son fichier de données dans `/tmp`; le service déjà présent sur `5080` n’a pas été modifié.

Cette passe prouve le cycle de lobby et la conservation du payload typé par le serveur entre deux sessions invitées. Elle ne simule pas le moteur Unity qui produit ces faits, le décodage `JsonUtility` côté client en ligne, ni le rendu du Dock en WebGL; ces éléments restent couverts séparément par les régressions Editor, et le parcours Unity WebGL avec loyer partagé reste à confirmer.

Le scénario a ensuite été intégré à `Server/PolyRisk.LobbyServer/six_player_reconnect_test.py` : après le contrôle initial des six sessions et des bandes de patrimoine, un second snapshot porte le débit total de 100 $ et les deux faits 25/75. Les six sessions vérifient les deux faits et leurs parts; le nouvel hôte les relit encore après redémarrage et passation de l’autorité. Le test reste un contrôle du protocole serveur et de la persistance, pas un substitut au parcours Unity WebGL en direct.

Le partage est couvert jusqu’à la commande hôte Unity en Editor, en plus du transport serveur des reçus et de leur coalescence locale. La recette réseau antérieure à trois clients couvrait une taxe de 600 $ et un passage de propriété, mais pas encore le partage de loyer WebGL — le parcours ci-dessous ferme ce trou.

## Recette Unity WebGL hôte + deux invités — 17 octobre

La build Release courante a été ouverte dans trois clients Chromium indépendants derrière HTTPS, avec l’API lobby Release isolée. L’hôte Unity a restauré une fixture où le visiteur (siège 2) arrive sur la tuile 8, propriété du siège 1, détournée par le siège 0. Le moteur a automatiquement résolu le loyer à l’arrivée et publié deux `TransferConfirmed` dans le même snapshot : **27,50 $** au siège 0 et **82,50 $** au siège 1. Le visiteur a été débité de **110 $**. Soldes constatés : 18 027,50 $, 18 082,50 $ et 17 890 $. Corrélation commune, acteur, bénéficiaires, raison Rent et tuile sont présents dans l’état reçu par les clients. Voir [`SplitRentWebGL_2026-10-17/`](SplitRentWebGL_2026-10-17/README.md) et les captures hôte/propriétaire/visiteur.

Cela vérifie le chemin moteur hôte Unity → snapshot lobby → deux clients WebGL invités et le rendu du Dock pour les écrans propriétaire et payeur. Le loyer est automatique à l’arrivée : aucune commande primaire ne l’a déclenché et la séquence de commande est restée à 0.

Écart visuel réel à retenir : les reçus affichés montrent la part locale de 82,50 $ arrondie à 83 $, sans expliquer dans ce message le total payé de 110 $ ni la part de 27,50 $ reçue par le saboteur. Le transport conserve pourtant les deux parts exactes. À traiter dans la conception du reçu partagé; aucun code de jeu n’a été modifié pendant cette recette.
