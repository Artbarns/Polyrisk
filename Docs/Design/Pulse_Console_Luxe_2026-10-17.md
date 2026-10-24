# Pulse — console sombre, minimale et technique

17 octobre 2026. Raffinement proposé après le choix explicite de **Pulse** par Arthur et ses références Photo 1 / Photo 2. La DA sombre est validée comme direction ; cette organisation du panneau reste une proposition à examiner. Pas d'intégration Unity dans ce chat.

## Référence interactive

`/Users/arthurbarreau/.codex/visualizations/2026/10/17/01a1498f-6c87-7e31-b929-4d90b504495c/pulse-console-luxe.html`

Nouvelle itération séparée de `pulse-centre-controle.html` pour conserver la référence antérieure. Le plateau de Photo 1 est incorporé dans la maquette, comme contexte statique.

## Organisation

- **Premier cran** : poignée, bandeau financier sur deux filets fins, raccourci Portefeuille avec nombre d'actifs, propriété / propriétaire / niveau, progression de famille x/3 et trois petits repères de collection, loyer / ROI brut / coût au GO, capital après achat et commandes Acheter / Passer.
- **Second cran** : onglets Case / Portefeuille / Finances. Case présente les trois propriétés de la famille et leurs propriétaires, risque non applicable et maintenance. Portefeuille présente la collection et les loyers par propriété ; une inspection distante porte un retour explicite à Fentanyl Labs. Finances détaille Sgo, dividendes, maintenance, taxe Hubs, net estimé et loyers déjà encaissés séparément.
- Bandeau « Prochain GO — estimation actuelle » ouvrant directement les finances. Les échéances Sgo / dividendes / maintenance sont celles du GDD, pas des versements garantis à chaque tour.
- Repli au premier cran : retour à la **case actuelle**, même après consultation d'un autre actif. Les actions d'achat ne sont jamais permises sur une propriété consultée à distance.
- Le menu du HUD est conservé comme menu, avec entrées vers portefeuille / finances ; le raccourci portefeuille est désormais visible sans ouvrir le second cran.

## DA et mouvement

Graphite `#101419`, console `#151b23`, lignes `#35404b`, accent menthe désaturé `#b6ddcb`. Peu de surfaces remplies, pas de glow continu, nombres lisibles, icônes fines. L'accent indique action / possession ; la collection utilise aussi les nombres et libellés.

Transition entre deux positions stables sur 340 ms, Bézier (0.2, 0.8, 0.2, 1). Poignée cliquable, glissable et utilisable au clavier (haut/bas) ; onglets gauche/droite. Mode réduit : changement direct. Aucun déplacement du pion ni temps moteur commandé par l'inspection.

La maquette utilise un cadre portrait de référence de 960 px (1000 px en étroit), et un panneau de 390 / 680 px (405 / 710 px en étroit). Ces chiffres servent à comparer le contenu : le builder doit calculer les crans à partir du viewport réel, des safe areas et de la hauteur du HUD ; ils ne sont pas des dimensions de production à imposer à tous les appareils. En étendu, le panneau recouvre volontairement le plateau ; l'activité centrale masquée ne doit pas dépasser de son bord.

## Sémantique des exemples

Données fictives de possession mais cohérentes : 18 000 $ de capital, 8600 $ d'actifs de niveau 0, patrimoine 26 600 $. Fentanyl Labs disponible à 1300 $, loyer 130 $, ROI brut loyer/prix 10 %. Pharma 1/3 ; achat simulé : capital 16 700 $, Pharma 2/3, portefeuille de 3 à 4 actifs, patrimoine inchangé.

Tous les actifs étant de niveau 0 et les familles incomplètes, maintenance et dividendes valent zéro. Sgo illustré 2250 $, net estimé 2250 $. +420 $ de loyers déjà encaissés n'est pas ajouté au versement futur. Pour une vraie partie, lire les données autoritatives et les modificateurs de cycle ; ne pas introduire de nouveau calcul économique dans la présentation.

## Vérification de cette itération

JavaScript vérifié. Parcours navigateur : cran compact puis étendu, portefeuille, inspection Antarctica Datacenter et retour à la case actuelle, achat, mise à jour capital / compteur / Pharma, finances. Composition examinée sur format normal et à 360 px. Les rectangles du contenu famille et du pied d'actions ont été contrôlés sans recouvrement dans l'inspection étendue normale. Le geste glissé et le rendu sur matériel tactile physique ne sont pas validés par ces tests ; aucune affirmation de compilation Unity ou de build WebGL.

Après retour d'Arthur, figer l'organisation et préparer un handoff ciblé UI Toolkit avec états (case disponible / possédée / adverse, tour local / adverse, manque de fonds, famille complète, hypothèque), liens aux données et vérification 2–6 joueurs / portrait / paysage / mouvement réduit.
