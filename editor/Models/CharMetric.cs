namespace editor.Models;

public record CharMetrics(float LineHeight, float Padding)
{
    public int Quantity { get; set; } = 1;
};