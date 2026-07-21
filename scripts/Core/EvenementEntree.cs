using Godot;

// Helpers réutilisables autour des InputEvent : (dé)sérialisation vers un
// descripteur simple stockable dans un ConfigFile, comparaison pour la détection
// de conflits, et libellé lisible (clavier AZERTY/QWERTY et manette). Centralise
// toute la connaissance du format d'un événement d'entrée pour ne pas la disperser
// entre le catalogue, la persistance et l'UI.
public static class EvenementEntree
{
	// Seuil au-delà duquel un mouvement d'axe manette (stick / gâchette) est
	// considéré comme une pression volontaire (capture) et non du bruit de zone morte.
	public const float SeuilAxe = 0.5f;

	// Sérialise un InputEvent en dictionnaire Godot (stockable tel quel dans un
	// ConfigFile). Retourne null pour un type non géré.
	public static Godot.Collections.Dictionary Serialiser(InputEvent evenement)
	{
		switch (evenement)
		{
			case InputEventKey cle:
				return new Godot.Collections.Dictionary { ["type"] = "cle", ["code"] = (int)cle.PhysicalKeycode };
			case InputEventJoypadButton bouton:
				return new Godot.Collections.Dictionary { ["type"] = "bouton", ["index"] = (int)bouton.ButtonIndex };
			case InputEventJoypadMotion axe:
				return new Godot.Collections.Dictionary { ["type"] = "axe", ["axe"] = (int)axe.Axis, ["signe"] = axe.AxisValue >= 0f ? 1 : -1 };
			default:
				return null;
		}
	}

	// Reconstruit un InputEvent depuis un descripteur. Tolérant : retourne null si le
	// type est inconnu ou une clé manque (même esprit que DonneesSauvegarde).
	public static InputEvent Deserialiser(Godot.Collections.Dictionary d)
	{
		if (d == null || !d.TryGetValue("type", out var type))
			return null;

		switch ((string)type)
		{
			case "cle":
				return d.TryGetValue("code", out var code)
					? new InputEventKey { PhysicalKeycode = (Key)(int)code }
					: null;
			case "bouton":
				return d.TryGetValue("index", out var index)
					? new InputEventJoypadButton { ButtonIndex = (JoyButton)(int)index }
					: null;
			case "axe":
				if (!d.TryGetValue("axe", out var axe))
					return null;
				int signe = d.TryGetValue("signe", out var s) ? (int)s : 1;
				return new InputEventJoypadMotion { Axis = (JoyAxis)(int)axe, AxisValue = signe };
			default:
				return null;
		}
	}

	// Vrai si deux événements désignent la même entrée physique (même touche, même
	// bouton, ou même axe + direction) : base de la détection de conflits.
	public static bool Correspond(InputEvent a, InputEvent b)
	{
		return (a, b) switch
		{
			(InputEventKey ka, InputEventKey kb) => ka.PhysicalKeycode == kb.PhysicalKeycode,
			(InputEventJoypadButton ba, InputEventJoypadButton bb) => ba.ButtonIndex == bb.ButtonIndex,
			(InputEventJoypadMotion ma, InputEventJoypadMotion mb) =>
				ma.Axis == mb.Axis && Mathf.Sign(ma.AxisValue) == Mathf.Sign(mb.AxisValue),
			_ => false,
		};
	}

	public static bool EstClavier(InputEvent evenement) => evenement is InputEventKey;

	public static bool EstManette(InputEvent evenement) =>
		evenement is InputEventJoypadButton or InputEventJoypadMotion;

	// Libellé lisible d'un événement (pour l'UI). Clavier : la position physique est
	// retraduite vers l'étiquette réelle du clavier de l'utilisateur (W physique ->
	// « Z » en AZERTY). Manette : nom français du bouton / de l'axe + direction.
	public static string Libelle(InputEvent evenement)
	{
		switch (evenement)
		{
			case InputEventKey cle:
				var etiquette = DisplayServer.KeyboardGetLabelFromPhysical(cle.PhysicalKeycode);
				if (etiquette == Key.None)
					etiquette = cle.PhysicalKeycode;
				return OS.GetKeycodeString(etiquette);
			case InputEventJoypadButton bouton:
				return LibelleBouton(bouton.ButtonIndex);
			case InputEventJoypadMotion axe:
				return LibelleAxe(axe.Axis, axe.AxisValue);
			default:
				return "?";
		}
	}

	// Nom lisible d'un bouton de manette (convention Xbox, la plus répandue).
	private static string LibelleBouton(JoyButton bouton) => bouton switch
	{
		JoyButton.A => "Manette A",
		JoyButton.B => "Manette B",
		JoyButton.X => "Manette X",
		JoyButton.Y => "Manette Y",
		JoyButton.LeftShoulder => "Gâchette L",
		JoyButton.RightShoulder => "Gâchette R",
		JoyButton.LeftStick => "Stick G (clic)",
		JoyButton.RightStick => "Stick D (clic)",
		JoyButton.Start => "Start",
		JoyButton.Back => "Select",
		JoyButton.Guide => "Guide",
		JoyButton.DpadUp => "Croix ↑",
		JoyButton.DpadDown => "Croix ↓",
		JoyButton.DpadLeft => "Croix ←",
		JoyButton.DpadRight => "Croix →",
		_ => $"Manette {(int)bouton}",
	};

	// Nom lisible d'un axe de manette avec sa direction.
	private static string LibelleAxe(JoyAxis axe, float valeur)
	{
		bool positif = valeur >= 0f;
		return axe switch
		{
			JoyAxis.LeftX => positif ? "Stick G →" : "Stick G ←",
			JoyAxis.LeftY => positif ? "Stick G ↓" : "Stick G ↑",
			JoyAxis.RightX => positif ? "Stick D →" : "Stick D ←",
			JoyAxis.RightY => positif ? "Stick D ↓" : "Stick D ↑",
			JoyAxis.TriggerLeft => "Gâchette L (analog.)",
			JoyAxis.TriggerRight => "Gâchette R (analog.)",
			_ => $"Axe {(int)axe}",
		};
	}
}
