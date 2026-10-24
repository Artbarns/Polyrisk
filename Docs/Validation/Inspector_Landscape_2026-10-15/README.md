# Fiche inspecteur en paysage large — 15 octobre 2026

## Changement

En WebGL Release à 1280×800, la fiche sélectionnée occupait environ 58–60 %
la hauteur et cachait une partie inutile du plateau. `UIUXRuntime` applique
maintenant une classe à la fiche uniquement lorsque l’écran est en paysage
large (largeur supérieure à la hauteur, hauteur supérieure à 430 px). La fiche
est plafonnée à 50 % ; son badge icône, ses statistiques et ses espacements
sont compactés. Les actions conservent leurs tailles tactiles existantes.

Le seuil laisse les écrans portrait et paysage de hauteur ≤430 px sur leurs
règles actuelles. La détection utilise le callback de géométrie existant ; le
changement n’ajoute ni Canvas, ni mesh, ni dépendance.

## Vérification

Build Unity 2022.3.62f3 Release `BuildOptions.None` : PASS, 12 MiB.
Economy Regression : 114 assertions PASS. Mechanics Regression : 110 assertions
PASS. Le build chargé en navigateur 1280×800 démarre une partie locale, résout
un lancer vers French Riviera et affiche l’inspecteur (famille/niveau,
Rent/Risk/ROI, slider d’achat, Pass) sans erreur JavaScript.

Comparaison visuelle : `before_1280x800.png` et `after_1280x800.png`, mêmes
dimensions CSS. Le bord supérieur de la fiche descend d’environ 72 px, de 41 %
à 50 % de la hauteur de l’écran, laissant davantage de plateau visible ; toutes
les statistiques et les deux actions restent visibles.

## Limites

Ce contrôle porte sur Chrome/WebGL à 1280×800 ; il ne remplace pas la mesure de
cadence/chauffe et de taille tactile sur iPhone/Safari physique. Les captures
cibles du mockup restent indisponibles pour une comparaison fidèle écran par
écran.

## Contrôle du panneau vide sur la source actuelle

Le premier contrôle 932×430 effectué depuis l’ancien dossier de build ne
correspondait pas à la dernière version des scripts/styles UI. Un Release neuf
a été reconstruit depuis la source actuelle avec Economy 114/114 et Mechanics
128/128 PASS. En Chrome 155/Metal, après lancement local, l’inspecteur vide est
maintenant masqué en Planning en 430×900 comme en 932×430 ; seuls le plateau,
le HUD et le dock ROLL DICE/PASS restent affichés. Les deux vues ont zéro
erreur JavaScript. Captures : `empty_sheet_hidden_430x900_current_source.png`
et `empty_sheet_hidden_932x430_current_source.png`.

## Cadrage adaptatif du plateau en Planning

Le build Release le plus récent cadre le plateau dans la zone libre entre le
HUD et le dock, uniquement en Planning à ≤430 px de hauteur. Il utilise les
dimensions réellement rendues de ces panneaux et les 40 ancrages du plateau ;
quand le tour quitte Planning, il restaure la caméra normale avant l’arrivée
sur la case. Aucun changement n’est apporté aux boutons ou à la logique de
sélection.

Unity 2022.3.62f3 : Economy 114/114, Mechanics 130/130, Release
`BuildOptions.None` PASS (~12 MiB). Dans Chrome 155/Metal, à 932×430, le plateau
est nettement plus grand qu’avant sans passer sous le HUD ni le dock. Au
premier lancer, Deep Ocean Drill ouvre sa fiche, ses métriques Rent/Risk/ROI et
la glissière ; aucune erreur JavaScript. Captures :
`compact_planning_frame_932x430_current_source.png` et
`planning_property_after_roll_932x430_current_source.png`.

La lisibilité finale des badges doit être confrontée au mockup, absent de cette
session, et mesurée sur Safari/iPhone physique. Le contrôle tactile réel reste
ouvert.
