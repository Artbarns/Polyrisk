# GDD - Économie & Systèmes Financiers

> [!NOTE]
> **C# Implementation Status of Economic Systems:**
> - **Victory Condition**: `[OPERATIONAL]` — Checked in the turn loop.
> - **Sgo (Salaire GO) Indexation**: `[OPERATIONAL]` — Sgo is initialized at $2,000 and increments by $1,000 when all players complete a lap.
> - **Burn Rate (Maintenance Cost)**: `[MISSING]` — Infrastructure maintenance costs and "Sleep Mode" penalties are **not implemented** in the C# loop.
> - **Passive Dividends**: `[MISSING]` — Tiered dividends (10%/14%/25%) are **not implemented**. Passing GO only awards flat Sgo.
> - **Stock Market**: `[MOCKED]` — The 1/3/5 turns investment logic is fully coded in `FinanceSystem.cs`. However, market cycle modulators (BOOM/CRASH) are missing.
> - **Casino**: `[OPERATIONAL]` — Safe, Standard, and High Risk betting options are fully implemented in `FinanceSystem.cs`.
> - **Dark Board**: `[MISSING]` — Players' exact cash amounts are visible in the HUD and logs; no `WealthStatus` enums or obfuscation are implemented.

---

## 1. Capital de Départ & Objectifs
- **Capital de Départ** : $18,000 par joueur (défini dans `GameBootstrapper.cs`).
- **Objectif de Victoire (L'Hyper-Mogul)** : Le premier joueur à atteindre **$10,000,000 en capital liquide net**.
- **Victoire aux points (Tournoi)** : Si la limite de 50 tours est atteinte, le vainqueur est celui avec la plus haute **Valeur Nette (VN)** :
`VN = Cash + Valeur d'achat propriétés (100%) + Coût cumulé upgrades achetés (50%) + $50,000 par famille complète possédée`

---

## 2. Le Salaire GO (Sgo) & Inflation
- **Montant Initial** : $2,000.
- **Croissance** : `+floor($1,000 ÷ nombre_de_joueurs)` à chaque passage GO individuel
  (variable globale partagée, gérée dans `TurnManager.cs`).
  - 2 joueurs : +$500/GO | 3 joueurs : +$333/GO | 4 joueurs : +$250/GO
  - 5 joueurs : +$200/GO | 6 joueurs : +$167/GO
- **Objectif** : Sgo ≈ $17,000 en fin de partie (15 tours/joueur) quelle que soit
  la configuration, garantissant des économies comparables entre 2 et 6 joueurs.
- **Indexation** : Les montants des cartes Venture et taxes utilisent le Sgo pour s'adapter à l'inflation.

---

## 3. Burn Rate (Coût de Maintenance) — `[MISSING]`

Prévelé **par propriété** au passage GO du joueur propriétaire.

**Mode Actif (100%)** : la propriété a généré au moins 1 loyer depuis le dernier GO du propriétaire.
**Mode Veille (25%)** : aucun adversaire n'a atterri depuis le dernier GO du propriétaire.
**Hypothèque** : exemption au **prochain** GO uniquement — pas le tour courant (anti-exploit).

| Niveau | Burn Rate Actif | Burn Rate Veille (25%) |
|--------|----------------|----------------------|
| 0      | $0             | $0                   |
| 1      | $300           | $75                  |
| 2      | $900           | $225                 |
| 3      | $3,000         | $750                 |
| 4      | $10,000        | $2,500               |
| 5      | $35,000        | $8,750               |

**Infrastructure Tax — 4 Hubs** : $20,000/GO prélevés au passage Case 0 quand les 4 Hubs
sont possédés simultanément. Nul pour 1–3 Hubs.

**Anti-exploit Prison** : Si le Burn Rate total d'un joueur dépasse 5 × Sgo, la sortie de
prison (Case 10) is forcée après 1 seul tour de rétention. Il ne peut pas rester
volontairement pour éviter les passages GO et économiser sur le Burn Rate.

**Case 0 — Passage vs Atterrissage** : Le Sgo is collecté à chaque **passage** par la Case 0,
qu'il soit atterrissage direct ou transit. Exception : Transit Express Hub 35 → Hub 5
(pas de Sgo si Case 0 traversée via téléportation).

*Note* : Le passif Real Estate "Optimisation de Chantier" réduit les coûts d'upgrade de 15%,
pas le Burn Rate. Les Logistics Hubs n'ont pas de Burn Rate individuel par niveau d'upgrade.

---

## 4. Dividendes Passifs (Revenu Garanti) — `[MISSING]`

Versés **par propriété** à chaque passage GO, uniquement si le joueur détient le
**monopole complet** de la famille.

**Formule** : `Dividendes = (Loyer_Base × Multiplicateur_Niveau × 2) × Taux_Tier`
*(×2 = multiplicateur de monopole inclus dans le calcul théorique)*

| Tier   | Taux | Familles                         | Auto-suffisance               |
|--------|------|----------------------------------|-------------------------------|
| Tier 1 | 10%  | Real Estate, Pharma              | Jamais — stratégie expansion  |
| Tier 2 | 14%  | Automobile, Oil & Gas, Luxe      | Jamais — stratégie pression   |
| Tier 3 | 25%  | Tech, Aerospace, Armement        | Oui au Lvl 4–5 (prix ≥ $4k)  |

**Exemples** :
- French Riviera (T1), Lvl3 : ($100 × 12 × 2) × 10% = **$240/GO** | Burn $3,000 → déficit voulu
- Nord Stream Pipe (T2), Lvl3 : ($220 × 12 × 2) × 14% = **$739/GO** | Burn $3,000 → déficit voulu
- Wagner Army Base (T3), Lvl4 : ($750 × 50 × 2) × 25% = **$18,750/GO** | Burn $10,000 → surplus $8,750 ✓
- Wagner Army Base (T3), Lvl5 : ($750 × 200 × 2) × 25% = **$75,000/GO** | Burn $35,000 → surplus $40,000 ✓

**Rappel Dividendes (Ultime Real Estate) — scope** : propriétés avec monopole → loyer base × mult × 2 ;
propriétés sans monopole → loyer base × mult (sans ×2). Empêche l'exploit "1 prop Wagner Lvl5
sans payer le Burn Rate du deuxième Armement".

**Formule Revenu net au GO** :
`Revenu = Sgo + Σ(Dividendes) − Σ(Burn Rate) − Infrastructure_Tax(Hubs)`

---

## 5. Outils Financiers Spéculatifs

### Stock Market
- **Plafond d'investissement par durée** (à mettre à jour dans `FinanceSystem.cs`) :
  - 1 Tour : **15 × Sgo** par joueur
  - 3 Tours : **15 × Sgo** par joueur
  - 5 Tours : **20 × Sgo** par joueur (prime pour immobilisation longue — différencie les options)
- **Volatilité (Implémenté dans `FinanceSystem.cs`)** :
  - **1 Tour** : -30% à +40% ROI.
  - **3 Tours** : -30% à +70% ROI.
  - **5 Tours** : -20% à +120% ROI (20% de probabilité de perte totale).
- **Cycles (BOOM / CRASH)** : `[MISSING]` (non implémenté).

### Casino
Trois niveaux de risque pour tenter de multiplier son cash immédiat (implémenté dans `FinanceSystem.cs` via `PlayCasino`) :
- **Safe** : 80% de chances de gagner (+25% de gain / multiplicateur 1.25).
- **Standard** : 50% de chances de gagner (+80% de gain / multiplicateur 1.80).
- **High Risk** : 25% de chances de gagner (+200% de gain / multiplicateur 3.00).

---

## 6. Liquidation & Faillite
En cas d'insolvabilité, l'ordre de liquidation est strict (implémenté théoriquement) :
1. **Vente d'Upgrades** : 50% de la valeur d'achat.
2. **Hypothèque** : 50% du prix d'achat.
3. **Vente de Titres à la Banque** : 50% de la valeur.
Si la dette persiste après liquidation, le joueur est déclaré en faillite.

**Règle propriétés en faillite** : Le créancier récupère uniquement le **cash résiduel**.
Les propriétés ne sont pas transférées automatiquement — elles sont mises aux enchères
à la Case 20 (Marketplace) dans les **3 tours suivants**. Propriétés non vendues → retour
banque au prix initial.

---

## 7. Cycles Macro-Économiques — `[MISSING]`

### NORMAL (état par défaut)
Aucun modificateur actif.

### BOOM
**Déclencheur** : Carte Venture #20 "Effondrement Global" → résultat BOOM. **Durée** : 5 tours.
- Loyers reçus : ×1.3 | Burn Rate : ×1.2 | Croissance Sgo : ×1.3 | Stock Market : +20pp toutes plages

### CRASH
**Déclencheur** : Carte Venture #20 → résultat CRASH. **Durée** : 5 tours.
- Tous les revenus (loyers, dividendes, Sgo) : ×0.6
- Stock Market : 1T: −50%/+20% | 3T: −50%/+42% | 5T: −40%/+72% | Proba perte totale ×1.75

### WAR
**Déclencheur** : Carte Venture #11 "Guerre Terres Rares". **Durée** : durée de la carte.
- Famille Armement — tous les revenus : ×2
- Famille Luxe — tous les revenus : ×0.5
- Logistics Hubs — loyer = $0 (Infrastructure Tax de $20,000 toujours due si 4 Hubs)

---

## 8. Dark Board (Information Cachée) — `[MISSING]`

| WealthStatus | Seuil liquidités |
|-------------|-----------------|
| LOW         | < 5 × Sgo       |
| MID         | 5 – 20 × Sgo    |
| HIGH        | 20 – 50 × Sgo   |
| MOGUL       | > 50 × Sgo      |

**Règles** :
- HUD en jeu : WealthStatus uniquement (pas le montant exact).
- Case 20 Marketplace : WealthStatus visible lors des enchères, liquidités masquées.
- Case 0 : vérification $10M interne. Montant révélé uniquement si victoire déclenchée.
- Écran fin de partie : tous les montants révélés.

---

## 9. Mode Aggressive Growth — `[MISSING]`

**Déclencheur** : automatique si chronomètre > 90 minutes.
**Effets jusqu'à fin de partie** :
- Croissance Sgo : +$5,000 par GO individuel (remplace la formule $1,000/P standard).
- Seuil Bouclier de Liquidité : 30 × Sgo (remplace 10 × Sgo).
