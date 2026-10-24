# Rebond d’atterrissage des pions — 16 octobre 2026

## Changement visuel

`PawnMovement` conservait l’arc existant, mais ne marquait pas visuellement l’arrivée sur chaque case. La coroutine existante ajoute maintenant un rebond très court (80 ms, 0,055 unité de hauteur) et une légère compression/extension de 6 %/2,5 %. Pour les cases intermédiaires, l’impulsion se superpose au déplacement vers la case suivante ; elle ne rallonge donc pas chaque segment. Seule l’arrivée finale ajoute au plus 80 ms. Une liste de points sans déplacement ne déclenche rien. « Réduire les animations » désactive arc et rebond ensemble.

Aucun asset, objet de scène, Canvas, matériau, plugin ou draw call n’est ajouté. Le coût supplémentaire est limité à quelques calculs et à l’écriture de la position/échelle du pion pendant son animation.

## Validation Unity

- Unity 2022.3.62f3 : `EconomyRegression` **114/114 PASS**.
- `MechanicsRegression` **140/140 PASS**.
- Build WebGL Release, `BuildOptions.None` : **PASS**, processus Unity code 0.
- `git diff --check` : PASS après retrait d’un espace final.
- Build isolé, non publié : `/tmp/PolyRiskPawnBounceRelease_20261016` ; journal `/tmp/PolyRiskPawnBounceRelease_20261016.log`.
- Fichiers compressés principaux : 12 661 440 octets contre 12 661 004 avant le changement, soit **+436 octets (+0,0035 %)**. Avec le loader, 12 682 120 octets. Cette mesure est un proxy du téléchargement, pas un profil GPU ou FPS.

## Limite

Le mouvement n’a pas été observé en navigateur WebGL après ce build : l’outil de navigateur a refusé l’ouverture de l’origine QA lors de la recette précédente. Il reste à vérifier visuellement l’impulsion sur un vrai déplacement, puis à comparer le rendu avec « Réduire les animations » activé/désactivé. Le build reste local et n’a pas remplacé la préversion partagée.
