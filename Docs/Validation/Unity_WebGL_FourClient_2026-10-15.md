# Recette WebGL multi-client — 15 octobre 2026

## Résultat observé

Le build WebGL Release `PolyRiskFamilyNotificationRelease` a été servi depuis
des origines locales distinctes avec un serveur de lobby local isolé. Quatre
clients Unity indépendants ont rejoint le même salon (`SRYBXU`) ; le panneau du
client hôte a affiché `4/6` et les quatre noms de siège. Deux clients ont été
contrôlés en 430 × 900 portrait, les deux autres en 1280 × 720.

Cela vérifie la connexion et le rendu simultanés de quatre clients WebGL sur le
même poste. Ce n’est pas une partie à quatre appareils physiques, une mesure de
performance mobile, ni une preuve de démarrage à six.

Depuis cette recette, l’utilisateur confirme avoir terminé une vraie partie
avec quatre amis et indique que panneaux et boutons ont bien répondu. Aucun
incident précis n’a été rapporté ; appareils, navigateurs et durée restent à
consigner. Cela valide le test humain à quatre, pas la recette à six clients.

## Limites de cette tentative

Le cinquième client a chargé le menu mais n’a pas rejoint : la tentative a
renvoyé « This name is already used in the lobby » malgré la saisie visible
d’un nom distinct. Le banc d’interaction a aussi produit des états Ready qui
n’étaient pas assez cohérents pour valider le démarrage. Cette tentative est
donc enregistrée comme inconclusive pour le parcours à six ; elle ne suffit pas
à attribuer le refus à un défaut du jeu.

Le sixième siège, le démarrage effectif, un tour complet et la reprise restent
à vérifier depuis six clients rendus. Les tests HTTP/persistance à six restent
PASS selon `SixPlayer_2026-10-15.md`.

Une nouvelle tentative locale a d’abord révélé un défaut du banc : les
préférences conservées sur les ports 8950–8955 envoyaient `/api/lobbies` aux
serveurs statiques eux-mêmes (404), sans atteindre l’API sur 5080. Sur six
origines vierges (8970–8975), un client Unity a ensuite créé un lobby par son
interface et l’API isolée a persisté le siège hôte, avec une capacité de six.
Cela confirme la création depuis WebGL quand l’adresse est propre, mais pas
l’arrivée des autres clients, Ready, le démarrage, un tour ou la reprise à six.
