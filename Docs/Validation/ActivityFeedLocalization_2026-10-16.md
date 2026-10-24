# Cohérence de langue du flux d’activité — 16 octobre 2026

## Symptôme et correction

En partie locale WebGL, le HUD affichait les messages enregistrés en français — par exemple « Joueur 1 passe sur Opioid Paradise » — alors que les commandes, le HUD et le reste de l’interface étaient en anglais.

`TopHUDWidget` traduit les phrases connues à la présentation, et utilise « Player »/« Game » comme noms de secours. Les snapshots de partie, l’historique d’activité et les textes employés par les systèmes restent inchangés ; les parties reprises après reconnexion sont également présentées en anglais lorsque leur message correspond à une phrase connue.

## Vérification

`UIAssetRegression` vérifie 42 formes d’événement couvrant le tour, les dés, l’arrivée, la passe, les achats/loyers, l’Audit, les investissements, le casino, la famille, les pouvoirs, la Marketplace, les sanctions, l’OPA, la faillite, le transit et les cycles. Les cas vérifient aussi que les montants et les noms propres restent intacts.

Unity 2022.3.62f3, `RunAllEditorRegressions` : Économie **114/114**, Mécaniques **140/140**, UI **246/246**, total **500 assertions**, processus terminé code 0. `BuildVerifiedWebGLPreview`, `BuildOptions.None` : build Release WebGL PASS, sortie isolée `/tmp/PolyRiskActivityLocalizationReleaseFinal_20261016`, processus terminé code 0. Les trois fichiers compressés data/framework/WASM représentent **12 663 311 octets**.

La capture mobile précédente montre le défaut avant correction. Le navigateur Chromium n’a pas rendu de capture exploitable après le nouveau build ; la vérification runtime visible du bandeau reste à refaire. Le build est local et non publié. Journaux : `/tmp/PolyRiskActivityLocalizationFinalTests_20261016.log` et `/tmp/PolyRiskActivityLocalizationReleaseFinal_20261016.log`.
