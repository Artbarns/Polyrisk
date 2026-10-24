# État du preview ouvert — 16 octobre 2026

- L’onglet WebGL déjà ouvert montrait le canvas du jeu et un lobby privé à **1/6** avec la bannière **« Unknown Error »**. Aucun message `error` ou `warning` n’était présent dans les logs console du navigateur au moment du contrôle.
- L’onglet n’a pas été rechargé ni manipulé : cela aurait pu interrompre le lobby actif.
- Le serveur local du preview (`127.0.0.1:8930`) répond HTTP 200. Le serveur lobby (`127.0.0.1:5093`) répond HTTP 200 avec `status: ok`, `protocol: 1`. L’endpoint local `/ready` de Cloudflare indique `readyConnections: 1`.
- Le résolveur système n’a pas résolu le nom public; une requête DNS directe à `1.1.1.1` a renvoyé **NXDOMAIN** pour l’enregistrement A. Le lien public vu dans cet onglet est donc expiré/non résolvable à cet instant. Le tunnel local connecté peut correspondre à un autre nom; son domaine public n’a pas été récupéré.

## Reprise en lecture seule du tunnel existant

Le tunnel `cloudflared` est resté actif sur la même origine locale, avec une
connexion edge, sans le recréer ni toucher au lobby. Son hostname courant a été
récupéré depuis les métriques locales :
`https://cooper-justin-seven-devel.trycloudflare.com/`.

Le lien répond HTTP 200 sur la page et sur les quatre ressources du Player
Unity; le WASM est servi en `application/wasm` avec `Content-Encoding: gzip`.
Le tunnel pointe vers `/tmp/PolyRiskWebGLQuitMenuFinal` (index daté du
14 octobre), donc vers l’ancienne préversion de sortie, pas le build isolé
récent `/tmp/PolyRiskFinalAudit_20261016`. Cette vérification HTTP ne teste ni
la création de lobby ni le rendu Unity. Aucun onglet existant n’a été rechargé,
et aucune commande/lobby n’a été touché.

Une ouverture QA en nouvel onglet CUA vers ce hostname HTTPS a également
expiré en attente de l’attachement de la webview; aucune interaction de jeu n’a
eu lieu.

## Suite

Utiliser le hostname courant seulement si l’ancienne préversion convient au
prochain essai humain. Pour recetter la build du 16 octobre, il faudra un
chargement interactif sur un navigateur graphique et une origine de preview
servant cette build; ne pas rediriger le tunnel courant pendant qu’il sert
l’ancienne session. Reproduire l’avertissement dans un lobby distinct et
examiner la réponse API sans réutiliser ni rafraîchir le lobby actif.
