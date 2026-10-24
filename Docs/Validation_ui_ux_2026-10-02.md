# Audit UI/UX — 2 octobre 2026

## Objectif

La passe couvre chaque écran UI Toolkit livré dans le projet. Elle vise une interface
tactile lisible, des parcours sans impasse et un coût raisonnable pour Unity mobile et
WebGL. Elle ne remplace pas une recette sur iPhone et Android physiques ni les sessions
avec des joueurs externes.

## Couverture des panneaux

| Panneau | Contrôle effectué | État |
|---|---|---|
| TopHUD | Safe area, valeurs compactes, aucun traitement par frame | Validé structurellement et dans WebGL |
| TileDetail / actions | CTA 48 px, états désactivés explicites, libellés Audit cohérents | Validé dans WebGL |
| Marketplace landing | Entrée légère, CTA principal, panneau non destructif | Validé dans WebGL |
| Marketplace hub | Deux choix sémantiques, cartes tactiles, contenu sans débordement | Validé dans WebGL après correction flex |
| Sanctions | Liste scrollable, coûts corrigés, sélection explicite | Validé structurellement et dans WebGL |
| Sélection joueur | Cibles tactiles, état vide, fermeture et touche Échap | Validé structurellement |
| Tender initial | Panneau réduit pendant la sélection plateau, réouverture avec détails, confirmation lisible | Validé dans WebGL |
| Attente des offres | Statut lisible et points de suspension typographiques | Validé structurellement |
| Formulaire d'offre | Actions tactiles, fermeture cohérente | Validé structurellement |
| Spectateur d'offres | Lignes non interactives, état vide | Validé structurellement |
| Revue des offres | Lignes sous forme de boutons, sélection clavier/tactile, état vide, panneau étendu | Validé structurellement |
| Notification Marketplace | Transition légère, durée indépendante du time scale | Validé structurellement |
| Carte d'enchère réutilisable | Élément interactif sémantique | Validé structurellement |
| Faillite | Actifs scrollables, état vide, confirmation en deux temps avant abandon | Validé en WebGL portrait, y compris le second état de confirmation |
| Carte Venture | Fermeture cohérente, animation désactivable | Validé en WebGL portrait avec une carte longue |
| Victoire | Statistiques scrollables, limites de hauteur | Validé en WebGL portrait avec le classement de quatre joueurs |
| MarketplaceChoice historique | Noms de contrôles et copie remis en cohérence | Conservé comme écran de compatibilité |

## Changements transversaux

- Les boutons ont un focus visible et les cibles importantes font au moins 44–48 px.
- La touche Échap ferme les panneaux qui exposent une action de fermeture.
- Les safe areas sont recalculées sur changement de géométrie, sans boucle `Update`.
- Pendant un picker plateau, un tap à 36–56 px d'une case prend la case la plus proche ;
  la tolérance n'est évaluée qu'au tap et ne modifie ni les colliders ni le rendu.
- Les états vides expliquent pourquoi une liste ne contient aucune action possible.
- Les actions destructives de faillite demandent une seconde activation explicite.
- Les animations utilisent uniquement translation, opacité et échelle. Elles fonctionnent
  en temps réel et peuvent être désactivées par `PolyRisk.ReduceMotion`.
- Le template WebGL remplit le viewport et limite la densité de rendu mobile à 1× afin
  d'éviter le débordement 960×600 et le surcoût des écrans à forte densité.

## Choix de performance

Aucun flou dynamique, masque complexe, ombre animée, texture supplémentaire ou calcul de
layout par frame n'a été ajouté. WebGL conserve le profil graphique Low et 256 Mo de
mémoire initiale. Les listes longues sont des `ScrollView`; les transitions restent
courtes et portent sur des propriétés peu coûteuses. Le template ne dépend d'aucune
bibliothèque JavaScript.

## Validation automatique

- Compilation Unity 2022.3.62f3 : réussie.
- Régression mécaniques : **54/54 assertions**.
- `git diff --check` : aucune erreur de whitespace.
- Build WebGL de développement : `WEBGL PLAYER BUILD PASS`.
- Build WebGL de production : `WEBGL RELEASE BUILD PASS`, **12 Mo** compressés contre
  **61 Mo** pour le build de développement.

## Recette navigateur

- **844×390** : hub complet sans texte coupé, panneau Tender réduit, plateau visible ;
  un tap volontairement posé sur la case voisine sélectionne Ferrari Maranello, la cible
  possédée la plus proche, puis restaure le panneau avec revenu et confirmation.
- **390×844** : plateau, bottom sheet et hub tiennent dans le viewport ; les deux cartes
  restent côte à côte et leurs descriptions sont entièrement lisibles.
- **390×844, écrans exceptionnels** : carte Venture longue, faillite sans actif avec la
  confirmation destructive en deux temps, puis victoire avec classement complet ; aucun
  CTA ni texte utile ne sort du viewport.
- **1280×720** : HUD, plateau et bottom sheet restent alignés et utilisent toute la page.
- Console du navigateur après les trois recettes : aucune entrée de niveau `error`.
- Taille du build de développement : **61 Mo**.

Le premier rechargement après reconstruction a révélé un mélange de fichiers WebAssembly
anciens et nouveaux dans le cache navigateur (`RangeError`). Les noms de fichiers WebGL
sont désormais basés sur leur hash. Après reconstruction, le build de production atteint
l'état Unity prêt, masque son écran de chargement et ne remonte aucune erreur JavaScript.

Les libellés F1/F2/F5/F6/F7/F9 visibles pendant la recette appartiennent uniquement à la
build de développement. Les composants concernés sont maintenant inactifs hors debug ; la
build de production n'affiche pas cette couche et n'écoute pas ces raccourcis.

## Recette encore nécessaire

- iPhone avec encoche et barre d'accueil, iPad, puis un Android compact et un grand écran ;
- clavier/souris desktop et navigation tactile sur tous les formulaires ;
- textes localisés ou noms exceptionnellement longs ;
- faillite, Venture et victoire déclenchées au cours d'une vraie partie, après leur recette
  visuelle forcée déjà validée ;
- cinq à dix parties humaines pour mesurer compréhension, rythme et fatigue visuelle.
