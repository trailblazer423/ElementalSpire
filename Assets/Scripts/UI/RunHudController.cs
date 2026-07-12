using System;
using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent, runtime-built HUD for every run scene. MainMenuScene and SampleScene are excluded.
/// </summary>
public sealed class RunHudController : MonoBehaviour
{
    private static RunHudController _instance;

    private readonly HashSet<string> _excludedScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MainMenuScene",
        "SampleScene"
    };

    private GameObject _canvasRoot;
    private Text _hpText;
    private Button _drawPileButton;
    private Text _drawPileButtonText;
    private Button _discardPileButton;
    private Text _discardPileButtonText;
    private CardCollectionPanel _collectionPanel;
    private Button _enemyInfoButton;
    private EnemyIntroductionPanel _enemyIntroductionPanel;
    private GameObject _confirmRoot;
    private Text _confirmMessageText;

    private BattleManager _battleManager;
    private playerHP _battlePlayerHp;
    private string _activeSceneName = string.Empty;
    private bool _hudVisible;
    private bool _modalOpen;
    private bool _exitInProgress;
    private float _timeScaleBeforeModal = 1f;
    private Font _font;
    private int _appliedSceneHandle = int.MinValue;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        _font = CardView.GetCompatibleFont();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplyScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (_instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        RestoreTimeScaleForSceneChange();
        _instance = null;
    }

    private void OnApplicationQuit()
    {
        if (_instance == this)
            RestoreTimeScaleForSceneChange();
    }

    private void Update()
    {
        if (!_hudVisible)
            return;

        RefreshHp();
        if (IsBattleScene())
            RefreshPileButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene);
    }

    private void ApplyScene(Scene scene)
    {
        if (_appliedSceneHandle == scene.handle && _canvasRoot != null)
            return;

        RestoreTimeScaleForSceneChange();
        if (_canvasRoot != null)
            Destroy(_canvasRoot);

        _canvasRoot = null;
        _collectionPanel = null;
        _confirmRoot = null;
        _appliedSceneHandle = scene.handle;
        _activeSceneName = scene.name ?? string.Empty;
        _battleManager = null;
        _battlePlayerHp = null;
        _exitInProgress = false;

        _hudVisible = !_excludedScenes.Contains(_activeSceneName) && !string.IsNullOrEmpty(_activeSceneName);
        if (!_hudVisible)
            return;

        // 独立高排序 Canvas 可避免 Event/Rest 在 Start 中后创建的全屏 UI 覆盖 HUD。
        Canvas sceneCanvas = CreateSceneCanvas(scene);

        BuildUi(sceneCanvas.transform);
        EnsureEventSystem();
        bool battleScene = IsBattleScene();
        _drawPileButton.gameObject.SetActive(battleScene);
        _discardPileButton.gameObject.SetActive(battleScene);
        _enemyInfoButton.gameObject.SetActive(battleScene);
        RefreshHp();
        RefreshPileButtons();
    }

    private void BuildUi(Transform sceneCanvas)
    {
        _canvasRoot = new GameObject("GlobalRunHudRoot", typeof(RectTransform));
        _canvasRoot.transform.SetParent(sceneCanvas, false);
        Stretch(_canvasRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        _canvasRoot.transform.SetAsLastSibling();

        GameObject hpPanel = CreatePanel("HealthPanel", _canvasRoot.transform,
            new Color(0.08f, 0.08f, 0.1f, 0.92f));
        hpPanel.GetComponent<Image>().raycastTarget = false;
        SetTopLeft(hpPanel.GetComponent<RectTransform>(), new Vector2(24f, -24f), new Vector2(270f, 60f));
        _hpText = CreateText("HealthText", hpPanel.transform, "生命 -- / --", 26, Color.white,
            TextAnchor.MiddleCenter);
        _hpText.raycastTarget = false;
        Stretch(_hpText.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));

        Button gearButton = CreateButton("ExitButton", _canvasRoot.transform, "\u2699", 38,
            new Color(0.12f, 0.13f, 0.16f, 0.96f));
        SetTopRight(gearButton.GetComponent<RectTransform>(), new Vector2(-24f, -24f), new Vector2(64f, 64f));
        gearButton.onClick.AddListener(OpenExitConfirmation);

        Button deckButton = CreateButton("DeckButton", _canvasRoot.transform, "牌组", 25,
            new Color(0.16f, 0.24f, 0.38f, 0.96f));
        SetTopRight(deckButton.GetComponent<RectTransform>(), new Vector2(-104f, -24f), new Vector2(112f, 64f));
        deckButton.onClick.AddListener(OpenPermanentDeck);

        _enemyInfoButton = CreateButton("EnemyInfoButton", _canvasRoot.transform, "敌人简介", 22,
            new Color(0.38f, 0.18f, 0.15f, 0.96f));
        SetTopRight(_enemyInfoButton.GetComponent<RectTransform>(), new Vector2(-232f, -24f), new Vector2(122f, 64f));
        _enemyInfoButton.onClick.AddListener(OpenEnemyIntroduction);

        _drawPileButton = CreateButton("DrawPileButton", _canvasRoot.transform, "抽牌堆 0", 21,
            new Color(0.16f, 0.34f, 0.52f, 0.94f));
        SetTopLeft(_drawPileButton.GetComponent<RectTransform>(), new Vector2(24f, -100f),
            new Vector2(168f, 52f));
        _drawPileButtonText = _drawPileButton.GetComponentInChildren<Text>();
        _drawPileButton.onClick.AddListener(OpenDrawPile);

        _discardPileButton = CreateButton("DiscardPileButton", _canvasRoot.transform, "弃牌堆 0", 21,
            new Color(0.42f, 0.24f, 0.22f, 0.94f));
        SetTopLeft(_discardPileButton.GetComponent<RectTransform>(), new Vector2(24f, -164f),
            new Vector2(168f, 52f));
        _discardPileButtonText = _discardPileButton.GetComponentInChildren<Text>();
        _discardPileButton.onClick.AddListener(OpenDiscardPile);

        GameObject collectionRoot = new GameObject("CardCollectionOverlay", typeof(Image),
            typeof(CardCollectionPanel));
        collectionRoot.transform.SetParent(_canvasRoot.transform, false);
        _collectionPanel = collectionRoot.GetComponent<CardCollectionPanel>();
        _collectionPanel.Initialize(_font);

        GameObject enemyInfoRoot = new GameObject("EnemyIntroductionOverlay", typeof(Image),
            typeof(EnemyIntroductionPanel));
        enemyInfoRoot.transform.SetParent(_canvasRoot.transform, false);
        _enemyIntroductionPanel = enemyInfoRoot.GetComponent<EnemyIntroductionPanel>();
        _enemyIntroductionPanel.Initialize(_font);

        BuildExitConfirmation();
        _canvasRoot.SetActive(true);
    }

    private void BuildExitConfirmation()
    {
        _confirmRoot = new GameObject("ExitConfirmation", typeof(Image));
        _confirmRoot.transform.SetParent(_canvasRoot.transform, false);
        Image backdrop = _confirmRoot.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.62f);
        Stretch(_confirmRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject window = CreatePanel("WhiteConfirmation", _confirmRoot.transform, Color.white);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = new Vector2(680f, 380f);

        Text title = CreateText("Title", window.transform, "退出本局？", 38,
            new Color(0.1f, 0.1f, 0.12f), TextAnchor.MiddleCenter);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(-60f, 62f);

        _confirmMessageText = CreateText("Message", window.transform,
            "将保留最近的安全进度并返回主界面。\n当前战斗中的临时状态不会写入存档。",
            24, new Color(0.18f, 0.18f, 0.2f), TextAnchor.MiddleCenter);
        RectTransform messageRect = _confirmMessageText.rectTransform;
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = new Vector2(0f, 20f);
        messageRect.sizeDelta = new Vector2(580f, 130f);

        Button noButton = CreateButton("NoButton", window.transform, "否", 28,
            new Color(0.4f, 0.42f, 0.46f, 1f));
        RectTransform noRect = noButton.GetComponent<RectTransform>();
        noRect.anchorMin = new Vector2(0.5f, 0f);
        noRect.anchorMax = new Vector2(0.5f, 0f);
        noRect.pivot = new Vector2(0.5f, 0f);
        noRect.anchoredPosition = new Vector2(-130f, 34f);
        noRect.sizeDelta = new Vector2(190f, 68f);
        noButton.onClick.AddListener(CloseExitConfirmation);

        Button yesButton = CreateButton("YesButton", window.transform, "是", 28,
            new Color(0.74f, 0.18f, 0.15f, 1f));
        RectTransform yesRect = yesButton.GetComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(0.5f, 0f);
        yesRect.anchorMax = new Vector2(0.5f, 0f);
        yesRect.pivot = new Vector2(0.5f, 0f);
        yesRect.anchoredPosition = new Vector2(130f, 34f);
        yesRect.sizeDelta = new Vector2(190f, 68f);
        yesButton.onClick.AddListener(ConfirmExitToMainMenu);

        _confirmRoot.SetActive(false);
    }

    private void RefreshHp()
    {
        int currentHp;
        int maxHp;

        if (IsBattleScene() && TryGetLiveBattleHp(out playerHP liveHp))
        {
            currentHp = liveHp.CurrentHP;
            maxHp = liveHp.MaxHP;
        }
        else if (GameManager.Instance != null)
        {
            currentHp = GameManager.Instance.playerHp;
            maxHp = GameManager.Instance.playerMaxHp;
        }
        else
        {
            _hpText.text = "生命 -- / --";
            return;
        }

        _hpText.text = $"生命 {Mathf.Max(0, currentHp)} / {Mathf.Max(0, maxHp)}";
    }

    private bool TryGetLiveBattleHp(out playerHP hp)
    {
        hp = _battlePlayerHp;
        if (hp != null)
            return true;

        BattleManager battleManager = ResolveBattleManager();
        if (battleManager == null || battleManager.PlayerObject == null)
            return false;

        _battlePlayerHp = battleManager.PlayerObject.GetComponent<playerHP>();
        hp = _battlePlayerHp;
        return hp != null;
    }

    private void RefreshPileButtons()
    {
        DeckManager deckManager = ResolveDeckManager();
        int drawCount = deckManager != null ? deckManager.DrawPileCount : 0;
        int discardCount = deckManager != null ? deckManager.DiscardPileCount : 0;
        _drawPileButtonText.text = $"抽牌堆 {drawCount}";
        _discardPileButtonText.text = $"弃牌堆 {discardCount}";
        _drawPileButton.interactable = deckManager != null;
        _discardPileButton.interactable = deckManager != null;
    }

    private void OpenPermanentDeck()
    {
        List<CardInstance> cards = GameManager.Instance != null
            ? CardInstanceCodec.DecodeMany(GameManager.Instance.playerCardBag)
            : new List<CardInstance>();
        OpenCollection("当前牌组", cards);
    }

    private void OpenDrawPile()
    {
        DeckManager deckManager = ResolveDeckManager();
        var cards = deckManager != null
            ? new List<CardInstance>(deckManager.drawPile)
            : new List<CardInstance>();
        OpenCollection("抽牌堆", cards);
    }

    private void OpenDiscardPile()
    {
        DeckManager deckManager = ResolveDeckManager();
        var cards = deckManager != null
            ? new List<CardInstance>(deckManager.discardPile)
            : new List<CardInstance>();
        OpenCollection("弃牌堆", cards);
    }

    private void OpenEnemyIntroduction()
    {
        if (_exitInProgress || _enemyIntroductionPanel == null)
            return;

        _collectionPanel?.Hide();
        _enemyIntroductionPanel.Show();
    }

    private void OpenCollection(string title, IReadOnlyList<CardInstance> cards)
    {
        if (_exitInProgress)
            return;

        PauseTimeScaleForModal();
        _collectionPanel.Show(title, cards, CloseCardCollection);
    }

    private void CloseCardCollection()
    {
        if (!_collectionPanel.gameObject.activeSelf)
            return;

        _collectionPanel.Hide();
        ResumeTimeScaleAfterModal();
    }

    private void OpenExitConfirmation()
    {
        if (_exitInProgress || _confirmRoot.activeSelf)
            return;

        PauseTimeScaleForModal();
        ChallengeRunTracker.EnsureExists().SuspendRun();
        _confirmMessageText.text = "将保留最近的安全进度并返回主界面。\n当前战斗中的临时状态不会写入存档。";
        _confirmRoot.SetActive(true);
        _confirmRoot.transform.SetAsLastSibling();
    }

    private void CloseExitConfirmation()
    {
        if (_exitInProgress || !_confirmRoot.activeSelf)
            return;

        _confirmRoot.SetActive(false);
        ResumeTimeScaleAfterModal();
        ChallengeRunTracker tracker = ChallengeRunTracker.Instance;
        if (tracker != null && tracker.HasActiveRun)
            tracker.ResumeRun();
    }

    private void ConfirmExitToMainMenu()
    {
        if (_exitInProgress)
            return;

        _exitInProgress = true;
        try
        {
            ChallengeRunTracker tracker = ChallengeRunTracker.EnsureExists();
            tracker.SuspendRun();
            bool saveRequired = GameManager.Instance != null && GameManager.Instance.gameInitialized;
            if (saveRequired && !RunSaveRepository.UpdateChallengeStateOnly())
            {
                _confirmMessageText.text = "保存失败，尚未退出。\n请检查磁盘空间或权限后重试。";
                _exitInProgress = false;
                return;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("[RunHud] 保存退出计时状态失败：" + exception);
            _confirmMessageText.text = "保存失败，尚未退出。\n请检查控制台后重试。";
            _exitInProgress = false;
            return;
        }

        RestoreTimeScaleForSceneChange();
        SceneManager.LoadScene("MainMenuScene");
    }

    private BattleManager ResolveBattleManager()
    {
        if (_battleManager == null && IsBattleScene())
            _battleManager = FindObjectOfType<BattleManager>();
        return _battleManager;
    }

    private DeckManager ResolveDeckManager()
    {
        BattleManager battleManager = ResolveBattleManager();
        return battleManager != null ? battleManager.DeckManager : null;
    }

    private bool IsBattleScene()
    {
        // 第 1 关名为 BattleScene，后续关卡为 BattleScene2~10。
        return !string.IsNullOrEmpty(_activeSceneName)
            && _activeSceneName.StartsWith("BattleScene", StringComparison.OrdinalIgnoreCase);
    }

    private void PauseTimeScaleForModal()
    {
        if (_modalOpen)
            return;

        _timeScaleBeforeModal = Time.timeScale;
        Time.timeScale = 0f;
        _modalOpen = true;
    }

    private void ResumeTimeScaleAfterModal()
    {
        if (!_modalOpen)
            return;

        Time.timeScale = _timeScaleBeforeModal;
        _modalOpen = false;
    }

    private void RestoreTimeScaleForSceneChange()
    {
        Time.timeScale = 1f;
        _timeScaleBeforeModal = 1f;
        _modalOpen = false;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindObjectOfType<EventSystem>() != null)
            return;

        new GameObject("RunHudEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas FindSceneCanvas(Scene scene)
    {
        Canvas selected = null;
        foreach (Canvas canvas in FindObjectsOfType<Canvas>(true))
        {
            if (canvas == null || canvas.gameObject.scene != scene || canvas.renderMode == RenderMode.WorldSpace)
                continue;

            if (selected == null || canvas.sortingOrder > selected.sortingOrder)
                selected = canvas;
        }

        return selected;
    }

    private static Canvas CreateSceneCanvas(Scene scene)
    {
        GameObject canvasObject = new GameObject(
            "GlobalRunHudCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private Button CreateButton(string objectName, Transform parent, string label, int fontSize, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;

        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        button.colors = colors;

        Text text = CreateText("Label", obj.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, new Vector2(6f, 4f), new Vector2(-6f, -4f));
        return button;
    }

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, Color color,
        TextAnchor alignment)
    {
        GameObject obj = new GameObject(objectName, typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = _font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void SetTopRight(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
