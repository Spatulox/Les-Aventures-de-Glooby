# Budget de génération PixelLab — Mission autonome

Plafond de la mission : **60 générations**. Seuil d'alerte (80%) : 48.
Solde compte au départ de la mission : 1812 restantes / 2000 (hors plafond mission).

| # | Jalon | Quoi | Générations | Cumul |
|---|---|---|---|---|
| 1 | B | Icône cœur (HUD, PV) — vide/plein géré par teinte en code, pas de 2e génération | 1 | 1 |
| 2 | (hors jalon, demande directe) | Lot de 20 décors de grotte (props divers, même style) | 20 | 21 |
| 3 | (hors jalon, demande directe) | Fond répétable de la Crevasse (transition banquise->grotte, système de régions) | 1 | 22 |
| 4 | (hors jalon, demande directe) | Pack décor parallax Grotte — 3 couches (lointain/intermédiaire/proche) + 3 éléments (cristal/éboulis/stalagmite) | 6 | 28 |
| 5 | (hors jalon, demande directe) | Pack décor parallax Banquise — 4 couches (dont 2 régénérées pour bords plats/transparence correcte) + 3 éléments (congère/bloc/plaque) | 9 | 37 |
| 6 | (hors jalon, demande directe) | Plateformes-objets style illustré (adapté en pixel art) — fixe x3 tailles, traversable, mobile, fragile x3 états (dont 1 régénéré, perspective incohérente), glissante | 10 | 47 |
| 7 | (hors jalon, demande directe) | Sol banquise segmenté — 1 segment de base + 2 variantes utilisables (après 4 essais écartés : dérive de style x2, embouts x2 refaits en procédural, inpainting x2 non respecté) | 9 | 56 |
| 8 | (hors jalon, demande directe) | Pentes banquise 22°/45° x2 sens — 100% procédural | 0 | 56 |
| 9 | (hors jalon, demande directe) | PNJ pingouin : 3 vignettes d'options (choix utilisateur : écharpe) + 2 animations (idle, parler) sur le sprite validé via animate_object — dépassement du plafond 60 signalé et validé par l'utilisateur | 7 | 63 |
| 10 | (hors jalon, demande directe) | Fond d'arène Boss Cerf : 3 vignettes de composition (choix utilisateur : canyon de glace) + panneau central 400x200 + panneau paroi (écarté, remplacé par composition procédurale) | 5 | 68 |
| 11 | (hors jalon, demande directe) | Panneaux bois (poteau, accroché, flèche — miroir gratuit pour l'autre sens) — 3/3 réussis du premier coup | 3 | 71 |
| 12 | (hors jalon, demande directe) | Lutin CGT gréviste : 3 vignettes de pose (réponse utilisateur "fais plusieurs" → les 3 gardées) + 3 animations idle (2 gén. chacune) | 9 | 80 |
| 13 | (hors jalon, demande directe) | Pack Noël : 2 lutins usine (dont 1 régénéré, proportions incohérentes), Père Noël + idle animé, 2 sapins, guirlande segment + embout (variante bleue et embout droit gratuits en procédural) | 10 | 90 |
| 14 | (hors jalon, demande directe) | Usine du Père Noël : fond 2 couches (mur + poutres), tapis segment + embout (1 régénéré, bûche au lieu d'extrémité), 2 établis — défilement de bande et embout droit procéduraux | 7 | 97 |
| 15 | (hors jalon, demande directe) | 3 flocons de neige — générés en PROCÉDURAL (symétrie 6 branches exacte, contour fin, meilleur que PixelLab à petite échelle) ; servent désormais les 3 couches du blizzard (`scenes/meteo/blizzard.tscn`) | 0 | 97 |
| 16 | (hors jalon, demande directe) | 5 vignettes concept ennemis (ours/ver/morse + bonhomme malicieux/grognon), choix user | 5 | 102 |
| 17 | (hors jalon, demande directe) | Ours polaire : 6 animations (idle/marche/detection/charge/etourdi/mort) via animate_object — frames déposées dans TES dossiers (code OursDeNeige non modifié) | ~14 | 116 |
| 18 | (hors jalon, demande directe) | Bonhomme malicieux : 4 animations (idle/armer/lancer/mort) + fichiers neufs BonhommeDeNeige/BouleDeNeige (impact procédural) | ~13 | 129 |
| 19 | (hors jalon, demande directe) | Murs de grotte empilables : 5 gén. (3 centres colonnes + couronnement + base) puis 3 centres régénérés « texture pleine » — traités en tuilage vertical, 2 variantes dérivées du meilleur | 8 | 137 |
| 20 | (hors jalon, demande directe) | Sol de grotte décoré : 3 centres (dessous stalactites/cristaux, densité variable) + 2 fins à falaise cassée — surface alignée, bords greffés | 5 | 142 |
| 21 | (hors jalon, demande directe) | Obstacles : 2 tas de neige + 2 rochers enneigés (frames de destruction procédurales, 0 gén.) | 4 | 146 |
| 22 | (hors jalon, demande directe) | **Boss Lutin Mecha + mini-jouet explosif** — 3 packs de vignettes (4 silhouettes de boss, 16 jouets, 16 glace/parachute) + 7 animations de boss (dont « vaincu » régénéré : le mecha restait debout) + 2 animations de jouet (les 2 régénérées : l'explosion n'explosait pas, la course ne courait pas). 5 animations dérivées **sans génération** : saut découpé en accroupi/vol/impact, tir en armement/tir, fermeture de trappe = ouverture inversée. Touché + onde de choc + balancement du parachute = procéduraux (0 gén.). **78 générations réelles** (solde 1249/2000) | 20 | 166 |
| 23 | (hors jalon, demande directe) | **Ennemis d'usine : Locomotive jouet (fonceur) + Mécha jouet lanceur** — 1 pack de 16 vignettes (8 locomotives + 8 automates, choix user : `[6]` et `[13]`) + 9 animations (loco : idle/detection/charge/etourdi/mort ; mécha : idle/armer/lancer/mort) + 3 régénérations (mort de la loco ×1, idle du mécha ×1 — échec, voir ci-dessous ; mort du mécha comptée dans les 9). Projectile **réutilisé tel quel** (`BouleDeNeige`, 0 gén.). Flash d'armement et basculement à la mort = procéduraux (0 gén.). **31 générations réelles** (solde 1218/2000) | 12 | 178 |

| 24 | (correctif, 0 génération) | **Morts des 2 ennemis d'usine refaites en PROCÉDURAL** : le sprite intact est découpé en blocs de 4-5 px (« éclats de bois ») projetés vers l'extérieur avec gravité, qui retombent et s'entassent sur la ligne de sol — 6 frames chacune, uniquement des translations sur la grille (aucune rotation ni redimensionnement, donc palette et netteté d'origine préservées). Remplace les frames PixelLab qui refusaient de s'effondrer. Même approche que la ligne 21 (destruction des tas de neige/rochers). | 0 | 178 |

> **Limite constatée sur `animate_object` (v3)** : le modèle anime À PARTIR de la frame de base et refuse de détruire la silhouette du sujet ou d'en changer la pose de repos. Trois morts (boss, locomotive, mécha) ont dû être régénérées ; seule celle du boss a fini par s'effondrer correctement. Pour les deux ennemis, les frames de destruction ont finalement été **dessinées en procédural** (ligne 24) — c'est plus fiable ET gratuit. L'idle du mécha garde de son côté le bras levé malgré une consigne explicite : compensé par un flash chaud + une anticipation en écrase-étire (procéduraux).
>
> **À retenir pour les prochains lots** : ne pas relancer PixelLab plus d'une fois sur une destruction ou un changement de pose de repos — passer directement au procédural.
