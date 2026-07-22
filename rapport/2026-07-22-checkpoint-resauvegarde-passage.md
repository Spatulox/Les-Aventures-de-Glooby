# Campement de pêche : resauvegarder à chaque passage

## Besoin
Le jeu devait resauvegarder **à chaque fois que le joueur passe sur un point de
sauvegarde**, y compris si ce campement est déjà l'actif.

## Diagnostic
- Build à jour (pas un problème de DLL obsolète).
- Le code appelait déjà `Sauvegarder()` sans garde à chaque déclenchement.
- Vrai problème : détection par **événement de bord `BodyEntered`**, qui ne se
  déclenche qu'à l'entrée exacte dans la boîte 48×32 — raté après un respawn (le joueur
  réapparaît *sur* le campement) ou en longeant le campement.

## Changement
`scripts/Entities/Misc/Checkpoint.cs` — abandon de `BodyEntered` au profit d'un
**sondage de position par frame + hystérésis**, en miroir de `CameraZone` :
- `PreparerDeclencheur()` retourne désormais `false` (ne câble plus `BodyEntered`).
- Suppression de l'override `SurEntreeJoueur`.
- Nouveau `_PhysicsProcess` : récupère le joueur via le groupe `"joueur"`
  (`GetFirstNodeInGroup`), teste `Contient(joueur.GlobalPosition)`, et sur le **front
  montant** uniquement appelle `DeclencherSauvegarde()` ; `_joueurDansZone` ré-arme à la
  sortie (une seule sauvegarde par passage, pas de spam disque).
- `DeclencherSauvegarde()` : active le campement si besoin, puis `Sauvegarder()`.

Aucune modification de scène (le `CollisionShape2D` reste, réutilisé par `Contient` ;
aucune scène ne se connecte au signal `JoueurEntre` du checkpoint).

## Vérification
- `godot --headless --build-solutions --quit` → build OK, 0 erreur.
- `godot --headless scenes/niveaux/monde.tscn --quit-after 200` → aucune erreur de
  script/runtime.
- Play-test manuel recommandé pour confirmer une écriture à chaque passage
  (entrer → sortir → revenir).
