using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using Fellow.Services;
using Fellow.Services.Ingestion;
using Fellow.Services.MCP;

namespace Fellow.Web.UI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFellowAIServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // You will need to set the endpoint and key to your own values
        // You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
        //   cd this-project-directory
        //   dotnet user-secrets set GitHubModels:Token YOUR-GITHUB-TOKEN
        var credential = new ApiKeyCredential(configuration["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. See the README for details."));
        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://models.inference.ai.azure.com")
        };

        var ghModelsClient = new OpenAIClient(credential, openAIOptions);
        var chatClient = ghModelsClient.GetChatClient("gpt-4o-mini").AsIChatClient();
        var embeddingGenerator = ghModelsClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();

        var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
        var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
        services.AddSqliteVectorStore(_ => vectorStoreConnectionString);
        services.AddSqliteCollection<string, IngestedChunk>(IngestedChunk.CollectionName, vectorStoreConnectionString);

        services.AddSingleton<DataIngestor>();
        services.AddSingleton<SemanticSearch>();
        services.AddSingleton<KnowledgeBaseTools>();
        services.AddKeyedSingleton("ingestion_directory", new DirectoryInfo(Path.Combine(environment.WebRootPath, "Data")));
        services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
        services.AddEmbeddingGenerator(embeddingGenerator);

        return services;
    }
}
