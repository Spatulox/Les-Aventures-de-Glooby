# Interface `Talkative` — système de dialogue PNJ / panneaux

Système d'interaction réutilisable pour faire parler PNJ, panneaux, etc. via une bulle
« banquise » au-dessus du model 2D. Rendu 100 % procédural (aucun asset généré).

## Fichiers ajoutés

- **`scripts/Common/Talkative.cs`** — l'interface (contrat). Membres : `Dialogue`
  (lignes), `PointBulle` (ancrage monde), `DeclencheAuPassage` (auto vs touche),
  `PeutParler()` (verrou usage unique...), hooks `SurDebutDialogue`/`SurFinDialogue`.
- **`scripts/Core/DeclencheurDialogue.cs`** (`: Area2D`) — le moteur réutilisable, par
  composition : posé en enfant du nœud parlant (ou ciblé via `Cible`). Gère proximité,
  rappel de touche, défilement ligne par ligne à l'appui de `action`, et les deux modes
  de déclenchement. Aucune logique propre au PNJ (même esprit que `Boss`/`ZoneBoss`).
- **`scripts/UI/BulleDialogue.cs`** (`: Node2D`) — la bulle cartoon glace, dessinée en
  `_Draw` (StyleBoxFlat arrondi + queue triangulaire). **S'adapte à la longueur du
  texte** : `Font.GetMultilineStringSize` mesure le bloc (retour à la ligne au-delà de
  240 px), le fond épouse le texte. Deux modes : `AfficherDialogue` (bulle claire +
  queue) et `AfficherRappel` (étiquette foncée « touche », sans queue).
- **`scripts/Entities/Interactable/PanneauBavard.cs`** — exemple concret implémentant
  `Talkative` (lignes + ancrage + options exportés). Patron pour tout PNJ parlant.
- **`scenes/test/test_dialogue.tscn`** — scène de test (panneau + déclencheur) pour F5.

## Fichiers modifiés

- **`scripts/Core/GameState.cs`** — action `action` (Entrée + Espace) enregistrée dans
  `ConfigurerActionsParDefaut()` ; flag `DialogueDisponible` (à portée d'un parlant).
- **`scripts/Entities/Player/Player.cs`** — le saut n'est plus armé quand
  `DialogueDisponible` : à côté d'un PNJ, Espace **parle** au lieu de faire sauter
  (pas de saut parasite).

## Utilisation

Implémenter `Talkative` sur un nœud (ou réutiliser `PanneauBavard`), puis lui ajouter un
`DeclencheurDialogue` enfant avec un `CollisionShape2D` (rayon de proximité). Régler les
lignes, `AncrageBulle`, `AuPassage`, `UneSeuleFois`/`IdDialogue`.

## Vérification

- `godot --headless --build-solutions` : compile sans erreur.
- Boot headless + `test_dialogue.tscn` headless : câblage OK (aucun warning « Talkative »).
- Mesure/rendu bulle exercés (court, long avec retour à la ligne, rappel) : OK.
- Reste à faire : un vrai F5 pour le rendu visuel de la bulle (headless ne dessine pas).
