# Glissade prolongée sous plafond bas

**Objectif** : tant que le joueur n'a pas la place de se relever (plafond bas au-dessus de lui), il continue de « slider » à l'infini jusqu'à retrouver la hauteur nécessaire pour se redresser.

## Changements (`scripts/Entities/Player/Player.cs`)

- Nouveau helper `PeutSeRelever()` : requête de forme physique directe (`IntersectShape`) qui teste la capsule `CollisionDebout` (plus haute que la hitbox de glissade) à la position courante contre le terrain (`Constantes.LayerTerrain`). Renvoie `false` si un plafond chevauche la capsule debout. Requête de forme plutôt qu'un `TestMove` car la capsule debout est désactivée pendant la glissade.
- Fin de glissade conditionnée : `if (_slideTimer <= 0f && PeutSeRelever()) FinirGlissade(true);`. Le minuteur reste négatif frame après frame, donc tant que la place manque la glissade se prolonge (vitesse maintenue), sans figer le joueur ni le faire réapparaître à moitié dans le plafond.

## Vérification

- Compilation .NET : OK.
- Run headless 200 frames : aucune erreur liée au joueur (seules les erreurs préexistantes de l'écran paramètres sous display server headless subsistent).
