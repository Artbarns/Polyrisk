# Réduction des animations — Venture et cases du plateau — 16 octobre 2026

## Correctif

`VentureCardModalController.ShowCard` n’ajoute maintenant la classe d’entrée `card-flip-from` et sa coroutine de retrait que si « Réduire les animations » est désactivé. Le réglage activé affiche donc directement la carte, sans état initial réduit/roté pendant deux frames. Le contenu, le bouton de confirmation et l’application de l’effet ne changent pas.

Les classes de couleur positive/négative/chaos restent appliquées quel que soit le réglage : une préférence de mouvement réduit ne masque plus cette information sémantique.

Le pulse lumineux des cases sélectionnables suit aussi ce réglage : en mouvement réduit, la case garde un surlignage fixe de sélection et ne lance aucune coroutine de pulsation. Cela couvre le shader du plateau procédural comme le rendu legacy.

## Vérification

- Unity 2022.3.62f3 : Économie **114/114**, Mécaniques **140/140**, UI **196/196** (17 écrans/composants) ; **450 assertions** passent. Le contrôle UI vérifie aussi qu’une sélection de case garde un surlignage fixe en mouvement réduit et ne démarre pas de coroutine de pulse. La première tentative du contrôle utilisait un renderer non initialisé en Edit Mode ; son fixture assigne désormais explicitement le renderer.
- Build WebGL Release `BuildOptions.None` : `Build Finished, Result: Success` et `WEBGL PLAYER BUILD PASS`.
- Sortie isolée finale : `/tmp/PolyRiskReducedMotionFinal_20261016`. Les quatre fichiers player compressés totalisent **12 680 966 octets** (loader inclus), 2 295 octets de moins que le précédent build de contrôle.
- `git diff --check` passe.

Les transitions n’ont pas été observées en navigateur pour cette correction ciblée. La recette visuelle Venture/sélection de case sur navigateur et Safari/iPhone reste ouverte; ce build n’a pas été publié.
