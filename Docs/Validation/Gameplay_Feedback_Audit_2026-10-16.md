# PolyRisk — audit global des retours visuels et du motion design

**Périmètre :** clarté et plaisir de jeu sur Unity UI Toolkit/WebGL/mobile. Cet audit ne propose aucune modification de règle, de calcul économique, de données de partie ni de protocole réseau.

**Sources examinées :** `GameEvents`, les publications d’activité des systèmes de tour, dés, plateau, finances, pouvoirs, Venture, Marketplace et insolvabilité, les contrôleurs UI, les UXML/USS, l’inventaire de panneaux et les validations visuelles existantes. Les captures/mockup de référence ne sont pas présentes comme images consultables dans le workspace : les écarts de structure et les problèmes déjà consignés sont vérifiables, mais la conformité visuelle pixel à pixel attendra ces références.

## Conclusion

PolyRisk possède déjà une bonne base fonctionnelle : HUD avec compteurs interpolés, fil d’activité, bottom sheet de propriété, animation de déplacement par case et rebond, surbrillance de sélection, panneaux Marketplace et Venture animés, minuteur d’offre, retours d’erreur réseau, réglage de mouvement réduit et écrans de faillite/victoire. Le récent retour de dé montre les faces et le total **après** résolution.

Le manque principal n’est donc pas « ajouter une animation partout », mais relier chaque temps fort à une séquence cohérente **contexte → action attendue → confirmation → résultat → conséquence**. Aujourd’hui beaucoup d’événements sont publiés en français dans le feed, mais partagent une présentation de notification similaire; plusieurs interactions montrent le résultat sans mise en scène de l’action, et plusieurs panneaux ont seulement une validation structurelle plutôt qu’une recette visuelle runtime. Le jet de dés est le meilleur premier candidat : une anticipation visuelle courte, puis le vrai résultat autoritaire, le déplacement, et enfin l’inspection de la case.

Une animation doit attirer l’attention uniquement quand elle apporte une information. Les retours critiques (résultat, perte d’argent, choix) ont la priorité; les événements secondaires restent calmes et lisibles. C’est ainsi que l’ensemble semblera plus vivant sans transformer le jeu en flux de néons ou ralentir les tours.

## Cartographie des moments de jeu

| Moment / événement | Présence confirmée dans le code ou les écrans | Expérience à obtenir | Écart / priorité |
|---|---|---|---|
| Début du tour / tour actif | `OnTurnStarted`, activité « commence son tour », HUD et accent du pion actif | Repère immédiat « à qui de jouer », phase courante et action primaire disponible | Le pion actif est mieux distingué; vérifier que la consigne de tour est toujours évidente, notamment en ligne et après reprise. **P1** |
| Lancer de dés | `DiceSystem` publie les deux dés; le feed les présente comme faces et total avec entrée animée | Geste déclencheur clair, anticipation brève, résultat réellement roulé, puis total de cases | L’animation actuelle intervient après le résultat; manque la sensation de lancer/settle. Concevoir un temps fort compact. **P0** |
| Déplacement du pion | Chemin publié et déplacement case par case avec arc/rebond; supprimé en mouvement réduit | Faire suivre le regard du résultat vers le pion et rendre l’arrivée incontestable | Mouvement existant; tester vitesse, lisibilité des autres pions et arrivée bord/coin du plateau. Caméra suiveuse non recommandée tant qu’elle masque le contexte du plateau. **P1** |
| Arrivée / inspection d’une case | activité d’arrivée, sélection du plateau, bottom sheet avec famille, niveau, Rent/Risk/ROI et actions | La case d’arrivée est mise en évidence; le panneau explique en une vue ce qui est ici et la prochaine action possible | Séquencer l’arrivée, le feed et le panneau sans qu’ils se masquent; vérifier les cases spéciales autant que les propriétés. **P0** |
| Achat / refus / passage | Boutons et achat coulissant présents; activités purchase/pass | Action irréversible confirmée, coût visible avant action, validation lisible après action | Vérifier le slide sur tactile et clavier, feedback de réussite, état insuffisant/refusé et animation du solde. **P0** |
| Loyer, taxe, revenu, GO, coûts | Transactions et événements balance; activités rent/tax/transit/income; compteurs HUD | Montrer brièvement qui paie/reçoit, montant et raison; lier le delta au bon compteur sans cacher l’action suivante | Le compte d’argent s’interpole mais les conséquences financières ne semblent pas toutes disposer d’un retour visuel dédié. Inventorier les sources réelles et normaliser les messages. **P1** |
| Amélioration / monopole / pouvoir de famille | activités upgrade/family/power et sélection de cibles | Expliquer la cible admissible, l’effet appliqué et la confirmation d’un bonus de famille | Les possibilités de ciblage sont signalées; les retours de résultat doivent être homogènes entre familles et lisibles en ligne. **P1** |
| Investissement, casino, marchés et cycles | panneaux de sélection, durée/montant/risque; activités investment/casino/cycle/tax | Avant décision : exposer montant, durée et risque déjà définis. Après : résultat, gain/perte et état du cycle | Contrôler la hiérarchie entre risque et confirmation et distinguer visuellement gain, perte, volatilité et changement macro. **P1** |
| Marketplace : choisir, invitation, attente, offre, revue, résultat | États d’écran dédiés, sélection du vendeur/biens, minuteur, liste d’offres animée, revue/spectateur; activités marketplace | À tout moment savoir qui attend qui, ce qui est sélectionné, le temps restant, l’offre à examiner et l’issue | Plusieurs parcours sont couverts; rejouer le parcours complet à deux puis six sièges et vérifier que l’activité n’est pas perdue dans les transitions d’écran. **P0** |
| OPA hostile / sanctions / accord | formulaires, ciblage joueur/propriété, confirmation, transports/résultats réseau et activités | Faire comprendre « qui cible qui », le prix/coût/risque affiché, la réponse et l’effet final | Les tests logiques existent, la recette visuelle runtime de confirmation/résultat est incomplète selon l’inventaire; éviter d’animer un état seulement envoyé mais pas confirmé. **P0** |
| Venture / carte à effet | carte modale, catégories visuelles, flip, confirmation et effets couverts en réseau | Tirage lisible, type d’effet, texte, cible/action requise, puis résolution et conséquence | Animation de carte existe déjà; contrôler textes longs, attente de confirmation et mouvement réduit. **P1** |
| Insolvabilité / liquidation / faillite | écran de dette et d’actifs, liquidation, écrans Bankruptcy/Victory présents | Expliquer le déclencheur, les choix de sortie, actifs récupérés/perdus et sortie de partie | Logique très couverte; recettes visuelles de ces états de fin restent à produire. Événement majeur : priorité forte. **P0** |
| Fin de partie / victoire | événement de victoire, bilan/statistiques et écran dédié | Conclusion claire et gratifiante, condition de victoire compréhensible, bilan scannable | Ajouter une mise en scène mesurée et vérifier l’état final en WebGL, sans confondre célébration et statistiques utiles. **P1** |
| Attente, erreur, reconnexion et actions en ligne | lobby six sièges, bandeau réseau après échecs et réessai idempotent, reprise testée | Indiquer clairement « en cours », « reçu », « appliqué », « attente des autres » ou « à réessayer » | Ne jamais présenter une action comme appliquée avant l’état autoritaire; conserver un état d’attente calme et une erreur actionnable. **P0** |

Les publications centrales couvrent notamment `dice`, `turn`, `move`, `purchase`, `pass`, `rent`, `upgrade`, `power`, `family`, `investment`, `tax`, `casino`, `cycle`, `transit`, `audit`, `venture`, `marketplace` et `bankruptcy`. Cette couverture textuelle est un bon point de départ, mais elle ne garantit ni une animation dédiée ni que l’information ait le bon niveau de priorité à l’écran.

## Concept concret du lancer de dés

À réaliser comme séquence de présentation, jamais comme nouveau calcul du hasard :

1. **Prêt à jouer :** le bouton de lancer reste l’action primaire; une pression donne un retour immédiat. En mode en ligne, l’état « lancer envoyé/en attente » peut démarrer l’anticipation, sans annoncer de résultat.
2. **Anticipation courte :** deux faces dans une petite zone proche du HUD ou du pion actif, accélération visuelle contrôlée et quelques changements de faces. Cible initiale : environ 450–650 ms, interrompable si la réponse serveur arrive plus tôt; pas de simulation physique, pas de particules ni de caméra secouée par défaut.
3. **Résultat autoritaire :** les faces s’arrêtent sur les valeurs de `OnDiceRolled`, avec un settle/rebond discret et le total de pas. L’animation n’invente ni ne retarde la commande et ne change aucun dé.
4. **Lecture du déplacement :** le pion suit son trajet existant; un accent sobre relie les cases parcourues, puis l’arrivée est signalée.
5. **Décision suivante :** le panneau de case s’ouvre ou se met en avant après l’arrivée; un seul retour prioritaire est visible à la fois. Le feed reste trace secondaire.

**Mode réduit :** suppression de l’anticipation et des rebonds décoratifs, mais affichage instantané et contrasté des dés réels, du total et de l’arrivée. **WebGL/mobile :** réutiliser une UI Toolkit existante, deux éléments dés et transitions USS/C# bornées, zéro dépendance nouvelle, aucun canvas de rendu 3D. Faire une mesure sur appareil avant toute variante plus coûteuse. Cette proposition est à prototyper/recetter, pas déclarée déjà présente.

## Système transversal de feedback à mettre en place

- **Action :** le joueur voit une action primaire, son coût/conséquence utile et sa disponibilité; le reste est secondaire.
- **Confirmation :** état pressé/en attente visuellement distinct. Pour le multijoueur, « envoyé » et « confirmé par l’état partagé » doivent être deux états différents.
- **Résultat :** valeur et acteur sont lisibles avant l’effet décoratif : gain/perte, propriétaire, cible, dé, choix ou erreur.
- **Conséquence :** animer uniquement l’objet concerné (solde, propriété, pion, carte) plutôt que faire pulser tout le HUD.
- **Priorité des messages :** un événement critique (dés, décision, perte, faillite, victoire, résultat de marché) ne doit pas être immédiatement remplacé par un message secondaire. Les messages répétitifs sont regroupés ou restent dans l’historique, pas empilés au-dessus de la partie.
- **Palette sémantique cohérente :** vert = positif confirmé, rouge/orange = coût/risque/erreur, or = patrimoine, couleur de siège = identité. Toujours ajouter un libellé ou symbole, ne pas faire dépendre le sens uniquement de la couleur.
- **Accessibilité :** respecter « Réduire les animations », contraste, zones tactiles, lisibilité des nombres et ordre de focus/clavier.

## Inventaire d’écrans à recetter

1. Accueil, connexion, code de partie, roster 2–6, attente et départ.
2. Plateau en phase d’attente, à son tour, pendant lancer/déplacement, sélection Marketplace et case spéciale; portrait, paysage, zones sûres.
3. HUD : capital, valeur nette, tour, marché, feed d’activité, statut local et réseau.
4. Bottom sheet de propriété : libre/possédée, achat, passer, loyer, stats, panneau réduit pour sélectionner sur le plateau.
5. Pouvoirs : disponibilité, choix de cible, cibles inéligibles, confirmation, refus et résultat.
6. Finance : investissement, casino, impôts, cycles, coûts et revenus.
7. Marketplace : hub, sélection propriété, sélection cible, invitation, attente, formulaire, spectateur, liste, revue, accepté/refusé/expiration, erreur/reconnexion.
8. Venture : tirage, texte court/long, choix de cible, confirmation et résolution.
9. Insolvabilité : dette, sélection/liquidation d’actifs, faillite.
10. Victoire : condition, gagnant, bilan et sortie/rejouer.
11. Menu en partie, quitter/abandon confirmé, retours d’erreur, états vides/chargement.

La vérification UXML/Editor n’est pas une capture visuelle, et une capture statique ne valide pas le timing d’une séquence. Pour chaque état, la recette finale doit combiner contrôles structurels déjà présents, interaction WebGL réelle, capture avant/après et profilage ciblé.

## Ordre de réalisation le plus efficace

**Étape A — grammaire commune et jet de dés (P0, petit lot).** Définir une seule petite API/convention de feedback de présentation, puis prototyper le lancer et la séquence de tour jusqu’à l’inspecteur. Rejouer local/online, réponse lente et mouvement réduit. C’est la boucle la plus fréquente et celle qui répond directement au retour des testeurs.

**Étape B — retours économiques et lisibilité des événements (P0/P1, systèmes groupés).** Regrouper les événements par feedback commun (gain/perte, choix/confirmation, pouvoir/cible, activité en attente). Réutiliser le HUD/feed, l’inspecteur et les modales; pas une animation sur mesure par type si le résultat est équivalent. Prioriser achat/loyer/taxe, ciblage, Marketplace/OPA et insolvabilité.

**Étape C — audit visuel runtime de tous les panneaux (P0, parcours de recette).** Construire un parcours déterministe qui visite l’inventaire ci-dessus sur une même build, en portrait et paysage, avec deux et six clients quand l’état dépend du réseau. Consigner les écrans dont le panneau masque le plateau, la prochaine action, ou une info essentielle.

**Étape D — polish, accessibilité et performance (P1/P2).** Ajuster durées, espacements, couleurs sémantiques et transitions face au mockup; comparer mouvement activé/réduit; mesurer cadence, mémoire/chargement et toucher sur iPhone 14 Pro Max et Android représentatif. Ne retenir un effet que s’il améliore la compréhension/plaisir sans régression.

Cette séquence est plus efficace qu’un chantier écran par écran indépendant : le lancer, le feed et le changement de panneau sont conçus/validés une fois comme boucle complète, puis la grammaire est réutilisée sur des événements apparentés. Elle réduit doublons et retours de mise en page; le compromis est que la recette de chaque état reste obligatoire, même lorsque son animation réutilise un composant commun.

## Critères de clôture

- Chaque phase indique sans ambiguïté **qui joue, ce qui vient de se passer et quelle action est attendue**.
- Chaque action a un état disponible / pressé / en attente / réussi / refusé cohérent; aucun état en ligne n’annonce un succès avant confirmation autoritaire.
- Les événements critiques ont un retour visible et prioritaire; les messages secondaires restent accessibles sans interrompre la boucle.
- L’anticipation des dés finit exactement sur le résultat réel; le parcours déplacement → arrivée → décision reste synchronisé et non bloquant.
- Tous les panneaux de l’inventaire sont joués dans le build WebGL aux formats ciblés; les étapes risquées sont répétées avec 2–6 clients.
- Mouvement réduit, lisibilité mobile, clavier, FPS et temps de chargement sont validés; aucune régression mesurée.
- Les captures cibles/mockup sont comparées dès qu’elles sont accessibles dans les fichiers de la session.

## Hors périmètre

Pas de modification des règles, formules économiques, probabilités, propriétés, conditions de victoire, protocole réseau ni contenu de carte. Les décisions de design qui changeraient le temps de tour ou la disponibilité d’une action doivent rester hors de cet audit visuel.
