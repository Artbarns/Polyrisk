# Parcours Release WebGL à six sièges — 16 octobre 2026

Release `/tmp/PolyRiskBoardBadgesFinal_20261016`, Unity 2022.3.62f3, servie localement en HTTP compressé sur le port 8801. Serveur lobby de recette isolé sur 5080 et données QA sous `/tmp/PolyRiskSixMixedCurrentRelease_20261016`. La prévisualisation utilisateur sur 5093 est restée intacte.

Un client Chrome/WebGL réel a créé le salon `53T4KW`. Cinq sièges distincts sont arrivés par l’API de QA et se sont mis Ready. L’interface WebGL a reçu et affiché `6/6 PLAYERS`, les cinq invités Ready et l’hôte Waiting. L’hôte a ensuite cliqué Ready, démarré la partie depuis l’interface puis lancé les dés. Le serveur a accepté `turn.roll` en séquence 1 et publié l’état de partie version 3 : `ResolveTile`, dés 3+3, joueur hôte case 16. Résultat : **PASS** pour création UI, roster live six sièges, Ready hôte, Start UI et première commande hôte.

Captures :

- [Roster six sièges, les six prêts](roster_6of6_ready.png)
- [Partie démarrée dans l’interface hôte](after_start.png)
- [Après le premier lancer](host_after_first_roll.png)

Portée : un seul client WebGL était rendu ; cinq sièges étaient des clients API QA. Les cinq autres écrans n’ont donc pas été vérifiés visuellement dans cette recette. La recette à deux clients UI indépendants du Release courant est dans `../LobbyUI_CurrentRelease_2026-10-16/README.md`. Ce contrôle ne remplace pas une partie humaine à six, Safari/iPhone physique ou le profilage sur appareil.
