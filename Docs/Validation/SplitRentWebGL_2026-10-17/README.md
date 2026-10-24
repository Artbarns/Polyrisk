# Reçu de loyer partagé — WebGL à trois clients

## Résultat

Le parcours a été exécuté sur la build Unity WebGL Release actuelle, avec trois clients Chromium indépendants derrière une origine HTTPS et un serveur lobby Release isolé. Un état de recette plaçait le visiteur sur la tuile 8, propriété du siège 1 et détournée par le siège 0. À la restauration de l’état autoritaire par Unity, le loyer s’est résolu automatiquement.

Le snapshot publié contient deux `TransferConfirmed` pour le même acteur (visiteur 2), la même tuile 8 et la même corrélation : **27,50 $** vers le siège 0 et **82,50 $** vers le propriétaire siège 1. Le visiteur a payé **110 $** au total. Soldes après résolution : siège 0 **18 027,50 $**, siège 1 **18 082,50 $**, visiteur **17 890 $**. L’état partagé a atteint la version 2; la partie est revenue au tour du siège 0. Les données détaillées sont dans `rent-result.json`.

## Ce que prouvent les écrans

Les trois clients ont reçu le même état de jeu. Les captures propriétaire et visiteur affichent le reçu de loyer dans le Dock; l’hôte montre le nouvel état de tour et les soldes après résolution.

**Écart UX observé à garder dans le chantier visuel :** les messages observés montrent la part de 82,50 $ arrondie à 83 $, sans rendre visible dans ce reçu le total de 110 $ ni la part de 27,50 $ versée au saboteur. Le snapshot réseau contient bien les deux bénéficiaires et les montants exacts. À concevoir avant intégration : un reçu qui explique clairement le total payé et les deux destinations, avec un arrondi cohérent. Aucune correction visuelle/runtime n’a été appliquée.

Le loyer a été réglé automatiquement à l’arrivée pendant la restauration de la fixture; il n’a pas nécessité de commande primaire. La séquence de commande reste donc 0. Ce résultat valide le chemin réel moteur Unity hôte → état réseau → clients invités et l’affichage du Dock; il ne prétend pas que le bouton primaire a déclenché le paiement.
