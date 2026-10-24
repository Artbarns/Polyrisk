# Validation UI, UX et WebGL — 2 octobre 2026

## Parcours Marketplace corrigé

Le panneau Marketplace change maintenant de taille selon l'étape de l'action :

1. la vue d'opérations s'ouvre en panneau complet ;
2. le choix Tender réduit le panneau à une barre de 88–112 px et masque la liste, le CTA et le footer ;
3. le plateau reste visible et les propriétés éligibles continuent de pulser ;
4. le clic sur une propriété arrête le mode de sélection et restaure le panneau complet ;
5. le panneau affiche le nom de l'actif, son revenu et l'action de confirmation ;
6. retirer la propriété sélectionnée ramène au mode compact pour en choisir une autre.

Recette réelle effectuée dans le build WebGL : ouverture Marketplace, choix Tender, panneau compact « SELECT A PROPERTY ON THE BOARD », sélection de Ferrari Maranello, puis panneau restauré « PROPERTY SELECTED » avec `Income: $170 / turn`.

## Autres parcours humains ajoutés

- Assistance Automobile : décision `-1 / KEEP / +1` après le résultat réel du dé, avant le mouvement. L'IA note les trois cases atteignables et n'ajuste que si la destination est meilleure.
- Ultimes : le bouton ouvre la liste de toutes les capacités actuellement disponibles au lieu de prendre automatiquement la première famille.
- Armement : passage en sélection compacte, pulsation des propriétés adverses éligibles et validation de la cible cliquée.
- Aerospace : même parcours pour les propriétés Aero/Tech/Logistics et la case Marketplace.

## WebGL

Le module WebGL de Unity 2022.3.62f3 est installé et le build de développement termine avec `WEBGL PLAYER BUILD PASS`.

Le premier démarrage révélait un `memory access out of bounds` dans `UniversalRenderPipeline.SetupPerFrameShaderConstants`. La cause était déterministe : tous les profils URP sauf Ultra demandaient un dithering LOD Blue Noise avec une référence de texture nulle. Les profils ont reçu les textures URP déjà référencées par Ultra. Le probe de réflexion temps réel est aussi désactivé dans le player WebGL, le profil WebGL passe de High à Low et la mémoire initiale passe de 32 à 256 Mo.

Après reconstruction, le player démarre sans nouvelle alerte : le HUD, le plateau, les pions et les panneaux UI sont rendus. Le parcours Marketplace décrit ci-dessus a été exécuté dans le navigateur intégré.

## Régressions finales

- Mécaniques : **54/54 assertions**.
- Économie : **27/27 assertions**.
- Smoke IA précédent après le choix Automobile : **5/5 parties**, zéro anomalie.
- Vérification structurelle : `git diff --check` sans erreur.

Une seconde passe UI/UX a ensuite couvert les 16 écrans actifs et le panneau historique.
Elle ajoute les safe areas, les cibles tactiles, le focus clavier, les états vides, les
confirmations destructives et un template WebGL responsive. La matrice détaillée se trouve
dans `Docs/Validation_ui_ux_2026-10-02.md`.

## Impact et étapes suivantes

Le changement Marketplace améliore directement la visibilité du plateau pendant une sélection et retire un aller-retour mental entre la liste et la case. Le profil Low réduit le coût des ombres et lumières dans le navigateur ; les mécaniques, les calculs économiques et la qualité desktop ne changent pas.

La recette navigateur couvre maintenant téléphone paysage, téléphone portrait et desktop.
La prochaine passe doit se faire sur appareils physiques pour les encoches, gestes, textes
longs, chauffe et mémoire. Le résultat actuel rend les enchaînements principaux
fonctionnels, mais « parfait » demandera plusieurs sessions avec de vrais joueurs et des
mesures de friction.

Le jeu local accepte déjà deux à six joueurs et les règles sont centralisées, mais le dépôt ne contient aucune couche réseau. Le chantier en ligne nécessite donc un choix de transport et de services de lobby/relay, un modèle serveur autoritaire, la sérialisation des commandes, les reconnexions et des tests multi-clients. Il ne faut pas confondre le build WebGL jouable avec un multijoueur en ligne déjà opérationnel.
