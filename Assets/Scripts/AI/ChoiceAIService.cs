using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChoiceAIService : AIConversationServiceBase
{
    [SerializeField] private List<AIQuestionData> questions = new List<AIQuestionData>();
    [SerializeField] private GameFlagManager gameFlagManager;

    private readonly List<ChatMessage> history = new List<ChatMessage>();
    private readonly HashSet<string> selectedQuestionIds = new HashSet<string>();
    private readonly HashSet<string> unlockedQuestionIds = new HashSet<string>();
    private readonly List<AIQuestionData> availableQuestions = new List<AIQuestionData>();

    public override IReadOnlyList<ChatMessage> History => history;

    private void Awake()
    {
        foreach (AIQuestionData question in questions)
        {
            if (question != null && question.IsInitiallyAvailable)
            {
                unlockedQuestionIds.Add(question.QuestionId);
            }
        }

        RefreshAvailability();
    }

    public override IReadOnlyList<AIQuestionData> GetAvailableQuestions()
    {
        RefreshAvailability();
        return availableQuestions;
    }

    public override AIConversationResult SelectQuestion(string questionId)
    {
        AIQuestionData question = questions.FirstOrDefault(q => q != null && q.QuestionId == questionId);
        if (question == null || !IsQuestionAvailable(question))
        {
            ChatMessage systemMessage = new ChatMessage(ChatRole.System, "この質問は現在選択できません。");
            history.Add(systemMessage);
            return new AIConversationResult(systemMessage, null);
        }

        selectedQuestionIds.Add(question.QuestionId);

        foreach (string unlockId in question.UnlockQuestionIds)
        {
            if (!string.IsNullOrWhiteSpace(unlockId))
            {
                unlockedQuestionIds.Add(unlockId);
            }
        }

        ChatMessage playerMessage = new ChatMessage(ChatRole.Player, question.QuestionText);
        ChatMessage aiMessage = new ChatMessage(ChatRole.AI, question.ResponseText);
        history.Add(playerMessage);
        history.Add(aiMessage);

        RefreshAvailability();
        return new AIConversationResult(playerMessage, aiMessage);
    }

    public override void RefreshAvailability()
    {
        availableQuestions.Clear();

        foreach (AIQuestionData question in questions)
        {
            if (question != null && IsQuestionAvailable(question))
            {
                availableQuestions.Add(question);
            }
        }
    }

    private bool IsQuestionAvailable(AIQuestionData question)
    {
        if (question == null || string.IsNullOrWhiteSpace(question.QuestionId))
        {
            return false;
        }

        if (question.HideAfterSelection && selectedQuestionIds.Contains(question.QuestionId))
        {
            return false;
        }

        if (!question.IsInitiallyAvailable && !unlockedQuestionIds.Contains(question.QuestionId))
        {
            return false;
        }

        foreach (string requiredFlag in question.RequiredFlags)
        {
            if (string.IsNullOrWhiteSpace(requiredFlag))
            {
                continue;
            }

            if (gameFlagManager == null || !gameFlagManager.HasFlag(requiredFlag))
            {
                return false;
            }
        }

        return true;
    }
}
