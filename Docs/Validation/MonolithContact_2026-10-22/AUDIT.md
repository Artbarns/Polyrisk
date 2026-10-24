# PASS 2E.1 — audit et stratégie

SampleScene → PlayerVisualManager → PlayerPawn.prefab → PawnMovement + CompactPawnVisual. Aucun prefab alternatif actif identifié. Les offsets SharedTile restent sur le child ; les roots/colliders gardent leur index/position réels.

PolyRiskBoard porte les 40 ancres et quatre meshes fusionnés. TileVisuals transmet les états existants à sa texture 64×1 (NeonState / GraphiteSurface). Ces états ne sont pas composables entre eux dans tous les anciens flux ; cette passe ne les réécrit pas. Le contact dispose de sa propre texture 64×2, ajoutée au shader sans sauvegarde/restauration des pixels métier.

BoardInteractionManager reste l’unique auteur de la caméra. Le hold commence via OnPawnMovementFinished, après le rebond final historique de PawnMovement. Un booléen de lecture seule indique la fin de cette présentation, juste avant le callback original. CompactPawnVisual attend ce booléen en LateUpdate pour déclencher ensemble lock, audio et réponse finale. Aucun délai supplémentaire, aucun changement de callback.

Fichiers ciblés : CompactPawnVisual, PawnMovement (horloge seule), BoardContactFeedback (nouveau), GraphiteSurface et NeonState (canal de contact seul), MonolithValidation et MonolithValidationDriver (preuves). Aucun changement de scène, prefab, caméra, board geometry, sélection, règles, économie, réseau, dés ou cockpit prévu.

Risques : lisibilité sous le pixel et impulsions courtes sous faible cadence ; conflit visuel avec une sélection ; restauration réseau ; synchronisation hold/lock. Vérifications : captures natives 430×830, comparaison de tous les pixels persistants pendant les contacts, parcours déterministes avec callbacks réels et cadence ralentie, restauration réelle ApplyState et inventaire source avant/après. Reduced Motion conserve le contour de position à 45 % et supprime les expansions/pulses du pion.
