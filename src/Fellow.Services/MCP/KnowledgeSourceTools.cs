using System.ComponentModel;
using System.Text.Json;
using Fellow.Services.Knowledge;
using Microsoft.Extensions.Configuration;

namespace Fellow.Services.MCP;

public class KnowledgeSourceTools(IKnowledgeSource source)
{
    private const string Name = "fellow-blob-knowledge-source";
    
    [Description("Checks the status of the specified knowledge source")]
    public async Task<string> GetKnowledgeSourceStatusAsync()
    {
        var status = await source.CheckStatus(Name);
        var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
        return json;
    }

    [Description("Gets the details of the specified knowledge source")]
    public async Task<string> GetKnowledgeSourceAsync()
    {
        var sourceDetail = await source.GetKnowledgeSourceAsync(Name);
        var json = JsonSerializer.Serialize(sourceDetail, new JsonSerializerOptions { WriteIndented = true });
        return json;
    }
}