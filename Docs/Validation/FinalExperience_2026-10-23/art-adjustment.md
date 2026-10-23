# Ajustement artistique final — Reflective Neon

La capture réelle `close-before-art-portrait.png`, 430×830 DPR1, montre un socle/cases très sombres, les reliefs fins et le tracé lisible, mais des contours sous-échantillonnés. La matière doit gagner une réponse locale sans relever son albedo ou exposer tout le plateau.

Ajustement B uniquement, quatre valeurs dans `GraphiteTheme.asset` et son fallback `BoardVisualTheme.SurfaceFinish.ReflectiveNeon()` :

| Paramètre | Avant2F.1 | Final proposé |
|---|---:|---:|
| Smoothness support | 0,47 | 0,49 |
| Smoothness cases | 0,41 | 0,45 |
| EdgeSpecular | 0,040 | 0,052 |
| NeonEmission | 0,88 | 0,92 |

La légère réduction de rugosité resserre la réponse satinée existante ; elle ne change ni le metallic, ni le lobe de studio. Le gain d'arêtes privilégie la séparation des surfaces. L'émission ne gagne que4,55%, sans hausse de bloom. Valeurs A strictement conservées pour référence.

Aucun changement de couleurs, exposition, lumière, contact, pions, géométrie, couverture minimale des bandes, shader ou passe. Le coût structurel est identique ; absence de coût GPU supplémentaire exact non mesurée. Le résultat artistique doit être confirmé sur les images du nouveau binaire et en densité2 simulée si possible (une simulation de résolution ne valide pas Safari/iPhone).

Je n'ai pas augmenté `minimumBandPixels` global1,2 : cela affecterait aussi A. Si le rendu DPR2 révèle encore une perte de contour fonctionnel, un réglage B séparé serait préférable à une inflation globale du bloom ou des matériaux.

Vérification statique réalisée : changements limités à ces quatre coefficients dans chacun des deux fichiers ; bloc A et données contact identiques avant/après. Preuve `/tmp/polyrisk-final-art-scope.json` ; bytes avant conservés dans `/tmp/graphite-theme-before-final-art.asset` et `/tmp/board-visual-theme-before-final-art.cs`.
