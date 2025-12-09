using Microsoft.Extensions.AI;
using Fellow.Services.MCP;

namespace Fellow.Services.Chat;

public class ChatService : IChatService
{
    private readonly IChatClient _chatClient;
    private readonly KnowledgeBaseTools _knowledgeBaseTools;
    private readonly KnowledgeSourceTools _knowledgeSourceTools;
    private readonly string _systemPrompt;
    private readonly ChatOptions _chatOptions;
    private readonly List<ChatMessage> _messages;
    private int _statefulMessageCount;
    private CancellationTokenSource? _currentResponseCancellation;

    public ChatService(
        IChatClient chatClient,
        KnowledgeBaseTools knowledgeBaseTools,
        KnowledgeSourceTools knowledgeSourceTools,
        string? systemPrompt = null)
    {
        _chatClient = chatClient;
        _knowledgeBaseTools = knowledgeBaseTools;
        _knowledgeSourceTools = knowledgeSourceTools;
        _systemPrompt = systemPrompt ?? GetDefaultSystemPrompt();
        _chatOptions = new ChatOptions();
        _messages = new List<ChatMessage>();

        InitializeConversation();
    }

    private void InitializeConversation()
    {
        _messages.Clear();
        _messages.Add(new ChatMessage(ChatRole.System, _systemPrompt));
        _statefulMessageCount = 0;
        _chatOptions.ConversationId = null;

        _chatOptions.Tools = [
            AIFunctionFactory.Create(_knowledgeBaseTools.InitAsync),
            AIFunctionFactory.Create(_knowledgeBaseTools.SearchAsync),
            AIFunctionFactory.Create(_knowledgeSourceTools.GetKnowledgeSourceAsync),
            AIFunctionFactory.Create(_knowledgeSourceTools.GetKnowledgeSourceStatusAsync)
        ];
    }

    public void StartNewConversation()
    {
        CancelCurrentResponse();
        InitializeConversation();
    }

    public async Task<ChatMessage> SendMessageAsync(
        string userMessage,
        Action<string> onUpdate,
        CancellationToken cancellationToken = default)
    {
        CancelCurrentResponse();

        // Add user message to conversation
        var userChatMessage = new ChatMessage(ChatRole.User, userMessage);
        _messages.Add(userChatMessage);

        // Prepare response message
        var responseText = new TextContent("");
        var responseMessage = new ChatMessage(ChatRole.Assistant, [responseText]);
        _currentResponseCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await foreach (var update in _chatClient.GetStreamingResponseAsync(
                _messages.Skip(_statefulMessageCount),
                _chatOptions,
                _currentResponseCancellation.Token))
            {
                // Check for function calls (short-circuit Search tool)
                var functionCalls = update.Contents.OfType<FunctionCallContent>().ToList();

                if (functionCalls.Any())
                {
                    var searchFunctionCall = functionCalls.FirstOrDefault(fc => fc.Name.Contains("Search"));
                    if (searchFunctionCall is not null && searchFunctionCall.Arguments is not null)
                    {
                        // Short-circuit: Execute search and return results directly
                        var result = await _knowledgeBaseTools.SearchAsync(
                            string.Join(" ", searchFunctionCall.Arguments));

                        _messages.Add(new ChatMessage(ChatRole.Assistant, [searchFunctionCall]));
                        _messages.Add(new ChatMessage(ChatRole.Tool,
                            [new FunctionResultContent(searchFunctionCall.CallId, result)]));

                        responseText.Text = string.Join(" ", result);
                        onUpdate(responseText.Text);
                        break;
                    }
                }

                // Normal streaming behavior for all other content
                _messages.AddMessages(update, filter: c => c is not TextContent);
                responseText.Text += update.Text;
                _chatOptions.ConversationId = update.ConversationId;
                onUpdate(responseText.Text);
            }

            // Store the final response
            _messages.Add(responseMessage);
            _statefulMessageCount = _chatOptions.ConversationId is not null ? _messages.Count : 0;

            return responseMessage;
        }
        catch (Exception ex)
        {
            responseText.Text += $"\n\n**Error:** {ex.Message}";
            onUpdate(responseText.Text);
            throw;
        }
        finally
        {
            _currentResponseCancellation?.Dispose();
            _currentResponseCancellation = null;
        }
    }

    public IReadOnlyList<ChatMessage> GetConversationHistory()
    {
        return _messages.AsReadOnly();
    }

    public void CancelCurrentResponse()
    {
        _currentResponseCancellation?.Cancel();
        _currentResponseCancellation?.Dispose();
        _currentResponseCancellation = null;
    }

    private static string GetDefaultSystemPrompt()
    {
        return @"
        You are an assistant who answers questions about information you have in your knowledge base.
        If you don't know the answer, just say that you don't know. Do not try to make up.

        Use the InitAsync tool to prepare for searches before answering any questions.
        Use the GetKnowledgeSourceAsync tool to get info about knowledge source.
        Use the GetKnowledgeSourceStatusAsync tool to check the status of the knowledge source.

        Use the Search tool to find relevant information. When you do this, end your
        reply with citations in the special XML format: 

        <citation>exact quote here</citation>

        Use only simple markdown to format your responses.
        
        Other than answering other questions, just politly refuse to answer questions that are not exists in your knowledge base.
        ";
    }
}

