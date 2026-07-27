// Noms des couches de données personnalisées du TileSet du monde (baké dans
// 01-monde1.tscn). Le TileSet était autrefois construit par code ici à partir des
// feuilles PixelLab, puis il a été baké dans la scène et le générateur débranché ;
// seules ces clés survivent, lues par Player pour piloter glisse et glace fragile.
public static class TileSetFabrique
{
	public const string DonneeIsIce = "is_ice";
	public const string DonneeIsFragile = "is_fragile";
}
