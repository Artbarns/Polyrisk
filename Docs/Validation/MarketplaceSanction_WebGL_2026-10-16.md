# Recette WebGL de sanction Marketplace — 16 octobre 2026

## Parcours tenté sur deux clients

Un build Unity WebGL Release frais a été servi à deux onglets sur loopback, avec un lobby QA isolé. Le snapshot a placé l’hôte sur la case Marketplace 20 en `ResolveTile`, l’invité sur la case Départ, chacun avec 18 000 $. Les deux clients ont affiché la même case et l’hôte a ouvert Marketplace → Sanctions → Audit → sélection du joueur invité. Le coût affiché était de 2 000 $ et le panneau a bien affiché la cible.

À la taille de fenêtre de recette 992 × 720, la barre « Confirmer » n’était pas visible ni actionnable après la sélection. Le snapshot autoritaire est resté en version 2/séquence 0, les deux soldes sont restés à 18 000 $ et aucune activité d’audit n’a été publiée. Ce parcours est donc **non PASS** : il vérifie la navigation jusqu’au choix de cible et reproduit un blocage d’interface, mais n’envoie pas la sanction.

## Correctifs de layout et vérification

Le sélecteur permet à la liste de se contracter et défiler, réserve la barre de confirmation et agrandit la feuille à 96 % de l’écran. La rangée horizontale coût/bouton a ensuite été remplacée par une pile verticale : coût lisible, puis bouton pleine largeur de 48 px minimum. Ce format évite qu’une largeur minimale du bouton et le texte de cible se disputent la place sur petit écran. Aucun script de règle, payload réseau ou calcul économique n’a changé.

Unity 2022.3.62f3 a passé Economy **114/114**, Mechanics **137/137** et le build WebGL Release (`BuildOptions.None`). Artefact 12 MiB : `/tmp/PolyRiskSanctionConfirmVertical_20261016` ; journal : `/tmp/PolyRiskSanctionConfirmVertical_20261016.log`.

Le contrôle de poids Release entre le build précédent et celui-ci relève **12 662 169 → 12 664 026 octets compressés**, soit **+1 857 octets (+0,015 %)**. Le framework JavaScript et le WASM gardent les mêmes noms et tailles ; la variation se trouve dans le fichier de données Unity qui contient les ressources UI. Ce contrôle couvre le téléchargement, pas la cadence d’image, la mémoire ou la chauffe sur appareil.

La nouvelle barre n’a pas encore été visuellement confirmée et aucune commande audit n’a été soumise depuis ce dernier correctif. Un lobby temporaire neuf a été créé et rejoint par deux clients ; les deux étaient Ready, le lancement a réussi, puis l’hôte et l’invité ont affiché le snapshot Marketplace avec 18 000 $ chacun. Contrôle du stockage : version 1, séquence 0, zéro commande. Les onglets QA ouverts en arrière-plan n’affichaient le canvas que dans une zone réduite d’environ 320 × 180 pixels sur une capture 640 × 360, ce qui rendait les coordonnées de clic impropres à une validation UI fiable. Le lobby temporaire a été arrêté et supprimé sans envoyer de commande. Il faut refaire l’interaction dans un onglet visible avec un viewport calibré.

La cible de ce correctif est `Marketplace_Select_Player.uxml` (`Container_SelectConfirmBar`/`Btn_SelectConfirm`) : le bouton Audit apparaît après sélection d’un joueur et est déjà empilé en pleine largeur avec un minimum de 48 px. `Marketplace_Sanctions.uxml` contient un autre bouton, `Btn_ConfirmSanction`, utilisé par les sanctions qui passent ensuite par une sélection de propriété ; il n’est pas le bouton Audit décrit par ce parcours.

Le relais HTTP, l’idempotence et les effets économiques côté contrôleur sont couverts séparément dans `MarketplaceSanction_Transport_2026-10-16.md` et `ForceAudit_Snapshot_2026-10-16.md`. Le parcours UI restant doit confirmer : bouton visible et actionnable, débit de 2 000 $, taxe de 5 %, snapshot synchronisé et activité reçue par l’invité.

## Nouvelle tentative de recette

Le 16 octobre, l’API lobby et deux origines QA (`5082`/`5083`) ont démarré sur la build Release actuelle avec un stockage temporaire vide ; `index.html` répondait HTTP 200. L’outil de navigateur a ensuite refusé la navigation vers l’adresse QA avec sa politique de sécurité d’URL. Aucun client n’a donc créé de partie et aucune commande de jeu n’a été envoyée. Les processus QA ont été arrêtés immédiatement ; la préversion publique et son lobby n’ont pas été touchés. Je n’ai pas contourné le refus en changeant d’outil ou de surface navigateur. La confirmation visuelle et le clic restent ouverts ; la tentative n’ajoute aucune preuve de fonctionnalité.
