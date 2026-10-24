# PolyRisk — intégration du lancer de dés

## Mission

Implémente dans PolyRisk le lancer décrit ci-dessous en prenant `preview.html`, dans ce même dossier, comme référence visuelle et temporelle. Le prototype montre trois variantes ; commence par **Rebond vertical** comme MVP recommandé, garde les deux autres en variantes de présentation configurables si cela reste simple. Le choix définitif entre les trois variantes n'a pas été explicitement arrêté par Arthur.

Réalise l'intégration ciblée, puis montre un aperçu du résultat réel et rapporte les vérifications. Ne te limite pas à proposer un plan. Préserve le travail en cours et les règles du jeu.

## Direction créative validée

- Deux dés lisibles côte à côte, avec points et un léger volume ; identité sombre et menthe du prototype, adaptée à la charte existante.
- Mouvement simple : rebond vertical, petit lancer vers l'avant ou glissade courte. Aucun croisement, mélange, trajectoire circulaire ou rotation complète. Petites inclinaisons seulement.
- Le premier dé se stabilise ; le second reste brièvement sur son arête / coin d'appui, hésite légèrement, puis bascule d'un **tac** sec.
- L'appui doit rester crédible : le dé pivote autour du point au sol ; une simple rotation autour de son centre ne reproduit pas le mouvement validé.
- La somme apparaît **en très grand derrière les dés**, uniquement après le contact final. Elle reste visible ; les dés restent opaques et au premier plan.
- Aucun texte de calcul du type « 5 + 5 ». Sous le résultat, un discret « PAS » suffit.
- Résultat ordinaire : grand total gris neutre. Double : grand total vert et **DOUBLE**, avec la signature décrite ci-dessous.
- Le plaisir du double évoque la ponctuation « green » citée par Arthur : arrêt bref, détente nette et confirmation sonore. Il ne s'agit pas d'ajouter une mécanique d'adresse ou une jauge qui modifierait les probabilités.
- Le maintien du bouton est une mise en tension facultative. Le clic / tap rapide et le clavier doivent fonctionner. La durée du maintien ne modifie jamais le résultat.

## Ce qui est référence, ce qui est démonstration

`preview.html` est un prototype de motion, pas une implémentation Unity à embarquer. Il utilise du CSS 3D, la Web Animations API et des sons Web Audio synthétisés. Ne crée pas de WebView et ne transpose pas le CSS directement en USS.

Le bouton « Voir un double », la navigation entre variantes et les mentions « MOTION LAB » sont des outils de démonstration ; ne les ajoute pas à l'interface de jeu. Les valeurs aléatoires du prototype sont uniquement des exemples : la production consomme les résultats confirmés de PolyRisk.

Les petits changements cosmétiques de faces du prototype ne constituent pas une simulation physique. En production, si plusieurs faces sont visibles, respecte des dés cohérents, notamment les faces opposées dont la somme vaut 7. La face finale doit être sans ambiguïté.

## Inspecter avant de modifier

Lis `AGENTS.md`, `.agents/skills/poly-risk-motion/SKILL.md` et `.agents/skills/ui-uitk/SKILL.md`. Vérifie les API dans l'éditeur installé : le projet utilise Unity 2022.3.62f3 et UI Toolkit runtime.

Points d'entrée existants, à réinspecter car le projet évolue :

- `Assets/Scripts/UI/TopHUDWidget.cs` : `MountDiceRollStage`, `OnGameEventPublished`, `SetDiceRollResult`, skip, masquage et restauration des overlays / dock ; séquencement et mode réduit existants.
- `Assets/UI/Styles/Theme.uss` : `.dice-roll-*`, palettes et responsive.
- `Assets/Scripts/UI/ActionButtonsWidget.cs` : commande de lancer et `SetTransientDicePresentation`.
- `Assets/Scripts/UI/UIUXRuntime.cs` : préférence `ReducedMotion`.
- `Assets/Scripts/UI/UIToolkitTransitions.cs` : helpers à réutiliser si adaptés.
- `Assets/Scripts/Systems/DiceSystem.cs` : génération et confirmation des valeurs ; ne change pas les probabilités.
- `Assets/Scripts/Systems/TurnManager.cs` : ordonnancement réel, doubles, Audit, Turbo et Précision. Une attente de 1 seconde a été observée dans le flux normal : vérifier sa fonction, sans la supprimer aveuglément.
- `Assets/Scripts/Multiplayer/OnlineGameController.cs` : événements / snapshots et prévention d'une répétition du même lancer à la reconnexion.
- `Assets/Scripts/Systems/GameEventPresenter.cs` et `Assets/Scripts/Core/GameEvents.cs` : inspecter l'articulation entre présentation, file d'événements et mouvement.

Préserve les changements non commités. Aucune réécriture globale de l'interface, du moteur ou du réseau.

## Séquence du MVP : Rebond vertical

Les unités ci-dessous correspondent au prototype de 72 × 72 px par dé. En Unity, convertir selon l'échelle du panneau et le viewport. Les timings sont des points de départ ajustables.

`t = 0` désigne la disponibilité du résultat confirmé, après la commande et une éventuelle attente réseau. Avant cela, donner un feedback immédiat de l'appui et un état d'attente sobre, sans annoncer de faux résultat.

| Phase | Durée / moment | Comportement |
|---|---|---|
| Appui | 50–70 ms | Bouton comprimé vers 0,96 ; commande acceptée une seule fois. |
| Premier dé | 0–520 ms | Montée d'environ 64 unités, chute, compression de contact d'environ 3–4 %, petit rebond de 17 unités, stabilisation. Inclinaison maximale d'environ 5°. |
| Second dé | 0–640 ms | Montée d'environ 73 unités, chute, petit rebond de 11 unités ; arrivée sur l'arête inclinée vers 38°. |
| Suspension | 640–840 ms | Pivot au point d'appui : 38° → 35° → 37°, amorti, durée par défaut 200 ms, ajustable entre 80 et 400 ms. |
| Bascule | 840–935 ms | 37° → 0°, chute accélérée ; courbe de référence Bézier (0.55, 0, 0.95, 0.55). |
| Contact final | ≈ 935 ms | « Tac » aligné avec la pose au sol ; révélation de la somme. |
| Stabilisation ordinaire | 125 ms | Dé 2 : échelle (1.04, 0.96) → 1 ; petite reprise angulaire de −3° puis 0°. |

Courbes : montée qui ralentit, chute qui accélère, rebonds décroissants ; pas de vitesse uniforme qui ferait flotter les dés. Pour les keyframes exactes des trois variantes, consulter les objets `frames` et les appels `animate` dans `reference.fragment.html`.

Petit lancer : déplacement vertical d'environ −48 / −56 unités avec perspective 1 → 0,85 → 1, léger dépassement à l'atterrissage. Glissade : translation de −28 → 0 unités avec rebond court de 16 / 20 unités. Chaque dé conserve sa voie et son ordre gauche/droite.

## Signature du double

Au contact final du second dé :

1. Son « tac » normal.
2. **65 ms de compression locale** de la paire à 0,97, sans arrêter le moteur, le réseau ou le déplacement du pion via `Time.timeScale`.
3. Révélation du total vert derrière les dés et du mot **DOUBLE**.
4. Anneau vert bref, échelle 0,76 → 0,94 → 1,45, opacité 0 → 0,9 → 0 sur 500 ms.
5. Quelques traits radiaux courts pendant 360 ms et un accent de trois barres au-dessus, visible brièvement puis disparu vers 850 ms. Aucun effet continu.
6. Petite détente commune : échelle 0,97 → 1,055 → 1, déplacement vertical maximal d'environ 6 unités sur 280 ms.
7. Texte DOUBLE : entrée sur 240 ms, décalage vertical 9 → −2 → 0 et échelle 0,75 → 1,1 → 1.
8. Accord synthétique ascendant bref ; conserver le réglage audio global et éviter la saturation / les superpositions.

Couleurs indicatives du prototype : fond `#0e1117`, menthe `#a1e2cf`, total neutre `#d4d5db` à ~25 % d'opacité, double `#8aff82` à ~40 % pour le grand total. Ne sacrifie pas la lisibilité pour correspondre numériquement au prototype.

Attention au contexte réel : un double peut avoir une conséquence défavorable selon la règle des doubles consécutifs, ou une conséquence particulière en Audit. Conserve la signature verte pour un double favorable / ordinaire ; pour un double pénalisant, conserve sa reconnaissance visuelle mais indique la conséquence réelle sans célébration de réussite trompeuse. Suis les GDD et les règles existantes, jamais une règle inventée.

## Architecture recommandée

- Préserve UI Toolkit pour le HUD, le bouton, le total et les états. Utilise les primitives Unity et helpers existants.
- Pour les dés, privilégie une représentation légère compatible avec le projet : faces préparées / volume 2,5D ou deux meshes Unity simples si cela reproduit nettement mieux le point d'appui. Ne recrée pas un moteur physique ; les trajectoires sont contrôlées et le résultat final est déjà connu.
- Évalue les coûts d'une caméra / RenderTexture avant de la choisir. Ne les ajoute pas automatiquement.
- Un composant de présentation dédié si nécessaire ; expose durées, amplitudes, pivot, couleurs, intensité du double et volume via une configuration réutilisable. Les noms et nouveaux chemins sont à choisir après inspection.
- États clairs : disponible, pressé, attente réseau, rebond, suspension, bascule, résultat ordinaire, double, annulation / indisponible.
- Un identifiant de séquence / mécanisme d'annulation doit invalider callbacks, sons et effets précédents. Les 3 variantes sont un choix de présentation, pas trois systèmes indépendants en production.
- N'utilise pas le `UnityEngine.Random` de `DiceSystem` pour les variations cosmétiques : ne perturbe jamais l'état aléatoire gameplay.
- Réutilise les éléments et ressources ; pas de reconstruction de l'arbre visuel à chaque frame. Précharge les clips audio. Pas de nouvelle dépendance runtime sans bénéfice mesuré.

## Synchronisation et durée de lecture

Le prototype laisse le résultat visible pour comparaison. En production, conserve le résultat dans le HUD / historique et laisse le jeu poursuivre son flux normal. Inspecte l'ordonnancement pour éviter que le pion ou une fenêtre de choix soient masqués pendant la révélation. Ne fais pas dépendre l'état autoritatif d'une animation locale ou de la visibilité d'un client. Ne rallonge pas les délais gameplay / réseau pour faire durer l'effet.

Pour Turbo / Précision : distingue somme des faces et déplacement final ; n'étiquette pas la somme brute comme déplacement final si un choix peut encore la modifier. Affiche la valeur corrigée au bon moment, sans modifier les dés individuels ni la détection des doubles.

Conserve skip, un résultat final fiable et la restauration des panneaux / dock quelle que soit la sortie. Évite les rejouages cosmétiques sur snapshots inchangés ; une reconnexion doit reconstruire l'état présent, pas célébrer un ancien lancer.

## Audio fourni

Le dossier `audio/` contient trois WAV mono 48 kHz / PCM 16 bits créés pour ce handoff : `contact_light.wav`, `contact_tac.wav`, `double_green.wav`. Ce sont des synthèses originales, sans sample NBA 2K ni bibliothèque tierce. Utilise-les comme base ajustable, en les important à l'emplacement approprié dans les Assets après inspection de la gestion audio existante.

Le prototype synthétise son audio à l'exécution ; les WAV reprennent ses principes et son enveloppe, sans prétendre à une identité acoustique parfaite entre navigateurs et Unity.

## Accessibilité, mobile et performance

- `ReducedMotion` : valeurs finales et total directement lisibles, aucun rebond, pivot, zoom, rayon ou anneau en expansion ; double identifié par le texte et la couleur. Respecte le mute ; aucune information uniquement sonore.
- Souris, tactile et clavier, appui annulé, perte de capture et double clic ; une seule commande acceptée.
- Dés / total lisibles en portrait, paysage et petite hauteur ; ne masque pas les actions utiles.
- Vise 60 FPS quand réaliste ; mesure temps de frame et allocations dans un vrai build Release WebGL avant/après. N'annonce pas de validation iPhone / Android physique à partir d'une simple émulation desktop.

## Critères d'acceptation

- Vérifier les 36 couples de valeurs et les 6 doubles avec valeurs forcées uniquement dans un outil de test, jamais dans l'UI publiée.
- Aucun trajet circulaire, croisement ou rotation complète. La petite oscillation de l'arête reste volontaire.
- Premier dé stable avant le second ; point d'appui crédible, suspension perceptible et « tac » synchronisé.
- Total exact, grand et derrière les dés ; aucune addition affichée ; ordinaire neutre, double favorable vert + signature.
- Après plusieurs lancers : pas d'effet vert résiduel sur un résultat ordinaire, pas de son / callback en retard.
- Vérifier skip pendant rebond, suspension et double ; désactivation du widget ; préférence réduite ; mute ; réseau lent, rejet de commande, reconnexion, nouvel événement pendant une présentation.
- Vérifier mouvement du pion et choix Audit / Turbo / Précision / pénalité de doubles sans modification de règles.
- Vérifier import / compilation Unity, Console USS/UXML, et parcours réel dans un build WebGL avec captures ou vidéo.
- Rapporter les fichiers modifiés, les arbitrages de rendu, les contrôles réellement exécutés et les limites restantes. Produire un aperçu de l'intégration pour Arthur.
