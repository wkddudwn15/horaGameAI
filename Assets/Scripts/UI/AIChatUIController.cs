using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIChatUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private TerminalUIController terminalUIController;
    [SerializeField] private AIConversationServiceBase conversationService;
    [SerializeField] private TextMeshProUGUI aiNameText;
    [SerializeField] private Transform historyContent;
    [SerializeField] private Transform questionChoicesContent;
    [SerializeField] private TextMeshProUGUI messageTextPrefab;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Settings")]
    [SerializeField] private string aiName = "生活支援AI";
    [SerializeField] private float responseDelay = 0.6f;

    private readonly List<Button> activeChoiceButtons = new List<Button>();
    private bool isResponding;

    private void Awake()
    {
        if (aiNameText != null)
        {
            aiNameText.text = aiName;
        }

        ClosePanelOnly();
    }

    public void Open()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
            RestoreVisibleChatChildren();
        }

        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Chat);
        }

        RefreshHistory();
        RefreshQuestionChoices();
    }

    public void Close()
    {
        ClosePanelOnly();

        if (terminalUIController != null)
        {
            terminalUIController.ClosePanelOnly();
        }

        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Gameplay);
        }
    }

    public void ClosePanelOnly()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
    }

    private void RestoreVisibleChatChildren()
    {
        if (chatPanel == null)
        {
            return;
        }

        for (int i = 0; i < chatPanel.transform.childCount; i++)
        {
            GameObject child = chatPanel.transform.GetChild(i).gameObject;
            if (messageTextPrefab != null && child == messageTextPrefab.gameObject)
            {
                continue;
            }

            if (choiceButtonPrefab != null && child == choiceButtonPrefab.gameObject)
            {
                continue;
            }

            child.SetActive(true);
        }
    }

    private void RefreshHistory()
    {
        ClearChildren(historyContent);

        if (conversationService == null)
        {
            AddMessageToHistory(new ChatMessage(ChatRole.System, "AI会話サービスが設定されていません。"));
            return;
        }

        foreach (ChatMessage message in conversationService.History)
        {
            AddMessageToHistory(message);
        }
    }

    private void RefreshQuestionChoices()
    {
        ClearChildren(questionChoicesContent);
        activeChoiceButtons.Clear();

        if (conversationService == null || choiceButtonPrefab == null || questionChoicesContent == null)
        {
            return;
        }

        foreach (AIQuestionData question in conversationService.GetAvailableQuestions())
        {
            if (question == null)
            {
                continue;
            }

            Button button = Instantiate(choiceButtonPrefab, questionChoicesContent);
            button.gameObject.SetActive(true);
            button.enabled = true;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.enabled = true;
                label.text = question.QuestionText;
            }

            string questionId = question.QuestionId;
            button.onClick.AddListener(() => OnQuestionSelected(questionId));
            activeChoiceButtons.Add(button);
        }
    }

    private void OnQuestionSelected(string questionId)
    {
        if (isResponding || conversationService == null)
        {
            return;
        }

        StartCoroutine(SelectQuestionRoutine(questionId));
    }

    private IEnumerator SelectQuestionRoutine(string questionId)
    {
        isResponding = true;
        SetChoiceButtonsInteractable(false);

        AIConversationResult result = conversationService.SelectQuestion(questionId);

        if (result != null && result.PlayerMessage != null)
        {
            AddMessageToHistory(result.PlayerMessage);
        }

        if (responseDelay > 0f)
        {
            yield return new WaitForSeconds(responseDelay);
        }

        if (result != null && result.AIMessage != null)
        {
            AddMessageToHistory(result.AIMessage);
        }

        if (conversationService != null)
        {
            conversationService.RefreshAvailability();
        }

        RefreshQuestionChoices();
        SetChoiceButtonsInteractable(true);
        isResponding = false;
    }

    private void AddMessageToHistory(ChatMessage message)
    {
        if (message == null || messageTextPrefab == null || historyContent == null)
        {
            return;
        }

        TextMeshProUGUI text = Instantiate(messageTextPrefab, historyContent);
        text.gameObject.SetActive(true);
        text.enabled = true;
        text.text = FormatMessage(message);
    }

    private static string FormatMessage(ChatMessage message)
    {
        string role = message.Role switch
        {
            ChatRole.Player => "あなた",
            ChatRole.AI => "生活支援AI",
            _ => "System"
        };

        return $"{role}: {message.Message}";
    }

    private void SetChoiceButtonsInteractable(bool interactable)
    {
        foreach (Button button in activeChoiceButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
