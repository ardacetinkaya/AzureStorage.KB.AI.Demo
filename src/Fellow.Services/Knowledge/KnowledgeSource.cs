using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Fellow.Services.Knowledge;

public class KnowledgeSource : IKnowledgeSource
{
    private readonly SearchIndexClient _searchIndexClient;
    private readonly Configurations _options;

    public KnowledgeSource(IConfiguration configuration, IOptions<Configurations> options)
    {
        _options = options.Value;

        var searchConfig = _options.KnowledgeSource?.AzureSearch ??
                           throw new InvalidOperationException("Azure Search configuration is missing.");

        _searchIndexClient = new SearchIndexClient(
            new Uri(searchConfig.Endpoint),
            new AzureKeyCredential(searchConfig.ApiKey));
    }

    public async Task<bool> IsExistingKnowledgeSource(string knowledgeSourceName)
    {
        // Check if the knowledge source exists
        var knowledgeSources = _searchIndexClient.GetKnowledgeSourcesAsync();
        await foreach (var ks in knowledgeSources)
        {
            if (ks.Name.Equals(knowledgeSourceName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public async Task Create(string name)
    {
        if (!await IsExistingKnowledgeSource(name))
        {
            var embeddingConfig = _options.AI?.AzureAI?.Embedding 
                                  ?? throw new InvalidOperationException("Azure AI Embedding configuration is missing.");
            
            var blobConfig = _options.KnowledgeSource?.BlobStorage 
                             ?? throw new InvalidOperationException("Blob Storage configuration is missing.");

            var ingestionParams = new KnowledgeSourceIngestionParameters
            {
                DisableImageVerbalization = false,
                EmbeddingModel = new KnowledgeSourceAzureOpenAIVectorizer
                {
                    AzureOpenAIParameters = new AzureOpenAIVectorizerParameters
                    {
                        ResourceUri = new Uri(embeddingConfig.Endpoint),
                        DeploymentName = embeddingConfig.ModelName,
                        ModelName = new AzureOpenAIModelName(embeddingConfig.ModelName),
                        ApiKey = embeddingConfig.ApiKey,
                    }
                }
            };


            var blobParams = new AzureBlobKnowledgeSourceParameters(
                connectionString: blobConfig.ConnectionString,
                containerName: blobConfig.ContainerName
            )
            {
                IsAdlsGen2 = false,
                IngestionParameters = ingestionParams
            };

            var knowledgeSource = new AzureBlobKnowledgeSource(
                name: name,
                azureBlobParameters: blobParams
            )
            {
                Description = "This knowledge source pulls from a blob storage container."
            };

            await _searchIndexClient.CreateOrUpdateKnowledgeSourceAsync(knowledgeSource);
        }
    }

    public async Task<KnowledgeSourceStatus> CheckStatus(string knowledgeSourceName)
    {
        var statusResponse = await _searchIndexClient.GetKnowledgeSourceStatusAsync(knowledgeSourceName);

        return statusResponse;
    }

    public async Task<Azure.Search.Documents.Indexes.Models.KnowledgeSource> GetKnowledgeSourceAsync(
        string knowledgeSourceName)
    {
        var knowledgeSource = await _searchIndexClient.GetKnowledgeSourceAsync(knowledgeSourceName);

        return knowledgeSource;
    }
}