# Current Task

Icônes de tuiles + refonte UI (migration UI Toolkit). Plan complet :
[sequential-jingling-frog.md](/Users/arthurbarreau/.claude/plans/sequential-jingling-frog.md)

Tâche précédente (plateau 3D) terminée, checkpoint accepté par Arthur — voir
[resilient-strolling-mochi.md](/Users/arthurbarreau/.claude/plans/resilient-strolling-mochi.md)
pour l'historique si besoin.

## Plan

### Phase 1 — Icônes, branchées sur Marketplace
- [x] `Assets/Scripts/UI/TileIconResolver.cs` — résolveur (Sprite override → Hub →
      famille → TileType → fallback neutre), 18 glyphes procéduraux (9 familles + 9
      tuiles spéciales), zéro asset
- [x] `Theme.uss` — nouvelle section glyphes + tints (19 classes `.tile-icon--*`,
      5 primitives `.glyph-*`, `.icon-box--compact`)
- [x] `Marketplace_UIManager.cs` — 3 points d'injection (BuildPropertyCard,
      AddSelectedPropertyToTenderUI, AddBidPropertyToUI)
- [x] `Marketplace_Waiting.uxml` — conteneur `Icon_WaitingProp` + câblage C#

### Phase 2 — Câblage TopHUD (scène uniquement, pas de code)
- [x] Aucun changement C# nécessaire — `TopHUDWidget.uiDocument`/`topHudTemplate`
      existent déjà, dormants. Étapes manuelles ci-dessous.

### Phase 3 — Bottom-sheet (fusion Inspector + ActionButtons)
- [x] `TileDetail_BottomSheet.uxml` — icône + nom/famille + stats (Rent/Level/Rent
      Yield) + 2 boutons d'action
- [x] `ActionButtonsWidget.cs` — champs UI Toolkit + `SetButtonState` étendu (pilote
      les 2 chemins sans toucher aux ~15 points d'appel existants) +
      `UpdateInspectorSection` (nom/famille/niveau/rent/rent yield + icône)
- [x] `InspectorWidget.cs` — retiré façon `AutoPrecisionWidget` (`[Obsolete]`, classe
      vidée, référence scène laissée inerte)
- [x] Bug d'ambiguïté corrigé en cours de route : `UnityEngine.UI.Button` et
      `UnityEngine.UIElements.Button` portent le même nom — les 3 déclarations uGUI
      existantes (`primaryButton`/`secondaryButton`/paramètre de `SetButtonState`)
      qualifiées explicitement en `UnityEngine.UI.Button` pour éviter une erreur de
      compilation CS0104 (ambigu) une fois `UnityEngine.UIElements` importé.

### Étapes manuelles requises dans Unity (aucun accès aux scènes cette session)
1. **TopHUD** : ajouter un `UIDocument` sur `TopHUD_Panel`, Panel Settings =
   `Assets/UI/MainPanelSettings.asset`. Assigner `TopHUDWidget.uiDocument` = ce
   composant, `topHudTemplate` = `TopHUD.uxml`. **Désassigner en même temps** les 3
   champs TMP legacy (`capitalText`/`netWorthText`/`marketStatusText`) — sinon
   double-rendu (`UpdateHumanPlayerUI()` alimente les deux chemins sans condition).
2. **Bottom-sheet** : ajouter un `UIDocument` sur `BottomInspector_Panel`, mêmes
   Panel Settings. Assigner `ActionButtonsWidget.uiDocument` = ce composant,
   `bottomSheetTemplate` = `TileDetail_BottomSheet.uxml`.
3. `sortingOrder` proposés (aucune convention existante avant cette passe, un seul
   UIDocument existait) : Marketplace/UI Manager = 0 (inchangé), Bottom-sheet = 5,
   TopHUD = 10 — à confirmer visuellement une fois vu en Editor.
4. `Assets/Materials/M_BoardTile.mat` (créé par Arthur pour le plateau 3D) —
   sans rapport avec cette UI, aucune action ici.

### Phase 4 — Nettoyage (fait plus tôt que prévu)
- [x] Arthur a supprimé (pas juste désactivé) les GameObjects uGUI legacy dans la
      scène (Image de fond, anciens boutons/textes) — plus rapide que prévu.
- [x] Scripts nettoyés en conséquence : `ActionButtonsWidget.cs` (champs
      `primaryButton`/`secondaryButton`/leurs TMP + `SetButtonState` remplacés par
      `SetPrimaryButtonState`/`SetSecondaryButtonState`, ~37 points d'appel mis à jour
      mécaniquement) et `TopHUDWidget.cs` (`capitalText`/`netWorthText`/
      `marketStatusText` + `NotificationRoutineTMP` retirés) — plus de chemin legacy,
      plus de références mortes vers des GameObjects supprimés.

## Review

**Ce qui a changé** (tout non commité) :
- Nouveaux : `TileIconResolver.cs`, `TileDetail_BottomSheet.uxml`.
- Modifiés : `Theme.uss` (+~200 lignes glyphes/stats/action-row), `Marketplace_UIManager.cs`
  (3 injections icône), `Marketplace_Waiting.uxml`, `ActionButtonsWidget.cs` (fusion +
  UI Toolkit), `InspectorWidget.cs` (retiré).

**Décisions prises pendant l'implémentation (déjà tranchées dans le plan approuvé)** :
- Stats "Risk/ROI" du mockup : pas de donnée "Risk" par tuile dans le modèle actuel →
  affiché "RENT YIELD" (BaseRent/BasePrice) à la place, pas de case "Risk" inventée.
- Identifiant d'icône : résolveur C# par `FamilyData.FamilyName` (pas de nouveau champ
  sur les 9 assets `FamilyData`).
- `ActionButtonsWidget` garde son nom (6 références externes typées dans
  `TurnManager.cs`/`Marketplace_UIManager.cs`).

**Pas vérifié** : toujours aucun accès Unity dans cette session — ni compilation, ni
rendu, ni les étapes manuelles ci-dessus. Prochaine étape côté Arthur : faire les
3 étapes manuelles, régénérer/Play, comparer icônes + bottom-sheet au mockup, cliquer
à travers les principaux états de `ActionButtonsWidget` (Planning/Audit/achat/upgrade/
tuile spéciale/case 20) pour confirmer qu'aucune des ~15 branches n'a régressé.
