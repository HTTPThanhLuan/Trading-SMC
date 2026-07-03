namespace Trading.Smc.v2;

public sealed class SwingPoint
{
    public int Index { get; set; }
    public DateTime Time { get; set; }
    public SwingType Type { get; set; }
    public decimal Level { get; set; }
}
