# Suppression des décors qui « volent » (hors grotte)

## Problème

Dans `scenes/niveaux/monde.tscn`, cinq props de décor utilisaient des textures de
**paroi/plafond de grotte** (`veine_cristal_mur.png`, `fissure_lumineuse.png`) mais
étaient posés en **plein ciel** au-dessus de la banquise et de l'arène du boss
(surface de marche ≈ y272, props à y110–140) → ils flottaient dans le vide.

## Changements

Édition chirurgicale de `scenes/niveaux/monde.tscn` — **5 nœuds `Sprite2D` retirés** :

- `Banquise/Decor` : `VeineMur1`, `Fissure1`, `Fissure2`
- `ZoneBossCerf/Decor` : `VeineMur2`, `Fissure3` (arène du boss = sol ouvert type
  banquise, traitée comme la banquise)

Les 2 `ext_resource` devenus morts ont aussi été supprimés (`g_veine`, `g_fissure`) —
`grep` confirme qu'ils n'étaient référencés que par ces 5 props (aucun usage dans
`Grotte`). Les PNG restent sur disque dans `assets/props/grotte/`.

## Non touché

- Le nœud `Grotte` (parois cave cohérentes) — exclu à la demande.
- Les autres props de `Banquise`/`ZoneBossCerf`, déjà ancrés au sol (y ≈ 252–286).
- Le `Village` (aucun décor volant).

## Vérification

- `godot --headless --build-solutions --quit` → build OK.
- `godot --headless --quit-after 200` → aucune erreur `ext_resource` manquante ni
  référence `veine`/`fissure` (seuls des warnings préexistants « Not supported by this
  display server » liés au headless, sans rapport avec ce changement).
