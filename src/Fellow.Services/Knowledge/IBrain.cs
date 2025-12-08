using Azure.Search.Documents.Models;

namespace Fellow.Services.Knowledge;

public interface IBrain
{
    Task InitializeAsync();
    Task<List<string>> SearchAsync(string query);
}