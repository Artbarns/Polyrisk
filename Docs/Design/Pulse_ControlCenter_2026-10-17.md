# Pulse — centre de contrôle à deux crans

17 octobre 2026. **DA Pulse validée par Arthur.** Ce document décrit le raffinement proposé du bottom widget ; ses détails ne sont pas encore validés. Aucun code runtime modifié.

## Intention

Retrouver le contenu utile du concept de base : identité de la case, specs, progression de famille, actions, portefeuille et bilan financier. Conserver la DA Pulse pour le HUD, les joueurs et la palette. Pas de retour du vieux bandeau d'état au-dessus de la fiche.

Références utilisateur : Photo 1 (Fentanyl Labs, prix 1 300 $, loyer 130 $, ROI 10 %, risque N/A, slide to buy), Photo 2 (contrôle de propriété / portefeuille dans une scène isométrique). La nouvelle preview conserve le plateau issu de Photo 1 par une fenêtre CSS sur l'image ; le plateau est statique et ne représente pas une intégration Unity.

Preview autonome UTF-8 : `/Users/arthurbarreau/.codex/visualizations/2026/10/17/01a1498f-6c87-7e31-b929-4d90b504495c/pulse-control-test.html`. Variante cadrée iPhone 430 × 932 : `pulse-centre-controle-mobile-fit.html` dans le même dossier.

## Deux crans

- **Essentiel** : poignée, estimation du prochain GO (Sgo / coûts / net), identité de case et propriétaire / niveau, famille x/3, loyer / ROI brut / maintenance au GO, prix, capital après achat, Acheter / Passer. La simulation affiche le cas Fentanyl Labs disponible.
- **Étendu** : même poignée et même bandeau financier, trois vues Case / Portefeuille / Finances. L'inspecteur ajoute les trois propriétés de la famille et leurs propriétaires ; le portefeuille permet d'inspecter une propriété avec un retour explicite à la case actuelle ; les finances détaillent les revenus et coûts à leur bonne échéance.
- Le premier cran cache les onglets pour conserver la priorité à l'action. Le bandeau financier ouvre directement le second cran sur Finances. Le bouton menu ouvre le portefeuille dans cette maquette ; cette affectation doit être réévaluée pour le vrai menu de partie.
- Poignée activable au clic, au tactile / glisser et au clavier ; flèches haut/bas pour les crans. Les onglets gèrent les flèches gauche/droite.

## Hiérarchie et sémantique

Le x/3 représente les titres possédés par le joueur local, pas le niveau d'upgrade ni les chances de réussite. Le niveau de propriété est affiché séparément. La famille incomplète explique le verrouillage des améliorations et dividendes.

Le « ROI brut » illustré vaut loyer / prix d'achat ; il n'est pas un rendement garanti par tour. Le champ Risque de l'ancien écran est N/A : ne pas inventer un score de risque pour une propriété. Si une activité possède une mesure définie et confirmée par les règles, l'afficher seulement dans son contexte.

D'après `Docs/GDD_Economy.md`, Sgo, dividendes, maintenance et taxe des Hubs sont réglés **au passage GO**. Le bandeau porte donc « Prochain GO — estimation actuelle », pas « revenus par tour ». Les loyers déjà encaissés sont séparés, avec leur période (« depuis votre dernier GO »). Ne pas additionner ces loyers à une promesse de versement au prochain GO.

## Données illustratives

- Capital 18 000 $, patrimoine 26 600 $, trois propriétés de niveau 0 : Opioid Paradise (1 100 $), Antarctica Datacenter (3 500 $), Boston Dynamics Factory (4 000 $). Aucune famille complète, pas de bonus de monopole ni investissement en upgrade dans cet exemple.
- Pharma 1/3 ; Tech 2/3. Pfizer-BioTech appartient à Player 3 ; Quantum AI Labs appartient à Player 2 dans cette fiction.
- Fentanyl Labs disponible, prix 1 300 $, loyer de base 130 $. Ces deux paramètres sont confirmés dans l'asset du projet et le générateur. Un achat simulé donne capital 16 700 $, patrimoine inchangé 26 600 $, quatre propriétés, Pharma 2/3.
- Sgo illustré 2 250 $, maintenance et taxe Hubs 0, dividendes 0, net actuel estimé +2 250 $. Aucun coût aux niveaux 0. +420 $ de loyers déjà encaissés est une donnée fictive de démonstration.
- Les valeurs de possession / partie sont des exemples cohérents, pas une lecture de la partie en cours.

## Motion

Panneau : transition de hauteur sur ~340 ms avec courbe (0.2, 0.8, 0.2, 1). Deux destinations stables ; pas de panneau libre qui finirait entre deux crans. L'achat donne une petite confirmation et une impulsion de la progression de famille sur 250 ms. Mouvement réduit : changements d'état directs, sans impulsion.

## Pour le futur builder, après validation

Inspecter le bottom sheet réel, UI Toolkit, les helpers et les événements confirmés ; ne pas embarquer le HTML ni la capture comme interface du jeu. Conserver les règles, la caméra et la confidentialité des données adverses.

Le même inspecteur doit distinguer case actuelle, case simplement consultée et portefeuille local. Une inspection ne déplace pas le pion et ne donne pas le droit d'acheter une case distante. Les boutons dépendent du vrai tour et des permissions du moteur. Les valeurs du propriétaire adverse restent limitées aux informations publiques.

Le bilan prévisionnel doit être produit par un calcul de lecture cohérent avec `FinanceSystem.ProcessGoIncome` et les événements confirmés, sans recalculer / appliquer une transaction dans l'UI. Présenter l'absence de données comme telle ; jamais une estimation 0 par défaut qui ressemblerait à un chiffre confirmé.

À vérifier dans l'intégration : achat, passe, propriétaire local / adverse, choix en cours, manque de capital, hypothèque, famille complète, upgrade, risques contextuels, données de GO modifiées par un cycle ou une capacité, 2–6 joueurs, portrait / paysage, petits écrans, commandes clavier / tactile et préférence réduite. Les deux crans doivent se dimensionner au viewport et garder les actions accessibles, sans masquer le HUD ni faire déborder le contenu.

## Vérification de cette preview

Syntaxe JavaScript vérifiée. Parcours navigateur contrôlé : ouverture / fermeture des crans, accès direct aux finances, onglets, portefeuille → inspection d'Antarctica Datacenter → retour à la case actuelle, achat de Fentanyl Labs, capital 16 700 $, portefeuille 4 et Pharma 2/3. Composition examinée en format normal et avec viewport de 360 px. Mesure des rectangles du premier cran : les statistiques et le pied d'actions ne se recouvrent pas sur ces formats. Le geste glissé est implémenté, mais le tactile physique, Unity et le build WebGL ne sont pas validés par cette recette HTML.

## Variante cadrée pour iPhone — contrôle du 17 octobre

Preview mobile autonome : `/Users/arthurbarreau/.codex/visualizations/2026/10/17/01a1498f-6c87-7e31-b929-4d90b504495c/pulse-centre-controle-mobile-fit.html`. Le fragment direct n'a pas d'en-tête HTML/viewport ; utiliser le wrapper UTF-8 autonome ou cette variante, pas ouvrir le fragment brut comme page.

En viewport simulé **430 × 932**, le premier cran garde Acheter et Passer entièrement dans le cadre : le bas du bouton est à 895 px, dans le viewport de 932 px, sans débordement horizontal. Parcours HTML effectué : repli/dépli, finances, portefeuille, inspection de la propriété Tech et retour à Fentanyl Labs, puis achat simulé ; capital 18 000 → 16 700 $, Pharma 1/3 → 2/3, Patrimoine inchangé à 26 600 $. Rechargement effectué ensuite pour restaurer l’état de démonstration.

Compromis constaté : le second cran de 696 px rend la famille et les actions lisibles, mais masque presque tout le plateau et une partie du rail des joueurs. C'est un point de conception à faire trancher par Arthur ; cette variante reste une preview HTML et n'autorise pas une intégration Unity.
