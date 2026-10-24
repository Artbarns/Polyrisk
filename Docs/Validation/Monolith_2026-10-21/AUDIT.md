# PASS 2E — audit avant modification

SampleScene référence PlayerVisualManager et Assets/Prefab/PlayerPawn.prefab (capsule primitive, CapsuleCollider et PawnMovement). PlayerVisualManager applique les six couleurs existantes, les échelles 0,76 / 0,91 et le délai de préparation des dés de PASS 2D. PawnMovement calcule les segments à 5 unités/s, leur arc vertical, leurs rebonds et son callback unique après la réaction finale historique de 80 ms. TurnManager attend ce callback avec son garde-fou existant de 10 s. TeleportPlayer écrit directement la racine ; ApplyState notifie PresentationStateApplied.

Architecture minimale : un enfant CompactPawnVisual observera les positions réelles atteintes par PawnMovement. Il compensera uniquement l’arc / la déformation sur son enfant pour afficher un glide bas. Il ne modifiera ni les waypoints, ni la vitesse, ni la racine, ni les callbacks. Les franchissements sont observés sur les snaps réellement atteints, pas par une horloge indépendante. Le dernier franchissement déclenche un lock visuel non bloquant. La réaction historique reste dans la racine, rendue invisible par cette compensation.

PlayerVisualManager montera / configurera cet enfant et lui transmettra l’activité sans créer des matériaux via renderer.material. PawnMovement, TurnManager, BoardInteractionManager, BoardDicePresenter, la sélection, les meshes, matériaux, UI et scènes restent inchangés. La caméra conserve ses paramètres / déclencheurs ; son fit voit toujours le renderer de référence désactivé pour garder l’enveloppe de PASS 2D.

Cohabitation : offsets sur l’enfant, stables par identifiant, déterminés à partir de racines effectivement au même endroit ; aucune écriture de position logique. Les transitions d’offset sont courtes ; restauration sans pulse ni audio.

Audio : AudioListener existant sur la caméra ; aucun lecteur de contact runtime trouvé. Réutiliser Resources/PolyRiskAudio/contact_light.wav et contact_tac.wav avec une source persistante par pion, volume faible et variations déterministes. Aucun son définitif nouveau.

Risques : lisibilité de la silhouette à quelques pixels en portrait ; compensation du rebond final sans retard métier ; détection unique des snaps en frames lentes ; annulation / restauration ; volume de plusieurs occupants ; SRP Batcher avec matériaux partagés plutôt que MPB. Preuves réelles et limites obligatoires avant validation artistique.
