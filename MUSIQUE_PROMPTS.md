# Prompts musicaux — Les Aventures de Glooby

10 prompts prêts à coller dans une IA musicale pour générer la bande-son du jeu.

## Recommandations d'outils
- **SoundRaw** ou **MusicGen (Meta, local)** → instrumental **libre de droits**, idéal pour un jeu (usage commercial OK).
- **Suno / Udio** → très bonne qualité, mais offre gratuite = **pas de droits commerciaux** ; à réserver au prototypage.
- Toujours demander de l'**instrumental (no vocals)** et penser au **bouclage** (loop) : la musique de jeu tourne en boucle.

## Règles de prompt appliquées (issues des références VGM)
- Formule : **Genre + BPM chiffré + Mood + Instruments + Référence** (4-7 descripteurs, en anglais).
- **Glace/neige** : carillons cristallins, cloches, réverb, « beauté mystérieuse » (réf. Ice Cap Zone *Sonic 3*, Phendrana Drifts *Metroid Prime*, DKC2).
- **Grotte** : ambient, gouttes/échos, ostinatos discrets (réf. *Cave Story*, Underground BGM *Mario*).
- **Boss** : 140-180 BPM, percussions lourdes (taïko, toms), cuivres, cordes en ostinato, **dynamique par phases** ; rappel du thème principal au climax.
- **Village/hub** : mélodique, chaleureux, **bouclable** (démarrer/finir sur le même accord).

---

## 1. Menu principal (`menu_principal.tscn`) — thème d'accueil
> Charming 8-bit inspired orchestral chiptune, 100 BPM, warm and inviting with a hint of adventure, glockenspiel and soft strings over a gentle music box melody, cozy winter wonder mood, in the style of a classic Nintendo platformer title screen. Instrumental, loopable, seamless loop.

## 2. Village des pingouins (hub paisible) — banquise biome
> Cozy melodic village theme, 96 BPM, warm and quirky and heartwarming, celesta and pizzicato strings and light woodwinds with a memorable folk-like melody, snowy peaceful town mood like Animal Crossing meets Donkey Kong Country. Instrumental, seamless loop starting and ending on the same chord.

## 3. Banquise (champ de glace ouvert) — exploration
> Wondrous arctic exploration theme, 110 BPM, adventurous and shimmering and airy, crystalline bells and glockenspiel and sustained strings with a soaring flute melody, wide open icy landscape, in the style of Ice Cap Zone (Sonic 3) and Phendrana Drifts (Metroid Prime), lots of reverb. Instrumental, loopable.

## 4. Grotte (souterrain) — ambiance mystérieuse
> Mysterious underground cave ambient, 78 BPM, eerie and cold and tense, echoing marimba and low sustained pads and sparse plucked strings with dripping-water reverb, dark cavern exploration mood inspired by Cave Story Last Cave. Minimalist, instrumental, seamless loop.

## 5. Approche du boss — tension montante
> Ominous pre-boss buildup, 90 BPM rising, foreboding and suspenseful, low brass swells and tremolo strings and a distant ticking timpani ostinato, the calm before a battle in a frozen arena. Instrumental, short loop, builds dread.

## 6. Boss Cerf (Rodolphe) — Phase 1
> Epic boss battle theme, 150 BPM, aggressive and driving and heroic, heavy taiko and orchestral bass drum with brass stabs, string ostinato and a bold horn melody, an antlered winter beast charging in an ice cavern, in the style of a Zelda boss fight. Instrumental, loopable, relentless energy.

## 7. Boss Cerf (Rodolphe) — Phase 2 (transition à 50% PV)
> Intensified final-phase boss theme, 168 BPM, frantic and desperate and epic, add choir "ah" voices and distorted electric guitar and double-time percussion over the same brass and string ostinato as phase one, key change up a step, blizzard fury climax. Instrumental, loopable, escalation of phase one.

## 8. Victoire / écran de fin (`ecran_fin.tscn`)
> Triumphant victory fanfare then a warm resolving outro, 100 BPM, joyful and emotional and heartwarming, full brass fanfare resolving into celesta and strings reprising the main title melody, the hero saves the frozen land. Instrumental, no loop needed, satisfying ending.

## 9. Checkpoint / trou de pêche (`checkpoint_peche.tscn`) — jingle court
> Gentle short reward jingle, 4 seconds, warm and reassuring, ascending glockenspiel arpeggio with a soft harp glissando and a single bell chime, a safe cozy moment of rest. Instrumental one-shot, no loop.

## 10. Mort / game over — sting court
> Short melancholic game over sting, 3 to 5 seconds, gentle and sad but not harsh, descending music box melody with a soft low piano note, a brief pause of defeat in a snowy world. Instrumental one-shot, no loop, kid-friendly.

---

### Notes d'intégration Godot
- L'export `Musique` de `ZoneBoss` / `ZoneBossCerf` attend un asset audio (actuellement vide) → y brancher **#6** (et gérer le passage à **#7** au changement de phase à 50% PV côté `BossCerf.cs`).
- Musique de fond continue → utiliser un `AudioStreamPlayer` global (autoload type `Musique`), synchronisé avec les régions (`village`/`banquise`/`grotte`) comme le fait déjà `BackgroundManager.AfficherRegion`.
- Réglage import : cocher **Loop** sur les pistes 1-7 dans les paramètres d'import audio de Godot ; laisser 8-10 en one-shot.
