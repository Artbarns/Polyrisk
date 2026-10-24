# Validation protocole réseau à six — 15 octobre 2026

## Résultat

Les tests d’intégration du service de salon passent sur un banc local. Un salon
à six sièges est créé et lancé après Ready des six sessions ; un septième joueur
et les démarrages prématurés sont refusés. Le test de protocole confirme ensuite
la livraison d’une commande, sa consommation par l’hôte, la publication d’un
snapshot autoritaire et sa lecture par un autre siège.

Les tests de reprise passent aussi : les six sessions survivent au redémarrage,
l’état et les commandes en attente sont restaurés, puis le transfert d’hôte
permet de poursuivre la livraison des commandes. Résultats des scripts :

- `LOBBY + GAME PROTOCOL SMOKE PASS: players=6, lobby_version=13, state_version=1`
- `LOBBY PERSISTENCE RESTART PASS: credentials hashed, queue restored, state restored`
- `SIX-PLAYER RECONNECT PASS: six sessions restored, state recovered, host handoff and command delivery verified`

## Rejeu de régression du service — 15 octobre 2026

Après le test humain à quatre, la suite serveur a été relancée sur la source
courante : compilation .NET Release réussie avec zéro avertissement et zéro
erreur, puis smoke test de protocole, test de persistance après redémarrage et
test de reprise des six sessions/transfert d’hôte tous PASS. Le test à six a
utilisé un port local isolé différent du port de la préversion partagée ; aucun
salon ni processus public n’a été modifié.

Le banc était local et séparé de la préversion partageable. Aucun changement de
règles ni de données du jeu n’a été effectué.

### Revalidation fraîche après la partie entre amis

Le 15 octobre, le projet serveur courant a été recompilé en Release avec
`dotnet build --no-restore` : **0 avertissement, 0 erreur**. Les trois scripts
ont ensuite été rejoués contre un processus isolé sur port loopback aléatoire et
des fichiers de données temporaires : smoke six sièges/protocole PASS,
persistance/reprise après redémarrage PASS, et reconnexion des six sessions avec
transfert d’hôte PASS. Aucun appel n’a été envoyé au service de préversion sur
5093 ; les processus QA et leurs données étaient séparés.

Une tentative de charger six instances Unity WebGL dans le même navigateur n’a
pas été retenue comme preuve : la capture d’accessibilité a expiré et un canvas
était noir. La cause n’a pas été isolée (capacité du poste/banc ou application) ;
la recette visuelle est à refaire avec six appareils ou un banc navigateur stable.

### Harnais de smoke autonome

Le smoke test protocole demandait auparavant un service déjà lancé sur le port
5080, contrairement aux deux autres scripts qui créent leur serveur QA. Il
démarre maintenant, sans argument, le serveur Release sur un port loopback libre
et un stockage temporaire, puis arrête le processus à la fin. Une URL passée en
argument continue de cibler explicitement un service déjà lancé. Après le
changement, le build Release a **0 avertissement / 0 erreur** et les trois
scénarios ont été rejoués en parallèle : protocole six sièges PASS, persistance
après redémarrage PASS, reprise des six sessions et transfert d’hôte PASS ;
`py_compile` des scripts PASS. Aucun code Unity, règle, donnée économique ni
service de préversion n’a été modifié.

## Limites et suite

Ces contrôles exercent le serveur HTTP et sa persistance avec six sessions
distinctes ; ils ne valident pas à eux seuls les six rendus Unity/WebGL. Une
recette distincte a maintenant confirmé le lobby à six et la reprise après trois
503 dans six clients desktop Chrome/WebGL rendus sur un même Mac (`Docs/Validation/NetworkResilience_2026-10-15.md`). Les tests sur six appareils
physiques, Safari/iPhone et la cadence thermique restent ouverts.

L’utilisateur confirme avoir terminé une partie avec ses amis à quatre. Les
détails sur durée, appareils/navigateurs et incidents éventuels restent à
recueillir pour ajouter les corrections humaines à la todo.

### Rejeu isolé supplémentaire — 15 octobre 2026

Nouvelle exécution parallèle des trois scripts de recette, chacun sur un
serveur et un stockage temporaires : le protocole confirme six sièges (`KAXSU6`,
version du salon 13, snapshot initial v1), la reprise après redémarrage restaure
identifiants hachés, file et état, et le test six joueurs restaure les six
sessions puis valide le transfert d’hôte et la livraison d’une commande. Les
trois processus ont terminé avec le code 0. Cette passe confirme de nouveau la
couche serveur ; elle ne simule pas six personnes ni six navigateurs Unity.

### Revalidation depuis la source actuelle — 15 octobre 2026

Comme les derniers fichiers serveur étaient plus récents que la DLL Release, le
projet a été recompilé avec `dotnet build --configuration Release` : **0
avertissement, 0 erreur**. Puis `smoke_test.py`, `persistence_test.py` et
`six_player_reconnect_test.py` ont été lancés en parallèle, chacun avec son
serveur, son port et ses données temporaires. Les trois se terminent avec le
code 0 : le protocole atteint six sièges et lance la partie, la persistance
restaure authentification/queue/snapshot, et la reprise restaure les six
sessions puis vérifie le transfert d’hôte et la livraison de commande.

Résultats de cette passe : `LOBBY + GAME PROTOCOL SMOKE PASS: code=WXMUWP,
players=6, lobby_version=13, state_version=1`, `LOBBY PERSISTENCE RESTART PASS`
et `SIX-PLAYER RECONNECT PASS`. Aucun appel n’a touché au service de préversion.
Cette preuve valide le serveur actuel, pas six appareils humains ni Safari/iOS.

### Revalidation du serveur courant — 16 octobre 2026

Le projet serveur a été recompilé en Release avant de relancer en parallèle les
trois scripts, chacun sur un port loopback libre et avec ses propres données
temporaires. `dotnet build --no-restore` termine avec **0 avertissement et 0
erreur**. Résultats : smoke six sièges/protocole PASS (`EKXWJ3`, lobby v13,
snapshot v1), persistance et reprise après redémarrage PASS, reconnexion des six
sessions et transfert d’hôte PASS. Les trois processus terminent avec le code 0.

Cette passe revalide la couche serveur et le protocole après la recette à quatre.
Elle ne valide ni six clients Unity visibles en même temps ni une partie humaine
à six. Aucune requête n’a touché au service de préversion.

### Rejeu ciblé après confirmation du test à quatre — 15 octobre 2026

Les trois scripts ont de nouveau été exécutés en parallèle depuis le DLL Release
présent, avec un port libre et un stockage temporaire par processus. Résultats :
`LOBBY + GAME PROTOCOL SMOKE PASS: code=B6AWVJ, players=6, lobby_version=13,
state_version=1`, `LOBBY PERSISTENCE RESTART PASS: credentials hashed, queue
restored, state restored` et `SIX-PLAYER RECONNECT PASS: six sessions restored,
state recovered, host handoff and command delivery verified`. Les trois
processus terminent avec le code 0. Le DLL Release est plus récent que les
sources serveur ; cette passe revalide l’artefact Release disponible. Aucun
client WebGL ni endpoint public n’a été utilisé pendant ces tests.
