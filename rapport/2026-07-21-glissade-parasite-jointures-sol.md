# Glissade parasite et blocages aux jointures de sol — 2026-07-21

## Symptômes

1. En marchant vers le village, le joueur partait en glissade/dash **vers l'extérieur**, sans rien
   demander.
2. Il restait aussi parfois **bloqué**, « comme un demi-millimètre de différence de hauteur entre
   deux plateformes », en rentrant vers le village.

## Cause du 1. — glissade parasite (corrigé, commit `cc33019`)

Pas `faf0d45` (no-clip) mais **`9c122db`** : `GererPenteRaide` forçait la glissade dès qu'**une
seule frame** rapportait `GetFloorAngle() >= 35°`. Le `floor_max_angle` du joueur vaut 45°, donc
toute normale entre 35° et 45° passait pour une pente raide — y compris une normale de **coin** de
dalle. Aggravants : cooldown ignoré, aucun filtre de bruit, `_directionRegard` écrasé puis
verrouillé pour toute la glissade. Aucune `PenteBanquiseForte*` n'étant instanciée dans
`monde.tscn`, 100 % des déclenchements étaient des faux positifs.

Reproduction sur le code d'origine : à la jointure `Village/Sol3` ↔ `Village/Sol4` (x ≈ 646, en
plein village), le joueur marchant vers l'ouest est projeté de x=652 à x=828 **vers l'est**.

Correctif retenu (`Player.cs`) : le déclencheur est le **collider** sous les pieds
(`EstSurPenteForte()`, rayon court, masque `Constantes.LayerTerrain`) et non l'angle ; seule une
vraie `PenteBanquise` de type `Forte*` impose la glissade, après un délai de confirmation
(`DelaiConfirmationPenteRaide`). `AnglePenteMaxDegres` n'est plus qu'un second garde-fou.
`FloorBlockOnWall = false` en complément.

## Cause du 2. — micro-marches aux jointures

Le sol est un pavage de pièces distinctes (dalles, embouts, pentes) dont les dessus ne tombent pas
au pixel près : il subsiste des ressauts de quelques pixels aux jointures (2,5 px à l'embout gauche
du village, 4 px au pied est de `RampeEst`). Le bas arrondi de la capsule accroche l'arête et le
joueur se bloque net, sans que rien ne se voie à l'écran.

**Choix retenu : corriger côté joueur, pas dans la géométrie** — le niveau reste tel qu'il est
dessiné, et n'importe quelle jointure future est absorbée automatiquement.

## Correctif — `Player.GererMarcheAutomatique` (`scripts/Entities/Player/Player.cs`)

Appelée juste avant `MoveAndSlide`, elle fait deux choses :

1. **Franchissement de ressaut.** Si le déplacement horizontal de la frame est bloqué alors qu'on
   est au sol, on cherche la **plus petite** élévation qui le libère (par pas de `PasMarche`, 1 px)
   et on y remonte le corps — la montée est donc proportionnée à la marche réelle. Au-delà de
   `HauteurMarcheMax` (10 px) c'est un vrai mur : on ne grimpe pas. `FloorSnapLength` recolle
   ensuite le joueur au sol, donc aucun flottement.
2. **Déblocage d'arête.** Si `TestMove` prouve que le chemin devant est **libre** mais que le joueur
   n'a pas progressé depuis `FramesAvantDeblocage` frames (0,1 s), on applique le déplacement. Ce
   cas arrive au sommet d'une butte, où deux pentes se rejoignent en pointe : le joueur y oscille en
   équilibre sur l'arête, un contact rasant remettant sa vitesse à zéro à chaque frame. Le
   déplacement appliqué venant d'être vérifié libre, il ne peut traverser aucun obstacle.

Détails qui comptent :
- La garde utilise le **coyote time** plutôt que `IsOnFloor()` strict : en passant un sommet le
  joueur décolle d'une frame, et c'est précisément là qu'il accrochait. Un saut remet le coyote à
  zéro, donc on ne grimpe pas en l'air.
- La sonde fait au minimum `DistanceSondeMarche` (4 px) : la frame où l'arête bloque, `MoveAndSlide`
  met `Velocity.X` à zéro, et le `dx` suivant (une fraction de pixel) n'atteindrait plus l'obstacle.

Nouveaux exports : `HauteurMarcheMax`, `PasMarche`, `DistanceSondeMarche`, `FramesAvantDeblocage`.

## Vérification

Build C# propre. Harnais headless jetables (supprimés après coup) :

- **Balayage de 140 essais** sur 7 jointures (village, pieds de rampe, sommet, embouts), départs
  décalés au sub-pixel et hauteurs de chute variées, dans les deux sens :
  **0 blocage, 0 glissade parasite**. Avant le correctif : 12 blocages sur 20 essais au sommet.
- **Traversée est→ouest** : arrêt correct à x = 11,5 contre `MurGauche`, aucune glissade.
- **`MurGlace` (`mur_fondable`) reste infranchissable** : x maxi 2674,5, bien avant 2700 — la marche
  automatique ne permet pas de contourner un mur de gameplay.
- **Non-régression pente forte** : sur une `PenteBanquiseForteDescendante`, la glissade obligatoire
  s'enclenche (f=30), dévale la pente et se prolonge sur le plat.

**Réserve** : play-test manuel `godot` recommandé pour le ressenti de la marche automatique (une
montée de 10 px max est instantanée, à valider à la manette/clavier).

## Piste non retenue

Aligner les collisions sur le dessus de neige dessiné (mesuré : -46 pour les dalles centrales, -51
pour embouts et pentes, contre -34/-39 en collision) supprimait aussi les ressauts, mais imposait de
retoucher toute la géométrie du sol. Abandonné au profit du correctif joueur, plus robuste pour la
suite du level design.
