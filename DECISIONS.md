# Décisions de game design non spécifiées

Règle par défaut appliquée : lisible, goofy, généreux avec le joueur.

## Jalon C : nombre d'écrans banquise

Plutôt que 3-4 écrans banquise quasi identiques (répétition de faible valeur),
je compresse en 2 écrans denses (01 déjà long et varié + 02 plus dur) puis la
Crevasse, pour garder le temps/budget sur le chemin critique (pouvoir de
chaleur + boss). Le critère de fin réel est "atteindre et vaincre le boss",
pas un compte exact d'écrans.

## Boss Cerf : réutilisation d'animations (économie de budget)

Le piétinement réutilise l'animation "idle" (pas de pose de cabrage dédiée)
et le souffle de givre réutilise "charge" comme télégraphe. Seul le résultat
gameplay est nouveau (stalactites qui tombent, cône de givre procédural en
ColorRect/Area2D). Ça respecte "une animation moyenne mais fonctionnelle
vaut mieux que trois tentatives pour la perfection" - 0 génération
supplémentaire pour tout le combat.

## Boss Cerf : tuning des PV et dégâts (à ajuster après playtest réel)

PvMax=40, boule de neige=1 (x3 en fenêtre de vulnérabilité = 3), charge=1,
souffle de givre=2. Joueur PvMax=5. Non testé en conditions réelles
(impossible de playtester à la manette en headless) - chiffres posés par
défaut raisonnable, à réajuster après un premier essai humain.

## Chemin 3 (Carrefour) : impasse propre

Sur consigne explicite de cette mission ("chemin optionnel en impasse propre
à venir"), je ne tranche pas la vieille question Rencontre/Trésor : le
Chemin 3 reste un aller simple vers une plateforme qui ne mène nulle part
pour l'instant, sans contenu de fin.

## Plateformes-objets : adaptation pixel art plutôt que rendu illustré cel-shading

Consigne initiale demandait un rendu "haute résolution, PAS de pixel art"
façon cartoon à contours épais (référence fournie en pièce jointe chat, pas
un fichier du dépôt). Vérifié directement auprès de PixelLab
(`agent_help`) : la plateforme ne produit que du pixel art, aucun mode
lisse/vectoriel n'existe. Option validée par l'utilisateur : réinterpréter
la structure de la référence (strates neige/roche/glace turquoise,
stalactites, fissures) en pixel art cohérent avec tout le reste du jeu,
plutôt que de chercher un autre outil. Cohérence visuelle du projet
préservée ; c'était le compromis le moins coûteux des trois proposés.

## Plateformes-objets : échelle x2 au lieu du 1:1 des petits props

Les sprites sont générés à ~200-400px natif (plafond PixelLab 400px en mode
basique) puis affichés à l'échelle x2, comme les couches de fond
parallax — pas en 1:1 comme les petits props de décor (cristaux, stalactites
existants). Justification : ce sont de gros objets de gameplay dimensionnés
par rapport à la vitesse de course du joueur (220px/s, "petite" ≈ 1.8s de
traversée), pas des éléments décoratifs à taille fixe. Sans ce facteur x2,
la plateforme "grande" plafonnerait à 400px natifs (~1.8s), trop courte pour
la progression de taille demandée.

## Plateforme traversable : layer physique dédié plutôt que détection de type de sol

Le mécanisme existant de détection glace/fragile passe par les custom data
d'un TileMapLayer unique (groupe "sol") — inutilisable pour un objet
autonome. Plutôt que d'étendre ce système, layer physique séparé
(`Constantes.LayerPlateformesTraversables`, bit 5) retiré temporairement du
`collision_mask` du joueur sur bas+saut : n'affecte jamais le layer 1
(terrain normal), donc aucun risque de régression sur le reste des
collisions.

## Sol banquise : bords partagés par greffe plutôt que génération "raccordable"

PixelLab ne sait pas produire deux images aux bords identiques, et son
inpainting à masque ne respecte pas les zones gelées (l'image entière est
régénérée — 2 générations perdues pour le confirmer). Solution retenue :
un segment de base rendu auto-tuilable par post-traitement (roll + fondu),
puis les bords exacts de ce segment sont greffés par script sur chaque
variante (24 px de transition). Toute pièce portant ces bords se raccorde
avec n'importe quelle autre, dans n'importe quel ordre. Les variantes sont
générées avec le prompt EXACT du segment de base (toute reformulation fait
dériver le style — leçon confirmée deux fois), puis harmonisées par
transfert de couleurs rangée par rangée. Les embouts sont fabriqués
procéduralement à partir du segment de base (cassure en zigzag + stalactite
existante réutilisée) : palette et raccord garantis par construction, zéro
génération.

## Pentes banquise : 100% procédurales (décalage de colonnes), 0 génération

Les pentes sont fabriquées en décalant verticalement chaque colonne de
pixels du segment A auto-tuilable (marches d'1px — l'esthétique pixel art
standard des pentes). Conséquences garanties par construction : palette
identique, bords gauche/droit exactement ceux d'un segment plat, donc
surface alignée aux deux extrémités avec le sol plat. 22° = dénivelé 68px
natif, 45° = 171px. Collision en CollisionPolygon2D parallélogramme (dessus
en diagonale sur la neige) : pas de marches invisibles.

## Joueur : floor_snap_length passé de 1 (défaut) à 8

Attrapé en testant la descente 45° : à 220px/s le joueur avance ~3,7px par
frame, le snap par défaut (1px) ne le recolle pas au sol en descente → il
dévale en petits sauts (auSol perdu). 8px couvre 45° avec marge. Vérifié
dans la foulée que le saut fonctionne toujours (le snap de Godot 4
n'empêche pas de décoller quand la vélocité part vers le haut).

## PNJ pingouin : animer l'objet validé plutôt que recréer un personnage

Le choix d'accessoire s'est fait sur 3 vignettes générées en petits objets
1-direction (pas des personnages 8 directions). Pour la version finale,
plutôt que recréer un personnage complet à partir de la vignette élue
(coûteux, 8 directions inutiles pour un PNJ statique de profil, et identité
non garantie), les animations idle/parler sont générées directement sur
l'objet vignette via animate_object v3 : identité strictement préservée
(frame 0 = le sprite validé), coût minimal. Le flip horizontal suffira si
un PNJ doit regarder à gauche.

## Headless : --quit-after compte les frames de RENDU, pas la physique

Attrapé en déboguant un test qui « se figeait » : en headless le rendu
tourne ~2,4x plus vite que les ticks physiques, donc --quit-after 400
n'exécute que ~170 frames physiques (~2,8 s de jeu). Prévoir large
(quit-after ~150 par seconde de gameplay à simuler).

## Fond d'arène boss : composition procédurale autour d'un panneau généré

L'arène fait 2880x400 px écran (1440x200 natif) mais PixelLab plafonne à
400px de large. L'image unique livrée est donc composée : panneau canyon
400x200 généré (composition choisie par l'utilisateur sur 3 vignettes),
falaises du panneau réparties aux deux extrémités de l'arène, milieu rempli
par un dégradé lissé tiré du ciel du panneau + nuages et streaks de sol
détourés (chroma-key) et posés épars — zéro répétition visible à hauteur
de gameplay. Deux approches d'extension écartées en route : miroir-tuilage
des falaises (effet kaléidoscope) et panneau de paroi séparé (effet
palissade + dérive de saturation). Désaturation globale -12% pour le
détacher des plateformes, conformément au brief.

## Pack Noël : bulle de dialogue factorisée, variantes procédurales

Troisième PNJ à bulle → la fabrique de bulle est extraite dans
Common/BulleDialogue.Creer() (PNJPingouin et LutinCGT refactorés dessus),
et les PNJ sans logique propre passent par un PNJSimple générique
(dossier d'idle, fps, dialogue et décalage de bulle en [Export]) — une
seule scène couvre les deux poses du lutin d'usine. La variante bleue de
la guirlande et l'embout droit sont fabriqués en procédural (rotation de
canal sur les boules rouges, miroir + greffe de bord), soit 3 sprites
gratuits. Échelle validée en comparaison : joueur (43px) < lutins (~55px)
< Père Noël (91px), affichage 1:1 cohérent avec les PNJ existants.

## Tapis roulant : StaticBody2D + ConstantLinearVelocity, défilement par roll

Le brief suggérait un AnimatableBody2D, mais le mécanisme Godot canonique
pour un tapis roulant est ConstantLinearVelocity sur un StaticBody2D : le
corps ne bouge pas, la vélocité est transmise aux corps posés dessus —
validé en headless (joueur immobile poussé à la vitesse exportée, arrêt
net en quittant le tapis). Le défilement visuel est procédural : les 4
frames sont la même image dont seules les rangées de la bande de cuir sont
décalées d'un quart de largeur — chaque frame reste donc raccordable, la
cadence est calée sur la vitesse et le sens négatif joue l'animation à
l'envers. Une animation générée n'aurait pas préservé les bords.
