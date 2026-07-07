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
