namespace editor.Models;

public record CharMetric(float LineHeight, float Padding)
{
    public int Quantity { get; set; } = 1;
}