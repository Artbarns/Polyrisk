# Reprise de fidélité visuelle — Pulse

La capture fournie par Arthur le 17 octobre à 23:04 est rejetée. Corrige l'intégration UI existante pour retrouver la composition Pulse déjà validée. Ne propose pas une nouvelle DA. Le présent document précise le handoff initial ; il ne change pas les règles du jeu et ne demande pas de refaire les dés.

## Références à ouvrir avant de modifier

- `reference-approved.html` : copie exacte de la preview approuvée. Manipuler les deux crans, les cartes et les onglets.
- `reference-approved.fragment.html` : source des proportions, couleurs et hiérarchies.
- `result-rejected.png` : capture du résultat refusé. C'est une preuve de défaut, jamais la cible.
- Le package d'origine `Docs/Design/PolyRisk_Builder_Handoff_2026-10-17`, ses briefs et `AGENTS.md` restent applicables. Pour HUD/joueurs/reçus : branche Pulse du source historique de coque ; pour cockpit et palette : dernière référence approuvée ici.

## Diagnostic constaté dans le code local

| Écart visible | Emplacement vérifié | Correction attendue |
|---|---|---|
| Ancien HUD avec grosses séparations et capsules joueurs | `Assets/UI/Layouts/TopHUD.uxml` ; `.top-hud-*`, `.player-roster-chip` dans `Theme.uss` | Recomposer le HUD et son rail joueurs selon Pulse. Noms lisibles, état actif intégré ; conserver les informations de statut utiles. |
| Bouton « CONTROL CENTER COLLAPSE » traité comme un gros CTA flottant | `.indicator-row`, `.pulse-toggle` dans `Theme.uss` | Ligne de poignée puis titre calme à gauche et Déplier/Réduire à droite. Grande zone de tap possible, sans grand cartouche dessiné. |
| Ancien bandeau « YOUR TURN · AT… » toujours au-dessus de la propriété | `GameStateDock` dans `TileDetail_BottomSheet.uxml` | Retirer cet emplacement redondant de la composition du cockpit ; conserver son information et les fonctions événement/journal/skip via le reçu du plateau et un accès discret au journal. |
| Trois métriques Rent/Risk/ROI d'importance égale | `.tile-stat-row` et ses trois cellules UXML | Refaire le bloc unifié validé : loyer principal, rendement secondaire, risque en petite mention, flux GO intégré dessous. |
| GO absent en mode étendu | `#Panel_TileDetail.pulse-panel--expanded .pulse-go-summary { display: none; }` | Conserver la synthèse financière dans le bloc unifié aux deux crans. Onglet Flux GO pour le détail. Valeurs manquantes signalées honnêtement. |
| Onglets en trois grosses boîtes | `.pulse-tab` dans `Theme.uss` | Onglets typographiques, fond transparent et mince trait actif. Préserver focus et surface tactile. |
| Carte sélectionnée entièrement verte | `.pulse-property-card--selected` | Carte neutre, sélection ambre discrète comme la référence. Le vert de possession ne doit pas signifier achat déjà confirmé. |
| Achat vert plein, gros texte centré | Classes partagées `btn-standard`, `tile-action-btn` | Style cockpit dédié : vert profond, trait fin, « Acheter » à gauche et prix à droite ; Passer secondaire plus étroit. |

Le liseré cyan observé peut venir de `.ux-interactive:focus` : vérifier le focus calculé. Préserver une indication clavier lisible ; ne pas supprimer globalement l'accessibilité pour masquer le défaut.

`Assets/UI/MainPanelSettings.asset` utilise une résolution de référence 553×549 avec `m_Match: 0.265`. C'est un point à contrôler, pas une cause démontrée de tous les défauts. Relever viewport CSS, DPR, dimensions du canvas et taille logique du panneau avant de toucher au scale global. La capture PNG ne donne pas à elle seule le viewport CSS. Ne pas modifier l'échelle de toute l'application pour compenser les marges d'un composant.

## Structure de cible

```text
[VisualElement] name="PulseCockpit" class="pulse-cockpit"
├── [Button] name="Btn_PulseToggle" : poignée + Centre de contrôle / Déplier
├── [VisualElement] class="pulse-property-header"
│   ├── [VisualElement] name="Icon_TileFamily" : icône compacte
│   ├── [VisualElement] : état de possession, nom, famille/niveau
│   └── [Button] : possédées/total + segments, ouvre Inspecteur
├── [VisualElement] class="pulse-summary"
│   ├── [VisualElement] : loyer principal / rendement secondaire
│   ├── [Label] : risque non renseigné si absent
│   └── [Button] name="Btn_PulseGoSummary" : GO / coûts / net estimé
├── [VisualElement] name="PulseDetails" : cran étendu seulement
│   ├── [VisualElement] : onglets Inspecteur / Portefeuille / Flux GO
│   └── [ScrollView] : contenu si réellement nécessaire
└── [VisualElement] : actions toujours accessibles
```

Ces noms décrivent l'intention ; préserver les identifiants déjà bindés ou mettre à jour leurs requêtes de manière cohérente. Ne pas supprimer aveuglément un élément que le C# recherche.

## Repères mesurés sur la preview

Mesures DOM relevées le 17 octobre dans l'export fourni, avec la surface `.pc-phone` de **430×830 px CSS**. Repères de reproduction, pas hauteurs Unity fixes ni coordonnées à copier sur tous les écrans.

| Élément | Référence |
|---|---:|
| Largeur intérieure du cockpit | 390 px |
| Cockpit compact, contenu d'exemple | ~413 px de haut |
| Cockpit étendu, Inspecteur d'exemple | ~675 px de haut |
| Ligne poignée/titre | 53 px |
| Entête propriété, exemple Fentanyl Labs | ~100 px |
| Bloc unifié propriété + GO | ~178 px |
| Bouton d'achat / Passer | 49 px de haut |
| Largeur achat / Passer | ~307 / 73 px |
| Onglets | ~49 px de haut |
| Cartes inspecteur pour 3 propriétés | ~125×128 px chacune |

Le cran étendu est un mode d'inspection qui recouvre davantage le plateau. Le mode compact sert à jouer et doit rendre le plateau exploitable. Adapter la densité au paysage et aux petits écrans ; ne pas imposer ces proportions de portrait à tous les formats. Pour une famille de deux propriétés, deux cartes à largeur adaptée : le nombre vient du modèle.

Couleurs : fond `#0b0e16`, surface `#111620`, séparateur `#2a303b`, texte `#f2f3f5`, secondaire `#a1a6b2`, vert `#13db83`, ambre `#edb95c`. Dans la référence, achat sur fond `#0d3d2c`, contour vert fin ; carte sélectionnée sur fond `#25221b`, contour ambre. Conserver les couleurs sémantiques des familles.

## Exécution attendue

1. Lis le code actuel, les skills locaux UI/motion et les références. Préserve les modifications non commitées. Fais un inventaire rapide des différences sans solliciter une nouvelle validation de la DA.
2. Recompose le cockpit dans `TileDetail_BottomSheet.uxml` et un style Pulse ciblé. Évite l'héritage des grosses cartes/boutons marketplace. Réutilise les bindings dans `ActionButtonsWidget.cs`. Révise les règles héritées contradictoires pour ce composant ; n'empile pas une nouvelle série d'overrides globaux.
3. Préserve noms, prix, loyers, niveaux, famille réelle, sélection, droits d'action et états spéciaux. Pour GO, affiche les projections métier disponibles ; sinon champs « indisponible » explicites. Ne jamais appeler `FinanceSystem.ProcessGoIncome` pour afficher une estimation et ne pas copier les chiffres de la démo.
4. Recompose HUD/joueurs et le reçu d'événement. Inspecte `TopHUDWidget.cs`, `ActionButtonsWidget.cs` et `GameEventPresenter.cs` avant de déplacer la présentation : les requêtes sont actuellement attachées à des racines UI. Un simple déplacement UXML entre UIDocuments peut casser les bindings. Préserve journal, skip, déduplication, informations privées et synchronisation des dés.
5. Garde les options nouvelles (slider et célébrations additionnelles) en dehors de cette reprise visuelle. Les animations déjà validées restent utilisables ; aucun nouvel effet pour masquer les problèmes de layout.
6. Compile/import Unity puis ouvre le vrai build WebGL. Corrige les différences visibles, pas seulement les erreurs Console. Vérifie compact, étendu, cartes, portefeuille, GO et tour adverse.

## Preuve obligatoire avant livraison

- Montrer référence et vrai jeu aux **mêmes dimensions utiles** et au **même cran**. Pour vérifier la géométrie, utiliser si possible une fixture comparable ; les captures de recette finale doivent aussi montrer les vraies données, dont le cas Armement 0/2 du résultat refusé.
- Comparer les positions de la poignée, du nom, du bloc unifié, des onglets et des actions. Examiner des captures juxtaposées ou superposées à 50 % ; cette superposition est un outil de contrôle, jamais une image à intégrer dans le jeu.
- Vérifier que les cartes sont lisibles jusqu'en bas et que les actions ne recouvrent pas leur contenu. Pas de scrollbar verte massive ; scroll seulement si le contenu dépasse réellement.
- Portrait courant, petit portrait, paysage bas et bureau. Relever dimensions CSS, DPR et échelle UI pour rendre les comparaisons reproductibles. Aucune capture de l'HTML présentée comme preuve d'intégration Unity.
- Livrer les captures réelles, les écarts restants et les contrôles effectivement exécutés. Si le build ne peut pas être ouvert, le dire et ne pas déclarer la fidélité visuelle validée.

Critères de refus : ancien bandeau au-dessus de la propriété encore visible ; triple colonne Rent/Risk/ROI inchangée ; GO masqué au cran étendu ; gros cartouche Control Center ; onglets en boîtes ; achat vert plein centré ; capsules joueurs qui débordent. Chacun correspond à un écart du résultat rejeté.

## État de ce correctif

La reprise de fidélité au code de la preview est intégrée dans `TopHUD.uxml`, `TileDetail_BottomSheet.uxml`, `Theme.uss`, `TopHUDWidget.cs`, `ActionButtonsWidget.cs` et `CurrencyFormatter.cs`. Le compteur de famille et ses segments viennent du catalogue/propriétés réellement détenues ; la ligne GO entière ouvre les flux ; le HUD et le cockpit affichent les montants complets groupés ; les actions visibles de la propriété sont localisées et conservent leurs handlers existants. Les coûts et le net GO restent « Indisponible », car aucune projection métier pure n’existe encore. Aucun chiffre de la fixture HTML n'est utilisé comme donnée du jeu.

Vérification Unity du 18 octobre 2026 : économie (114 assertions), mécaniques (148), événements (14), UI (303) et build WebGL ont réussi. Après correction des propriétés USS non prises en charge, la dernière analyse de style ne rapporte aucun avertissement USS. Export local : `/tmp/PolyRiskPulseReferenceMatch`.

La comparaison visuelle du WebGL réel aux dimensions CSS 430×830 et aux deux crans reste à faire. La politique de la session a refusé l'accès au navigateur local et interdit de contourner ce refus par une autre surface ou un outil de capture indirect ; aucune capture réelle après correction n'est donc jointe et la fidélité visuelle finale n'est pas déclarée validée. Ne pas partager l'export comme version de test finale avant cette comparaison et la recette des vrais états (dont famille Armement 0/2).
