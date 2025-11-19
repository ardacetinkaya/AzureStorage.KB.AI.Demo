using System.ComponentModel;
using Fellow.Services;

namespace Fellow.Services.MCP;

public class KnowledgeBaseTools(SemanticSearch search)
{
    [Description("Loads the documents needed for performing searches. Must be completed before a search can be executed, but only needs to be completed once.")]
    public async Task LoadDocumentsAsync()
    {
        await search.LoadDocumentsAsync();
    }

    [Description("Searches for information using a phrase or keyword. Relies on documents already being loaded.")]
    public async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("If possible, specify the filename to search that file only. If not provided or empty, the search includes all files.")] string? filenameFilter = null)
    {
        var results = await search.SearchAsync(searchPhrase, filenameFilter, maxResults: 5);
        return results.Select(result =>
            $"<result filename=\"{result.DocumentId}\">{result.Text}</result>");
    }
}
