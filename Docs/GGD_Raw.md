# POLYRISK — GAME DESIGN DOCUMENT V5.0
**Édition Définitive Révisée**

- Plateforme : Mobile (iOS / Android)
- Joueurs : 1 à 6 | Mode : PvE (vs IA) & PvP en ligne
- Capital de départ : $18,000
- Objectif de victoire : $10,000,000 en liquidités nettes

---

## CHAPITRE 1 : VISION ET CORE LOOP

### 1.1 Vision : La Jungle Capitaliste

PolyRisk est une satire hardcore du capitalisme sauvage de l'an 2026. Ce n'est pas une simple course immobilière, mais une simulation de survie corporatiste où la réputation, le sabotage et la gestion impitoyable de la liquidité sont les seuls maîtres.

**Piliers du Design :**
- **Gestion de l'Asymétrie** : Chaque famille d'industrie offre des pouvoirs passifs et ultimes uniques, définissant un style de jeu distinct.
- **Croissance Sigmoïde** : Économie lente au départ, explosion exponentielle dès le franchissement du Niveau 3 d'upgrade.
- **Mouvement Prédateur** : Le plateau est un champ de mines probabiliste où les cases les plus chères précèdent systématiquement la collecte du salaire.
- **Information Asymétrique (Dark Board)** : Les liquidités de chaque joueur sont cachées, transformant enchères et sanctions en exercices de bluff économique.

---

### 1.2 Objectif de Victoire : L'Hyper-Mogul

Le premier joueur à atteindre **$10,000,000 en capital liquide net** — calculé APRÈS déduction du Burn Rate lors du passage DÉPART — remporte immédiatement la partie.

En mode tourno
<truncated 34849 bytes>
100 %) | $10,000 × 100 % | –$10,000 |
| Burn Rate Antarctica (Veille, 25 %) | $10,000 × 25 % | –$2,500 |
| Burn Rate Quantum AI (Veille, 25 %) | $10,000 × 25 % | –$2,500 |
| **REVENU NET DU TOUR** | **$15,000 − $15,000** | **$0 (équilibre exact)** |

> Le joueur Tech Lvl 4 atteint l'équilibre exact au tour 15. Il reste sous pression pour activer des Sanctions (forcer le trafic) ou franchir le Lvl 5 via Casino/Stock Market pour passer en excédent.

---

### A.3 Checklist des Correctifs V5 (Cumulatif)

| Correctif | Statut |
|-----------|--------|
| Capital de départ défini à $18,000 | ✓ |
| Plateau exactement 40 cases (0 à 39) | ✓ |
| Deck Venture 20 cartes sans doublon | ✓ |
| Ultimate Aerospace — Saut Quantique (téléportation libre Aerospace/Tech/Hub/Marketplace) | ✓ |
| Taxe Somptuaire — 1 % plafonné à 8 × Sgo, versé au joueur actif | ✓ |
| Dividendes Passifs tiérisés (10 % / 14 % / 25 %) — calcul vérifié mathématiquement | ✓ |
| Audit Fiscal = 15 % du CASH uniquement (jamais Valeur Nette) | ✓ |
| Règle anti-exploit hypothèque avant GO (Burn Rate comptabilisé au tour courant) | ✓ |
| Hubs redesignés en propriétés achetables (loyer Sgo, pas de Burn Rate, Transit Express vers Hub le plus proche) | ✓ |
| Burn Rate Mode Veille = 25 % | ✓ |
| Stock Market plafonné à 5 × Sgo / tour | ✓ |
| Venture Tax Bracket → Bouclier de Liquidité (cash > 10 × Sgo = ×2) | ✓ |
| Burn Rate supprimé sur hypothèque (tour suivant uniquement) | ✓ |
| Timing de victoire clarifié (après Burn Rate complet) | ✓ |
| Marketplace unique case 20 — Stock Market case 12 — Taxe Plus-Values case 22 — Casino case 32 | ✓ |
| Dark Board + carte Due Diligence | ✓ |
| Spécifications mobile 1 à 6 joueurs — Chapitre 7 complet | ✓ |

The above content shows the entire, complete file contents of the requested file.
---

## CHAPITRE 8 : ÉCONOMIE COMPLÈTE V7 — SOURCE OF TRUTH

### 8.1 Sgo
Formule : `Sgo += floor(1000 / P)` par GO individuel (P = joueurs actifs).
Mode Aggressive Growth (>90 min) : `Sgo += 5000` par GO.

### 8.2 Burn Rate / propriété / GO

| Lvl | Actif   | Veille  |
|-----|---------|---------|
| 0   | $0      | $0      |
| 1   | $300    | $75     |
| 2   | $900    | $225    |
| 3   | $3,000  | $750    |
| 4   | $10,000 | $2,500  |
| 5   | $35,000 | $8,750  |

Anti-exploit prison : sortie forcée après 1 tour si Burn Rate total > 5 × Sgo.
Hypothèque : exemption au GO suivant seulement.

### 8.3 Infrastructure Tax Hubs

| Hubs | Loyer/atterrissage | Tax/GO  |
|------|--------------------|---------|
| 1    | 0.5 × Sgo          | $0      |
| 2    | 1.5 × Sgo          | $0      |
| 3    | 4 × Sgo            | $0      |
| 4    | 6 × Sgo            | $20,000 |

### 8.4 Dividendes
`Div = (base × mult × 2) × taux` | T1: 10% | T2: 14% | T3: 25%
Sans monopole (Rappel uniquement) : `Div = (base × mult) × taux` (pas de ×2).

### 8.5 Formule GO complète
`Revenu net = Sgo + Σ(Dividendes) − Σ(Burn Rate) − Infrastructure_Tax(Hubs)`
Case 0 collecté au passage ET atterrissage. Exception : Transit Express (pas de Sgo si Case 35→5).

### 8.6 Upgrades

| Lvl | Mult | Coût     |
|-----|------|----------|
| 0   | ×1   | —        |
| 1   | ×2   | $500     |
| 2   | ×5   | $2,000   |
| 3   | ×12  | $7,500   |
| 4   | ×50  | $25,000  |
| 5   | ×200 | $100,000 |

### 8.7 Stock Market

| Durée  | Plafond   | Rendement              |
|--------|-----------|------------------------|
| 1 Tour | 15 × Sgo  | −30% / +40%            |
| 3 Tours| 15 × Sgo  | −30% / +70%            |
| 5 Tours| 20 × Sgo  | −20% / +120% (20% perte totale) |

### 8.8 Casino

| Mode      | Proba | Gain  | EV   |
|-----------|-------|-------|------|
| Safe      | 80%   | +25%  | −0%  |
| Standard  | 50%   | +80%  | −10% |
| High Risk | 25%   | +200% | −25% |

### 8.9 Taxes cases spéciales

| Case | Nom              | Valeur   |
|------|------------------|----------|
| 4    | Taxe Douanière   | 0.3 × Sgo|
| 10   | Audit Fiscal     | −15% cash + rétention 3 tours max |
| 22   | Tax / Impôt      | 1.0 × Sgo|

### 8.10 Sanctions Hostiles

| Sanction             | Coût     | Effet                          |
|----------------------|----------|--------------------------------|
| Embargo Commercial   | 5 × Sgo  | Loyer $0 pendant 3 tours       |
| Audit Ciblé          | 1 × Sgo  | Cible −5% cash                 |
| Grève Production     | 2 × Sgo  | Burn Rate ×2 prochain GO       |
| Sabotage Industriel  | 5 × Sgo  | −1 niveau upgrade (Aero immunisé) |

### 8.11 Cycles

BOOM (5T) : loyers ×1.3, burn ×1.2, Sgo ×1.3, SM +20pp
CRASH (5T) : revenus ×0.6, SM −20pp, pertes totales ×1.75
WAR : Armement ×2, Luxe ×0.5, Hubs loyer $0 (Tax due)

### 8.12 Règles Anti-Exploit (récap)
- Hypothèque : exemption Burn Rate au GO suivant uniquement.
- Prison : sortie forcée 1 tour si Burn Rate total > 5 × Sgo.
- Transit Express : pas de Sgo si Hub 35 → Hub 5 via téléportation.
- OPA Hostile : max(150% prix d'achat, prix + 75% coût upgrades).
- Case 0 : Sgo collecté au passage ET atterrissage.
- Case 30 → Case 10 : pénalité −15% cash non appliquée.
- Rappel Dividendes : sans monopole = loyer base × mult (pas ×2).
- Infrastructure Tax : $20,000/GO uniquement quand 4 Hubs possédés.

### 8.13 Dark Board
LOW < 5×Sgo | MID 5–20×Sgo | HIGH 20–50×Sgo | MOGUL >50×Sgo

### 8.14 Victory
Standard : $10,000,000 liquidités nettes au GO.
Tournoi (50 tours) : VN = Cash + props(100%) + upgrades(50%) + $50k/famille.
Aggressive Growth (>90 min) : Sgo +$5,000/GO, Bouclier 30×Sgo.
