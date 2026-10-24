# Inspecteur compact — 15 octobre 2026

## Changement

Pendant Planning, quand aucune case n’est sélectionnée, le titre vide de
l’inspecteur se masque ; les actions de tour restent disponibles. À l’arrivée
sur une case, le titre et les actions reviennent. La rangée Rent/Risk/ROI ne
s’affiche que pour une propriété disposant de ses données : une case spéciale
ne réserve donc plus de place à des tirets sans signification. Aucun état,
règle, montant ou calcul de jeu n’est modifié.

## Validation

Dans le build WebGL Release final, le navigateur mobile portrait confirme
l’absence de fiche vide pendant Planning, puis affiche la fiche Venture à
l’arrivée. Le titre « Venture », le badge « Special Tile » et l’action « Draw
Card » restent visibles ; la rangée Rent/Risk/ROI est masquée. Les actions de
tour étaient présentes avant et après le déplacement. La vérification couvre
le masquage et la réapparition dans ce build ; l’affichage des métriques sur
une propriété reste couvert par la recette propriété existante.

Unity 2022.3.62f3 : Economy **114/114**, Mechanics **128/128**, build WebGL
Release `BuildOptions.None` **PASS** (≈12 MiB), code de sortie 0. Build QA
isolé servi sous `/tmp/PolyRiskWebGLValidationFixed`. `git diff --check`
effectué après mise à jour de ce rapport.
