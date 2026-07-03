using Akka.Actor;
using System.Net;
using System.Text;
using System.Text.Json;
using NobelServer.Actors;
using NobelServer.Models;
using NobelServer.Services;

class Program
{
    private static readonly Dictionary<string, IActorRef> categoryActors = new();
    private static readonly NobelRxService nobelRxService = new NobelRxService();
    private static readonly ActorSystem actorSystem = ActorSystem.Create("NobelActorSystem");

    private static readonly Dictionary<string, string> ValidCategories = new()
    {
        { "phy", "Physics" },
        { "che", "Chemistry" },
        { "med", "Medicine" },
        { "lit", "Literature" },
        { "pea", "Peace" },
        { "eco", "Economic Sciences" }
    };

    static async Task Main()
    {
        string url = "http://localhost:8080/";

        using HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();

        using CancellationTokenSource shutdownCts = new CancellationTokenSource();

        Console.WriteLine($"Server pokrenut na: {url}");
        Console.WriteLine("Probaj u browseru: http://localhost:8080/nobel?category=phy");
        Console.WriteLine("Za gasenje servera pritisni ESC.");

        _ = Task.Run(() =>
        {
            while (!shutdownCts.IsCancellationRequested)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    Console.WriteLine("ESC pritisnut. Gasim server...");

                    shutdownCts.Cancel();
                    listener.Stop();

                    break;
                }
            }
        });

        while (!shutdownCts.IsCancellationRequested)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();

                _ = Task.Run(async () => await HandleRequest(context));
            }
            catch (HttpListenerException)
            {
                if (shutdownCts.IsCancellationRequested)
                {
                    break;
                }

                throw;
            }
            catch (ObjectDisposedException)
            {
                if (shutdownCts.IsCancellationRequested)
                {
                    break;
                }

                throw;
            }
        }

        listener.Close();

        Console.WriteLine("Server ugasen.");
    }

    static async Task HandleRequest(HttpListenerContext context)
    {
        HttpListenerResponse response = context.Response;

        try
        {
            HttpListenerRequest request = context.Request;

            Console.WriteLine();
            Console.WriteLine($"Primljen zahtev: {request.HttpMethod} {request.RawUrl}");

            string path = request.Url?.AbsolutePath ?? "";
            string? category = request.QueryString["category"];

            if (path == "/favicon.ico")
            {
                response.StatusCode = 204;
                response.OutputStream.Close();
                return;
            }

            string responseText;
            string contentType = "text/plain; charset=utf-8";

            if (path == "/nobel")
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    response.StatusCode = 400;
                    responseText = "Greska: morate proslediti category parametar. Primer: /nobel?category=phy";
                    Console.WriteLine("Zahtev nema category parametar.");
                }
                else
                {
                    category = category.Trim().ToLower();

                    if (!ValidCategories.ContainsKey(category))
                    {
                        response.StatusCode = 400;

                        responseText =
                            "Nepostojeca kategorija.\n\n" +
                            "Dozvoljene kategorije su:\n" +
                            "phy - Physics\n" +
                            "che - Chemistry\n" +
                            "med - Medicine\n" +
                            "lit - Literature\n" +
                            "pea - Peace\n" +
                            "eco - Economic Sciences\n";

                        Console.WriteLine($"Neispravna kategorija: {category}");
                    }
                    else
                    {
                        string categoryName = ValidCategories[category];

                        Console.WriteLine("--------------------------------------------------");
                        Console.WriteLine($"Obrada zahteva za kategoriju: {category} - {categoryName}");

                        IActorRef nobelActor = GetOrCreateActor(category);

                        NobelResult currentResult = await nobelActor.Ask<NobelResult>(
                            new GetResults(category, categoryName),
                            TimeSpan.FromSeconds(10)
                        );

                        if (currentResult.TotalLaureates == 0)
                        {
                            Console.WriteLine("Rx.NET poziva API...");
                            await nobelRxService.SendLaureatesToActorAsync(category, nobelActor);
                        }

                        Console.WriteLine("Rx.NET emituje podatke aktoru...");

                        Console.WriteLine("Podaci poslati aktoru. Ceka se rezultat od aktora...");

                        NobelResult result = await nobelActor.Ask<NobelResult>(
                            new GetResults(category, categoryName),
                            TimeSpan.FromSeconds(10)
                        );

                        response.StatusCode = 200;
                        contentType = "application/json; charset=utf-8";

                        responseText = JsonSerializer.Serialize(
                            result,
                            new JsonSerializerOptions
                            {
                                WriteIndented = true
                            }
                        );

                        Console.WriteLine($"Uspesno obradjeno laureata: {result.TotalLaureates}");

                        if (result.MonthWithMostAwards.HasValue)
                        {
                            Console.WriteLine(
                                $"Mesec sa najvise dodela: {result.MonthWithMostAwardsName} ({result.MonthWithMostAwards})"
                            );
                        }
                        else
                        {
                            Console.WriteLine("Nema dostupnih datuma dodele.");
                        }

                        Console.WriteLine("Zahtev uspesno zavrsen.");
                        Console.WriteLine("--------------------------------------------------");
                    }
                }
            }
            else
            {
                response.StatusCode = 404;
                responseText = "Ruta nije pronadjena. Koristi: /nobel?category=...";
                Console.WriteLine("Nepostojeca ruta.");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseText);

            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Doslo je do greske pri obradi zahteva:");
            Console.WriteLine(ex.Message);

            response.StatusCode = 500;

            string responseText = "Doslo je do greske na serveru: " + ex.Message;
            byte[] buffer = Encoding.UTF8.GetBytes(responseText);

            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    static IActorRef GetOrCreateActor(string category)
{
    if (!categoryActors.ContainsKey(category))
    {
        IActorRef actor = actorSystem.ActorOf(
            Props.Create(() => new NobelActor()),
            "nobel-actor-" + category
        );

        categoryActors[category] = actor;
    }

    return categoryActors[category];
}
}