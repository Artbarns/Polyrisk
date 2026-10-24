# Refus de cible invalide et protégée en ligne — 15 octobre 2026

## Contrôle exécuté

Sur le build Unity WebGL Release local `/tmp/PolyRiskShortLandscapeFixV2`, un
client rendu a créé le salon de recette `TKP3N6`. Le lobby isolé contenait
deux sièges, l’hôte Unity/WebGL et un invité QA. Avant de lancer la scène, le
snapshot de recette plaçait l’hôte en `Planning`, avec 100 % d’énergie, la
famille Aerospace complète et les deux propriétés Armement ; l’invité possédait
la case 1. Le plateau ne permet pas ce saut quantique vers la case 1.

L’API QA a ensuite soumis `ultimate.use` avec `family: Aerospace` et
`targetTileIndex: 1`. Le client Unity hôte a reçu la commande, l’a rejetée dans
`ApplyCommand`, puis a publié son état pour acquitter la séquence. Le snapshot
est passé de la version 1/séquence 0 à la version 2/séquence 1. Après traitement,
l’hôte avait toujours 100 % d’énergie, la position 0 et 18 000 $ ; les
propriétés restaient inchangées et aucune activité de pouvoir n’avait été
ajoutée. Cela confirme que cette commande invalide ne modifie pas la partie et
ne bloque pas la file réseau.

## Portée

Le build Unity/WebGL et son autorité réseau ont été exécutés dans le navigateur.
La commande invalide a été injectée via l’API QA avec la session hôte ; le siège
invité était présent dans le lobby, mais aucun second client WebGL rendu n’a
émis cette requête. Ce contrôle valide le trajet API → polling WebGL →
validation Unity → publication/acquittement. Il ne valide pas l’immunité
Aerospace contre Armement entre deux clients rendus, ni l’état visuel chez
l’invité. Les assertions Unity batch couvrent séparément le rejet de la cible
protégée. Le contrôle réseau plus fort de l’immunité Aerospace est consigné
ci-dessous ; sa confirmation visuelle sur un second client WebGL reste ouverte.

Aucun fichier de logique, règle, économie ou scène n’a été modifié pour cette
recette. Le serveur, le relais, le stockage et le lobby étaient temporaires et
isolés du service de préversion.

## Contrôle additionnel — immunité Aerospace contre Armement

Un second scénario a été joué sur un build WebGL Release courant avec une
autorité Unity/WebGL rendue et un siège invité distinct au protocole. Le lobby
QA `T62VDW` a démarré normalement. Avant de recharger l’autorité, la fixture
plaçait le joueur 0 en `Planning` avec 100 % d’énergie et les deux propriétés
Armement (cases 38 et 39), tandis que le joueur 1 possédait les deux propriétés
Aerospace (cases 33 et 36).

Le siège hôte a reçu `ultimate.use` pour Armement ciblant NASA Moonbase (case
33). L’autorité Unity a journalisé « Cyber-Sabotage bloqué — Aerospace
immunité », puis acquitté exactement la séquence 1 et publié l’état. Le
snapshot est passé de la version 2/séquence 0 à la version 3/séquence 1 ; les
deux soldes sont restés à 18 000 $, l’énergie de l’attaquant à 100, la case 33
est restée au joueur 1 sans pirate ni durée de sabotage, et aucune activité de
pouvoir n’a été ajoutée. Aucune exception JavaScript n’a été relevée.

Preuves expurgées : [snapshot autoritaire après refus](Online_Ultimate_Refusals_2026-10-15/state_after_immunity_refusal.json) et [capture de l’autorité WebGL](Online_Ultimate_Refusals_2026-10-15/armement_refused_authority.png).

Cette étape valide le refus côté autorité après transmission réseau. Le second
siège n’était pas une instance WebGL rendue : la confirmation de l’état visuel
chez l’invité, et la reprise d’effets temporaires après reconnexion, restent à
tester. La recette a utilisé un stockage et un serveur loopback temporaires ;
aucun code de jeu ni service de préversion n’a été modifié.

## Rejeu avec deux clients WebGL rendus — 15 octobre 2026

Le scénario a ensuite été rejoué avec l’hôte et l’invité dans deux profils
persistants et deux processus Chrome distincts. Les deux clients ont rejoint le
même lobby (`B29WNJ`), sont passés Ready, puis l’hôte a démarré la partie depuis
son interface. La fixture QA donnait l’Armement au joueur 0 et Aerospace au
joueur 1. La commande Armement vers NASA Moonbase a été reçue puis refusée par
l’autorité.

Le snapshot est passé de la version 2/séquence 0 à la version 3/séquence 1.
Le joueur 0 conserve ses 100 % d’énergie et les deux soldes restent à 18 000 $ ;
NASA Moonbase reste au joueur 1, sans pirate ni durée de sabotage. Aucune
activité de pouvoir n’a été publiée. L’invité WebGL rendu a interrogé le
serveur et reçu lui aussi la version 3/séquence 1 avec la case toujours à lui.
Les deux pages n’ont remonté aucune exception JavaScript. L’hôte journalise
« Cyber-Sabotage bloqué — Aerospace immunité sur NASA Moonbase ».

Preuves : [résultat contrôlé](Online_Ultimate_Refusals_2026-10-15/two_client_result.json), [snapshot partagé](Online_Ultimate_Refusals_2026-10-15/state_after_two_client_refusal.json), [vue hôte](Online_Ultimate_Refusals_2026-10-15/armement_refusal_two_client_host.png) et [vue invitée](Online_Ultimate_Refusals_2026-10-15/armement_refusal_guest.png).

Cela valide le trajet entre deux clients WebGL rendus pour le refus et la
réception du snapshot partagé. Ce n’est pas un test humain à deux joueurs ; le
refus a été injecté via l’API QA et l’invité reçoit l’état inchangé, sans
activité visible de refus. Les reprises d’effets actifs autres qu’Armement et
Pharma et les parcours humains restent à recetter.
