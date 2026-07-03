namespace NobelServer.Models;

public record ProcessLaureate(LaureateInfo Laureate);

public record GetResults(string Category, string CategoryName);

public class NobelResult
{
    public string Category { get; set; } = "";
    public string CategoryName { get; set; } = "";

    public int TotalLaureates { get; set; }

    public Dictionary<int, int> MonthDistribution { get; set; } = new();

    public Dictionary<string, int> MonthDistributionByName { get; set; } = new();

    public int? MonthWithMostAwards { get; set; }

    public string MonthWithMostAwardsName { get; set; } = "Nema dostupnog meseca";

    public List<LaureateInfo> Laureates { get; set; } = new();
}