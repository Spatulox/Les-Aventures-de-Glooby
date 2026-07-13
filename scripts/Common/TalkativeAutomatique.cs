// Talkative dont la bulle défile toute seule, sans appui de touche : bavardage
// d'ambiance. Le moteur DeclencheurDialogue détecte ce type et fait défiler les
// lignes sur minuteur (au lieu d'attendre la touche « action »). La bulle reste
// possédée par le moteur ; Incrementer/Cacher sont des hooks de notification.
public interface TalkativeAutomatique : Talkative
{
	// Intervalle (secondes) entre deux lignes en défilement automatique.
	float IntervalleAuto { get; }

	// Hook « ligne suivante » : le moteur l'appelle à chaque avancée (réaction PNJ).
	void Incrementer();

	// Hook « bulle cachée » : appelé quand le cycle s'arrête (joueur éloigné / fin).
	void Cacher();
}
