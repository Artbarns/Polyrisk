# Notifications et rotation du viewport — 15 octobre 2026

## Correctif

Les lignes du feed utilisaient `white-space: nowrap` et `text-overflow: ellipsis`; une notification longue, telle qu'un loyer avec le nom du bâtiment, perdait sa fin dans la carte en demi-largeur. Le style autorise désormais le retour à la ligne et conserve le message complet.

## Recette

- Build WebGL Release exécuté dans le navigateur, à DPR 1.
- 1280 × 720 : deux notifications actives sont visibles côte à côte. « Joueur 2 lance 2 + 2 · total 4 » et « Joueur 1 paie $200 de loyer · Deep Ocean Drill » apparaissent ensemble; le second message revient à la ligne et reste complet.
- Rotation vers 430 × 840 : le feed adopte sa capacité compacte d'une notification. L'événement prioritaire reste lisible; le plateau et le panneau inférieur avec ses actions restent visuellement séparés.
- Console navigateur : aucune erreur JavaScript.
- Régressions Unity : économie 114 assertions PASS; mécanique 117 assertions PASS. `WEBGL PLAYER BUILD PASS`.
- Payload WebGL compressé : 12 678 114 octets (12,09 MiB). Comparé au build juste avant le correctif USS : +10 octets; le wasm reste identique.

Le test confirme la disposition et l'absence de chevauchement à ces viewports. L'orientation physique et l'interaction tactile après rotation restent à vérifier sur iPhone/Safari.
