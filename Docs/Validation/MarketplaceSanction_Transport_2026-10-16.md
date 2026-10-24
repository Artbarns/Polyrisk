# Transport d’une sanction Marketplace — 16 octobre 2026

Le smoke test du serveur lobby couvre désormais la commande `marketplace.sanction` à six sièges. Le quatrième participant soumet une sanction `audit` visant le sixième ; le rejeu de la même commande cliente retourne le même numéro de séquence et l’hôte ne reçoit qu’une commande, avec le type, l’émetteur et le payload attendus.

Vérifications exécutées sur des services et stockages QA temporaires :

- `dotnet build --configuration Release` : réussi, 0 avertissement et 0 erreur.
- `python3 smoke_test.py` : PASS, six joueurs, transmission de sanction et idempotence au rejeu.
- `python3 persistence_test.py` : PASS, file de commandes et état restaurés après redémarrage.
- `python3 six_player_reconnect_test.py` : PASS, six sessions restaurées, transfert d’hôte et remise de commandes.

Cette preuve valide l’authentification, la mise en file et la remise HTTP du message au client hôte. Le serveur ne tranche pas l’économie de la sanction : son application, ses préconditions et ses effets restent du ressort du contrôleur Unity. Le banc ne lance pas deux clients Unity/WebGL ; une recette de bout en bout avec une sanction déclenchée dans l’interface et son résultat visible chez l’autre joueur reste à faire.

## Revalidation pendant la continuation

Les trois bancs ont été relancés depuis la source de travail courante, chacun avec un serveur, un port et un stockage temporaires :

- Smoke six joueurs : `LOBBY + GAME PROTOCOL SMOKE PASS`, lobby version 13, snapshot version 1.
- Redémarrage/persistance : `LOBBY PERSISTENCE RESTART PASS`, credentials hachés, queue et snapshot restaurés.
- Six sièges : `SIX-PLAYER RECONNECT PASS`, sessions restaurées, récupération du snapshot, transfert d’hôte et remise de commande.

Les trois commandes ont terminé avec code 0. Le périmètre reste celui du protocole serveur ; cela ne certifie pas le rendu d’un client WebGL.
