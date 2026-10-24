# Refonte UI/UX — trois DA sur la capture réelle

17 octobre 2026. Demande explicite : reprendre le jeu réel fourni en capture et proposer trois refontes de l'interface complète pour valider une direction artistique avant handoff au builder.

## Référence et statut

Capture fournie par Arthur : `/Users/arthurbarreau/Desktop/Capture d’écran 2026-10-17 à 11.39.04.png`, 764 × 1460.

La capture montre le Top HUD (capital 18K, patrimoine 18K, tour 1/50, marché stable), une rangée de quatre joueurs dont les noms et statuts débordent, le plateau isométrique et ses badges, puis un dock d'état au-dessus de la fiche Temu Sweatshop Terminal et des boutons BUY ($1K) / PASS.

Arthur rejette la disposition et le style du statut joueurs et du bandeau d'état. La présence des joueurs reste intéressante si elle est mieux intégrée. Les premiers schémas de panneau d'état ne reproduisaient pas assez la vraie interface : ne pas les traiter comme une DA validée.

Aucune nouvelle DA n'est validée à cette étape. Aucun code runtime modifié.

## Preview

`/Users/arthurbarreau/.codex/visualizations/2026/10/17/01a1498f-6c87-7e31-b929-4d90b504495c/polyrisk-ui-trois-da.html`

Le plateau est une fenêtre CSS sur la capture originale, incorporée comme donnée PNG une seule fois. Les HUD, joueurs, événements, fiche de case, actions et journal sont des éléments interactifs distincts. Le plateau est statique : il ne prouve ni l'animation des pions ni le rendu 3D final d'une future intégration. Ne jamais embarquer cette capture comme plateau de production.

## Pulse — graphite et menthe

- Fond graphite `#10141e`, surface `#191e29`, action menthe `#b9eed9`, texte `#f6f7f9`, secondaire `#a5aebe`.
- Capital dominant, patrimoine secondaire, marché et tour dans une ligne calme ; suppression des séparateurs verticaux lourds.
- Joueurs dans un rail intégré au HUD : numéro, nom complet, marqueur de tour sous le joueur actif. Pas de quatre grandes boîtes avec des libellés qui débordent.
- Événement confirmé dans une petite carte au-dessus du plateau : acteur, action, montant / résultat. Distinct de la fiche de case ; pas de répétition du vieux bandeau au-dessus des actions.
- Fiche de case compacte, rôle et nom de case, achat avec prix dans le même bouton ; Passer secondaire. Journal accessible à cet endroit.
- Direction recommandée pour une évolution cohérente du jeu existant ; reste un avis du directeur, pas une validation utilisateur.

## Arena — compétition et énergie

- Fond bleu nuit `#0b1226`, surface `#182442`, action citron `#d7ff60`, texte `#f1f6ff`.
- HUD plus angulaire, chiffres affirmés et touche typographique sportive.
- Quatre joueurs dans une colonne latérale compacte sur la zone plateau ; plus de rangée sous le HUD. Numéros et couleurs constants, nom consultable et actif signalé par forme et texte.
- Annonce de l'action dans un cartouche de commentaire à droite du rail ; montants plus expressifs. Éviter tout masquage de case utile lors de l'intégration réelle.
- Fiche de case comme ticket, bord haut citron, commandes franches et plus carrées.

## Atelier — papier et corail

- Fond papier `#f1eee6`, texte encre `#292c2b`, surface `#e6e1d7`, action corail `#d95e43`, état stable vert sombre `#345b47`.
- Typographie éditoriale avec sérif pour la marque, le capital et le nom de case. Fort changement de DA, palette claire à contrôler sur les écrans cibles.
- Plateau réel conservé dans une scène sombre arrondie. Les joueurs descendent entre le plateau et la fiche de case pour rapprocher tour et décision.
- Reçu clair dans la scène de plateau ; fiche de case ouverte et simple en dessous, sans empilement de bandeaux.
- Bouton achat corail ; Passer sobre. Pas de recoloration du plateau requise à ce stade.

## Interactions démontrées

- Sélection d'un joueur : fiche de présence / tour actif. Pas d'informations stratégiques privées inventées.
- Menu, journal, achat local et passage local simulés.
- « Voir le tour adverse » : Player 2 fait 8, se dirige vers French Riviera, achète pour 1 000 $ ; Player 3 verse ensuite un loyer de 420 $ au joueur local. Ces faits sont une séquence fictive de démonstration, pas un historique réel ni un prix tiré des règles.
- Les commandes d'achat sont désactivées pendant le tour adverse.
- Appui : compression maximale du bouton vers 0,97 ; nouveau reçu : entrée de 7 unités sur 230 ms. Pas de mouvement continu. Réduction du mouvement respectée.
- La somme des dés peut être rappelée en information durable sans rejouer la scène de lancer validée.

## Contraintes du futur handoff

Après choix de DA, produire tokens, hiérarchie, règles portrait / paysage / 2–6 joueurs, états du composant, variantes d'événements et critères de validation. Préserver Unity 2022.3 et UI Toolkit runtime, les sources d'événements confirmés et le réseau.

La position des overlays sur le vrai plateau doit respecter la caméra, les cibles tactiles et la visibilité des badges. Les montants et destinations proviennent du jeu autoritatif ; aucune inférence prématurée avant Turbo / Précision. Pour les transferts, distinguer total payé, bénéficiaires et impact local. L'état courant ne doit pas réattribuer un ancien reçu à un nouveau joueur actif.

Ne pas copier une propriété CSS sans équivalent USS : volume, ombres, arrondis, sérif et alignements devront être adaptés aux ressources et API réellement disponibles. La preview valide une intention visuelle, pas encore une implémentation Unity.

## Vérifications de la preview

Contrôle navigateur de la présence des trois directions et de la composition sur le plateau de la capture. Pulse : simulation du tour adverse, joueur actif et désactivation des actions locales observés. Arena et Atelier : captures visuelles examinées. Pulse et Arena également examinés avec viewport navigateur de 360 px ; le rail latéral Arena dispose d'un couloir distinct du plateau pour éviter de recouvrir ses badges. JavaScript vérifié par `node --check`. Ces contrôles portent sur la maquette HTML, pas sur Unity ni sur un téléphone physique.
