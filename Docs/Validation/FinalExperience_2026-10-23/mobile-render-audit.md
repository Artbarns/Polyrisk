# Final Experience — audit rendu mobile

Audit local le 23 octobre 2026. Pas de mesure iPhone physique et pas de validation Safari iOS.

## Chaîne réellement configurée

- Unity 2022.3.62f3 / URP 14.0.12, WebGL2, espace couleur linéaire.
- Qualité WebGL par défaut : Low (index 1). iOS natif index 2 ne s'applique pas au navigateur Safari.
- Low et Medium : renderScale 1, HDR actif, MSAA 1 (désactivé). Low main directional par pixel sans ombre ; Medium ombres main + additional supportées, une cascade, distance 20.
- SampleScene caméra : post-processing actif, SMAA High (m_Antialiasing 2, quality 2). URP source PostProcessPass confirme SMAA sur les appareils non GLES2 ; ce choix coûte plusieurs passes, et ne signifie pas MSAA.
- GraphitePost : exposition +0,15 / contraste 6 / saturation −8 / ACES / bloom 0,08, seuil 1,15, scatter 0,2. Conservé.
- Catégories et états : texture 64×1 RGBAHalf, contact : 64×2 RGBAHalf ; Point / Clamp, sans mipmap, données linéaires. Aucun filtrage doux entre indices.
- GraphiteSurface utilise des coordonnées float pour position, dérivées et reconstruction ; couleurs/lobes half. Les dérivées fwidth garantissent une couverture minimale sans modifier la géométrie fusionnée. Pas de realtime probe, SSR, nouvelle lumière ou texture de matière.
- MinimumBandPixels effectif : 1,2 pixels du framebuffer (valeur thème), et non 0,95 (défaut shader). Contour transitoire ~1,1 pixels. À DPR2 : respectivement ~0,6 et ~0,55 pixels CSS, à ne pas confondre avec une largeur CSS garantie.

## Modifications template autorisées et réalisées

`Assets/WebGLTemplates/PolyRiskResponsive/index.html` : détection iPadOS demandant une UA desktop (MacIntel + plusieurs touch points), même budget framebuffer pour desktop haute densité et mobile, métadonnées non visibles sur le canvas. Elles exposent dimensions CSS, backing store réel, DPR configuré et DPR écran, actualisées par resize et observation des attributs width/height. Aucun polling, aucun accès au moteur ou aux données métier.

`Assets/WebGLTemplates/PolyRiskResponsive/TemplateData/style.css` : touch-action:none uniquement sur le canvas pour réserver les gestes d'exploration au jeu.

DPR choisi à l'ouverture : max(1,min(deviceDPR,2,sqrt(2400000/(viewportCSSWidth*viewportCSSHeight)))). Le plancher1 conserve au moins la définitionCSS sur très grands écrans ; le budget2,4M est donc indicatif au-delà2,4M pixelsCSS. La valeur configurée reste stable pendant une session (pas de rescale agressif au déplacement des barres Safari).

Exemples théoriques, pas mesures appareil :

| Viewport / DPR écran | DPR configuré | Pixels nominaux |
|---|---:|---:|
| 430×830 / 3 | 2 | 1 427 600 |
| 1024×1366 / 2 | 1,30988 | ~2 400 000 |
| 1200×900 / 2 | 1,49071 | ~2 400 000 |
| 1200×900 / 1 | 1 | 1 080 000 |
| 3840×2160 / 2 | 1 | 8 294 400 |

Les dimensions entières réelles dépendent de l'arrondi du moteur : lire dataset.backingWidth/Height dans le build. Changement Retina desktop : ancien framebuffer4,32M à1200×900 DPR2, nouveau~2,4M ; mobile430×830 inchangé.

## Rapport qualité/coût A/B existant

GraphiteSatin et ReflectiveNeon partagent le shader, les passes, les textures et les lumières. A réduit surtout les paramètres des reflets ; il ne supprime pas les calculs du contact. Pas de gain structurel démontré à retenir A.

PASS2F.1 : même session stabilisée Low430×830 Mac/IrisPlus Development, A54,19ms et B54,21ms, chacun76 drawcalls /67 batches /37 SetPass. Écart0,02ms non significatif, pas de tempsGPU séparé. Les autres sessions varient38,95–62,91ms même avec l'ancien binaire ; impossible d'attribuer ces fluctuations au shader. Le choix B reste cohérent pour le reflet de contact plus lisible, sous réserve des nouvelles captures rapprochées.

Ne pas augmenter bloom/exposition ou renderScale pour compenser une ligne trop fine. Examiner d'abord le nouveau cadrage1,4, qui augmente la largeur géométrique projetée. Si nécessaire, ajuster uniquement la couverture minimale ou les coefficients d'arêtes existants, après comparaison d'images natives. Aucun réglage artistique n'a été changé dans cet audit.

## Validations réellement exécutées ici

37 assertions Node sur le bloc JavaScript réel du template : six environnements UA/densité, calculDPR, reconnaissance iPadOS/mobile, métadonnées réelles, regroupement des notifications de resize et mutation, mise à jour backingwidth, règle CSStouch. Succès. Script et résultats : `/tmp/polyrisk-template-mobile-check.cjs` et `.json`.

Inspection statique des assets Unity, des shaders et des sources URP14 installées. Pas de build lancé par cet agent, pour éviter deux instances Unity sur le projet.

## À vérifier sur le build intégré

- Canvas DOM/backing au viewport430×830, puis1200×900.
- Low/Medium comparés même viewport/scène ; budgetCPU/GPU pas déduit des vidéos enregistrées.
- Tap/drag et annulationgestesUI, Safari iOS réel si accessible.
- BarresSafari, safearea et retour après veille/reconnexion sur téléphone.
- Temps de chargement, mémoire navigateur et stabilité pendant plusieurs tours ; countersUnity seuls ne couvrent pas tout leGPU/navigateur.

Pas de revendication60FPS ou validationiPhone sur la seule base des37 assertions et du précédentWebGLMac.
