using System.ComponentModel;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using OpenAI.Embeddings;

namespace ContosoTravelAgent.Host.Tools;

/// <summary>
/// Chat history search tool for retrieving past conversations
/// Searches chat history in Cosmos DB based on semantic similarity
/// </summary>
public class ChatHistorySearchTool
{
    private readonly Database _database;
    private readonly string _containerName;
    private readonly ILogger<ChatHistorySearchTool> _logger;
    private readonly EmbeddingClient _embeddingClient;
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the ChatHistorySearchTool class.
    /// </summary>
    public ChatHistorySearchTool(
        Database database,
        string containerName,
        ILogger<ChatHistorySearchTool> logger,
        EmbeddingClient embeddingClient,
        IChatClient chatClient)
    {
        _database = database;
        _containerName = containerName;
        _logger = logger;
        _embeddingClient = embeddingClient;
        _chatClient = chatClient;
    }

    /// <summary>
    /// Enriches a search query by extracting entities and adding related travel terms
    /// </summary>
    private async Task<string> EnrichQueryAsync(string query)
    {
        try
        {
            var enrichmentPrompt = $"""
            Extract key entities from this travel query and add related terms: "{query}"
            Return only enriched search terms (e.g., for "Sydney trip" return "Sydney travel accommodation hotel flight booking").
            """;

            var chatMessages = new List<ChatMessage>
            {
                new(ChatRole.User, enrichmentPrompt)
            };

            // Use string response for simple text output
            var completion = await _chatClient.GetResponseAsync<string>(chatMessages, new ChatOptions { MaxOutputTokens = 50 });
            var enrichedQuery = completion?.Result?.Trim() ?? query;

            _logger.LogInformation("[ChatHistorySearch] Query enrichment: '{Original}' → '{Enriched}'", query, enrichedQuery);
            return enrichedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChatHistorySearch] Query enrichment failed, using original query");
            return query;
        }
    }

    /// <summary>
    /// Searches past conversations to recall previous discussions, bookings, and user preferences
    /// </summary>
    [Description("Search past conversations to recall previous discussions, bookings, preferences, and interactions. Use when user references past conversations ('last time', 'remember when', 'previously'), asks about previous bookings, or when context from conversation history would help personalize the response.")]
    public async Task<string> SearchChatHistory(
        [Description("Natural language query describing what to search for in past conversations (e.g., 'flights', 'hotel bookings', 'restaurant recommendations', 'budget preferences', 'adventure activities')")] string query,
        [Description("Optional: Filter by specific user ID to search only that user's conversation history")] string? userId = null)
    {
        try
        {
            _logger.LogInformation("[ChatHistorySearch] Searching chat history for query: {Query}, userId: {UserId}", query, userId ?? "all");

            var container = _database.GetContainer(_containerName);
            // Generate embedding for the search query
            var embeddingResponse = await _embeddingClient.GenerateEmbeddingAsync(query);
            var queryEmbedding = embeddingResponse.Value.ToFloats().ToArray();

            // Build WHERE clause with optional user filter
            var whereClause = "c.ApplicationId = @appId";
            if (!string.IsNullOrEmpty(userId))
            {
                whereClause += " AND c.UserId = @userId";
            }

            // Use Cosmos DB vector search with VectorDistance function
            var queryText = $@"
                SELECT TOP 10
                    c.id,
                    c.UserId,
                    c.ThreadId,
                    c.Role,
                    c.Content,
                    c.CreatedAt,
                    VectorDistance(c.ContentEmbedding, @queryEmbedding) AS SimilarityScore
                FROM c
                WHERE {whereClause}
                ORDER BY VectorDistance(c.ContentEmbedding, @queryEmbedding)
            ";

            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@queryEmbedding", queryEmbedding)
                .WithParameter("@appId", "ContosoTravelApp");

            if (!string.IsNullOrEmpty(userId))
            {
                queryDefinition.WithParameter("@userId", userId);
            }

            var messages = new List<ChatHistoryResult>();
            using var iterator = container.GetItemQueryIterator<ChatHistoryResult>(queryDefinition);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var message in response)
                {
                    messages.Add(message);
                }
            }

            _logger.LogInformation("[ChatHistorySearch] Found {Count} messages using vector search", messages.Count);

            if (messages.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    totalResults = 0,
                    message = "No matching conversations found in chat history.",
                    conversations = Array.Empty<ChatHistoryResult>()
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Group messages by thread for better context
            var groupedByThread = messages
                .GroupBy(m => m.ThreadId)
                .Select(g => new
                {
                    ThreadId = g.Key,
                    MessageCount = g.Count(),
                    Messages = g.OrderBy(m => m.CreatedAt).Select(m => new
                    {
                        m.Role,
                        m.Content,
                        m.CreatedAt,
                        SimilarityScore = m.SimilarityScore,
                        RelevancePercent = Math.Round((1 - m.SimilarityScore) * 100, 1)
                    }).ToList(),
                    FirstMessageDate = g.Min(m => m.CreatedAt),
                    MostRelevantScore = g.Min(m => m.SimilarityScore)
                })
                .OrderBy(g => g.MostRelevantScore)
                .ToList();

            var result = new
            {
                success = true,
                searchMethod = "vector_similarity_with_enrichment",
                totalResults = messages.Count,
                threadCount = groupedByThread.Count,
                conversations = groupedByThread
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "[ChatHistorySearch] Cosmos DB error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Failed to search chat history",
                details = ex.Message
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatHistorySearch] Unexpected error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "An unexpected error occurred while searching chat history",
                details = ex.Message
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private class ChatHistoryResult
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ThreadId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double SimilarityScore { get; set; }
    }
}
