using System.ClientModel;
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using OpenAI;
using Fellow.Services;
using Fellow.Services.Ingestion;
using Fellow.Services.Knowledge;
using Fellow.Services.MCP;
using SemanticSearch = Fellow.Services.SemanticSearch;

namespace Fellow.Web.UI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFellowAiServices(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        
        var aiOptions = new Configurations();
        configuration.GetSection(Configurations.Name).Bind(aiOptions);
        services.Configure<Configurations>(configuration.GetSection(Configurations.Name));
        
        IChatClient chatClient;
        IEmbeddingGenerator embeddingGenerator;
        if (string.Compare(aiOptions.AI?.Provider, "AzureAi", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            var chatConfig = aiOptions.AI?.AzureAI?.Chat ?? throw new InvalidOperationException("Missing AzureAI Chat configuration");
            var embeddingConfig = aiOptions.AI.AzureAI?.Embedding ?? throw new InvalidOperationException("Missing AzureAI Embedding configuration");
            var chatCredential = new AzureKeyCredential(chatConfig.ApiKey);
            var embeddingCredential = new AzureKeyCredential(embeddingConfig.ApiKey);
            
            var chatCompletionsClient = new ChatCompletionsClient(
                new Uri($"{chatConfig.Endpoint}/openai/deployments/{chatConfig.ModelName}"), 
                chatCredential);
            chatClient = chatCompletionsClient.AsIChatClient(chatConfig.ModelName);
            
            var embeddingsClient = new EmbeddingsClient(
                new Uri($"{embeddingConfig.Endpoint}/openai/deployments/{embeddingConfig.ModelName}"), 
                embeddingCredential);
            embeddingGenerator = embeddingsClient.AsIEmbeddingGenerator(embeddingConfig.ModelName);
        }
        else if (string.Compare(aiOptions.AI?.Provider, "GitHubModels", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            var ghConfig = aiOptions.AI?.GitHubModels ?? throw new InvalidOperationException("Missing GitHubModels configuration");

            var credential = new ApiKeyCredential(ghConfig.ApiKey);

            var openAiClientOptions = new OpenAIClientOptions { Endpoint = new Uri(ghConfig.Endpoint) };
            var ghModelsClient = new OpenAIClient(credential, openAiClientOptions);

            chatClient = ghModelsClient
                .GetChatClient(ghConfig.ChatModel)
                .AsIChatClient();
            
            embeddingGenerator = ghModelsClient
                .GetEmbeddingClient(ghConfig.EmbeddingModel)
                .AsIEmbeddingGenerator();
        }
        else
        {
            throw new InvalidOperationException("Missing configuration: AI:Provider");
        }


        var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
        var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
        services.AddSqliteVectorStore(_ => vectorStoreConnectionString);
        services.AddSqliteCollection<string, IngestedChunk>(IngestedChunk.CollectionName, vectorStoreConnectionString);

        services.AddSingleton<DataIngestor>();
        services.AddSingleton<SemanticSearch>();
        services.AddSingleton<KnowledgeBaseTools>();

        services.AddSingleton<IKnowledgeSource, KnowledgeSource>();
        services.AddSingleton<IBrain, Brain>();
        services.AddSingleton<KnowledgeSourceTools>();

        services.AddKeyedSingleton("ingestion_directory",
            new DirectoryInfo(Path.Combine(environment.WebRootPath, "Data")));
        services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
        services.AddEmbeddingGenerator((IEmbeddingGenerator<string, Embedding<float>>)embeddingGenerator);


        return services;
    }
}