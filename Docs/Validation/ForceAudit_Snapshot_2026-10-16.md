# Reprise du statut Force Audit — 16 octobre 2026

Une régression ciblée renforce la couverture du statut de sanction OPA en ligne, sans modifier le code du jeu.

MarketplaceSystem.ExecuteSanction(..., SanctionType.ForceAudit) a été exécuté dans Unity Editor. Le contrôle vérifie le coût de 4 Sgo, la position 10, IsInAudit=true et trois tours restants. L’état a ensuite été capturé par OnlineGameController.CaptureSnapshot, sérialisé, effacé du client simulé, puis restauré par OnlineGameController.ApplyState. Les trois champs — état Audit, durée restante et position — sont récupérés.

- Unity 2022.3.62f3 batch : MechanicsRegression.Run **137/137 PASS**, code de sortie 0. Les contrôles appliquent l’immunité Pharma à Force Audit, puis à la commande hôte marketplace.sanction/audit sur la vraie case 20 : avec immunité, aucun débit, déplacement, remise à zéro de durée ni activité, tout en consommant la commande ; sans immunité, la commande applique exactement 1 Sgo, 5 % du solde cible et une activité, puis termine la résolution. Le fixture Editor définit maintenant explicitement la case Marketplace.
- Journal : /tmp/PolyRiskOnlineSanctionCommandQA_20261016_final.log.
- Modification confinée au banc Editor Assets/Scripts/Editor/MechanicsRegression.cs.
- Aucun calcul économique, règle, comportement Player ni donnée de production n’a changé.

Cette preuve valide le chemin local de snapshot/restauration et la branche ApplyCommand du contrôleur hôte. Elle n’exerce pas le transport HTTP entre clients et ne remplace pas une reconnexion WebGL réelle ; cette recette réseau demeure ouverte.
