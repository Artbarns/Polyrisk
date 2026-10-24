# Modale de sortie en écran étroit — 15 octobre 2026

## Écart et correction

À 332 × 763 px, le titre « LEAVE THIS GAME? » dépassait du modal. La règle
générique des titres de menu appliquait 34 px aux écrans sous 520 px ; elle
agrandissait aussi le titre de confirmation. Une règle USS plus spécifique
ramène les titres des menus en jeu à 20 px sur ces écrans, avec espacement de
lettres réduit et retour à la ligne autorisé. Aucun code de jeu, Canvas ou
dépendance n’est ajouté.

## Vérification

Le build WebGL Release frais (Unity 2022.3.62f3) a été ouvert dans le
navigateur QA en portrait 332 × 763 px. L’accueil a lancé une partie locale,
le menu de jeu puis la confirmation de sortie ; le titre tient dans le modal,
et Quit/Cancel sont visibles. Cancel revient au menu de jeu. Quit sur cette
partie QA locale revient à l’accueil. Ce test de sortie ne modifie aucune
partie partagée.

Economy **114/114**, Mechanics **128/128**, build Release
`BuildOptions.None` **PASS** (≈12 MiB), code de sortie 0. Journal :
`/tmp/PolyRiskQuitModalNarrowQA_20261015.log`.
