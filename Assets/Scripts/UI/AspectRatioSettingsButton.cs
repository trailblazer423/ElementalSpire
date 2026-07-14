using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 独立的屏幕比例设置入口。
/// 每个场景自动生成一个可见按钮，不依赖旧设置面板的结构。
/// </summary>
public sealed class AspectRatioSettingsButton : MonoBehaviour
{
    private const string AspectRatioPreferenceKey = "DisplayAspectRatio";
    private GameObject _panel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSetting()
    {
        if (PlayerPrefs.HasKey(AspectRatioPreferenceKey))
            ApplyAspectRatio(PlayerPrefs.GetFloat(AspectRatioPreferenceKey));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSceneButtonBootstrap()
    {
        if (FindObjectOfType<AspectRatioSettingsBootstrap>() != null)
            return;

        GameObject bootstrap = new GameObject("AspectRatioSettingsBootstrap");
        DontDestroyOnLoad(bootstrap);
        bootstrap.AddComponent<AspectRatioSettingsBootstrap>();
    }

    internal static bool CreateSceneButton()
    {
        if (GameObject.Find("AspectRatioSettingsButton") != null)
            return true;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return false;

        Font font = CardView.GetCompatibleFont();
        GameObject buttonObject = new GameObject("AspectRatioSettingsButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -96f);
        rect.sizeDelta = new Vector2(142f, 48f);
        buttonObject.GetComponent<Image>().color = new Color(0.14f, 0.32f, 0.52f, 0.96f);

        buttonObject.AddComponent<AspectRatioSettingsButton>();
        Text label = CreateText(buttonObject.transform, "Label", "屏幕比例", 19,
            Vector2.zero, new Vector2(130f, 40f), font);
        label.color = Color.white;
        return true;
    }

    private void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OpenPanel);
    }

    private void OnDestroy()
    {
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.RemoveListener(OpenPanel);
    }

    private void OpenPanel()
    {
        if (_panel == null)
            BuildPanel();

        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
    }

    private void BuildPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[AspectRatioSettings] 未找到 Canvas，无法显示屏幕比例设置。");
            return;
        }

        Font font = CardView.GetCompatibleFont();
        _panel = new GameObject("AspectRatioSettingsOverlay", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvas.transform, false);
        Image overlay = _panel.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.58f);
        Stretch(_panel.GetComponent<RectTransform>());

        GameObject window = new GameObject("AspectRatioWindow", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(_panel.transform, false);
        window.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.98f);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(540f, 330f);

        CreateText(window.transform, "Title", "屏幕比例", 30, new Vector2(0f, 118f), new Vector2(420f, 42f), font);
        Text hint = CreateText(window.transform, "Hint", "选择后立即生效，并自动保存", 17,
            new Vector2(0f, 78f), new Vector2(420f, 30f), font);
        hint.color = new Color(0.74f, 0.8f, 0.9f);

        CreateOption(window.transform, "自动适配", 0f, new Vector2(-160f, 18f), font);
        CreateOption(window.transform, "16:9", 16f / 9f, new Vector2(0f, 18f), font);
        CreateOption(window.transform, "16:10", 16f / 10f, new Vector2(160f, 18f), font);
        CreateOption(window.transform, "4:3", 4f / 3f, new Vector2(-80f, -48f), font);
        CreateOption(window.transform, "21:9", 21f / 9f, new Vector2(80f, -48f), font);

        Button closeButton = CreateButton(window.transform, "Close", "关闭", new Vector2(0f, -112f),
            new Vector2(140f, 44f), font, new Color(0.38f, 0.4f, 0.46f, 1f));
        closeButton.onClick.AddListener(() => _panel.SetActive(false));
        _panel.SetActive(false);
    }

    private void CreateOption(Transform parent, string label, float ratio, Vector2 position, Font font)
    {
        Button button = CreateButton(parent, "Aspect_" + label, label, position,
            new Vector2(140f, 46f), font, new Color(0.16f, 0.34f, 0.55f, 1f));
        button.onClick.AddListener(() => ApplyAndRefresh(ratio));
    }

    private void ApplyAndRefresh(float ratio)
    {
        if (ratio <= 0f)
            PlayerPrefs.DeleteKey(AspectRatioPreferenceKey);
        else
            PlayerPrefs.SetFloat(AspectRatioPreferenceKey, ratio);
        PlayerPrefs.Save();
        ApplyAspectRatio(ratio);
    }

    private static void ApplyAspectRatio(float ratio)
    {
        Resolution maximum = Screen.currentResolution;
        if (ratio <= 0f)
        {
            Screen.SetResolution(maximum.width, maximum.height, Screen.fullScreen);
            return;
        }

        int width = maximum.width;
        int height = Mathf.RoundToInt(width / ratio);
        if (height > maximum.height)
        {
            height = maximum.height;
            width = Mathf.RoundToInt(height * ratio);
        }
        Screen.SetResolution(width, height, Screen.fullScreen);
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position,
        Vector2 size, Font font, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = CreateText(obj.transform, "Label", label, 20, Vector2.zero, size - new Vector2(8f, 8f), font);
        text.color = Color.white;
        return obj.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize,
        Vector2 position, Vector2 size, Font font)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// 场景切换后等待 Canvas/UI 完成创建，再生成屏幕比例入口。
/// </summary>
public sealed class AspectRatioSettingsBootstrap : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(CreateButtonWhenReady());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(CreateButtonWhenReady());
    }

    private IEnumerator CreateButtonWhenReady()
    {
        // RunHud 等 UI 会在 Start 中创建 Canvas；延后一帧并在短时间内重试。
        for (int frame = 0; frame < 8; frame++)
        {
            yield return null;
            if (AspectRatioSettingsButton.CreateSceneButton())
                yield break;
        }

        Debug.LogWarning("[AspectRatioSettings] 当前场景未找到 Canvas，无法创建屏幕比例按钮。");
    }
}
