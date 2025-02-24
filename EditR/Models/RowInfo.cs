namespace EditR.Models;

public class RowInfo
{
    public SortedDictionary<int, CharMetric> SizeByMetric { get; } = [];
    public required float Height { get; set; }
}