# PASS 2F.1 — audit avant modification

Exécution réelle : SampleScene → BoardInteractionManager (seul writer caméra), BoardManager / PlayerVisualManager → PawnMovement et CompactPawnVisual → BoardContactFeedback, BoardSurfaceLighting et GraphiteSurface / NeonState.

Caméra PASS 2D : orthographique 40°/45°, zoom action 1,14, entrée 0,30 s, hold 0,25 s, retour 0,375 s. Le suivi horizontal vise le centre initial + 50 % du déplacement ; le vertical agit surtout aux limites de la zone 62 % × 82 %. Ce calcul explique la dérive. Le hop logique est volontairement éliminé du suivi. Cockpit et inspection ne font pas partie de la clé de calibration.

Contact : durée 120 ms / arrivée 240 ms. Intensité à pleine valeur au déclenchement, maintien sur les 18 % initiaux puis smoothstep descendant : pas d’attaque. Texture transitoire 64×2 séparée de la texture persistante 64×1. Le contact vient du franchissement réellement observé, le lock garde son propre temps.

Matière : B et A partagent le shader opaque et les 4 meshes. La réflexion B reste courte mais son gain reste faible en pixels éloignés. Le contraste dynamique peut être renforcé sans éclaircir la matière neutre ni toucher au bloom 0,08 / exposition +0,15. Low : une directional per-pixel sans ombres, SRP Batcher ; Medium : ombres existantes. Aucune nouvelle probe/SSR/lumière/texture nécessaire.

Périmètre prévu : BoardInteractionManager, BoardContactFeedback, BoardVisualTheme / GraphiteTheme, BoardSurfaceLighting, GraphiteSurface ; bancs QA séparés et documents de preuve. Préserver scène, meshes/ancres/colliders, PawnMovement/CompactPawnVisual/PlayerVisualManager, TileVisuals, règles/économie/réseau/UI/dés.

Risques : rattrapage initial depuis un coin, inversion d’anticipation à 90°, capture moins fluide que le rendu sans capture, sous-échantillonnage d’une attaque de 30 ms, intensité contact excessive, données sérialisées de tuning anciennes. Valider en WebGL natif 430×830 ; ne pas inférer téléphone physique/GPU.
