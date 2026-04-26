using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Backend API - Contoso Travel Agent
var backend = builder.AddProject<Projects.ContosoTravelAgent_Host>("backend");

// Frontend - Next.js application (uses Dockerfile for container deployment)
//var frontend = builder.AddDockerfile("frontend", "../frontend")
//    .WithHttpEndpoint(3000, targetPort: 3000)
//    .WithReference(backend)
//    .WaitFor(backend)
//    .WithEnvironment("BACKEND_AGENT_BASE_URL", backend.GetEndpoint("http"));

builder.AddExecutable(
    name: "frontend",
    command: "npm",
    args: new[] { "run", "start" },
    workingDirectory: "../frontend"
)
.WaitFor(backend)
.WithHttpEndpoint(port: 3000, env: "PORT")
.WithReference(backend)
.WithEnvironment("BACKEND_AGENT_BASE_URL", backend.GetEndpoint("http"));

await builder.Build().RunAsync();