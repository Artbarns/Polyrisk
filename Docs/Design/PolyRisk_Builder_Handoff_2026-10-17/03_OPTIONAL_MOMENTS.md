# Recommandations — non validées pour intégration

Ces trois idées ont une démonstration dans `previews/key-moments-optional.html` et un source dans `sources/key-moments-optional.fragment.html`. Elles prolongent la DA approuvée, mais ne doivent pas être activées dans le build sans nouveau choix d'Arthur.

## 1. Achat glissé — sensation d'engagement

Geste : saisir la poignée, tirer vers la droite puis relâcher dans la zone de confirmation (~90 %). Une course incomplète, annulation, perte de capture, changement de tour, fermeture du panneau ou propriété devenue indisponible remet la poignée au départ en ~220 ms. Aucune commande tant que l'engagement n'est pas confirmé.

À l'engagement : poignée verrouillée, « Confirmation… », puis reçu du jeu. Succès : cran final, coche et propriété acquise. Refus : motif lisible, retour à l'état réel, aucun débit ni complétion de famille simulés. Envoi unique, attente réseau non bloquante et contrôles contre les doubles commandes. Le prix doit rester visible pendant tout le geste.

Garder une alternative clic/tap, avec le même prix et la même confirmation, ainsi que le clavier. Ne pas confondre « clavier disponible » et « alternative sans drag disponible au tactile ». C'est une exigence de conception cohérente avec [W3C, Dragging Movements](https://www.w3.org/WAI/WCAG22/Understanding/dragging-movements.html). Le seuil et le geste sont des paramètres de prototype, pas une garantie d'accessibilité ou de prévention de toute erreur.

Le `input type=range` de la démo matérialise le principe, pas une recommandation technique pour Unity. Ne pas faire de slider une obligation sur toutes les actions : réserver ce geste à l'achat ou à un autre engagement significatif.

## 2. Famille complète — la dernière carte prend sa place

Déclencheur : acquisition confirmée qui fait effectivement passer possédées/total à une famille complète. La carte manquante se pose en 330 ms ; une ligne ambre relie brièvement la famille en 650 ms ; le compteur passe à complet et une mention concise apparaît. Afficher le bonus réel seulement s'il est défini par les règles.

Pas de confetti ni de popup bloquante. Une seule célébration par événement, pas de rejeu lors de snapshots/reconnexion. Mode réduit : carte, compteur et texte immédiatement corrects. Cette démo utilise 2/3 → 3/3 ; la production supporte les vraies tailles de familles.

## 3. Loyer reçu — comprendre où va l'argent

Déclencheur : crédit confirmé au joueur local. Montant près du capital, déplacement court vers sa position, mise à jour de la somme ; reçu conservé. Durée indicative 230–350 ms, annotation retirée après ~1,6 s. Pour un loyer partagé : montrer total débité, votre part et les autres bénéficiaires sans inventer de transaction.

Regrouper les effets si plusieurs transferts arrivent ensemble, en conservant leur détail dans le journal. Ne pas modifier le solde du modèle pour animer ; animer une valeur affichée qui finit exactement sur le solde confirmé. Mode réduit : solde final + libellé de crédit.

## Ordre recommandé

D'abord la base validée. Puis tester le slider avec des gestes réels et l'alternative tap. La complétion de famille est le meilleur moment rare à ponctuer ; le crédit de loyer est le plus utile à rendre compréhensible. Pas d'effet de réussite permanent pour une action ordinaire, pas de faux near-miss ni de pression pour acheter.

Repère UX : [Board Game Arena, D.2/D.3](https://en.doc.boardgamearena.com/images/5/57/Guidelines_UX_new_compressed.pdf) recommande de rendre acteur et conséquence lisibles et de garder les animations courtes, utiles à la compréhension. Ce document est une référence de principes, pas une banque d'assets.
