using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.KnowledgeBases.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Fellow.Services.Knowledge;

public class Brain(IOptions<Configurations> configuration, IKnowledgeSource knowledgeSource) : IBrain
{
    private readonly string _searchEndpoint = configuration.Value.KnowledgeSource?.AzureSearch.Endpoint ??
                                              throw new InvalidOperationException(
                                                  "Azure Search endpoint is not configured.");

    private readonly string _apiKey = configuration.Value.KnowledgeSource.AzureSearch.ApiKey ??
                                      throw new InvalidOperationException("Azure Search API key is not configured.");

    private const string IndexName = "fellow-blob-knowledge-source-index";

    private readonly SearchIndexClient _searchIndexClient = new(
        new Uri(configuration.Value.KnowledgeSource.AzureSearch.Endpoint ??
                throw new InvalidOperationException("Azure Search endpoint is not configured.")),
        new AzureKeyCredential(configuration.Value.KnowledgeSource.AzureSearch.ApiKey ??
                               throw new InvalidOperationException("Azure Search API key is not configured."))
    );


    public async Task<SearchResults<string>> SearchAsync(string query)
    {
        var searchClient = new SearchClient(new Uri(_searchEndpoint)
            , IndexName
            , new AzureKeyCredential(_apiKey));

        var result = await searchClient.SearchAsync<string>(query);

        return result;
    }

    private async Task<bool> IsExistingKnowledgeBase(string knowledgeBaseName)
    {
        // Check if the knowledge base exists
        var knowledgeSources = _searchIndexClient.GetKnowledgeBasesAsync();
        await foreach (var ks in knowledgeSources)
        {
            if (ks.Name.Equals(knowledgeBaseName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public async Task InitializeAsync(string name)
    {
        var exists = await IsExistingKnowledgeBase(name);
        if (!exists)
        {
            var model = new KnowledgeBaseAzureOpenAIModel(new AzureOpenAIVectorizerParameters
            {
                ResourceUri = new Uri(configuration.Value.AI!.AzureAI!.Chat.Endpoint),
                DeploymentName = configuration.Value.AI.AzureAI.Chat.ModelName,
                ModelName = new AzureOpenAIModelName(configuration.Value.AI.AzureAI.Chat.ModelName),
                ApiKey = configuration.Value.AI.AzureAI.Chat.ApiKey,
            });

            var knowledgeSourceName = "fellow-blob-knowledge-source";
            var isExists = await knowledgeSource.IsExistingKnowledgeSource(knowledgeSourceName);
            if (isExists is false)
            {
                await knowledgeSource.Create(knowledgeSourceName);
            }

            var sources = new KnowledgeSourceReference[] { new(knowledgeSourceName) };
            var knowledgeBase = new KnowledgeBase(name, sources)
            {
                RetrievalReasoningEffort = new KnowledgeRetrievalLowReasoningEffort(),
                RetrievalInstructions = @"Provide an informative answer based on the retrieved documents from knowledge source. 
Answer concisely but in short complete max 2 sentences.
                ",
                OutputMode = KnowledgeRetrievalOutputMode.ExtractiveData,
                Models = { model }
            };

            await _searchIndexClient.CreateOrUpdateKnowledgeBaseAsync(knowledgeBase);
        }
    }
}