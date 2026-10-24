# Transit Express — choix facultatif (15 octobre 2026)

## Règle vérifiée

Le GDD prévoit qu'un joueur qui possède les quatre Hubs peut, après avoir atterri sur l'un d'eux, choisir le Hub suivant dans le sens horaire ou rester sur place. Le passage du Hub 35 au Hub 5 ne verse pas de salaire de départ additionnel.

## Changement

- Le tour s'arrête sur le Hub d'arrivée et présente « Use Transit Express » / « Stay on Hub ».
- La commande `transit.choose` est validée et diffusée par l'hôte en ligne ; le choix est sauvegardé dans l'état du tour pour permettre sa reprise.
- Les IA conservent leur comportement précédent et prennent le Transit quand il est disponible.
- Si un joueur humain ne répond pas en 60 secondes, il reste sur le Hub.

## Vérifications

- Suite économie Unity : 114 assertions PASS.
- Suite mécanique Unity : 117 assertions PASS, dont validation de la commande, condition quatre Hubs, destinations circulaires, choix rester/voyager et passage 35 → 5 sans salaire supplémentaire.
- Build Release du serveur lobby : 0 avertissement, 0 erreur.
- Smoke test lobby : PASS, commande acceptée, numérotée et publiée dans la file des commandes après le jet.
- Tests serveur de persistance et reconnexion à six sièges : PASS.
- Build Unity WebGL Release (`BuildOptions.None`) : le lancement initial a expiré après génération des ressources; le build Release suivant, incluant ces sources Transit, s'est terminé proprement avec `Build Finished, Result: Success` et `WEBGL PLAYER BUILD PASS` dans `/tmp/PolyRiskActivityWrap.log`. Le client Transit chargé depuis `/tmp/PolyRiskTransitChoice` avait déjà passé les deux parcours dans le navigateur.
- Vérification navigateur avec deux places en lobby local : après rechargement de l'hôte pendant l'attente, les deux actions sont revenues. « Use Transit Express » a publié la commande (séquence 1), amené le joueur de 35 à 5 et affiché l'activité; le solde est resté 18 000 $. « Stay on Hub » a publié la seconde commande (séquence 2), gardé le joueur en 5 et affiché « Player reste sur son Hub »; le solde est resté 18 000 $.

## Limites

Ces scénarios utilisent un état de jeu QA injecté dans un lobby local à deux places; ils ne remplacent pas la partie humaine qui atteint naturellement les quatre Hubs ni le test sur appareils iPhone. La recette réseau générale du mouvement restauré et la recette à six appareils restent ouvertes.
