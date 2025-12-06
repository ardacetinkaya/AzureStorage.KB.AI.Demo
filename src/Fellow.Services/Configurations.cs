namespace Fellow.Services;

public class Configurations
{
    public AIOptions? AI { get; set; }
    public KnowledgeSourceOptions? KnowledgeSource { get; set; }
    public static string Name { get; } = "Fellow";
}

public class AIOptions
{
    public string Provider { get; set; } = string.Empty;
    public AzureAIOptions? AzureAI { get; set; }
    public GitHubModelsOptions? GitHubModels { get; set; }
}

public class AzureAIOptions
{
    public AzureAIEndpointOptions Chat { get; set; } = new();
    public AzureAIEndpointOptions Embedding { get; set; } = new();
}

public class AzureAIEndpointOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
}

public class GitHubModelsOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

public class KnowledgeSourceOptions
{
    public AzureSearchOptions AzureSearch { get; set; } = new();
    public BlobStorageOptions BlobStorage { get; set; } = new();
}

public class AzureSearchOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "source";
}