using UnityEngine;

public abstract class AIConversationServiceBase : MonoBehaviour, IAIConversationService
{
    public abstract System.Collections.Generic.IReadOnlyList<ChatMessage> History { get; }
    public abstract System.Collections.Generic.IReadOnlyList<AIQuestionData> GetAvailableQuestions();
    public abstract AIConversationResult SelectQuestion(string questionId);
    public abstract void RefreshAvailability();
}
