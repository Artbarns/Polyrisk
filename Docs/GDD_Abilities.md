# GDD - Familles & Capacités Spéciales

> [!NOTE]
> **État vérifié le 15 octobre 2026 :** les huit passifs et les huit ultimes décrits ci-dessous sont implémentés dans le code C#. La suite Unity `MechanicsRegression` passe **117/117 assertions**, dont la complétion d'une famille au troisième titre, les effets passifs, les huit activations ultimes et plusieurs routes d'acquisition. Les preuves et limites du contrôle sont consignées dans `Docs/Validation/Mechanics_Family_OPA_2026-10-15.md`. Cette validation moteur ne signifie pas que chaque pouvoir a été rencontré pendant une partie humaine ni que chaque scénario en ligne à six a été vérifié.

---

## 1. La Matrice des Tiers

Les familles industrielles sont divisées en 3 Tiers, définissant leur difficulté et puissance.
- **Tier 1 (Fondations)** : Real Estate, Pharma. Charge Ultime facile (1 pt / 100% énergie).
- **Tier 2 (Flux & Pression)** : Automobile, Oil & Gas, Luxe. Charge Ultime moyenne (2 pts / 100% énergie).
- **Tier 3 (Domination)** : Tech, Aerospace, Armement. Charge Ultime difficile (3 pts / 100% énergie).

---

## 2. Mécaniques de Maîtrise (Monopole)
Pour débloquer le potentiel d'une famille, il faut posséder **100% de ses titres**.
- **Monopole de Loyer** : Le loyer de base est doublé (implémenté).
- **Passif** : Bonus permanent tant que la famille est complète (implémenté).
- **Ultime** : Capacité active propre à chaque famille (implémentée).

---

## 3. Base de Données des Capacités (Spécifications théoriques)

*Les effets spécifiés ci-dessous sont implémentés. Les détails de couverture de test et les limites de recette sont indiqués dans la note d'état en tête de document.*

### TIER 1 — Fondations (Charge : 1 point)
*   **Real Estate (Immobilier)**
    *   **Passif — Optimisation de Chantier** : Coût des upgrades réduit de 15%.
    *   **Ultime — Rappel de Dividendes** : Collecte immédiate de 25% du loyer théorique de toutes les propriétés possédées. Calcul : monopole actif → base × mult × 2 ; sans monopole → base × mult (sans ×2). Anti-exploit : empêche d'acheter 1 seule prop T3 Lvl5 pour gonfler le Rappel sans payer le Burn Rate de la famille complète.
*   **Pharma (Pharmaceutique)**
    *   **Passif — Optimisation Fiscale** : Taxes (Cases 4, 22, 30), Audit Fiscal (Case 10) et amendes des cartes Venture négatives réduits de 25%.
    *   **Ultime — Protocole Sanitaire** : Immunité totale aux événements négatifs et sanctions adverses pour 1 tour.

### TIER 2 — Flux et Pression (Charge : 2 points)
*   **Automobile**
    *   **Passif — Assistance de Précision** : +1 ou −1 au **total des deux dés** (1 fois/tour, phase Planification). N'affecte pas les dés individuels ni la règle des doubles.
    *   **Ultime — Moteur Turbo** : Relancez les dés et choisissez le meilleur résultat.
*   **Oil & Gas**
    *   **Passif — Forage Intensif** : Loyers de la famille augmentés de 20%.
    *   **Ultime — Boom Énergétique** : Revenus de la famille boostés de 80% pour 1 tour.
*   **Luxe**
    *   **Passif — Prestige Social** : Adversaires paient +25% de loyer sur cette famille.
    *   **Ultime — Taxe Somptuaire** : Chaque adversaire vous verse 1% de son cash (plafonné à 8 × Sgo total).

### TIER 3 — Domination (Charge : 3 points)
*   **Tech**
    *   **Passif — Algorithme Scalable** : Coût des upgrades des propriétés Tech réduit de 10%.
    *   **Ultime — Mise à Jour Système** : 1 upgrade gratuit sur deux propriétés Tech.
*   **Aerospace**
    *   **Passif — Blindage High-Tech** : Immunité totale aux sabotages.
    *   **Ultime — Saut Quantique** : Téléportation vers n'importe quelle case Aero, Tech, Hub ou Marketplace.
*   **Armement**
    *   **Passif — Logistique de Conflit** : +15% de revenus en cycle WAR.
    *   **Ultime — Cyber-Sabotage** : Volez 25% des **loyers effectivement encaissés** sur une propriété adverse pendant 3 tours. Les dividendes passifs de la propriété ciblée ne sont pas affectés.

---

## 4. Correspondance avec le code

Les huit méthodes d'activation se trouvent dans `Assets/Scripts/Systems/AbilitySystem.cs` :
`RealEstate_RappelDividendes`, `Pharma_ProtocoleSanitaire`,
`Automobile_MoteurTurbo`, `OilGas_BoomEnergetique`,
`Luxe_TaxeSomptuaire`, `Tech_MaJSysteme`, `Aerospace_SautQuantique` et
`Armement_CyberSabotage`. Les passifs et la détection du monopole sont répartis
entre `RulesEngine`, `UpgradeSystem`, `TurnManager` et `FinanceSystem`.

`ExecuteTargetedStrike` est conservée comme alias de compatibilité historique
vers Cyber-Sabotage ; ce n'est pas une capacité générique distincte.

---

## 5. Règles de Gestion des Charges
- **Gain par passage GO** : `AbilitySystem.AddEnergy(PlayerModel player, float amount)` à chaque GO.
  - Tier 1 (Real Estate, Pharma) : +100%/GO → Ultime disponible chaque tour de plateau.
  - Tier 2 (Automobile, Oil & Gas, Luxe) : +50%/GO → Ultime tous les 2 tours.
  - Tier 3 (Tech, Aerospace, Armement) : +33%/GO → Ultime tous les 3 tours.
  *Coût d'activation = 100% de la barre pour toutes les familles. La Recharge/GO détermine
  la fréquence maximale d'utilisation.*
- **Activation** : Durée de Phase 1 (Planification), avant les dés.
