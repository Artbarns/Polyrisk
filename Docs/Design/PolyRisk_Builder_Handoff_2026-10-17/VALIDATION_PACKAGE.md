# Validation du package — 17 octobre 2026

## Contrôles effectués

- Les trois exports autonomes ont été ouverts dans un navigateur sur serveur local.
- Dés : « Voir un double » déclenche la séquence et aboutit à deux faces identiques, total cohérent et libellé DOUBLE ; les commandes redeviennent disponibles.
- Pulse : le bouton famille ouvre le cran étendu / inspecteur ; sélectionner Opioid Paradise remplace le titre, niveau et loyer, désactive l’achat d’une propriété détenue et propose Retour.
- Moments optionnels : le parcours clic alternatif demande une confirmation ; l’achat simulé passe le capital de 18 000 à 16 700 et la famille de 2/3 à 3/3. Le loyer simulé +130 porte le capital à 16 830. Rendu visuel de cette démo inspecté.
- Syntaxe des 21 scripts incorporés aux trois exports vérifiée avec `node --check`. JSON des paramètres valide. Sources inférieures à 1 Mo ; écouteurs d’hôte non exportables retirés.
- Trois WAV vérifiés : PCM 16 bits, mono, 48 kHz ; contacts 100 ms, ponctuation double 430 ms.
- Inventaire SHA-256 généré après les documents ; vérificateur exécuté sur le dossier puis après extraction du ZIP. Structure ZIP contrôlée.

## Limites explicites

Ces contrôles concernent les démonstrations et l’archive. Aucune intégration, compilation Unity, mesure de performances, couverture mobile physique ou validation WebGL du vrai jeu n’a été effectuée ici. Les gestes tactiles, tous les parcours clavier, interruptions, dimensions et états réseau restent à vérifier dans l’implémentation via `04_ACCEPTANCE.md`.

Le fond plateau de Pulse est une capture de contexte. Les données des trois démos sont illustratives. L’habillage autonome utilise des CDN ; ne pas présenter ces exports comme garantis hors connexion.

Le code et les documents du jeu ont été consultés pour les points d’intégration. L’estimation GO doit rester une projection en lecture seule : `FinanceSystem.ProcessGoIncome` effectue le règlement et ne doit pas être appelée pour afficher un aperçu.
