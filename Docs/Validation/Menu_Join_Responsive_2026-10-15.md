# Entrée dans un lobby — correction responsive — 15 octobre 2026

## Écart constaté et correction

Dans le rendu WebGL large observé, le bouton `JOIN` apparaissait hors de la
zone visible à côté du champ de code. La pression d’Entrée pouvait encore
rejoindre le lobby, mais le parcours tactile/souris n’était pas assez évident.

Le champ de code et le bouton sont maintenant empilés dans `Theme.uss`, avec
le bouton sur toute la largeur disponible. Cette composition est utilisée à
toutes les largeurs ; elle réutilise le `ScrollView` déjà présent du menu et
n’ajoute ni élément Unity actif, ni texture, ni dépendance.

## Vérification

- Régressions Unity 2022.3.62f3 : Economy **114/114**, Mechanics **130/130**.
- Build joueur WebGL Release `BuildOptions.None` : **PASS**, artefact QA isolé
  `/tmp/PolyRiskJoinWideRelease_20261015` (~12 MiB), non publié.
- Deux navigateurs WebGL indépendants : hôte créé le lobby `B87TR2`, invité a
  saisi le code puis cliqué le bouton visible. L’invité rejoint et l’hôte
  affiche les deux noms en `WAITING` (`2/6 PLAYERS`).

Le test est navigateur QA sur ordinateur ; Safari/iPhone réel et profilage
mobile restent à faire.
