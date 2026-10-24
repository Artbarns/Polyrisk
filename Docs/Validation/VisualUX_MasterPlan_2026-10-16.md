# PolyRisk — audit UI/UX et plan de feedback visuel

Date : 16 octobre 2026  
Périmètre : UI/UX, motion et lisibilité du jeu. Les règles, probabilités, économie et résultats autoritaires du moteur restent inchangés.

## Audit factuel

Le projet utilise Unity **2022.3.62f3** et UI Toolkit runtime pour les panneaux principaux. Les contrôles et données sont en partie construits en C# et stylés par UXML/USS. Le dock d’état et l’historique des douze activités existent déjà (`ActionButtonsWidget`, validation `GameStateDock_2026-10-16.md`), de même que le feed du HUD (`TopHUDWidget`). Le feed s’appuie encore sur `GameActivityEntry.kind/text`; ces textes sont répliqués dans `LobbySnapshot.activities`. Le format actuel ne transporte ni cible, ni montant autoritaire structuré, ni ventilation de bénéficiaires. L’interface reformate encore certains textes.

Éléments visuels déjà en place : achat coulissant, distinction CTA primaire/secondaire, transitions UI Toolkit et option de mouvement réduit, compteurs interpolés, dés affichés après le lancer dans le feed, déplacement case par case avec rebond, badges de rendement projetés sur les tuiles, fiche de tuile, panneaux Marketplace/Venture, timer d’enchère. Ces fondations sont à réutiliser; aucun tween externe n’est installé dans `Packages/manifest.json`.

Écarts confirmés dans le code ou signalés sur les captures fournies :

- **Événements :** `GameEvents` expose surtout des callbacks non typés et `PublishActivity(kind, playerId, text)`. `TransactionManager` applique les soldes, mais ne publie pas un reçu structuré avec payeur, bénéficiaire, motif et montant. Les loyers détournés par sabotage sont exécutés en deux transferts distincts; le message d’activité n’en donne pas la ventilation. Le feed temporaire et le Dock peuvent donc omettre la cible/le reçu complet.
- **Actions en attente :** `ActionButtonsWidget` utilise encore le caractère « … » pour plusieurs états d’attente, notamment déplacement, états de décision et transitions. Le bouton ne précise pas toujours acteur, action attendue ou condition d’attente.
- **Joueurs :** le HUD principal montre les métriques du siège local; il n’expose pas encore un roster permanent des 2–6 joueurs avec capital, couleur/initiale et tour actif. Les noms de sièges existent ailleurs (lobby/Marketplace) mais pas comme repère de partie stable.
- **Plateau :** `BoardTileOverlayWidget` crée un chip de rendement par propriété; sur les captures, la répétition de « 10 % » réduit la hiérarchie et les glyphes sont trop petits. Le cadrage portrait rogné, les pions cylindriques/chevauchés et les zones mortes sont des constats des captures; ils doivent être mesurés sur un appareil avant de toucher à la caméra ou au layout 3D.
- **Venture :** la carte actuelle et le bouton sous-jacent doivent être contrôlés sur build runtime opaque. L’inventaire de cartes objets (ex. sortie d’audit) n’a pas d’emplacement HUD identifié dans le code UI audité.
- **Libellés :** UI principalement anglaise, cartes françaises; `ActionButtonsWidget` affiche `N/A` pour le risque de tuile quand la donnée n’existe pas. Les flèches Rent/ROI et le format `$1,3K` doivent être remplacés par des libellés explicites et un formateur monétaire unique, sans changer les valeurs.
- **Safari / mémoire :** la capture iPhone indique les barres Safari et nécessite un essai installé sur l’écran d’accueil. La mémoire réelle, le seuil de rechargement Safari, l’impact ASTC et le paramètre de mémoire initiale ne sont pas mesurés ici. Toute limite chiffrée provenant du brief reste **À VÉRIFIER** sur iPhone 14 Pro Max avec Web Inspector.
- **Visuel 3D :** netteté/cadrage, mémoire des textures, caméra orthographique de dés et éclairage doivent être mesurés séparément du texte UI. L’existence et l’activation d’une option `navigator.vibrate` dans Safari iOS ne sont pas confirmées; aucun haptique WebGL ne sera requis.

Les captures/mockup décrites dans la note n’étaient pas présentes comme images dans le dépôt. Les défauts ci-dessus qui en proviennent sont donc conservés comme observations utilisateur et seront vérifiés à la recette; aucun jugement pixel à pixel n’est affirmé.

## Catalogue d’événements et chorégraphies

Les durées sont des budgets visuels, pas des délais de règle. Les séquences ne suspendent jamais une entrée de jeu, sauf si le moteur attend déjà un choix. Le résultat affiché vient exclusivement du moteur.

| Événement / priorité | Annonce → anticipation → impact → transfert → trace | Son / variante autre joueur | Budget / coût / risque |
|---|---|---|---|
| Tour actif / normal | Pastille active → anneau de siège → CTA verbalisé → trace « à toi » | Signal discret; pour autrui, avatar + « joue » en mode rapide | 0,4 s; coût faible; risque de densifier le haut |
| Dés / majeur | « Tu lances » → deux dés UI animés vers les faces déjà décidées → total pop → compteur de cases → arrivée ciblée → historique | Deux ticks + impact; autres joueurs x0,6, sans input bloqué | ≤1,1 s local / ≤0,6 s autre; coût faible si UI 2D, moyen avec RenderTexture; risque de désynchroniser résultat si source mal branchée |
| Déplacement / normal | total décrémenté → pion suit le trajet existant → case cible pulse à l’arrivée | Son discret à l’arrivée; mode rapide pour autrui | 0,3–0,6 s de feedback ajouté, mouvement existant séparé; faible coût; risque de cadrage/chevauchement |
| Arrivée / majeur | nom de case au centre de scène → accent de tuile → dock résume la décision → bottom sheet CTA | Impact court; variante autrui sans détail long | 0,5 s; coût faible; risque de collision avec fiche/dock |
| Achat / majeur | coût et prix restent visibles → validation slide existante → marqueur propriétaire/claim → capital interpolé → reçu signé | Click/claim; autres joueurs: annonce siège + propriété + montant confirmé | ≤2,2 s; faible/moyen; risque: feedback avant confirmation serveur |
| Refus / passage / normal | état « tu passes » → retour calme → transition vers l’étape autoritaire suivante → trace | Tick léger; autres: mise à jour roster/feed | 0,5 s; faible; risque de présenter « refusé » alors que requête en attente |
| Loyer / transfert / majeur | payeur→bénéficiaire(s) visible(s) → montant signé → pastilles de sièges pulsent → compteurs roulent → reçu | Son de transfert; autre joueur garde couleur/sens/montant | ≤2,2 s; faible en UI; risque élevé tant que `GameActivityEntry` ne porte pas le transfert confirmé et correlationId |
| Taxe, GO, revenu, vente / normal | motif + somme → delta au compteur concerné → trace | Son distinct gain/perte; autres en x0,6 | ≤1 s; faible; risque moyen de coalescer des paiements sans masquer leur cause |
| Pouvoir/famille/ciblage / majeur | acteur et cible éligible → sélection → confirmation → effet/case/roster change → historique | Accent de couleur siège; autres voient « acteur cible cible » | ≤2,2 s; faible; risque de confondre ciblage et effet résolu |
| Venture / majeur | carte opaque au centre → flip → polarité/icône → effet et inventaire → trace | Son court; pour les autres une seule annonce résumée | ≤2,2 s; faible/moyen; risque de cacher un CTA ou de révéler l’effet avant confirmation |
| Enchère/OPA/Marketplace / majeur | initiateur/cible/prix → timer → offres avatar + montant → résultat claim → historique | Son timer seulement en fin; spectateurs en mode rapide | ≤2,2 s hors attente métier; moyen; risque de bruit et surcharge réseau visuelle |
| Attente réseau / normal | avatar + statut exact (connexion, choix d’un siège, réponse serveur) + progression discrète → confirmation/reprise → historique | Pas de boucle sonore; « envoyé » distinct de « confirmé » | persiste tant que requête; coût nul/faible; risque de fabriquer un état non autoritaire |
| Insolvabilité/faillite/victoire / critique | cause, débiteur/créancier, montant confirmé → choix requis ou bilan → issue à fort contraste → historique/récap | Son réservé; autres voient résumé partageable | ≤2,2 s par séquence; faible/moyen; risque élevé si le UI anticipe le règlement asynchrone |

Décision de présentation pour les transferts : **paiement automatique animé après confirmation du moteur**, sans confirmation UI supplémentaire. L’écran d’insolvabilité reste le seul chemin de choix déjà défini par le jeu; l’animation ne peut ni retarder ni annuler le règlement. Pour les loyers répartis, un reçu regroupe les bénéficiaires sous le même `correlationId` et un chip/flux par bénéficiaire.

## Proposition d’écran en trois couches

1. **Chiffres en haut :** conserver Capital/Net worth/Tour/Marché; ajouter une rangée compacte 2–6 sièges avec couleur, initiale, cash autorisé par le modèle, indicateur actif et delta signé. Une seule micro-jauge marché et changement d’état annoncé une fois. Le roster se replie en portrait très court; pas de nouvelles cartes fixes.
2. **Scène centrale :** utiliser l’espace central pour une seule séquence majeure (dés, carte, reçu ou annonce), avec fond sombre opaque/scrim local; elle se ferme/est skippable au tap et laisse le plateau visible. Aucune pile de toasts en bas. Commencer avec UI 2D native; une caméra/RenderTexture pour dés reste un prototype seulement si la 2D ne satisfait pas le mouvement, car elle ajoute mémoire et coût de composition.
3. **Dock bas :** conserver un seul bloc fusionnant statut, fiche de tuile et actions déjà existants. Remplacer « … » par avatar/indicateur/texte de l’attente. Historique repliable avec acteur→cible, montant signé, raison localisée. Pas de nouvel espace permanent sous le plateau.

## Plan de réalisation, effort, risque et coût performance

| Étape | Livrable | Effort | Risque produit/technique | Budget perf / recette |
|---|---|---:|---|---|
| 0. Audit et catalogue | Ce document, inventaire panneau/événement et décisions UX | 1 j | Faible; références visuelles manquantes | Nul; inspection code/captures |
| 1. Événements typés + Presenter + historique | Payload immuable, IDs/corrélation, priorités, coalescence, vitesse/skippable, reçu moteur confirmé; adaptateur historique conservateur | 2–4 j | Élevé: paiement asynchrone, activités répliquées; schéma réseau additif doit rester compatible | Faible allocations amorties; tests Editor + 2 clients; aucun nouveau Canvas |
| 2. Roster + Dock | Roster responsive, état d’attente explicite, montants autoritaires, trace compactée | 1–2 j | Moyen: privacy/cash status et états réseau | Un petit subtree UITK; vérifier rebuilds et portrait court |
| 3. Dés → déplacement → arrivée | Animation UI 2D déterministe, faces autoritaires, total/compte de cases, intégration mouvement existant | 1–3 j | Moyen: doubles, turbo/precision/audit et clients distants | UI 2D privilégiée; si caméra dédiée: mémoire RenderTexture à mesurer; profiler |
| 4. Achats et transferts | Claim, cash delta, reçu avec payee/splits, gains/pertes | 2–3 j | Élevé jusqu’à émission moteur/serveur confirmée | Faible; pas de particules permanentes |
| 5. Venture + inventaire | Carte centrale opaque, reveal/effet et emplacement d’objet | 1–2 j | Moyen: effets d’objets existants/état réseau | Sprite/UI existants; atlas à mesurer |
| 6. Enchères/OPA/Marketplace | Timer, offres, acteurs/cibles, résultat et waiting copy | 2–3 j | Moyen/élevé: flux multi-client et timeout | Réutilisation des panneaux; éviter liste animée entière |
| 7. Plateau/pions/cadrage | Lentilles Rendement/Propriétaire/Risque, badges actifs, sièges empilés, portrait et safe area | 2–4 j | Élevé: modification caméra/surface 3D et capture de référence absente | Aucun post-process; cible: aucune régression mesurée |
| 8. Localisation, son, accessibilité, fin de partie | FR par défaut, format monétaire unique, audio déverrouillé au premier geste, partager si supporté, mouvement réduit | 2–3 j | Moyen; partage Web/iOS à vérifier | Son léger; appareil réel nécessaire |
| 9. Recette de clôture | Parcours déterministe de tous les états, 2/6 sièges, iPhone Safari + standalone, build WebGL | 2–4 j | Haut tant que recette physique non faite | Web Inspector mémoire/FPS, netteté, payload, touch; aucun benchmark simulé présenté comme appareil |

Les lots se regroupent par grammaire de feedback plutôt que par écran indépendant; cela réutilise la file d’événements et évite d’ajouter un contrôleur/tween différent à chaque panneau. La recette de chaque état reste obligatoire.

## Avancement de ce lot

- **Étape 0 terminée.** Audit et catalogue ci-dessus; décisions de présentation et coûts/réserves consignés.
- **Socle typé et transport en ligne terminés pour le dé.** `GameEvent` immuable, corrélation, bénéficiaires ventilés, `GameEventPresenter` non visuel (priorité, historique 50 entrées, beats, skip, vitesse rapide, mouvement réduit), producteurs dés/achat/transferts, transport additif dans les snapshots et déduplication par séquence. Le jet confirmé est consommé côté client distant par la carte centrale.
- **Intégration UI générale encore ouverte.** La carte de dés masque temporairement le bottom sheet complet puis restaure son état; le reste de l’UI utilise encore `GameActivityEntry`. La jonction visuelle du Dock d’état et de l’inspecteur reste trop dense, et aucun roster responsive n’est encore raccordé au Presenter. Ce travail doit réutiliser la file existante plutôt que créer un second système d’animation.
- Régression batchmode la plus récente : économie **114 PASS**, mécaniques **141 PASS**, événements **12 PASS**, UI **252 PASS sur 17 écrans/composants** (**519 assertions**). Build WebGL Release isolé PASS; recette réseau locale à deux clients Chromium : événements typés répliqués, dés 2+6 reçus, 0 erreur console. Aucune vérification iPhone physique ni publication de l’aperçu partagé.
- Le dépôt contenait déjà de nombreux changements non commités avant ce lot. Aucun commit isolé n’a été créé pour ne pas embarquer ces changements; les fichiers de cette étape restent séparables et listés dans le diff.
- **Passe de dés et réception distante vérifiées.** Rapport/captures actuels : `Docs/Validation/DiceShowcase_Network_2026-10-17.md` et son dossier. Pendant l’animation, les valeurs sont celles du `GameEvent` autoritaire; les transitions restent décoratives, SKIP et mouvement réduit vont au résultat. La recette deux clients prouve le transport, mais pas Safari/iPhone, le mouvement case par case synchronisé au beat, le traitement différencié des doubles, ni l’UI générale des achats/transferts.

## Références et décisions techniques

- Unity du projet : **2022.3.62f3**. Une version ultérieure **2022.3.67f2** a des notes officielles et des artefacts WebGL/iOS disponibles; évaluer ce saut sur une branche d’essai avec builds comparatifs avant de changer l’éditeur du projet. Ne pas migrer dans le présent lot UI.
- Le dépôt du dice roller évoqué dans le brief est [`adammyhre/Unity-Baldurs-Gate-Dice-Roller`](https://github.com/adammyhre/Unity-Baldurs-Gate-Dice-Roller) (le propriétaire n’est pas `unitycoder`). Son README recommande une seconde caméra orthographique et un layer dédié, mais l’exemple utilise Rigidbody. Seule l’idée de présentation UI peut être prototypée; les faces et résultats ne doivent jamais venir de la physique.
- Le package App UI actuel en amont indique un minimum Unity `6000.3`; les versions antérieures étaient plus permissives, mais une Toast App UI n’est pas requise ici. La file de priorité propriétaire évite une dépendance et son support WebGL/iOS doit être mesuré si le package est reconsidéré.
- PrimeTween n’est pas présent et n’est pas retenu en fondation; animations courtes avec UI Toolkit/USS et code existants sont moins risquées. Réévaluer uniquement si un prototype natif manque un besoin mesuré.
- MDN classe l’API Vibration comme disponibilité limitée; l’absence de prise en charge Safari iOS doit être traitée comme l’hypothèse de conception, sans faire dépendre le jeu des haptiques. Les claims de seuil mémoire Safari, d’allocation initiale optimale et de bénéfice ASTC ne sont pas mesurés dans ce projet. Les essais mémoire et build séparé exigent l’appareil réel et une mesure.

Sources : [release Unity 2022.3.67f2](https://unity.com/releases/editor/whats-new/2022.3.67f2), [package App UI en amont](https://github.com/Unity-Technologies/unity-app-ui), [README dice roller](https://github.com/adammyhre/Unity-Baldurs-Gate-Dice-Roller), [MDN — Vibration API](https://developer.mozilla.org/en-US/docs/Web/API/Vibration_API).

## Acceptation

- En cinq secondes, le joueur identifie le siège actif, l’action requise, l’événement récent et sa conséquence.
- Une action d’un autre joueur reste intelligible sans lire une phrase longue; acteur/cible/somme/motif ont chacun un repère non basé uniquement sur la couleur.
- Les succès visuels ne précèdent jamais l’état confirmé du moteur/serveur; chaque événement majeur est skippable et le mouvement réduit raccourcit la séquence sans perdre le résultat.
- Aucune animation ne recalcule les règles, bloque le tour ou détourne un paiement. Aucune régression de mémoire, cadence, chargement ou interaction tactile n’est acceptée.
- Revue visuelle portrait/paysage, 2–6 clients et iPhone 14 Pro Max réel/standalone; limites d’essai desktop clairement signalées.
