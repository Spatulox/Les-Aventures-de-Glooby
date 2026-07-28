using System;
using System.Collections.Generic;
using Godot;

// Cage du Père Noël, posée en bout de l'arène finale. Dans la fin CACHÉE, on le voit
// prisonnier pendant tout le combat contre le Lutin Mecha — c'est ce qui explique
// visuellement qu'un lutin défende l'atelier — et on le délivre une fois le boss tombé ;
// il remet alors son Contenu (le pantalon). Dans la fin normale, où le Père Noël est
// lui-même le boss, la cage n'a aucun sens : elle se retire d'elle-même (MemoireRequise).
//
// Parlante (Talkative) sur le modèle de PanneauBois : tout le rappel de touche et la
// bulle viennent du DeclencheurDialogue enfant, la cage ne fait qu'effacer son sprite,
// mettre le prisonnier debout (PnjLibere) et lâcher son contenu. Le verrou de
// progression reprend celui de PorteInterne (BossRequis + GameState.EstBossVaincu).
public partial class CagePereNoel : Node2D, Talkative
{
	// Mémoire GameState exigée pour que la cage EXISTE (vide = toujours présente).
	// Même clé que le ZoneBoss.MemoireRequise de l'arène : la cage n'appartient qu'à
	// la branche où le Père Noël est prisonnier au lieu d'être le boss.
	[Export] public string MemoireRequise = "";

	// Boss à vaincre pour pouvoir ouvrir la cage (vide = ouvrable d'emblée). Le nom est
	// celui passé à GameState.MarquerBossVaincu, donc le ZoneBoss.NomChoisi de l'arène.
	[Export] public string BossRequis = "";

	// Texture de la cage une fois ouverte (celle de la cage fermée vit dans la scène).
	// VIDE (défaut) = le prop s'efface purement et simplement à l'ouverture : c'est le
	// cas quand le prisonnier sort pour de bon (PnjLibere), sans quoi on continuerait de
	// le voir derrière ses barreaux alors qu'il est censé être dehors.
	[Export] public Texture2D TextureOuverte;

	// Le prisonnier, DEBOUT ET LIBRE : la scène de PNJ qui prend la place de la cage une
	// fois celle-ci ouverte (vide = personne n'en sort). C'est un vrai PNJ animé, qui a
	// donc ses propres répliques — la cage, elle, se tait dès qu'elle est ouverte.
	[Export] public PackedScene PnjLibere;

	// Où le libéré se plante, par rapport au centre de la cage (son origine est au CENTRE
	// de son sprite, celle d'un PNJ à ses pieds). Réglé pour tomber JUSTE AU-DESSUS du sol
	// de l'arène : un PNJ est un corps physique, il finit de descendre tout seul, alors
	// qu'apparaître sous le plancher le coincerait dedans.
	[Export] public Vector2 DecalagePnjLibere = new(0f, 50f);

	// Objet libéré à l'ouverture, posé en enfant de la cage (vide = rien à donner).
	[Export] public PackedScene Contenu;
	[Export] public Vector2 DecalageContenu = new(0f, 40f);

	// ---- Volet parlant (Talkative) ----
	// Ce que dit le prisonnier tant que le boss est debout, puis au moment où on le
	// délivre. Deux jeux de répliques et non un seul : la cage n'a pas le même propos
	// avant et après, et c'est le seul état qu'elle a besoin d'exposer.
	[Export] public string[] LignesAvant = Array.Empty<string>();
	[Export] public string[] LignesApres = Array.Empty<string>();
	[Export] public Vector2 AncrageBulle = new(0f, -110f);

	private Sprite2D _sprite;
	private bool _ouverte;

	// Le boss est-il tombé ? C'est ce qui fait passer la cage de « supplique » à
	// « délivrance ».
	private bool Deverrouillee =>
		string.IsNullOrEmpty(BossRequis) || GameState.Instance?.EstBossVaincu(BossRequis) == true;

	public override void _Ready()
	{
		// Branche sans prisonnier : la cage n'a rien à faire dans la salle.
		if (!string.IsNullOrEmpty(MemoireRequise) && GameState.Instance?.EstConsomme(MemoireRequise) != true)
		{
			QueueFree();
			return;
		}

		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		// Fin de conversation, et seulement si elle est allée à son terme : s'éloigner
		// en plein milieu ne doit pas ouvrir la cage à distance.
		foreach (var enfant in GetChildren())
		{
			if (enfant is DeclencheurDialogue declencheur)
			{
				declencheur.DialogueTermine += SurDialogueTermine;
				break;
			}
		}
	}

	private void SurDialogueTermine(bool complet)
	{
		if (!complet || _ouverte || !Deverrouillee)
			return;

		_ouverte = true;

		// Sans texture d'après, la cage disparaît : le prisonnier est dehors, garder son
		// image derrière les barreaux le montrerait à deux endroits à la fois. Le nœud,
		// lui, reste (il porte le libéré et le contenu).
		if (_sprite != null)
		{
			if (TextureOuverte != null)
				_sprite.Texture = TextureOuverte;
			else
				_sprite.Visible = false;
		}

		PoserEnfant(PnjLibere, DecalagePnjLibere);
		PoserEnfant(Contenu, DecalageContenu);
	}

	// Fait apparaître une scène à l'endroit de la cage (libéré, contenu...). Différé : on
	// peut arriver ici depuis une sortie de zone, donc en plein flush des requêtes
	// physiques, où une forme de collision ne peut pas être ajoutée.
	private void PoserEnfant(PackedScene scene, Vector2 decalage)
	{
		if (scene == null)
			return;

		var noeud = scene.Instantiate<Node2D>();
		noeud.Position = decalage;
		CallDeferred(Node.MethodName.AddChild, noeud);
	}

	// ---- Talkative ----

	public IReadOnlyList<string> Dialogue => Deverrouillee ? LignesApres : LignesAvant;

	public Vector2 PointBulle => ToGlobal(AncrageBulle);

	public bool Aleatoire => false;

	// Jamais au passage : délivrer quelqu'un se fait volontairement, à la touche.
	public bool DeclencheAuPassage => false;

	// Une fois ouverte, la cage n'a plus rien à dire (le prisonnier est dehors).
	public bool PeutParler() => !_ouverte;

	public void SurDebutDialogue() { }

	public void SurFinDialogue() { }
}
