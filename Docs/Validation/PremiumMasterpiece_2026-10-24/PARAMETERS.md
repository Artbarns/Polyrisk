# Paramètres artistiques retenus

Comparaison avec ReflectiveNeon du commit9242725. Toutes les valeurs sérialisées du nouveau profil sont listées ci-dessous. Les teintes sont des coefficients de matériau, pas des couleurs HDR de référence.

| Paramètre | Baseline A | Proposition B |
|---|---|---|
| architectural | — nouveau | 1 |
| pawnStudioReflection | — nouveau | 0.68 |
| pawnEdgeSpecular | — nouveau | 0.045 |
| boardTint | {r: 0.8, g: 0.83, b: 0.87, a: 1} | {r: 0.86, g: 0.89, b: 0.94, a: 1} |
| tileTint | {r: 0.88, g: 0.91, b: 0.95, a: 1} | {r: 0.98, g: 0.99, b: 1, a: 1} |
| boardMetallic | 0.36 | 0.46 |
| tileMetallic | 0.27 | 0.32 |
| boardSmoothness | 0.49 | 0.58 |
| tileSmoothness | 0.45 | 0.48 |
| studioReflection | 0.46 | 0.82 |
| edgeSpecular | 0.052 | 0.035 |
| neonEmission | 0.92 | 0.78 |
| neonReflection | 0.08 | 0.22 |
| reflectionFalloff | 0.03 | 0.055 |
| reflectionReach | 0.1 | 0.16 |
| contactReflection | 0.78 | 0.9 |
| arrivalReflection | 1.1 | 1.3 |
| contactOutline | 1.12 | 0.7 |
| contactReflectionFalloff | 0.065 | 0.1 |
| contactReflectionReach | 0.22 | 0.28 |
| activeSurfaceContrast | 0.14 | 0.08 |
| backgroundSeparation | 0.014 | 0.012 |
| pawnBody | {r: 0.23, g: 0.28, b: 0.33, a: 1} | {r: 0.25, g: 0.29, b: 0.33, a: 1} |
| pawnMetallic | 0.48 | 0.38 |
| pawnSmoothness | 0.46 | 0.48 |
| pawnEmission | 1.1 | 1 |
| pawnRing | 1.12 | 1.1 |
| pawnTop | 1.02 | 1.08 |
| pawnBaseReflection | 0.065 | 0.1 |

## Architecture lumineuse

| Paramètre | B |
|---|---|
| keyEuler | {x: 55, y: -25, z: 0} |
| keyColor | {r: 0.97, g: 0.98, b: 1, a: 1} |
| keyIntensity | 0.68 |
| ambientSky | {r: 0.28, g: 0.31, b: 0.35, a: 1} |
| ambientEquator | {r: 0.13, g: 0.15, b: 0.18, a: 1} |
| ambientGround | {r: 0.045, g: 0.055, b: 0.07, a: 1} |
| studioKey | {x: 8, y: 8, z: 9, w: 12} |
| studioFill | {x: 0, y: 7, z: 10, w: 12} |
| surfaceResponse | {x: 1.25, y: 0.32, z: 0.18, w: 0.035} |
| bloom | 0 |

## Conservé

Épaisseur4,5%, géométrie et index40, quatre meshes fusionnés, caméra orthographique40°/45° et zoom1,40, SMAAHigh, render scale1, exposition+.15, contraste6, saturation−8, tone mappingNeutral. Une Directional, aucune probe temps réel. Pas de nouvelle texture ni de nouvelle passe de shader.

Contact : attaque32ms, décroissance145ms, arrivée215ms ; coefficients temporels et callbacks inchangés. Le graphite reçoit maintenant le contact aussi sur les quatre inlays de coin. La largeur géométrique des rainures reste inchangée. Une couverture par dérivées protège la sélection et la fine accroche du chanfrein ; elle ne garantit pas une taille CSS identique sur chaque DPR.

Les captures finales du banc utilisent B/stage3 dans le QA construit avant le retrait du bloom. Ce stage correspond visuellement au stage4 livré avec bloom0. Les anciennes captures stage4/B avec bloom.045 sont des essais archivés, pas le réglage final.
