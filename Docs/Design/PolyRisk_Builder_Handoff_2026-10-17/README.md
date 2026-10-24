# PolyRisk — package Builder GPT-6

17 octobre 2026. Deux designs validés par Arthur : **dés avec bascule finale** et **Pulse Dark Luxury Néon / inspecteur visuel**. Aucune intégration runtime n'est effectuée par ce package.

## À donner au builder

Lui transmettre l'archive complète ou ce dossier, puis coller le contenu de `START_HERE_GPT6.md`. Dans le même projet, un message court suffit :

```text
Implémente les deux designs validés du package Docs/Design/PolyRisk_Builder_Handoff_2026-10-17. Commence par lire START_HERE_GPT6.md en entier, ouvre les deux previews approved et suis les briefs et critères de validation. Réalise l'intégration Unity existante puis montre-moi un aperçu réel. Les moments clés optional sont des propositions, pas des ajouts approuvés.
```

## Contenu

| Fichier / dossier | Usage |
|---|---|
| `START_HERE_GPT6.md` | Prompt autonome, ordre de travail et périmètre |
| `01_DICE_APPROVED.md` | Dés, timings, double, audio et intégration |
| `02_PULSE_APPROVED.md` | UI/cockpit, données, crans, cartes, finances et intégration |
| `03_OPTIONAL_MOMENTS.md` | Recommandations distinctes, non validées |
| `04_ACCEPTANCE.md` | Vérifications à exécuter dans le vrai jeu |
| `design-motion-tokens.json` | Couleurs et paramètres de départ, pas une config Unity importable telle quelle |
| `previews/*-approved.html` | Deux aperçus des designs approuvés |
| `previews/key-moments-optional.html` | Achat glissé, famille complète et crédit de loyer à essayer |
| `sources/` | Fragments HTML, keyframes et logique ; archive de coque Pulse explicitement délimitée |
| `DIRECTION_MEMORY_SNAPSHOT.md` | Copie des décisions enregistrées dans la mémoire projet |
| `audio/` | Trois WAV originaux 48 kHz mono / PCM 16 bits, sans sample tiers |
| `manifest.json` | Statut des références et empreintes SHA-256 des fichiers |
| `verify_package.py` | Vérification d'intégrité locale : `python3 verify_package.py` |

Les previews s'ouvrent dans un navigateur ; l'habillage charge des utilitaires/icônes depuis un CDN, prévoir une connexion pour son affichage complet. Les sources visuelles et le plateau de contexte sont inclus. Les appels d'hôte ont été limités à la sauvegarde d'état prise en charge par l'export.

## Autorité et limites

En cas d'écart : dernière validation d'Arthur et briefs APPROVED > anciennes explorations ; GDD et modèle autoritatif > données illustratives des maquettes. La preview UI approuvée est l'itération `pulse-inspecteur-visuel.html`, pas la version menthe initiale, les listes, Arena ou Atelier.

Les deux apparences/propositions sont approuvées. Rebond vertical reste le choix par défaut recommandé parmi les trois mouvements simples présentés. Les gestes du slider, la complétion de famille et le crédit animé de loyer restent optionnels. Aucun droit à changer les règles, déployer, publier ou contacter un autre chat n'est inclus.

`VALIDATION_PACKAGE.md` précise ce qui a été contrôlé sur le package et ce qui reste à tester par le builder. Les maquettes ne sont pas du code Unity prêt à importer. Leurs captures incorporées ne sont pas des assets de plateau de production. Les noms, montants, ROI et tailles de famille de démonstration ne doivent pas être copiés comme règles.
