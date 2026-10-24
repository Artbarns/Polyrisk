# Lisibilité des badges ROI en paysage — 16 octobre 2026

La recette du Release courant a révélé que les badges (icône + rendement) se chevauchaient sur les plateaux paysages compacts. `BoardTileOverlayWidget` réduit maintenant largeur/hauteur, icône et taille du libellé quand le viewport est paysage (rapport largeur/hauteur ≥ 1,35). Le comportement portrait reste inchangé. Aucun changement aux données, règles, caméras, mesh ou Canvas.

- Avant, 932×430 : [before_932x430.png](before_932x430.png)
- Release rebâti, 1280×720 : [after_1280x720.png](after_1280x720.png)
- Release rebâti, 932×430 : [after_932x430.png](after_932x430.png)
- Release rebâti, 430×932 : [after_430x932.png](after_430x932.png)

Unity 2022.3.62f3 : 114 assertions économie, 140 mécaniques, 246 UI (500 au total), puis `BuildOptions.None` WebGL Release PASS. Payload des quatre fichiers Player compressés : 12 685 030 octets (12,09 MiB). Les captures Chromium montrent les ROI plus compacts en paysage et l'ancien dimensionnement conservé en portrait. La lisibilité sur appareil physique, le profilage iPhone/Safari et la chauffe restent à mesurer ; ces captures n'en sont pas une preuve.
