# Validation des familles, OPA et sanctions — 2 octobre 2026

## Périmètre appliqué

Cette passe utilise le GDD comme référence économique et les classes de production Unity comme système testé.

- Énergie Tier 3 : charge exacte de `100 / 3` par passage GO. Trois passages donnent maintenant 100%, alors que `33,3 × 3 = 99,9` imposait auparavant un quatrième passage. La cadence maximale passe donc d'une activation tous les 4 GO à une activation tous les 3 GO, soit +33%.
- OPA hostile : acquisition réelle au prix `P = max(1,5 × B, B + 0,75 × U)`, où `B` est le prix de base et `U` le cumul des upgrades payés. La seconde branche devient dominante lorsque `U > 2B/3`. Le prix est transféré intégralement de l'acquéreur au vendeur : la masse monétaire totale est conservée, le niveau et les upgrades de l'actif restent attachés à la propriété.
- Embargo : coût aligné sur le GDD à `5 × Sgo`, contre `1,5 × Sgo` auparavant. Chaque usage retire donc `3,5 × Sgo` supplémentaires de l'économie. La sélection IA tire désormais parmi les sanctions abordables et applicables, ce qui rend l'Embargo réellement accessible.
- Cyber-Sabotage Armement : la durée est comptée en trois tours du propriétaire. Un paiement de loyer ne décrémente plus aussi la durée ; plusieurs loyers peuvent donc être interceptés pendant un même tour.
- Automobile : le droit à Assistance de Précision est réinitialisé au début de chaque tour uniquement si la famille complète est encore détenue. L'appel moteur refuse désormais un joueur sans monopole.
- Aerospace : Saut Quantique accepte une destination éligible explicite. Son fallback privilégie une propriété Aero/Tech/Logistics de valeur, puis la Marketplace, au lieu de forcer systématiquement la Marketplace.

## Régression mécanique Unity

Commande :

```text
Unity -batchmode -nographics -quit -projectPath /chemin/PolyRisk -executeMethod PolyRisk.Editor.MechanicsRegression.Run
```

Résultat final : **54/54 assertions**, code de sortie 0.

La suite vérifie :

- les familles complètes de 2 ou 3 actifs et la désactivation immédiate après perte d'un actif ;
- les charges Tier 1, Tier 2 et Tier 3 ;
- les huit passifs : réductions Real Estate et Tech, taxe Pharma, Assistance Automobile, loyers Oil & Gas et Luxe, blindage Aerospace, multiplicateur Armement en WAR ;
- les huit ultimes : paiement Real Estate, immunité Pharma, Turbo Automobile, Boom Oil & Gas, prélèvement Luxe plafonné, deux upgrades Tech gratuits, destination Aerospace, interception Armement ;
- l'OPA : deux branches de prix, transfert de propriété, conservation des upgrades et du cash total, refus sans fonds ;
- les sanctions courantes : Audit, Strike, Embargo, Sabotage, immunité Pharma, immunité Aerospace et refus sans fonds.

Les deux assertions ajoutées couvrent le refus de l'Assistance Automobile sans monopole et son delta valide avec monopole. La régression économique historique passe aussi à **27/27 assertions**.

## Impact économique observé

Les simulations utilisent exactement les mêmes graines que la baseline du 1er octobre.

### Quatre joueurs — 30 parties

| Indicateur | Baseline | Après correction |
|---|---:|---:|
| Parties valides | 30/30 | 30/30 |
| Anomalies | 0 | 0 |
| Dernier survivant | 25 | 25 |
| Limite tour 50 | 5 | 5 |
| Cash gagnant médian | 471 236 $ | 594 935 $ |
| Cash gagnant moyen | 823 995 $ | 834 072 $ |
| Cash gagnant maximum | 2 990 240 $ | 2 812 182 $ |
| Valeur nette gagnante médiane | 573 011 $ | 771 460 $ |
| Tours individuels moyens | 450,6 | 447,4 |
| Victoires à 10 M$ | 0 | 0 |

Les traces contiennent 112 OPA, 95 ventes Tender et 45 échanges Tender, soit 3,7 OPA par partie. La mobilité des actifs modifie souvent l'identité du gagnant, mais ne change pas la répartition des conditions de fin et réduit légèrement la durée moyenne.

### Six joueurs — 5 parties

| Indicateur | Baseline | Après correction |
|---|---:|---:|
| Parties valides | 5/5 | 5/5 |
| Anomalies | 0 | 0 |
| Dernier survivant | 1 | 2 |
| Limite tour 50 | 4 | 3 |
| Cash gagnant médian | 2 808 197 $ | 2 210 337 $ |
| Cash gagnant moyen | 2 788 688 $ | 3 151 669 $ |
| Cash gagnant maximum | 5 514 993 $ | 8 178 796 $ |
| Valeur nette gagnante moyenne | 2 919 598 $ | 3 270 407 $ |
| Tours individuels moyens | 1 178,6 | 884,4 |
| Victoires à 10 M$ | 0 | 0 |

Les traces contiennent 40 OPA sur ces cinq parties. L'échantillon six joueurs reste petit : la hausse du maximum à 8,18 M$ mérite un batch plus large avant un changement de seuil ou de prix.

## Conclusion et limites

Les corrections n'introduisent ni anomalie, ni création monétaire, ni victoire artificielle à 10 M$ sur 35 parties. Les résultats confirment les mécaniques moteur et leur impact économique, avec un signal de concentration plus fort à six joueurs qui demande davantage de données.

Le parcours humain dispose maintenant d'un picker d'ultime, d'une sélection visuelle de destination Aerospace et de cible Armement, ainsi que du choix Automobile `-1 / conserver / +1` après le jet réel. L'IA évalue aussi les trois destinations Automobile à partir du résultat obtenu. Le build et le parcours Marketplace WebGL sont couverts dans `Docs/Validation_ui_webgl_2026-10-02.md`. Le multijoueur réseau reste un chantier distinct : aucun transport, lobby ou modèle d'autorité réseau n'est encore présent dans le projet.

Exports utilisés :

- `StressTestResults/stress_test_20261002_130220_119_games.csv` — 30 parties, 4 joueurs.
- `StressTestResults/stress_test_20261002_131202_695_games.csv` — 5 parties, 6 joueurs.
