using Microsoft.Extensions.AI;

namespace Fellow.Services.Chat;

public interface IChatService
{
    /// <summary>
    /// Starts a new chat conversation
    /// </summary>
    void StartNewConversation();

    /// <summary>
    /// Sends a user message and streams the response
    /// </summary>
    /// <param name="userMessage">The user's message</param>
    /// <param name="onUpdate">Callback invoked for each streaming update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The complete response message</returns>
    Task<ChatMessage> SendMessageAsync(
        string userMessage, 
        Action<string> onUpdate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current conversation history
    /// </summary>
    IReadOnlyList<ChatMessage> GetConversationHistory();

    /// <summary>
    /// Cancels any ongoing response streaming
    /// </summary>
    void CancelCurrentResponse();
}

