# Final Experience — audit et périmètre

Référence artistique et technique : PASS2F.1 intégrée dans la copie de travail, scène SampleScene, plateau PolyRiskBoard, quatre meshes fusionnés, 40 ancres/colliders indexés. Caméra orthographique : BoardInteractionManager est le seul writer. Mouvement : PlayerVisualManager → PawnMovement → CompactPawnVisual ; contact indépendant BoardContactFeedback. Aucun changement économique, résultat de dés, callback ou protocole requis pour le rapprochement.

La référence de taille est le calcul stratégique PASS2F.1 inchangé, appliqué à son enveloppe plateau/pions et à la réservation du cockpit compact. Mesure du précédent binaire : **18,2081718445 à430×830** ; **11,2809009552 à1200×900**. Cible : **13,005837** / **8,057786**, soit référence÷1,40. Le repos utilise le pion actif, une anticipation courte et des bornes d'exploration. Le zoom additionnel de mouvement passe de1,14 à1,00 ; hold250ms conservé. Le cockpit n'entre jamais dans la clé de recalibration. Les40cases n'ont plus à tenir simultanément dans le viewport.

Exploration : seuil10pixels UI, tap au relâchement, drag borné sans sélection, touch prioritaire sur souris émulée. Un snapshot de même joueur/même position laisse l'exploration intacte. Reprise au prochain mouvement/tour/position pertinente. Les droits d'inspection existants sont conservés.

WebGL : Unity2022.3.62f3/URP14.0.12, Low par défaut, renderScale1, SMAAHigh caméra, MSAAoff, HDR+PP existants. Pas de réglages Unity6 appliqués. DPR plafonné2 avec budget initial2,4Mpixels, reconnaissance iPadOS et gestescanvas dédiés. Voir mobile-render-audit.md. Aucun appareil iPhone/simulateur disponible ; SafariMac refuse l'automatisation distante (préférence laissée inchangée).

Multijoueur existant : ASP.NETCore net10, commandes HTTP JSON authentifiées, Unityhost autorité des règles, snapshots versionnés, persistenceJSON atomique, sixplaces, reconnexion/transfert d'hôte. Aucun WebSocket, donc pas deWSS à ajouter. Livraison possible sur une seule origine HTTPS avec volume persistant et une seule réplica. Le bootstrap de fichiersstatiques ne modifie pas les routes de jeu ni l'authentification.

Infrastructure découverte : aucune remoteGit, aucun workflowCI, aucun hébergement autonome configuré. Les URL anciennes étaient des QuickTunnels dépendants duMac. Vercel n'a retourné aucun projet/équipe disponible. Railway/GitHub ont été proposés ; accès utilisateur requis pour hébergement autonome. Aucun token ne doit être collé au rapport ni ajouté au bundle. Pas d'URL publique inventée.
