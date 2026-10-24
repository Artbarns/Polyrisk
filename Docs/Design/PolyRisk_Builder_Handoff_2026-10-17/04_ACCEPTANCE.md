# Validation de l'intégration à effectuer par le builder

Cette liste concerne le futur jeu intégré. Les vérifications des previews fournies ne la remplacent pas.

## Dés

- Les 36 couples et 6 doubles affichent les bonnes faces et sommes. Tester les valeurs imposées via un outil de test ; aucun bouton de triche dans l'UI publiée.
- Premier dé arrêté avant le second ; appui au sol crédible ; suspension, bascule et contact final synchronisé au son. Pas de croisement, spin complet ni dérive de la valeur gameplay.
- Grand total derrière les dés ; aucune addition textuelle ; total final encore consultable après l'animation.
- Double ordinaire/favorable correctement ponctué ; double pénalisant, Audit, Turbo et Précision cohérents avec le résultat réel.
- Skip à chaque phase, nouveau lancer, widget désactivé, changement de scène, perte de focus, mode réduit, mute : aucun callback/son ancien et aucune interface bloquée.
- Conserver le modèle RNG ; les variations cosmétiques ne consomment pas le générateur gameplay.

## Pulse et cockpit

- Tous les montants et propriétés sont liés au modèle ; aucun montant, 10 %, 2/3 ou nom de fixture conservé en dur.
- Familles : 0, partielle et complète, vraie taille du catalogue. Propriétés libres, locales, adverses, hypothéquées ou indisponibles correctement identifiées selon les règles existantes.
- Deux crans : expansion, réduction, drag facultatif, clic/tap, clavier ; sélection conservée ; focus cohérent ; ouverture directe depuis famille et finances.
- Cartes : sélection explicite, fiche synchronisée, actions adaptées. Ne pas confondre inspecter une propriété avec déplacer le pion ou changer le joueur actif.
- GO : base, dividendes, burn rate / veille / actif, taxe Hubs et effets réellement présents pris en compte par la source métier. Flux clairement estimé et libellé au passage GO. Les inconnues ne deviennent pas des zéros trompeurs.
- Joueurs : 2, 4 et 6, noms longs, joueur actif et spectateur ; pas de label tronqué incompréhensible ni débordement.
- Achats, loyers partagés, taxes, arrivée et changement de joueur : acteur du reçu conservé, montant exact et bénéficiaires exacts, aucune double narration de la scène de dés.
- Tour adverse et refus réseau : actions locales interdites réellement désactivées ; pas de fausse confirmation, pas de double commande et état de rejet lisible.
- Reconnexion : reconstruire l'état actuel, pas rejouer des célébrations anciennes ou perdre le contexte inspecté.

## Surface et performance

- Compiler/importer avec l'éditeur installé ; contrôler les diagnostics USS/UXML et Console.
- Régressions existantes pertinentes de règles/économie/UI/événements selon les fichiers touchés.
- Build Release WebGL dans un navigateur. Couvrir petit portrait, portrait courant, paysage bas et bureau, par exemple 320×720, 430×900, 932×430 et 1280×720. Ces tailles sont des cas de test, pas des layouts fixes.
- Actions accessibles, HUD visible, pas de contrôle sous safe area ; aucun overlay qui empêche une décision utile.
- Comparer temps de frame, allocations et mémoire avant/après. Cible de 60 FPS à confirmer sur les appareils visés ; ne pas revendiquer une couverture iPhone/Android physique sans essai réel.
- Mode réduit : résultat final sans animation obligatoire. Information jamais uniquement sonore ou uniquement colorée. Contrastes et cibles tactiles vérifiés sur le vrai rendu.

## Preuves attendues

Captures ou vidéo des deux crans, changement de carte, famille/portefeuille, flux GO, lancer ordinaire et double ; résumé des tests réellement exécutés, des commandes de build et des limites. Une capture de la maquette HTML ne compte pas comme preuve d'intégration Unity.

## Options, seulement après nouveau choix d'Arthur

- Slider : relâchement avant/après seuil, relâchement hors cible, annulation/capture perdue, tap alternatif, clavier, réseau lent, refus, prix/solde/état devenus invalides, commande unique et retour fiable.
- Famille complète : utiliser l'événement confirmé et le vrai bonus, annulation d'une présentation, pas de répétition au replay d'un snapshot.
- Loyer crédité : solde affiché exactement égal au modèle à la fin, transferts fusionnés sans perdre les bénéficiaires, pas de particules pour chaque micro-opération.
