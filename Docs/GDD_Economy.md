# GDD - Économie & Systèmes Financiers

> [!NOTE]
> **État C# vérifié le 15 octobre 2026 :**
> - **Victoire** : `[OPÉRATIONNEL]` — Le seuil de 10 M$ est contrôlé après le règlement GO ; les autres issues sont contrôlées par le tour.
> - **Indexation Sgo** : `[OPÉRATIONNEL]` — Sgo commence à 2 000 $ ; chaque passage individuel augmente l’indice de `floor(1 000 $ / joueurs actifs)`, modulé par le cycle et Aggressive Growth.
> - **Burn Rate et taxe Hubs** : `[OPÉRATIONNEL]` — Modes actif/veille, hypothèque, double prélèvement Venture, BOOM et taxe des quatre Hubs sont traités dans `FinanceSystem.ProcessGoIncome`.
> - **Dividendes** : `[OPÉRATIONNEL]` — Taux par tier, avec monopole complet, dans le règlement GO.
> - **Aggressive Growth** : `[OPÉRATIONNEL]` — Après strictement 90 minutes, croissance Sgo à 5 000 $ par passage et Bouclier à 30×Sgo ; horloge UTC répliquée en ligne.
> - **Bourse, Casino et cycles macro** : `[OPÉRATIONNELS]` — Investissements de 1/3/5 tours, modificateurs BOOM/CRASH et paris sont codés. Les revenus GO CRASH et les bornes de rendement CRASH sont alignés sur §7 depuis le 15 octobre.
> - **Dark Board** : `[PARTIEL — hôte de confiance]` — Les snapshots invités projettent le cash adverse dans sa tranche ; l’hôte autoritaire conserve l’état exact et peut inspecter son client.
>
> Régressions Unity Economy 114/114 et Mechanics 126/126, build WebGL Release et build serveur Release passent. Lot IA corrigé : 30 parties à quatre joueurs, puis 5 graines appariées à six ; détails dans `Docs/Validation/Economy_Cycle_Conformance_2026-10-15.md`. L’équilibrage humain reste ouvert.

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

## 3. Burn Rate (Coût de Maintenance) — `[OPÉRATIONNEL]`

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

## 4. Dividendes Passifs (Revenu Garanti) — `[OPÉRATIONNEL]`

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
- **Plafond d'investissement par durée** (implémenté dans `FinanceSystem.cs`) :
  - 1 Tour : **15 × Sgo** par joueur
  - 3 Tours : **15 × Sgo** par joueur
  - 5 Tours : **20 × Sgo** par joueur (prime pour immobilisation longue — différencie les options)
- **Volatilité (Implémenté dans `FinanceSystem.cs`)** :
  - **1 Tour** : -30% à +40% ROI.
  - **3 Tours** : -30% à +70% ROI.
  - **5 Tours** : -20% à +120% ROI (20% de probabilité de perte totale).
- **Cycles (BOOM / CRASH)** : `[OPÉRATIONNEL]` (voir §7).

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

## 7. Cycles Macro-Économiques — `[OPÉRATIONNELS]`

### NORMAL (état par défaut)
Aucun modificateur actif.

### BOOM
**Déclencheur** : Carte Venture #20 "Effondrement Global" → résultat BOOM. **Durée** : 5 tours.
- Loyers reçus : ×1.3 | Burn Rate : ×1.2 | Croissance Sgo : ×1.3 | Stock Market : +20pp toutes plages

### CRASH
**Déclencheur** : Carte Venture #20 → résultat CRASH. **Durée** : 5 tours.
- Tous les revenus (loyers, dividendes, salaire Sgo) : ×0.6 ; le Burn Rate reste inchangé.
- Stock Market : 1T: −50%/+20% | 3T: −50%/+42% | 5T: −40%/+72% | probabilité de perte totale ×1.75 (35% à 5 tours).

### WAR
**Déclencheur** : Carte Venture #11 "Guerre Terres Rares". **Durée** : durée de la carte.
- Famille Armement — tous les revenus : ×2
- Famille Luxe — tous les revenus : ×0.5
- Logistics Hubs — loyer = $0 (Infrastructure Tax de $20,000 toujours due si 4 Hubs)

---

## 8. Dark Board (Information Cachée) — `[PARTIEL — hôte de confiance]`

| WealthStatus | Seuil liquidités |
|-------------|-----------------|
| LOW         | < 5 × Sgo       |
| MID         | 5 – 20 × Sgo    |
| HIGH        | 20 – 50 × Sgo   |
| MOGUL       | > 50 × Sgo      |

**Règles** :
- HUD : capital exact du joueur local ; ses adversaires sont représentés par leur WealthStatus.
- Case 20 Marketplace : WealthStatus visible lors des enchères, liquidités masquées.
- Case 0 : vérification $10M interne. Montant révélé uniquement si victoire déclenchée.
- Écran fin de partie : tous les montants révélés.

**Limite réseau** : les invités reçoivent leur solde exact et une valeur de
projection correspondant à la tranche de richesse pour les autres sièges ; le
snapshot complet reste stocké côté service afin qu’un nouvel hôte puisse
reprendre l’autorité après un transfert. L’hôte actif conserve le snapshot
complet pour exécuter le moteur de règles localement. La règle protège donc les
soldes exacts contre l’inspection du client invité, mais le modèle actuel ne
protège pas contre l’hôte qui inspecte ou modifie son client.

---

## 9. Mode Aggressive Growth — `[OPERATIONAL]`

**Déclencheur** : automatique si chronomètre > 90 minutes.
**Effets jusqu'à fin de partie** :
- Croissance Sgo : +$5,000 par GO individuel (remplace la formule $1,000/P standard).
- Seuil Bouclier de Liquidité : 30 × Sgo (remplace 10 × Sgo).

L’horloge débute à `TurnManager.StartGame`. En ligne, son timestamp UTC est transmis
dans le snapshot autoritaire et restauré lors d’une reprise/changement d’hôte. Le
multiplicateur de cycle Sgo déjà actif continue de s’appliquer à la croissance.
