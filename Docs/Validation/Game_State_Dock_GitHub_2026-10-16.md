# PolyRisk — état de partie lisible et recherche GitHub

**Date :** 16 octobre 2026. **Périmètre :** présentation et compréhension des actions, sans changement de règles, d’économie ni de protocole en ligne.

## État des lieux du HUD et du fil d’activité

- `TopHUD.uxml` place CAPITAL, NET WORTH, TURN et MARKET dans une barre haute de 64 px. Elle décrit l’état patrimonial, mais pas l’acteur actif, la phase de son tour ni la prochaine action attendue.
- `ActivityFeed` est un calque absolu juste sous le HUD. Il ne réserve pas de place dans la mise en page, affiche au plus deux messages en format large et un seul en format étroit, puis les retire après environ 4,2 s (5,6 s pour une emphase). C’est adapté au toast, pas à une explication persistante de la partie.
- Le statut personnel (actuellement immunité Pharma) partage le conteneur du feed. Une erreur réseau peut apparaître ailleurs encore. Le sens de « statut de partie », « évènement passé » et « erreur nécessitant une action » est donc réparti sur plusieurs endroits.
- `GameActivityEntry` contient `id`, `kind`, `playerId`, `text` et `emphasis`. Le fil traduit le texte à l’affichage et identifie l’auteur, mais le modèle ne porte pas de champs dédiés pour la cible, le payeur, le bénéficiaire, le montant reçu ou la confirmation. Exemple réel : l’activité de loyer dit que le joueur actif paie un montant pour une propriété, mais pas qui reçoit l’argent. En cas de revenu réparti, un libellé seul est encore moins suffisant pour afficher correctement le transfert.
- `OnBalanceChanged` informe le HUD du solde local; ce n’est pas à lui seul une preuve d’un transfert donné entre deux sièges. Une ligne plus explicite doit être fondée sur le résultat effectivement confirmé par la simulation/état répliqué, pas sur une déduction approximative du texte.
- La limite UX observée est donc structurelle : une information éphémère est forcée à jouer le rôle de statut permanent. Sur mobile elle se fait remplacer rapidement et se trouve loin des commandes du bas. Le joueur doit recoller les chiffres HUD, le pion et le panneau flottant lui-même.

## Proposition : un « Dock de partie » au-dessus des commandes

Séparer clairement trois fonctions, tout en les reliant visuellement :

1. **Barre haute = tableau de bord.** Conserver les quatre métriques utiles et l’accès menu. Ne pas y empiler les notifications.
2. **Dock de partie = ce qui se passe maintenant.** Un seul bandeau compact et persistant, ancré juste au-dessus du panneau inférieur/inspecteur, dans la zone proche des actions. Il reprend la couleur/avatar du siège, un libellé de phase (« À TOI », « AU TOUR DE LÉA », « EN ATTENTE »), une phrase d’état et, si c’est réellement l’action attendue, un raccourci vers le bouton existant. Il se replie à une ligne quand l’inspecteur est développé, sans masquer la case choisie.
3. **Journal = ce qui vient de se passer.** Le dock peut s’ouvrir en panneau/historique des dernières actions, à la demande. Les entrées y restent consultables au lieu de disparaître après 4 secondes; les répétitions peuvent se regrouper. Les notifications très brèves restent seulement pour les confirmations secondaires.

Le Dock ne doit pas créer un second bouton de jeu. Il renvoie/focalise le contrôle principal existant. Priorité à l’information, puis à l’effet : acteur et résultat avant l’animation; réduction de mouvement garde la même hiérarchie et les mêmes états.

### Exemples d’états lisibles

| Situation réelle | Message principal pour le joueur local | Traitement |
|---|---|---|
| C’est son tour, avant le lancer | **À TOI · Lance les dés** | État persistant, cible visuelle vers le bouton de lancer déjà présent |
| Un autre joueur joue | **AU TOUR DE LÉA · Lancer les dés** (ou phase actuelle exacte) | Identité de siège visible; calme, pas de compte à rebours inventé |
| Un joueur vient de lancer | **LÉA · 4 + 3 = 7 cases** | Résultat confirmé; animation de dés puis historique conservé |
| Un joueur achète | **LÉA achète “Tour Atlas” · 1 300 $** | Montant et propriété, état confirmé, changement du pion/propriété coordonné |
| Le joueur local paie un loyer | **TU PAIES 1 200 $ À LÉA · Loyer de “Tour Atlas”** | Bandeau prioritaire, rouge/orange + montant, delta du capital synchronisé |
| Le joueur local reçoit un loyer | **LÉA TE VERSE 1 200 $ · “Tour Atlas”** | Vert + identité du payeur, montant raccordé au HUD |
| Une action réseau est envoyée | **Action envoyée…** puis **Action confirmée** (ou erreur/réessayer) | Ne jamais confondre envoi avec application autoritaire |

Ces textes sont des exemples de présentation à valider contre les résultats exacts du code. Si le flux est automatique, le Dock dit ce qui a été payé/reçu; s’il existe réellement un choix ou une liquidation à faire, il le dit comme décision attendue. Il ne faut pas afficher « tu dois payer » ou des boutons fictifs si la règle a déjà réglé la transaction.

## Données de présentation nécessaires

Pour un état précis et sûr en multijoueur, le Dock doit consommer une petite représentation typée plutôt que parser des phrases localisées : `category`, `actorSeat`, `targetSeat`/bénéficiaires, `propertyId`, montants effectivement transférés, état (`pending/confirmed/failed`) et identifiant d’événement. Les messages affichés restent générés à la couche UI selon la langue. L’état de tour/phase vient de la projection de jeu existante.

Le premier prototype doit couvrir un parcours à faible risque : tour courant → dés → déplacement → achat/passe, puis loyer payé/reçu et attente joueur distant. Avant de choisir où transporter les métadonnées, examiner les transactions et snapshots en ligne afin de réutiliser une preuve d’effet déjà disponible. Ne pas recalculer les parts de loyer dans l’UI et ne pas introduire une nouvelle règle. Si une donnée n’existe pas côté client, l’ajouter au modèle de présentation de manière déterministe et compatible, et non reconstituer le résultat depuis une chaîne.

## Recherche GitHub : technique utile et compatibilité

| Repo | Domaine | Intérêt pour PolyRisk | Décision |
|---|---|---|---|
| [Unity-Technologies/ui-toolkit-manual-code-examples](https://github.com/Unity-Technologies/ui-toolkit-manual-code-examples) | UI Toolkit natif : runtime, ListView, liaisons, transitions | Référence la plus sûre pour construire le Dock/historique en UXML/USS et garder un feed virtualisable si l’historique s’allonge. Le dépôt indique que plusieurs exemples orientés Editor fonctionnent aussi au runtime. | **Réutiliser les patterns seulement.** L’UI Toolkit est déjà la base du projet; pas de dépendance. Pour un historique borné de quelques lignes, une ListView n’est pas automatiquement un gain. |
| [Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo](https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo) | Démo officielle de UI Toolkit runtime | Référence pratique sur l’intégration UXML/USS d’une UI de jeu. Sa fiche indique un projet de démonstration testé sur une ancienne version Unity, donc utile pour les idées d’architecture, moins comme code à copier tel quel dans 2022.3. | **Étudier à la demande**, ne pas importer ses packages/dépendances de démo. |
| [KyryloKuzyk/PrimeTween](https://github.com/Kyrylokuzyk/PrimeTween) | Tween UI, UI Toolkit et séquences | Le README annonce l’animation de UI Toolkit et des séquences, intéressant pour orchestrer dock → dés → résultat si les transitions USS/C# actuelles deviennent difficiles à coordonner. | **Évaluer, pas intégrer d’emblée.** La construction actuelle sait déjà animer les transitions courantes; valider package, version Unity 2022.3, WebGL et mouvement réduit avec un prototype avant toute dépendance. |
| [instance-id/ElementAnimationToolkit](https://github.com/instance-id/ElementAnimationToolkit) | Helpers d’animation UI Toolkit | Exemples de séquences et d’éléments prêts à animer. | **Inspiration seulement.** Le dépôt signale lui-même que l’API UIElements est expérimentale et que le package est en développement peu actif. Éviter ce risque pour une UI de release. |
| [vobbbb/vui](https://github.com/vobbbb/vui) | Navigation de modales et notifications prioritaires | Bonne idée de file à priorités, anti-doublons et préemption : utile pour ne pas laisser un événement de loyer/faillite disparaître derrière un message de tour. | **Copier le principe, pas le framework.** Le repo décrit un système générique Unity (pile UI/popups) sans démonstration ciblée UI Toolkit/PolyRisk; le projet a déjà ses modales et ses flux. |
| [sinanata/unity-ui-toolkit-design-system](https://github.com/sinanata/unity-ui-toolkit-design-system) | Design system sombre UI Toolkit | Peut inspirer la densité, les composants, les tokens et la lisibilité d’un HUD sombre de jeu. | **Ne pas importer dans la version actuelle.** Le dépôt annonce Unity 6 et utilise des ajouts USS qui ne sont pas tous disponibles en Unity 2022.3. Réutiliser seulement les idées visuelles après comparaison au thème PolyRisk. |

Les repos de notification « toast » ou de dés en Rigidbody ne sont pas retenus pour cette demande : ils résolvent un toast éphémère ou un lancer physique isolé, tandis que PolyRisk a déjà un résultat autoritaire, un fil UI Toolkit et un déplacement répliqué. Le rendu peut être plus satisfaisant sans transférer la mécanique à une dépendance inadaptée.

## Ordre de travail recommandé

1. **Prototype sans modifier la logique :** composer une maquette du Dock aux dimensions réelles du panneau bas, portrait et paysage, avec six noms longs et les exemples ci-dessus. Vérifier qu’il ne cache ni case sélectionnée ni action.
2. **Modèle de lecture :** établir pour chaque état si l’information vient de l’acteur courant, d’une transaction confirmée ou d’un snapshot; choisir la source des bénéficiaires/montants avant le code UI.
3. **Intégration pilotée :** relier le Dock aux états de tour et aux activités existantes; réserver le journal consultable aux dernières actions, avec priorité « décision/transaction critique > résolution > information de contexte ».
4. **Recette :** parcours local et deux clients d’abord, puis six clients; événements achat/passe/loyer, spectateur en attente, erreur réseau/réessai, portrait/paysage, mouvement réduit; mesurer les FPS WebGL et vérifier que le Dock n’occasionne pas de reconstruction UI à chaque frame.

Le meilleur gain d’efficacité est un composant partagé pour état courant + file d’événements priorisée, testé sur une boucle complète avant d’être branché aux autres systèmes. Cela évite un toast maison par mécanique et les conflits de superposition. Aucun dépôt externe n’a été installé.
