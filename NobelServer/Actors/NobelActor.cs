using Akka.Actor;
using NobelServer.Models;

namespace NobelServer.Actors;

public class NobelActor : ReceiveActor
{
    private readonly List<LaureateInfo> laureates = new();
    private readonly Dictionary<int, int> monthDistribution = new();

    public NobelActor()
    {
        Receive<ProcessLaureate>(message =>
        {
            LaureateInfo laureate = message.Laureate;

            laureates.Add(laureate);

            if (laureate.AwardMonth.HasValue)
            {
                int month = laureate.AwardMonth.Value;

                if (!monthDistribution.ContainsKey(month))
                {
                    monthDistribution[month] = 0;
                }

                monthDistribution[month]++;
            }
        });

        Receive<GetResults>(message =>
        {
            int? monthWithMostAwards = null;

            if (monthDistribution.Count > 0)
            {
                monthWithMostAwards = monthDistribution
                    .OrderByDescending(pair => pair.Value)
                    .First()
                    .Key;
            }

            NobelResult result = new NobelResult
            {
                Category = message.Category,
                CategoryName = message.CategoryName,
                TotalLaureates = laureates.Count,

                MonthDistribution = monthDistribution
                .OrderBy(pair => pair.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value),

                MonthDistributionByName = monthDistribution
                    .OrderBy(pair => pair.Key)
                    .ToDictionary(pair => GetMonthName(pair.Key), pair => pair.Value),

                MonthWithMostAwards = monthWithMostAwards,
                MonthWithMostAwardsName = GetMonthName(monthWithMostAwards),

                Laureates = laureates
            };

            Sender.Tell(result);
        });
    }

    private static string GetMonthName(int? month)
    {
        return month switch
        {
            1 => "Januar",
            2 => "Februar",
            3 => "Mart",
            4 => "April",
            5 => "Maj",
            6 => "Jun",
            7 => "Jul",
            8 => "Avgust",
            9 => "Septembar",
            10 => "Oktobar",
            11 => "Novembar",
            12 => "Decembar",
            _ => "Nema dostupnog meseca"
        };
    }
}