# Pulse — Stock Market, Casino, Marketplace

Proposition du 18 octobre 2026, en attente de validation visuelle par Arthur. Livrable de cette passe : trois panneaux interactifs et leurs parcours de décision. Aucun fichier runtime, règle, protocole réseau ou build du jeu n’est modifié.

Source de preview : `/Users/arthurbarreau/.codex/visualizations/2026/10/17/01a1498f-6c87-7e31-b929-4d90b504495c/pulse-operations.html`.

## Cadre commun

- DA Pulse approuvée : fond `#0b0e16`, surface `#111620`, séparateurs `#2a303b`, texte `#f2f3f5`, secondaire `#a1a6b2`, vert `#13db83`, ambre `#edb95c`, rouge `#f16b77`.
- La fiche compacte permanente du jeu reste la référence. Le panneau d’opération s’ouvre uniquement sur une action explicite : Investir, Casino, Ouvrir Marketplace. Il ne s’ouvre ni ne se déplie automatiquement à chaque événement adverse.
- Même coque, en-tête, zone de contenu et emplacement des actions dans les étapes d’une opération. Sélection, attente, examen et reçu ne changent pas la hauteur de la coque. Retour volontaire à la fiche compacte.
- Dans la maquette, la coque détaillée mesure 720 px, 780 px sous 380 px : ce sont des dimensions d’exploration, pas des valeurs à appliquer directement au PanelSettings Unity. Pour le runtime, dimensionner selon l’espace réellement disponible après HUD et safe areas ; contenu central défilable si nécessaire, actions accessibles en permanence. Éviter d’empiler ces dimensions sur une fiche qui serait restée active derrière.
- Le plateau fourni en contexte provient d’une capture réelle du jeu. Les choix, portefeuilles, positions et résultats de la maquette sont des scénarios fictifs indépendants. Les contrôles Rejouer, Scénario, Résultat démo et Simuler les réponses appartiennent à la preview ; ne pas les intégrer au jeu.
- Pas de nouveau bandeau de statut permanent. Les événements publics continuent d’utiliser la scène centrale et la file chronologique validées dans PulseFixedV3. L’inspection libre reste réservée à son propre tour.

## Bourse

### Lecture et choix

1. Cycle courant et Sgo dans le sous-titre.
2. Horizons 1, 3, 5 tours, avec bornes de rendement lisibles sur chaque option.
3. Montant immobilisé mis en avant. Préréglages actuels conservés : 1× Sgo, 5× Sgo, Maximum. Maximum tient compte des liquidités et du plafond.
4. Intervalle de **capital récupéré**, séparé du gain net. Le repère dans la bande indique le capital initial, pas une probabilité.
5. Risque de perte totale explicite pour 5 tours, séparé de l’intervalle hors perte totale.
6. Liquidités restantes immédiatement avant le bouton de confirmation.
7. Onglet Positions : capital engagé, durée initiale et tours restants ; aucun cours de Bourse fictif ni profit latent inventé.

### Données et commandes

`FinanceSystem.InvestInStockMarket`, `GetActiveInvestmentsForPlayer`, `CurrentSgo`, `CurrentMarketState` ; préréglages et résolution du montant actuellement dans `ActionButtonsWidget`.

| Durée | Plafond | Bornes de base | Perte totale |
|---|---|---|---|
| 1 tour | 15 × Sgo | −30 % / +40 % | pas de branche dédiée |
| 3 tours | 15 × Sgo | −30 % / +70 % | pas de branche dédiée |
| 5 tours | 20 × Sgo | −20 % / +120 % | 20 % |

Utiliser les méthodes de `CycleManager` pour les shifts de bornes et la probabilité de perte totale. BOOM ajoute 20 points aux deux bornes. CRASH : 1T −50/+20, 3T −50/+42, 5T −40/+72 ; perte totale 35 % à 5T. La projection dépend du cycle actuel, alors que le moteur utilise le cycle à l’échéance. Ne jamais présenter les bornes comme verrouillées dès l’ordre.

L’état confirmé montre le capital immobilisé et les liquidités restantes ; il ne révèle pas le résultat futur. En ligne, garder `stock.invest` et son DTO existant. Ne pas afficher un succès à la seule émission de la commande : attendre l’état autoritatif. Solde nul, montant nul, commande en attente, refus ou changement de contexte doivent préserver une interface explicite et empêcher les doubles envois.

## Casino

### Point à valider avant runtime

Le moteur sait résoudre trois profils, mais le parcours humain actuel dans `RulesEngine.ExecutePrimaryAction` appelle directement **Standard, mise 1 500 $**. La preview conserve cette mise et expose les profils comme **proposition de nouvelle sélection UI**. Leur intégration, surtout en multijoueur où l’action primaire ne transporte pas ce choix, demande une décision explicite ; ne pas ajouter silencieusement un nouveau protocole ni un réglage de mise.

| Profil moteur | Probabilité de gain | Crédit si gain, mise comprise | Gain net pour 1 500 $ |
|---|---|---|---|
| Safe / Prudent | 80 % | ×1,25 | +375 $ |
| Standard | 50 % | ×1,80 | +1 200 $ |
| HighRisk / Élevé | 25 % | ×3 | +3 000 $ |

Dans les trois cas, un échec perd la mise entière. « Prudent » ne signifie pas garanti.

### Parcours

- Choix : profil, mise, probabilité, gain net possible, perte possible et cash après mise.
- Confirmation : une seule soumission. Contrôle désactivé pendant le traitement, sans disparition de la coque.
- Révélation : courte respiration graphique, puis résultat confirmé. Le montant principal est le **bénéfice ou la perte nette** ; le crédit brut, mise comprise, reste explicite dessous.
- Reçu : Retour à la case. Pas de bouton Rejouer dans le produit après une arrivée déjà résolue.
- Capital insuffisant : mise désactivée, explication et Passer disponibles.
- Si Arthur conserve le parcours existant, retirer le sélecteur des risques et présenter Standard uniquement avec la même hiérarchie.

Pas de roue de roulette ni de machine à sous : elles suggéreraient une règle différente du tirage du moteur. Ne pas truquer une animation pour simuler un presque-gain. Le résultat du moteur précède la révélation visuelle ; l’animation ne relance jamais le hasard.

## Marketplace

### Vendeur

Portefeuille en cartes compactes sélectionnables, fiche de l’actif choisi, puis lecture symétrique « Vous proposez / Ils peuvent offrir ». Mise en vente d’une propriété ; offres adverses en cash, propriétés, ou les deux.

Étapes : sélection → invitation → attente des réponses → comparaison → vérification de l’échange → reçu. Toutes dans la même coque. L’attente affiche le nombre réel de réponses, sans compte à rebours inventé. Le vendeur humain choisit l’offre ; aucun choix automatique de la plus élevée.

Les offres montrent séparément le cash, les propriétés reçues et leur valeur de base totale. La valeur de base sert de comparaison, pas de prix garanti. À la vérification, afficher côte à côte les biens cédés et reçus, le partenaire et le cash restant. Refuser toutes les offres reste disponible selon le flow existant.

### Enchérisseur

Actif vendu et vendeur, offre en cash, jusqu’à deux propriétés ajoutées, valeur de comparaison et envoi. Une offre doit contenir du cash ou au moins une propriété. Le statut adverse LOW/MID/HIGH/MOGUL remplace ses liquidités exactes. Après envoi, reçu puis vue d’attente ou de revue autorisée. Aucune modification des offres une fois les enchères closes.

Le slider de cash correspond à une fonction déjà existante ; il ne constitue pas une confirmation de transaction par glissement. Conserver une alternative clavier et une valeur lisible.

### Spectateur

Vue en lecture seule indiquant qui doit décider et le stade de l’échange, sans boutons d’envoi ou d’acceptation. Le formulaire d’enchère autorisé pendant le tour d’un autre joueur constitue une interaction de Marketplace ; il ne doit pas réactiver l’inspection libre du plateau.

### Sanctions

Grille 2 × 2 pour le choix de l’effet, cible éligible, résumé de l’impact, coût en dollars et Sgo, cash après coût et confirmation nominative. Éviter une grande liste verticale où le joueur perd la relation effet/cible.

| Sanction | Coût | Cible | Effet implémenté |
|---|---|---|---|
| Audit ciblé | 1 × Sgo | Joueur rival | 5 % de ses liquidités à la banque |
| Embargo | 5 × Sgo | Propriété | Aucun loyer pendant 3 tours |
| Grève | 2 × Sgo | Propriété appartenant à un rival | Double les coûts de son propriétaire au prochain GO |
| Sabotage | 5 × Sgo | Propriété améliorée | −1 niveau, Aerospace protégé |

Attention : l’ancien libellé de Grève parle d’une famille ciblée, alors que le code pose `owner.DoubleBurnRateNextGo`. La proposition suit l’effet réellement implémenté et ne réduit pas cet effet à une seule famille.

Utiliser l’éligibilité et les protections réelles de `Marketplace_UIManager` / `MarketplaceSystem` : immunités Pharma, Golden Ticket, Aerospace, niveau, propriétaire, solde. Ne pas révéler le cash adverse pour calculer à l’écran le montant de l’audit. Ne pas consommer une protection pendant une simple preview.

Préserver les états d’enchère, la sélection de biens sur le plateau, la restauration de snapshot et les rôles après reconnexion. Ne pas remplacer ces mécanismes par les scénarios locaux de la maquette.

## Motion et intégration

- Ouverture volontaire : glissement doux d’environ 220–260 ms, sans bouger le HUD.
- Sélection : couleur et bordure en 120–180 ms ; pas de redimensionnement des cartes.
- Confirmation : contenu local remontant de 7 px sur 280 ms ; léger appui puis retour du chiffre (0,96 → 1,016 → 1, 420 ms). Aucune mise à l’échelle de la coque.
- Contour : impulsion unique de 650 ms. Investissement, offre ou accord confirmés verts ; paiement et coût de sanction rouges ; passer ambre. Toujours un libellé, pas une information portée seulement par la couleur.
- Casino : révélation visuelle courte, 750 ms dans la maquette ; aucune temporisation ajoutée aux règles du jeu.
- Réduire les animations : résultat et texte immédiatement accessibles ; pas de translation, pulsation, balayage ni oscillation. Pas de son automatique dans la preview.
- Garder les données dans les systèmes existants et ne reconstruire que la présentation. Pas de dépendance ajoutée, pas de logique métier copiée dans les panneaux.

## Vérification de la proposition

La preview HTML ne valide pas l’intégration Unity/WebGL. Les vérifications navigateur concernent uniquement ses parcours et sa géométrie. Tester ensuite le runtime sur petits écrans et paysage : HUD intact, actions visibles, ouverture/fermeture volontaire, refus et attente réseau, double clic, reconnexion, zéro cash, pas d’actifs/cibles éligibles, enchères mixtes cash + deux biens, protections, passages de tour et réduction des animations.

Vérifications effectuées dans la preview : sélection Bourse 5T/Maximum, limite par cash et avertissement de perte totale, confirmation d’immobilisation ; Casino Standard gagné et Élevé perdu, bénéfice net distinct du crédit brut ; vente et offre mixte 600 $ + Fentanyl Labs, vérification et accord ; formulaire enchérisseur, offre vide désactivée, offre avec bien seul transmise ; sélection Grève, confirmation Sabotage et vue spectateur. Largeurs de coque observées : 480 et 305 px, sans débordement horizontal dans les vues étroites testées. Hauteurs communes finales : 720 et 780 px ; retour volontaire compact observé à 236,4 px. Syntaxe JavaScript vérifiée avec Node, aucun avertissement ni erreur console observé sur la dernière vérification.
