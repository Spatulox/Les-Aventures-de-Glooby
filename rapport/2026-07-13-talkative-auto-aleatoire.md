# Talkative — bavardage automatique + affichage aléatoire

Extension du système de dialogue (`Talkative`) avec deux capacités, en réutilisant le
moteur `DeclencheurDialogue` existant. La bulle reste possédée par le moteur (pas de
double système).

## Fichiers ajoutés

- **`scripts/Common/TalkativeAutomatique.cs`** — nouvelle interface `: Talkative` pour un
  parlant dont la bulle **défile toute seule** (sans appui de touche). Membres :
  `IntervalleAuto` (délai entre deux lignes), hooks `Incrementer()` (avancée) et `Cacher()`
  (arrêt). Le moteur détecte ce type et pilote le défilement.
  L'interface s'implémente sur n'importe quel nœud parlant (comme `PanneauBavard`
  implémente `Talkative`) : exposer `IntervalleAuto`/`Aleatoire` en `[Export]`,
  `DeclencheAuPassage => true`, puis ajouter un `DeclencheurDialogue` enfant.

## Fichiers modifiés

- **`scripts/Common/Talkative.cs`** — ajout au contrat de `bool Aleatoire { get; }`
  (afficher UNE réplique au hasard au lieu de tout faire défiler).
- **`scripts/Entities/Interactable/PanneauBavard.cs`** et
  **`scripts/Entities/Pnj/PnjAmical.cs`** — exposent `[Export] public bool Aleatoire`
  (visible dans l'inspecteur Godot), satisfont la nouvelle interface.
- **`scripts/Entities/Pnj/Pingouin.cs`** et **`scripts/Entities/Pnj/LutinNoel.cs`** —
  deviennent `TalkativeAutomatique` : pingouins et lutins bavardent tout seuls au passage du
  joueur en piochant une réplique au hasard (`Aleatoire` forcé dans `Initialiser`,
  `IntervalleAuto` exporté). Câblage déjà présent dans `scenes/entites/pingouin.tscn` et
  `scenes/entites/lutin_noel.tscn` (`DeclencheurDialogue` enfant) — aucun changement de scène.
- **`scripts/Core/DeclencheurDialogue.cs`** — le moteur gère les deux modes :
  - **Aléatoire** : `DemarrerDialogue` tire l'index de départ au hasard ; en manuel une
    seule réplique puis fin, en automatique une nouvelle réplique au hasard à chaque tour.
  - **Automatique** : `_auto` (cast du `Talkative` en `TalkativeAutomatique`), minuteur
    `_minuteurAuto` dans `_Process` qui appelle `LigneSuivante()` toutes les `IntervalleAuto`
    s et **boucle** tant que le joueur reste ; `Incrementer()`/`Cacher()` notifiés ; ne
    touche pas `GameState.DialogueDisponible` (la touche de saut n'est pas détournée).
  - Un `TalkativeAutomatique` **démarre toujours au passage** du joueur (`SurEntreeJoueur`),
    quel que soit `AuPassage` — son défilement ne dépend pas de la touche.
  - Le mode manuel touche-par-touche existant est **inchangé**.

## Vérification

- `godot --headless --build-solutions --quit` : compile sans erreur (les 3 implémentations
  de `Talkative` fournissent bien `Aleatoire`).
- Boot headless : aucune erreur liée au dialogue (le `Not supported by this display server`
  vient du menu principal, artefact headless préexistant).
- Reste à faire : un vrai F5 — poser un nœud implémentant `TalkativeAutomatique` (2-3 `Lignes`)
  + `DeclencheurDialogue` et s'en approcher (défilement seul + boucle) ; cocher `Aleatoire`
  sur un `PanneauBavard` (une seule réplique au hasard par interaction).
