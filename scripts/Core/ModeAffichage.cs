// Modes d'affichage proposés dans les paramètres, indépendants de l'API Godot :
// Parametres les traduit en DisplayServer.WindowMode. Sérialisés par leur valeur
// entière dans la section [affichage] du ConfigFile — ne pas réordonner les membres
// (les sauvegardes existantes stockent l'entier).
public enum ModeAffichage
{
	Fenetre,            // fenêtre normale (redimensionnable via la résolution)
	PleinEcran,         // plein écran exclusif (Windows ; ailleurs = plein écran fenêtré)
	PleinEcranFenetre,  // plein écran sans bordure (borderless), ami du multi-écran
}
