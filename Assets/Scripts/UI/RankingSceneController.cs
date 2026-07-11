using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>在既有排行榜背景上动态生成挑战记录表。</summary>
public class RankingSceneController : MonoBehaviour
{
    private const string RankingSceneName = "RankingScene";
    private TMP_FontAsset _font;
    private Button _backButton;
    private bool _backRuntimeListenerAdded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, RankingSceneName, StringComparison.Ordinal))
            return;

        if (FindObjectOfType<RankingSceneController>() != null)
            return;

        new GameObject("RankingSceneController").AddComponent<RankingSceneController>();
    }

    private void Awake()
    {
        _font = FindSceneFont();
        _backButton = FindButtonByName("返回主页", "返回主菜单", "Back", "CloseButton");
        if (_backButton != null && _backButton.onClick.GetPersistentEventCount() == 0)
        {
            _backButton.onClick.AddListener(BackToMainMenu);
            _backRuntimeListenerAdded = true;
        }

        BuildLeaderboard();
    }

    private void OnDestroy()
    {
        if (_backRuntimeListenerAdded && _backButton != null)
            _backButton.onClick.RemoveListener(BackToMainMenu);
    }

    public static void OpenRanking()
    {
        SceneManager.LoadScene(RankingSceneName);
    }

    public static void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    private void BuildLeaderboard()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[RankingSceneController] RankingScene 中未找到 Canvas。");
            return;
        }

        Transform oldPanel = canvas.transform.Find("DynamicLeaderboardPanel");
        if (oldPanel != null)
            Destroy(oldPanel.gameObject);

        GameObject panelObject = CreateUiObject("DynamicLeaderboardPanel", canvas.transform);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        SetAnchors(panelRect, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.88f));
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.96f, 0.87f, 0.65f, 0.18f);

        TMP_Text title = CreateText("Title", panelObject.transform, "元素尖塔 · 挑战记录", 32f, TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, new Vector2(0.03f, 0.90f), new Vector2(0.97f, 0.985f));
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.30f, 0.16f, 0.06f, 1f);

        GameObject header = CreateTableRow(panelObject.transform, "Header", true);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        SetAnchors(headerRect, new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.90f));
        headerRect.SetAsLastSibling();
        AddCell(header.transform, "名次", 45f, true);
        AddCell(header.transform, "开始时间", 220f, true);
        AddCell(header.transform, "实际用时", 120f, true);
        AddCell(header.transform, "进度", 95f, true);
        AddCell(header.transform, "结果", 75f, true);

        GameObject viewportObject = CreateUiObject("Viewport", panelObject.transform);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        SetAnchors(viewportRect, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.82f));
        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject = CreateUiObject("Content", viewportObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(0, 8, 6, 6);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewportObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        List<ChallengeRecord> records = ChallengeRunTracker.EnsureExists().LoadLeaderboard(50);
        if (records.Count == 0)
        {
            TMP_Text empty = CreateText("Empty", contentObject.transform, "暂无挑战记录", 24f, TextAlignmentOptions.Center);
            LayoutElement emptyLayout = empty.gameObject.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 70f;
            empty.color = new Color(0.30f, 0.18f, 0.09f, 0.78f);
            return;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ChallengeRecord record = records[index];
            GameObject row = CreateTableRow(contentObject.transform, "Record_" + (index + 1), false);
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 38f;

            Image background = row.AddComponent<Image>();
            background.color = index % 2 == 0
                ? new Color(1f, 1f, 1f, 0.08f)
                : new Color(0f, 0f, 0f, 0.10f);

            AddCell(row.transform, (index + 1).ToString(), 45f, false);
            AddCell(row.transform, record.StartTime, 220f, false);
            AddCell(row.transform, FormatDuration(record.DurationSeconds), 120f, false);
            AddCell(row.transform, Mathf.Clamp(record.HighestProgress, 0, GameManager.NodesPerFloor) + "/10", 95f, false);
            AddCell(row.transform, record.IsWin ? "通关" : "失败", 75f, false);
        }
    }

    private GameObject CreateTableRow(Transform parent, string name, bool isHeader)
    {
        GameObject row = CreateUiObject(name, parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 0, 0);
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        if (isHeader)
        {
            Image headerBackground = row.AddComponent<Image>();
            headerBackground.color = new Color(0.52f, 0.30f, 0.10f, 0.78f);
        }
        return row;
    }

    private void AddCell(Transform row, string value, float width, bool bold)
    {
        TMP_Text text = CreateText("Cell", row, value ?? string.Empty, 20f, TextAlignmentOptions.Center);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        if (bold)
        {
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.95f, 0.82f, 1f);
        }
        else
        {
            text.color = new Color(0.24f, 0.13f, 0.06f, 1f);
        }

        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        if (_font != null)
            text.font = _font;
        return text;
    }

    private TMP_FontAsset FindSceneFont()
    {
        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null
                && text.font != null
                && text.font.name.IndexOf("KTGB2312", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return text.font;
            }
        }

        foreach (TMP_FontAsset loadedFont in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (loadedFont != null
                && loadedFont.name.IndexOf("KTGB2312", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return loadedFont;
            }
        }

        Font legacyChineseFont = CardView.GetCompatibleFont();
        if (legacyChineseFont != null)
        {
            try
            {
                TMP_FontAsset runtimeFont = TMP_FontAsset.CreateFontAsset(legacyChineseFont);
                if (runtimeFont != null)
                {
                    runtimeFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    return runtimeFont;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[RankingSceneController] 创建运行时中文 TMP 字体失败：" + exception.Message);
            }
        }

        foreach (TMP_Text text in texts)
        {
            if (text != null && text.font != null)
                return text.font;
        }
        return null;
    }

    private static string FormatDuration(int seconds)
    {
        TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (duration.TotalHours >= 1d)
            return string.Format("{0}时{1}分{2}秒", (int)duration.TotalHours, duration.Minutes, duration.Seconds);
        if (duration.TotalMinutes >= 1d)
            return string.Format("{0}分{1}秒", duration.Minutes, duration.Seconds);
        return duration.Seconds + "秒";
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Button FindButtonByName(params string[] acceptedNames)
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            foreach (string acceptedName in acceptedNames)
            {
                if (string.Equals(button.gameObject.name, acceptedName, StringComparison.OrdinalIgnoreCase))
                    return button;
            }
        }
        return null;
    }
}
