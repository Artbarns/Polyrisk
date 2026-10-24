# Oracle — préparation de la livraison, 24 octobre 2026

Arthur a choisi Oracle, sous réserve de performances adaptées et d'une offre gratuite. Il doit encore créer son compte. Aucun compte connecté, machine, IP publique ou origine de jeu distante n'est disponible à ce stade. Aucune ressource payante n'a été commandée.

## Choix technique

Une seule machine Ampere A1 ARM64, cible2OCPU/12GB RAM et50GB de disque de démarrage, Ubuntu24.04 LTS. Vérifier les limites effectives et le label Always Free dans le compte. La documentation consultée indique1 500OCPU-heures et9 000GB-heures par mois pour A1, ainsi que200GB de stockage bloc cumulé ; ce ne sont pas les anciens chiffres4OCPU/24GB. Une machine2/12 sur31jours utilise1 488/8 928heures. Capacité gratuite non garantie et récupération possible des machines inactives.

Le dimensionnement est une hypothèse raisonnable pour une partie privée de six joueurs ; les graphismes tournent dans le navigateur de chaque joueur. Aucune équivalence de performance avec un fournisseur payant n'a été mesurée. Le réseau Internet, les performances ARM64 et l'iPhone restent à vérifier.

Le backend.NET10 et le WebGL Release sont inchangés. La couche de déploiement ajoute un proxy Caddy HTTPS et un montage privé `/data`. Aucun port backend n'est publié. Une seule instance conserve le protocole et la sauvegardeJSON existants. Les logs sont bornés ; les sauvegardes privées peuvent être exportées hors de la VM. Le guide traite les règles réseau, le nom DNS, l'accès SSH, les mises à jour et les contrôles distants.

## Fichiers créés et modifiés

Créés sous `Server/Deployment/Oracle` : `compose.yaml`, `Caddyfile`, `.env.example`, `prepare.py`, `backup.py`, `release.dockerignore`, `README.md`. Modifié : `Server/Deployment/README.md`, pour référencer le choix Oracle et conserver Railway comme alternative. Ce dossier de validation est également nouveau. Les documents de synchronisation GitHub de la reprise précédente restent conservés.

Aucun changement dans les sources du jeu, shaders, paramètres Unity, backendC#, économie, caméra, pions, callbacks ou protocole. Aucun moteur Docker, service de démarrage ou tâche récurrente installé sur le Mac. Le client Compose officiel utilisé pour vérifier la configuration est temporaire, hors dépôt ; sonSHA256 a été vérifié avant exécution.

## Contrôles exécutés

- Compilation Release du backend : réussite,0erreur/0avertissement. [Journal](backend-build.log).
- Six clients protocole aux cadences réelles pendant65secondes :849requêtes, toutesHTTP200. Limiteur de protection vérifié séparément : six rejets429 sur65requêtes avec budget volontairement réduit à60/min. Ce sont des clients HTTP locaux, pas six navigateurs ni un test Oracle. [Résultats](six-client-budget.json).
- Préparation, sauvegarde et configuration :18contrôles passent, notamment empreintes des24fichiers revus, refus des fichiers altérés/privés/liens symboliques, exclusion des caches générés, refus d'écrasement, archive de sauvegarde intacte et permissions0600, rejet des snapshots invalides, variables obligatoires, origine stricte et absence de port backend publié. Compose officielv5.6.1 valide la configuration sans moteur Docker. [Résultats](preparation-checks.json).
- Archive prête :31fichiers, intégritéZIP vérifiée et24empreintes originales conservées ; aucune donnée runtime, variable privée ou cache de compilation inclus. [Manifest de livraison](delivery-package.json).
- `git diff --check` passe.

Les premiers essais de préparation ont correctement refusé des répertoires `backend/obj` générés par l'outillageC#. La préparation a été ajustée pour ignorer ces caches, copier uniquement les fichiers du manifeste et filtrer le contexte Docker. Les caches locaux n'ont pas été effacés. Une assertion de comptage de l'archive attendait32fichiers au lieu des31réels ; elle a été corrigée et l'intégrité a été vérifiée sur l'archive déjà créée, sans l'écraser.

## Paquet et limites

Livraison locale : `Builds/PolyRisk-OracleReady-20261024.zip` ; dossier préparé final : `Builds/OraclePremiumMasterpieceReady_20261024`. L'archive artistique d'origine est conservée. Le ZIP ne se déploie pas sur GitHub Pages : son Dockerfile doit être construit sur le serveur Linux.

Le conteneur n'a pas été construit ou lancé : pas de moteur Docker sur ce Mac et pas encore de VM Oracle. HTTPS, DNS, configuration pare-feu distante, sauvegarde/restauration sur la VM, disponibilité gratuite, charge ARM64, latence Internet, parties entre navigateurs distants et Safari physique ne sont pas validés. Le script de sauvegarde est testé isolément ; aucune planification n'est installée.

Prochaine étape : Arthur crée son compte Oracle gratuit ; vérifier ensuite les ressources proposées et obtenir un accès autorisé au serveur. Puis construire l'image, activer HTTPS, vérifier persistance et multijoueur réels et mesurer les performances. Le Mac pourra être éteint après cette mise en ligne effective ; actuellement le jeu reste local et seule la galerie est publique.

Références : [Oracle Always Free](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm), [Docker Ubuntu](https://docs.docker.com/engine/install/ubuntu/), [.NET ARM64](https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md), [Caddy HTTPS](https://caddyserver.com/docs/automatic-https).
