# Prompts bruitages (SFX) — Les Aventures de Glooby

Prompts prêts à coller dans une IA de génération de sons, un par action réelle du jeu
(recensées dans le code : `Player.cs`, `BossCerf.cs`, ennemis, interactifs, UI).

## IA recommandée
- **ElevenLabs — AI Sound Effects (SFX v2)** → **le meilleur** pour un jeu : sons à partir
  d'une description en langage naturel, **48 kHz**, clips jusqu'à 30 s, **bouclage possible**,
  **libre de droits** (usage commercial sur offre payante). **50 générations gratuites/mois**.
- Alternatives : **Dubbing AI** (bibliothèque + temps réel), **Pixabay SFX** (banque gratuite si tu ne veux pas générer).

## Conseils de prompt SFX
- Décrire **la matière + l'action + le style** : *« short cartoony … »*, préciser durée courte.
- Style global du jeu : **cartoon, mignon, pixel-art, thème hivernal/Noël, kid-friendly** — le rappeler dans le prompt.
- Générer **plusieurs variations** des sons répétés (pas, saut) pour éviter la lassitude.
- Sons courts = one-shot ; ambiances (blizzard, grotte) = demander un **seamless loop**.

---

## 1. Joueur — mouvements & actions (`Player.cs`)

| # | Action (code) | Prompt |
|---|---------------|--------|
| 1 | Course (`"course"`) | Short crunchy footstep on fresh snow, single step, cute cartoon platformer, soft and light |
| 2 | Saut (`Sauter`) | Short playful jump whoosh with a tiny cartoon "boing", light and bouncy, kid-friendly |
| 3 | Atterrissage (`IsOnFloor`) | Soft muffled landing thud in snow, small "poff", cartoon platformer |
| 4 | Glissade sur glace (`DemarrerGlissade`) | Whooshing ice slide, penguin belly-sliding on frozen ice, smooth glossy swoosh, playful |
| 5 | Relevé après glissade (`glissade_relever`) | Short light "get up" cloth-and-snow shuffle, quick and soft, cartoon |
| 6 | Lancer boule de neige (`Lancer`) | Quick soft whoosh of a small snowball being thrown, light air swipe, cartoon |
| 7 | Manger poisson / soin (`Manger`) | Cute gulp then a warm gentle healing chime, positive and soft, kid-friendly |
| 8 | Pouvoir de Chaleur (`UtiliserPouvoirChaleur`) | Warm magical fire whoosh with a soft crackle, cozy heat aura burst, cartoon fantasy |
| 9 | Pouvoir de Glace (`UtiliserPouvoirGlace`) | Crystalline ice forming sound, a small platform of frost crystallizing quickly, sparkly and cold |
| 10 | Prendre un coup / dégâts (`Blesser`) | Short comical "ouch" hit, soft cartoon impact with a tiny squeak, non-violent, kid-friendly |
| 11 | Traversée plateforme (`GererTraverseePlateforme`) | Tiny soft "pop" of dropping through a snow platform, quick and light, cartoon |
| 12 | Tuile de glace fragile qui casse (`GererGlaceFragile`) | Thin ice cracking then shattering into small pieces, short crisp frozen crack, cartoon |
| 13 | Chute dans le vide / mort (`TomberDansLeVide`, `OnJoueurMort`) | Short descending cartoon "falling" whistle ending in a soft comical defeat plop, kid-friendly |
| 14 | Apparition / respawn (`JouerApparition`) | Cheerful magical "pop-in" appearance, sparkly bright spawn shimmer, short and cute |

## 2. Boss Cerf — Rodolphe (`BossCerf.cs`)

| # | Action (état) | Prompt |
|---|---------------|--------|
| 15 | Intro / entrée (`Etat.Intro`) | Big friendly-but-menacing reindeer bellow, cartoon boss roar with sleigh bells jingle, wintery |
| 16 | Télégraphe de charge (`Telegraphe`) | Reindeer hoof scraping the icy ground, angry snort wind-up before a charge, cartoon tension |
| 17 | Charge (`Etat.Charge`) | Fast galloping hooves on ice with jingling bells, rushing cartoon charge, energetic |
| 18 | Impact mur + étourdi (`PasserEnEtourdi`) | Heavy comical crash into an ice wall then dizzy wobbling "stars" chime, cartoon stun |
| 19 | Piétinement → stalactites (`DemarrerPietinement`) | Powerful ground stomp with a rumbling shake, cartoon boss slam, wintery |
| 20 | Souffle de givre (`DemarrerSouffleGivre`) | Freezing frost breath, an icy wind exhale with sparkling crystals, cold whoosh, cartoon |
| 21 | Boss encaisse un coup (`AjusterDegats`) | Short soft cartoon impact on a big creature, harmless "bonk", kid-friendly |
| 22 | Transition phase 2 (`DeclencherTransitionPhase2`) | Dramatic power-up surge, a rising magical charge with sleigh bells, boss enrages, cartoon |
| 23 | Boss vaincu (`Mourir` → `"vaincu"`) | Comical defeated reindeer groan, gentle descending "aww" collapse, non-violent, kid-friendly |

## 3. Ennemis

| # | Ennemi (fichier) | Prompt |
|---|------------------|--------|
| 24 | Bonhomme de neige — déplacement (`BonhommeDeNeige.cs`) | Soft rolling/waddling snow shuffle of a little snowman moving, cute cartoon |
| 25 | Bonhomme de neige — vaincu (`Mourir`) | A snowman puffing apart into a burst of soft snow, gentle comical poof, kid-friendly |
| 26 | Boule de neige ennemie — lancer (`LanceurBouleNeige`) | Quick light whoosh of a thrown snowball, short air swipe, cartoon |
| 27 | Boule de neige — impact (`BouleDeNeige`, `Projectile`) | Soft snowball splat "pof", harmless powdery impact, cartoon |

## 4. Interactifs & ramassables

| # | Élément (fichier) | Prompt |
|---|-------------------|--------|
| 28 | Checkpoint / trou de pêche activé (`Checkpoint.cs`) | Warm positive checkpoint chime, gentle rising sparkly bell confirmation, cozy and reassuring |
| 29 | Mur de glace qui fond (`MurFondable.Melt`) | Ice melting into water, a soft sizzling trickle and crackle, magical warm thaw, cartoon |
| 30 | Stalactite piège — chute + impact (`StalactitePiege`) | Sharp icicle whoosh falling then a crisp icy shatter on the ground, cartoon danger |
| 31 | Ramassage de pouvoir (`ElementRamassable`, chaleur/glace) | Bright rewarding power-up jingle, sparkly ascending magical pickup, joyful and short |

## 5. Interface (menus, HUD, fin)

| # | Événement | Prompt |
|---|-----------|--------|
| 32 | Navigation menu (`MenuFabrique`/`MenuPause`) | Soft UI blip for menu navigation, tiny bright cursor click, clean and cute |
| 33 | Confirmer / valider | Positive UI confirm chime, short cheerful two-note ding, friendly |
| 34 | Retour / annuler | Soft low UI back sound, gentle short "boop" cancel, neutral |
| 35 | Pause / reprise (`MenuPause`) | Quick pause swoosh with a soft muffle, and a bright unpause reverse swoosh, cartoon UI |
| 36 | Victoire (`EcranFin.cs`) | Triumphant short victory fanfare sting with sleigh bells, joyful win celebration, kid-friendly |
| 37 | Game over | Gentle sad short game-over sting, soft descending music-box notes, non-harsh, kid-friendly |

---

### Notes d'intégration Godot
- Chaque bruitage = un `AudioStreamPlayer` (ou `AudioStreamPlayer2D` pour les sons localisés :
  boss, ennemis, stalactites) déclenché à l'endroit de l'action dans le code listé ci-dessus.
- Générer **2-3 variations** des pas (#1) et du saut (#2), choisies au hasard, pour éviter la répétition.
- Régler un **bus « SFX »** distinct du bus « Musique » pour un contrôle de volume séparé (options).
- Formats : **.wav** pour les sons courts (latence nulle), **.ogg** pour les boucles d'ambiance longues.
