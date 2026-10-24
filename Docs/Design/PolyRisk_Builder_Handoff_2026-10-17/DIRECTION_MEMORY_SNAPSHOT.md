# Mémoire de direction interactive — Arthur / PolyRisk

Mise à jour : 17 octobre 2026. Préférences explicitement exprimées dans le chat de direction motion.

## État de référence — livraison validée du 17 octobre 2026

Les **deux chantiers sont validés** : lancer de dés avec bascule finale puis « tac », et Pulse Dark Luxury Néon avec le cockpit `pulse-inspecteur-visuel.html`. Cet état remplace les mentions de validation en attente dans l’historique ci-dessous. La mémoire conserve ces étapes pour contexte.

- Package commun sauvegardé : [dossier](PolyRisk_Builder_Handoff_2026-10-17/README.md), [archive complète](PolyRisk_Builder_Handoff_2026-10-17.zip), [instructions GPT-6](PolyRisk_Builder_Handoff_2026-10-17/START_HERE_GPT6.md).
- Références approuvées, exports autonomes, sources des démos, sons WAV originaux, paramètres, critères d’intégration et d’acceptation inclus. Le ZIP possède un inventaire SHA-256 vérifiable.
- Les recommandations achat glissé / famille complète / crédit de loyer ont leur preview et restent **optionnelles, non validées**. Les distinguer de la base approuvée.
- Livraison de direction uniquement : aucune intégration runtime ou modification des règles dans ce chat. Le builder doit lier les données réelles et montrer le résultat dans le jeu.
- GO : ne jamais utiliser `FinanceSystem.ProcessGoIncome` pour une estimation visuelle ; cette méthode règle et modifie l’état. Utiliser une projection en lecture seule.

## Façon de travailler

- Toute idée, proposition de motion ou prototype doit avoir un support visuel, idéalement interactif et animé. Proposer plusieurs directions visuellement distinctes lorsqu'un choix reste ouvert, puis affiner avec de nouvelles previews.
- Ce chat sert à la recherche, à la direction créative et aux briefs pour le builder. Ne pas intégrer directement dans le jeu sans demande explicite. Les exports / documents de handoff sont autorisés quand demandés.
- Privilégier la simplicité, la lisibilité et les sensations tactiles. Arthur a explicitement rejeté les dés qui tournent en rond, se croisent ou se mélangent.

## Lancer de dés validé

- Deux dés côte à côte, mouvement simple avec rebond ; premier dé stabilisé avant le second.
- Second dé un court instant sur son arête / point de bascule, légère hésitation puis **tac** sur le résultat.
- Grand total derrière les dés ; aucune addition textuelle du type « 5 + 5 ».
- Total neutre en temps normal ; vert pour un double, avec signature satisfaisante évoquant un « green » NBA 2K : compression locale brève, impulsion verte, DOUBLE et accord sonore bref.
- Trois mouvements simples présentés et appréciés : Rebond vertical, Petit lancer vers l'avant, Glissade courte. Rebond vertical est la recommandation du directeur ; aucun choix exclusif n'a été explicitement arrêté.
- Handoff complet : [brief](DiceMotion_Handoff_2026-10-17/BRIEF_BUILDER.md), [aperçu](DiceMotion_Handoff_2026-10-17/preview.html), [archive](PolyRisk_DiceMotion_Handoff_2026-10-17.zip). Contient aussi les keyframes et des WAV originaux.
- L'export et les previews sont faits ; l'intégration Unity n'a pas été effectuée dans ce chat.

## Panneau d'état — chantier en cours

### Décision la plus récente : Pulse validé

Arthur a explicitement choisi **Pulse** parmi les trois DA. Ce choix remplace le statut « aucune DA validée » des explorations précédentes. Le bottom widget Pulse initial reste à retravailler, pas à intégrer tel quel.

Pour ce bas d'écran, il veut retrouver un **centre de contrôle** : specs de la propriété, progression de famille (x/3), portefeuille et finances. Direction demandée : deux crans simples, un mode principal (inspecteur de case + interactions + synthèse financière), puis un mode étendu pour les informations détaillées. Photos de référence reçues le 17 octobre : `1-Photo-1.jpg` (Fentanyl Labs, loyer/risque/ROI, achat) et `2-Photo-2.jpg` (concept de contrôle financier et propriété).

Une preview affinée est en cours de validation, avec vues Case / Portefeuille / Finances. Le Sgo, les dividendes et les frais du modèle actuel sont réglés au passage GO ; ne pas les présenter comme un paiement garanti à chaque tour. L'intégration runtime n'a pas été autorisée dans ce chat.

Arthur précise conserver une DA **sombre, minimaliste, technique et luxury**. Itération correspondante : `Pulse_Console_Luxe_2026-10-17.md`, preview `pulse-console-luxe.html` dans le dossier de visualisations du chat. Conserver deux crans, un accès portefeuille visible au premier, la collection x/3 et un bilan GO clairement estimatif. Les détails de cette proposition ne sont pas encore validés.

### Historique d'exploration

- Arthur rejette la configuration actuelle du bandeau / Dock d'état.
- Besoin : comprendre ce que fait l'adversaire, son résultat de dés, sa destination / arrivée, ses achats et montants ; suivre le tour sans lire un bandeau compact peu lisible.
- Explorer par previews avant de retenir une direction. Aucune nouvelle configuration validée pour l'instant.
- Nouvelle consigne : prendre la capture réelle `Capture d’écran 2026-10-17 à 11.39.04.png` comme référence de toute l'interface. Les premières previews schématiques étaient trop éloignées. Reconcevoir ensemble le Top HUD, les joueurs, les états et la fiche de case ; garder le vrai plateau en contexte. Trois DA proposées : Pulse, Arena et Atelier, sans choix validé. Voir `UI_DA_Screenshot_2026-10-17.md`. L'objectif est de valider le design puis préparer les instructions / fichiers pour le Builder Codex.
- Correction explicite d'Arthur : les premières previews de panneau étaient trop éloignées de l'interface réelle. Utiliser sa capture comme fond exact pour explorer les insertions, garder le HUD, le plateau, la fiche et les actions existants. Chercher une information moderne, intuitive et compacte, avec détail dépliable ; ne pas remplacer le contexte par un plateau inventé. Capture de référence : `/Users/arthurbarreau/Desktop/Capture d’écran 2026-10-17 à 11.39.04.png`.
- Direction précisée ensuite : Arthur n'aime pas la forme en capsules et veut l'information **au milieu**. Elle doit être assez grande pour lire toutes les informations de l'action, mais assez compacte pour voir ce qui se passe derrière. Explorer une composition centrale ouverte et typographique, sans capsule ou grand panneau opaque, avec de petits détails de motion satisfaisants. Les variantes de disposition précédentes sont écartées ; le choix final entre nouvelles compositions reste ouvert.

Ces notes décrivent des préférences et décisions de conception, pas une autorisation de publier ni de changer les règles, l'économie ou le protocole réseau.


## Validation du cockpit et suite motion

- Arthur a retenu Pulse avec la palette originale sombre / néon vert / ambre, « Dark Luxury Néon ».
- Dernière version du cockpit explicitement validée : `pulse-inspecteur-visuel.html` dans le dossier de visualisations du chat. Specs hiérarchisées dans un bloc unifié, loyer prioritaire, rendement secondaire ; inspecteur par cartes sélectionnables et portefeuille par familles. Deux crans maintenus.
- Préférence de communication : réponses courtes, décisions concrètes, pas de longs développements. Toute nouvelle idée de design doit garder sa preview.
- Propositions complémentaires en cours de validation : achat glissé (annulable, alternative clavier/clic), complétion de famille, loyer crédité au capital. Preview `pulse-moments-cles.html`. Ne pas traiter ces ajouts comme validés à ce stade.
- Demande explicite de livraison : une fois les deux chantiers validés (dés et UI/UX), préparer un package commun avec références, paramètres, assets et instructions autonomes pour GPT-6 / Codex Builder qui possède le projet. N'envoyer aucun message à un autre chat sans autorisation explicite.
