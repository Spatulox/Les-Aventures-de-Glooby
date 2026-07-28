# Locomotive jouet : même comportement que le gardien des ronces

La locomotive avait sa propre machine à états (`Roulement → Detection → Charge → Deraille`),
proche mais différente de celle du gardien : pas de sursaut de surprise, pas de verrouillage
du côté du joueur, pas de poursuite après la charge. Le schéma du gardien a été **extrait dans
une base partagée** et les deux ennemis rebranchés dessus.

Commit `75cf13f` (uniquement ces 4 fichiers — les modifs en cours côté `LivingEntity`/`Boss`/
`03-monde2.tscn` sont restées hors commit).

## 1. Nouveau — `scripts/Entities/Pnj/MechantFonceur.cs` (`: PnjMechant`)

Base des méchants « fonceurs », placée à côté de `PnjMechant` puisqu'elle est partagée entre
biomes. Le cycle, repris tel quel du gardien :

| État | Comportement |
| --- | --- |
| `Patrouille` | va-et-vient de `PnjMechant` tant que le joueur est hors de portée |
| `Detection` | **petit saut de surprise** (`ImpulsionSursaut`), puis immobile face à la cible ; côté **verrouillé** |
| `Ruee` | à `DelaiRuee`, ruée à direction figée (esquivable d'un pas de côté) |
| `Poursuite` | à `DureeDetection`, marche sur le joueur, plus lentement que lui |
| `Immobilise` | état de vulnérabilité générique, déclenché par la sous-classe |

Faiblesse commune conservée : si le joueur passe **de l'autre côté**, toute la phase recommence
(sursaut + ruée + immobilité). Hystérésis `DistanceArret` sur le changement de côté, garde-fou de
durée sur la ruée (méchant bloqué contre un mur).

Trois points d'extension pour les sous-classes :

- `RueeBorneeParJoueur` (défaut vrai) — arrêter la ruée à hauteur du joueur, ou la laisser filer
  sur toute `DistanceRuee` ;
- `InterrompreRuee()` — couper la ruée en cours (impact) ;
- `Immobiliser(duree)` — ouvrir une fenêtre de punition ;
- plus `Etat` en lecture (dégâts doublés, animation dédiée).

**Animations pilotées par l'état** dans la base : `detection` / `charge` / `etourdi` si la scène
en fournit les frames, sinon repli automatique sur le couple idle/marche de `PnjMechant` — le
gardien, sans ces dossiers, reste donc inchangé sans une ligne de code.

## 2. `GardienRonces.cs` — réduit à ses animations

Toute sa machine à états est partie dans la base ; il ne reste que `ConstruireAnimations()`.
**Comportement identique** : les défauts des exports de la base sont ses anciennes valeurs.

## 3. `LocomotiveJouet.cs` — désormais un `MechantFonceur`

Elle gagne le sursaut au repérage, le verrouillage du côté et la poursuite après charge.
Ses spécificités passent par les hooks, plus par une machine à états dupliquée :

- `RueeBorneeParJoueur => false` — la charge **dépasse** le joueur pour aller percuter le décor
  (la boucle *charge → mur → déraillement* est conservée) ;
- `InterrompreRuee()` → `IsOnWall()` : déraillement (`Immobiliser(DureeDeraillement)`) + flash ;
- `AjusterDegats` : coups **doublés** pendant le déraillement (`MultiplicateurVulnerable`).

Réglages par défaut posés au constructeur (donc visibles et surchargeables dans l'inspecteur) :
`VitesseRuee=250`, `DistanceRuee=320`, `DelaiRuee=0.7`, `DureeDetection=1.4`,
`ImpulsionSursaut=-150` (soubresaut discret, elle reste sur ses rails), `VitessePoursuite=45`.
Exports supprimés car remplacés par ceux de la base : `VitesseCharge`, `DureeChargeMax`
(aucune instance de `03-monde2.tscn` ne les surchargeait — seul `PvMax` l'est, dans la scène).

## Vérification

⚠️ **Compilation non vérifiée.** `godot --headless --build-solutions` a échoué et le
`dotnet build` de diagnostic a été interrompu ; l'erreur peut venir de ces fichiers comme des
modifications en cours (`LivingEntity.cs`, `Boss.cs`, `PorteeJoueur.cs`). À relancer.
