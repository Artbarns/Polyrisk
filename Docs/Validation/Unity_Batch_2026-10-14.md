# Régressions Unity en batch — 14 octobre 2026

Les régressions ont été exécutées dans l’Éditeur Unity **2022.3.62f3** installé sur la machine, en mode batch, sur le projet courant. Il s’agissait de tests du code de production via les méthodes Editor du projet, pas d’une simulation externe ni d’un test MCP.

| Suite | Résultat | Assertions |
|---|---:|---:|
| `PolyRisk.Editor.EconomyRegression.Run` | PASS, processus Unity code 0 | 114 |
| `PolyRisk.Editor.MechanicsRegression.Run` | PASS, processus Unity code 0 | 99 |

Le correctif Aggressive Growth et son extension ciblée des régressions sont enregistrés dans `Docs/Validation/Unity_Aggressive_Growth_2026-10-14.md`. Les sorties complètes ont été conservées temporairement dans `/tmp/polyrisk_aggressive_growth_build_2026-10-14.log`. Aucune erreur de compilation C# ou assertion en échec n’a été relevée. Ces suites valident les cas qu’elles couvrent, mais ne remplacent pas une partie complète dans l’interface, une recette Safari/iPhone, ni des parties humaines avec des amis.

## Revalidation dans Unity — 15 octobre 2026

Pour réduire les allers-retours, `EconomyBatchValidation.BuildVerifiedWebGLPreview`
a exécuté dans un seul lancement l’économie, les mécaniques puis un build Release
WebGL (`BuildOptions.None`). Commande :

```bash
POLYRISK_WEBGL_OUTPUT=/tmp/PolyRiskPostPlaytestReview unity run . \
  --editor-version 2022.3.62f3 --timeout 900 \
  --log-file /tmp/PolyRiskPostPlaytestReview.log --no-tail -- \
  -executeMethod PolyRisk.Editor.EconomyBatchValidation.BuildVerifiedWebGLPreview
```

Résultats du journal Unity : `ECONOMY REGRESSION PASS: 114 assertions`,
`MECHANICS REGRESSION PASS: 99 assertions` et `WEBGL PLAYER BUILD PASS`.
Le build Release frais occupe environ 12 MiB et se trouve dans
`/tmp/PolyRiskPostPlaytestReview`. La page a été servie en local sur
`http://127.0.0.1:8932/` : index, loader, framework, WASM, données et ressources
de template ont tous répondu HTTP 200 ; le navigateur a exposé le conteneur et
le canvas Unity. Cette build de contrôle n’a pas été publiée et n’a pas remplacé
la préversion partagée.

La commande batch a émis des avertissements de jeton/licence Unity absent dans
le journal, mais a terminé compilation, tests et build avec les marqueurs PASS.
Cette validation ne remplace pas une recette iPhone/Safari ni les résultats
humains de la partie à quatre, qui restent à consigner.
