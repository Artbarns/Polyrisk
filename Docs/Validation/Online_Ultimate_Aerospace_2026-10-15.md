# Pouvoir Aerospace en multijoueur WebGL — 15 octobre 2026

Deux clients WebGL Release indépendants ont rejoint le lobby QA local
`UD8KZ4`. La fixture installée avant l’initialisation de l’autorité donnait à
l’invité les propriétés Aerospace nécessaires, 100 points d’énergie et le
premier tour en phase `Planning`. Depuis le sélecteur du client invité, le
joueur a choisi la destination NASA Moonbase (case 33), mise en évidence comme
admissible.

Le contrôle initial confirmait l’activation, mais son snapshot final arrivait
après l’avancement automatique de Planning. La reprise ciblée ci-dessous capture
maintenant le résultat avant ce délai.

## Destination, retour invité et reconnexion — 15 octobre 2026

Deux clients WebGL Release ont joué sur un lobby local isolé. La fixture
préalable donnait à l’hôte les deux propriétés Aerospace, 100 points d’énergie
et le tour en `Planning`. Depuis le sélecteur de l’hôte, Aerospace a été choisi,
puis NASA Moonbase (case 33) a été sélectionnée dans le plateau.

Le snapshot immédiatement après l’action est la version 2/séquence 1 : le
joueur hôte est à la position 33, son énergie passe de 100 à 0 et l’activité
unique est « active Saut quantique ». Les deux soldes restent à 18 000 $. La
case 33 correspond à NASA Moonbase dans `Docs/GDD_Board.md`. L’écran invité a
reçu la mise à jour ; après rechargement WebGL, il a réaffiché la bannière
violette « Tech QA Host active Saut quantique » et le plateau.

Le snapshot serveur pris après ce rechargement est en version 3/séquence 1 et
conserve une seule activation. Entre-temps, le filet Planning avait fait lancer
les dés et avancer le pion jusqu’à la case 1 ; les 33,3 points d’énergie visibles
à cette étape viennent du passage par GO. Ce mouvement automatique est postérieur
à la téléportation et ne constitue pas une seconde activation.

Preuves : [snapshot immédiat à NASA Moonbase](Online_Ultimate_Aerospace_2026-10-15/state_after_nasa_moonbase_selection.json)
et [snapshot après rechargement et filet Planning](Online_Ultimate_Aerospace_2026-10-15/state_after_guest_reload_and_planning_timeout.json).
Le serveur, le stockage et les deux navigateurs étaient isolés sur loopback ;
aucune règle ni source Player n’a été modifiée.

**Résultat : activation, destination 33, réplication invitée et reprise avec
activité visible PASS.** La capture invitée après rechargement montre le plateau
et la bannière ; la position 33 est établie par le snapshot immédiat d’avant le
rechargement, le snapshot suivant étant déjà postérieur au filet Planning.
