using Godot;

// Capture réutilisable d'un événement d'entrée pour le remapping. Une fois armée via
// Demarrer(clavier), elle écoute dans _Input le premier événement du périphérique
// attendu, le normalise, consomme l'événement et émet Capturee. Échap annule toujours
// et émet Annulee (Échap n'est donc jamais capturable comme liaison). N'écoute que
// lorsqu'elle est active, pour ne pas interférer avec le jeu. Gère le clavier (touche)
// et la manette (bouton, ou axe stick/gâchette au-delà de la zone morte, normalisé ±1).
public partial class CaptureEntree : Node
{
	[Signal]
	public delegate void CaptureeEventHandler(InputEvent evenement);

	[Signal]
	public delegate void AnnuleeEventHandler();

	private bool _actif;
	private bool _clavier;

	public bool EnCours => _actif;

	public override void _Ready() => SetProcessInput(false);

	// Arme la capture pour le périphérique voulu (clavier = true, manette = false).
	public void Demarrer(bool clavier)
	{
		_clavier = clavier;
		_actif = true;
		SetProcessInput(true);
	}

	// Désarme la capture (sans émettre) : à appeler si l'écran se ferme pendant l'attente.
	public void Arreter()
	{
		_actif = false;
		SetProcessInput(false);
	}

	public override void _Input(InputEvent evenement)
	{
		if (!_actif)
			return;

		// Échap annule toujours, quel que soit le périphérique attendu.
		if (evenement is InputEventKey { Pressed: true, PhysicalKeycode: Key.Escape })
		{
			GetViewport().SetInputAsHandled();
			Arreter();
			EmitSignal(SignalName.Annulee);
			return;
		}

		var capture = Extraire(evenement);
		if (capture == null)
			return;

		GetViewport().SetInputAsHandled();
		Arreter();
		EmitSignal(SignalName.Capturee, capture);
	}

	// Retourne l'événement normalisé à lier, ou null si l'événement reçu n'est pas une
	// pression valide pour le périphérique attendu.
	private InputEvent Extraire(InputEvent evenement)
	{
		if (_clavier)
		{
			if (evenement is InputEventKey { Pressed: true, Echo: false } cle)
				return new InputEventKey { PhysicalKeycode = cle.PhysicalKeycode };
			return null;
		}

		// Manette : bouton pressé, ou axe (stick/gâchette) franchissant la zone morte.
		// L'axe est normalisé à ±1 pour que Input.GetAxis fonctionne comme avec les
		// défauts, et pour une sérialisation stable.
		if (evenement is InputEventJoypadButton { Pressed: true } bouton)
			return new InputEventJoypadButton { ButtonIndex = bouton.ButtonIndex };
		if (evenement is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) >= EvenementEntree.SeuilAxe)
			return new InputEventJoypadMotion { Axis = motion.Axis, AxisValue = motion.AxisValue >= 0f ? 1 : -1 };
		return null;
	}
}
