# Protection réseau du Dark Board — 15 octobre 2026

## Comportement validé

Le service conserve le snapshot exact publié par l’hôte, mais `game/state/get`
projette les soldes des autres joueurs pour chaque invité. Le siège local garde
son montant exact afin que son interface reste correcte ; les autres sièges
reçoivent un montant représentatif de leur tranche : LOW `0`, MID `10 × Sgo`,
HIGH `30 × Sgo`, MOGUL `51 × Sgo`. Ces représentants sont cohérents avec les
seuils stricts du helper Unity.

Seul l’hôte actif peut publier l’état complet et le serveur ne le transmet
intégralement qu’à cet hôte. Lors d’un transfert, le nouvel hôte reçoit l’état
conservé avec les montants exacts avant de reprendre la publication.

## Validation

- Build `dotnet build ... -c Release` : **0 avertissement, 0 erreur**.
- Test six sièges après redémarrage du service : projections pour chaque siège,
  frontières à `5`, `20` et `50 × Sgo`, hôte complet et récupération complète
  après transfert d’hôte : **PASS**.
- Test de persistance du lobby et de l’état après redémarrage : **PASS**.
- Smoke test isolé lobby/protocole, six joueurs et publication/récupération
  d’état : **PASS**.

## Limite et préversion

L’hôte exécute encore le moteur autoritaire sur son client ; un hôte peut donc
inspecter les soldes ou falsifier ses propres calculs. La correction protège
les invités de l’inspection directe du snapshot, pas d’un hôte malveillant.

Le serveur Release corrigé a été redémarré sur le service de préversion sans
perdre de salon actif. Sur la préversion HTTPS, la page, le loader Unity, le
framework, les données compressées et le WASM répondent tous HTTP 200. Un test
à deux sièges par la même origine HTTPS a créé/rejoint/démarré un lobby, publié
un snapshot de contrôle et confirmé la projection host/invité, puis abandonné
le lobby QA.

Lien temporaire actuel :
https://cooper-justin-seven-devel.trycloudflare.com/ . Il dépend du Mac hôte et
peut expirer ou changer si le tunnel redémarre.
