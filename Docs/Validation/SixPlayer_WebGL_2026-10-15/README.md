# Recette UI WebGL à six sièges — 15 octobre 2026

## Parcours vérifié

Sur le build Unity WebGL Release `BuildOptions.None` de 12 MiB, un hôte a créé
le salon dans l'interface. Deux invités distincts ont rejoint par l'interface
WebGL et sont passés Ready. Trois sièges de QA ont rejoint et passé Ready via
l'API. Le roster hôte affichait 6/6, les six sièges Ready et le bouton Start
activé. L'hôte a démarré depuis le bouton, réponse HTTP 200, puis a lancé le dé
sur son premier tour.

Les deux interfaces invitées rendues ont affiché Venture et l'action « WAIT ».
Le snapshot serveur confirme le tour 1, six joueurs, le joueur 0 actif, la
position 7, la phase `ResolveTile`, la carte Venture #07, la version 3 et une
seule séquence de commande appliquée. L'activité serveur indique un seul lancer
(3 + 4), le déplacement vers Venture puis la pioche #07.

Captures :

- `lobby_6_ready.png` — roster compact hôte avec six sièges Ready ;
- `host_venture.png` — modale Venture ouverte après le lancer hôte ;
- `guest_2_wait.png` et `guest_3_wait.png` — deux clients invités sur la même
  tuile, en attente du joueur actif.

## Portée et limite

Ce premier run mixte prouve six sièges en ligne avec trois interfaces
Unity/WebGL rendues (hôte + deux invités) et trois sièges protocole/QA. Il
vérifie le roster six, Ready/Start par l’hôte, un premier tour et la réplication
sur deux interfaces invitées. La recette complémentaire ci-dessous vérifie
ensuite les six interfaces indépendantes simultanées. Aucun de ces runs ne
mesure six appareils distincts ni Safari/iPhone physique ; la partie humaine à
quatre amis reste une preuve distincte.

## Recette à six interfaces indépendantes — 15 octobre 2026

Une seconde recette a repris le build Release sur le même serveur local isolé,
avec six profils Chrome persistants et six stockages client séparés. L’hôte a
créé le salon dans l’interface ; les cinq invités l’ont rejoint avec leurs
interfaces, puis les six sièges ont passé Ready. L’hôte a démarré depuis son
bouton ; les six clients WebGL ont ensuite été ouverts simultanément. L’hôte a
lancé les dés depuis le plateau.

Résultat serveur après le lancer : six joueurs Ready, partie démarrée, tour 1,
état `ResolveTile`, joueur 0 à la case 8, dés 3 + 5, séquence de commande 1.
L’activité autoritaire indique l’arrivée sur « Opioid Paradise ». Les cinq
invités rendus affichent la même propriété et « WAIT » pendant le tour de
l’hôte. Les six pages n’ont remonté aucune erreur JavaScript.

Captures ajoutées : `lobby_6_clients_ready.png`, `six_clients_host_after_roll.png`
et `six_clients_guest_2_after_roll.png` à `six_clients_guest_6_after_roll.png`.
Ce run établit le parcours complet à six clients WebGL indépendants sur un
même Mac, dans des profils navigateur distincts. Il ne mesure pas les
performances de six appareils physiques et ne remplace pas un test humain à
six personnes ; le test humain confirmé à ce jour reste celui à quatre amis.
