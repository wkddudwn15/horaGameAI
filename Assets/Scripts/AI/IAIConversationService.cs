using System.Collections.Generic;

public interface IAIConversationService
{
    IReadOnlyList<ChatMessage> History { get; }
    IReadOnlyList<AIQuestionData> GetAvailableQuestions();
    AIConversationResult SelectQuestion(string questionId);
    void RefreshAvailability();
}
