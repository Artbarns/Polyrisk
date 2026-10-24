# Validation du lobby compact — 15 octobre 2026

## Résultat

Build Unity WebGL Release réalisé depuis le code courant, avec Economy Regression
114 assertions PASS et Mechanics Regression 110 assertions PASS. Le build
`/tmp/PolyRiskCompactRosterFix` pèse environ 12 MiB.

Dans un navigateur à 900 × 430, un lobby de six sièges a affiché les joueurs en
deux colonnes sur trois lignes. Les noms et états étaient lisibles, et les
boutons Ready, Start Game, Leave Lobby ainsi que la préférence Réduire les
animations restaient visibles sans chevauchement ni défilement. Le même lobby
est ensuite passé en portrait 430 × 900 (une colonne, liste défilante, actions
visibles), puis est revenu en paysage à la grille 2×3 en environ une seconde,
sans nouvel événement réseau. Le lobby a été créé depuis le client WebGL ; les
cinq autres sièges QA l’ont rejoint via le serveur local isolé sur le port 5094.

Le premier essai a exposé une limite du sélecteur USS visant le nom interne du
conteneur `ScrollView` Unity : la classe de largeur de ligne s’appliquait, mais
les rangées continuaient à s’empiler. La correction configure directement
`ScrollView.contentContainer` en `Row`/`Wrap` pour cinq joueurs ou plus en
paysage, et revient à `Column` en portrait. Le contrôleur lit
`Screen.width/height` deux fois par seconde seulement tant que le lobby est
ouvert, puis reconstruit le roster uniquement quand l’orientation change. La
surveillance s’arrête à la fermeture du lobby ou au démarrage de la partie. Les
rangées compactes gardent une hauteur de 24 unités UI. Aucun changement aux
règles ou à l’économie.

## Limites restantes

Le contrôle est visuel dans un navigateur desktop avec viewport compact ; il ne
mesure pas la cible tactile sur iPhone/Safari ni les seuils intermédiaires de
viewport. La recette sur appareil réel et le test complet à six appareils
restent ouverts.

Contrôle de cas limite : le 15 octobre, un lobby de six sièges a reçu via l’API
locale cinq noms à la longueur maximale autorisée par le serveur (18 caractères).
Les noms `LongPlayerName0001` à `LongPlayerName0005` apparaissaient complets dans
les rangées du build WebGL. Une page de diagnostic temporaire, sans changement
au jeu, a ensuite relevé `window.innerWidth × innerHeight = 1280 × 720`, DPR 2
et un canvas CSS de 1280 × 720. L’API de capture ne renvoyait qu’une image de
1048 × 720, tronquant son bord droit ; cela ne démontre donc pas une coupure du
layout. Il reste à refaire une capture réellement calibrée à 1048 px de viewport
CSS avant de conclure sur ce seuil intermédiaire.
