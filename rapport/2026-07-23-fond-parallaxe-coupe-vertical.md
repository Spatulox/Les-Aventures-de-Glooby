# Fond parallaxe « le plus proche » — bord vertical coupé

## Problème
Dans une salle haute (surtout la **Grotte**, `ZoneGrotte` ~1416 px), quand la
caméra montait/descendait, les couches parallaxe mobiles glissaient
verticalement et laissaient voir leur bord haut/bas → image visiblement
« coupée ».

**Cause :** textures 360×180 affichées à ×2 = exactement 360 px (hauteur du
viewport) → **zéro marge verticale**, alors que les couches ont un
`scroll_scale.Y` non nul et ne se répètent pas verticalement.

## Choix retenu
Dérive verticale douce conservée + léger zoom (option validée par l'utilisateur).

## Changements
Dans `scenes/decors/DecorGrotte.tscn` et `scenes/decors/DecorBanquise.tscn`,
sur chaque couche parallaxe mobile :
- `Sprite2D.scale` : `(2, 2)` → **`(2.5, 2.5)`** → image 900×450, centrée à y=180
  → **45 px de marge en haut et en bas**.
- `Parallax2D.repeat_size` : `(720, 0)` → **`(900, 0)`** (tuilage horizontal sans
  couture à la nouvelle largeur).
- `scroll_scale.Y` fortement réduit pour que l'excursion verticale reste bien
  sous la marge de 45 px (une 1re passe à `~0.04` laissait encore dépasser le
  bord bas dans la grotte — excursion réelle biaisée d'un côté trop proche des
  45 px ; valeurs abaissées ~×2,5 pour une vraie marge de sécurité) :
  - Grotte : Lointain `0.05→0.006`, Intermediaire `0.1→0.01`, Proche `0.15→0.015`.
  - Banquise : TresLointain `0.02→0.005`, Lointain `0.06→0.008`,
    Intermediaire `0.12→0.012`, Proche `0.2→0.015`.
  Dérive verticale toujours présente mais très subtile ; bord jamais visible.
- `scroll_scale.X` (parallaxe horizontale) **inchangé**.

Aucun C#, aucun `monde.tscn`, aucun asset régénéré. Le fond lointain
(`Fond*`, `scroll_scale=(0,0)`) était déjà épinglé, non touché.

## Effets de bord assumés
- Léger zoom (×1.25) des décors parallaxe, y compris village/banquise (préventif).
- Échelle 2.5 non entière = légère irrégularité de pixels sur les couches
  lointaines (imperceptible ; le fond net reste en ×2).

## Vérification
- `godot --headless --quit-after 200` : aucun parse/load error sur les décors.
- **Playtest manuel recommandé** dans la Grotte (monter/descendre une salle
  haute) pour confirmer : plus de bord coupé, dérive verticale subtile présente,
  parallaxe horizontale sans couture.
