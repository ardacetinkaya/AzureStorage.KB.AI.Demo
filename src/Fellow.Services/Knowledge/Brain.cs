using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.KnowledgeBases;
using Azure.Search.Documents.KnowledgeBases.Models;
using Microsoft.Extensions.Options;

namespace Fellow.Services.Knowledge;

public class Brain(IOptions<Configurations> configuration, IKnowledgeSource knowledgeSource) : IBrain
{
    private const string Name = "fellow-brain";

    private readonly SearchIndexClient _searchIndexClient = new(
        new Uri(configuration.Value.KnowledgeSource?.AzureSearch.Endpoint ?? throw new InvalidOperationException("Azure Search endpoint is not configured.")),
        new AzureKeyCredential(configuration.Value.KnowledgeSource.AzureSearch.ApiKey ?? throw new InvalidOperationException("Azure Search API key is not configured."))
    );

    private readonly List<Dictionary<string, string>> _messages = [];
    
    public async Task<List<string>> SearchAsync(string query)
    {
        // Add query validation/sanitization
        if (string.IsNullOrWhiteSpace(query) || query.Length > 1000)
        {
            throw new ArgumentException("Invalid query", nameof(query));
        }
        
        query = SanitizeQuery(query);
        
        var baseClient = new KnowledgeBaseRetrievalClient(
            endpoint: new Uri(configuration.Value.KnowledgeSource!.AzureSearch.Endpoint),
            knowledgeBaseName: Name,
            credential: new AzureKeyCredential(configuration.Value.KnowledgeSource.AzureSearch.ApiKey) 
        );

        _messages.Add(new Dictionary<string, string>
        {
            { "role", "user" },
            { "content", query }
        });
        
        var retrievalRequest = new KnowledgeBaseRetrievalRequest();
        foreach (var message in _messages.Where(message => message["role"] != "system"))
        {
            retrievalRequest.Messages.Add(
                new KnowledgeBaseMessage(content: [
                    new KnowledgeBaseMessageTextContent(message["content"]) 
                ])
                {
                    Role = message["role"]
                }
            );
        }
        retrievalRequest.RetrievalReasoningEffort = new KnowledgeRetrievalLowReasoningEffort();

        try
        {
            var retrievalResponse = await baseClient.RetrieveAsync(retrievalRequest).ConfigureAwait(false);
            var retrievalResponseText = (retrievalResponse.Value.Response[0].Content[0] as KnowledgeBaseMessageTextContent)!.Text;
            
            _messages.Add(new Dictionary<string, string>
            {
                { "role", "assistant" },
                { "content", retrievalResponseText }
            });

            return [retrievalResponseText];
        }
        catch (Exception e)
        {
            return ["Unable to process this query, please rephrase your question.", e.Message];
        }

    }
    
    private static string SanitizeQuery(string query)
    {
        // Basic sanitization - adjust based on your needs
        return query.Trim()
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("  ", " ");
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

    public async Task InitializeAsync()
    {
        var exists = await IsExistingKnowledgeBase(Name);
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
            if (!isExists)
            {
                await knowledgeSource.Create(knowledgeSourceName);
            }

            var sources = new KnowledgeSourceReference[] { new(knowledgeSourceName) };
            var knowledgeBase = new KnowledgeBase(Name, sources)
            {
                RetrievalReasoningEffort = new KnowledgeRetrievalLowReasoningEffort(),
                RetrievalInstructions = @"Find and return factual information relevant to the query from the knowledge base.",
                OutputMode = "answerSynthesis",
                Models = { model },
                AnswerInstructions = "Provide a clear, factual response in markdown format based on the retrieved information."
            };

            await _searchIndexClient.CreateOrUpdateKnowledgeBaseAsync(knowledgeBase);
        }
    }
}