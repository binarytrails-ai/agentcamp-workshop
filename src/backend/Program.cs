using ContosoTravelAgent.Host;
using ContosoTravelAgent.Host.Agents;
using ContosoTravelAgent.Host.Extensions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Services.LoadContosoTravelConfig(builder.Configuration);
builder.Services.AddSingleton(config);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(sp => new Microsoft.AspNetCore.Http.Json.JsonOptions().SerializerOptions);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.ConfigureOpenTelemetry(
    serviceName: Constants.ApplicationId,
    serviceVersion: "1.0.0",
    otlpEndpoint: config.OtelExporterOtlpEndpoint,
    applicationInsightsConnectionString: config.ApplicationInsightsConnectionString,
    additionalSources: ["Microsoft.Agents.AI", "Microsoft.Extensions.AI"],
    additionalMeters: ["Microsoft.Agents.AI", "Microsoft.Extensions.AI"]);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.AddOpenAIChatCompletions();
builder.Services.AddAGUI();

IChatClient chatClient;
OpenAI.Embeddings.EmbeddingClient embeddingClient;

Console.WriteLine("Using Azure AI Models");
var azureOpenAIClient = new Azure.AI.OpenAI.AzureOpenAIClient(
    new Uri(config.AzureAIServicesEndpoint!), new ApiKeyCredential(config.AzureAIServicesKey!));

// Create Azure AI chat client
chatClient = azureOpenAIClient.GetChatClient(config.AzureTextModelName).AsIChatClient().AsBuilder()
    .UseOpenTelemetry(sourceName: Constants.ApplicationId, configure: (cfg) => cfg.EnableSensitiveData = true)
    .Build();

embeddingClient = azureOpenAIClient.GetEmbeddingClient(config.AzureEmbeddingModelName);

builder.Services.AddChatClient(chatClient);
builder.Services.AddSingleton(embeddingClient);
builder.Services.AddSingleton(sp =>
{
    var cosmosClient = new Microsoft.Azure.Cosmos.CosmosClient(
        config.CosmosDbConnectionString,
        new Microsoft.Azure.Cosmos.CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default
        });
    return cosmosClient.GetDatabase(config.CosmosDbDatabaseName);
});

// Register agent factories
builder.Services.AddSingleton<ContosoTravelAgentBuilder>();
builder.Services.AddKeyedSingleton("ContosoTravelAgent", (sp, key) =>
{
    var factory = sp.GetRequiredService<ContosoTravelAgentBuilder>();
    return factory.CreateAsync().Result;
});

//Register workflow agent factories
//builder.Services.AddSingleton<TriageAgentFactory>();
//builder.Services.AddSingleton<TripAdvisorAgentFactory>();
//builder.Services.AddSingleton<FlightBookingAgentFactory>(sp =>
//{
//    var chatClient = sp.GetRequiredService<IChatClient>();
//    var mcpClient = sp.GetRequiredKeyedService<McpClient>("mcp-contoso-travel");
//    var jsonOptions = sp.GetRequiredService<JsonSerializerOptions>();
//    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var config = sp.GetRequiredService<ContosoTravelAppConfig>();
//    var cosmosDatabase = sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
//    return new FlightBookingAgentFactory(
//        chatClient, mcpClient, jsonOptions, httpContextAccessor, loggerFactory, config, cosmosDatabase);
//});
//builder.Services.AddSingleton<ContosoTravelWorkflowAgentFactory>();
//builder.Services.AddKeyedSingleton("ContosoTravelWorkflowAgent", (sp, key) =>
//{
//    var factory = sp.GetRequiredService<ContosoTravelWorkflowAgentFactory>();
//    return factory.CreateAsync().Result;
//});

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { status = "healthy", service = "Travel Assistant API" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var travelBot = app.Services.GetRequiredKeyedService<AIAgent>("ContosoTravelAgent");
app.MapOpenAIChatCompletions(travelBot, "/ContosoTravelAgent/v1/chat/completions");
// Map AGUI endpoint
app.MapAGUI("/agent/contoso_travel_bot", travelBot);

//app.MapPost("/agent/contoso_travel_bot", async (HttpContext context) =>
//{
//    using var reader = new StreamReader(context.Request.Body);
//    var body = await reader.ReadToEndAsync();
//    var request = JsonSerializer.Deserialize<ChatRequest>(body);

//    if (request?.Messages == null || !request.Messages.Any())
//        return Results.BadRequest("Messages are required");

//    var response = await travelBot.InvokeAsync(request.Messages, cancellationToken: context.RequestAborted);
//    return Results.Ok(new { response = response.Content });
//});

//// Map workflow agent endpoint
//var workflowBot = app.Services.GetRequiredKeyedService<AIAgent>("ContosoTravelWorkflowAgent");
//app.MapAGUI("/agent/contoso_travel_workflow", workflowBot);

app.UseRequestContext();
app.UseCors();
await app.RunAsync();