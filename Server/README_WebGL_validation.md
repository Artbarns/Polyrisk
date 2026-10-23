# Recette WebGL locale à plusieurs clients

Le build séparé de reconnexion est produit avec `EconomyBatchValidation.BuildWebGL`
et la variable `POLYRISK_WEBGL_OUTPUT`. Sans cette variable, le builder garde le
chemin historique. Ne pas remplacer le build mobile pendant un essai humain.

Après réussite du build, servir ses fichiers sur des origines distinctes :

```bash
python3 Server/serve_webgl.py \
  --directory /Users/arthurbarreau/.codex/validation/PolyRiskWebGLReconnect \
  --ports 8770 8771 8772 8773 8774 8775
```

Le serveur prend en charge les fichiers Unity `.gz` et `.br` avec leurs en-têtes
Content-Encoding et le type application/wasm. Chaque port conserve les préférences
et les identifiants de reconnexion du joueur séparément. Le serveur lie uniquement
127.0.0.1 par défaut. L’API de recette actuelle est http://127.0.0.1:5091 ; renseigner
cette adresse dans le menu de chaque client. L’instance historique sur 5080 n’est
pas celle validée par le dernier smoke test.

Commencer par 8770/8771 : création, code de partie, ready, lancement, premier tour,
rechargement de l’hôte et reprise. Puis utiliser les six ports pour vérifier les
pouvoirs, OPA, Marketplace, Venture, liquidation et fin de partie. Relever les
montants/positions/versions dans l’état serveur avant et après reconnexion, et les
comparer au jeu affiché. Vérifier aussi les commandes répétées ou arrivées en retard.

Statut : serveur statique préparé et syntaxe Python vérifiée. Chargement du build
compressé et tests navigateurs encore non confirmés à la création de ce document.


Pour figer l’état de référence avant/après un rechargement, utiliser le code de
partie visible dans le menu :

```bash
python3 Server/capture_game_state.py \
  --storage /Users/arthurbarreau/.codex/validation/PolyRiskNetwork/lobbies.json \
  --code CODE_PARTIE \
  --output /Users/arthurbarreau/.codex/validation/PolyRiskNetwork/before_reload.json
```

L’export sélectionne uniquement le snapshot de jeu public, sa version et la
séquence acquittée. Il n’exporte ni identifiants de reconnexion, ni hashes, ni queue.
Le script a été exécuté sur la partie du smoke test (version 1) ; cela valide
l’extraction, pas les règles Unity ni une reconnexion navigateur.

## Adresse unique pour la recette mobile en ligne

Le serveur statique peut désormais relayer les POST `/api/lobbies` vers une API
locale. Cela permet un seul tunnel HTTPS pour le build et le multijoueur :

```bash
python3 Server/serve_webgl.py \
  --directory CHEMIN_DU_BUILD_VERIFIE \
  --ports 8800 \
  --api-upstream http://127.0.0.1:5091
```

Le relais n’accepte qu’une origine HTTP locale comme destination, les routes du
protocole lobby, et au plus 384 KiB de corps (même plafond que Kestrel). L’API
garde ses validations de session, commandes et limites de débit. Sur une page
WebGL publique, le client prend par défaut l’origine de la page comme serveur ;
un choix explicite enregistré reste prioritaire. Les pages localhost gardent
l’adresse de développement historique, à remplacer par 5091 dans leur menu QA.

Recette de passerelle locale : smoke protocole six joueurs PASS via 8800 ; page
Unity servie, route hors protocole 404 et corps trop volumineux 413 confirmés.
Le nouveau défaut WebGL public reste à vérifier dans le navigateur et sur iPhone.


### Scénarios locaux reproductibles

`qa_fixture.py --base http://127.0.0.1:5095 --session-file CHEMIN_SESSION_QA
--fixture CHEMIN_FIXTURE --capture CHEMIN_PREUVE` installe un scénario dans
un salon local existant. Le fichier session est fourni par le relais QA
explicite ; ne jamais ajouter ce fichier aux preuves ou au dépôt. Le script
associe les sièges aux joueurs du salon, vérifie leur nombre, utilise la
version courante et exporte le snapshot sans credentials ni lobbyPlayerId.
Sans --fixture, --capture est un contrôle de lecture. Après injection,
recharger le navigateur autorité afin de reprendre le moteur sur cet état.
Les effets doivent ensuite être déclenchés par l’UI et constatés côté serveur.
Une fixture injectée seule ne prouve pas une mécanique exécutée.
