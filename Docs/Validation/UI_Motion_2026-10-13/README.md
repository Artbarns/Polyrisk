# PolyRisk UI visuelle — contrôle V21, 13 octobre 2026

Périmètre : aucune règle ni donnée économique modifiée.

Le motif historique du plateau déterminait la couleur par position. Une comparaison des 26 assets `PropertyData` avec leurs `FamilyData` a trouvé 17 propriétés dont la couleur de la case ne suivait pas la famille. `PolyRiskBoard` lit désormais la famille de chaque propriété pour la texture d'état du shader. Les teintes correspondent à la palette des glyphes UI existante. Les cases au repos utilisent 22 % de l'intensité d'origine ; le highlight de sélection conserve son intensité propre. Le shader, le mesh et le nombre de draw calls restent inchangés.

Build Unity WebGL V21 : Economy 106 + Mechanics 99 assertions PASS, `WEBGL PLAYER BUILD PASS`, sortie 0. Taille du dossier : 12 292 KiB. Le build V20 occupait environ la même taille. Contrôle navigateur local 430×900 : plateau au repos, puis sélection Marketplace avec une seule propriété en surbrillance, choix de Trump Tower et retour au formulaire avec titre et valeur affichés.

Captures : `board_rest_v21.png`, `board_selection_v21.png`. La préversion HTTPS sert l'index V21, identique au build, ainsi que les quatre ressources WebGL répondant 200. Le navigateur local a chargé et exécuté V21 ; le dernier contrôle du chargement public n'a pas abouti parce que le navigateur de recette s'est arrêté. La mesure de fluidité et netteté sur iPhone réel reste ouverte.

Le rendu final des icônes et du pourcentage sur chaque case attend la confrontation avec le mockup cible, indisponible dans cette session. Le chantier reste dans `tasks/todo.md`.
