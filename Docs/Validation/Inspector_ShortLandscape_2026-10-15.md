# Inspecteur en paysage court — 15 octobre 2026

## Écart observé et correction

Le contrôle du nouveau build à 932 × 430 a montré que la fiche de propriété
plafonnée à 60 % de hauteur recouvrait des cases. Un premier essai à 42 % a
laissé Rent/Risk/ROI trop près des actions. Le correctif V78 détecte les
viewports paysage de 430 px de haut ou moins, limite la fiche à 46 %, et
resserre l’icône, le titre, les marges et les métriques. Les trois stats et les
actions restent présentes, avec une cible tactile de 40 px. Le portrait et le
paysage de plus de 430 px gardent leurs règles existantes. Aucun calcul ou état
de jeu n’a changé.

## Validation

Unity 2022.3.62f3 : `EconomyRegression` **114/114**, `MechanicsRegression`
**128/128**, build Release `BuildOptions.None` PASS (≈12 MiB), code 0.
L’artefact QA est `/tmp/PolyRiskShortLandscapeFixV2` et son journal
`/tmp/PolyRiskShortLandscapeFixV2.log`.

Le client Release a été servi localement avec les en-têtes compressés attendus.
En 932 × 430, le plateau reste visible au-dessus de la fiche. Après arrivée sur
Trump Tower, Rent **$90**, Risk `N/A`, ROI **10 %**, le slider et Pass sont
lisibles ; glisser le slider valide exactement un achat de **$900** et le HUD
converge à **$17,1 k$**. Quit Game ouvre sa confirmation et revient au menu.
En 430 × 932, accueil et plateau restent utilisables après le changement
d’orientation.

Deux clients WebGL Release sur des origines locales distinctes ont rejoint le
même lobby isolé, passé Ready, démarré, puis vérifié les commandes et la
réplication : l’hôte a lancé les dés, pioché Venture #17 et confirmé la carte ;
l’invité a ensuite joué et payé la taxe de $600. Les deux HUD ont convergé à
**$17,4 k$** et le même écran affichait WAIT chez l’observateur. API QA sur
5094, stockage temporaire isolé sous `/tmp/PolyRiskShortLandscapeQA`, supprimé
après le test. Cette recette WebGL à deux onglets ne remplace pas un contrôle
Safari sur appareil physique ni une partie humaine à six.
