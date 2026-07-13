# Interface `Talkative` — système de dialogue PNJ / panneaux

Système d'interaction réutilisable pour faire parler PNJ, panneaux, etc. via une bulle
« banquise » au-dessus du model 2D. Rendu 100 % procédural (aucun asset généré).

## Fichiers ajoutés

- **`scripts/Common/Talkative.cs`** — l'interface (contrat). Membres : `Dialogue`
  (lignes), `PointBulle` (ancrage monde), `DeclencheAuPassage` (auto vs touche),
  `PeutParler()` (verrou usage unique...), hooks `SurDebutDialogue`/`SurFinDialogue`.
- **`scripts/Core/DeclencheurDialogue.cs`** (`: DeclencheurZone`) — le moteur réutilisable,
  par composition : posé en enfant du nœud parlant (ou ciblé via `Cible`). Réutilise le
  socle `DeclencheurZone` (hooks `PreparerDeclencheur` pour résoudre le `Talkative` + créer
  la bulle, `SurEntreeJoueur` pour l'entrée) ; ajoute la sortie (`BodyExited`), le rappel
  de touche, le défilement ligne par ligne à l'appui de `action`, et les deux modes de
  déclenchement. Aucune logique propre au PNJ (même esprit que `Boss`/`ZoneBoss`).
- **`scripts/UI/BulleDialogue.cs`** (`: Node2D`) — la bulle cartoon glace, dessinée en
  `_Draw` (StyleBoxFlat arrondi + queue triangulaire). **S'adapte à la longueur du
  texte** : `Font.GetMultilineStringSize` mesure le bloc (retour à la ligne au-delà de
  240 px), le fond épouse le texte. Le **texte est dessiné directement** en `_Draw`
  (`DrawMultilineString`, placement au pixel via les métriques de police) plutôt que via un
  `Label` : ça supprime le bug de centrage vertical du 1er affichage (layout paresseux d'un
  Control à sa première frame) — **centré dès la 1re ligne**. Un `_Process` **recadre la
  boîte dans la vue de la caméra** (via la transform du viewport, sans coupler `CameraZone`) ;
  la queue reste ancrée sur le PNJ. Deux modes : `AfficherDialogue` (bulle claire + queue)
  et `AfficherRappel` (étiquette foncée « touche », sans queue).
- **`scripts/Entities/Interactable/PanneauBavard.cs`** — exemple concret implémentant
  `Talkative` (lignes + ancrage + options exportés). Patron pour tout élément parlant.
- **`scenes/interactifs/panneau_bavard.tscn`** — le GameObject panneau (placeholder carré
  + `DeclencheurDialogue` enfant) à instancier dans un niveau.
- **`scenes/test/test_dialogue.tscn`** — scène de test (panneau + déclencheur) pour F5.

## Fichiers modifiés

- **`scripts/Core/GameState.cs`** — action `action` (Entrée + Espace) enregistrée dans
  `ConfigurerActionsParDefaut()` ; flag `DialogueDisponible` (à portée d'un parlant).
- **`scripts/Entities/Player/Player.cs`** — le saut n'est plus armé quand
  `DialogueDisponible` : à côté d'un PNJ, Espace **parle** au lieu de faire sauter
  (pas de saut parasite).
- **`scenes/niveaux/monde.tscn`** — un `PanneauAccueil` (instance de `panneau_bavard.tscn`)
  ajouté au Village sous `Village/Interactifs` (édition chirurgicale, décors intacts).
- **`scripts/Entities/Pnj/PnjAmical.cs`** — implémente désormais `Talkative` : tout PNJ
  (pingouin, lutin) peut parler si on lui renseigne des `Lignes` + un `DeclencheurDialogue`
  enfant ; il s'immobilise pendant la conversation.

## Utilisation

Implémenter `Talkative` sur un nœud (ou réutiliser `PanneauBavard`), puis lui ajouter un
`DeclencheurDialogue` enfant avec un `CollisionShape2D` (rayon de proximité). Régler les
lignes, `AncrageBulle`, `AuPassage`, `UneSeuleFois`/`IdDialogue`.

## Vérification

- `godot --headless --build-solutions` : compile sans erreur.
- Boot headless de `monde.tscn` : câblage OK (aucun warning « aucun Talkative » ; panneau
  + pingouins résolvent leur `Talkative`).
- Mesure/rendu bulle exercés (court, long avec retour à la ligne, rappel) : OK.
- Reste à faire : un vrai F5 pour le rendu visuel — vérifier le centrage de la 1re ligne et
  qu'une bulle en lisière de zone reste bien dans le cadre (queue toujours vers le PNJ).
