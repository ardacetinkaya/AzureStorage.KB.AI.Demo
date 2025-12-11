using System.ComponentModel;
using Fellow.Services;
using Fellow.Services.Knowledge;

namespace Fellow.Services.MCP;

public class KnowledgeBaseTools(IBrain knowledgeBase)
{
    [Description(
        "Initialize the knowledge base for performing searches. Must be done before a search can be executed, but only needs to be done once.")]
    public async Task InitAsync()
    {
        await knowledgeBase.InitializeAsync();
    }

    [Description("Searches for information using a phrase or keyword. Relies on documents already being loaded.")]
    public async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")]
        string searchPhrase,
        [Description(
            "If possible, specify the filename to search that file only. If not provided or empty, the search includes all files.")]
        string? filenameFilter = null)
    {
        var results = await knowledgeBase.SearchAsync(searchPhrase);

        return results?.Select(result => $"{result}") ?? Enumerable.Empty<string>();
    }
}