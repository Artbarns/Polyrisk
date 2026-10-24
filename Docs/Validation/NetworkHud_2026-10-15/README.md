# Résilience UI réseau — 15 octobre 2026

Sur le build WebGL Release V76, un client hôte rendu a créé un lobby à deux sièges et lancé une partie. L’action Venture `action.primary` (« Draw Card ») a été refusée trois fois par le relais QA avec des réponses 503 **avant transmission au serveur**. Le bandeau « No server confirmation. Retry the same action to resync. » apparaît alors au-dessus du plateau. Le snapshot serveur est resté à la séquence 1 pendant l’échec.

Après rétablissement du relais, le même bouton a été renvoyé depuis le client. Le serveur a appliqué une seule commande à la séquence 2 (`action.primary`) et publié une seule activité `venture` (« pioche Venture #11 »). La carte s’est affichée dans le client. Cette capture a cependant révélé que le bandeau d’erreur restait visible après l’application autoritaire de la commande.

- `error_after_three_failures_v76.png` : bandeau visible après trois 503.
- `retry_applied_banner_stale_v76.png` : carte Venture résolue, mais ancien bandeau encore visible.

Le correctif V77 efface maintenant le bandeau à la confirmation de l’état autoritaire (`ClearPendingClientCommand`). Son build et sa recette rendue de disparition restent en cours ; cette note ne revendique pas leur validation.

## V77 — effacement après confirmation

V77 ajoute aussi l’effacement du bandeau à `OnlineGameController.ClearPendingClientCommand`, appelé lors de la publication d’un état autoritaire. Build Unity 2022.3.62f3 Release PASS : 114 assertions économie, 110 mécaniques, `BuildOptions.None`, sortie 12 MiB.

Dans un client hôte WebGL V77 à deux sièges, le lancer a reçu trois réponses 503 avant commit : la capture `error_after_three_failures_v77.png` montre le bandeau, et le serveur est resté à la séquence 0. Le clic de reprise dans cette même session a ensuite produit une seule séquence `turn.roll` (séquence 1, un seul reçu). Vérification visuelle finale par CUA : retour au plateau et bandeau absent après l’acquittement. Le message disparaît donc bien dans la session récupérée, sans double application de l’action. Le proxy de recette isolé utilisait `--fail-next-commands 3`; le service de preview public n’a pas été modifié.

Un second parcours a exercé le vrai slider « Slide to Buy » de Fentanyl Labs. Après un 503 transitoire, le serveur a appliqué un seul `action.primary` à la séquence 2, déduit 1 300 $ et publié un achat unique. La capture `property_purchase_after_transient_retry_v77.png` montre l’état après achat sans bandeau, mais ce run n’avait pas d’erreur persistante à effacer. Il valide la commande et son idempotence, pas la disparition après trois échecs.
