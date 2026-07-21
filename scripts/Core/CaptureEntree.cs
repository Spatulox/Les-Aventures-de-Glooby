using Godot;

// Capture réutilisable d'un événement d'entrée pour le remapping. Une fois armée via
// Demarrer(clavier), elle écoute dans _Input le premier événement du périphérique
// attendu, le normalise, consomme l'événement et émet Capturee. Échap annule toujours
// et émet Annulee (Échap n'est donc jamais capturable comme liaison). N'écoute que
// lorsqu'elle est active, pour ne pas interférer avec le jeu. Le remapping manette
// (boutons + axes) est branché à l'itération suivante.
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

		// Périphérique manette : branché à l'itération 3 (boutons + axes).
		return null;
	}
}
