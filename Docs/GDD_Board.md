# GDD - Plateau & Propriétés

> [!NOTE]
> **C# Implementation Status of Board Features:**
> - **Board Mapping**: `[OPERATIONAL]` — Exactly 40 tiles generated according to `PolyRiskBoardGenerator.cs` (12 is Stock Market, 22 is Tax/Impôt, 32 is Casino).
> - **Logistics Hubs Loyer/Scaling**: `[OPERATIONAL]` — Handled dynamically in `RulesEngine.cs`.
> - **Transit Express (Hub teleportation)**: `[MISSING]` — Not implemented in `TurnManager.cs` or `RulesEngine.cs`.
> - **Upgrade System Scaling**: `[OPERATIONAL]` — Sigmoid pricing and monopoly multiplier (2x) is fully integrated into `UpgradeSystem.cs` and rent payments.

---

## 1. Mapping du Plateau (40 Cases)

Le plateau est divisé en 4 lignes thématiques de 10 cases.

### LIGNE 1 : L'ACCUMULATION (0-9)
| Case | Nom | Type | Prix / Détails |
|------|-----|------|----------------|
| 0 | **DÉPART / GO** | Spécial | Salaire + Dividendes - Burn Rate |
| 1 | **Zuckerberg's Bunker** | Real Estate | $800 |
| 2 | Venture | Carte | Deck "Cruel World 2026" |
| 3 | **Trump Tower** | Real Estate | $900 |
| 4 | Taxe Douanière | Taxe | 0.3 × Sgo |
| 5 | **Temu Sweatshop Terminal** | Hub 1 | $1,000 |
| 6 | **French Riviera** | Real Estate | $1,000 |
| 7 | Venture | Carte | Deck "Cruel World 2026" |
| 8 | **Opioid Paradise** | Pharma | $1,100 |
| 9 | **Pfizer-BioTech** | Pharma | $1,200 |

### LIGNE 2 : L'EXPANSION (10-19)
| Case | Nom | Type | Prix / Détails |
|------|-----|------|----------------|
| 10 | **AUDIT FISCAL** | Prison | Sortie: Double ou 1×Sgo |
| 11 | **Fentanyl Labs** | Pharma | $1,300 |
| 12 | **Stock Market** | Spécial | Plafond: 15 × Sgo (1T & 3T) / 20 × Sgo (5T) |
| 13 | **Hydrogen Speedways** | Auto | $1,500 |
| 14 | **CyberTruck Factory** | Auto | $1,600 |
| 15 | **Amazon Prime Drone-Pad** | Hub 2 | $1,500 |
| 16 | **Ferrari Maranello** | Auto | $1,700 |
| 17 | Venture | Carte | Deck "Cruel World 2026" |
| 18 | **Greenwashing Solutions** | Oil & Gas | $1,800 |
| 19 | **Deep Ocean Drill** | Oil & Gas | $2,000 |

### LIGNE 3 : LA SPÉCULATION (20-29)
| Case | Nom | Type | Prix / Détails |
|------|-----|------|----------------|
| 20 | **MARKETPLACE** | Spécial | Enchères / Sanctions |
| 21 | **Nord Stream Pipe** | Oil & Gas | $2,200 |
| 22 | **TAX / IMPÔT** | Taxe | 1.0 × Sgo |
| 23 | **LVMH Palace** | Luxe | $2,500 |
| 24 | **Rolex Tower** | Luxe | $2,700 |
| 25 | **SpaceX Uplink Hub** | Hub 3 | $2,000 |
| 26 | **Tax-Haven Island** | Luxe | $3,000 |
| 27 | **Antarctica Datacenter** | Tech | $3,500 |
| 28 | **Boston Dynamics Factory** | Tech | $4,000 |
| 29 | Venture | Carte | Deck "Cruel World 2026" |

### LIGNE 4 : LA DOMINATION (30-39)
| Case | Nom | Type | Prix / Détails |
|------|-----|------|----------------|
| 30 | **DIRECTION AUDIT** | Spécial | Aller en case 10 (sans pénalité −15% cash) |
| 31 | **Quantum AI Labs** | Tech | $4,500 |
| 32 | **CASINO** | Spécial | Safe / Standard / High Risk |
| 33 | **NASA Moonbase** | Aero | $5,000 |
| 34 | Venture | Carte | Deck "Cruel World 2026" |
| 35 | **Silk Road 3.0 Port** | Hub 4 | $2,500 |
| 36 | **SpaceX Orbital Station** | Aero | $5,500 |
| 37 | Venture | Carte | Deck "Cruel World 2026" |
| 38 | **Lockheed Martin** | Armement | $6,500 |
| 39 | **Wagner Army Base** | Armement | $7,500 |

---

## 2. Logistics Hubs (Infrastructures)
Les 4 Hubs ne font partie d'aucune famille industrielle et n'ont **pas de Burn Rate** ni d'upgrades.

- **Hub 1** : Temu Sweatshop Terminal ($1,000)
- **Hub 2** : Amazon Prime Drone-Pad ($1,500)
- **Hub 3** : SpaceX Uplink Hub ($2,000)
- **Hub 4** : Silk Road 3.0 Port ($2,500)

**Loyer** : Dépend du nombre de Hubs possédés par le propriétaire (implémenté dans `RulesEngine.cs`) :
- 1 Hub : 0.5 × Sgo
- 2 Hubs : 1.5 × Sgo
- 3 Hubs : 4 × Sgo
- 4 Hubs : 6 × Sgo

**Infrastructure Tax (Anti-Snowball)** : Quand un joueur possède les 4 Hubs
simultanément, $5,000/Hub/GO est prélevé au passage Case 0 (total $20,000/GO).
Aucun coût pour 1, 2 ou 3 Hubs. *Statut C#* : `[MISSING]`

**Règle "Transit Express"** :
- **Prérequis** : Posséder les 4 Hubs simultanément.
- **Déclencheur** : Le joueur atterrit sur l'un de ses 4 Hubs.
- **Effet** : Téléportation optionnelle vers le Hub suivant dans le sens horaire.
- **Sgo** : Non collecté si la téléportation traverse la Case 0 (Case 35 → Case 5).
- **Séquence** : Case 5 → Case 15 → Case 25 → Case 35 → Case 5.
- *Statut C#* : `[MISSING]` — Non implémenté dans `TurnManager.cs` ou `RulesEngine.cs`.

---

## 3. Données Financières des Propriétés
- **Loyer de Base** : 10% du prix d'achat (Ex : Zuckerberg's Bunker = $80).
- **Monopole** : Loyer doublé si toute la famille est possédée (implémenté).
- **Coût total du plateau** : $68,800 (propriétés industrielles $61,800 + Logistics Hubs $7,000).

---

## 4. Système d'Upgrades (Sigmoïde)
Condition : Avoir le monopole de la famille (les titres de propriété complets).
- **Niveau 1** : ×2 Loyer (Coût : $500)
- **Niveau 2** : ×5 Loyer (Coût : $2,000)
- **Niveau 3 (Pivot)** : ×12 Loyer (Coût : $7,500)
- **Niveau 4** : ×50 Loyer (Coût : $25,000)
- **Niveau 5 (Endgame)** : ×200 Loyer (Coût : $100,000)
