# Handoff — lancer de dés PolyRisk

Référence issue de l'itération motion avec Arthur, 17 octobre 2026.

- `preview.html` : aperçu autonome ; ouvrir dans un navigateur. L’habillage de navigation charge des utilitaires depuis un CDN ; prévoir une connexion pour son affichage complet. Trois variantes, lancer ordinaire et bouton « Voir un double ». Le son est désactivé par défaut ; l'activer pour entendre le contact et l'accord du double.
- `reference.fragment.html` : source des animations, keyframes et sons synthétiques. Référence technique, pas code Unity directement intégrable.
- `BRIEF_BUILDER.md` : prompt autonome à transmettre au builder, avec décisions validées, paramètres et vérifications.
- `audio/` : WAV originaux utilisables comme base d'intégration.

MVP recommandé : Rebond vertical. Les deux autres variantes restent disponibles pour comparer ; Arthur n'a pas arrêté explicitement le choix entre elles. Les essais précédents avec mouvements circulaires / croisés ont été écartés.

Pour transmettre dans un autre chat du même projet, coller :

```text
Intègre le lancer de dés décrit dans Docs/Design/DiceMotion_Handoff_2026-10-17/BRIEF_BUILDER.md. Lis ce brief en entier et inspecte preview.html et reference.fragment.html dans le même dossier. Commence par Rebond vertical, conserve la bascule finale sur l'arête puis « tac », le grand résultat derrière les dés et la signature verte du double. Réalise une intégration Unity ciblée avec les règles et le réseau inchangés, puis montre-moi un aperçu réel et les validations effectuées.
```

Si le builder n'a pas accès au disque de ce projet, lui transmettre l'archive et lui demander de commencer par `BRIEF_BUILDER.md`.

Le prototype a été vérifié dans le navigateur pendant l'itération (navigation et fin de lancer des trois variantes, double et résultat neutre). Cela ne valide pas une intégration Unity, un build WebGL de production ou du matériel mobile réel. Les WAV du handoff sont des synthèses de référence à ajuster après écoute dans le mix du jeu.

Ce dossier ajoute des documents et références uniquement. Aucun code de jeu n'est modifié par cet export.
