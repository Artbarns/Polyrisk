# Recette lobby en ligne — Release WebGL courante (16 octobre 2026)

Build testée : `/tmp/PolyRiskActivityLocalizationReleaseFinal_20261016`, servie sur deux origines Chromium séparées avec une fenêtre contrôlée de 1280×720 CSS et DPR 1. Le serveur de lobby .NET était isolé sur `127.0.0.1:5080`, avec les données QA dans `/tmp/PolyRiskLobbyUiQA_20261016/retry.json`. La prévisualisation publique, toujours servie sur le port 5093, n'a pas été modifiée.

## Résultat de bout en bout : PASS à deux clients

1. L'hôte a créé le salon `THZW6R` depuis le bouton Create Online Game. État serveur : `1/6`, hôte Player.
2. Le second client a saisi le code dans le champ de l'interface et a rejoint sous le nom GuestQA. Le serveur confirme les deux sièges et la capture montre le même roster `2/6` sur les deux clients. Le premier essai GuestQA avait été refusé en HTTP 400, car les deux profils utilisaient le nom « Player » ; après avoir saisi un nom distinct, Join est passé.
3. Les deux clients ont cliqué Ready. Snapshot serveur : deux joueurs Ready, version lobby 4. Les deux écrans ont reçu le roster convergent.
4. L'hôte a démarré la partie depuis l'UI. Snapshot : état `started`, version lobby 6, état de partie version 1.
5. L'hôte a lancé les dés. Le serveur a enregistré une commande `turn.roll` (séquence 1), état de partie version 2, dés 3 + 2, joueur hôte déplacé à la case 5. Les deux clients ont chargé le même tour : l'hôte voit l'inspecteur « Temu Sweatshop Terminal » et Buy/Pass, l'invité voit WAIT.

## Captures

- Création depuis l'UI : [host_created_current.png](host_created_current.png)
- Roster `2/6` vu par l'hôte : [host_roster_2of6.png](host_roster_2of6.png)
- Roster `2/6` vu par l'invité : [guest_roster_2of6.png](guest_roster_2of6.png)
- Après le lancer, vue de l'hôte : [host_after_roll.png](host_after_roll.png)
- Après le lancer, vue de l'invité : [guest_after_roll.png](guest_after_roll.png)

## Portée

Cette recette valide l'interface Release et le flux réseau de Create → Join → Ready → Start → première action à deux clients indépendants. Elle ne remplace ni la recette six clients indépendants, déjà consignée dans `SixPlayer_WebGL_2026-10-15/README.md`, ni les tests humains à six, Safari/iPhone physique, le profilage mobile ou la publication finale. Aucun code du jeu ni donnée gameplay n'a été modifié dans cette reprise.
