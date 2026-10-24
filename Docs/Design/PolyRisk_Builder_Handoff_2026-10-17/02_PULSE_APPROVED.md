# UI/UX approuvée — Pulse Dark Luxury Néon

## Autorité visuelle

Arthur a choisi Pulse puis validé la dernière itération **`pulse-inspecteur-visuel.html`**. Référence figée dans `previews/pulse-approved.html`, source dans `sources/pulse-approved.fragment.html`. Cette version remplace les anciennes listes et les deux blocs de specs/finances séparés.

La palette finale reste celle du jeu : sombre, néon vert, ambre, familles colorées. La menthe de la première proposition Pulse ne remplace pas cette décision. Les autres directions Arena / Atelier sont écartées. Ne pas remettre les anciennes capsules qui tronquaient les noms ni le bandeau redondant au-dessus de la fiche de case.

## Ce qui est validé

- Lecture Pulse : HUD hiérarchisé, capital et patrimoine distincts, tour et état du marché, joueurs identifiables et joueur actif explicite. Couleur **et** texte/numéro, jamais la couleur seule.
- Palette sombre, minimaliste, technique, luxury : éclairage / accents ponctuels, pas de glow permanent sur tous les éléments.
- Cockpit en bas, deux crans : compact pour jouer ; étendu pour inspecter et gérer. Poignée et commande explicite « Déplier / Réduire ». Drag facultatif, clic/tap et clavier disponibles.
- Identité de la case : icône de famille, nom, famille, niveau, état de possession et progression de famille possédées/total. Le bouton de famille ouvre les cartes.
- **Un seul bloc de specs et finances** : loyer par visite en valeur principale, rendement secondaire ; risque « non renseigné » s'il n'existe pas. Sous un séparateur discret : prochain GO, coûts, net estimé. Le détail financier s'ouvre en cliquant cette ligne.
- Inspecteur : trois cartes dans l'exemple, une par propriété de la famille. Sélection visible, statut « À vous » ou prix d'acquisition, niveau, nom lisible. Cliquer met à jour la fiche et les actions. Ce n'est plus une liste de lignes.
- Portefeuille : cartes de familles, nombre possédé/total et emplacements visuels détenus/manquants ; clic vers les propriétés. Les nombres réels ne sont pas forcément trois.
- Vues étendues : Inspecteur, Portefeuille, Flux GO. Conserver la sélection lors des changements de cran ; revenir à la case de départ explicitement quand on inspecte une autre propriété.
- Acheter avec prix visible et Passer secondaire. Propriété déjà détenue : ne pas proposer de l'acheter. Les commandes doivent refléter les actions réelles disponibles, sans supprimer les améliorations, pouvoirs, marché, audit ou autres fonctions existantes.

## Coque Pulse et état du jeu

L’itération finale du cockpit simplifie le HUD pour se concentrer sur le bas d’écran. Pour la coque et les reçus d’actions, conserver les choix Pulse de l’exploration d’origine : capital dominant, patrimoine secondaire, tour/marché sur une ligne calme ; rail joueurs intégré au HUD, noms lisibles, numéro/couleur et marqueur sous le joueur actif. Pas de grandes capsules tronquées.

Les événements confirmés apparaissent dans une petite carte au-dessus du plateau : acteur, action, destination ou montant/résultat, conséquence locale si utile. Exemple de structure : « Player 2 · Achat confirmé / Achète [propriété] / −[prix] ». Pour un déplacement : somme confirmée et destination réelle. Ne pas répéter cette information dans un bandeau au-dessus de la fiche. Journal consultable pour retrouver les reçus ; ne pas bloquer les actions par une durée d’animation.

Entrée indicative du reçu : 230 ms, déplacement vertical 7 unités, Bézier (0.2, 0.8, 0.2, 1) ; mode réduit sans translation. Séparer joueur actuellement actif et auteur de l’événement. Les actions locales restent désactivées durant le tour adverse, la lecture/inspection reste possible. Adapter la position à la caméra et aux badges interactifs du vrai plateau.

Pour provenance uniquement, `sources/pulse-shell-history.fragment.html` archive l’exploration originale : examiner **uniquement la branche `data-design="pulse"`** pour HUD, joueurs, reçu et journal. La menthe et son ancien bottom widget sont dépassés par les couleurs et le cockpit approuvés ici ; Arena et Atelier ne sont pas à intégrer. Cette archive n’est pas une quatrième DA validée.

## Données et libellés

Les valeurs de la preview (+2 000 $, −480 $, +1 520 $, Fentanyl Labs à 1 300 $, loyer 130 $, rendement 10 %, autres loyers/niveaux et portefeuille) sont des **fixtures de conception**. Elles ne définissent aucune règle ni aucun catalogue.

- Lire prix, loyers, risque, niveau, rendement et propriétés depuis le modèle réel. Ne jamais déduire un risque fictif ou conserver un taux de 10 % constant pour toute propriété. Clarifier l'horizon du rendement selon le GDD.
- Pour la famille : calculer possédées/total à partir du catalogue et des propriétaires autoritatifs. Distinguer « famille complète » d'un autre bonus de gameplay ; afficher uniquement les bonus effectivement définis.
- Le Sgo, les revenus et frais soumis au passage GO doivent être libellés selon leur vraie échéance. Pas de paiement garanti « à chaque tour ». Utiliser les méthodes d'estimation/calcul existantes après inspection, pas une seconde formule économique dans l'UI.
- Ne jamais appeler `FinanceSystem.ProcessGoIncome` pour obtenir une estimation : cette méthode règle réellement le passage GO et modifie l'état. Réutiliser une projection existante ou extraire prudemment une fonction de lecture pure partagée avec les calculs métier, puis vérifier l'équivalence et l'absence d'effet de bord.
- S'il manque une projection autoritative, afficher une valeur indisponible / partielle clairement indiquée, plutôt que des chiffres fabriqués. Distinguer base GO, autres revenus GO, coûts agrégés et net ; éviter les doubles comptes.
- Ne pas compter les loyers futurs et événements incertains comme revenus garantis. La vue de flux détaille les motifs disponibles.
- Le statut du tour actuel et l'acteur du reçu confirmé restent distincts : un changement de joueur actif ne réattribue pas un achat ou un transfert ancien.

## Interactions et motion

- Déplier/réduire : 260 ms, Bézier (0.2, 0.8, 0.2, 1), interruptible ; le panneau reste ancré en bas.
- Sélection d'une carte : 170 ms, petit fondu et translation verticale de 3 unités. Pas de rotation, spin ou flottement continu.
- Appui sur action : compression vers 0,97 pendant 90 ms, commande émise une seule fois.
- Deux crans adaptatifs, pas deux hauteurs fixes copiées de la maquette de 830 px. Respecter safe areas et clavier éventuel. Le cran compact doit laisser le plateau lisible ; le cran étendu peut occuper davantage d'espace sans faire disparaître le HUD.
- Sur petits écrans/paysage : actions toujours accessibles, cibles tactiles ~44 unités, texte lisible, noms longs repliés sur deux lignes. Un scroll de contenu devient acceptable si nécessaire ; éviter une grande zone scrollable à l'intérieur d'une autre. La maquette n'épuise pas les états 2–6 joueurs.
- Mode réduit : états finaux immédiats, aucune translation obligatoire. Préserver focus, sélection et contexte pendant une interruption.
- Le slider d'achat ne fait **pas** partie de la base approuvée. Voir les options séparées.

## Intégration à inspecter

Vérifier existence et état actuel de ces fichiers avant modification :

- `Assets/Scripts/UI/TopHUDWidget.cs`, `Assets/UI/Layouts/TopHUD.uxml` : métriques et joueurs, cohérence avec la scène de dés.
- `Assets/Scripts/UI/ActionButtonsWidget.cs` : actions, dock d'état, historique, commandes réseau, restauration après une animation.
- `Assets/UI/Layouts/TileDetail_BottomSheet.uxml`, `Assets/UI/Styles/Theme.uss` : fiche, crans et styles.
- `Assets/Scripts/UI/UIUXRuntime.cs`, `Assets/Scripts/UI/UIToolkitTransitions.cs` : modes responsive/réduit et helpers existants.
- `Assets/Scripts/Core/GameEvent.cs`, `Assets/Scripts/UI/GameEventTextFormatter.cs`, `Assets/Scripts/Systems/GameEventPresenter.cs` : faits confirmés et séquençage.
- `Assets/Scripts/Systems/FinanceSystem.cs`, `Assets/Scripts/Systems/AbilitySystem.cs`, `Assets/Scripts/Core/PropertyData.cs`, `Assets/Scripts/Core/PlayerModel.cs` et les GDD : valeurs autoritatives, familles, projections et actions.
- `Assets/Scripts/Multiplayer/OnlineGameController.cs` : répliques, reconnexion et commandes acceptées/refusées.

Créer un composant de présentation dédié si cela réduit le couplage. Séparer contexte de tour, propriété inspectée, portefeuille et estimation financière ; ne pas coder des règles de jeu dans les composants visuels.

## Ressources et limites de la maquette

Les icônes visibles sont des références d'intention, pas l'obligation d'ajouter Lucide au runtime Unity. Réutiliser les ressources existantes ou vérifier droits/licences et compatibilité d'une sélection cohérente. Les captures incorporées servent uniquement de contexte : garder le vrai plateau 3D, ses badges, sa caméra et ses pions.

La maquette fixe la hiérarchie et les interactions principales. Les CSS 3D, gradients et propriétés sans équivalent USS doivent être reproduits par les moyens Unity adaptés. Garder les couleurs approuvées, puis ajuster tailles/contrastes selon les mesures dans le vrai jeu. Pas de remplacement global du HUD si une modification ciblée suffit.
