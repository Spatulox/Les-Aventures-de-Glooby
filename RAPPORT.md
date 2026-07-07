# Rapport de mission autonome — Un Pantalon Trop Grand

Mission : rendre le jeu jouable du départ jusqu'à la victoire contre le Boss Cerf.

## Jalon A — Contrôleur joueur (terminé)

- Idle, course, saut/chute (coyote time + jump buffer), glissade accélérée sur `is_ice`,
  lancer de boule de neige, dégâts : tous déjà en place depuis les étapes précédentes.
- Ajouté : invincibilité courte (1s, clignotement) après un coup, `GameState.Degats()`
  déclenche désormais le respawn au checkpoint quand les PV tombent à 0 (mort réelle,
  pas seulement la chute dans le vide).
- Générations PixelLab consommées : 0.
- Vérifié : compilation propre, headless sans erreur sur Écran 01 et le Carrefour.

## Jalon B — HUD, poissons, progression (terminé)

- HUD minimal en autoload (persiste à travers les scènes) : cœurs de vie + compteur
  de poissons. Un seul cœur généré, la variante "vide" est juste une teinte grisée
  appliquée en code (économie).
- Poisson ramassable avec état persistant (ne revient pas si déjà pris), 4 placés
  dans Écran 01. "Manger" (touche E) soigne, déjà relié à `GameState.ManagerPoisson`
  qui existait mais n'était câblé à aucune entrée jusqu'ici.
- Flag `GameState.PouvoirChaleurActif` posé pour le Jalon C.
- Générations : 1 (icône cœur). Cumul mission : 2/60.
- Bug rencontré et corrigé : collision de nom entre un signal Godot et une propriété
  (le signal génère un `event` C# du même nom que le délégué).
- Vérifié : compilation propre, headless sans erreur.
