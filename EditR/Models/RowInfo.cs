namespace EditR.Models;

public class RowInfo
{
    public SortedDictionary<int, CharMetric> SizeByMetric { get; } = [];
    public required float Height { get; set; }
    public float RowStart { get; set; }
    public int Index {get; set;}
}