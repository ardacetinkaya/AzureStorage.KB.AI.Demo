using Azure.Search.Documents.Indexes.Models;

namespace Fellow.Services.Knowledge;

public interface IKnowledgeSource
{
    Task<bool> IsExistingKnowledgeSource(string knowledgeSourceName);
    Task Create(string name);
    Task<KnowledgeSourceStatus> CheckStatus(string knowledgeSourceName);
    Task<Azure.Search.Documents.Indexes.Models.KnowledgeSource> GetKnowledgeSourceAsync(string knowledgeSourceName);
}