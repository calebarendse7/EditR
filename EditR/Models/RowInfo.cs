namespace EditR.Models;

public class RowInfo
{
    public SortedDictionary<int, CharMetric> SizeByMetric { get; } = [];
    public int LargestSize { get; set; }
    public float Height { get; set; }
    public float RowOffset { get; set; }
    public float Row { get; set; }
    public int Start { get; set; }
}