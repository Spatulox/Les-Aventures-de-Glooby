// Marqueur des entités vivantes amicales (certains PNJ). Une entité qui implémente
// cette interface ne peut subir AUCUN dégât, quelle qu'en soit la source (boule de
// neige, pouvoir de chaleur...), même si elle est par ailleurs Damageable.
// Les sources de dégâts appliquent leurs coups via Damageable.Infliger, qui court-
// circuite systématiquement les cibles amicales.
public interface FriendlyLivingEntity
{
}
