# Dock d’état de partie — prototype et validation (16 octobre 2026)

## Réalisation

- Un Dock persistant a été ajouté au panneau de commandes : phase et joueur actifs, dernière activité durable et bouton d’ouverture/fermeture d’un historique des 12 dernières activités.
- Lors d’une décision sur une tuile, le Dock adopte un état compact : le titre indique la phase et la tuile, la ligne d’événement redondante se replie et reste consultable dans le journal. Cela préserve la place nécessaire à la fiche et à ses actions.
- L’état est dérivé du `TurnManager` et des activités déjà émises par le jeu. Le HUD supérieur conserve les métriques; les événements ne sont plus présentés comme un toast temporaire redondant.
- Les événements locaux d’achat et de loyer sont formulés à la première personne. Le message de loyer ne nomme aucun bénéficiaire, car l’événement d’activité actuel ne transporte pas cette donnée; aucun destinataire n’est inféré.
- Le contrôle visuel a révélé un débordement du message vers la fiche de propriété à l’arrivée. La mise en page a été corrigée par un état compact dédié aux décisions de tuile; les messages restent accessibles dans le journal. Le Dock et l’historique restent dans le flux du panneau, sans ajout de Canvas ou de dépendance.

## Vérifications

- Unity 2022.3.62f3 — `RunAllEditorRegressions` : économie **114 assertions PASS**, mécaniques **141 PASS**, UI **252 PASS sur 17 écrans/composants**.
- Unity WebGL Release — `BuildWebGLRelease` : **Build Finished, Result: Success**; artefact compressé total **12 MiB**.
- Contrôle runtime WebGL dans navigateur local avec en-têtes `Content-Encoding: gzip`, viewport étroit **320 × 720** : accueil → partie locale → lancer → déplacement → arrivée sur une propriété. Le Dock a affiché la phase et le résultat du lancer, s’est compacté à l’arrivée et a laissé visibles la fiche, le prix et les actions. L’historique s’ouvre et conserve les trois activités du parcours sans recouvrir la fiche.
- `git diff --check` : PASS.

## Limites restantes

- Cette vérification visuelle est un parcours local sur navigateur en viewport étroit; elle ne valide pas encore Safari sur iPhone physique, l’ensemble des dimensions portrait/paysage, la performance mobile, ni les parties réseau à deux et six clients.
- Le schéma d’activité ne permet pas à l’interface d’identifier le bénéficiaire d’un loyer ni de rendre un reçu précis. Pour cela, consommer un événement/transfert autoritaire existant, après vérification de sa source, sans recalculer la transaction dans l’UI.
- La build est produite dans `/tmp/PolyRiskWebGLRelease` pour recette locale; elle n’est pas publiée ni utilisée pour remplacer le lien partagé.

## Hors périmètre

Aucune règle, formule économique, donnée de partie ni donnée réseau n’a été modifiée dans ce chantier.
