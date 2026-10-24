# PolyRisk — audit de reprise du 25 septembre 2026

## Périmètre et niveau de preuve

Inspection statique des scripts C#, de SampleScene, des assets de propriétés, des GDD, du manifeste Unity, des paramètres de build, de tasks/todo.md et des CSV locaux. HEAD : `86500e5`. Unity déclaré : 2022.3.62f3.

Aucune compilation, partie Unity, validation visuelle ni build navigateur exécuté dans cet audit. « Présent » signifie du code ou des assets identifiés, pas une fonctionnalité validée de bout en bout. La scène comporte des modifications locales préexistantes, conservées : batch 30 parties, vitesse 100, délai 240 s et position d'un objet.

## État réel

| Domaine | Présent | Reste à faire |
|---|---|---|
| Boucle locale | Déplacement, décisions, propriétés, IA, prison/audit, cartes | Fiabiliser fin de partie, dettes, redémarrage et cas limites |
| Économie | Sgo, maintenance, dividendes, upgrades, cycles, capacités, casino et bourse | Corriger la mécanique et la mesure avant de rééquilibrer ; démontrer la trajectoire vers 10 M$ |
| Interface | HUD, bottom sheet, Marketplace, cartes, liquidation, victoire, styles et transitions | Compléter les choix et valider tous les parcours humains |
| Six joueurs | Initialisation par boucle avec nombre configurable | Scène à 4 joueurs ; pas de configuration utilisateur ; banc de test limité à quatre colonnes joueurs |
| Multijoueur humain | Le bootstrap crée uniquement le joueur 0 humain | Gestion des places humaines/IA, identité locale et réseau à construire |
| Web | Projet Unity ; paramètres WebGL génériques | Aucune scène dans les Build Settings ; build, validation navigateur et hébergement à réaliser |
| Produit | Une scène de jeu | Accueil, création/rejoindre partie, aide, paramètres, reprise de session ; audio non trouvé dans les scripts |

Les statuts MISSING/MOCKED des GDD ne décrivent plus fidèlement le code. Les documents spécialisés restent les références de conception, mais leur état d'implémentation est à actualiser.

## 1. Économie : résultats disponibles et limites

Lecture directe des CSV de StressTestResults :

| Batch du 9 septembre | Parties | Résultats | Cash du gagnant, médiane / maximum |
|---|---:|---|---|
| 11:15:39 | 10 | 10 timeouts | Sans gagnant |
| 12:18:49 | 30 | 29 derniers survivants, 1 timeout | 7 525 $ / 140 904,23 $ |
| 14:35:50 | 30 | 30 derniers survivants | 11 657,35 $ / 215 210,39 $ |

Ces fichiers ne démontrent pas une fin de partie proche de 10 M$. Ils précèdent les commits du 10 septembre corrigeant le double déclenchement de résolution IA et ajoutant l'instrumentation. Aucun nouveau CSV postérieur trouvé. Les résumés anciens seuls ne constituent pas une validation actuelle.

### Problèmes à traiter avant un nouvel équilibrage

1. **Maintenance impayée ignorée.** `FinanceSystem.ProcessGoIncome` crédite les revenus puis appelle `DeductMoney(totalDebit)` sans traiter son résultat. `TransactionManager.DeductMoney` refuse un débit non couvert sans débiter ni déclencher une liquidation. Dans ce cas le joueur garde les revenus et ne paie pas la maintenance. Prévoir un paiement obligatoire et une résolution de dette explicite.
2. **Victoire à 10 M$ non centralisée.** Elle émet `OnGameVictory` au passage GO, sans positionner `TurnManager.isGameOver`. Seules les autres branches de victoire arrêtent la boucle. L'écran de victoire n'arrête pas le moteur. Définir également si franchir 10 M$ hors GO doit gagner immédiatement ou attendre le revenu net au GO.
3. **Faillite de tous les joueurs.** `TurnManager.EndTurn` cherche le prochain survivant dans une boucle sans sortie avant de compter les survivants. Si aucun ne reste, cette boucle peut bloquer le thread principal ; le timeout du harness ne suffit pas à interrompre une boucle synchrone. Traiter zéro survivant avant cette recherche.
4. **Valeur nette incomplète.** Le classement au tour 50, le HUD, l'écran de victoire et le CSV utilisent cash + prix des propriétés. Le GDD ajoute 50 % des coûts d'upgrades et 50 000 $ par famille complète. Centraliser une formule conforme à la règle retenue.
5. **Paiement de loyer détourné partiel accepté.** `RulesEngine` effectue séparément les transferts de 25 % et 75 %, puis utilise `paid = s1 || s2`. Un paiement partiel peut donc être considéré comme réglé. Garantir la résolution de la dette totale sans versements incohérents.

### Le banc de test lui-même doit être corrigé

- `EndedOnLap` prend le minimum de tous les joueurs, y compris les éliminés. Il ne mesure pas la durée réelle d'une partie. Le résumé exclut les zéros du minimum, mais les conserve dans le dénominateur de la moyenne : affichage trompeur.
- Le CSV capture seulement P0–P3 et le redémarrage attend au moins quatre joueurs. Adapter aux configurations 2–6 joueurs et préciser que le solo désigne un humain contre IA.
- `PropertyData` mélange configuration et état mutable (niveau, hypothèque, embargo, investissement). `BoardManager` utilise directement les ScriptableObjects ; aucun clonage ou reset complet de ces états n'a été trouvé au démarrage. **Risque de contamination entre parties à reproduire**, notamment avec les assets encore référencés par les contrôleurs persistants. Les niveaux sérialisés sur disque sont actuellement à zéro.
- `DoReset` efface `GameEvents.OnInsolvency`, tandis que `BankruptcyScreenController` persiste via `DontDestroyOnLoad` et s'abonne dans `OnEnable`. Le reset ne le recrée ni ne réabonne explicitement. **Risque majeur de liquidation IA désactivée après reset**, à reproduire et corriger avant d'interpréter les faillites.
- L'ultime Tech augmente gratuitement les niveaux via `SetLevel`, en dehors de `TryUpgradeProperty` : les traces `[UPGRADE]` ne capturent pas toutes les augmentations. Instrumenter aussi les pouvoirs, cartes et transferts.
- Ajouter une graine reproductible, un identifiant de partie, les tours/actions et passages GO par joueur, les pics de cash, la dette déclenchant chaque faillite, les flux monétaires et les niveaux. Séparer temps accéléré et durée attendue d'une partie humaine.

**Validation attendue :** tests ciblés des dettes et fins de partie ; répétition de parties avec état initial identique ; petit batch propre puis campagnes 2/3/4/5/6 joueurs. Mesurer cash final et maximal, causes de victoire, éliminations, durée, contribution des familles et upgrades. Décider ensuite de la proportion souhaitée de victoires à 10 M$ versus dernier survivant : les deux conditions actuelles ne garantissent pas des fortunes finales proches du seuil.

## 2. UI / UX : compléter et tester

### Déjà présent

- TopHUD et fiche de case / boutons d'action.
- Marketplace : choix, sélection, offres, examen, attente, spectateur, notifications et sanctions.
- Modales Venture, liquidation et victoire, avec UXML et contrôleurs.
- Apparence Cyber-Luxury et transitions.

La scène référence notamment TopHUDWidget, ActionButtonsWidget, Marketplace_UIManager, CycleManager et AbilitySystem. Les trois contrôleurs de modales sont prévus pour une création automatique. Leur présence en code ne valide ni leurs références effectives ni le rendu.

### Manques concrets

- **Casino :** bouton disponible, mais `RulesEngine` mise un montant fixe de 1 500 $ en risque Standard. Ajouter sélection du montant/risque, résultat et retour au tour.
- **Bourse :** bouton disponible, mais investissement fixe de 1 000 $ sur trois tours. Ajouter montant, durée, plafonds, portefeuille et échéances.
- **Pouvoirs :** le bouton humain choisit la première famille disponible dans une liste fixe. Ajouter un choix explicite lorsque plusieurs ultimes sont possibles et les cibles nécessaires. Aerospace porte encore un TODO de sélection de destination ; son choix actuel est automatique.
- **Enchères après refus d'achat :** AuctionManager existe, mais n'est pas attaché dans SampleScene ; aucun appel à PlaceBid/EndAuction ni abonné à OnAuctionStarted trouvé dans les autres scripts. Ce parcours ne doit pas être confondu avec les offres de la Marketplace, qui ont leur propre système. Compléter le parcours ou formaliser une règle sans enchère.
- **Nouvelle partie :** le bouton victoire recharge la scène, alors que son contrôleur persiste et conserve `isShown`. Les singletons persistants et l'état des propriétés nécessitent une vraie réinitialisation. Ajouter un scénario victoire → rejouer → seconde victoire.
- **Identité :** HUD fixé au joueur 0 ; Marketplace peut choisir le premier humain. Remplacer par une identité locale pour le jeu à plusieurs.
- **Accueil et session :** pas de parcours identifié pour choisir les participants, créer/rejoindre une salle, reprendre une partie ou régler le jeu.
- **Finition :** clarté des règles, retours d'erreurs, aide, audio, petits écrans, redimensionnement navigateur, tactile et zones sûres à valider ; pas de prise en charge explicite de safeArea trouvée dans les scripts.

### Recette humaine nécessaire

Tester achat/refus/amélioration ; loyer payable et insolvabilité ; liquidation avec paiement puis abandon ; cartes positives/négatives et cycles ; audit/paiement/sortie ; casino et bourse ; sélection/activation des pouvoirs ; Marketplace humain vendeur et enchérisseur, acceptation/refus/aucune offre ; victoire et rejouer. À chaque étape : ouverture correcte, montant exact, absence de double action, fermeture et reprise du bon joueur. Répéter après changement de taille d'écran et sur la version navigateur.

## 3. Jeu entre amis, jusqu'à six

`GameBootstrapper` fixe `isAI = ForceAllAI || (i != 0)` et la scène utilise quatre joueurs. Aucune couche réseau applicative, dépendance Netcode/Photon/Mirror, lobby ou gestion de connexion n'a été trouvée. La mention PvP en ligne est une intention du GDD.

Travail requis pour une première version privée :

1. Configuration de 2–6 places, nom/pion, humain/IA ; un humain peut jouer seul contre IA.
2. Salon privé avec code ou lien, rejoindre/quitter, prêt et lancement par l'hôte.
3. Autorité de partie qui valide les commandes, tire les dés/cartes et publie l'état ; empêcher achats ou actions hors tour et doubles soumissions.
4. Synchronisation plateau, finances, capacités, offres simultanées, timers et fin de partie ; informations privées adaptées à chaque joueur.
5. Déconnexion/reconnexion, état complet de reprise et politique explicite si l'hôte quitte ; délai ou remplacement IA pour un joueur absent.
6. Recette sur plusieurs clients réels, jusqu'à six, incluant déconnexion pendant une offre et une liquidation.

Le choix du transport/hébergement n'est pas arrêté dans cet audit. Il devra tenir compte du client navigateur. Pas besoin de matchmaking public pour une première session entre amis.

## 4. Version web

- `EditorBuildSettings.asset` contient `m_Scenes: []` : configuration de build à compléter.
- Installer/vérifier le module WebGL de l'éditeur utilisé, produire un premier build et traiter ses erreurs réelles. La présence de champs WebGL dans ProjectSettings ne prouve pas qu'un build fonctionne.
- Vérifier chargement des UXML/USS, polices, matériaux, performance URP, entrées clavier/tactile, redimensionnement et perte de focus dans le navigateur.
- Exclure les outils de debug de la version distribuée : raccourcis Space/M, cartes forcées 0–9, F1/F2 et F9. La distribution de propriétés de DebugMarketplaceSetup se produit avant le bloc de compilation conditionnelle qui protège la téléportation.
- Préparer l'hébergement du client et le service réseau, puis tester le lien sur les machines des amis. Un client web publié seul ne fournit pas le multijoueur.

## Ordre proposé et critères de sortie

1. **Fiabilité du moteur et des mesures.** Corriger les dettes, fins, reset, valeur nette et instrumentation ; tests ciblés réussis et batches sans contamination démontrée.
2. **Économie mesurée.** Comparer 2–6 joueurs, expliquer les faillites et trajectoires monétaires ; ajuster les paramètres seulement sur des données fiables.
3. **Partie humaine locale complète.** Compléter les choix UI et réussir la recette de bout en bout, y compris rejouer.
4. **Prototype WebGL tôt.** Vérifier les contraintes navigateur avant d'investir dans le réseau ; finitions visuelles finales après validation du parcours.
5. **Salon privé et multijoueur.** Identité locale, autorité, synchronisation, reconnexion et six clients.
6. **Session entre amis.** Déployer un build privé testé, récupérer des retours de durée, compréhension et plaisir de jeu ; ensuite polish graphique/audio et mobile.

Cet audit ajoute uniquement ce document. Aucun correctif de gameplay ni changement des paramètres économiques n'a été appliqué.
