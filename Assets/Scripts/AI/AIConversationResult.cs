using System.Collections.Generic;

public class AIConversationResult
{
    public ChatMessage PlayerMessage { get; }
    public ChatMessage AIMessage { get; }
    public IReadOnlyList<ChatMessage> NewMessages { get; }

    public AIConversationResult(ChatMessage playerMessage, ChatMessage aiMessage)
    {
        PlayerMessage = playerMessage;
        AIMessage = aiMessage;

        List<ChatMessage> messages = new List<ChatMessage>();
        if (playerMessage != null)
        {
            messages.Add(playerMessage);
        }

        if (aiMessage != null)
        {
            messages.Add(aiMessage);
        }

        NewMessages = messages;
    }
}
