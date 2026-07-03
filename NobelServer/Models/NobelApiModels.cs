using System.Text.Json.Serialization;

namespace NobelServer.Models;

public class NobelApiResponse
{
    [JsonPropertyName("nobelPrizes")]
    public List<NobelPrizeDto> NobelPrizes { get; set; } = new();
}

public class NobelPrizeDto
{
    [JsonPropertyName("awardYear")]
    public string AwardYear { get; set; } = "";

    [JsonPropertyName("dateAwarded")]
    public string? DateAwarded { get; set; }

    [JsonPropertyName("laureates")]
    public List<NobelLaureateDto> Laureates { get; set; } = new();
}

public class NobelLaureateDto
{
    [JsonPropertyName("knownName")]
    public MultiLangText? KnownName { get; set; }

    [JsonPropertyName("fullName")]
    public MultiLangText? FullName { get; set; }

    [JsonPropertyName("orgName")]
    public MultiLangText? OrgName { get; set; }

    [JsonPropertyName("motivation")]
    public MultiLangText? Motivation { get; set; }
}

public class MultiLangText
{
    [JsonPropertyName("en")]
    public string? En { get; set; }
}