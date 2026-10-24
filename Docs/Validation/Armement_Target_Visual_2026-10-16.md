# Rendu de la cible Cyber-sabotage — 16 octobre 2026

## Présentation

La puce de rendement d’une propriété sabotée reçoit maintenant un contour violet discret tant que `HijackedBy` et `HijackTurnsRemaining` sont actifs. À la sélection, la fiche affiche aussi « CYBER-SABOTAGE · N TURNS ». Les indicateurs lisent les champs déjà présents dans `PropertyData`; aucune règle, donnée économique, sérialisation ou commande n’a changé. Un masque 64 bits des cases actives est vérifié dans la boucle de rafraîchissement existante (150 ms), ce qui actualise la puce lors d’une réplication ou d’une expiration sans allocation, Canvas ni boucle dédiée.

## Vérification

Unity 2022.3.62f3, sur la version finale au masque 64 bits : Economy **114/114**, Mechanics **130/130**, build WebGL Release `BuildOptions.None` **PASS**, sortie isolée `/tmp/PolyRiskArmementBadgeQA` (~12 MiB).

Deux clients WebGL Release étaient connectés au serveur QA local. Le snapshot invité contenait Opioid Paradise (case 8), propriété de l’invité, sabotée par l’hôte pendant trois tours. Côté invité, le contour violet apparaît sur la case correspondante ; en la sélectionnant, sa fiche affiche le nom, la famille Pharma, le loyer/ROI et le badge « CYBER-SABOTAGE · 3 TURNS ». L’état serveur après observation reste version 2/séquence 0, propriétaire siège 1, saboteur siège 0, trois tours. Snapshot expurgé : [state_guest_visible_target.json](Armement_Target_Visual_2026-10-16/state_guest_visible_target.json).

La mutation a été semée par la fixture QA pour isoler le rendu. L’application réseau réelle, l’énergie dépensée et les trois tours après rechargement sont déjà vérifiés dans [la recette Armement en ligne](Online_Ultimate_Armement_2026-10-15.md). Cette vérification ferme le manque précis de présentation adverse ; elle ne constitue pas une partie humaine ni une recette sur Safari/iPhone. `git diff --check` passe.
