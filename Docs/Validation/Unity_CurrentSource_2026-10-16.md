# Validation Unity du source courant — 16 octobre 2026

## Résultat

Unity 2022.3.62f3 a exécuté les suites Editor puis produit un WebGL Release (`BuildOptions.None`) à partir de `Assets/Scenes/SampleScene.unity`, avec le roster compact pour l’hôte seul et les noms insécables.

- EconomyRegression : **114/114**, PASS.
- MechanicsRegression : **140/140**, PASS.
- Build WebGL Release : **Success** ; marqueur `WEBGL PLAYER BUILD PASS` ; processus Unity terminé code 0.
- `git diff --check` : PASS.

Sortie isolée : `/tmp/PolyRiskLobbyNameWrapRelease_20261016` ; journal : `/tmp/PolyRiskLobbyNameWrapRelease_20261016.log`.

## Payload

Fichiers WebGL compressés produits : données 4 898 161 octets, WASM 7 681 625, framework JS 81 218 et loader JS 20 680. Les trois fichiers compressés principaux représentent 12 661 004 octets ; avec le loader, 12 681 684 octets. Le journal Unity rapporte 4,2 MB compressés pour la scène. Ces tailles ne mesurent ni la cadence, ni la mémoire, ni la chauffe sur appareil.

Le build servi temporairement en loopback renvoie HTTP 200 pour `index.html`, le loader, le framework, le WASM et les données compressées. Le serveur QA a ensuite été arrêté.

Cette exécution confirme la compilation, les régressions locales et le rendu du roster 6/6 en client Release aux trois dimensions indiquées dans `Docs/Validation/LobbyRoster_2026-10-16.md`. Elle ne valide pas six navigateurs indépendants, Safari/iPhone physique ni les performances sur appareil.
