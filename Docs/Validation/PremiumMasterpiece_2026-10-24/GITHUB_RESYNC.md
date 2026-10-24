# Reprise après publication GitHub — 24 octobre 2026

## Synchronisation vérifiée

Arthur a publié les sources sur [Artbarns/Polyrisk](https://github.com/Artbarns/Polyrisk). Après lecture du distant et fetch, `main`, `origin/main` et le distant pointent tous sur `73dd655e997da70af0a1067b18301daddc188fb0`. L'arbre de travail était propre à la reprise. Aucun reset, nettoyage ou push forcé n'a été effectué.

L'import ajoute ou modifie 1 150 chemins : 15 dans `.agents`, 1 134 dans `Docs`, un dans `tasks`. Aucun changement dans `Assets`, `Server`, `Packages` ou `ProjectSettings` par rapport au point artistique `9773edb`. Les anciennes branches d'import supprimées par Arthur ne sont pas recréées. Un repère de sauvegarde local, `refs/backups/pre-release-resync-20261024`, conserve le commit publié avant les mises à jour de documentation.

## Livraison vérifiée

- Les 14 empreintes du manifeste des sources artistiques correspondent.
- Les 24 fichiers du paquet correspondent à leur manifeste et aux entrées actuelles : backend, Dockerfile, script de démarrage et export WebGL Release.
- L'archive reste identique : SHA256 `c4ee1cc85131296e1829b55bc2a3487e4d9f782d17ca5fa51304a81e5a92c0be`.
- [Le déploiement GitHub Pages](https://github.com/Artbarns/Polyrisk/actions/runs/40081415640) est terminé avec succès sur `73dd655`.
- [La galerie publique](https://artbarns.github.io/Polyrisk/Docs/Validation/PremiumMasterpiece_2026-10-24/review.html) répond enHTTPS. Les 20 images A/B et le HTML sont identiques aux fichiers locaux. Les quatre vidéos répondent aux requêtes partielles avec le statut206 et les octets attendus.
- Le sélecteur A/B a également été actionné dans le navigateur sur la galerie publique. Les résultats détaillés sont dans [github-resync-validation.json](github-resync-validation.json).

Le premier essai HTTP a utilisé `read(-1)`, rejeté par la réponse HTTP de cet environnement Python. L'appel a été corrigé en `read()` et les contrôles ont été réexécutés avec succès. Il s'agissait du script de vérification, pas d'un échec du site.

Aucun rebuild Unity nécessaire : aucune source ni configuration du jeu n'a changé depuis le build Release vérifié. Les tests économiques, caméra et rendu précédents restent des résultats de cette version ; ils n'ont pas été relancés artificiellement pendant cette synchronisation. Aucun code du jeu n'a été modifié.

## Hébergement du jeu : décision encore ouverte

La galerie est publiée ; le jeu et son serveur multijoueur ne le sont pas. Le paquet same-origin existant conserve un serveur.NET10, une seule instance et le fichier de sauvegarde sous `/data`, hors racine publique.

Arthur souhaite étudier une solution gratuite avant d'ouvrir un hébergement payant. Comparaison documentaire, sans test de performance de ces fournisseurs :

| Offre | Compatibilité avec le stockage actuel | Limite pertinente |
|---|---|---|
| Oracle Always Free, machine virtuelle Linux | Possible avec stockage persistant et déploiement du conteneur existant | Capacité gratuite parfois indisponible ; machines inactives récupérables ; administration, HTTPS et sauvegardes à configurer |
| Railway Hobby | Correspond à la préparation existante | Minimum5USD/mois, usage supplémentaire possible ; aucun abonnement souscrit |
| Render gratuit | Pas directement | Disque éphémère, pas de volume persistant gratuit ; veille après15minutes |
| Koyeb gratuit | Pas directement | Pas de volume persistant gratuit ; arrêt à zéro après1heure sans trafic |
| Northflank Sandbox | Ne prouve pas une livraison gratuite compatible telle quelle | Services gratuits toujours actifs et base gratuite, mais la gratuité d'un volume fichier compatible n'est pas établie ; ne pas assimiler base de données et fichierJSON |

Sources officielles consultées : [Oracle](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm), [Railway](https://railway.com/pricing), [Render](https://render.com/docs/free), [Koyeb](https://www.koyeb.com/docs/reference/instances), [Northflank](https://northflank.com/pricing).

La piste gratuite conservant le serveur actuel est Oracle, sous réserve de création du compte et de disponibilité d'une machine Always Free. Ce n'est pas une garantie de disponibilité ou de performances équivalentes à un hébergement payant. Le Mac n'aurait pas besoin de rester allumé après un déploiement distant effectif. Il reste nécessaire pour le jeu local actuel.

## Suite concrète

1. Obtenir l'accès au fournisseur choisi ; ne souscrire aucun forfait payant sans accord.
2. Construire et vérifier l'image Linux sur l'hébergement : Docker n'est pas installé sur ce Mac.
3. Configurer une seule instance, stockage durable privé, sauvegarde et HTTPS.
4. Vérifier les fichiers WebGL, deux vrais clients rendus, le protocole six joueurs, la reconnexion et la persistance après redémarrage distant.
5. Tester Safari et le tactile sur iPhone physique avant toute affirmation de compatibilité ou de fluidité sur appareil.

L'absence de compte d'hébergement connecté reste la dépendance externe. La publication des sources et de la galerie ne la remplace pas.
