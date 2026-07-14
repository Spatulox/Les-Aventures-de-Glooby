# Panneau : fusion PanneauBavard → PanneauBois (Talkative)

## Contexte
Il existait **deux systèmes de panneau redondants** : `PanneauBois` (vrai sprite en bois, texte gravé sur la planche via un `Label`, mais non parlant) et `PanneauBavard` (placeholder `ColorRect`, texte dans une **bulle externe** via `Talkative`). Le village utilisait le second. Objectif : un seul système — le beau `PanneauBois` devient parlant, `PanneauBavard` disparaît.

## Changements
- **`scripts/Entities/PanneauBois.cs`** — implémente désormais `Talkative`. Garde son `Texte` gravé sur le bois **et** gagne le volet bavard (patron repris de l'ex-`PanneauBavard`) : `[Export]` `Lignes`, `AncrageBulle`, `Aleatoire`, `AuPassage`, `UneSeuleFois`, `IdDialogue`, + les membres `Talkative` (`Dialogue`/`PointBulle`/`DeclencheAuPassage`/`PeutParler`/`SurFinDialogue` avec verrou `GameState` pour l'usage unique).
- **`scenes/props/PanneauBois.tscn`** — ajout d'un enfant `DeclencheurDialogue` (Area2D + `CollisionShape2D` 96×120) qui crée la bulle au-dessus du panneau ; ajout d'un `uid://` à la scène.
- **`scenes/niveaux/monde.tscn`** (édits chirurgicaux) — l'`ext_resource` du panneau pointe vers `PanneauBois.tscn` ; le nœud du village `PanneauAccueil` (PanneauBavard) devient **`panneau_poteau`** (`PanneauBois`, type Poteau), `scale = 0.5` (48×64 px au lieu de 96×128, trop gros), `position.y = 268` pour poser la base du poteau exactement sur le sol (surface à y=300), `Texte = "Village des pingouins"` gravé + `Lignes = ["Bienvenue au village des pingouins !"]` lues dans la bulle à la touche d'action.

### Ajustements taille / position / lecture
- Sprite forcé ×2 en code → instance mise à `scale 0.5` (panneau trop gros).
- Sprite centré sur l'origine : base = origine + demi-hauteur ; `position.y` calé sur `sol − demi-hauteur` (300 − 32 = 268) pour ne plus s'enfoncer dans le sol.
- **`scenes/props/PanneauBois.tscn`** — la zone de proximité du `DeclencheurDialogue` était posée en `(0,-24)` (au-dessus de l'origine, correct pour l'ancien `PanneauBavard` ancré à sa base, mais bien trop haut pour le poteau centré) → repositionnée en `(0,40)` et agrandie à `160×140` pour englober le joueur au pied du panneau ; sans ça la touche d'action ne déclenchait rien.
- **Suppressions** : `scripts/Entities/Interactable/PanneauBavard.cs`, `scenes/interactifs/panneau_bavard.tscn`, `scenes/test/test_dialogue.tscn` (scène de test jetable qui le référençait). Commentaire d'exemple de `PnjAmical.cs` recablé sur `PanneauBois`.

## Réutilisation
`Talkative`, `DeclencheurDialogue` et `BulleDialogue` réutilisés **sans modification** : `PanneauBois` est simplement un nouveau `Talkative`, comme `PnjAmical`.

## Vérification
- `godot --headless --build-solutions --quit` → **compile sans erreur** (aucune référence résiduelle à `PanneauBavard`).
- `godot --headless scenes/niveaux/monde.tscn --quit-after 200` → `monde.tscn` charge, `panneau_poteau` instancié sans erreur ni warning « aucun Talkative ».
- Play-test manuel recommandé pour le rendu de la bulle / lisibilité de la gravure.
