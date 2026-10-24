# PolyRisk

Jeu de stratégie financière sous Unity 2022.3.62f3 / URP 14, avec plateau isométrique à40cases et client WebGL. Le serveur multijoueur.NET10 se trouve dans `Server/PolyRisk.LobbyServer`.

## Ouvrir le projet

Ouvrir ce dossier avec Unity2022.3.62f3, puis la scène `Assets/Scenes/SampleScene.unity`. Unity restaure les packages depuis `Packages/manifest.json` et `Packages/packages-lock.json`.

Le rendu actuel utilise le thème partagé `Assets/Settings/GraphiteFoundation/GraphiteTheme.asset`. Dans l'Editor, **PolyRisk → Art → Graphite Calibration** ouvre le banc de comparaison. Il compare la finition précédente et la nouvelle architecture graphite sans changer les données économiques.

## Validation visuelle

[Galerie A/B et vidéos avec audio](Docs/Validation/PremiumMasterpiece_2026-10-24/review.html) · [rapport](Docs/Validation/PremiumMasterpiece_2026-10-24/README.md) · [paramètres](Docs/Validation/PremiumMasterpiece_2026-10-24/PARAMETERS.md).

Les médias sont des captures WebGL réelles sur Mac. La nouvelle version est construite en Release, mais Safari/iPhone physique, performances sur appareils et parties distantes restent à valider. Les essais de protocole locaux ne représentent pas sixiPhone rendus.

## Serveur et publication

```sh
dotnet build Server/PolyRisk.LobbyServer/PolyRisk.LobbyServer.csproj -c Release
```

La configuration de livraison du client et du serveur sur une même adresse est documentée dans [Server/Deployment/README.md](Server/Deployment/README.md). Elle requiert une instance de serveur.NET10 et un volume persistant séparé de la racineWeb. Les secrets de déploiement ne doivent pas être versionnés.

GitHubPages peut afficher la galerie statique ; il ne peut pas exécuter le serveur multijoueur.NET. Une galerie accessible enHTTPS n'est donc pas une validation de release multijoueur.

## Périmètre

La passe graphique conserve les40index, les meshes fusionnés, les colliders, la caméra×1,40, les règles économiques et le protocole réseau. Les outils de calibration runtime sont exclus des builds nonDevelopment. Le dépôt public contient les sources nécessaires au projet et une sélection des preuves finales ; les caches Unity, données privées, builds générés et archives intermédiaires restent exclus.
