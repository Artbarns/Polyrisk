# Dock d’état — reçus d’achat et de transfert typés — 17 octobre 2026

## Réalisation

- Le Dock conserve la file priorisée de `GameEventPresenter` et n’anime que les événements utiles à cette surface. Les jets de dés restent affichés par leur carte dédiée, sans rejouer la même séquence dans le Dock.
- Les achats de propriété et transferts confirmés sont présentés avec l’acteur, le montant réellement appliqué, les bénéficiaires et leurs parts, le motif et la tuile. Passe, taxe réglée et amélioration appliquée utilisent aussi les événements typés avec acteur, montant ou niveaux et propriété quand disponibles. Une activité historique correspondante est rapprochée du reçu pour éviter une ligne en double.
- La présentation est skippable; le choix « Réduire les animations » continue de raccourcir les beats du Presenter. Le journal conserve les dernières activités et les reçus pris en charge.
- Aucun changement de règle, d’économie, de donnée de partie ou de protocole n’a été fait dans cette tranche.

## Vérifications

- Unity 2022.3.62f3, `RunAllEditorRegressions` : économie **114**, mécaniques **141**, événements **14**, UI **262** — **531 assertions PASS**, processus terminé code 0.
- Unity Release WebGL (`BuildOptions.None`), scènes de production : `WEBGL PLAYER BUILD PASS`, code 0. Artefact de recette isolé `/tmp/PolyRiskTypedDockEvents_20261017`, environ **12 MiB**. Journal `/tmp/PolyRiskTypedDockEventsBuild_20261017.log`.
- Le formatter est testé avec achat, loyer réparti entre deux bénéficiaires, passe, taxe et amélioration. Le Presenter fusionne ces transferts dans un seul reçu animé même quand le premier est déjà en cours de présentation.
- Navigateur local, WebGL Release, viewports observés **414 × 608** et **1280 × 720** sur les versions de cette passe : démarrage d’une partie locale à quatre sièges, jet, arrivée sur propriété, achat local puis tours IA. Dans le build final, après PASS sur Opioid Paradise, le Dock affiche `YOU passed on Opioid Paradise`; l’historique conserve l’action. La précédente Release a aussi montré le reçu d’achat `Player 2 acquired French Riviera · −$1K` avec SKIP. La build finale est servie à `http://127.0.0.1:8782/`.
- `git diff --check` : PASS avant le lancement Unity; une dernière vérification du diff est à refaire après les mises à jour de ce rapport.

## Couverture restante

Les reçus typed de taxe et d’amélioration passent par le formatter mais n’ont pas encore été déclenchés à l’écran. L’OPA, pouvoirs, enchères, Venture et faillite restent sur les activités historiques jusqu’à émission d’un reçu confirmé adapté. Le loyer réparti est couvert par un test de formatage et de fusion du Presenter; son rendu réseau runtime n’a pas été exercé. La recette de ce Dock en multijoueur à 2–6 clients, sur plusieurs tailles portrait/paysage, Safari/iPhone physique, mouvement réduit et profilage WebGL/mobile reste à faire. Cette préversion locale ne remplace ni ne publie le lien partagé.
