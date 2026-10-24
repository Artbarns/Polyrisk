# Pouvoir Tech en multijoueur WebGL — 15 octobre 2026

## Parcours exécuté

Deux clients WebGL Release indépendants ont rejoint le lobby QA local
`V486GN`. Avant le démarrage de l’autorité, le test a chargé une fixture où
l’hôte possédait les trois propriétés Tech, avait 100 points d’énergie et se
trouvait en phase `Planning`. Depuis l’interface hôte, le sélecteur a soumis
`ultimate.use` pour Tech.

## Résultat autoritaire

Le reçu serveur confirme une commande `ultimate.use`, séquence 1 ; l’état est
publié en version 2 et reste en `Planning`. L’énergie de l’hôte passe de 100 à
0, les deux soldes restent à 18 000 $, et les propriétés Tech 27 et 28 passent
du niveau 0 au niveau 1 ; la propriété 31 reste au niveau 0. Le serveur
enregistre l’activité « active Mise à jour système ». C’est un PASS pour le
parcours interface hôte → commande réseau → effet et snapshot autoritaires.

L’invité est resté connecté sur le plateau et en état d’attente. Sa capture ne
permet pas de confirmer visuellement les niveaux des propriétés. La bannière
temporaire de l’activité n’était pas visible ; le toast et l’effet côté invité
restent à capturer immédiatement lors d’une prochaine activation.

## Portée

Build Release WebGL produite le 15 octobre avec Unity 2022.3.62f3 ; service et
stockage de lobby isolés sur loopback. La fixture a été installée alors que les
deux sièges étaient prêts et avant le démarrage de l’autorité. Aucune règle,
économie ni source Player n’a été modifiée. Cette preuve couvre le traitement
autoritaire de Tech en réseau. Lors de ce premier parcours, la capture invitée
était trop tardive pour confirmer le retour visuel ; le parcours complémentaire
ci-dessous confirme la bannière et la reprise du plateau après rechargement.
Les cinq autres pouvoirs restent hors de cette validation.

## Retour visuel invité et reprise après rechargement — 15 octobre 2026

Un second parcours utilise deux clients WebGL Release sur un lobby loopback
isolé. Une fixture Tech a été restaurée avant le démarrage de l’autorité : les
trois cases Tech appartenaient à l’hôte, qui avait 100 points d’énergie. Après
l’activation depuis l’interface hôte, l’invité a affiché la bannière violette
« Tech QA Host active Mise à jour système ».

L’hôte a publié la version 2/séquence 1 : énergie 100 → 0, propriétés 27 et 28
au niveau 1, propriété 31 au niveau 0, soldes inchangés à 18 000 $ et activité
Mise à jour système. Après rechargement de l’onglet invité, le WebGL a restauré
le plateau et continué la même partie. Le snapshot capturé ensuite conserve
l’énergie nulle, les deux améliorations et la séquence 1 sans nouvelle dépense
ni nouvelle commande. Entre les deux captures, le filet Planning a lancé le tour
(version 3, `ResolveTile`, arrivée Venture) ; cette progression est distincte
de la reprise Tech.

Snapshots avant et après reprise :
[avant](Online_Ultimate_Tech_2026-10-15/state_before_guest_reload.json) et
[après](Online_Ultimate_Tech_2026-10-15/state_after_guest_reload.json). Le QA a utilisé le build Release du
15 octobre, le serveur et le stockage temporaires sur loopback, sans publier ni
modifier les règles. La bannière a été observée avant rechargement ; la reprise
visuelle du plateau est confirmée, mais l’animation/toast éphémère ne survit pas
au rechargement, comme attendu pour une notification temporaire.
