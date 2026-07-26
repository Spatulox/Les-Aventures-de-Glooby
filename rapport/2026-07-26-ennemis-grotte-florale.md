# Ennemis de la grotte florale (4 GameObjects réutilisables)

Les PNG de `assets/ennemis/grotte_florale/` n'étaient référencés nulle part : ni scène,
ni script. Créé les 4 ennemis correspondants, prêts à être glissés dans un niveau.

## 1. Scènes — `scenes/ennemis/grotte_florale/`

Sous-dossier **par biome**, comme `assets/ennemis/<biome>/` et `scenes/sol/<biome>/`
(les ennemis banquise restent à plat dans `scenes/ennemis/`).

Chaque `.tscn` porte la même structure que les ennemis existants : `AnimatedSprite2D`
(frames chargées au runtime), `Apercu` (1re frame idle, visibilité éditeur), collision de
corps, `ZoneDetection` (portée **réglable par instance**), zones d'attaque.

| Scène | Rôle | Réglages |
| --- | --- | --- |
| `GardienRonces.tscn` | patrouille, puis marche sur le joueur repéré ; blesse au contact ; meurt de ses PV | `PvMax=3`, `VitessePoursuite=55` (< joueur : semable), `ZoneContact` |
| `FleurCarnivore.tscn` | plante enracinée en embuscade : ouverture (télégraphe) → morsure → refermeture → recharge | `PvMax=2`, `CadenceMorsure=1.5s`, `ZoneMorsure` (r=26 sur la tête) |
| `BulbeExplosif.tscn` | piège à retardement : gonflement à l'approche **ou** sur boule de neige, puis explosion et disparition | jamais de PV perdus, `ZoneExplosion` r=52, `ZoneDetection` r=60 |
| `NueePollen.tscn` | **volant** : ondule autour de son point de départ, fond en diagonale sur le joueur, contact continu | `PvMax=1`, `VitesseVol=50`, pas de gravité |

Collisions calées sur la **bounding box alpha** de chaque sprite (base du visuel = bas de
la collision), pas sur la taille du PNG.

## 2. Scripts — `scripts/Entities/Ennemis/GrotteFlorale/`

`GardienRonces.cs`, `FleurCarnivore.cs`, `BulbeExplosif.cs`, `NueePollen.cs` — tous des
`PnjMechant`. Économies d'assets : la **fermeture** de la fleur réutilise les frames
d'ouverture jouées à l'envers (`AnimationsSprite.EnregistrerAnimation(inverse: true)`) ;
le bulbe n'a pas d'animation de mort — l'explosion *est* sa mort.

## 3. Mutualisé dans `PnjMechant` (plutôt que dupliqué 4×)

- **`Mourir()` générique** : joue `mort` si la scène en a les frames, coupe physique +
  collisions, estompe puis libère. Tout méchant en profite désormais.
- `SubitGravite` (virtuel) → les volants pilotent leur `velocite.Y`.
- `MettreAJourAnimation` (virtuel) → les méchants à machine à états gardent la main sur
  leurs animations (le défaut idle/marche est inchangé).
- `JouerSiPresente` (remonté depuis `BonhommeDeNeige`), `BlesserJoueur`,
  `BlesserJoueursDansZone`, `DesactiverCollisions`.
- **`[Export] ContactContinu`** : blesse à chaque frame de chevauchement au lieu du seul
  `BodyEntered` (les i-frames du joueur espacent les coups). Activé sur le gardien et la
  nuée, qui restent collés à leur cible. `false` par défaut → ours et bonhomme inchangés.

## 4. Divers

- `scenes/test/TestEnnemisGrotteFlorale.tscn` — sol de grotte + les 4 ennemis + joueur,
  pour un F5 manuel.
- `CLAUDE.md` : nouvelle puce `scenes/ennemis/` (la section listait tous les dossiers
  de `scenes/` sauf celui-là).

## Vérification

- `godot --headless --build-solutions --quit` → build OK.
- Run headless de la scène de test avec une sonde temporaire (téléportation du joueur
  devant chaque ennemi, supprimée depuis) : poursuite + dégâts du gardien puis mort en
  2 boules de neige avec animation `mort` et libération du nœud ; cycle
  ouverture → morsure (−1 PV) → fermeture → recharge de la fleur ; bulbe
  gonflement → explosion (−1 PV) → libération ; nuée qui rejoint le joueur et le grignote
  ~1 PV/s au contact, puis retourne à son ondulation. Aucune erreur.
- Reste à faire : F5 manuel pour le ressenti (lisibilité des télégraphes, portées) et
  placement des instances dans un niveau.
