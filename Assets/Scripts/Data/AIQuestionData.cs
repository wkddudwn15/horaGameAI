using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AIQuestionData", menuName = "Horror Prototype/AI Question Data")]
public class AIQuestionData : ScriptableObject
{
    [SerializeField] private string questionId;
    [TextArea]
    [SerializeField] private string questionText;
    [TextArea]
    [SerializeField] private string responseText;
    [SerializeField] private bool isInitiallyAvailable = true;
    [SerializeField] private bool hideAfterSelection;
    [SerializeField] private List<string> requiredFlags = new List<string>();
    [SerializeField] private List<string> unlockQuestionIds = new List<string>();

    public string QuestionId => questionId;
    public string QuestionText => questionText;
    public string ResponseText => responseText;
    public bool IsInitiallyAvailable => isInitiallyAvailable;
    public bool HideAfterSelection => hideAfterSelection;
    public IReadOnlyList<string> RequiredFlags => requiredFlags;
    public IReadOnlyList<string> UnlockQuestionIds => unlockQuestionIds;
}
