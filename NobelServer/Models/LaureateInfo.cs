namespace NobelServer.Models;

public class LaureateInfo
{
    public string Name { get; set; } = "";
    public string AwardYear { get; set; } = "";
    public string Motivation { get; set; } = "";
    public string DateAwarded { get; set; } = "";
    public int? AwardMonth { get; set; }
}