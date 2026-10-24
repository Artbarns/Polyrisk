# Recette responsive mobile WebGL — 15 octobre 2026

## Résultat

Le build Unity WebGL Release courant a été ouvert dans Chromium 155 (Chrome) accéléré par Metal, avec émulation tactile iPhone et densité DPR 3. Il s’affiche en portrait et en paysage sans erreur JavaScript. Le canvas WebGL suit la taille CSS de l’écran et son buffer est plafonné à deux fois la densité CSS :

| Orientation | Viewport CSS | DPR | Canvas CSS | Buffer WebGL |
|---|---:|---:|---:|---:|
| Portrait | 430 × 932 | 3 | 430 × 932 | 860 × 1864 |
| Paysage | 932 × 430 | 3 | 932 × 430 | 1864 × 860 |

Le menu reste dans l’écran en portrait. En paysage court, les options basses sont accessibles par défilement vertical ; les actions principales restent visibles. Un tap tactile émulé sur « Play Local » a aussi fermé le menu et affiché le plateau portrait, le HUD, ainsi que les actions « Roll Dice » et « Pass ». Le rendu utilise ANGLE/Metal sur Intel Iris Plus. Le chargement et le contrôle d’orientation n’ont produit aucune erreur JavaScript.

## Captures

- [Portrait 430 × 932](release_portrait.png)
- [Paysage 932 × 430](release_landscape.png)
- [Partie locale après un tap émulé sur Play Local](release_local_game_portrait.png)

## Portée et suites

Il s’agit d’une émulation iPhone dans Chrome desktop, pas d’un test de Safari ou d’un appareil physique. Elle confirme le dimensionnement du canvas, la mise en page de l’accueil et l’entrée en partie locale par tap émulé, mais ne mesure pas la cadence soutenue, la chauffe, la mémoire, les gestes iOS, l’encoche ou la barre d’accueil. Elle ne valide pas non plus la disparition du clavier virtuel après la création/jointure d’un salon.

L’iPhone 14 Pro Max de l’utilisateur a déjà fait l’objet d’un essai exploratoire, et une partie entre quatre amis a été confirmée sans défaut rapporté. La recette approfondie sur appareils réels et la partie à six restent ouvertes.

Build utilisé : `/tmp/PolyRiskFinalCheckpoint_20261015` (Release Unity frais, non publié). Le serveur local de recette a été arrêté après le contrôle.

## Revalidation du Release courant — 16 octobre 2026

Le build `/tmp/PolyRiskLobbyNameWrapRelease_20261016` a été servi en local et
chargé dans Chromium headless avec User-Agent iPhone explicite, `deviceScaleFactor`
3 et événements tactiles. Le canvas CSS suit les deux viewports et le template
plafonne le buffer de rendu à DPR 2 : portrait 430×932 → 860×1864; paysage
932×430 → 1864×860. Le buffer WebGL actif rapporte les mêmes dimensions. Les deux
pages renvoient HTTP 200, sans erreur JavaScript.

Capture du menu du build courant en viewport 430×932 :
[`current_release_menu_430x932.png`](current_release_menu_430x932.png).

Cette passe ne mesure pas la cadence soutenue, la mémoire ou la chauffe et ne
remplace pas Safari sur iPhone physique. Le User-Agent iPhone est une émulation
dans Chromium desktop.
