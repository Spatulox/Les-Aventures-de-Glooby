// Marqueur des murs auxquels le joueur ne peut PAS s'agripper : ni wall jump, ni
// glisse murale, même si leur collision est par ailleurs un vrai mur vertical.
// Sert aux surfaces trop lisses ou instables pour offrir une prise (mur fondable
// prêt à disparaître...). Le joueur teste le collider du mur en contact contre
// cette interface dans EstContreMur et refuse l'accroche s'il la porte.
public interface MurNonAgrippable
{
}
