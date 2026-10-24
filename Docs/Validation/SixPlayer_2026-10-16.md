# Revalidation du serveur six joueurs — 16 octobre 2026

La source courante du service lobby a été recompilée en Release, puis trois bancs isolés ont été rejoués avec leurs propres données et processus temporaires.

- `dotnet build --configuration Release` : 0 avertissement, 0 erreur.
- `python3 smoke_test.py` : PASS ; six sièges, commandes/état autoritaires, refus du 7e joueur et transfert d’hôte.
- `python3 persistence_test.py` : PASS ; identifiants de reprise hachés, file de commandes et état restaurés après redémarrage.
- `python3 six_player_reconnect_test.py` : PASS ; six sessions restaurées, état récupéré, transfert d’hôte et remise des commandes vérifiés.
- Rejeu live complémentaire du Release WebGL courant : un client WebGL visible a créé le salon, affiché six sièges après cinq arrivées API, montré les cinq sièges Ready, puis l’hôte a confirmé Ready et démarré. L’hôte a lancé `turn.roll` depuis le bouton ; le serveur a enregistré la séquence 1 et l’état de partie est passé à la version 3, `ResolveTile`, dés 3+3, pion hôte case 16. Capture et limites : `Docs/Validation/SixPlayer_WebGL_CurrentRelease_2026-10-16/README.md`.
- Cette recette du Release courant utilise un seul client WebGL rendu et cinq sièges QA API ; elle ne mesure pas la réplication visuelle chez cinq autres clients. La recette à deux interfaces indépendantes du même Release est consignée dans `LobbyUI_CurrentRelease_2026-10-16/README.md`.

Les scripts ont utilisé leurs serveurs et stockages QA temporaires. Cette validation couvre le service HTTP et sa persistance ; elle ne remplace pas une partie WebGL à six appareils ni les tests Safari/iPhone physiques.

## Rejeu de continuation

Les trois scripts ont été rejoués en parallèle sur des stockages/processus temporaires le 16 octobre, sans recompilation ni modification du service entre les contrôles : smoke six joueurs PASS (`EV9FRQ`, 6 joueurs, lobby v13, état v1), persistance après redémarrage PASS, reprise des six sessions avec transfert d’hôte et remise de commande PASS. `git diff --check` PASS. Les ports de QA sont arrêtés; le preview public local sur 5093 reste actif.


## Recette à six UIs Release

Le 16 octobre, une recette complémentaire a connecté six contextes WebGL indépendants au Player Release depuis les interfaces Create/Join/Ready. Chacun a joué un tour; 12 commandes ont été appliquées jusqu’au retour à l’hôte. Captures, séquences et limites : `Docs/Validation/SixIndependentUI_CurrentRelease_2026-10-16/README.md`. Le test humain sur six appareils reste ouvert.
