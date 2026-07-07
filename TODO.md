# TODO / placeholders connus

- Tuning des PV/dégâts du Boss Cerf non validé manuellement (voir DECISIONS.md).
- Chemin 3 (Carrefour) est une impasse vide sur consigne explicite - contenu
  de fin (Rencontre ou Trésor) toujours en attente si un jour souhaité.
- Piétinement et Souffle de Givre du boss réutilisent les animations
  idle/charge existantes (économie de budget) - une passe de polish
  visuel dédiée serait un plus mais n'est pas nécessaire au jeu.
- Pas de vrai playtest manuel effectué (environnement headless uniquement) -
  recommandé de passer la main en F5 pour valider le ressenti.
- `goutte_figee` (lot de 20 décors de grotte) a un contour trop pâle (2.9%
  de pixels sombres, contre 5%+ pour tout le reste du lot) - pas rangée
  dans assets/props/grotte/, en attente d'une décision (corriger ou
  laisser de côté). Les 19 autres pièces du lot sont bonnes.
- Les 20 décors de grotte ne sont pas encore intégrés dans une salle -
  générés mais pas placés dans Monde.cs/les Salle*.cs.
