# Réseau et livraison publique — 23 octobre 2026

## Infrastructure existante

Le jeu utilise `LobbySession` et `OnlineGameController` Unity, un serveur ASP.NET Core **net10.0**,
et des requêtes HTTP JSON authentifiées. L'hôte Unity reste l'autorité des règles. Le serveur gère
les salons privés de 2 à 6 joueurs, les credentials de reconnexion, l'ordre/idempotence des commandes,
les versions de snapshots et leur persistance atomique JSON. Aucun WebSocket : le HTTPS suffit
pour le transport public. Les calculs économiques restent dans le moteur Unity existant.

Cadences réelles : commandes hôte 0,25 s, snapshots répliques 1 s après le premier snapshot
(0,25 s pendant son attente initiale), appartenance au salon 1,5 s pendant la partie. Six clients
au repos représentent environ 780 requêtes/minute derrière une seule IP de proxy. L'estimation
initiale de 1680/minute ne concernait que l'attente du tout premier snapshot ; elle a été corrigée.

## Hébergement et accès

L'inventaire du dépôt ne trouve ni remote Git, ni workflow de déploiement, ni service hébergé durable
configuré. Les liens publics précédents sont des Cloudflare Quick Tunnels, dépendants de ce Mac ;
ils ne satisfont pas la demande de livraison indépendante. Le connecteur Vercel répond mais renvoie
zéro équipe et zéro projet. Aucun credential d'hébergement trouvé dans les variables d'environnement,
les chemins de configuration courants ou les métadonnées Keychain ciblées. Aucune valeur secrète
n'a été affichée.

Le package `Server/Deployment` prépare un service unique .NET 10 + export WebGL, avec volume
persistant `/data`, un seul replica et origine HTTPS commune. Il réutilise les règles/protocole
actuels. Il reste à fournir un accès au projet/service d'hébergement autorisé et à déployer le
bundle Release réellement vérifié. **Aucun déploiement public n'est revendiqué par cet audit.**

## Changements ciblés

- `Program.cs` : configuration du port hébergeur, activation optionnelle du service statique et
  limite globale configurable. Toutes les routes, DTO, validations, stockage et événements sous
  `app.MapGet` sont identiques au fichier avant intervention (`source-validation.json`).
- `WebGLHosting.cs` : `PORT` ou URL explicite ; `POLYRISK_WEBGL_ROOT` optionnel ; types MIME
  wasm/data et fichiers gzip/Brotli ; index revalidé ; cache immutable réservé aux noms Unity
  commençant par un hash de 32 caractères. Pas de listing de dossiers.
- `POLYRISK_REQUESTS_PER_MINUTE` reste à 900 par défaut ; configuration bornée de 60 à 3600.
  Le conteneur de livraison choisit 3600 pour conserver une marge proxy/actions/démarrage. La
  protection globale 429 et la limite distincte des entrées de salon restent actives.
- `Server/Deployment` : Dockerfile, entrée qui initialise le volume puis abandonne les privilèges
  root, préparation sélective du bundle et manifeste SHA-256. Aucun secret n'est embarqué.
- Nouveaux tests : `webgl_hosting_test.py` et `polling_budget_test.py`.

## Validations exécutées

SDK .NET **10.0.102** ; compilation Release du serveur : **0 erreur, 0 avertissement**.
Les sorties sont conservées dans `protocol-validation.json` :

- Smoke six joueurs : capacité, authentification, lancement, commandes idempotentes, autorité de
  publication, états périmés et invalides refusés, transport des commandes et migration hôte : PASS.
- Persistance/redémarrage : queue, snapshots, reconnexion et absence des bearer tokens en clair
  dans le disque : PASS.
- Six reconnexions après redémarrage, événements de loyer partagé et bénéficiaires, passage de
  relais de l'hôte et livraison d'une commande : PASS.
- Service WebGL même origine : **26 contrôles**, dont protocole six joueurs, racine HTML, MIME
  wasm/data/JS, gzip décodable, en-tête Brotli, Range 206, cache, CORS restreint, headers de sécurité
  et absence d'accès à `/data`, source et `.env` : PASS.
- Ports invalides et budgets hors bornes : **4 refus sur 4 attendus** (`config-bounds.json`).
- Préparation du bundle sur export synthétique : manifeste vérifié, fichiers de galerie/session
  exclus, refus d'écrasement : PASS (`package-validation.json`). Ce contrôle ne déploie rien.
- Syntaxe Python et entrée shell : PASS.

Le test de charge six clients suit les cadences Unity réelles pendant **90 secondes** : **1170
requêtes authentifiées, toutes HTTP 200**, sans erreur ni 429 au budget de livraison 3600/minute.
Le contrôle de protection à 60/minute reçoit **59 réponses 200 et 6 réponses 429** sur 65 requêtes
supplémentaires (la requête initiale de disponibilité avait consommé un crédit). Résultats complets :
`polling-budget.json`. Ce test de transport local ne mesure pas la performance du rendu Unity.

Un premier lancement du smoke a rencontré un timeout de démarrage pendant une forte charge de
compilation ; les trois scripts ont ensuite réussi, puis la série finale enregistrée a réussi.
Un lancement prématuré du test de cadence a été interrompu pour attendre la nouvelle compilation ;
seul le résultat complet final est retenu.

## Limites et recette du jeu

Ces clients Python valident le **transport**, la persistance et les garde-fous du serveur. Les
payloads des tests protocolaires ne prouvent aucun achat, paiement ou mouvement effectué par le
moteur Unity. Les preuves des vrais clients WebGL sont à intégrer par l'intégrateur dans le rapport
principal. Le serveur QA isolé 5097 utilise un dossier temporaire privé, distinct des anciennes
parties et de l'éventuel futur volume public.

Aucun moteur Docker n'est installé ici : l'image proposée n'a pas été construite ni exécutée dans
un conteneur. Aucun iPhone physique, Safari iOS ni appareil distant n'a été testé par cet agent.
Le HTTPS public, le redémarrage sur le volume hébergé et les clients distants restent à vérifier
sur le service effectivement autorisé avant de déclarer cette livraison publique terminée.
