# Prompt autonome — GPT-6 / Codex Builder

Tu es le builder du projet PolyRisk. Arthur a validé deux chantiers : le lancer de dés et l'UI Pulse Dark Luxury Néon avec son cockpit de propriété. Implémente ces deux chantiers dans le projet existant, en conservant les règles, l'économie et le protocole. Ce package est la référence de direction ; les HTML servent d'aperçus, pas de runtime à embarquer.

## Lire et comparer avant de coder

1. Lis `AGENTS.md`, la mémoire `Docs/Design/MOTION_LAB_MEMORY.md` et les skills locaux applicables (`poly-risk-motion`, `ui-uitk`). Respecte le travail non commité.
2. Lis `01_DICE_APPROVED.md`, `02_PULSE_APPROVED.md`, `04_ACCEPTANCE.md` et `design-motion-tokens.json` dans ce dossier.
3. Ouvre et manipule `previews/dice-approved.html` et `previews/pulse-approved.html`. Lis les keyframes et interactions correspondantes dans `sources/`. Si tu ne peux pas ouvrir un aperçu, exploite le source et signale cette limite : ne prétends pas l'avoir vu.
4. Inspecte le code actuel avant de choisir les composants à modifier. Le repo est Unity 2022.3.62f3 / UI Toolkit, pas une application web. Ne copie pas le CSS en USS, ne crée pas une WebView et ne remplace pas le moteur.

## Résultat demandé

**Dés :** mouvement simple, deux dés côte à côte, premier stabilisé avant le second, bascule sur l'arête puis « tac ». Grand total derrière les dés, jamais « 5 + 5 ». Double avec ponctuation verte / DOUBLE / son bref. Rebond vertical comme variante par défaut recommandée. Aucun croisement ni rotation complète. Les résultats viennent exclusivement du jeu confirmé.

**UI :** structure Pulse, palette originale sombre / vert néon / ambre ; Top HUD lisible et statut joueurs correctement intégré. Bottom widget à deux crans. Dans un seul bloc : loyer par visite prioritaire, rendement secondaire, risque absent honnêtement signalé, puis synthèse GO/coûts/net estimé. Inspecteur par cartes de propriétés sélectionnables, portefeuille par cartes de familles et progression possédées/total. Le contexte de case est conservé à l'ouverture. Les actions restent accessibles ; aucune commande locale interdite pendant le tour adverse.

Les tableaux de chiffres, noms, 2/3, revenus GO et niveaux des previews sont illustratifs. Remplace-les par les données autoritatives, sans inventer de taux ni coder « 3 propriétés » en dur. Les captures incorporées ne remplacent jamais le vrai plateau, sa caméra ou ses pions. Conserve le contenu et les fonctions du HUD existant non représentés dans la maquette.

## Ordre de travail

- Fais un bref état des points d'intégration et de tout écart technique réellement bloquant, puis poursuis sans demander à reconfirmer les choix déjà validés.
- Termine une tranche cohérente : dés, puis coque Pulse / cockpit et liaison aux données, puis synchronisation entre les deux.
- Présente le résultat dans le vrai jeu avec captures ou courte vidéo et compare avec les aperçus. Corrige les écarts de hiérarchie, densité, lisibilité ou timing avant livraison.
- Exécute les vérifications pertinentes de `04_ACCEPTANCE.md`, compile avec l'éditeur disponible et teste un vrai build Release WebGL si l'environnement le permet. Signale clairement les contrôles indisponibles.

## Limites de mandat

`03_OPTIONAL_MOMENTS.md` et `previews/key-moments-optional.html` contiennent des recommandations **non validées** : slider d'achat, complétion de famille, loyer crédité. Ne les active pas et ne remplace pas le bouton d'achat approuvé automatiquement. Intègre d'abord la base validée ; ces idées font l'objet d'une prochaine décision d'Arthur.

Aucun changement de règles, nouveau backend, nouvelle dépendance automatique, publication, déploiement, merge ou message à un autre chat n'est demandé. L'animation ne doit ni décider du résultat ni bloquer l'état autoritatif.

Livre les changements ciblés, l'aperçu réel, les tests exécutés, les limites et les paramètres ajustables. Les preuves de ce package valident les maquettes HTML ; elles ne valent pas validation d'une future intégration Unity.
