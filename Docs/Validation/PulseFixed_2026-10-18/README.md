# Pulse — fiche permanente et présentation centrale

Direction confirmée par Arthur le 18 octobre 2026 après rejet des changements automatiques de hauteur de Pulse v2.

## Comportement

- Fiche compacte : même cadre de 236 unités pendant Planning, lancer, déplacement, arrivée, achats et paiements. 220 unités en paysage court. Propriété, résumé financier et actions conservent leur place ; les cases spéciales gardent aussi leurs emplacements. Le HUD compact de la passe précédente est conservé.
- Un clic volontaire sur « Lancer » referme l’inspecteur étendu avant le début du jet. Inspection et extension accessibles uniquement sur le tour humain local. Pendant les tours adverses : case de l’action suivie, données atténuées mais lisibles et commandes désactivées. Le journal reste consultable.
- Dés sans fenêtre au centre de la zone de plateau disponible. En paysage, cette zone occupe l’espace libre à gauche de la fiche. L’échelle se calcule sur les dimensions de cette zone plutôt que sur la taille du panneau.
- Une seule file chronologique présente les dés puis les transactions confirmées. Un paiement ne passe plus après le lancer du joueur suivant ; aucun des deux ne recouvre l’autre. Le jeu et le réseau continuent selon leurs délais existants : cette file concerne uniquement la présentation.
- Achat confirmé : vert ; passer : jaune ; paiement : rouge. Message central avec acteur / montant / bénéficiaire et impulsion brève du contour, sans agrandissement du widget. Les couleurs complètent les libellés.
- Les coordonnées des dés s’ancrent à un point au sol stable. Les faces du cube partagent leurs arêtes, ce qui retire les interstices de la projection précédente. Rebonds simples, suspension du deuxième dé sur son coin, tac, total derrière et signature de double conservés.
- Réduire les animations supprime les rebonds et les transitions, tout en conservant le temps de lecture des messages. « Abréger » termine seulement la présentation des dés ; aucun nouveau résultat n’est généré.

## Vérification

Unity 2022.3.62f3, WebGL : 612 assertions réussies (143 économie / intégration UI, 148 mécaniques, 17 événements, 304 assets UI sur 17 écrans). Les tests couvrent notamment le cadre conservé pendant les dés, l’inspection refusée pendant les tours adverses, les destinataires d’un paiement et l’ordre paiement → lancer du joueur suivant.

La recette visuelle porte sur le canvas du WebGL compilé, pas une maquette HTML. Les captures et le GIF enregistrent le rendu réel ; ils ne constituent pas une mesure de fps. Aucun test sur téléphone physique revendiqué. Règles, économie, résultats RNG et protocole réseau conservés.

## Reprise

- `baseline/` : les dix sources avant cette passe.
- `changes-from-pulse-v2.patch` et `source-inventory.json` : modifications ciblées et empreintes.
- `unity-build.log` et `build-inventory.json` : compilation et identité de l’export.
- Build principal : `Builds/PulseFixedV3` ; les exports précédents restent conservés.

Lancer depuis la racine du projet principal :

```sh
python3 Docs/Validation/PulseFixed_2026-10-18/serve_webgl.py --build-root Builds/PulseFixedV3 --port 8977
```

Ne pas réintroduire le mode automatique `pulse-panel--dice`, le masquage du widget, ni le lancement direct des dés en dehors de la file centrale. Les détentes manuelles de l’inspecteur restent distinctes des événements de jeu.

## Recette et captures

- `final-property-430.jpg` / `final-property-320.jpg` : fiche et cinq valeurs sur un étage.
- `ship-inspector-430.jpg` / `ship-inspector-landscape.jpg` : dernier export, inspection volontaire et deux colonnes en paysage.
- `lancer-fiche-fixe.gif` : rebond, coin, tac, total et arrivée ; cadre inchangé.
- `achat-paiement-tours.gif` : achat et paiements confirmés avec montants / bénéficiaires complets, puis les lancers suivants.
- `capture-timing.json` et les JPEG numérotés : horodatage des captures. Les GIF sont échantillonnés à la cadence de l’outil de capture, avec un arrêt final de lecture ; ils ne mesurent pas la cadence du moteur.

Les GIF du cadre compact proviennent de l’export de recette précédent de cette passe. Les seules retouches ultérieures sont la fermeture explicite du cran étendu au clic Lancer et sa largeur de 70 % en paysage ; elles ont été vérifiées dans le dernier export (`ship-*`). Le format compact reste à 54 % en paysage et 236 unités de hauteur en portrait.

Dernière recette : clic Lancer depuis l’inspecteur étendu (`ship-close-*`), mode réduit avec résultat immédiat (`ship-reduced-motion.jpg`), puis option d’animation rétablie. Confirmation jaune de refus d’achat observée dans le WebGL ; confirmations verte d’achat et rouge de paiement enregistrées dans le GIF des transactions. Les restrictions d’inspection adverse font aussi partie des assertions d’intégration.
