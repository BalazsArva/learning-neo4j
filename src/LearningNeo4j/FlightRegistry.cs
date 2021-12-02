using Neo4j.Driver;

namespace LearningNeo4j
{
    public class FlightRegistry
    {
        private const string GoesToEdgeType = "GoesTo";

        private readonly IDriver driver;

        public FlightRegistry()
        {
            driver = GraphDatabase.Driver("neo4j://localhost:7687", AuthTokens.Basic("neo4j", "Neo4j"));
        }

        public async Task EraseAllFlightsAsync()
        {
            using var session = driver.AsyncSession();

            await session.WriteTransactionAsync(async tr => await tr.RunAsync($"MATCH (n:Station)-[r:{GoesToEdgeType}]->(m:Station) DELETE r"));
        }

        public async Task EraseAllStationsAsync()
        {
            using var session = driver.AsyncSession();

            await session.WriteTransactionAsync(async tr => await tr.RunAsync("MATCH (n:Station) DELETE n"));
        }

        public async Task<int> RegisterStationAsync(string name)
        {
            using var session = driver.AsyncSession();

            return await session.WriteTransactionAsync(async tr =>
            {
                var cursor = await tr.RunAsync("CREATE (st:Station { Name: $name }) RETURN id(st)", new { name = name });
                var outputs = await cursor.ToListAsync();

                return int.Parse(outputs.Single().Values["id(st)"].ToString()!);
            });
        }

        public async Task RegisterFlightAsync(int originId, int destinationId, bool isDirect)
        {
            using var session = driver.AsyncSession();

            await session.WriteTransactionAsync(async tr =>
            {
                var cursor = await tr.RunAsync(
                    $"MATCH (origin:Station), (dest:Station) " +
                    $"WHERE id(origin) = $originId AND id(dest) = $destId " +
                    $"CREATE (origin)-[r:{GoesToEdgeType} {{ IsDirect: $isDirect }}]->(dest) RETURN type(r)",
                    new { originId = originId, destId = destinationId, isDirect = isDirect });

                var outputs = await cursor.ToListAsync();
            });
        }

        public async Task<IEnumerable<int>> GetStationIdsWithFlightToTargetAsync(int destinationId)
        {
            using var session = driver.AsyncSession();

            var flightsArrivingAtOrigin = await session.ReadTransactionAsync(async tr =>
            {
                var cursor = await tr.RunAsync(
                    $"MATCH (src:Station)-[:{GoesToEdgeType}]->(dest:Station) " +
                    $"WHERE id(dest) = $destinationId " +
                    $"RETURN id(src) AS Result",
                    new { destinationId = destinationId });

                var outputs = await cursor.ToListAsync();

                return outputs.Select(x => int.Parse(x[0].ToString()!)).ToList();
            });

            return flightsArrivingAtOrigin;
        }

        public async Task<IEnumerable<int>> GetStationIdsWithFlightFromAsync(int sourceId)
        {
            using var session = driver.AsyncSession();

            var flightsLeavingFromSource = await session.ReadTransactionAsync(async tr =>
            {
                var cursor = await tr.RunAsync(
                    $"MATCH (src:Station)-[:{GoesToEdgeType}]->(dest:Station) " +
                    $"WHERE id(src) = $sourceId " +
                    $"RETURN id(dest) AS Result",
                    new { sourceId = sourceId });

                var outputs = await cursor.ToListAsync();

                return outputs.Select(x => int.Parse(x[0].ToString()!)).ToList();
            });

            return flightsLeavingFromSource;
        }

        public async Task<List<List<string>>> GetFlightsWithTransfersAsync(string from, string to, int numberOfTransfers)
        {
            const string pathName = "p";
            const string fromParamName = "from";
            const string toParamName = "to";

            using var session = driver.AsyncSession();

            var segments = Enumerable.Repeat("(:Station)", numberOfTransfers).Prepend("(start:Station)").Append("(end:Station)");
            var path = string.Join($"-[:{GoesToEdgeType}]->", segments);

            return await session.ReadTransactionAsync(
                async tr =>
                {
                    var results = new List<List<string>>();

                    var cursor = await tr.RunAsync(
                        $"MATCH {pathName}={path} WHERE start.Name = ${fromParamName} AND end.Name = ${toParamName} RETURN {pathName}",
                        new Dictionary<string, object>
                        {
                            [fromParamName] = from,
                            [toParamName] = to,
                        });

                    while (await cursor.FetchAsync())
                    {
                        var path = (IPath)cursor.Current[pathName];
                        var stops = new List<string>(path.Nodes.Count);

                        foreach (var node in path.Nodes)
                        {
                            stops.Add(node.Properties["Name"].ToString()!);
                        }

                        results.Add(stops);
                    }

                    return results;
                });
        }
    }
}