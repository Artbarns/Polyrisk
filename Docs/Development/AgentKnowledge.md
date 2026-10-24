# Références de développement assisté

Ces références sont reliées aux consignes persistantes du dépôt (`AGENTS.md` et `.agents/skills/`). Elles accélèrent les tâches ciblées ; elles n’autorisent pas l’ajout automatique de dépendances au jeu.

| Référence | Domaine | Usage retenu | Limite PolyRisk |
|---|---|---|---|
| [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) — `ui-uitk`, `optimize-web`, `optimize-text-mesh-pro` | Consignes d’agent Unity, UI Toolkit, WebGL, TextMeshPro legacy | Skills installés dans `.agents/skills/` et chargés selon le sujet | Le projet est Unity 2022.3.62f3 ; vérifier chaque API et instruction qui cible Unity 6 ou dépend d’un CLI indisponible. Le skill TMP ne s’applique qu’aux quelques composants legacy |
| [Unity UI Toolkit Manual Code Examples](https://github.com/Unity-Technologies/ui-toolkit-manual-code-examples) | UXML/USS, contrôles, listes et transitions | Référence officielle lors de travaux UI | Exemples à adapter à la version et aux conventions déjà présentes |
| [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween) | Motion/tween | Candidat de prototype pour les séquences d’animation | Non installé ; adoption seulement après preuve de compatibilité IL2CPP WebGL/iOS et bénéfice mesuré |
| [game-icons/icons](https://github.com/game-icons/icons) | Icônes de plateau | Source pour une sélection d’icônes sectorielles cohérentes | Ne pas importer tout le dépôt ; choisir un même style et respecter CC BY/CC0 par icône |
| [AspNetCoreDiagnosticScenarios](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios) et [ASP.NET Core Best Practices](https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/fundamentals/best-practices.md) | Fiabilité et performance .NET | Revue ciblée du lobby existant, pilotée par mesures | Pas de migration vers Nakama/SignalR ni d’infrastructure nouvelle sans besoin démontré |
| [dotnet/crank](https://github.com/dotnet/crank) | Benchmarks de charge .NET | Option si les tests légers existants ne suffisent plus | Surdimensionné comme premier outil pour des parties de six joueurs |

Le skill TextMeshPro est installé uniquement pour les quelques composants legacy ; l’UI runtime actuelle est majoritairement UI Toolkit/TextCore. Les exemples UI officiels, PrimeTween, Game Icons et les dépôts backend restent des références, pas des packages intégrés au build.
