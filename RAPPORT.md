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
