using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CreateHoraScene
{
    [MenuItem("Hora/Create Simple Scene")]
    public static void CreateSimpleScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material groundMaterial = CreateMaterial("Ground", new Color(0.07f, 0.09f, 0.07f));
        Material stoneMaterial = CreateMaterial("Stone", new Color(0.28f, 0.28f, 0.26f));
        Material woodMaterial = CreateMaterial("Wood", new Color(0.28f, 0.21f, 0.17f));
        Material redMaterial = CreateMaterial("ToriiRed", new Color(0.5f, 0.08f, 0.06f));
        Material blackMaterial = CreateMaterial("Black", new Color(0.03f, 0.02f, 0.02f));
        Material paperMaterial = CreateMaterial("OfudaPaper", new Color(0.82f, 0.75f, 0.52f));
        Material enemyMaterial = CreateMaterial("EnemyBlack", new Color(0.01f, 0.01f, 0.01f));

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.01f, 0.02f, 0.03f);
        RenderSettings.fogDensity = 0.055f;
        RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.12f);

        CreateCube("Ground", new Vector3(0f, -0.05f, 0f), new Vector3(80f, 0.1f, 80f), groundMaterial);
        CreateCube("Path", new Vector3(0f, 0.01f, 5f), new Vector3(4.2f, 0.06f, 28f), stoneMaterial);

        CreateShrine(woodMaterial, blackMaterial, stoneMaterial);
        CreateTorii("Exit Torii", new Vector3(0f, 0f, 18f), redMaterial, blackMaterial);
        GameObject sealedTorii = CreateCube("Sealed Torii", new Vector3(0f, 1.5f, 18.2f), new Vector3(3f, 3f, 0.2f), blackMaterial);

        GameObject bell = CreateCylinder("Bell", new Vector3(0f, 2.25f, -10.25f), new Vector3(0.44f, 0.45f, 0.44f), CreateMaterial("BellGold", new Color(0.72f, 0.62f, 0.32f)));

        List<GameObject> ofuda = new List<GameObject>
        {
            CreateCube("Ofuda 1", new Vector3(-7.45f, 1.6f, 0.55f), new Vector3(0.35f, 0.6f, 0.04f), paperMaterial),
            CreateCube("Ofuda 2", new Vector3(7f, 1.55f, -1.35f), new Vector3(0.35f, 0.6f, 0.04f), paperMaterial),
            CreateCube("Ofuda 3", new Vector3(6f, 1.8f, 4.05f), new Vector3(0.35f, 0.6f, 0.04f), paperMaterial)
        };

        CreateEnvironment(stoneMaterial, woodMaterial);

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 1.7f, 18f);
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.7f;
        controller.radius = 0.42f;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = Vector3.zero;
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 72f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 120f;
        AudioListener listener = cameraObject.AddComponent<AudioListener>();
        listener.enabled = true;

        GameObject flashlightObject = new GameObject("Flashlight");
        flashlightObject.transform.SetParent(cameraObject.transform);
        flashlightObject.transform.localPosition = Vector3.zero;
        flashlightObject.transform.localRotation = Quaternion.identity;
        Light flashlight = flashlightObject.AddComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.color = new Color(1f, 0.93f, 0.75f);
        flashlight.intensity = 11.5f;
        flashlight.range = 22f;
        flashlight.spotAngle = 49f;

        GameObject enemy = CreateEnemy(enemyMaterial);

        GameObject moonObject = new GameObject("Moon Light");
        Light moon = moonObject.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.62f, 0.72f, 0.85f);
        moon.intensity = 0.55f;
        moonObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        UiRefs ui = CreateUi();

        GameObject managerObject = new GameObject("GameManager");
        GameManager manager = managerObject.AddComponent<GameManager>();
        AssignGameManager(manager, controller, camera, flashlight, enemy, sealedTorii, bell, ofuda, ui);
        WireButtons(ui, manager);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        Selection.activeGameObject = managerObject;
        Debug.Log("Created Assets/Scenes/Main.unity. Press Play and click 開始.");
    }

    private static Material CreateMaterial(string name, Color color)
    {
        const string directory = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(directory))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        string path = $"{directory}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static void CreateShrine(Material wood, Material black, Material stone)
    {
        CreateCube("Shrine Base", new Vector3(0f, 0.18f, -11.7f), new Vector3(7f, 0.35f, 3.8f), stone);
        CreateCube("Shrine Body", new Vector3(0f, 1.7f, -13.2f), new Vector3(6.5f, 3f, 3f), wood);
        CreateCube("Shrine Roof", new Vector3(0f, 3.4f, -13.2f), new Vector3(7.5f, 0.45f, 3.7f), black);
        CreateCube("Offering Box", new Vector3(0f, 1.2f, -10.7f), new Vector3(1.1f, 1.1f, 0.4f), stone);
    }

    private static void CreateTorii(string name, Vector3 origin, Material red, Material black)
    {
        GameObject group = new GameObject(name);
        CreateCube($"{name} Left Pillar", origin + new Vector3(-1.65f, 2f, 0f), new Vector3(0.32f, 4f, 0.32f), red).transform.SetParent(group.transform);
        CreateCube($"{name} Right Pillar", origin + new Vector3(1.65f, 2f, 0f), new Vector3(0.32f, 4f, 0.32f), red).transform.SetParent(group.transform);
        CreateCube($"{name} Beam", origin + new Vector3(0f, 3.85f, 0f), new Vector3(4.4f, 0.36f, 0.44f), red).transform.SetParent(group.transform);
        CreateCube($"{name} Top Beam", origin + new Vector3(0f, 4.25f, 0f), new Vector3(5f, 0.28f, 0.5f), black).transform.SetParent(group.transform);
    }

    private static void CreateEnvironment(Material stone, Material wood)
    {
        CreateCube("Storehouse Left", new Vector3(-7.5f, 0.7f, -1f), new Vector3(3.6f, 1.4f, 2.4f), wood);
        CreateCube("Storehouse Left Roof", new Vector3(-7.5f, 1.55f, -1f), new Vector3(4.2f, 0.35f, 3f), stone);
        CreateCube("Storehouse Right", new Vector3(7.4f, 1.1f, -3.4f), new Vector3(4.4f, 2.2f, 3.1f), wood);
        CreateCube("Storehouse Right Roof", new Vector3(7.4f, 2.35f, -3.4f), new Vector3(5f, 0.35f, 3.6f), stone);
        CreateCube("Fence", new Vector3(6.9f, 1.2f, 4.2f), new Vector3(4.4f, 2f, 0.25f), wood);

        for (int i = 0; i < 20; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            float z = -8f + i * 1.45f;
            float x = side * (5.5f + (i % 5) * 0.75f);
            CreateCylinder($"Tree {i + 1}", new Vector3(x, 1.6f, z), new Vector3(0.5f, 1.9f + (i % 4) * 0.25f, 0.5f), wood);
        }

        float[] zs = { -6f, -1f, 4f, 9f };
        foreach (float z in zs)
        {
            foreach (float x in new[] { -2.8f, 2.8f })
            {
                CreateCube("Lantern Base", new Vector3(x, 0.6f, z), new Vector3(0.45f, 1.2f, 0.45f), stone);
                CreateCube("Lantern Top", new Vector3(x, 1.32f, z), new Vector3(0.75f, 0.28f, 0.75f), stone);
            }
        }
    }

    private static GameObject CreateEnemy(Material material)
    {
        GameObject enemy = new GameObject("Fox Monster");
        enemy.transform.position = new Vector3(0f, 0f, -15f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(enemy.transform);
        body.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        body.transform.localScale = new Vector3(0.9f, 0.9f, 1.35f);
        body.GetComponent<Renderer>().sharedMaterial = material;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(enemy.transform);
        head.transform.localPosition = new Vector3(0f, 1.35f, -0.72f);
        head.transform.localScale = new Vector3(0.6f, 0.68f, 0.82f);
        head.GetComponent<Renderer>().sharedMaterial = material;

        GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        snout.name = "Snout";
        snout.transform.SetParent(enemy.transform);
        snout.transform.localPosition = new Vector3(0f, 1.31f, -1.1f);
        snout.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        snout.transform.localScale = new Vector3(0.22f, 0.28f, 0.22f);
        snout.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Bone", new Color(0.86f, 0.78f, 0.62f));

        enemy.SetActive(false);
        return enemy;
    }

    private static UiRefs CreateUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        Text objective = CreateText("ObjectiveText", canvasObject.transform, font, "拝殿の鈴を鳴らせ", 16, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(520f, 40f));
        Text ofuda = CreateText("OfudaText", canvasObject.transform, font, "札 0/3", 14, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(140f, 30f));
        Text stamina = CreateText("StaminaText", canvasObject.transform, font, "スタミナ 100%", 14, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(150f, 24f), new Vector2(180f, 30f));
        Text blind = CreateText("BlindText", canvasObject.transform, font, "目くらまし 3/3 Q", 14, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(320f, 24f), new Vector2(220f, 30f));
        Text prompt = CreateText("PromptText", canvasObject.transform, font, "", 15, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(280f, 34f));
        CreateText("Crosshair", canvasObject.transform, font, "+", 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));

        GameObject startPanel = CreatePanel("StartPanel", canvasObject.transform);
        CreateText("Title", startPanel.transform, font, "鳥居を出るまで", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(420f, 48f));
        CreateText("IntroText", startPanel.transform, font, "深夜の神社で肝試しをする。拝殿の鈴を鳴らして戻るだけだった。", 15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(430f, 60f));
        Button startButton = CreateButton("StartButton", startPanel.transform, font, "開始", new Vector2(0.5f, 0.38f));

        GameObject endingPanel = CreatePanel("EndingPanel", canvasObject.transform);
        Text endingTitle = CreateText("EndingTitleText", endingPanel.transform, font, "", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(420f, 48f));
        Text endingBody = CreateText("EndingBodyText", endingPanel.transform, font, "", 15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(430f, 60f));
        Button retryButton = CreateButton("RetryButton", endingPanel.transform, font, "もう一度", new Vector2(0.5f, 0.38f));
        endingPanel.SetActive(false);

        return new UiRefs(startPanel, endingPanel, objective, ofuda, stamina, blind, prompt, endingTitle, endingBody, startButton, retryButton);
    }

    private static Text CreateText(string name, Transform parent, Font font, string value, int size, TextAnchor anchor, Vector2 anchorPosition, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorPosition;
        rect.anchorMax = anchorPosition;
        rect.pivot = anchorPosition;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return text;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private static Button CreateButton(string name, Transform parent, Font font, string label, Vector2 anchorPosition)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.84f, 0.78f, 0.55f);
        Button button = obj.AddComponent<Button>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorPosition;
        rect.anchorMax = anchorPosition;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(150f, 44f);

        Text text = CreateText("Text", obj.transform, font, label, 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 44f));
        text.color = new Color(0.08f, 0.07f, 0.04f);
        return button;
    }

    private static void AssignGameManager(GameManager manager, CharacterController controller, Camera camera, Light flashlight, GameObject enemy, GameObject sealedTorii, GameObject bell, List<GameObject> ofuda, UiRefs ui)
    {
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("playerController").objectReferenceValue = controller;
        serialized.FindProperty("playerCamera").objectReferenceValue = camera;
        serialized.FindProperty("flashlight").objectReferenceValue = flashlight;
        serialized.FindProperty("enemy").objectReferenceValue = enemy;
        serialized.FindProperty("sealedTorii").objectReferenceValue = sealedTorii;
        serialized.FindProperty("bell").objectReferenceValue = bell;

        SerializedProperty ofudaItems = serialized.FindProperty("ofudaItems");
        ofudaItems.arraySize = ofuda.Count;
        for (int i = 0; i < ofuda.Count; i++)
        {
            ofudaItems.GetArrayElementAtIndex(i).objectReferenceValue = ofuda[i];
        }

        serialized.FindProperty("startPanel").objectReferenceValue = ui.StartPanel;
        serialized.FindProperty("endingPanel").objectReferenceValue = ui.EndingPanel;
        serialized.FindProperty("objectiveText").objectReferenceValue = ui.ObjectiveText;
        serialized.FindProperty("ofudaText").objectReferenceValue = ui.OfudaText;
        serialized.FindProperty("staminaText").objectReferenceValue = ui.StaminaText;
        serialized.FindProperty("blindText").objectReferenceValue = ui.BlindText;
        serialized.FindProperty("promptText").objectReferenceValue = ui.PromptText;
        serialized.FindProperty("endingTitleText").objectReferenceValue = ui.EndingTitleText;
        serialized.FindProperty("endingBodyText").objectReferenceValue = ui.EndingBodyText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireButtons(UiRefs ui, GameManager manager)
    {
        UnityEventTools.AddPersistentListener(ui.StartButton.onClick, manager.StartGame);
        UnityEventTools.AddPersistentListener(ui.RetryButton.onClick, manager.Retry);
    }

    private readonly struct UiRefs
    {
        public UiRefs(GameObject startPanel, GameObject endingPanel, Text objectiveText, Text ofudaText, Text staminaText, Text blindText, Text promptText, Text endingTitleText, Text endingBodyText, Button startButton, Button retryButton)
        {
            StartPanel = startPanel;
            EndingPanel = endingPanel;
            ObjectiveText = objectiveText;
            OfudaText = ofudaText;
            StaminaText = staminaText;
            BlindText = blindText;
            PromptText = promptText;
            EndingTitleText = endingTitleText;
            EndingBodyText = endingBodyText;
            StartButton = startButton;
            RetryButton = retryButton;
        }

        public GameObject StartPanel { get; }
        public GameObject EndingPanel { get; }
        public Text ObjectiveText { get; }
        public Text OfudaText { get; }
        public Text StaminaText { get; }
        public Text BlindText { get; }
        public Text PromptText { get; }
        public Text EndingTitleText { get; }
        public Text EndingBodyText { get; }
        public Button StartButton { get; }
        public Button RetryButton { get; }
    }
}
