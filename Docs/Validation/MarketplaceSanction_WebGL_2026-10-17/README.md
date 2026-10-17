# Recette WebGL — audit ciblé Marketplace — 17 octobre 2026

## Résultat

**PASS pour l’action d’audit à deux clients**, puis **PASS visuel du panneau de sanctions** sur la Release finale à 992 × 720 CSS. Le premier contrôle a confirmé l’envoi depuis le sélecteur jusqu’au snapshot autoritaire. La Release finale garde ce parcours et corrige le défilement horizontal parasite.

| Contrôle | Résultat observé |
|---|---|
| Parcours UI hôte | Open Market → Hostile Sanctions → Targeted Audit → Player 1 → Confirm |
| Sélecteur à 992 × 720 | Confirmation verte pleine largeur, entièrement visible |
| Solde initial QA | Hôte 18 000 $ ; invité 18 000 $ |
| Coût de l’audit | Hôte : −2 000 $, solde final 16 000 $ |
| Taxe | Invité : −900 $, soit exactement 5 % de 18 000 $ ; solde final 17 100 $ |
| État serveur | version 5 → 6 ; commande appliquée 1 → 2 ; tour transmis à l’invité |
| Activité autoritaire | `déclenche un audit ciblé contre Joueur 2`, emphase active |
| Client invité | HUD affiche capital et patrimoine à 17,1 K$ et le tour actif |
| Console navigateur | 0 erreur observée pendant les recettes |

Le snapshot autoritaire contient les deux soldes et l’activité d’audit. La capture invitée montre le nouveau solde et le tour suivant ; l’entrée du panneau LOG n’a pas été capturée ouverte. La recette d’action a utilisé une fixture QA sur un serveur local isolé, hôte à Marketplace en `ResolveTile`, avec 18 000 $ chacun. Aucune règle, donnée ou économie n’a été modifiée.

## Correctif visuel et nouvelle vérification

`Marketplace_Sanctions.uxml` limite maintenant le ScrollView à l’axe vertical, masque la barre horizontale et garde le curseur vertical accessible. `Theme.uss` supprime les flèches natives et donne au curseur un accent violet. Dans la Release finale, le clic sur la piste fait défiler la liste et révèle Production Strike et Industrial Sabotage ; le titre reste fixe. La piste verticale reste gris clair et les cartes hautes laissent apparaître le début de l’option suivante au bas de l’écran. La lecture des quatre options et leur accès sont confirmés ; l’apparence du rail peut encore être affinée. Le geste tactile Safari/iPhone n’a pas été vérifié.

Unity 2022.3.62f3 : Economy **114/114**, Mechanics **141/141**, événements **12/12**, UI **252/252** (519 assertions), build WebGL Release `BuildOptions.None` PASS. Artefact local non publié : `/tmp/PolyRiskSanctionsPanelPolishWebGL_20261017`. Fichiers compressés principaux + loader : **12 700 348 octets**. Journal : `/tmp/PolyRiskSanctionsPanelPolishWebGL_20261017.log`.

## Captures

- [Recette de l’audit avec barres originales](01-sanctions-992x720.png)
- [Choix de la cible](02-target-select-992x720.png)
- [Confirmation pleine largeur visible](03-confirm-visible-992x720.png)
- [Client invité après la sanction](04-guest-after-audit-992x720.png)
- [Release finale, sanctions en haut de liste](05-final-release-sanctions-top-992x720.png)
- [Release finale, sanctions après défilement](06-final-release-sanctions-scroll-992x720.png)

## Portée

La commande d’audit a été validée sur deux clients Chromium WebGL ; la vérification visuelle finale contrôle le panneau et son défilement à 992 × 720. Elle ne remplace pas le test Safari/iPhone physique, ne confirme pas l’apparition visuelle de l’activité dans le panneau LOG et ne recouvre pas les autres parcours de sanction ni les enchères Marketplace. La tentative antérieure sans confirmation reste documentée dans `../MarketplaceSanction_WebGL_2026-10-16.md`.
