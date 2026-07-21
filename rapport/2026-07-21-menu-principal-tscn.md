# Menu principal : layout déplacé dans `menu_principal.tscn`

## Problème

`scenes/ui/menu_principal.tscn` n'était qu'un `Control` vide : titre, boutons, voile et
mob étaient construits en C# dans `_Ready()`. Rien n'était éditable dans l'éditeur, et le
positionnement passait par un hack (`zoneColonne.AnchorRight = 0.72f`).

Le PNJ aléatoire, mis à l'échelle sur une hauteur fixe (`HauteurMob = 240`) sans aucune
borne, pouvait déborder sur la colonne de boutons (risque connu depuis le commit `5a94643`,
jamais vérifié).

## Changements

**`scenes/ui/menu_principal.tscn`** — porte désormais la mise en page :

```
MenuPrincipal (Control, FullRect)
├── Voile      ColorRect plein écran (0.06,0.08,0.14,0.5)
├── Colonne    VBoxContainer ancré à gauche (x 40→280), separation 12
│   ├── Titre + BoutonNouvelle / BoutonContinuer / BoutonParametres / BoutonQuitter
│   └── boutons en custom_minimum_size 240×36, sans override de style (= rendu MenuFabrique)
└── BoiteMob   Control ancré à droite (x 340→600, y 55→305), clip_contents = true
```

**`scripts/UI/MenuPrincipal.cs`** — ne porte plus que le comportement :

- `_Ready` branche les boutons de la scène (`GetNode<Button>("Colonne/…").Pressed += …`) et
  grise « Continuer » ; les appels `MenuFabrique.AjouterColonne/AjouterBouton/AjouterFond`
  du menu principal disparaissent, ainsi que le hack `AnchorRight`.
- `AjouterMobAleatoire` monte le sprite dans `BoiteMob` au lieu d'une ancre créée à la volée.
- Nouvelle méthode **`AjusterMob`** : échelle = `Mathf.Min(boite.X / largeurFrame, boite.Y / hauteurFrame)`,
  position = centre de la boîte. La boîte commande la taille du mob, jamais l'inverse — le
  débordement est donc impossible en largeur **comme** en hauteur ; `clip_contents` n'est
  qu'un filet de sécurité. Appelée depuis `_Ready` et depuis le signal `BoiteMob.Resized`
  (la taille d'un `Control` ancré n'est pas définitive dans `_Ready`).
- `HauteurMob` supprimée.
- `AjouterFondAleatoire` fait `MoveChild(fond, 0)` : le fond, tiré au hasard donc créé en
  code, doit passer sous les nœuds authorés dans la scène.

`MenuFabrique` est **inchangé** — `MenuPause` et le panneau Paramètres (dont les 9 lignes se
déduisent de l'`InputMap`) continuent de s'en servir.

## Bouton « Debug »

Nouveau bouton dans la colonne (entre « Continuer » et « Paramètres ») qui lance une partie
de test : tous les pouvoirs acquis d'emblée, et les mobs tués d'un seul coup.

- **`GameState`** : propriété `ModeDebug` (session uniquement, **hors `DonneesSauvegarde`** —
  elle ne doit pas contaminer un fichier de sauvegarde ; remise à `false` par `NouvellePartie`)
  et méthode `NouvellePartieDebug()` = `NouvellePartie()` + `ObtenirPouvoirChaleur()` +
  `ObtenirPouvoirGlace()` (passer par les `Obtenir*` fait bien émettre les signaux au HUD).
- **`DamageSource`** : extension `EstDuJoueur()` (`Snowball` ou `Fire`) — le mode debug ne
  surpuissance que les coups **portés** par le joueur ; ceux qu'il encaisse gardent leur
  montant normal.
- **`LivingEntity.TakeDamage`** : si `ModeDebug` et `source.EstDuJoueur()`, les dégâts valent
  `PvMax` au lieu de `AjusterDegats(source.MontantDegats())` → la cible tombe quels que soient
  ses PV. Le joueur n'est pas concerné (il surcharge `TakeDamage` pour router vers `GameState`).
- **`MenuPrincipal`** : `DemarrerPartieDebug()`, et les trois entrées de partie partagent
  désormais un `ChargerMonde()`.

- **`GameState.ConsommerManaGlace`** : ne prélève rien en mode debug → la jauge de glace est
  infinie. Le blocage est mis là plutôt que dans `PeutUtiliserPouvoirGlace` pour que la barre
  du HUD reste cohérente avec l'état réel (elle affiche plein, et c'est vrai).

Effet de bord assumé : un boss one-shot **saute ses transitions de phase** (BossCerf change de
phase à 50 % PV) — c'est le but d'un mode debug, mais ça le rend inutilisable pour tester ces
phases.

## Vérification

- `godot --headless --build-solutions --quit` : compilation propre.
- `godot --headless --quit-after 200` : aucune erreur de scène (`Node not found`, null…).
  Les `Not supported by this display server` de `ToucheDe` sont préexistantes (pas de clavier
  en headless).
- **Reste à faire par un humain** : un `godot` interactif pour juger le cadrage — relancer
  plusieurs fois pour tirer `boss_cerf` (le plus large) et `pingouin`, et confirmer que la
  boîte par défaut donne un rendu satisfaisant ; elle se redimensionne à la souris dans
  l'éditeur, le mob suit.
