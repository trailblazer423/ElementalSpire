#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 只负责一次性的场景资产接线。运行时流程仍由各 Controller 管理，避免今后 UI 与功能组
/// 在同一份大场景 YAML 中反复覆盖彼此的逻辑。
/// </summary>
public static class FullLoopSceneIntegrator
{
    private const string EventScenePath = "Assets/Scenes/EventScene.unity";
    private const string RestScenePath = "Assets/Scenes/RestScene.unity";
    private const string EventBackgroundPath = "Assets/Prefabs/Image/A19EC7C48A040F5ACF27015395472BB8.jpg";
    private const string BlankStoneHeaderPath = "Assets/Prefabs/Image/34F020AAC2546804CBEC4D50B76D5AF2.png";
    private const string RestBackgroundPath = "Assets/Prefabs/Image/关卡背景/RestSceneBackground.png";
    private const string RestFallbackBackgroundPath = "Assets/Prefabs/Image/关卡背景/73b9b49331248926437da1a690d4e244.jpg";

    [MenuItem("Tools/ElementalSpire/Build Full Ten-Node Loop Scenes")]
    public static void Run()
    {
        EnsureFolder("Assets/Editor");
        ConfigureRestBackgroundImporter();
        CreateEventScene();
        CreateRestScene();
        AttachController<MainMenuController>("Assets/Scenes/MainMenuScene.unity", "MainMenuController");
        AttachController<RankingSceneController>("Assets/Scenes/RankingScene.unity", "RankingSceneController");
        NormalizeMapScene();
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FullLoopSceneIntegrator] 十节点流程场景已生成并接线完成。");
    }

    private static void CreateEventScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        Canvas canvas = CreateCanvas("EventCanvas");
        CreateFullScreenSprite(canvas.transform, "EventBackground", EventBackgroundPath, Color.white);
        CreateHeaderCover(canvas.transform);
        new GameObject("EventSceneController").AddComponent<EventSceneController>();
        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, EventScenePath);
    }

    private static void CreateRestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        Canvas canvas = CreateCanvas("RestCanvas");

        GameObject backgroundObject = new GameObject("RestBackground", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        Stretch(rect);
        Image background = backgroundObject.GetComponent<Image>();
        background.color = Color.white;
        background.raycastTarget = false;
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RestBackgroundPath);
        if (background.sprite == null)
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RestFallbackBackgroundPath);
        if (background.sprite == null)
        {
            background.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            Debug.LogWarning("[FullLoopSceneIntegrator] 尚未找到休息背景图：" + RestBackgroundPath);
        }

        new GameObject("RestSceneController").AddComponent<RestSceneController>();
        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, RestScenePath);
    }

    private static void NormalizeMapScene()
    {
        const string mapPath = "Assets/Scenes/MapScene.unity";
        Scene scene = EditorSceneManager.OpenScene(mapPath, OpenSceneMode.Single);

        foreach (MapNode node in Object.FindObjectsOfType<MapNode>(true))
        {
            node.NodeType = MapManager.GetNodeType(node.NodeId);
            EditorUtility.SetDirty(node);
            if (PrefabUtility.IsPartOfPrefabInstance(node))
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
        }

        GameObject eventPanel = FindSceneObject("EventChoicePanel");
        if (eventPanel != null)
            eventPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, mapPath);
    }

    private static void AttachController<T>(string scenePath, string objectName) where T : Component
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        T existing = Object.FindObjectOfType<T>(true);
        if (existing == null)
            new GameObject(objectName).AddComponent<T>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void EnsureBuildSettings()
    {
        string[] runSceneOrder =
        {
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/ElementSelectScene.unity",
            "Assets/Scenes/CardDraftScene.unity",
            "Assets/Scenes/MapScene.unity",
            "Assets/Scenes/BattleScene.unity",
            EventScenePath,
            RestScenePath,
            "Assets/Scenes/RankingScene.unity"
        };

        List<EditorBuildSettingsScene> existingScenes = EditorBuildSettings.scenes.ToList();
        var orderedScenes = new List<EditorBuildSettingsScene>();
        foreach (string path in runSceneOrder)
            orderedScenes.Add(new EditorBuildSettingsScene(path, true));

        foreach (EditorBuildSettingsScene scene in existingScenes)
        {
            if (runSceneOrder.Contains(scene.path))
                continue;
            orderedScenes.Add(scene);
        }

        EditorBuildSettings.scenes = orderedScenes.ToArray();
    }

    private static void ConfigureRestBackgroundImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(RestBackgroundPath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        if (changed)
            importer.SaveAndReimport();
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateSolidBackground(Transform parent, Color color)
    {
        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(parent, false);
        Stretch(backgroundObject.GetComponent<RectTransform>());
        Image image = backgroundObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void CreateFullScreenSprite(Transform parent, string name, string assetPath, Color fallbackColor)
    {
        GameObject backgroundObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(parent, false);
        Stretch(backgroundObject.GetComponent<RectTransform>());
        Image image = backgroundObject.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        image.color = image.sprite != null ? Color.white : fallbackColor;
        image.raycastTarget = false;
    }

    private static void CreateHeaderCover(Transform parent)
    {
        GameObject headerObject = new GameObject("BlankStoneHeader", typeof(RectTransform), typeof(Image));
        headerObject.transform.SetParent(parent, false);
        RectTransform rect = headerObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.84f);
        rect.anchorMax = new Vector2(0.92f, 0.99f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = headerObject.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BlankStoneHeaderPath);
        image.color = image.sprite != null ? Color.white : new Color(0.65f, 0.67f, 0.69f, 1f);
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>(true) == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
            {
                if (child.name == name)
                    return child.gameObject;
            }
        }

        return null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
