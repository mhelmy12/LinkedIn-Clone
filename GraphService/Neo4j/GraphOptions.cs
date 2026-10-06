using System;

namespace GraphService.Neo4j;

public class GraphOptions
{
    public const string SectionName = "Graph";

    public int MaxMutualConnections { get; set; } = 50;
    public int MaxSuggestions { get; set; } = 20;
    public int MaxDegreesOfSeparation { get; set; } = 6;
}