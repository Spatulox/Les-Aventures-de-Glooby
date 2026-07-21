# ANALYSE — Système de paramètres & remapping des touches

> Document de recherche préalable (aucun code de gameplay écrit à ce stade).
> Objectif : refonte du menu Paramètres, priorité au **remapping complet**
> (clavier + manette) avec persistance entre sessions.

---

## Partie A — L'existant : conventions et systèmes en place

### A.1 Conventions de code (à respecter scrupuleusement)

- **Tout en français** : noms de classes, méthodes, champs, variables, commentaires,
  noms de nœuds. (`Sauvegarde`, `DonneesSauvegarde`, `ConfigurerActionsParDefaut`, …)
- **Pas de `namespace`** : les classes sont globales, le dossier d'un fichier est
  purement organisationnel (pour l'humain).
- **Commentaire de classe obligatoire** en tête de chaque fichier, décrivant son rôle
  (présent partout dans `scripts/`). Commentaires en `//`, jamais de doc XML `///`.
- **Réutilisabilité avant tout** : helpers partagés plutôt que duplication
  (`Constantes`, `Effets`, `MenuFabrique`, base `ElementRamassable`…).
- **Séparation données / gestionnaire** : ex. `DonneesSauvegarde` (structure de
  données pure) ↔ `Sauvegarde` (I/O disque) ↔ `GameState` (logique + signaux).
  Ce triptyque est le modèle à imiter pour les paramètres.
- **Autoloads = singletons** exposés via une propriété statique `Instance`
  (`GameState.Instance`), déclarés dans `[autoload]` de `project.godot`.
- **Signaux Godot C#** : `[Signal] public delegate void FooEventHandler(...)`.
  Piège documenté (RAPPORT.md, jalon B) : un signal `Foo` génère un membre `Foo`
  qui **entre en collision avec une propriété `Foo`** → nommer signaux et
  propriétés distinctement.
- **UI construite par code**, pas en `.tscn` : les scènes de menu
  (`menu_principal.tscn`) ne contiennent qu'un `Control` racine + le script ;
  toute la mise en page est bâtie dans `_Ready` via la fabrique `MenuFabrique`.

### A.2 Organisation des dossiers

- `scripts/` rangé par rôle : `Common/` (helpers transverses : `Constantes`,
  `Effets`, `Damageable`…), `Core/` (systèmes globaux & autoloads : `GameState`,
  `Sauvegarde`, `DonneesSauvegarde`, `BackgroundManager`, `CameraZone`…),
  `Entities/`, `Plateformes/`, `sol/`, `Terrain/`, `UI/`.
- `scenes/` **miroir** de `scripts/`, chaque élément réutilisable est un « GameObject »
  `.tscn` rangé par rôle. Menus dans `scenes/ui/`.
- → Un système de paramètres a donc sa place naturelle : logique dans
  `scripts/Core/` + `scripts/UI/`, scène éventuelle dans `scenes/ui/`.

### A.3 Système d'entrées actuel

- **Les actions sont enregistrées EN CODE**, pas dans `project.godot` (il n'y a
  **aucune** section `[input]`). Tout se passe dans
  `GameState.ConfigurerActionsParDefaut()` (`scripts/Core/GameState.cs:216`),
  appelée depuis `_Ready`.
- Helper `AjouterAction(string nom, params Key[] touches)` : `InputMap.AddAction(nom)`
  puis pour chaque touche `InputMap.ActionAddEvent(nom, new InputEventKey { PhysicalKeycode = touche })`.
- **Liaison en `PhysicalKeycode`** (position physique QWERTY) — choix délibéré pour
  rester stable quel que soit le clavier (AZERTY/QWERTY). L'affichage retraduit la
  position vers l'étiquette réelle via
  `DisplayServer.KeyboardGetLabelFromPhysical(...)` + `OS.GetKeycodeString(...)`
  (voir `MenuPrincipal.ToucheDe`, `MenuPrincipal.cs:192`). **À conserver.**
- **Actions définies** (11) :

  | Action | Défaut clavier | Rôle (libellé) |
  |---|---|---|
  | `move_left` | ← Left | Aller à gauche |
  | `move_right` | → Right | Aller à droite |
  | `jump` | Espace | Sauter |
  | `slide` | Maj (Shift) | Glisser (glissade ventrale) |
  | `bas` | ↓ Down | Descendre / traverser une plateforme |
  | `lancer` | D | Lancer une boule de neige |
  | `manger` | W | Manger un poisson |
  | `pouvoir_chaleur` | A | Pouvoir de chaleur |
  | `pouvoir_glace` | S | Pouvoir de glace |
  | `action` | Entrée + Espace | Interagir / valider dialogue |
  | `menu` | Échap | Menu / Pause |

- **Consommation des inputs** (sites à ne pas casser) : `Player.cs`
  (`Input.GetAxis("move_left","move_right")`, `IsActionJustPressed("jump"/"slide"/
  "lancer"/"manger"/"pouvoir_chaleur")`, `IsActionPressed("pouvoir_glace"/"bas")`),
  `DeclencheurDialogue` (`"action"`), `MenuPause`/`MenuPrincipal` (`"menu"`).
- **AUCUN support manette aujourd'hui** : pas une seule référence `Joy*`,
  `InputEventJoypad*`, `manette` dans le code. Tout est clavier.
- Subtilités à préserver :
  - `jump` et `action` partagent Espace ; `GameState.DialogueDisponible` arbitre
    (le dialogue capte Espace et n'active pas le saut).
  - `menu` (Échap) sert de touche de pause **et** servira de touche d'annulation de
    la capture de remapping → cas à traiter (cf. B.5).

### A.4 Système de sauvegarde en place

- **Progression uniquement**, pas de paramètres :
  - `DonneesSauvegarde` (`scripts/Core/DonneesSauvegarde.cs`) : structure pure
    (PV, poissons, pouvoirs, checkpoint, éléments consommés, boss vaincus) +
    `Version` de format (migrations futures) + `VersDictionnaire()` /
    `DepuisDictionnaire()` **tolérant aux clés absentes**.
  - `Sauvegarde` (`scripts/Core/Sauvegarde.cs`) : I/O JSON dans
    **`user://pantalon.json`** (`FileAccess` + `Json.Stringify/ParseString`).
  - `GameState` orchestre (`Sauvegarder()`/`Charger()`), ré-émet les signaux au
    chargement pour resynchroniser HUD & sprites.
- **Conséquence de conception** : les paramètres ne sont **pas** de la progression.
  Ils doivent persister même sans partie sauvegardée et survivre à « Nouvelle
  partie ». → **Fichier séparé** de `pantalon.json`. Le triptyque
  données/IO/gestionnaire est le patron à répliquer, mais dans son propre fichier.

### A.5 Menus en place

- `MenuFabrique` (`scripts/UI/MenuFabrique.cs`) : fabrique statique réutilisable —
  `AjouterFond`, `AjouterColonne` (titre + `VBoxContainer`, option panneau
  semi-opaque), `AjouterBouton`, `AjouterLigne`. Partagée menu principal / pause.
- `MenuPrincipal` (scène `menu_principal.tscn`, `run/main_scene`) : Créer / Continuer
  / **Paramètres** / Quitter. L'écran Paramètres actuel est **en LECTURE SEULE** :
  il liste `libellé : touche` (tableau `Controles` codé en dur, `MenuPrincipal.cs:11`)
  et un bouton Retour. Échap ferme le sous-panneau (`_UnhandledInput`).
  - ⚠️ Le tableau `Controles` **omet** `pouvoir_glace` et `action` → incohérence
    mineure à corriger au passage.
- `MenuPause` (dans `monde.tscn`, `CanvasLayer`, `ProcessMode = Always`) : Continuer /
  Retour au menu principal. **Pas d'accès aux Paramètres** aujourd'hui.

### A.6 Points de décision qui découlent de l'existant

1. **Fichier de config distinct** de la sauvegarde de progression (mandat + logique).
2. **Source unique des actions** : aujourd'hui les défauts vivent dans
   `GameState.ConfigurerActionsParDefaut()` et les libellés dans `MenuPrincipal`.
   Pour un « réinitialiser par défaut » fiable et un menu piloté par les données, il
   faut **un catalogue unique** (action → touche(s) clavier + bouton manette par
   défaut + libellé FR + catégorie). ⇒ **Évolution proposée** de l'existant, à
   valider (cf. plan).
3. Réutiliser **`MenuFabrique`** et l'étendre plutôt que créer une UI parallèle.

---

## Partie B — Recherche : remapping propre en Godot 4

### B.1 API `InputMap` (runtime) — briques officielles

Le remapping se fait à chaud sur le singleton `InputMap` (C#) :

- `InputMap.HasAction(StringName)` / `AddAction(StringName, float deadzone = 0.5f)` /
  `EraseAction(StringName)` / `GetActions()`.
- `InputMap.ActionGetEvents(StringName)` → `Array<InputEvent>` (événements liés).
- `InputMap.ActionAddEvent(StringName, InputEvent)` — ajoute une liaison.
- `InputMap.ActionEraseEvent(StringName, InputEvent)` — retire une liaison précise.
- `InputMap.ActionEraseEvents(StringName)` — **vide** toutes les liaisons d'une action
  (base du remapping : on efface puis on ré-ajoute).
- `InputMap.ActionHasEvent(StringName, InputEvent)` — test d'appartenance.
- `InputMap.ActionGetDeadzone / ActionSetDeadzone` — pour les axes manette.
- `InputMap.LoadFromProjectSettings()` — recharge tout depuis `project.godot`
  (inutile ici : nos défauts sont en code, pas dans les ProjectSettings).

**Recette de remapping d'une action** : `ActionEraseEvents(action)` → construire le
nouvel `InputEvent` → `ActionAddEvent(action, event)`.

### B.2 Types d'événements à gérer

- **Clavier** : `InputEventKey`. On continue en **`PhysicalKeycode`** (cohérent avec
  l'existant, robuste AZERTY/QWERTY). Affichage via
  `DisplayServer.KeyboardGetLabelFromPhysical` (déjà maîtrisé dans le projet).
- **Manette — boutons** : `InputEventJoypadButton { ButtonIndex = JoyButton.* }`
  (A/B/X/Y, gâchettes numériques, sticks cliqués…).
- **Manette — axes** : `InputEventJoypadMotion { Axis = JoyAxis.*, AxisValue = ±1 }`
  (sticks & gâchettes analogiques). `AxisValue` porte le **signe** de la direction
  (ex. stick gauche vers la gauche = `LeftX` à `-1`).
- Le signe permet à `Input.GetAxis("move_left","move_right")` (déjà utilisé par
  `Player`) de fonctionner tel quel une fois les axes manette liés aux deux actions
  directionnelles — **aucune modif du gameplay** nécessaire.

### B.3 Capture d'une nouvelle touche (écran d'attente)

Pattern reconnu :

1. L'utilisateur clique la ligne d'une action → passage en **mode capture** (overlay
   « Appuyez sur une touche… », entrée gameplay gelée).
2. On écoute dans **`_Input(InputEvent)`** (et non `_UnhandledInput` : la capture doit
   primer sur tout le reste) le **premier** événement pertinent :
   - `InputEventKey` avec `Pressed && !Echo` ;
   - `InputEventJoypadButton` avec `Pressed` ;
   - `InputEventJoypadMotion` avec `|AxisValue| > seuil` (~0.5) pour ne capter un
     stick/gâchette qu'au-delà d'une zone morte.
3. On consomme l'événement (`GetViewport().SetInputAsHandled()`), on applique le
   remap, on sort du mode capture.
4. **Filtrage clavier/manette** : un panneau « Clavier » n'accepte que
   `InputEventKey`, un panneau « Manette » n'accepte que `InputEventJoypad*` — ainsi
   chaque périphérique garde sa liaison et on ne mélange pas les deux.

### B.4 Détection & résolution des conflits

- Avant d'appliquer, parcourir les autres actions : si le nouvel événement **matche**
  une liaison existante (même `PhysicalKeycode`, ou même `ButtonIndex`, ou même
  `(Axis, signe)`), il y a conflit.
- Comparaison fiable : `InputEvent.IsMatch(autre, exactMatch: false)` fourni par
  Godot, ou comparaison manuelle des champs (plus explicite/lisible — cohérent avec
  le style du projet).
- **Résolution retenue** : proposer à l'utilisateur (dialogue de confirmation) —
  soit **échanger** (l'ancienne action récupère la touche libérée / est vidée), soit
  **annuler**. On ne laisse jamais deux actions sur la même touche silencieusement.
  Version minimale acceptable : retirer la touche de l'action qui la détenait, puis
  l'assigner à la nouvelle (comportement « la dernière gagne », signalé à l'écran).

### B.5 Annulation (Échap) & réinitialisation

- **Annulation** : en mode capture, `Key.Escape` **physique** annule et restaure la
  liaison précédente — traité en dur dans `_Input` **avant** la logique de capture
  (sinon on remapperait « menu » sur Échap). Conséquence : la touche Échap ne peut
  pas être capturée comme liaison ; c'est le comportement standard voulu.
- **Réinitialisation par défaut** :
  - *par action* : bouton sur chaque ligne → réapplique la/les liaison(s) par défaut
    de cette action depuis le catalogue.
  - *global* : bouton « Tout réinitialiser » → réapplique tout le catalogue.
  - Faisable proprement **grâce au catalogue** (A.6.2) qui détient les défauts.

### B.6 Persistance : comparaison des approches

| Approche | Principe | Pour | Contre |
|---|---|---|---|
| **`ConfigFile`** (retenu, **mandat**) | `.cfg` INI sectionné dans `user://` ; on stocke par action une représentation sérialisable des events | Lisible/éditable humain ; **sections** = extensible (`[touches]`, `[audio]`, `[affichage]`, `[accessibilite]`) sans réécriture ; API native `SetValue/GetValue/Save/Load` | Il faut sérialiser soi-même les `InputEvent` (ils ne se stockent pas tels quels proprement) |
| **`Resource` + `ResourceSaver`** (addon KoBeWi) | un `.tres` qui stocke `keycode`/`button_index` et s'auto-applique (`apply_remap`) | Sérialisation quasi gratuite | `.tres` peu lisible ; pas sectionnable pour audio/affichage ; s'éloigne du patron JSON existant |
| **JSON** (comme `Sauvegarde` actuel) | réutiliser le patron `pantalon.json` | Cohérent avec l'existant | Pas de notion de sections ; on réinvente ce que `ConfigFile` offre déjà |

**Choix : `ConfigFile`** — il est explicitement demandé, et c'est objectivement le
meilleur pour l'extensibilité réclamée (audio / affichage / accessibilité = autant de
sections). Il reste fidèle à l'esprit du projet (séparation données/IO/gestionnaire).

**Sérialisation d'un `InputEvent` dans `ConfigFile`** (une entrée par action, valeur =
liste de descripteurs) : pour chaque event, un petit `Dictionary` :
- clavier → `{ "type": "cle", "code": <int PhysicalKeycode> }`
- bouton manette → `{ "type": "bouton", "index": <int JoyButton> }`
- axe manette → `{ "type": "axe", "axe": <int JoyAxis>, "signe": <-1|1> }`

Reconstruction symétrique au chargement (tolérante aux clés absentes/inconnues,
comme `DonneesSauvegarde.DepuisDictionnaire`). On **versionne** le fichier (`version`)
pour d'éventuelles migrations, comme la sauvegarde de progression.

### B.7 Chargement au démarrage

- Un autoload `Parametres` (comme `GameState`) : dans `_Ready`, on **pose d'abord les
  défauts** (catalogue → `InputMap`), **puis** on applique par-dessus ce qui est lu
  dans le `.cfg` (les liaisons personnalisées écrasent les défauts). Ainsi une action
  absente du fichier garde son défaut (compat ascendante).
- Ordre des autoloads : `Parametres` doit configurer l'`InputMap` avant que le monde
  ne lise les entrées. Aujourd'hui c'est `GameState._Ready` qui appelle
  `ConfigurerActionsParDefaut()` — cette responsabilité **migre** vers `Parametres`
  (évolution proposée, cf. plan) pour centraliser toute la gestion d'entrées.

---

## Partie C — Synthèse de l'approche retenue

1. **Nouveau triptyque paramètres**, calqué sur le triptyque sauvegarde :
   - `CatalogueActions` (`Common/` ou `Core/`) : **source unique** des actions
     (nom, libellé FR, catégorie, défauts clavier + manette).
   - `DonneesParametres` : structure des réglages (liaisons + futurs audio/affichage),
     sérialisation `ConfigFile` tolérante + `version`.
   - `ConfigFichier` : I/O `ConfigFile` dans `user://parametres.cfg`.
   - `Parametres` (autoload singleton) : applique au `InputMap`, remap, conflits,
     reset, signaux ; **reprend** la config des actions à `GameState`.
2. **Manette** ajoutée dès les défauts (boutons + axes), gérée par les mêmes chemins
   de code que le clavier (filtrage par type d'event dans l'UI).
3. **UI extensible** : `MenuFabrique` étendu + un écran/panneau Paramètres réutilisable
   à **sections** (Touches d'abord ; Audio/Affichage/Accessibilité brancheront des
   sections sans réécriture), accessible **depuis le menu principal ET la pause**.
4. **Fidélité à l'existant** : français, commentaires de classe, `Instance`, signaux
   nommés distinctement, UI par code, `PhysicalKeycode`, tolérance aux clés absentes.

### Évolutions de l'existant que je propose (à valider, non faites unilatéralement)

- (E1) **Déplacer** la définition/paramétrage des actions de
  `GameState.ConfigurerActionsParDefaut()` vers le nouveau système (`CatalogueActions`
  + `Parametres`). `GameState` ne gère plus les entrées.
- (E2) **Supprimer le tableau `Controles` codé en dur** de `MenuPrincipal` au profit du
  catalogue (corrige au passage l'oubli de `pouvoir_glace` / `action`).
- (E3) Ajouter un nouvel **autoload `Parametres`** dans `project.godot`, ordonné avant
  le monde.

---

## Sources consultées

- API `InputMap` / `InputEvent*` (`InputEventKey`, `InputEventJoypadButton`,
  `InputEventJoypadMotion`) — documentation officielle Godot 4
  (docs.godotengine.org/en/stable/classes/class_inputmap.html) + connaissance de l'API C#.
- KoBeWi — *Godot-Input-Remap* (approche `Resource`/`ResourceSaver`, `apply_remap`,
  `restore_default_controls`) : github.com/KoBeWi/Godot-Input-Remap — comparée puis
  écartée au profit de `ConfigFile`.
- Forum Godot — *Saving gamepad inputs to config file*
  (forum.godotengine.org/t/saving-gamepad-inputs-to-config-file/21377) : confirme le
  patron `ConfigFile` pour clavier + manette.
- Guide to the Godot game engine — Input (Wikibooks) : rappel `_input` vs
  `_unhandled_input` et capture d'événement.

---

## Partie D — Extension : section Affichage

> Deuxième itération de la même méthodologie : ajouter une **section Affichage**
> (mode fenêtré / plein écran / plein écran fenêtré, résolution, VSync) qui s'insère
> dans le système de paramètres déjà écrit, sans architecture parallèle.

### D.1 Points d'insertion dans le code déjà écrit

- **Triptyque** en place : `CatalogueActions` / `DonneesParametres` / `ConfigFichier`
  + autoload `Parametres`. Fichier `ConfigFile` sectionné `user://parametres.cfg`
  (`[meta]` version, `[touches]`).
- **Persistance** : `DonneesParametres.VersConfig()` / `DepuisConfig()` = le
  (dé)sérialiseur du ConfigFile **entier** ; `ConfigFichier` = l'I/O ;
  `Parametres` = l'application. → une section Affichage s'ajoute naturellement comme
  une section `[affichage]` gérée par `DonneesParametres`.
- ⚠️ **Point d'intégration critique** : `Parametres.Sauver()` (Parametres.cs:62)
  reconstruit un `DonneesParametres` ne contenant **que** `Touches`, donc réécrit un
  `.cfg` **sans** `[affichage]` → il **écraserait** les réglages d'affichage à chaque
  remap. Il faut que `Sauver()` **préserve toutes les sections**. Solution retenue :
  `DonneesParametres` gagne des champs d'affichage, `Parametres` conserve l'état
  d'affichage **en mémoire**, et `Sauver()` écrit les deux sections en un bloc.
- **UI** : `EcranParametres` est déjà **sectionné**. Aujourd'hui
  `_sections["Affichage"] = ConstruireSectionAVenir(...)` et l'onglet est `Disabled`.
  → remplacer par `ConstruireSectionAffichage()` et passer l'onglet `actif: true`.
  Le signal `LiaisonsChangees` est propre aux touches ; l'affichage n'en a pas besoin
  (les contrôles reflètent directement l'état courant).
- **Conventions** à reconduire : français, commentaire de classe, `Instance`, UI par
  code via `MenuFabrique`, tolérance aux clés absentes, `version` du format.

### D.2 Recherche Godot 4 — affichage

- **Modes** (`DisplayServer.WindowSetMode(DisplayServer.WindowMode)`) :
  - `Windowed` — fenêtre normale.
  - `Fullscreen` — **plein écran sans bordure** (la fenêtre prend la taille de l'écran,
    pas de changement de mode vidéo, ami du multi-écran).
  - `ExclusiveFullscreen` — plein écran exclusif, **Windows uniquement** ; ailleurs
    (Linux — machine de dev) il **retombe sur `Fullscreen`** (identique).
  - Mapping FR proposé : **Fenêtré** = `Windowed`, **Plein écran fenêtré** =
    `Fullscreen` (borderless), **Plein écran** = `ExclusiveFullscreen`.
  - **Piège connu** (issues godot #70166 / #105747, forum) : repasser
    plein écran → fenêtré peut laisser une fenêtre mal dimensionnée / hors écran →
    on **redéfinit toujours la taille + on recentre** au retour en fenêtré.
- **Résolution / content scale** : le projet est en `stretch/mode="viewport"` +
  `scale_mode="integer"` (project.godot) : rendu **640×360** mis à l'échelle en **entier**
  avec letterbox. Conséquence directe : **aucun étirement ni coupure** du gameplay,
  quels que soient le mode et la taille — c'est **déjà garanti** par le projet. On ne
  touche donc **pas** au content scale. La « résolution » = **taille de la fenêtre**
  (utile en mode fenêtré). Choix retenu : proposer les **multiples entiers de 640×360**
  (1280×720, 1920×1080, 2560×1440, 3840×2160) **filtrés** à ceux qui tiennent dans
  l'écran courant (`DisplayServer.ScreenGetSize`) → pixels les plus nets, letterbox
  minimal. Godot 4 **n'énumère plus** les modes vidéo d'un écran (retiré depuis
  Godot 3) : une liste curatée est la bonne approche pour ce jeu.
- **VSync** : `DisplayServer.WindowSetVsyncMode(Enabled/Disabled)`, effet immédiat.
  Pertinent (anti-tearing) et trivial → simple bascule **on/off**.
- **Application immédiate** : `WindowSetMode` / `WindowSetSize` / `WindowSetVsyncMode`
  s'appliquent **à chaud** → **aucun redémarrage requis**, rien à marquer « redémarrage ».
- **Multi-écrans** : minimal — on applique sur l'écran courant
  (`WindowGetCurrentScreen`) et on recentre ; **pas** de sélecteur d'écran (hors périmètre).

### D.3 Périmètre retenu (strict)

Uniquement : **Mode** (3 choix), **Résolution** (tailles fenêtrées = multiples entiers
tenant dans l'écran), **VSync** (on/off). Rien d'autre (pas de FPS cap, luminosité,
qualité de particules, sélecteur d'écran…). Le content scale reste géré par le projet.

### Sources (ajout affichage)

- `DisplayServer` — documentation Godot 4 (docs.godotengine.org, class_displayserver) :
  `WindowSetMode`, `WindowMode` (Fullscreen borderless vs ExclusiveFullscreen
  Windows-only), `WindowSetSize`, `ScreenGetSize`, `WindowSetVsyncMode`.
- Forum Godot + issues #70166 / #105747 : bug de retour plein écran → fenêtré
  (nécessité de redéfinir taille + position).
