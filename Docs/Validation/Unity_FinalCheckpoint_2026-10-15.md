# Checkpoint Unity Release — 15 octobre 2026

## Résultat

Source du projet recompilé et vérifié dans Unity `2022.3.62f3` au cours d’un
lancement batch unique :

- Régression économie : **114/114 assertions PASS**.
- Régression mécaniques : **128/128 assertions PASS**.
- Build joueur WebGL Release (`BuildOptions.None`) : **PASS**.
- Sortie isolée : `/tmp/PolyRiskFinalCheckpoint_20261015`, environ **12 MiB**.
- Smoke navigateur local : l’application atteint l’écran d’accueil.
- Contrôle HTTP : index, loader, framework, WASM, données et ressources du
  template répondent `200`; framework, WASM et données sont servis en gzip.

Ce checkpoint a été produit sans publier ni remplacer la préversion en ligne.
Unity a affiché au démarrage un avertissement concernant le jeton de licence
(`Access token unavailable, No ULF license found`), mais le batch s’est terminé
normalement avec le build réussi et le message `Exiting batchmode successfully`.

Cette vérification confirme que la source compile et que le build démarre. Elle
ne remplace pas la recette visuelle responsive sur Safari/iPhone physique, ni
les tests de réseau à six clients rendus et les coupures longues.

## Rebuild de la source UI actuelle — 15 octobre, 21:13

L’audit HUD a relevé que le checkpoint initial précédait les dernières
modifications d’`ActionButtonsWidget.cs` et de `Theme.uss`. J’ai relancé le même
pipeline sur la source présente : Economy **114/114**, Mechanics **128/128**,
WebGL Release `BuildOptions.None` **PASS**. Le nouvel artefact isolé est
`/tmp/PolyRiskUIAuditRelease_20261015` (12 MiB) ; il n’a pas été publié.

Chrome 155 accéléré par Metal a démarré une partie locale à 430×900 puis a
basculé à 932×430. Le panneau inspecteur vide est masqué en phase Planning aux
deux orientations, les boutons du tour restent visibles, les 40 repères du
plateau sont rendus, et aucune erreur JavaScript n’apparaît. Captures :
`Inspector_Landscape_2026-10-15/empty_sheet_hidden_430x900_current_source.png`
et `empty_sheet_hidden_932x430_current_source.png`.

À 932×430, le plateau complet tient au-dessus du dock d’actions, mais ses
badges famille/rendement sont visuellement denses et le plateau pourrait mieux
utiliser l’espace disponible. C’est le prochain point d’audit du cadrage ; la
comparaison exacte au mockup et la recette Safari/iPhone restent ouvertes.

## Cadrage adaptatif Planning paysage — 15 octobre, 21:27

Pour utiliser l’espace libre au-dessus du dock en paysage court, la caméra
orthographique se recadre uniquement en phase Planning et pour une hauteur
d’écran ≤430 px. Le calcul réutilise les limites UI Toolkit réelles du HUD et du
dock, cadre les 40 cases avec une marge, et recentre le plateau dans la zone
disponible. À la sortie de Planning, le cadrage de base est restauré. Le test
Editor couvre le calcul dans une zone libre et le cas où le cadrage ne doit pas
décaler la caméra hors du réglage de scène. Aucun mesh, Canvas, shader ou
dépendance n’a été ajouté.

Unity 2022.3.62f3 : Economy **114/114**, Mechanics **130/130**,
WebGL Release `BuildOptions.None` **PASS**. Artefact QA isolé
`/tmp/PolyRiskLandscapeFrameQA3_20261015` (~12 MiB), non publié.
Chrome 155/Metal : entrée en partie portrait, rotation à 932×430, inspecteur
vide masqué et dock d’action visible ; le plateau occupe la zone utile entre le
HUD et le dock. Au premier lancer, cadrage de base restauré, arrivée sur Deep
Ocean Drill et Rent/Risk/ROI + glissière d’achat visibles. Zéro erreur
JavaScript. Captures dans
`Inspector_Landscape_2026-10-15/compact_planning_frame_932x430_current_source.png`
et `planning_property_after_roll_932x430_current_source.png`.

La densité des petits libellés famille/rendement reste à juger face au mockup
qui manque à l’espace de travail, et l’interaction tactile doit encore être
recettée sur Safari/iPhone réel.

## Lisibilité adaptative des badges ROI — 15 octobre

Le libellé du rendement projeté suit maintenant la hauteur réelle de chaque
badge, de 8 à 11 px, au lieu de rester bloqué à 7 px. Le badge conserve sa
largeur et sa hauteur calculées depuis la case ; son padding a été retiré pour
préserver la place du glyphe et du pourcentage sur les cases étroites. Aucun
changement de données, règle, Canvas ou dépendance.

Unity 2022.3.62f3 : Economy **114/114**, Mechanics **130/130**, Release WebGL
`BuildOptions.None` **PASS** (~12 MiB) dans
`/tmp/PolyRiskBadgeLegibilityQA_20261015`. Le rendu a été contrôlé dans Chrome
à 667×375 paysage et 430×900 portrait : icônes de famille et rendements restent
présents, et les panneaux/actions sont visibles. Aucun défaut JavaScript n’a
été relevé. Le filet d’attente Planning de 60 s s’est déclenché pendant le
contrôle long, comme prévu, puis le jeu a affiché l’inspecteur de propriété.
Ces vues restent des mesures navigateur : Safari/iPhone physique et la
comparaison au mockup manquent toujours.

## Cadrage portrait mobile — 15 octobre

La première émulation montrait des repères latéraux légèrement coupés à
430×900. Le cadrage mobile portrait utilise maintenant les limites projetées
des 40 cases et le ratio réel de la caméra, avec la marge déjà appliquée au
mode de sélection. Il s’active en portrait compact lorsque le tour n’est pas
dans un sélecteur ; le dock et la fiche inspecteur ne changent pas.

Unity 2022.3.62f3 : Economy **114/114**, Mechanics **130/130**, WebGL Release
`BuildOptions.None` **PASS** (~12 MiB) dans
`/tmp/PolyRiskPortraitFitQA_20261015`. En Chrome à 430×900, les repères des
40 cases restent dans le cadre ; après un lancer, l’inspecteur French Riviera
affiche Rent/Risk/ROI et Buy/Pass. À 932×430, HUD, plateau et fiche restent
visibles après rotation, mais la rangée d’actions touche le bord inférieur de
la capture : sa marge tactile reste à confirmer sur Safari. Aucun log
JavaScript error/warn durant ce parcours. La vérification ne remplace pas
Safari sur l’iPhone physique.

## Test tactile de la glissière en paysage court — 15 octobre

Sur le même build Release QA, Chrome à 932×430 a rejoint une partie locale,
résolu un lancer vers Pfizer-BioTech, affiché la fiche, puis accepté un glissé
complet de la poignée vers la droite. L’achat a été confirmé, le cash affiché
a changé et le tour suivant a commencé. Le curseur fonctionne donc dans sa
zone visible au bord inférieur du panneau ; aucun ajout de padding, Canvas ou
rebuild visuel n’est justifié par ce contrôle navigateur seul. Pas d’erreur
JavaScript ; un log `ACQUISITION` de niveau warning est émis par le code du jeu
sur l’achat. La marge de sécurité et la barre d’accueil doivent encore être
vérifiées dans Safari sur iPhone paysage réel.
