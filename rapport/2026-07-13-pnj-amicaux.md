# PNJ amicaux : Pingouin & Lutin du Père Noël

Ajout de deux types de PNJ amicaux réutilisables, calqués sur les patterns Joueur/Boss,
avec placeholders et comportement de déambulation.

## Changements

- **`scripts/Entities/Pnj/PnjAmical.cs`** (nouveau) — base abstraite `: LivingEntity,
  FriendlyLivingEntity`. Insensible à tous les dégâts (court-circuit `Degats.Infliger`).
  Déambulation simple : va-et-vient horizontal autour du point de départ
  (`DistancePatrouille`/`VitessePatrouille`/`TempsPause` en `[Export]`), gravité via le
  helper `AppliquerGravite`, `Sprite.FlipH` selon la direction. Le pipeline d'animation
  (passage à un `AnimatedSprite2D` + `ConstruireAnimations`) est **écrit en commentaire +
  TODO**, à activer quand les vraies frames existeront.
- **`scripts/Entities/Pnj/Pingouin.cs`** / **`LutinNoel.cs`** (nouveaux) — sous-classes
  fines portant l'identité du type + le bloc `ConstruireAnimations()` **commenté**
  (dossiers futurs `res://assets/pnj/{pingouin,lutin_noel}/{idle,marche}`).
- **`scenes/entites/pingouin.tscn`** / **`lutin_noel.tscn`** (nouveaux) — `CharacterBody2D`
  + `Sprite2D` (carré placeholder) + `CollisionShape2D` capsule. Pingouin = gabarit joueur
  (r8/h32), lutin plus petit (r6/h22). `collision_layer=4` / `collision_mask=16` : reposent
  sur le sol one-way (layer 5) **sans bloquer le joueur** (hors de son masque 17).
- **Placeholders** (`assets/pnj/pingouin/placeholder.png`, `assets/pnj/lutin_noel/placeholder.png`)
  — simples carrés de couleur unie générés en local (PIL) : **noir 32×32** (pingouin),
  **vert 24×24** (lutin). Pas de PixelLab (budget préservé) — stubs à remplacer.
- **`scenes/niveaux/monde.tscn`** (Edits chirurgicaux) — 2 `ext_resource` ajoutés ;
  2 pingouins sous `Village/Decor` (x≈250, 720), 2 lutins sous `Grotte/Decor` (x≈3100, 4600).

## Vérification

- `godot --headless --import` : `.import` des 2 carrés générés — OK.
- `godot --headless --build-solutions --quit` : compilation propre — OK.
- `godot --headless scenes/niveaux/monde.tscn --quit-after 200` : boot sans erreur, PNJ
  chargés (Sprite2D résolu, physique active) — OK.
- F5 manuel recommandé pour le rendu (déambulation, gabarits relatifs).

## Dialogue : PNJ bavards au cas par cas (interface `Talkative` existante)

Réutilisation du système de dialogue déjà en place (`Talkative` + `DeclencheurDialogue` +
`BulleDialogue`, patron `PanneauBavard`) plutôt que d'en créer un nouveau.

- **`PnjAmical`** implémente désormais l'interface existante **`Talkative`** (comme
  `PanneauBavard`) : champs `[Export]` `Lignes` / `AncrageBulle` / `AuPassage` /
  `UneSeuleFois` / `IdDialogue`, plus `Dialogue`, `PointBulle`, `PeutParler()`,
  `SurDebutDialogue`/`SurFinDialogue`. Le PNJ **s'immobilise** pendant la conversation
  (`_enConversation`) et reprend sa déambulation à la fin.
- La capacité est portée par la **classe** (tous les PNJ sont `Talkative`), mais c'est
  **l'instance** qui décide : un PNJ est bavard uniquement si on lui renseigne des `Lignes`
  dans `monde.tscn`. Sinon il reste muet (le `DeclencheurDialogue` ne se déclenche pas si
  `Lignes` est vide). C'est le patron « Option A », calqué sur `Damageable`.
- **`pingouin.tscn` / `lutin_noel.tscn`** : ajout d'un enfant `DeclencheurDialogue` (Area2D
  + zone de proximité) — comme `panneau_bavard.tscn` — pour que toute instance puisse
  parler dès qu'on remplit ses `Lignes`.
- **`monde.tscn`** : un pingouin (village) et un lutin (grotte) rendus bavards via `Lignes` ;
  les autres restent muets.

## Hors périmètre
- Vraies animations / pixel-art PixelLab (code d'anim laissé commenté).
- Déclencheur/UI de dialogue : **déjà fournis** par l'existant (réutilisés, non recréés).
