using Azure.Search.Documents.Models;

namespace Fellow.Services.Knowledge;

public interface IBrain
{
    Task InitializeAsync(string name);
    Task<SearchResults<string>> SearchAsync(string query);
}