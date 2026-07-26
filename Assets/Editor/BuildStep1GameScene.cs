#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildStep1GameScene
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string InputActionsPath = "Assets/Settings/PlayerControls.inputactions";
    private const string QuestionFolder = "Assets/Data/AIQuestions";
    private const string InputReferenceFolder = "Assets/Settings/InputActionReferences";
    private const string VolumeProfilePath = "Assets/Settings/Step1OpeningVolumeProfile.asset";
    private const string FallbackTmpFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private static readonly string[] JapaneseTmpFontSearchFolders =
    {
        "Assets/Fonts",
        "Assets/Resources/Fonts",
        "Assets/TextMesh Pro/Resources/Fonts & Materials",
        "Assets/TextMesh Pro/Resources",
        "Assets/Data/Fonts"
    };

    private static TMP_FontAsset cachedTmpFontAsset;
    private static bool warnedMissingJapaneseFont;

    [MenuItem("Tools/Horror Prototype/Build Step 1 GameScene")]
    public static void BuildScene()
    {
        cachedTmpFontAsset = null;
        warnedMissingJapaneseFont = false;

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Settings");

        Scene scene = OpenOrCreateScene();

        Materials materials = CreateMaterials();
        CreateDirectionalLight();
        Volume globalVolume = CreateGlobalVolume();

        GameObject environment = GetOrCreateRoot("Environment");
        BuildEnvironment(environment.transform, materials);

        PlayerRefs player = BuildPlayer();
        NormalizeSceneCamerasAndAudio(player.Camera);
        ManagerRefs managers = BuildManagers();
        UiRefs ui = BuildGameCanvas();

        TerminalInteractable terminal = ConfigureTerminal(environment.transform, materials, ui.TerminalUi);
        ConfigureChoiceAIService(managers.ChoiceAiService, managers.GameFlagManager);
        ConfigureControllers(player, managers, ui, terminal, globalVolume);
        WireButtons(ui);
        ConfigureOpeningPoses(player.Camera.transform, ui.FadeImage, managers.OpeningSequence);
        ApplyBuilderFontToTextChildren(ui.Canvas.transform);
        ApplyBuilderFontToTextChildren(terminal.transform);
        ImproveGeneratedUiReadability(ui, terminal.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = managers.OpeningManager;
        Debug.Log("Built Step 1 GameScene at Assets/Scenes/GameScene.unity.");
    }

    private static Scene OpenOrCreateScene()
    {
        Scene scene;
        if (System.IO.File.Exists(ScenePath))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        return scene;
    }

    private static GameObject CreateDirectionalLight()
    {
        GameObject lightObject = GetOrCreateRoot("Directional Light");
        Light light = GetOrAdd<Light>(lightObject);
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(0.86f, 0.9f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        return lightObject;
    }

    private static Volume CreateGlobalVolume()
    {
        GameObject volumeObject = GetOrCreateRoot("Global Volume");
        Volume volume = GetOrAdd<Volume>(volumeObject);
        volume.isGlobal = true;
        volume.priority = 0f;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        if (!profile.TryGet(out DepthOfField depthOfField))
        {
            depthOfField = profile.Add<DepthOfField>(true);
        }

        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(0.1f);
        depthOfField.gaussianEnd.Override(0.2f);
        depthOfField.highQualitySampling.Override(true);
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
        return volume;
    }

    private static void BuildEnvironment(Transform parent, Materials materials)
    {
        GameObject floor = CreateCube("Floor", parent, new Vector3(0f, -0.05f, 0f), new Vector3(9f, 0.1f, 9f), materials.Floor);
        floor.layer = 0;

        GameObject walls = GetOrCreateChild(parent, "Walls");
        CreateCube("BackWall", walls.transform, new Vector3(0f, 1.5f, 4.5f), new Vector3(9f, 3f, 0.15f), materials.Wall);
        CreateCube("FrontWall", walls.transform, new Vector3(0f, 1.5f, -4.5f), new Vector3(9f, 3f, 0.15f), materials.Wall);
        CreateCube("LeftWall", walls.transform, new Vector3(-4.5f, 1.5f, 0f), new Vector3(0.15f, 3f, 9f), materials.Wall);
        CreateCube("RightWall", walls.transform, new Vector3(4.5f, 1.5f, 0f), new Vector3(0.15f, 3f, 9f), materials.Wall);

        CreateCube("Bed", parent, new Vector3(-1.9f, 0.35f, -1.2f), new Vector3(2.1f, 0.55f, 0.95f), materials.Bed);
        CreateCube("SideTable", parent, new Vector3(-0.45f, 0.35f, -1.2f), new Vector3(0.55f, 0.7f, 0.55f), materials.Metal);
        CreateCube("Locker", parent, new Vector3(3.4f, 1.0f, 2.8f), new Vector3(0.8f, 2.0f, 0.6f), materials.Metal);
        CreateCube("MetalDoor", parent, new Vector3(0f, 1.15f, 4.42f), new Vector3(1.25f, 2.3f, 0.12f), materials.Door);

        GameObject terminal = GetOrCreateChild(parent, "Terminal");
        terminal.transform.position = new Vector3(-0.45f, 0.86f, -1.2f);
        terminal.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        terminal.transform.localScale = Vector3.one;
        BoxCollider terminalCollider = GetOrAdd<BoxCollider>(terminal);
        terminalCollider.size = new Vector3(0.5f, 0.12f, 0.35f);

        CreateCube("TerminalBody", terminal.transform, Vector3.zero, new Vector3(0.5f, 0.08f, 0.35f), materials.TerminalBody);
        GameObject terminalScreen = CreateCube("TerminalScreen", terminal.transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.43f, 0.01f, 0.27f), materials.TerminalScreen);
        terminalScreen.transform.localRotation = Quaternion.identity;

        Canvas worldCanvas = GetOrCreateWorldCanvas(terminal.transform);
        worldCanvas.transform.localPosition = new Vector3(0f, 0.11f, 0f);
        worldCanvas.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        worldCanvas.transform.localScale = Vector3.one * 0.0015f;
        EnsureWorldTerminalText(worldCanvas.transform);
    }

    private static PlayerRefs BuildPlayer()
    {
        GameObject player = GetOrCreateRoot("Player");
        player.transform.position = new Vector3(-0.1f, 0f, -2.3f);
        player.transform.rotation = Quaternion.identity;

        CharacterController characterController = GetOrAdd<CharacterController>(player);
        characterController.height = 1.8f;
        characterController.radius = 0.32f;
        characterController.center = new Vector3(0f, 0.9f, 0f);

        PlayerInput playerInput = GetOrAdd<PlayerInput>(player);
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions != null)
        {
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
        }
        else
        {
            Debug.LogWarning($"Input actions asset was not found: {InputActionsPath}");
        }

        PlayerInputController inputController = GetOrAdd<PlayerInputController>(player);
        ConfigureInputController(inputController, inputActions);

        GameObject cameraObject = GetOrCreateChild(player.transform, "Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = GetOrAdd<Camera>(cameraObject);
        camera.fieldOfView = 70f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        UniversalAdditionalCameraData cameraData = GetOrAdd<UniversalAdditionalCameraData>(cameraObject);
        cameraData.renderPostProcessing = true;
        GetOrAdd<AudioListener>(cameraObject);

        FirstPersonController firstPerson = GetOrAdd<FirstPersonController>(player);
        PlayerInteraction interaction = GetOrAdd<PlayerInteraction>(player);

        return new PlayerRefs(player, camera, inputController, firstPerson, interaction);
    }

    private static ManagerRefs BuildManagers()
    {
        GameObject gameStateManager = GetOrCreateRoot("GameStateManager");
        GameObject gameFlagManager = GetOrCreateRoot("GameFlagManager");
        GameObject aiConversationService = GetOrCreateRoot("AIConversationService");
        GameObject openingManager = GetOrCreateRoot("OpeningManager");

        return new ManagerRefs(
            gameStateManager,
            gameFlagManager,
            aiConversationService,
            openingManager,
            GetOrAdd<GameStateController>(gameStateManager),
            GetOrAdd<GameFlagManager>(gameFlagManager),
            GetOrAdd<ChoiceAIService>(aiConversationService),
            GetOrAdd<OpeningSequenceController>(openingManager),
            GetOrAdd<SceneTransitionController>(gameStateManager),
            GetOrAdd<QuitHandler>(gameStateManager));
    }

    private static UiRefs BuildGameCanvas()
    {
        GameObject canvasObject = GetOrCreateRoot("GameCanvas");
        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAdd<GraphicRaycaster>(canvasObject);

        EnsureEventSystem();

        Image fadeImage = CreateImage("FadeImage", canvasObject.transform, StretchRect(), new Color(0f, 0f, 0f, 1f));
        fadeImage.raycastTarget = false;

        TextMeshProUGUI subtitleText = CreateText("SubtitleText", canvasObject.transform, "おはようございます。", 34, TextAlignmentOptions.Center, new Color(0.92f, 0.96f, 1f));
        SetAnchoredRect(subtitleText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 125f), new Vector2(900f, 70f));
        CanvasGroup subtitleGroup = GetOrAdd<CanvasGroup>(subtitleText.gameObject);
        subtitleGroup.alpha = 0f;

        TextMeshProUGUI promptText = CreateText("InteractionPromptText", canvasObject.transform, "E：調べる", 26, TextAlignmentOptions.Center, Color.white);
        SetAnchoredRect(promptText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(260f, 44f));
        promptText.gameObject.SetActive(false);

        GameObject terminalPanel = CreatePanel("TerminalPanel", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(560f, 320f), new Color(0.015f, 0.023f, 0.028f, 0.98f));
        CreateText("TitleText", terminalPanel.transform, "生活支援AI", 38, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 92f), new Vector2(460f, 58f));
        CreateText("MessageText", terminalPanel.transform, "チャットを開始しますか？", 30, TextAlignmentOptions.Center, new Color(0.96f, 0.99f, 1f, 1f), new Vector2(0f, 24f), new Vector2(470f, 52f));
        Button terminalStartButton = CreateButton("StartButton", terminalPanel.transform, "開始", new Vector2(-92f, -88f), new Vector2(160f, 54f));
        Button terminalCloseButton = CreateButton("CloseButton", terminalPanel.transform, "閉じる", new Vector2(92f, -88f), new Vector2(160f, 54f));
        terminalPanel.SetActive(false);

        GameObject chatPanel = CreatePanel("ChatPanel", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(980f, 720f), new Color(0.012f, 0.016f, 0.02f, 0.98f));
        TextMeshProUGUI aiNameText = CreateText("AINameText", chatPanel.transform, "生活支援AI", 34, TextAlignmentOptions.Left, Color.white, new Vector2(-380f, 305f), new Vector2(320f, 52f));
        RectTransform historyContent = CreateScrollView("HistoryScrollView", chatPanel.transform, new Vector2(-170f, 55f), new Vector2(570f, 500f)).Content;
        RectTransform choicesContent = CreateScrollView("QuestionChoicesScrollView", chatPanel.transform, new Vector2(300f, 55f), new Vector2(310f, 500f)).Content;
        Button chatCloseButton = CreateButton("CloseButton", chatPanel.transform, "閉じる", new Vector2(390f, 305f), new Vector2(130f, 44f));
        TextMeshProUGUI messageTextPrefab = CreateText("MessageTextPrefab", chatPanel.transform, "System: Message", 24, TextAlignmentOptions.Left, Color.white, new Vector2(0f, -320f), new Vector2(500f, 64f));
        messageTextPrefab.gameObject.SetActive(false);
        Button choiceButtonPrefab = CreateButton("ChoiceButtonPrefab", chatPanel.transform, "質問候補", new Vector2(0f, -370f), new Vector2(292f, 58f));
        choiceButtonPrefab.gameObject.SetActive(false);
        chatPanel.SetActive(false);

        GameObject pausePanel = CreatePanel("PausePanel", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(460f, 360f), new Color(0.02f, 0.025f, 0.03f, 0.94f));
        Button resumeButton = CreateButton("ResumeButton", pausePanel.transform, "ゲームに戻る", new Vector2(0f, 80f), new Vector2(260f, 52f));
        Button returnTitleButton = CreateButton("ReturnTitleButton", pausePanel.transform, "タイトルへ戻る", new Vector2(0f, 10f), new Vector2(260f, 52f));
        Button quitButton = CreateButton("QuitButton", pausePanel.transform, "ゲームを終了する", new Vector2(0f, -60f), new Vector2(260f, 52f));
        pausePanel.SetActive(false);

        return new UiRefs(
            canvasObject,
            fadeImage,
            subtitleText,
            subtitleGroup,
            promptText,
            terminalPanel,
            terminalStartButton,
            terminalCloseButton,
            chatPanel,
            aiNameText,
            historyContent,
            choicesContent,
            messageTextPrefab,
            choiceButtonPrefab,
            chatCloseButton,
            pausePanel,
            resumeButton,
            returnTitleButton,
            quitButton,
            GetOrAdd<SubtitleController>(canvasObject),
            GetOrAdd<InteractionPromptController>(canvasObject),
            GetOrAdd<TerminalUIController>(canvasObject),
            GetOrAdd<AIChatUIController>(canvasObject),
            GetOrAdd<PauseMenuController>(canvasObject));
    }

    private static TerminalInteractable ConfigureTerminal(Transform environment, Materials materials, TerminalUIController terminalUi)
    {
        GameObject terminal = GetOrCreateChild(environment, "Terminal");
        TerminalInteractable terminalInteractable = GetOrAdd<TerminalInteractable>(terminal);
        Renderer screenRenderer = GetOrCreateChild(terminal.transform, "TerminalScreen").GetComponent<Renderer>();
        Canvas worldCanvas = GetOrCreateWorldCanvas(terminal.transform);

        SetSerializedObject(terminalInteractable, "terminalUIController", terminalUi);
        SetSerializedObject(terminalInteractable, "screenRenderer", screenRenderer);
        SetSerializedObject(terminalInteractable, "worldSpaceCanvas", worldCanvas.gameObject);

        if (screenRenderer != null)
        {
            screenRenderer.sharedMaterial = materials.TerminalScreen;
        }

        return terminalInteractable;
    }

    private static void ConfigureChoiceAIService(ChoiceAIService service, GameFlagManager flagManager)
    {
        List<AIQuestionData> questions = new List<AIQuestionData>();
        foreach (string guid in AssetDatabase.FindAssets("t:AIQuestionData", new[] { QuestionFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AIQuestionData question = AssetDatabase.LoadAssetAtPath<AIQuestionData>(path);
            if (question != null)
            {
                questions.Add(question);
            }
        }

        questions.Sort((left, right) => string.Compare(left.QuestionId, right.QuestionId, System.StringComparison.Ordinal));

        SerializedObject serializedService = new SerializedObject(service);
        SerializedProperty questionList = serializedService.FindProperty("questions");
        questionList.arraySize = questions.Count;
        for (int i = 0; i < questions.Count; i++)
        {
            questionList.GetArrayElementAtIndex(i).objectReferenceValue = questions[i];
        }

        serializedService.FindProperty("gameFlagManager").objectReferenceValue = flagManager;
        serializedService.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureControllers(PlayerRefs player, ManagerRefs managers, UiRefs ui, TerminalInteractable terminal, Volume volume)
    {
        SetSerializedObject(managers.GameStateController, "firstPersonController", player.FirstPersonController);
        SetSerializedObject(managers.GameStateController, "playerInteraction", player.PlayerInteraction);

        SetSerializedObject(player.FirstPersonController, "inputController", player.InputController);
        SetSerializedObject(player.FirstPersonController, "cameraRoot", player.Camera.transform);

        SetSerializedObject(player.PlayerInteraction, "inputController", player.InputController);
        SetSerializedObject(player.PlayerInteraction, "playerCamera", player.Camera);
        SetSerializedObject(player.PlayerInteraction, "promptController", ui.InteractionPromptController);

        SetSerializedObject(ui.SubtitleController, "subtitleGroup", ui.SubtitleGroup);
        SetSerializedObject(ui.SubtitleController, "subtitleText", ui.SubtitleText);

        SetSerializedObject(ui.InteractionPromptController, "promptText", ui.PromptText);

        SetSerializedObject(ui.TerminalUi, "terminalPanel", ui.TerminalPanel);
        SetSerializedObject(ui.TerminalUi, "gameStateController", managers.GameStateController);
        SetSerializedObject(ui.TerminalUi, "chatUIController", ui.ChatUi);

        SetSerializedObject(ui.ChatUi, "chatPanel", ui.ChatPanel);
        SetSerializedObject(ui.ChatUi, "gameStateController", managers.GameStateController);
        SetSerializedObject(ui.ChatUi, "terminalUIController", ui.TerminalUi);
        SetSerializedObject(ui.ChatUi, "conversationService", managers.ChoiceAiService);
        SetSerializedObject(ui.ChatUi, "aiNameText", ui.AiNameText);
        SetSerializedObject(ui.ChatUi, "historyContent", ui.HistoryContent);
        SetSerializedObject(ui.ChatUi, "questionChoicesContent", ui.ChoicesContent);
        SetSerializedObject(ui.ChatUi, "messageTextPrefab", ui.MessageTextPrefab);
        SetSerializedObject(ui.ChatUi, "choiceButtonPrefab", ui.ChoiceButtonPrefab);

        SetSerializedObject(ui.PauseMenu, "pausePanel", ui.PausePanel);
        SetSerializedObject(ui.PauseMenu, "gameStateController", managers.GameStateController);
        SetSerializedObject(ui.PauseMenu, "inputController", player.InputController);
        SetSerializedObject(ui.PauseMenu, "chatUIController", ui.ChatUi);
        SetSerializedObject(ui.PauseMenu, "terminalUIController", ui.TerminalUi);
        SetSerializedObject(ui.PauseMenu, "sceneTransitionController", managers.SceneTransitionController);
        SetSerializedObject(ui.PauseMenu, "quitHandler", managers.QuitHandler);

        SetSerializedObject(managers.OpeningSequence, "gameStateController", managers.GameStateController);
        SetSerializedObject(managers.OpeningSequence, "playerCamera", player.Camera.transform);
        SetSerializedObject(managers.OpeningSequence, "fadeImage", ui.FadeImage);
        SetSerializedObject(managers.OpeningSequence, "postProcessVolume", volume);
        SetSerializedObject(managers.OpeningSequence, "subtitleController", ui.SubtitleController);
        SetSerializedObject(managers.OpeningSequence, "terminal", terminal);
    }

    private static void ConfigureOpeningPoses(Transform cameraTransform, Image fadeImage, OpeningSequenceController opening)
    {
        GameObject poses = GetOrCreateRoot("OpeningCameraPoses");
        GameObject lyingPose = GetOrCreateChild(poses.transform, "LyingCameraPose");
        GameObject standingPose = GetOrCreateChild(poses.transform, "StandingCameraPose");

        lyingPose.transform.position = new Vector3(-1.9f, 1.05f, -1.2f);
        lyingPose.transform.rotation = Quaternion.Euler(-82f, 0f, 0f);
        standingPose.transform.position = cameraTransform.position;
        standingPose.transform.rotation = cameraTransform.rotation;

        SetSerializedObject(opening, "lyingCameraPose", lyingPose.transform);
        SetSerializedObject(opening, "standingCameraPose", standingPose.transform);

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
        }
    }

    private static void ConfigureInputController(PlayerInputController inputController, InputActionAsset inputActions)
    {
        if (inputController == null || inputActions == null)
        {
            return;
        }

        SetSerializedObject(inputController, "moveAction", GetOrCreateInputActionReference(inputActions, "Player", "Move"));
        SetSerializedObject(inputController, "lookAction", GetOrCreateInputActionReference(inputActions, "Player", "Look"));
        SetSerializedObject(inputController, "interactAction", GetOrCreateInputActionReference(inputActions, "Player", "Interact"));
        SetSerializedObject(inputController, "sprintAction", GetOrCreateInputActionReference(inputActions, "Player", "Sprint"));
        SetSerializedObject(inputController, "pauseAction", GetOrCreateInputActionReference(inputActions, "Player", "Pause"));
    }

    private static InputActionReference GetOrCreateInputActionReference(InputActionAsset asset, string mapName, string actionName)
    {
        EnsureFolder(InputReferenceFolder);

        string referencePath = $"{InputReferenceFolder}/{mapName}_{actionName}.asset";
        InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referencePath);
        InputAction action = asset.FindAction($"{mapName}/{actionName}", false);

        if (action == null)
        {
            Debug.LogWarning($"Input action was not found: {mapName}/{actionName}");
            return reference;
        }

        if (reference == null)
        {
            reference = InputActionReference.Create(action);
            AssetDatabase.CreateAsset(reference, referencePath);
        }
        else
        {
            reference.Set(action);
            EditorUtility.SetDirty(reference);
        }

        return reference;
    }

    private static void WireButtons(UiRefs ui)
    {
        SetButtonAction(ui.TerminalStartButton, ui.TerminalUi, nameof(TerminalUIController.OnStartChatClicked));
        SetButtonAction(ui.TerminalCloseButton, ui.TerminalUi, nameof(TerminalUIController.Close));
        SetButtonAction(ui.ChatCloseButton, ui.ChatUi, nameof(AIChatUIController.Close));
        SetButtonAction(ui.ResumeButton, ui.PauseMenu, nameof(PauseMenuController.ResumeGame));
        SetButtonAction(ui.ReturnTitleButton, ui.PauseMenu, nameof(PauseMenuController.ReturnToTitle));
        SetButtonAction(ui.QuitButton, ui.PauseMenu, nameof(PauseMenuController.QuitGame));
    }

    private static void SetButtonAction(Button button, Object target, string methodName)
    {
        if (button == null || target == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        UnityAction action = System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName) as UnityAction;
        if (action != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }
    }

    private static Canvas GetOrCreateWorldCanvas(Transform parent)
    {
        GameObject canvasObject = GetOrCreateChild(parent, "WorldSpaceCanvas");
        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 240f);
        GetOrAdd<GraphicRaycaster>(canvasObject);
        return canvas;
    }

    private static void EnsureWorldTerminalText(Transform parent)
    {
        TextMeshProUGUI title = CreateText("TitleText", parent, "生活支援AI", 48, TextAlignmentOptions.Center, new Color(0.92f, 1f, 1f, 1f));
        SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(360f, 58f));
        TextMeshProUGUI message = CreateText("MessageText", parent, "チャットを開始しますか？", 34, TextAlignmentOptions.Center, Color.white);
        SetAnchoredRect(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(380f, 50f));
        TextMeshProUGUI start = CreateText("StartLabelText", parent, "［開始］", 36, TextAlignmentOptions.Center, new Color(0.92f, 1f, 1f, 1f));
        SetAnchoredRect(start.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(260f, 50f));
    }

    private static ScrollViewRefs CreateScrollView(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject scrollObject = GetOrCreateChild(parent, name);
        RectTransform scrollRectTransform = GetOrAdd<RectTransform>(scrollObject);
        SetAnchoredRect(scrollRectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
        Image background = GetOrAdd<Image>(scrollObject);
        background.color = new Color(0.025f, 0.032f, 0.04f, 0.98f);

        ScrollRect scrollRect = GetOrAdd<ScrollRect>(scrollObject);
        GameObject viewport = GetOrCreateChild(scrollObject.transform, "Viewport");
        RectTransform viewportRect = GetOrAdd<RectTransform>(viewport);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -8f);
        Image viewportImage = GetOrAdd<Image>(viewport);
        viewportImage.color = new Color(0.02f, 0.026f, 0.032f, 0.9f);
        Mask mask = GetOrAdd<Mask>(viewport);
        mask.showMaskGraphic = false;

        GameObject content = GetOrCreateChild(viewport.transform, "Content");
        RectTransform contentRect = GetOrAdd<RectTransform>(content);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 8f;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return new ScrollViewRefs(scrollObject, contentRect);
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = GetOrCreateChild(parent, name);
        RectTransform rect = GetOrAdd<RectTransform>(buttonObject);
        SetAnchoredRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
        Image image = GetOrAdd<Image>(buttonObject);
        image.color = new Color(0.12f, 0.22f, 0.28f, 1f);
        Button button = GetOrAdd<Button>(buttonObject);
        button.targetGraphic = image;
        ApplyReadableButtonColors(button, image);

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 24, TextAlignmentOptions.Center, Color.white);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.raycastTarget = false;

        return button;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject panel = GetOrCreateChild(parent, name);
        RectTransform rect = GetOrAdd<RectTransform>(panel);
        SetAnchoredRect(rect, anchor, anchor, Vector2.zero, size);
        Image image = GetOrAdd<Image>(panel);
        image.color = color;
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, RectData rectData, Color color)
    {
        GameObject imageObject = GetOrCreateChild(parent, name);
        RectTransform rect = GetOrAdd<RectTransform>(imageObject);
        ApplyRect(rect, rectData);
        Image image = GetOrAdd<Image>(imageObject);
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = GetOrCreateChild(parent, name);
        TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(textObject);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Opaque(color);
        tmp.enableWordWrapping = true;
        ApplyBuilderFont(tmp);
        return tmp;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        TextMeshProUGUI tmp = CreateText(name, parent, text, fontSize, alignment, color);
        SetAnchoredRect(tmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
        return tmp;
    }

    private static void ImproveGeneratedUiReadability(UiRefs ui, Transform terminal)
    {
        SetImageColor(ui.TerminalPanel, new Color(0.015f, 0.023f, 0.028f, 0.98f));
        SetImageColor(ui.ChatPanel, new Color(0.012f, 0.016f, 0.02f, 0.98f));
        SetImageColor(ui.PausePanel, new Color(0.012f, 0.016f, 0.02f, 0.98f));

        ApplyReadableTextStyle(ui.TerminalPanel, 26f);
        ApplyReadableTextStyle(ui.ChatPanel, 24f);
        ApplyReadableTextStyle(ui.PausePanel, 24f);

        ApplyReadableTextStyle(terminal != null ? terminal.gameObject : null, 34f);

        foreach (Button button in ui.Canvas.GetComponentsInChildren<Button>(true))
        {
            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            ApplyReadableButtonColors(button, image);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                ApplyReadableText(label, 24f);
            }
        }
    }

    private static void ApplyReadableTextStyle(GameObject root, float minimumFontSize)
    {
        if (root == null)
        {
            return;
        }

        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            ApplyReadableText(text, minimumFontSize);
        }
    }

    private static void ApplyReadableText(TextMeshProUGUI text, float minimumFontSize)
    {
        if (text == null)
        {
            return;
        }

        text.color = Color.white;
        if (text.fontSize < minimumFontSize)
        {
            text.fontSize = minimumFontSize;
        }

        text.alpha = 1f;
        text.enableWordWrapping = true;
        ApplyBuilderFont(text);
        EditorUtility.SetDirty(text);
    }

    private static void ApplyReadableButtonColors(Button button, Image image)
    {
        if (button == null)
        {
            return;
        }

        if (image != null)
        {
            image.color = new Color(0.12f, 0.22f, 0.28f, 1f);
            EditorUtility.SetDirty(image);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.22f, 0.28f, 1f);
        colors.highlightedColor = new Color(0.18f, 0.34f, 0.42f, 1f);
        colors.pressedColor = new Color(0.06f, 0.14f, 0.18f, 1f);
        colors.selectedColor = new Color(0.16f, 0.3f, 0.38f, 1f);
        colors.disabledColor = new Color(0.08f, 0.09f, 0.1f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        EditorUtility.SetDirty(button);
    }

    private static void SetImageColor(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = Opaque(color);
            EditorUtility.SetDirty(image);
        }
    }

    private static Color Opaque(Color color)
    {
        color.a = 1f;
        return color;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject cube = GetOrCreateChild(parent, name);
        if (cube.GetComponent<MeshFilter>() == null)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MeshFilter meshFilter = GetOrAdd<MeshFilter>(cube);
            MeshRenderer meshRenderer = GetOrAdd<MeshRenderer>(cube);
            meshFilter.sharedMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(primitive);
            BoxCollider collider = GetOrAdd<BoxCollider>(cube);
            collider.size = Vector3.one;
            meshRenderer.sharedMaterial = material;
        }

        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return cube;
    }

    private static Materials CreateMaterials()
    {
        EnsureFolder("Assets/Materials");
        return new Materials(
            CreateMaterial("Step1_Floor", new Color(0.38f, 0.42f, 0.43f)),
            CreateMaterial("Step1_Wall", new Color(0.74f, 0.78f, 0.78f)),
            CreateMaterial("Step1_Bed", new Color(0.88f, 0.9f, 0.92f)),
            CreateMaterial("Step1_Metal", new Color(0.32f, 0.35f, 0.36f)),
            CreateMaterial("Step1_Door", new Color(0.2f, 0.23f, 0.24f)),
            CreateMaterial("Step1_TerminalBody", new Color(0.05f, 0.06f, 0.065f)),
            CreateMaterial("Step1_TerminalScreen", new Color(0.01f, 0.08f, 0.09f)));
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = ResolveSceneMaterialShader();
        if (material == null)
        {
            if (shader == null)
            {
                Debug.LogError($"Step 1 material was not created because no compatible shader was found: {path}");
                return null;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        SetMaterialColor(material, color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader ResolveSceneMaterialShader()
    {
        RenderPipelineAsset currentPipeline = GraphicsSettings.currentRenderPipeline;
        bool isUrpActive = IsUniversalRenderPipeline(currentPipeline);

        string[] shaderNames = isUrpActive
            ? new[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit"
            }
            : new[]
            {
                "Standard",
                "Unlit/Color"
            };

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                return shader;
            }
        }

        string pipelineName = currentPipeline == null ? "Built-in Render Pipeline" : currentPipeline.GetType().Name;
        Debug.LogError($"No compatible shader was found for generated Step 1 materials. Active pipeline: {pipelineName}");
        return null;
    }

    private static bool IsUniversalRenderPipeline(RenderPipelineAsset pipelineAsset)
    {
        if (pipelineAsset == null)
        {
            return false;
        }

        System.Type pipelineType = pipelineAsset.GetType();
        return pipelineType.Name.Contains("UniversalRenderPipelineAsset")
            || pipelineType.FullName.Contains("Universal.RenderPipeline");
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void ApplyBuilderFontToTextChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            ApplyBuilderFont(text);
        }
    }

    private static void ApplyBuilderFont(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset fontAsset = ResolveBuilderTmpFontAsset();
        if (fontAsset == null)
        {
            return;
        }

        text.font = fontAsset;
        ResetTmpMaterialPreset(text, fontAsset);
        EditorUtility.SetDirty(text);
    }

    private static void ResetTmpMaterialPreset(TextMeshProUGUI text, TMP_FontAsset fontAsset)
    {
        if (text == null || fontAsset == null || fontAsset.material == null)
        {
            return;
        }

        text.fontSharedMaterial = fontAsset.material;
        ResetTmpSdfMaterialProperties(text.fontSharedMaterial);
    }

    private static void ResetTmpSdfMaterialProperties(Material material)
    {
        if (material == null)
        {
            return;
        }

        SetMaterialFloatIfExists(material, "_FaceDilate", 0f);
        SetMaterialFloatIfExists(material, "_OutlineWidth", 0f);
        SetMaterialFloatIfExists(material, "_OutlineSoftness", 0f);
        SetMaterialFloatIfExists(material, "_UnderlaySoftness", 0f);
        SetMaterialColorIfExists(material, "_FaceColor", Color.white);
        SetMaterialColorIfExists(material, "_OutlineColor", new Color(0f, 0f, 0f, 1f));
        EditorUtility.SetDirty(material);
    }

    private static void SetMaterialFloatIfExists(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetMaterialColorIfExists(Material material, string propertyName, Color value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static TMP_FontAsset ResolveBuilderTmpFontAsset()
    {
        if (cachedTmpFontAsset != null)
        {
            return cachedTmpFontAsset;
        }

        TMP_FontAsset japaneseFont = FindJapaneseTmpFontAsset();
        if (japaneseFont != null)
        {
            cachedTmpFontAsset = japaneseFont;
            return cachedTmpFontAsset;
        }

        TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackTmpFontAssetPath);
        if (!warnedMissingJapaneseFont)
        {
            warnedMissingJapaneseFont = true;
            Debug.LogWarning(
                "Japanese TMP Font Asset was not found. Builder searched: "
                + string.Join(", ", JapaneseTmpFontSearchFolders)
                + $". Falling back to {FallbackTmpFontAssetPath}. Japanese glyph missing warnings may remain until a Japanese TMP Font Asset is added.");
        }

        cachedTmpFontAsset = fallbackFont;
        return cachedTmpFontAsset;
    }

    private static TMP_FontAsset FindJapaneseTmpFontAsset()
    {
        string[] validFolders = GetValidFontSearchFolders();
        if (validFolders.Length == 0)
        {
            return null;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset", validFolders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (SupportsRequiredJapaneseGlyphs(fontAsset))
            {
                return fontAsset;
            }
        }

        return null;
    }

    private static string[] GetValidFontSearchFolders()
    {
        List<string> validFolders = new List<string>();
        foreach (string folder in JapaneseTmpFontSearchFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                validFolders.Add(folder);
            }
        }

        return validFolders.ToArray();
    }

    private static bool SupportsRequiredJapaneseGlyphs(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return false;
        }

        const string sampleText = "生活支援現在地医療区画施設内担当扉解除コード教えてください何ができますかゲームに戻るタイトルへ終了調べる開始";
        foreach (char character in sampleText)
        {
            if (!fontAsset.HasCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    private static void NormalizeSceneCamerasAndAudio(Camera gameplayCamera)
    {
        if (gameplayCamera == null)
        {
            return;
        }

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera == null)
            {
                continue;
            }

            camera.gameObject.tag = camera == gameplayCamera ? "MainCamera" : "Untagged";
        }

        AudioListener[] gameplayListeners = gameplayCamera.GetComponents<AudioListener>();
        if (gameplayListeners.Length == 0)
        {
            gameplayCamera.gameObject.AddComponent<AudioListener>();
            gameplayListeners = gameplayCamera.GetComponents<AudioListener>();
        }

        for (int i = 0; i < gameplayListeners.Length; i++)
        {
            if (i == 0)
            {
                gameplayListeners[i].enabled = true;
            }
            else
            {
                Object.DestroyImmediate(gameplayListeners[i]);
            }
        }

        foreach (AudioListener listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (listener == null || listener.gameObject == gameplayCamera.gameObject)
            {
                continue;
            }

            Object.DestroyImmediate(listener);
        }
    }

    private static void EnsureEventSystem()
    {
        GameObject eventSystemObject = GameObject.Find("EventSystem");
        if (eventSystemObject == null)
        {
            eventSystemObject = new GameObject("EventSystem");
        }

        GetOrAdd<EventSystem>(eventSystemObject);
        GetOrAdd<InputSystemUIInputModule>(eventSystemObject);
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null && existing.transform.parent == null)
        {
            return existing;
        }

        return new GameObject(name);
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    private static void SetSerializedObject(Object target, string propertyName, Object value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"Serialized property was not found: {target.GetType().Name}.{propertyName}");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static RectData StretchRect()
    {
        return new RectData(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void ApplyRect(RectTransform rect, RectData data)
    {
        rect.anchorMin = data.AnchorMin;
        rect.anchorMax = data.AnchorMax;
        rect.offsetMin = data.OffsetMin;
        rect.offsetMax = data.OffsetMax;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folderPath);
        string folderName = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent.Replace("\\", "/"));
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private readonly struct RectData
    {
        public readonly Vector2 AnchorMin;
        public readonly Vector2 AnchorMax;
        public readonly Vector2 OffsetMin;
        public readonly Vector2 OffsetMax;

        public RectData(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            OffsetMin = offsetMin;
            OffsetMax = offsetMax;
        }
    }

    private readonly struct ScrollViewRefs
    {
        public readonly GameObject Root;
        public readonly RectTransform Content;

        public ScrollViewRefs(GameObject root, RectTransform content)
        {
            Root = root;
            Content = content;
        }
    }

    private readonly struct Materials
    {
        public readonly Material Floor;
        public readonly Material Wall;
        public readonly Material Bed;
        public readonly Material Metal;
        public readonly Material Door;
        public readonly Material TerminalBody;
        public readonly Material TerminalScreen;

        public Materials(Material floor, Material wall, Material bed, Material metal, Material door, Material terminalBody, Material terminalScreen)
        {
            Floor = floor;
            Wall = wall;
            Bed = bed;
            Metal = metal;
            Door = door;
            TerminalBody = terminalBody;
            TerminalScreen = terminalScreen;
        }
    }

    private readonly struct PlayerRefs
    {
        public readonly GameObject Player;
        public readonly Camera Camera;
        public readonly PlayerInputController InputController;
        public readonly FirstPersonController FirstPersonController;
        public readonly PlayerInteraction PlayerInteraction;

        public PlayerRefs(GameObject player, Camera camera, PlayerInputController inputController, FirstPersonController firstPersonController, PlayerInteraction playerInteraction)
        {
            Player = player;
            Camera = camera;
            InputController = inputController;
            FirstPersonController = firstPersonController;
            PlayerInteraction = playerInteraction;
        }
    }

    private readonly struct ManagerRefs
    {
        public readonly GameObject GameStateManager;
        public readonly GameObject GameFlagManagerObject;
        public readonly GameObject AIConversationService;
        public readonly GameObject OpeningManager;
        public readonly GameStateController GameStateController;
        public readonly GameFlagManager GameFlagManager;
        public readonly ChoiceAIService ChoiceAiService;
        public readonly OpeningSequenceController OpeningSequence;
        public readonly SceneTransitionController SceneTransitionController;
        public readonly QuitHandler QuitHandler;

        public ManagerRefs(
            GameObject gameStateManager,
            GameObject gameFlagManagerObject,
            GameObject aiConversationService,
            GameObject openingManager,
            GameStateController gameStateController,
            GameFlagManager gameFlagManager,
            ChoiceAIService choiceAiService,
            OpeningSequenceController openingSequence,
            SceneTransitionController sceneTransitionController,
            QuitHandler quitHandler)
        {
            GameStateManager = gameStateManager;
            GameFlagManagerObject = gameFlagManagerObject;
            AIConversationService = aiConversationService;
            OpeningManager = openingManager;
            GameStateController = gameStateController;
            GameFlagManager = gameFlagManager;
            ChoiceAiService = choiceAiService;
            OpeningSequence = openingSequence;
            SceneTransitionController = sceneTransitionController;
            QuitHandler = quitHandler;
        }
    }

    private readonly struct UiRefs
    {
        public readonly GameObject Canvas;
        public readonly Image FadeImage;
        public readonly TextMeshProUGUI SubtitleText;
        public readonly CanvasGroup SubtitleGroup;
        public readonly TextMeshProUGUI PromptText;
        public readonly GameObject TerminalPanel;
        public readonly Button TerminalStartButton;
        public readonly Button TerminalCloseButton;
        public readonly GameObject ChatPanel;
        public readonly TextMeshProUGUI AiNameText;
        public readonly Transform HistoryContent;
        public readonly Transform ChoicesContent;
        public readonly TextMeshProUGUI MessageTextPrefab;
        public readonly Button ChoiceButtonPrefab;
        public readonly Button ChatCloseButton;
        public readonly GameObject PausePanel;
        public readonly Button ResumeButton;
        public readonly Button ReturnTitleButton;
        public readonly Button QuitButton;
        public readonly SubtitleController SubtitleController;
        public readonly InteractionPromptController InteractionPromptController;
        public readonly TerminalUIController TerminalUi;
        public readonly AIChatUIController ChatUi;
        public readonly PauseMenuController PauseMenu;

        public UiRefs(
            GameObject canvas,
            Image fadeImage,
            TextMeshProUGUI subtitleText,
            CanvasGroup subtitleGroup,
            TextMeshProUGUI promptText,
            GameObject terminalPanel,
            Button terminalStartButton,
            Button terminalCloseButton,
            GameObject chatPanel,
            TextMeshProUGUI aiNameText,
            Transform historyContent,
            Transform choicesContent,
            TextMeshProUGUI messageTextPrefab,
            Button choiceButtonPrefab,
            Button chatCloseButton,
            GameObject pausePanel,
            Button resumeButton,
            Button returnTitleButton,
            Button quitButton,
            SubtitleController subtitleController,
            InteractionPromptController interactionPromptController,
            TerminalUIController terminalUi,
            AIChatUIController chatUi,
            PauseMenuController pauseMenu)
        {
            Canvas = canvas;
            FadeImage = fadeImage;
            SubtitleText = subtitleText;
            SubtitleGroup = subtitleGroup;
            PromptText = promptText;
            TerminalPanel = terminalPanel;
            TerminalStartButton = terminalStartButton;
            TerminalCloseButton = terminalCloseButton;
            ChatPanel = chatPanel;
            AiNameText = aiNameText;
            HistoryContent = historyContent;
            ChoicesContent = choicesContent;
            MessageTextPrefab = messageTextPrefab;
            ChoiceButtonPrefab = choiceButtonPrefab;
            ChatCloseButton = chatCloseButton;
            PausePanel = pausePanel;
            ResumeButton = resumeButton;
            ReturnTitleButton = returnTitleButton;
            QuitButton = quitButton;
            SubtitleController = subtitleController;
            InteractionPromptController = interactionPromptController;
            TerminalUi = terminalUi;
            ChatUi = chatUi;
            PauseMenu = pauseMenu;
        }
    }
}
#endif
