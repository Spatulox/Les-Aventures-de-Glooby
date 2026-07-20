using Godot;

// Petit morceau de banquise servant de sol surélevé : plaque de glace, bloc
// empilable ou congère. C'est un sol *plein*, pas une plateforme traversable :
// il reste sur le layer de collision par défaut (layer 1), donc bas+saut ne
// permet pas de tomber au travers — ces morceaux surplombent souvent un trou
// mortel, où une traversée serait une mort accidentelle.
// Chaque type a sa propre scène, qui porte sprite ET collision : elle est la
// seule source de vérité, le script ne réapplique rien au runtime.
public partial class PlateformeBanquise : StaticBody2D
{
	public enum TypeElement { Plaque, Bloc, Congere }

	[Export] public TypeElement Type = TypeElement.Plaque;
}
