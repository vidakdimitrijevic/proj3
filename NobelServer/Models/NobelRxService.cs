using Akka.Actor;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using NobelServer.Models;

namespace NobelServer.Services;

public class NobelRxService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task SendLaureatesToActorAsync(string category, IActorRef actor)
    {
        string url =
            $"https://api.nobelprize.org/2.1/nobelPrizes?limit=1000&nobelPrizeCategory={Uri.EscapeDataString(category)}";

        string json = await httpClient.GetStringAsync(url);

        NobelApiResponse? apiResponse = JsonSerializer.Deserialize<NobelApiResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        if (apiResponse == null)
        {
            return;
        }

        var observable = apiResponse.NobelPrizes
            .ToObservable()
            .SubscribeOn(TaskPoolScheduler.Default)
            .ObserveOn(TaskPoolScheduler.Default)
            .Where(prize => prize.Laureates.Count > 0)
            .SelectMany(prize =>
                prize.Laureates
                    .ToObservable()
                    .Select(laureate => MapToLaureateInfo(prize, laureate))
            )
            .Do(laureate =>
            {
                actor.Tell(new ProcessLaureate(laureate));
            });

        await observable.ToList().ToTask();
    }

    private LaureateInfo MapToLaureateInfo(NobelPrizeDto prize, NobelLaureateDto laureate)
    {
        string name =
            laureate.KnownName?.En ??
            laureate.FullName?.En ??
            laureate.OrgName?.En ??
            "Nepoznato ime";

        string motivation = laureate.Motivation?.En ?? "Nema motivacije";

        int? month = null;

        if (DateTime.TryParse(prize.DateAwarded, out DateTime parsedDate))
        {
            month = parsedDate.Month;
        }

        return new LaureateInfo
        {
            Name = name,
            AwardYear = prize.AwardYear,
            Motivation = motivation,
            DateAwarded = prize.DateAwarded ?? "Nema datuma",
            AwardMonth = month
        };
    }
}