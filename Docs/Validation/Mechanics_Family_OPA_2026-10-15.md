# Pouvoirs de famille et OPA — contrôle du 15 octobre 2026

## Vérification du troisième titre

La suite `MechanicsRegression.Run` a été relancée dans Unity 2022.3.62f3 sur
le source courant : **123/123 assertions PASS** (dernier rejeu en batch).
Elle vérifie qu’un troisième
titre acheté à la banque complète immédiatement la famille, rend le pouvoir
disponible et recharge l’énergie au passage GO.

Les routes d’acquisition qui complètent la famille sont maintenant couvertes
côté feedback : la même annonce de déblocage part après acquisition par achat,
OPA, vente Marketplace, enchère banque et tender. Le point de publication est centralisé dans
`AbilitySystem.PublishFamilyCompletionIfNeeded`; la restauration d’un snapshot
ne l’appelle pas, pour éviter de répéter l’annonce à la reconnexion. Les tests
Unity vérifient la complétion et le feed pour les acquisitions OPA, directes
Marketplace, enchère banque et tender. Les quatre assertions ajoutées après le
build Release ne touchent que le script Editor de régression ; aucun script du
Player Unity n’a changé après le build.

## OPA

Le contrôle Unity courant de logique confirme le prix calculé, le paiement au
vendeur, le transfert de propriété, le maintien du niveau/upgrades, la
conservation de la somme des liquidités et le refus d’une cible inabordable
sans transfert. La preuve réseau à deux clients couvre aussi la confirmation
native par l’invité, le retour après reconnexion sans second transfert et
l’OPA inabordable sans débit :
`Docs/Validation/Reconnect_2026-10-12/README.md`, sections « OPA V11 » et
« OPA inabordable ».

## Synchronisation en ligne des pouvoirs — audit de source

Le contrôleur en ligne accepte `ultimate.use` pour le joueur dont c’est le tour
et exécute le pouvoir côté hôte. Le contrôle commun de pouvoir vérifie le
monopole et l’énergie ; les destinations/cibles sont revalidées par les méthodes
d’Aerospace et d’Armement. Le snapshot contient l’énergie, le solde, la position,
les améliorations et les états temporaires utiles (immunité, Turbo, bonus Oil &
Gas et sabotage), ce qui permet leur réplication par l’hôte. Deux clients WebGL
ont maintenant exécuté les ultimes Armement, Pharma, Tech, Immobilier, Luxe,
Oil et Automobile ; l’invité a comparé les deux jets Automobile, choisi le
meilleur et terminé son déplacement. Il a aussi activé Aerospace depuis le
sélecteur et choisi NASA Moonbase comme destination admissible. Le reçu
autoritaire confirme l’activation Aerospace, mais le filet `Planning` a lancé
les dés avant la capture de l’état immédiat du saut : son déplacement et son
feedback visuel restent à confirmer. Les autres snapshots confirment les
changements d’état et le coût en énergie ; les interfaces ont montré le
sélecteur, les annonces et les choix de jet. L’invité Pharma avait retrouvé la
partie après rechargement pendant l’immunité. Un essai de reconnexion sous
Automobile a aussi été brouillé par l’avancement automatique du tour ; il ne
constitue pas une preuve de reprise de l’effet avant son utilisation. Preuves :
`Docs/Validation/Online_Ultimate_Armement_2026-10-15.md`,
`Docs/Validation/Online_Ultimate_Pharma_2026-10-15.md`,
`Docs/Validation/Online_Ultimate_Tech_2026-10-15.md`,
`Docs/Validation/Online_Ultimate_RealEstate_2026-10-15.md`,
`Docs/Validation/Online_Ultimate_Luxe_2026-10-15.md`,
`Docs/Validation/Online_Ultimate_Oil_2026-10-15.md` et
`Docs/Validation/Online_Ultimate_Automobile_2026-10-15.md` et
`Docs/Validation/Online_Ultimate_Aerospace_2026-10-15.md`.

## Build et portée

La suite mécanique a été relancée sur l’état courant du projet via l’Éditeur
Unity batch le 15 octobre : `MECHANICS REGRESSION PASS: 123 assertions`, puis
`Exiting batchmode successfully now!`. Journal :
`/tmp/PolyRiskOnlineUltimateCoverage.log`. Ce rejeu confirme la logique des
huit pouvoirs, la sérialisation/restauration des effets et les deux refus de
cible déjà couverts. Il n’exécute pas `OnlineGameController.ApplyCommand` avec
un serveur et deux clients ; les preuves réseau restent celles des parcours
WebGL dédiés documentés séparément.

`EconomyRegression` : **114/114** ; `MechanicsRegression` : **117/117**.
Le rejeu frais du 15 octobre s’est terminé avec code 0 dans Unity 2022.3.62f3.
Dans le même lancement, Unity a journalisé `Build Finished, Result: Success`
et `WEBGL PLAYER BUILD PASS`. L’artefact Release isolé
`/tmp/PolyRiskReleaseQA_2026-10-15` fait environ 12 MiB ; son index, loader,
framework, WASM et données compressés répondent HTTP 200, et le client WebGL
ouvre le canvas dans le navigateur local. Il n’a pas remplacé la préversion et
ce contrôle d’accueil ne remplace pas un parcours multijoueur en ligne.

## Revalidation complète sur l’état courant — 15 octobre 2026

Un lancement Unity supplémentaire a rejoué `EconomyRegression` (**114/114**)
et `MechanicsRegression` (**123/123**), puis produit un build WebGL Release
avec `BuildOptions.None` : `Build Finished, Result: Success` et
`WEBGL PLAYER BUILD PASS`. L’artefact isolé
`/tmp/PolyRiskUltimateCoverageBuild` fait environ 12 MiB. La page s’est chargée
dans un navigateur WebGL ; les écrans d’accueil et de lobby à deux joueurs
étaient visibles. Journal complet :
`/tmp/PolyRiskUltimateCoverageBuild.log`. Le build n’a pas été publié.

Aucun montant, calcul de pouvoir ou règle OPA n’a changé. La notification de
déblocage est un feedback visuel. Une partie humaine à quatre est maintenant
confirmée par l’utilisateur, mais les appareils, la durée et les mécaniques
effectivement rencontrées restent à consigner ; elle ne permet donc pas encore
de conclure sur la compréhension humaine de chaque pouvoir ou de l’OPA.

Après l’essai réseau ciblé, quatre assertions Editor ont été ajoutées : elles
appellent le `CaptureSnapshot` de production après les ultimes, sérialisent le
résultat, corrompent les valeurs locales puis appliquent le snapshot avec le
`ApplyState` de production. Elles vérifient l’énergie, les soldes/positions, les
états Pharma/Automobile/Oil, les améliorations Tech et le sabotage Armement.
Le plateau de test comporte maintenant les 40 cases, pour garder cohérents les
indices réseau et les positions. Le rejeu frais termine sur
`MECHANICS REGRESSION PASS: 123 assertions`, code 0
(`/tmp/PolyRiskInvalidUltimateRegression_2026-10-15.log`). Deux assertions
supplémentaires vérifient qu’Aerospace refuse une destination non admissible
sans mouvement ni dépense, et qu’Armement refuse un actif Aerospace sans
sabotage ni dépense. Ces changements ne touchent que le banc Editor ; aucun
script Player ni la build WebGL Release n’a changé. Ils ne prouvent pas la
livraison entre navigateurs : les tests en ligne directs restent prouvés pour
Armement, Pharma et Tech ; les cinq autres ultimes et les refus en réseau
attendent encore une recette WebGL à deux clients. La preuve Tech est détaillée
dans `Docs/Validation/Online_Ultimate_Tech_2026-10-15.md`.

## Rejeu final de la suite mécanique — 15 octobre 2026

Unity 2022.3.62f3 a rejoué `MechanicsRegression.Run` sur le dépôt courant :
**126/126 assertions PASS**, batch terminé avec code 0. Le rejeu inclut les
refus Aerospace/Armement et la restauration par `ApplyState` des effets Pharma,
Automobile, Oil, Tech et Armement. Journal :
`/tmp/PolyRiskMechanicsContinuation.log`. Cette preuve confirme la sérialisation
et l’application locale d’un snapshot de production ; elle ne remplace pas la
recette de reconnexion effective entre deux clients WebGL.

## Revalidation après partie humaine — 15 octobre 2026

Après confirmation par l’utilisateur de sa partie à quatre amis, Unity
2022.3.62f3 a rejoué `MechanicsRegression.Run` : **128/128 assertions PASS**,
et `EconomyRegression.Run` : **114/114 PASS**, puis a terminé un build WebGL
Release avec `BuildOptions.None` : `Build Finished, Result: Success` et
`WEBGL PLAYER BUILD PASS`, code de sortie 0. Le lancement combiné a été exécuté
une seule fois le 15 octobre (`/tmp/PolyRiskPostFriendRelease.log`) et
l’artefact QA isolé `/tmp/PolyRiskPostFriendRelease` pèse environ 12 MiB ; il
n’a pas été publié et ne remplace pas la préversion partagée. Deux nouvelles
assertions invoquent le gestionnaire de commandes autoritaire de production :
une commande Aerospace vers une case non admissible et une commande Armement
contre une propriété Aerospace sont toutes deux rejetées sans dépense, effet,
mouvement ou activité. Ces contrôles exécutent le vrai gestionnaire et ses règles
dans Unity Editor, mais sans
transport HTTP entre deux clients ; la livraison réseau de ces refus reste à
confirmer en recette WebGL.

## Rejeu ciblé des refus — 15 octobre 2026

Un lancement séparé de Unity batch a rejoué `MechanicsRegression.Run` sur le
dépôt courant : **128/128 assertions PASS**, code 0. Le log
`/tmp/PolyRiskPowerMechanicsQA.log` confirme de nouveau le passage des deux
assertions d’`ApplyCommand` pour les refus Aerospace et Armement. Unity a signalé
au démarrage l’absence de jeton ULF, mais la suite a tout de même démarré et
terminé avec succès. Cette revalidation reste locale à Unity Editor ; aucun
refus n’a été envoyé depuis un client WebGL pendant ce contrôle.
