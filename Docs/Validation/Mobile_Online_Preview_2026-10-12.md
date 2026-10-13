# Préversion mobile en ligne — 12 octobre 2026

Lien actif de recette au 13 octobre (V35) : https://discussing-intimate-outdoors-exclusively.trycloudflare.com/?visual-v35

Build V35 depuis le 13 octobre, serveur statique et relais API sur 8900, API de recette 5091. L'ancien domaine `law-organizational-postage-calibration.trycloudflare.com` a expiré ; le nouveau lien a chargé le menu WebGL public et ses quatre ressources répondent 200.
Le smoke test `smoke_test.py` sur cette nouvelle origine HTTPS passe avec six joueurs (salon A33BXU, version 13, état de partie version 1). Ce test de protocole ne remplace pas une partie humaine sur six appareils.
Tunnel temporaire : le Mac et les processus doivent rester allumés. Le lien
mobile historique sur 8765 reste distinct.

Chaque joueur ouvre ce lien sur son propre appareil et choisit un pseudo unique.
Un joueur crée le salon et partage le code ; les autres le rejoignent. Tous
cliquent Ready, puis l’hôte Start Game. Jusqu’à six joueurs. Aucun réglage
d’adresse serveur nécessaire sur ce lien public : origine HTTPS automatique.

## Contrôles confirmés

- WebGL V11 : Economy 106, Mechanics 92 assertions Unity puis build PASS / sortie 0.
- Six clients WebGL : rejoindre/ready/démarrer, six lancers manuels, achats,
  loyer, passage direct Planning→Planning avec bouton actif.
- Rechargement des six clients à un point de décision : snapshot public complet
  strictement identique. Sixième client récupère automatiquement après timeout.
- Achat après reprise : un seul débit de 1000 $, séquence acquittée.
- Serveur HTTPS : smoke protocole à six joueurs PASS (QBHYM8).
- Navigateur public : chargement et création 4LV5EC sans saisir de serveur.
- Format portrait 430×900 : code, ligne joueur, boutons visibles. Ce contrôle
  navigateur ne remplace pas un essai sur Safari/iPhone réel.
- Précompression : principaux fichiers 64020488→18086975 octets, facteur 3,54.
  HEAD négociation gzip/identity/q=0, MIME wasm et SHA décompressé confirmés.

## Encore à vérifier / finir

Parties humaines, Safari réel, Marketplace V8 avec offres/fermeture, OPA,
pouvoirs de famille, liquidation et fin de partie en ligne, équilibrage final.
V9 corrige la hauteur du salon en paysage court : contrôle navigateur à 900×430,
sixième joueur accessible et bouton Quitter fonctionnel. Safari réel reste à confirmer.
Publication HTTPS V9 vérifiée (index identique, wasm 200 application/wasm gzip).
Les anciens fichiers V8 restent disponibles pour les chargements déjà commencés.

Retour utilisateur : premier essai sur iPhone 14 Pro Max fluide, boutons et panneaux
réactifs, y compris le widget Marketplace. Cet essai exploratoire ne constitue pas
une recette approfondie du nouveau multijoueur ; parties entre amis encore prévues.

Relancer le serveur statique avec --precompress pour préparer les copies gzip
d’un nouveau build, puis --api-upstream http://127.0.0.1:5091.


## Mise à jour du 13 octobre — V11

Même lien HTTPS ; API5091 mise à jour et smoke six joueurs9H545G PASS,
commande dice.turbo incluse. Test serveur restart/persistence PASS sur stockage
isolé ; stockage public avant remplacement vide (backup conservé).
Turbo : choix humain et reprise validés avec deux navigateurs V10, pénalité
500 une fois au cinquième double. V11 rendus430×900/900×430 : contexte de
choix explicite, statistiques masquées, aucune superposition debug/plein écran.
Build sans Development. Les anciens assets restent disponibles, index changé
en dernier et égal au V11 via HTTPS200. Recharger avant nouvel essai.
Cela ne clôt pas la recette des autres familles/OPA et des parties humaines.

V12 : revue marketplace restaurée sans panneau de case superposé, offre mixte
acceptée sur deux navigateurs, cash et titres conservés après reconnexion.
Rendus portrait430×900 et paysage900×430 contrôlés ; index public V12 vérifié.

## Mise à jour visuelle du 13 octobre — V20

Le template WebGL ne force plus DPR 1 sur mobile. Il utilise la densité de
l'écran, plafonnée à 2 et à 2,4 millions de pixels de rendu : sur un viewport
430×932 à DPR natif 3, le buffer cible est 860×1864, soit 1,60 million de
pixels. La cause du flou global a été identifiée dans le template. La netteté
et la cadence sur iPhone 14 Pro Max réel restent à mesurer ; le navigateur de
recette disponible ici fonctionne à DPR 1 et ne peut confirmer ce point.

V20 : Economy 106 + Mechanics 99 assertions PASS, build Unity WebGL PASS
(sortie 0), taille du dossier identique à V19 (12 292 KiB). En portrait
430×900, la sélection Marketplace cadre le plateau complet, la case possédée
est atteignable et le panneau se réouvre avec le titre choisi. Captures :
`Reconnect_2026-10-12/visual_board_fit_v20.png` et
`Reconnect_2026-10-12/visual_board_selection_v20.png`.

L'index public a été remplacé en dernier, est identique au build V20 et
répond en HTTPS 200. Le menu public a été chargé dans le navigateur de recette
(`Reconnect_2026-10-12/visual_public_home_v20.png`). Utiliser le paramètre
`?visual-v20` pour contourner un ancien index mis en cache. Le tunnel reste
temporaire et dépend du Mac allumé.

## Mise à jour du plateau — V21

Les teintes des propriétés suivent maintenant leur famille réelle ; la lumière
des cases au repos est abaissée. Build Unity205 assertions PASS. En recette
locale 430×900, la case sélectionnable reste clairement visible et le choix
réouvre le formulaire avec la propriété affichée. Captures et limites :
`UI_Motion_2026-10-13/README.md`.

Index public V21 identique au build et quatre ressources WebGL en HTTPS 200.
Le dernier contrôle de chargement dans le navigateur public a été interrompu
par l'arrêt de l'outil de navigateur ; la recette locale V21 a réussi. Utiliser
`?visual-v21` pour contourner un ancien index en cache. Fluidité et netteté
sur iPhone réel restent à confirmer.
