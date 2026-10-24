# Roster du lobby — 16 octobre 2026

## Correction

Le `ScrollView` initial n’affichait pas ses lignes. Le roster a été remplacé par un `Label` alimenté à chaque snapshot. La capture à un siège montrait aussi les commandes basses en paysage; le style compact s’applique donc maintenant dès le premier joueur en paysage. Les espaces dans un nom sont insécables pour éviter de couper « QA Player 4 » entre deux lignes. Aucun changement de règle, d’économie ou de protocole.

## Validation Release

- Unity 2022.3.62f3, WebGL Release `BuildOptions.None` : `Build Finished, Result: Success`, processus code 0.
- EconomyRegression **114/114** et MechanicsRegression **140/140**, PASS.
- Depuis l’interface WebGL Release, l’hôte crée un lobby; cinq autres sièges le rejoignent par l’API isolée et le snapshot live de l’hôte affiche **6/6 joueurs**.
- Captures visuelles du même snapshot à **992 × 720**, **900 × 430** et **430 × 932** : les six noms/états, Ready, Start, Leave et Reduce Animations restent visibles. Les noms composés ne sont plus coupés au milieu du nom. À cette largeur, les textes privés redondants sont masqués en paysage pour préserver l’espace.
- Tailles des fichiers compressés : data 4 898 161 octets, WASM 7 681 625, framework 81 218. Total de ces trois fichiers : **12 661 004 octets**; loader inclus : **12 681 684 octets**.
- `git diff --check` : PASS.

Captures : [992 × 720](LobbyRoster_2026-10-16/Six_992x720.png), [900 × 430](LobbyRoster_2026-10-16/Six_900x430.png), [430 × 932](LobbyRoster_2026-10-16/Six_430x932.png).

## Limites restantes

Il s’agit d’un seul client WebGL qui reçoit le snapshot de six joueurs; cinq sièges ont été ajoutés au serveur QA par API. Cela valide le rendu et la mise à jour des six éléments, pas six navigateurs indépendants ni une partie humaine à six. Les seuils intermédiaires non capturés, Safari/iPhone physique, cibles tactiles, cadence/chauffe et comparaison avec le mockup restent à vérifier.

## Correctif du roster compact — Release frais

La recette courante à **900 × 430** a révélé que la chaîne du roster coupait le statut Ready du cinquième siège après retour à la ligne. Le texte est maintenant disposé par paires sur trois lignes aux largeurs paysage ≤1000 UI px; la hauteur du label compact accepte ces trois lignes. Changement de présentation seulement.

Unity 2022.3.62f3 : Economy **114/114**, Mechanics **141/141** (dont assertion dédiée aux six statuts), UI **246/246**, puis WebGL Release `BuildOptions.None` **Success**. Dans Chrome graphique, cette build a créé le lobby depuis l’UI; cinq sièges QA ont rejoint et affiché six statuts; l’hôte a confirmé Ready, démarré et lancé les dés. Le serveur confirme six sièges Ready, partie `Started`, commande `turn.roll` séquence 1 appliquée à l’état version 2 (`ResolveTile`, dés 3+4, joueur hôte case 7). `git diff --check` passe.

Captures comparables : [avant](LobbyRoster_2026-10-16/CurrentRelease_900x430/before.png), [roster corrigé](LobbyRoster_2026-10-16/CurrentRelease_900x430/after.png), [partie démarrée](LobbyRoster_2026-10-16/CurrentRelease_900x430/after_start.png), [premier lancer](LobbyRoster_2026-10-16/CurrentRelease_900x430/after_first_roll.png). Un seul client WebGL était rendu; les cinq autres sièges étaient QA API. La recette six interfaces WebGL indépendantes reste à exécuter sur ce Player courant.

## Complément : parcours mixte du 16 octobre 2026

Sur le même Player Release et le backend QA isolé, Guest2 (profil WebGL indépendant) a rejoint l’hôte initial via l’UI et s’est mis Ready. Après le départ de l’hôte initial pendant le pilotage automatisé, le transfert d’hôte au Guest2 a été observé. Cinq identités QA ont ensuite complété le lobby par l’API. Depuis l’UI WebGL du nouvel hôte, Start a fonctionné à **6/6 Ready** et a ouvert le plateau. Le premier lancer UI a produit 4+4; l’état autoritaire a appliqué la commande `turn.roll` séquence 1, état version 2, phase `ResolveTile`, joueur Guest2 en case 8 (Opioid Paradise). Captures d’écran : [lobby six sièges](CurrentRelease_900x430/after.png), [premier lancer du test mixte](CurrentRelease_900x430/mixed_api_seat_first_roll.png).

Portée : un seul client WebGL visible et actif pendant le démarrage/tour, un second client WebGL réel ayant auparavant rejoint et Ready, et cinq sièges API dans le roster final (l’hôte initial était parti, remplacé par QA7). Ce test complète la preuve de démarrage et de commande de jeu à six sièges, mais ne valide pas six clients WebGL indépendants ni la réception de l’état par les cinq autres interfaces. Il faut refaire ce parcours avec six profils/navigateurs connectés avant de fermer cette recette.

### Tentative d’automatisation six onglets

Une tentative supplémentaire a créé six contextes navigateur isolés avec le Player Release. À six instances WebGL simultanées dans un seul Chrome, plusieurs captures sont devenues noires et les événements clavier/souris n’étaient plus fiables avant l’arrivée des invités. Le salon d’essai n’a donc conservé que l’hôte; aucun résultat n’est compté pour la recette à six clients. Le banc a été arrêté. Pour la reprise, ouvrir/valider les clients de façon séquentielle et contrôler chaque Join/Ready côté serveur avant de poursuivre, ou utiliser six navigateurs/appareils indépendants.

## Clôture de la recette six clients — 16 octobre 2026

Le jalon six interfaces WebGL indépendantes sur le Player courant a depuis été validé. Six contextes Chrome distincts ont rejoint/Ready via l’UI; chaque client a joué un tour, Guest6 a tiré et confirmé une carte Venture, puis les six interfaces ont affiché le même état final. Voir `Docs/Validation/SixIndependentUI_CurrentRelease_2026-10-16/README.md`. Cela clôt la recette logicielle à six clients sur une seule machine; les six appareils humains et la recette physique iPhone/Safari restent ouverts.
