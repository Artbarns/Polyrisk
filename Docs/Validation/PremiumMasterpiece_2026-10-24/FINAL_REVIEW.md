# Revue finale avant validation artistique — 24 octobre 2026

**La proposition B est prête à être jugée par Arthur. Elle n'est pas déclarée artistiquement approuvée.** La reprise retrouve une passe déjà implémentée, construite, comparée et publiée : aucun nouveau réglage de shader, de caméra ou de matériau n'a été ajouté simplement pour refaire un travail terminé.

[Galerie publique A/B et vidéos avec audio](https://artbarns.github.io/Polyrisk/Docs/Validation/PremiumMasterpiece_2026-10-24/review.html) · [rapport complet](README.md) · [paramètres exacts](PARAMETERS.md) · [banc de calibration](CALIBRATION.md).

## Résultat observé

La séparation des cases, les faces satinées et les arêtes du support sont sensiblement plus lisibles que dans A à 430 CSS. Le centre conserve un dégradé sombre et calme. Sur les coins, la réception lumineuse vers l'intérieur remplace l'impression de double cadre épais. Les contacts et les arrivées restent courts et localisés ; la matière reste lisible sans bloom global. Les sources, les lobes de réception et les surfaces ne reposent plus uniquement sur une augmentation d'émission.

La cohérence matérielle des monolithes progresse, mais leur petite silhouette au cadrage réel limite encore la perception de leur finition propre. Certaines diagonales crénellent au framebuffer 430 × 830. Ces réserves sont visibles dans les comparatifs natifs ; les gros plans et le framebuffer 860 × 1660 ne les annulent pas. La revue spécialisée en lecture seule n'a trouvé aucun nouveau défaut visuel bloquant qui justifierait une nouvelle accumulation d'effets avant cette validation.

## Preuves à examiner

- Dix couples A/B dans la galerie : inactif, couleur, sélection, contact, arrivée, monolithe au repos/actif, bordure, reflet et coin. Les deux côtés partagent caméra, zoom, dimensions, position logique, palette et hash économique.
- [Court B sonore](final-short-b.mp4),2,70s ; [long B avec coin](final-long-b.mp4),5,27s ; [long A](final-long-a.mp4) ; [tours réels B](final-live-turns-b.mp4),25,02s. Vitesse réelle, audio du jeu, aucune interpolation. Les seules coupes de banc retirent la restauration QA après le retour au calme ; la prise de jeu réel reste intégrale.
- Les pics figés servent à comparer les matériaux. Les trajectoires de banc n'appliquent pas d'action économique. Les propriétés acquises et la prise de jeu réel constituent des preuves distinctes.

## Vérification exécutée pendant cette reprise

[Résultats vérifiables](final-resume-validation.json) :

- Les14 empreintes des sources artistiques correspondent à celles du Release déjà validé.
- Aucun changement runtime sous Assets, Packages, ProjectSettings ou dans le backendC# depuis le verrou artistique9773edb. Les ajouts ultérieurs concernent la préparation d'hébergement et la documentation.
- Les20 images finales publiques correspondent octet pour octet aux fichiers locaux. Les quatre vidéos publiques répondent 206 aux requêtes partielles et leurs octets vérifiés correspondent aux médias locaux.
- Les24 entrées du manifeste du bundle et le SHA256 de l'archive artistique correspondent.
- La galerie publique répond 200 enHTTPS. La galerie locale9007 et le vrai Release local5097 répondent également200 ; le lanceur a réutilisé les services déjà prêts.
- Avant ce nouveau compte rendu, main local et distant correspondaient à 6b56c27. Le dépôt GitHub est bien Artbarns/Polyrisk ; les mentions 73dd655 dans les anciens rapports décrivent la synchronisation antérieure.

Le navigateur automatisé n'était pas disponible lors de cette reprise. Aucun nouveau test d'interaction n'est donc revendiqué : la vérification fraîche porte sur les sources, artefacts et réponsesHTTP. Les tests Unity et navigateur précédents restent ceux de ce même runtime ; ils n'ont pas été relancés sans changement de code. Les documents Oracle non suivis déjà présents ont été laissés intacts.

## Tests et performance conservés

Le Release existant compile avec0 erreur / 0 avertissement. Les612 assertions existantes,31 contrôles statiques caméra,165 contrôles Editor de calibration,126 contrôles runtime de présentation,176 contrôles caméra en état stable et huit assertions isolées d'enveloppe ont leurs résultats et limites dans le rapport principal. Les sources économiques, réseau, PawnMovement et caméra validée restent préservées.

Sur le Mac Intel/IrisPlus, prisesDevelopment de 6 s : B sans bloom 17,09 ms moyens au framebuffer 430 × 830, contre 33,68 ms pour A ; la suppression du bloom explique une part importante de cette différence. À 860 × 1660, B reste à 83,74 ms dans la prise archivée. Ce ne sont ni des statistiques de longue session, ni un coûtGPU isolé, ni une mesure d'iPhone. **Fluidité, stabilité des petits contours et contrôle tactile sur Safari physique restent à vérifier.**

## Déploiement public

**La galerie est publiée ; aucune URL du jeu multijoueur public n'est encore validée.** Le paquet client+serveur.NET10 avec stockage privé persistant est prêt. GitHubPages sert les preuves, pas le serveur de jeu.

Le dernier journal Oracle disponible rapporte deux refus de capacité gratuite A1 dans la région prévue et aucune instance créée lors de ces tentatives. L'accès au compte n'est donc plus le blocage décrit dans les anciens audits ; la disponibilité d'une cible de serveur réellement autorisée reste à résoudre. Cette reprise ne crée aucune alternative payante, n'augmente aucun abonnement et ne lance aucune relance récurrente. Aucun test de conteneur distant, HTTPS du jeu, persistance distante, partie entre appareils ou Safari iPhone physique n'est déclaré effectué.

**Arrêt pour validation artistique d'Arthur.** Les réserves sur la taille perceptuelle des monolithes et la qualité et la fluidité sur appareil restent explicites. Une nouvelle itération visuelle doit partir de son retour sur les preuves B ; la publication et les tests distants restent un volet séparé.
