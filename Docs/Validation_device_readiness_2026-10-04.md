# Validation WebGL et préparation mobile — 4 octobre 2026

## Portée

Cette passe vérifie la livraison WebGL de production, les écrans UI rares et la capacité
du projet à entrer dans les pipelines iOS et Android. Les commandes Unity ont été lancées
dans une copie isolée du projet afin de ne pas interrompre l'éditeur déjà ouvert.

## Résultats

| Cible | Résultat | Preuve et limite |
|---|---|---|
| WebGL développement | Réussi | Build de 61 Mo, démarrage navigateur, recettes 390×844, 844×390 et 1280×720 |
| WebGL production | Réussi | `WEBGL RELEASE BUILD PASS`, 12 Mo compressés, initialisation Unity terminée, écran de chargement masqué, aucune erreur JavaScript collectée |
| iOS | Préparation partielle | Le module Unity iOS est installé et le projet `Unity-iPhone.xcodeproj`, les `Info.plist`, les classes et les bibliothèques IL2CPP ont été générés. Unity termine cependant le build en échec quand Burst demande le SDK `iphoneos`, absent de la machine |
| Android | Bloqué par l'outillage | Le module Unity Android Build Support n'est pas installé ; aucun export Gradle ou APK représentatif n'est possible |
| Appareils physiques | À faire | Aucun test de chauffe, mémoire, encoche, barre d'accueil, gestes ou densité réelle n'a encore été exécuté sur téléphone ou tablette |

## WebGL de production

Le build utilise maintenant des noms de fichiers fondés sur le hash. Cette option évite
qu'un navigateur combine un ancien `framework.js` avec un nouveau binaire WebAssembly
après une mise en ligne. Le défaut a été reproduit avec des noms stables, puis a disparu
après reconstruction avec les noms hashés.

Le build final contient quatre ressources de player hashées et compressées : données,
framework JavaScript, loader et WebAssembly. Sa taille passe de 61 Mo en développement à
12 Mo en production. Une instance Chrome propre a confirmé `unityReady=true`, un écran de
chargement masqué et une liste d'erreurs JavaScript vide.

## Écrans rares

Trois raccourcis limités à l'éditeur et aux builds de développement permettent la recette
visuelle sans modifier une sauvegarde ni attendre un événement aléatoire :

- `F5` ouvre une carte Venture longue ;
- `F6` ouvre la faillite du joueur courant avec un état sans actif ;
- `F7` ouvre la victoire à 10 M$ avec un classement complet.

En 390×844, les trois écrans conservent le texte utile et leurs actions dans le viewport.
La faillite passe correctement de `Give Up` à la seconde confirmation explicite. Cette
couche de recette et ses entrées clavier sont exclues des builds de production par les
symboles `UNITY_EDITOR` et `DEVELOPMENT_BUILD`.

## État iOS

Unity 2022.3.62f3 et son module iOS sont présents. La compilation C#, l'import des assets,
la compilation des shaders Metal et la génération IL2CPP se déroulent jusqu'à la création
du projet Xcode. Le point d'arrêt est externe au code PolyRisk : Burst 1.8.21 appelle
`xcrun --sdk iphoneos --show-sdk-path`, qui échoue car le développeur actif est
`/Library/Developer/CommandLineTools` et qu'aucun SDK iPhoneOS n'est installé.

Il faut installer l'application Xcode complète, l'ouvrir une première fois pour accepter
sa licence et ses composants, puis sélectionner son répertoire développeur. Le même export
Unity devra ensuite être relancé, suivi d'un build Xcode et d'une installation sur appareil.
L'export partiel actuel ne vaut donc pas validation iOS de production.

## Suite de la recette appareil

1. Installer Xcode complet et terminer le build iOS, sans désactiver Burst pour contourner
   le SDK manquant.
2. Installer Android Build Support avec SDK, NDK et OpenJDK, puis produire un export Gradle
   Release.
3. Tester au minimum un iPhone à encoche, un iPad, un Android compact et un grand Android.
4. Rejouer Marketplace, Venture, faillite et victoire au tactile, puis relever FPS, mémoire,
   chauffe, temps de chargement et textes longs.

Le package MCP Unity figure dans les dépendances du projet, mais aucune connexion MCP Unity
n'est exposée à cette tâche Codex. Les résultats ci-dessus proviennent du véritable exécutable
Unity en batchmode et d'une recette navigateur, pas d'une simulation de compilation.


## Retour humain iPhone — reçu le 12 octobre 2026

L’utilisateur confirme avoir testé le build WebGL fourni lors de sa demande de
lien mobile sur un iPhone 14 Pro Max. Le jeu démarrait, les boutons et panneaux
répondaient rapidement, sans lenteur marquante. Il confirme notamment le parcours
Marketplace et le widget qui descend pour laisser voir le plateau.

Portée : validation exploratoire du build mobile précédent, pas une recette
approfondie ni la validation du nouveau build de reconnexion. Reste à faire :
parties multijoueur entre amis, tests sur la durée, déconnexions, changements
portrait/paysage et relevé des problèmes pour une liste de corrections finale.
Le retour est humain ; il ne constitue pas une mesure instrumentée de FPS/mémoire.
