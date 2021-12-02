namespace LearningNeo4j
{
    public class FlightService
    {
        private readonly FlightRegistry registry;

        public FlightService()
        {
            registry = new();
        }

        public async Task EraseEverythingAsync()
        {
            await registry.EraseAllFlightsAsync();
            await registry.EraseAllStationsAsync();
        }

        public async Task<int> RegisterStationAsync(string name) => await registry.RegisterStationAsync(name);

        public async Task RegisterFlightAsync(int originId, int destinationId, DateTime departure, DateTime arrival)
        {
            var stationsWithFlightToOrigin = await registry.GetStationIdsWithFlightToTargetAsync(originId);
            var stationsWithFlightFromDestination = await registry.GetStationIdsWithFlightFromAsync(destinationId);

            // Direct flight
            await registry.RegisterFlightAsync(originId, destinationId, true);

            // Transitive flights from flights arriving at origin
            foreach (var stationId in stationsWithFlightToOrigin.Where(x => x != destinationId))
            {
                // await registry.RegisterFlightAsync(stationId, destinationId, false);
            }

            // Transitive flights from flights departing from origin
            foreach (var stationId in stationsWithFlightFromDestination.Where(destId => destId != destinationId))
            {
                //await registry.RegisterFlightAsync(originId, destId, false);
            }
        }

        public async Task<Dictionary<int, List<List<string>>>> GetFlightsWithTransfersAsync(string from, string to, int minTransfers = 0, int maxTransfers = 10)
        {
            var results = new Dictionary<int, List<List<string>>>();

            for (var i = minTransfers; i <= maxTransfers; i++)
            {
                var resultsWithITransfers = await registry.GetFlightsWithTransfersAsync(from, to, i);

                results[i] = resultsWithITransfers.ToList();
            }

            return results;
        }
    }
}