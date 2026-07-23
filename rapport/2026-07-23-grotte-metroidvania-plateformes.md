# Grotte metroidvania + fiabilisation des plateformes

## Assets grotte déplacés
- `assets/decors/sol_grotte` → `assets/sol/grotte`, `assets/decors/mur_grotte` → `assets/mur/grotte` (ré-importés, nouveaux UID).
- `.tscn` liés créés : `scenes/sol/SolGrotte.tscn` (+ CentreB/C, EmboutGauche/Droit), `scenes/mur/MurGrotte*.tscn`, avec sprites *bakés* → **visibles dans l'éditeur**. Scripts `scripts/sol/SolGrotte(Ligne).cs`, `scripts/mur/MurGrotte(Colonne).cs`.

## Rework de la grotte (Concept A — galeries étagées + cheminée de feu)
- Sous-arbre `Grotte` de `monde.tscn` réécrit chirurgicalement (seuls le nœud `Grotte` + quelques `ext_resource` touchés ; village/banquise/boss inchangés).
- Descente **par vrais trous** (le joueur ne traverse pas les hitboxes) ; remontée par marches ≤ 70 px (apex de saut ≈ 73 px).
- Sortie *fire-gated* : cheminée fermée par `MurGlaceCheminee` (mur fondable) ; pouvoir de feu placé au fond.
- Pièces `SolGrotte`/`MurGrotte`/`PlateformeUnidirectionnelle` posées **individuellement et visibles** (plus de composeur runtime type `SolGrotteLigne`, qui restait invisible dans l'éditeur).
- Densité : bonhommes de neige, lutins (indices), checkpoint, décors `g_*`.

## Fiabilisation des plateformes réutilisables
- **PlateformeFixe** : passée `[Tool]`, le setter `Taille` reconstruit sprite + collision **dans l'éditeur ET en jeu** (fin du décalage PNG/hitbox éditeur ≠ jeu). Configs Petite/Moyenne/Grande.
- **PlateformeFragile** : ajout de la `CollisionShape2D` solide manquante (fin du warning « ce nœud n'a pas de forme »).
- **GlaceEmpilee** : racine `Sprite2D` → `StaticBody2D` + `Sprite2D` (z_index=2, passe **devant** les sols pour bloquer) + `CollisionShape2D`.
- **PlateformeMobile** : trajectoire réglée directement dans l'inspecteur — **slider `AngleDegres` 0→360°** + `Distance` (px), gizmo `[Tool]` dessinant la trajectoire en direct. Passée en **plateforme traversable one-way** comme `PlateformeUnidirectionnelle` : collision sur le layer traversable (`collision_layer = 16`, `collision_mask = 0`) + `one_way_collision = true` → on saute dedans par en dessous et on atterrit dessus, et on redescend en **bas + saut**.

## Vérification
- `godot --headless --build-solutions --quit` → 0 erreur.
- Boot headless `monde.tscn` (200 frames) → aucune erreur nouvelle (seul le warning bénin pré-existant des glyphes clavier menu en headless).
- Playtest manuel F5 recommandé pour le *game feel* (raccords de surfaces, portées de saut, atterrissage sur plateforme mobile).
