# Banc de calibration Graphite

## Dans Unity

Unity2022.3.62f3 : ouvrir SampleScene, entrer en Play Mode et démarrer une partie locale isolée. Ouvrir **PolyRisk → Art → Graphite Calibration**. La fenêtre sélectionne le thème réutilisable et propose A/B, cinq couches, les spécimens et les trajectoires. Une décision de propriété en attente donne un état stable ; éviter de mesurer pendant un mouvement ou le délai de planning.

- **A** : traitement ReflectiveNeon de la baseline9242725, avec ses coefficients, éclairage plat, orientation de la key et bloom précédents.
- **B** : nouvelle architecture. Même scène, caméra×1,40 et palette fonctionnelle. La correction de visibilité des identités s'applique aux deux versions.
- **0 Neutre** : matériau sans studio ni sources ni reflets ajoutés.
- **1 Architecture** : lumière principale/remplissage orienté, accroches et studio, sans néon.
- **2 Sources** : sources nettes, sans réflexion de voisinage.
- **3 Réponse** : réflexion locale et empreinte du pion, bloom nul.
- **4 Optique retenue** : proposition complète retenue sans bloom ; exposition+.15, tone mapping Neutral et SMAA conservés. Le bloom.045 testé n’apportait rien au repos pour un coût de16drawcalls/16SetPass sur cette configuration.

Les couches0–3 créent une petite copie de profil uniquement à la commande de calibration ; elles n'altèrent pas le ScriptableObject. Aucun clone de profil ou de matériau n'est créé chaque frame en jeu normal.

## Spécimens

Inactive, couleur, sélection, contact, arrivée, monolithe au repos/actif, bordure/chanfrein et reflet. Deux spécimens supplémentaires exercent les coins visibles. Aucun recadrage ni agrandissement de pion n'est introduit pour les rendre avantageux.

Les couleurs de propriété sont des **échantillons de présentation**, sans attribution de propriétaire dans le modèle. Les pics de contact/arrivée sont figés pour comparer exactement les shaders ; ils ne démontrent pas la temporalité. **Restaurer la présentation avant de jouer.** Ces fixtures sont refusées en multijoueur ; la comparaison A/B et les contrôles de lecture restent disponibles.

Les trajectoires courtes/longues/coin utilisent GetPath et PawnMovement, avec un callback QA compté et une restauration du seul pion après1,2s de retour au calme. Elles ne changent pas sa position logique, ne tirent pas les dés et ne résolvent aucune conséquence économique. Une vidéo de lancer naturel complète ces preuves de banc. Les éventuelles coupes de fin retirent uniquement la restauration QA ; aucune vitesse n'est modifiée.

## Paramètres

L'asset **Assets/Settings/GraphiteFoundation/GraphiteTheme.asset** regroupe ArchitecturalGraphite et ArchitecturalLight. Modifier le thème puis cliquer B pour réappliquer. Le profil ReflectiveNeon précédent reste la référence ; ne pas le retoucher pour rendre un comparatif plus favorable.

Dimensions/géométrie, règles, camera, pions et audio restent ceux de la release de référence. Les matériaux partagés sont réutilisés ; le corps du pion reçoit le shader GraphiteSurface sans changer son mesh, ses identités lumineuses ou son animation.

## WebGL

Construire par **PolyRisk → Validation → Build Art Calibration WebGL QA**, avec POLYRISK_WEBGL_OUTPUT renseigné. Puis lancer make_capture.py dans ce dossier pour générer les commandes et le recorder locaux. Le script n'est pas un élément de l'interface livrée. Les méthodes publiques du composant [QA] ArtCalibration correspondent aux boutons de la fenêtre Editor.

capture.html suit le DPR réel/plafonné du template. capture-dpr2.html force un framebuffer×2 pour un test explicite de netteté ; le dataset du canvas indique aussi le vrai deviceDPR. Sur ce Mac, CSS430×830 et backing860×1660 avec deviceDPR1 constituent un test de résolution, **pas une émulation du GPU ou du navigateur d'un iPhone**.

## Mesures et garde-fous

RunChecks compare état économique, caméra, meshes, indices, colliders et limites des identités UI. Measure attend.5s puis mesure6s : moyenne/médiane/p95/max, draw calls/batches/SetPass, mémoire Unity et marqueur Main Thread si disponible. Ce marqueur inclut potentiellement des attentes ; ce n'est pas un profil CPU isolé. Aucun tempsGPU n'est revendiqué. Le champ unchangedDuringSample doit rester vrai ; sinon écarter la prise.

Attendre la fin des compilations et encodages, ne garder qu'un player Unity actif, comparer la même position et les mêmes dimensions. La valeur d'une seule prise ne prouve pas un gain statistique. Les compteurs mémoire Unity n'incluent pas tout le navigateur et le GPU.

Le Release est construit sans Development : aucune auto-initialisation du banc, aucun bouton QA ou recorder dans le paquet public. Le builder refuse d'écraser implicitement un export en l'absence du chemin d'environnement et conserve les octets des matériaux/pipelines. La recette finale restaure aussi les octets des matériaux après sortie de l'Editor.
