# Pingouin idle dans le menu principal

## Objectif
Afficher le pingouin du joueur (animation *idle*, non contrôlable) à droite du menu
principal, et décaler la colonne du menu vers la gauche.

## Changements

- **`scripts/UI/MenuPrincipal.cs`**
  - Décalage de la colonne : la zone de centrage (`CenterContainer` parent de la
    colonne) voit son `AnchorRight` réduit à `0.55`, ce qui recentre le menu dans
    la moitié gauche de l'écran.
  - `AjouterPingouinIdle()` : monte un `AnimatedSprite2D` décoratif (pas
    d'instance de `player.tscn` — celle-ci embarque physique, `Camera2D` et
    abonnement `GameState`, indésirables dans un menu et rendrait le pingouin
    contrôlable). Ancré au centre-droit de l'écran, joue `idle`.
  - `ChargerFramesIdle()` : construit le `SpriteFrames` idle via le helper partagé.

- **`scripts/Common/AnimationsSprite.cs`** (nouveau) — helper réutilisable :
  `ChargerFrames(dossier)` (PNG triés → textures) et `EnregistrerAnimation(...)`
  (registre nom/fps/boucle, tranche optionnelle). Source unique du chargement
  d'animations à partir de dossiers.

- **Réutilisation** (suppression des duplications) :
  - `Player.ChargerAnimations` utilise désormais `AnimationsSprite` (ses copies
    privées `ChargerFrames`/`EnregistrerAnimation` supprimées).
  - `Boss.AjouterAnimation` devient une façade « depuis un dossier » au-dessus de
    `AnimationsSprite` (donc `BossCerf` et les futurs PNJ en bénéficient).

- **PNJ amicaux** — pipeline d'animation activé via `AnimationsSprite` :
  - `PnjAmical` : `ConstruireAnimations()` (abstrait) est désormais appelé dans
    `_Ready`. Si les frames `idle` existent, un `AnimatedSprite2D` est monté et le
    carré placeholder masqué ; sinon on garde le carré. `_PhysicsProcess` joue
    `idle`/`marche` et gère le `FlipH`. `AjouterAnimation` (façade dossier →
    `AnimationsSprite`) ajoutée ici aussi.
  - `Pingouin` / `LutinNoel` : `ConstruireAnimations()` décommenté, pointant vers
    `res://assets/pnj/<nom>/{idle,marche}` (encore vides → carré placeholder
    conservé, aucune régression).

## Vérification
- `godot --headless --build-solutions --quit` : build OK.
- Boot headless : menu chargé, aucune erreur liée au pingouin / aux animations
  (les `Not supported by this display server` viennent de `ToucheDe`, pré-existant).
