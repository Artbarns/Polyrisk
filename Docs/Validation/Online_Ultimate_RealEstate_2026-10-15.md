# Pouvoir Immobilier en multijoueur WebGL — 15 octobre 2026

Deux clients WebGL Release indépendants ont rejoint le lobby QA local
`KNYB9A`. Avant l’initialisation de l’autorité, la fixture attribuait les trois
propriétés Immobilier au joueur invité, avec 100 points d’énergie, 18 000 $ et
la phase `Planning`. Depuis l’interface invitée, le joueur a sélectionné
Immobilier dans le sélecteur d’ultime.

Le serveur a accepté `ultimate.use` (séquence 1) et publié l’état en version 2.
L’activité autoritaire est « active Rappel de dividendes » ; l’énergie de
l’invité passe de 100 à 0 et son solde de 18 000 $ à 18 135 $, sans transfert
de propriété. L’hôte conserve 18 000 $ et les deux clients restent sur le
plateau en ligne. Le résultat correspond au calcul attendu : 25 % du loyer
complet de monopole (270 $), soit 135 $.

L’interface invitée affiche 18,14 k$ après l’action et l’hôte reste à 18 k$.
La bannière d’activité temporaire n’a pas été capturée. Test mené sur un
stockage et un serveur isolés en loopback ; aucune règle, donnée économique ni
source Player n’a été modifiée.
