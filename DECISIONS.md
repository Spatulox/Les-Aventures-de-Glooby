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
