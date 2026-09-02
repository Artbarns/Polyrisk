# PolyRisk — CLAUDE.md
> Fichier de contexte pour Claude Code. Chargé automatiquement à chaque session.
> Dernière mise à jour : 2026-06-11 (post-Phase E)

---

## 1. Description du projet

**PolyRisk** est un jeu de plateau type Monopoly en 3D, développé sous **Unity** (URP, TextMeshPro, UI Toolkit), ciblant **iOS / Android** (1 à 6 joueurs, PvE vs IA et PvP en ligne).

- **Dossier projet Unity** : `/Users/arthurbarreau/Desktop/Divers/CODE PROJECT/PolyRisk/PolyRisk`
- **GDD spécialisés (V7 — autorité)** : `Docs/GDD_Board.md`, `Docs/GDD_Economy.md`, `Docs/GDD_Abilities.md`
- **GDD synthèse** : `Docs/GGD_Raw.md` (synchronisé avec les V7)
- **Spec plateau** : `Docs/spec.md`
- **Namespace C#** : `PolyRisk.Core`, `PolyRisk.Systems`, `PolyRisk.UI`, `PolyRisk.Visuals`
- **Plateforme cible** : Mobile (portrait principalement)

### Règle de priorité en cas de conflit GDD
Les fichiers spécialisés (`GDD_Board.md`, `GDD_Economy.md`, `GDD_Abilities.md`) font autorité. `GGD_Raw.md` est la synthèse — jamais l'inverse.

---

## 2. Règles métier critiques (état codé)

| Règle | Valeur GDD | Statut code |
|---|---|---|
| Capital de départ | $18,000 | ✅ Corrigé (scène + C#) |
| Objectif victoire | $10,000,000 cash net | ✅ Vérifié dans `FinanceSystem.ProcessGoIncome` post-Burn Rate |
| Sgo initial | $2,000 | ✅ |
| Sgo croissance | +floor(1000÷P) par GO individuel | ✅ Modulé par cycle BOOM/CRASH |
| Case 12 | Stock Market | ✅ |
| Case 20 | Marketplace | ✅ |
| Bouclier de liquidité | Cash > 10×Sgo → pénalités Venture ×2 | ✅ |
| Burn Rate Mode Actif | 100% (trafic depuis dernier GO) | ✅ Table par niveau |
| Burn Rate Mode Veille | 25% | ✅ |
| Multiplicateurs upgrades | ×1 / ×2 / ×5 / ×12 / ×50 / ×200 (v7) | ✅ |
| Hub 4 loyer | 6×Sgo + Infrastructure Tax $20,000/GO | ✅ |
| Monopole bonus | ×2 loyer à TOUS les niveaux | ✅ |
| Stock Market caps | 15×Sgo (1T/3T) / 20×Sgo (5T) | ✅ |
| Casino Safe | 80% / +25% | ✅ |
| Casino Standard | 50% / +80% | ✅ |
| Casino High Risk | 25% / +200% | ✅ |
| Dividendes Tiérisés | T1=10% T2=14% T3=25% des loyers théoriques | ✅ Dans `ProcessGoIncome` |
| Transit Express 4 Hubs | 5→15→25→35→5, pas de Sgo si 35→5 | ✅ |
| Cycles BOOM/CRASH/WAR | Modificateurs GDD_Economy §7 | ✅ |
| Venture #11 déclencheur WAR | Cycle WAR 5 tours | ✅ |
| Venture #20 déclencheur BOOM/CRASH | 50/50, 5 tours | ✅ |
| Time-out tournoi | Tour 50 → gagnant par VN | ✅ |

---

## 3. Architecture du code (post-Phase E)

```
Assets/Scripts/
├── Core/           # Modèles + logique métier pure
│   ├── BoardTile.cs, BoardTileData.cs, PropertyData.cs, FamilyData.cs
│   ├── PlayerModel.cs         — Balance, propriétés, flags Venture + Phase D flags
│   │                            (AutoTurboReady, AutoPrecisionAvailable, PendingDiceDelta,
│   │                             ImmunityTurnsRemaining, OilGasBoostTurnsRemaining,
│   │                             OilGasPenaltyTurns, DoubleBurnRateNextGo, Profile)
│   │                            + enums PsychoProfile et WealthStatus (Phase E.1)
│   ├── GameEvents.cs          — Event bus + OnGameVictory (E.3) + OnInsolvency (E.4)
│   ├── RulesEngine.cs         — ExecutePrimaryAction, CalculateRent (avec tous les modificateurs),
│   │                            HasFullFamily, HandlePropertyLanding
│   ├── TileType.cs, ColorGroup.cs, TransactionManager.cs
│
├── Systems/
│   ├── TurnManager.cs         — State machine : TurnStart → Planning → RollDice → MovingPawn
│   │                            → ResolveTile → EndTurn. UseAutoPrecision, EndPlanningPhase,
│   │                            HandleTransitExpress, check victory time-out 50 tours
│   ├── BoardManager.cs        — 40 tiles, visualTiles[]
│   ├── GameBootstrapper.cs    — startingBalance $18,000, assign PsychoProfiles round-robin
│   ├── FinanceSystem.cs       — ProcessGoIncome (Sgo + Divs Tiérisés − Burn Rate − Infra Tax),
│   │                            Stock Market avec cycle shifts, Casino, check Hyper-Mogul $10M
│   ├── MarketplaceSystem.cs   — Tender + Sanctions, EvaluateBidValue, IsImmune helper,
│   │                            SimulateAIBidders par profil
│   ├── VentureSystem.cs       — Deck 20 cartes, pop-up modale humain (E.2), immunité check
│   ├── UpgradeSystem.cs       — TryUpgrade, SellAllUpgrades (E.4), track TotalUpgradeCostInvested
│   ├── AbilitySystem.cs       — 8 Ultimes + charge énergie par tier, ChargeEnergyOnGo
│   ├── AIManager.cs           — HandlePlanningPhase (Ultime + Auto Precision),
│   │                            HandleMarketplaceDecision (Tender vs Sanction par profil)
│   ├── DiceSystem.cs, BoardInteractionManager.cs, AuctionManager.cs, CycleManager.cs
│   ├── LiquidationHelper.cs   — MortgageProperty, SellPropertyToBank (OwnerID = -1)
│
├── UI/
│   ├── Marketplace_UIManager.cs   — Frames 1/2A/2B/3A/3B/4B + Waiting + BidderSpectator + SelectPlayer
│   ├── TopHUDWidget.cs            — UI Toolkit avec fallback TMP, Dark Board (E.1)
│   ├── ActionButtonsWidget.cs     — Bottom sheet, gère Phase Planning + Ultime
│   ├── InspectorWidget.cs, CurrencyFormatter.cs
│   ├── UIToolkitTransitions.cs    — SlideUpEnter, ScaleInEnter, FadeInOverlay (E.5)
│   ├── VentureCardCatalog.cs      — 20 cartes hard-codées
│   ├── VentureCardModalController.cs (E.2)
│   ├── VictoryScreenController.cs (E.3)
│   ├── BankruptcyScreenController.cs (E.4)
│   ├── AutoPrecisionWidget.cs     — [Obsolete] vidé, garde-fou compat scène
│
├── Debug/
│   └── DebugMarketplaceSetup.cs   — F1/F2 : distribue propriétés + téléporte case 20
│
└── Visuals/
    ├── TileVisuals.cs        — SetupVisuals, Highlight, BeginPulse/EndPulse
    ├── BoardGenerator.cs, PawnMovement.cs, PlayerVisualManager.cs, TileIdentifier.cs

Assets/UI/
├── Layouts/  — 12 UXML : Marketplace_*, TopHUD, VictoryScreen, BankruptcyScreen, VentureCardModal
└── Styles/Theme.uss — 900+ lignes, palette Cyber-Luxury, transitions, Dark Board pastilles
```

### Conventions
- Toutes valeurs monétaires en `double`
- GameObjects pawns : `Pawn_Player_{playerId}`
- Tiles 3D : `Tile_{index}`
- Injection via Inspector (Singletons uniquement pour BoardManager, TurnManager, TransactionManager, AbilitySystem, AuctionManager, CycleManager)
- **USS interdits** : `cursor: pointer`, `picking-mode`, `space-evenly`, `gap`, `box-shadow` natif, `filter`, `::before/::after`
- **TMP** : ASCII only dans les labels (LiberationSans SDF n'a pas les emojis Unicode)

---

## 4. Plateau — Cases 0-39 (résumé)

```
0  GO  |  1-3-6 RealEstate | 4 Tax0.3×Sgo | 5 Hub1 | 8-9-11 Pharma | 10 Audit |
12 Stock Market | 13-14-16 Auto | 15 Hub2 | 18-19-21 Oil&Gas |
20 Marketplace | 22 Tax0.5×Sgo (1×Sgo si Tier3) | 23-24-26 Luxe | 25 Hub3 | 27-28-31 Tech |
29-34-37 Venture | 30 GoToPrison | 32 Casino | 33-36 Aerospace | 35 Hub4 | 38-39 Armement
Loyer base = 10% prix. Total plateau : $61,800 + $7,000 Hubs
```

Prix : voir GDD_Board.md.

---

## 5. Phases livrées

- **Phase A** (moteur économique) : startingBalance, Sgo, multiplicateurs upgrades v7, Hub 6×Sgo, bonus monopole tous niveaux, caps Stock Market, Casino Safe 80%, Burn Rate complet, Dividendes Tiérisés, Infrastructure Tax 4 Hubs, DoubleBurnRateNextGo, OilGasPenaltyTurns.
- **Phase B** (asymétrie familles) : 8 Ultimes, 6 Passifs (Real Estate/Tech/Pharma/Oil&Gas/Luxe/Aerospace), énergie GO, Auto Turbo, Auto Precision, immunité Pharma bloque toutes sanctions + Venture négatives, Golden Ticket consommable.
- **Phase C** (cycles + Transit Express) : CycleManager BOOM/CRASH/WAR, hooks CalculateRent/ProcessGoIncome/Stock Market, Armement +15% WAR, Transit Express 4 Hubs 5→15→25→35.
- **Phase D** (IA cerveau) : TurnState.Planning, PsychoProfile (Predator/Rentier/Negotiator) assignés round-robin, AIManager.HandlePlanningPhase + HandleMarketplaceDecision, EvaluateBidValue, Predator case 20 = 100% Sanction, Ultimate priority Tech > Armement > Real Estate > Pharma > Luxe > Oil > Aerospace > Auto.
- **Phase E** (UI Cyber-Luxury + fin de partie) :
  - E.1 Dark Board (WealthStatus enum + pastilles) + TopHUD UI Toolkit format `CAPITAL | NET WORTH | TURN | MARKET`
  - E.5 Transitions USS (SlideUp/ScaleIn/FadeIn) via UIToolkitTransitions
  - E.3 Victory Screen ($10M Hyper-Mogul + tour 50 VN)
  - E.2 Venture Card Modal (20 cartes, flip animation, 3 variantes visuelles)
  - E.4 Bankruptcy Screen (flow liquidation strict : upgrades 50% → hypothèque 50% → vente banque 50%, IA auto-liquide)
  - E.6 AutoPrecisionWidget vidé (obsolete)

## 6. Ce qui reste — Phase F (QA & Prod)

- Stress test IA x1000 parties (vérifier économie stable au tour 50)
- Nettoyage ~150 Debug.Log
- Suppression raccourci "M" (téléport case 20) dans `TurnManager.Update`
- Suppression `DebugMarketplaceSetup` (F1/F2) ou passage en `#if UNITY_EDITOR`
- Profiling UI Toolkit + compression textures URP
- Remplacer visuels 3D placeholder (cubes/cylindres) par la DA finale
- Audio design (SFX + BGM synthwave)

---

## 7. Setup MCP Unity

`.mcp.json` déjà présent à la racine :
```json
{ "mcpServers": { "mcp-unity": { "command": "node", "args": ["Library/PackageCache/com.gamelovers.mcp-unity@5dc92f5401/Server~/build/index.js"] } } }
```
Unity doit être ouvert avec le projet pour que le WebSocket local soit actif.

---

## 8. Points connus non testés en runtime

- IA Predator sur case 20 doit choisir Sanction 100% (logique OK, jamais vérifié in-game)
- Venture #11 → cycle WAR complet (Armement ×2, Luxe ×0.5, Hubs loyer 0)
- Venture #20 → BOOM/CRASH (modificateurs cascade)
- Transit Express 4 Hubs
- BankruptcyScreen : flow humain complet
- VictoryScreen (attendre $10M)
- Toutes les Ultimes IA activées en Planning

---

## 9. Protocole opérationnel (workflow Claude Code)

### Orchestration
- **Mode Plan par défaut** pour toute tâche non triviale (3+ étapes ou décision d'architecture). Si ça part de travers en cours d'implémentation : stop et re-plan immédiatement, ne pas s'acharner sur un plan déjà faux.
- **Sous-agents** : à utiliser largement pour garder le contexte principal propre — recherche, exploration, analyse parallèle. Un agent = une tâche, focus étroit plutôt que généraliste. Agents disponibles dans `.claude/agents/` : `code-architect` (revue de design avant implémentation), `staff-reviewer` (revue critique de `tasks/todo.md`), `code-simplifier` (simplification post-implémentation), `verify-app`, `build-validator` (ces deux derniers inertes tant qu'aucun test runner / pipeline de build n'existe pour ce projet Unity).
- **Vérification avant "terminé" — règle n°1** : ne jamais marquer une tâche complète sans preuve (exécution, test, logs). "Ça devrait marcher" n'est pas une vérification. Si un sous-agent a fait le travail, un autre (ou toi) le relit contre le plan avant présentation.
- **Boucle d'amélioration** : après toute correction d'Arthur, logger le pattern dans `tasks/lessons.md`, en faire une règle standing, relire `tasks/lessons.md` en début de session.
- **Exiger l'élégance (dosé)** : pour les changements non triviaux, se demander s'il y a plus élégant. Skip pour les fix triviaux.

### Gestion des tâches
1. Plan d'abord — écrire le plan dans `tasks/todo.md` en items cochables.
2. Check-in avant l'implémentation — attendre le feu vert.
3. Suivre la progression — cocher au fur et à mesure.
4. Expliquer à haut niveau — résumé court par étape.
5. Documenter le résultat — section revue dans `tasks/todo.md` une fois fait.
6. Capturer les leçons — mettre à jour `tasks/lessons.md` après toute correction.

### Principes de base
- Simplicité d'abord — le plus petit changement qui résout le problème.
- Pas de paresse — trouver la cause racine, pas de patch temporaire.
- Blast radius minimal — ne toucher que le nécessaire.

### Commandes disponibles (`.claude/commands/`)
`grill`, `review-changes`, `techdebt`, `test-and-fix` fonctionnent indépendamment de git. `commit-push-pr`, `quick-commit`, `worktree` supposent un dépôt git — **ce projet n'en a pas encore** (ni au niveau `PolyRisk/PolyRisk/` ni au niveau parent) ; ces trois commandes resteront inutilisables tant que ce n'est pas initialisé.
</content>
