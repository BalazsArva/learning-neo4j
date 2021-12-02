using LearningNeo4j;

var flightService = new FlightService();

var stations = new Dictionary<string, int>
{
    ["Budapest"] = -1,
    ["Vienna"] = -1,
    ["Berlin"] = -1,
    ["Madrid"] = -1,
    ["London"] = -1,
    ["Paris"] = -1,
    ["Rome"] = -1,
    ["Stockholm"] = -1,
    ["Oslo"] = -1,
    ["Koppenhagen"] = -1,
    ["Brussels"] = -1,
    ["Prague"] = -1,
    ["The Hague"] = -1,
    ["Amsterdam"] = -1,
    ["Dublin"] = -1,
    ["Glassgow"] = -1,
    ["Munich"] = -1,
    ["Hamburg"] = -1,
    ["Lisboa"] = -1,
    ["Athens"] = -1,
    ["Warsaw"] = -1,
    ["Bucharest"] = -1,
    ["Zurich"] = -1,
    ["Riga"] = -1,
    ["Tallinn"] = -1,
    ["Helsinki"] = -1,
    ["Frankfurt"] = -1,
};

await flightService.EraseEverythingAsync();

foreach (var stationName in stations.Keys)
{
    stations[stationName] = await flightService.RegisterStationAsync(stationName);
}

var random = new Random();
var keySequence = stations.Keys.ToList();

var baseDate = DateTime.Now.AddDays(1);
var minutesPerWeek = (int)TimeSpan.FromDays(7).TotalSeconds;

var connections = keySequence
    .SelectMany(x => keySequence.Select(y => new { Origin = x, Destination = y, }))
    .Where(x => x.Origin != x.Destination)
    .OrderBy(_ => random.Next(0, int.MaxValue))
    .Take((keySequence.Count * keySequence.Count) / 4)
    .Select(x => new { x.Origin, x.Destination, Departure = baseDate.AddMinutes(random.Next(minutesPerWeek)), DurationInMinutes = random.Next(60, 180) })
    .Select(x => new { x.Origin, x.Destination, x.Departure, Arrival = x.Departure.AddMinutes(x.DurationInMinutes) })
    .ToList();

for (var i = 0; i < keySequence.Count; i++)
{
    for (var j = 0; j < keySequence.Count; j++)
    {
        var diff = Math.Abs(i - j);
        if (diff == 1 || diff == 3)
        {
            var originId = stations[keySequence[i]];
            var destinationId = stations[keySequence[j]];

            await flightService.RegisterFlightAsync(originId, destinationId, DateTime.Today.AddDays(1), DateTime.Today.AddDays(1).AddHours(1));
        }
    }
}

/*

foreach (var connection in connections)
{
    var originId = stations[connection.Origin];
    var destinationId = stations[connection.Destination];

    await flightService.RegisterFlightAsync(originId, destinationId, connection.Departure, connection.Arrival);
}

 */

var results = await flightService.GetFlightsWithTransfersAsync("Budapest", "Paris", 0, 6);
foreach (var numberOfTransfers in results.Keys)
{
    Console.WriteLine($"Flights with {numberOfTransfers} transfers:");
    foreach (var flight in results[numberOfTransfers])
    {
        var route = string.Join(" -> ", flight);

        Console.WriteLine($"  - {route}");
    }

    Console.WriteLine();
}