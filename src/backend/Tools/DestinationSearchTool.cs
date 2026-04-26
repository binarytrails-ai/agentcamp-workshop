using System.ComponentModel;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using OpenAI.Embeddings;

namespace ContosoTravelAgent.Host.Tools;

/// <summary>
/// Destination search tool for finding travel destinations
/// Searches destinations in Cosmos DB based on user queries
/// </summary>
public class DestinationSearchTool
{
    private readonly Database _database;
    private readonly string _containerName;
    private readonly ILogger<DestinationSearchTool> _logger;
    private readonly EmbeddingClient _embeddingClient;

    public DestinationSearchTool(
        Database database,
        string containerName,
        ILogger<DestinationSearchTool> logger,
        EmbeddingClient embeddingClient)
    {
        _database = database;
        _containerName = containerName;
        _logger = logger;
        _embeddingClient = embeddingClient;
    }

    /// <summary>
    /// Searches for travel destinations based on user preferences using semantic vector search
    /// </summary>
    [Description("Search for travel destinations in Australia and New Zealand. Returns destination options based on interests, activities, or travel style (e.g., 'beaches', 'adventure', 'hiking', 'family-friendly', 'wine region'). Use this when users ask for destination recommendations or want to know where to go.")]
    public async Task<string> SearchDestinations(
        [Description("Search query describing desired destination type, interests, or activities (e.g., 'beaches', 'adventure', 'hiking', 'wine', 'cultural')")] string query)
    {
        try
        {
            _logger.LogInformation("[DestinationSearch] Searching destinations for query: {Query}", query);

            var container = _database.GetContainer(_containerName);
            
            // Generate embedding for the search query
            var embeddingResponse = await _embeddingClient.GenerateEmbeddingAsync(query);
            var queryEmbedding = embeddingResponse.Value.ToFloats().ToArray();

            // Use Cosmos DB vector search with VectorDistance function
            var queryText = @"
                SELECT TOP 10
                    c.id,
                    c.destination,
                    c.location,
                    c.country,
                    c.category,
                    c.description,
                    c.experiences,
                    VectorDistance(c.descriptionVector, @queryEmbedding) AS SimilarityScore
                FROM c
                WHERE c.type = 'destination'
                ORDER BY VectorDistance(c.descriptionVector, @queryEmbedding)
            ";

            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@queryEmbedding", queryEmbedding);

            var destinations = new List<DestinationResult>();
            using var iterator = container.GetItemQueryIterator<DestinationResult>(queryDefinition);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var destination in response)
                {
                    destinations.Add(destination);
                }
            }

            _logger.LogInformation("[DestinationSearch] Found {Count} destinations using vector search", destinations.Count);

            if (destinations.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    searchCriteria = new { query },
                    totalResults = 0,
                    message = "No destinations found matching your criteria. Try broader terms like 'beach', 'adventure', 'nature', or 'culture'.",
                    destinations = Array.Empty<DestinationResult>()
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var result = new
            {
                success = true,
                searchCriteria = new { query },
                searchMethod = "vector_similarity",
                totalResults = destinations.Count,
                destinations = destinations.Select(d => new
                {
                    d.Id,
                    d.Destination,
                    d.Location,
                    d.Country,
                    d.Category,
                    d.Description,
                    d.Experiences,
                    SimilarityScore = d.SimilarityScore,
                    RelevancePercent = Math.Round((1 - d.SimilarityScore) * 100, 1)
                }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "[DestinationSearch] Cosmos DB error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new { error = $"Database error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DestinationSearch] Unexpected error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new { error = $"Search error: {ex.Message}" });
        }
    }
}

/// <summary>
/// Destination result model
/// </summary>
public class DestinationResult
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("experiences")]
    public List<string> Experiences { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("SimilarityScore")]
    public double SimilarityScore { get; set; }
}
