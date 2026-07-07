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

## Jalon C — Écrans banquise, Crevasse, Carrefour, Chemin du Pouvoir (terminé)

- Écran 02 (difficulté progressive), Crevasse (descente verticale en zigzag),
  chaîne complète Écran01 → Écran02 → Crevasse → Carrefour.
- Carrefour de Glace : mur de glace fondable visible dès l'entrée (verrou
  mémoire), descente vers le Chemin 2, paliers du Chemin 3 (impasse propre,
  voir DECISIONS.md).
- Chemin du Pouvoir : défi (stalactite-piège réutilisable + couloir),
  escalade en escalier, salle de récompense avec le pickup du Pouvoir de
  Chaleur, et un 2e mur fondable bloquant un raccourci retour vers le
  Carrefour - inutilisable au premier passage.
- Le Pouvoir de Chaleur est réellement utilisable en jeu (aura courte
  portée qui fait fondre les murs à proximité).
- 2 vrais bugs de conception attrapés en simulant des parcours automatiques
  (pas en relisant le code) : un mur fondable sautable par-dessus (collision
  invisible agrandie pour corriger), et des plateformes d'escalade empilées
  directement au-dessus d'un couloir (garde au plafond nulle - corrigé en
  décalant l'escalade en escalier, jamais deux niveaux à la même colonne).
- Compression assumée : 2 écrans banquise denses plutôt que 3-4 répétitifs
  (documenté dans DECISIONS.md), pour préserver le temps sur le combat.

## Jalon D — Conception du Boss Cerf (validée d'avance, non re-débattue)

Design imposé par la mission (personnage majestueux-mais-goofy, arène large,
2 phases, 3 patterns). Aucune génération nécessaire à cette étape : Rodolphe
existait déjà avec ses animations idle/patrouille/charge/étourdi/vaincu.

## Jalon E — Arène et combat complet (terminé)

- Machine à états complète : Intro, Idle, Télégraphe, Charge (esquivable en
  glissade, sonné 2s + x3 dégâts contre un mur), Piétinement (stalactites),
  Souffle de Givre (phase 2, cône procédural), transition de phase à 50% PV,
  Vaincu.
- Économie assumée : le piétinement et le souffle de givre réutilisent les
  animations idle/charge existantes plutôt que d'en générer de nouvelles -
  seul le résultat gameplay est nouveau. 0 génération pour tout le combat.
- Arène large (90 tuiles), fond cathédrale répété, 2 plateformes latérales,
  6 stalactites au plafond, barre de vie du boss, checkpoint juste avant
  l'arène.
- Écran de fin provisoire "Acte 2 terminé", relance en appuyant sur une touche.

## Bilan budget

**2 générations sur 60** (icône cœur du HUD). Tout le reste du Jalon C/D/E a
été construit par réutilisation pure des assets et animations déjà générés
lors des étapes de design précédentes.

## Bilan technique global

- Chaîne de jeu complète et vérifiée : Écran 01 → Écran 02 → Crevasse →
  Carrefour → (Chemin du Pouvoir en aller-retour) → Chemin 1 → Arène → Écran de fin.
- Chaque écran compile et se lance sans erreur (vérifié individuellement en
  headless après chaque tâche, pas seulement en fin de jalon).
- 4 vrais bugs attrapés et corrigés en simulant des parcours automatiques
  plutôt qu'en relisant le code : chute infinie sans filet de sécurité
  (Écran 01), plantage Godot en changeant de scène depuis un callback
  physique, mur fondable sautable par-dessus, plateformes d'escalade sans
  garde au plafond suffisante.
- Chemin critique dégâts → mort du boss → victoire → écran de fin vérifié
  explicitement par un test dédié (retiré après validation).

## Limites connues (voir TODO.md)

- Tuning des PV/dégâts du boss non validé par un vrai humain à la manette
  (impossible à playtester en headless) - à ajuster après un premier essai.
- Pas de mur fondable "avant la salle" côté Carrefour lui-même en plus de
  celui du Chemin du Pouvoir (un seul suffisait pour démontrer la mécanique
  sans dupliquer le même gimmick).
- Chemin 3 est une impasse volontairement vide (sur consigne explicite).
- Aucun test interactif réel (souris/clavier) n'a été possible dans cet
  environnement : toute la validation est headless + simulation d'entrées
  scriptées. Un passage manuel (F5) reste recommandé pour le ressenti final
  (vitesse, lisibilité des télégraphes, plaisir de jeu).
