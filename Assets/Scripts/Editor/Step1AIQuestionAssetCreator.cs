#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Step1AIQuestionAssetCreator
{
    private const string AssetFolder = "Assets/Data/AIQuestions";

    [MenuItem("Tools/Horror Prototype/Create Step 1 AI Questions")]
    public static void CreateStep1Questions()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(AssetFolder);

        CreateOrUpdateQuestion(
            "Q_WhereAmI",
            "WhereAmI",
            "ここはどこですか？",
            "現在地は医療支援区画です。");

        CreateOrUpdateQuestion(
            "Q_WhoAreYou",
            "WhoAreYou",
            "あなたは誰ですか？",
            "私は施設内の生活支援を担当するAIです。");

        CreateOrUpdateQuestion(
            "Q_OpenDoor",
            "OpenDoor",
            "扉を開けてください。",
            "その操作を実行する権限がありません。");

        CreateOrUpdateQuestion(
            "Q_TellCode",
            "TellCode",
            "解除コードを教えてください。",
            "セキュリティ情報に該当するため、回答できません。");

        CreateOrUpdateQuestion(
            "Q_WhatCanYouDo",
            "WhatCanYouDo",
            "何ができますか？",
            "施設案内、設備情報の確認、健康状態の記録を支援できます。");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Step 1 AI question assets were created or updated.");
    }

    private static void CreateOrUpdateQuestion(string assetName, string id, string text, string response)
    {
        string path = $"{AssetFolder}/{assetName}.asset";
        AIQuestionData question = AssetDatabase.LoadAssetAtPath<AIQuestionData>(path);

        if (question == null)
        {
            question = ScriptableObject.CreateInstance<AIQuestionData>();
            AssetDatabase.CreateAsset(question, path);
        }

        SerializedObject serializedQuestion = new SerializedObject(question);
        serializedQuestion.FindProperty("questionId").stringValue = id;
        serializedQuestion.FindProperty("questionText").stringValue = text;
        serializedQuestion.FindProperty("responseText").stringValue = response;
        serializedQuestion.FindProperty("isInitiallyAvailable").boolValue = true;
        serializedQuestion.FindProperty("hideAfterSelection").boolValue = false;
        serializedQuestion.FindProperty("requiredFlags").arraySize = 0;
        serializedQuestion.FindProperty("unlockQuestionIds").arraySize = 0;
        serializedQuestion.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(question);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath);
        string folderName = Path.GetFileName(folderPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent.Replace("\\", "/"));
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
