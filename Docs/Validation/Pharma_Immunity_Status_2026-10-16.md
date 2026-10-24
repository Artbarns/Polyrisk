# Indicateur persistant de l’immunité Pharma — 16 octobre 2026

## Changement

Ajout d’une pastille `PHARMA IMMUNITY · 1 TURN` dans la zone d’activité existante du HUD. Elle apparaît uniquement pour le joueur local lorsque `ImmunityTurnsRemaining` est positif et disparaît à l’expiration de l’effet. Le toast partagé conserve sa durée normale et sa disparition ne masque pas la pastille.

L’interface se met à jour lors de l’activation locale d’un pouvoir, à la réception d’un état répliqué (l’événement de solde est émis après application du snapshot) et au début d’un tour. Le style est une petite surface sombre avec un repère cyan discret. Aucun état de jeu, règle, valeur économique ou donnée réseau n’a été modifié.

Fichiers : `Assets/Scripts/UI/TopHUDWidget.cs`, `Assets/UI/Layouts/TopHUD.uxml`, `Assets/UI/Styles/Theme.uss`.

## Validation

- `git diff --check` : PASS.
- Unity 2022.3.62f3 : Economy **114/114**, Mechanics **130/130**.
- Build WebGL Release (`BuildOptions.None`) : `WEBGL PLAYER BUILD PASS`, code de sortie 0, artefact `/tmp/PolyRiskPharmaStatusFinal` (12 MiB), journal `/tmp/PolyRiskPharmaStatusFinal.log`.
- Recette WebGL locale à deux clients, backend et données QA isolés : activation du vrai ultime Pharma par le sélecteur Unity de l’hôte, énergie 100 → 0 et snapshot autoritaire version 2/séquence 1 avec immunité 1 tour. Le HUD affiche la pastille distincte du toast ; après rechargement complet et reprise de session, le plateau réapparaît avec la pastille encore visible.
- Recette complémentaire d’expiration naturelle : un état QA pré-partie place l’hôte case 39 avec une immunité d’un tour. Son vrai lancer **3+1** passe par Départ et l’amène case 3 ; le snapshot autoritaire version 2/séquence 1 confirme position 3, solde 25 700 $, énergie 33,33, `immunityTurnsRemaining = 0` et `hasImmunityCard = false`. Après le mouvement, le HUD ne montre plus la pastille. L’invité rechargé retrouve le même plateau, l’arrivée sur Trump Tower et son propre HUD ; aucune commande supplémentaire n’est reçue. Snapshots expurgés : [`snapshot_before_pass_go.json`](Pharma_Immunity_Status_2026-10-16/snapshot_before_pass_go.json) et [`snapshot_after_pass_go.json`](Pharma_Immunity_Status_2026-10-16/snapshot_after_pass_go.json).
- Lobby, stockage et clients de recette arrêtés et supprimés après le contrôle. L’artefact Release du build reste dans `/tmp/PolyRiskPharmaStatusFinal`.
