using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using ElementalSpire.Cards;

/// <summary>
/// 战斗UI可视化 - 自动创建所有UI元素，显示手牌和战斗信息。
/// </summary>
public class BattleUI : MonoBehaviour
{
    private BattleManager _battleManager;
    private TurnManager _turnManager;

    private playerHP _playerHPComponent;
    private currentEnergy _playerEnergyComponent;
    private playerBlock _playerBlockComponent;
    private enemyHP _enemyHPComponent;
    private enemyBlock _enemyBlockComponent;

    private Canvas _canvas;
    private Text _phaseText;
    private Text _turnText;
    private Text _playerNameText;
    private Text _playerHPText;
    private Text _playerEnergyText;
    private Text _playerWaterText;
    private Text _playerBlockText;
    private Text _playerPowerText;
    private Text _playerPoisonText;
    private Text _playerWeaknessText;
    private Text _playerVulnerableText;
    private Text _enemyNameText;
    private Text _enemyHPText;
    private Text _enemyBlockText;
    private Text _enemyPoisonText;
    private Text _enemyElementText;
    private Text _enemyPowerText;
    private Text _enemyWeaknessText;
    private GameObject _enemyIntentObj;
    private Text _enemyIntentText;
    private Image _enemyIntentBg;
    private Image _enemyIntentOutline;
    private Image _enemyIntentArrow;

    // HP 血条
    private RectTransform _playerHPBarFill;
    private RectTransform _playerHPBarBg;
    private RectTransform _enemyHPBarFill;
    private RectTransform _enemyHPBarBg;
    private float _displayedPlayerHP = -1;
    private float _displayedEnemyHP = -1;

    private Text _drawPileText;
    private Text _discardPileText;
    private Text _exhaustPileText;
    private Text _powerPileText;
    private Text _battleLogText;
    private Button _endTurnButton;
    private GameObject _resultPanel;
    private Text _resultText;
    private Transform _handArea;

    private Font _font;
    private bool _uiCreated = false;
    private float _infoUpdateDelay = 0.2f;
    private List<CardView> _cardViews = new List<CardView>();
    private CardInstance _pendingTargetCard;
    private ElementType _pendingTargetElement;
    private Text _targetSelectionText;
    private readonly List<string> _battleLogs = new List<string>();
    private const int MaxBattleLogLines = 9;

    private string _lastBattleResult = "";

    private static readonly string[] PhaseNames = new string[]
    {
        ">>> 玩家回合开始",   // PlayerTurnStart
        ">>> 获得基础能量",   // EnergyRefill
        ">>> 抽牌阶段",       // DrawPhase
        ">>> 我方行动",       // PlayerAction
        ">>> 弃牌阶段",       // DiscardPhase
        ">>> 中毒结算",       // PoisonTickPhase
        ">>> 敌人行动",       // EnemyAction
        ">>> 护盾清空",       // ShieldClear
        ">>> 回合结束"        // TurnEnd
    };

    void Awake()
    {
        if (_battleManager == null)
            _battleManager = FindObjectOfType<BattleManager>();

        if (_battleManager != null)
            _turnManager = _battleManager.TurnManager;

        if (!_uiCreated)
            CreateUI();

        if (_turnManager != null)
        {
            _turnManager.OnPhaseChanged += OnPhaseChanged;
            _turnManager.OnTurnStarted += OnTurnStarted;
        }

        if (_battleManager != null)
        {
            FindAndCacheComponents(_battleManager.PlayerObject, _battleManager.EnemyObject);
            _battleManager.OnBattleOver += OnBattleOver;
            _battleManager.OnHandChanged += RefreshHand;
            _battleManager.OnBattleInfoChanged += RefreshAllInfo;
            _battleManager.OnBattleLog += OnBattleLog;
        }

        if (_endTurnButton != null)
            _endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    void Start()
    {
        if (_battleManager == null || _turnManager == null)
        {
            Debug.LogError("[BattleUI] BattleManager 或 TurnManager 未找到");
            return;
        }

        RefreshHand();
        StartCoroutine(DelayedInitialUpdate());
    }

    void OnDestroy()
    {
        if (_turnManager != null)
        {
            _turnManager.OnPhaseChanged -= OnPhaseChanged;
            _turnManager.OnTurnStarted -= OnTurnStarted;
        }
        if (_battleManager != null)
        {
            _battleManager.OnBattleOver -= OnBattleOver;
            _battleManager.OnHandChanged -= RefreshHand;
            _battleManager.OnBattleInfoChanged -= RefreshAllInfo;
            _battleManager.OnBattleLog -= OnBattleLog;
        }
        if (_endTurnButton != null)
            _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    private void CreateUI()
    {
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasObj.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        Font font = GetFont();

        // ===== 顶部横幅 =====
        CreatePanel("TopBanner", new Vector2(0, 460), new Vector2(800, 80),
            new Color(0.05f, 0.05f, 0.15f, 0.85f));
        _phaseText = CreateText("PhaseText", "玩家回合开始", 30, Color.white, new Vector2(0, 465), font);
        _turnText = CreateText("TurnText", "第 1 回合", 22, new Color(0.7f, 0.7f, 0.8f), new Vector2(0, 438), font);

        // ===== 左侧：玩家信息面板 =====
        CreatePanel("PlayerPanel", new Vector2(-460, 275), new Vector2(280, 330),
            new Color(0.08f, 0.12f, 0.25f, 0.8f));
        _playerNameText = CreateText("PlayerNameText", "玩家", 26, new Color(0.3f, 0.7f, 1f), new Vector2(-460, 395), font);
        _playerNameText.fontStyle = FontStyle.Bold;

        // HP 血条背景
        var pHpBg = CreatePanelObj("PlayerHPBarBg", new Vector2(-460, 365), new Vector2(240, 22),
            new Color(0.3f, 0.1f, 0.1f, 0.8f));
        _playerHPBarBg = pHpBg.GetComponent<RectTransform>();

        // HP 血条填充（锚点左侧，方便百分比缩放）
        var pHpFill = CreatePanelObj("PlayerHPBarFill", new Vector2(-460, 365), new Vector2(240, 22),
            new Color(0.2f, 0.85f, 0.3f, 0.85f));
        _playerHPBarFill = pHpFill.GetComponent<RectTransform>();
        _playerHPBarFill.pivot = new Vector2(0, 0.5f);
        _playerHPBarFill.anchorMin = new Vector2(0.5f, 0.5f);
        _playerHPBarFill.anchorMax = new Vector2(0.5f, 0.5f);
        _playerHPBarFill.anchoredPosition = new Vector2(-580, 365);

        _playerHPText = CreateText("PlayerHPText", "HP: 20/20", 16, Color.white, new Vector2(-460, 365), font);

        _playerEnergyText = CreateText("PlayerEnergyText", "能量: 3/3", 22, Color.cyan, new Vector2(-460, 330), font);
        _playerBlockText = CreateText("PlayerBlockText", "", 20, Color.yellow, new Vector2(-460, 298), font);
        _playerPowerText = CreateText("PlayerPowerText", "", 20, new Color(1f, 0.5f, 0f), new Vector2(-460, 268), font);
        _playerWaterText = CreateText("PlayerWaterText", "水源: 0", 20, new Color(0.35f, 0.75f, 1f), new Vector2(-460, 238), font);
        _playerPoisonText = CreateText("PlayerPoisonText", "", 18, new Color(0.3f, 0.8f, 0.1f), new Vector2(-460, 210), font);
        _playerWeaknessText = CreateText("PlayerWeaknessText", "", 18, new Color(0.8f, 0.4f, 0.8f), new Vector2(-460, 185), font);
        _playerVulnerableText = CreateText("PlayerVulnerableText", "", 18, new Color(1f, 0.6f, 0.1f), new Vector2(-460, 160), font);

        // ===== 右侧：敌人信息面板 =====
        CreatePanel("EnemyPanel", new Vector2(460, 275), new Vector2(280, 330),
            new Color(0.2f, 0.08f, 0.08f, 0.8f));
        _enemyNameText = CreateText("EnemyNameText", "", 26, new Color(1f, 0.4f, 0.3f), new Vector2(460, 395), font);
        _enemyNameText.fontStyle = FontStyle.Bold;

        // 敌人 HP 血条
        var eHpBg = CreatePanelObj("EnemyHPBarBg", new Vector2(460, 365), new Vector2(240, 22),
            new Color(0.3f, 0.1f, 0.1f, 0.8f));
        _enemyHPBarBg = eHpBg.GetComponent<RectTransform>();

        var eHpFill = CreatePanelObj("EnemyHPBarFill", new Vector2(460, 365), new Vector2(240, 22),
            new Color(0.9f, 0.2f, 0.2f, 0.85f));
        _enemyHPBarFill = eHpFill.GetComponent<RectTransform>();
        _enemyHPBarFill.pivot = new Vector2(0, 0.5f);
        _enemyHPBarFill.anchorMin = new Vector2(0.5f, 0.5f);
        _enemyHPBarFill.anchorMax = new Vector2(0.5f, 0.5f);
        _enemyHPBarFill.anchoredPosition = new Vector2(340, 365);

        _enemyHPText = CreateText("EnemyHPText", "HP: 15/15", 16, Color.white, new Vector2(460, 365), font);

        _enemyBlockText = CreateText("EnemyBlockText", "", 20, Color.yellow, new Vector2(460, 330), font);
        _enemyPoisonText = CreateText("EnemyPoisonText", "", 20, new Color(0.3f, 0.8f, 0.1f), new Vector2(460, 298), font);
        _enemyElementText = CreateText("EnemyElementText", "", 20, new Color(0.8f, 0.9f, 1f), new Vector2(460, 268), font);
        _enemyPowerText = CreateText("EnemyPowerText", "", 18, new Color(1f, 0.5f, 0f), new Vector2(460, 238), font);
        _enemyWeaknessText = CreateText("EnemyWeaknessText", "", 18, new Color(0.8f, 0.4f, 0.8f), new Vector2(460, 210), font);
        _enemyIntentObj = CreateIntentDisplay(font);

        _targetSelectionText = CreateText("TargetSelectionText", "", 20,
            new Color(1f, 0.85f, 0.25f), new Vector2(495, 438), font);
        _targetSelectionText.fontStyle = FontStyle.Bold;
        _targetSelectionText.gameObject.SetActive(false);

        // ===== 底部左侧：牌堆信息 =====
        CreatePanel("DeckPanel", new Vector2(-580, -380), new Vector2(340, 110),
            new Color(0.05f, 0.08f, 0.12f, 0.75f));
        _drawPileText = CreateText("DrawPileText", "抽牌堆: 0", 17, Color.white, new Vector2(-720, -340), font);
        _discardPileText = CreateText("DiscardPileText", "弃牌堆: 0", 17, Color.white, new Vector2(-545, -340), font);
        _exhaustPileText = CreateText("ExhaustPileText", "消耗区: 0", 17, Color.white, new Vector2(-495, -390), font);
        _powerPileText = CreateText("PowerPileText", "能力区: 0", 17, Color.white, new Vector2(-665, -390), font);

        // ===== 底部右侧：战斗日志 =====
        CreatePanel("LogPanel", new Vector2(620, -300), new Vector2(280, 110),
            new Color(0.05f, 0.08f, 0.10f, 0.75f));
        _battleLogText = CreateText("BattleLogText", "", 14, new Color(0.85f, 0.85f, 0.75f), new Vector2(620, -295), font);
        _battleLogText.alignment = TextAnchor.UpperLeft;
        _battleLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _battleLogText.verticalOverflow = VerticalWrapMode.Truncate;
        _battleLogText.rectTransform.sizeDelta = new Vector2(260, 90);

        var handObj = new GameObject("HandArea", typeof(Image));
        _handArea = handObj.transform;
        _handArea.SetParent(_canvas.transform, false);
        var handImage = handObj.GetComponent<Image>();
        handImage.color = Color.clear;
        handImage.raycastTarget = false;
        var handRect = handObj.GetComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.5f, 0);
        handRect.anchorMax = new Vector2(0.5f, 0);
        handRect.pivot = new Vector2(0.5f, 0.5f);
        handRect.anchoredPosition = new Vector2(0, 120);
        handRect.sizeDelta = new Vector2(1400, 300);

        _endTurnButton = CreateEndTurnButton(font);
        _resultPanel = CreateResultPanel(font);
        _resultText = _resultPanel.GetComponentInChildren<Text>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        _font = font;
        _uiCreated = true;
    }

    private void RefreshHand()
    {
        foreach (var cv in _cardViews)
        {
            if (cv != null) Destroy(cv.gameObject);
        }
        _cardViews.Clear();

        if (_battleManager?.DeckManager == null) return;

        var hand = _battleManager.DeckManager.handCards;
        bool isPlayerAction = _turnManager != null && _turnManager.CurrentPhase == BattlePhase.PlayerAction;

        for (int i = 0; i < hand.Count; i++)
        {
            CardInstance card = hand[i];
            var cardView = CardView.Create(_handArea, card, _font, OnCardClicked);
            _cardViews.Add(cardView);

            float totalWidth = hand.Count * 170;
            float startX = -totalWidth / 2f + 80;
            var rect = cardView.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + i * 170, 0);

            bool canPlay = isPlayerAction && CanAffordCard(card);
            cardView.SetInteractable(canPlay);
        }

        UpdatePileCounts();
        RefreshAllInfo();
    }

    private bool CanAffordCard(CardInstance card)
    {
        return _battleManager != null && _battleManager.CanAffordCard(card);
    }

    private void UpdatePileCounts()
    {
        if (_battleManager?.DeckManager == null) return;
        if (_drawPileText != null)
            _drawPileText.text = $"抽牌堆: {_battleManager.DeckManager.DrawPileCount}";
        if (_discardPileText != null)
            _discardPileText.text = $"弃牌堆: {_battleManager.DeckManager.DiscardPileCount}";
        if (_exhaustPileText != null)
            _exhaustPileText.text = $"消耗区: {_battleManager.DeckManager.ExhaustPileCount}";
        if (_powerPileText != null)
            _powerPileText.text = $"能力区: {_battleManager.DeckManager.PowerPileCount}";
    }

    private void OnCardClicked(CardInstance cardInstance, ElementType chosenElement)
    {
        CardData cardData = cardInstance?.GetCardData();
        if (_battleManager != null && _battleManager.RequiresTargetSelection(cardData))
        {
            BeginTargetSelection(cardInstance, chosenElement);
            return;
        }

        _battleManager?.PlayCard(cardInstance, chosenElement);
    }

    private void BeginTargetSelection(CardInstance cardInstance, ElementType chosenElement)
    {
        if (cardInstance == null) return;

        _pendingTargetCard = cardInstance;
        _pendingTargetElement = chosenElement;
        if (_targetSelectionText != null)
        {
            _targetSelectionText.text = "选择攻击目标";
            _targetSelectionText.gameObject.SetActive(true);
        }

        SetCardViewsInteractable(false);
        UpdateMultiEnemyTargetButtons();
    }

    private void ConfirmTargetSelection(EnemyUnit target)
    {
        if (_pendingTargetCard == null || target == null || !target.IsAlive) return;

        _battleManager?.PlayCard(_pendingTargetCard, _pendingTargetElement, target);
        ClearTargetSelection();
    }

    private void ClearTargetSelection()
    {
        _pendingTargetCard = null;
        _pendingTargetElement = ElementType.None;
        if (_targetSelectionText != null)
            _targetSelectionText.gameObject.SetActive(false);

        SetCardViewsInteractable(true);
        UpdateMultiEnemyTargetButtons();
    }

    private void SetCardViewsInteractable(bool interactable)
    {
        foreach (CardView cardView in _cardViews)
            cardView?.SetInteractable(interactable);
    }

    private Font GetFont()
    {
        return CardView.GetCompatibleFont();
    }

    private Text CreateText(string name, string text, int fontSize, Color color, Vector2 anchoredPos, Font font)
    {
        GameObject obj = new GameObject(name, typeof(Text));
        obj.transform.SetParent(_canvas.transform, false);

        var txt = obj.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = font;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(420, 50);

        return txt;
    }

    private void CreatePanel(string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        CreatePanelObj(name, anchoredPos, size, color);
    }

    private GameObject CreatePanelObj(string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(Image));
        obj.transform.SetParent(_canvas.transform, false);

        var img = obj.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        return obj;
    }

    private void UpdateHPBar(RectTransform fill, RectTransform bg, float currentHP, float maxHP, ref float displayedHP)
    {
        if (fill == null || bg == null || maxHP <= 0) return;

        float ratio = Mathf.Clamp01(currentHP / maxHP);
        fill.sizeDelta = new Vector2(bg.rect.width * ratio, fill.sizeDelta.y);

        var img = fill.GetComponent<Image>();
        if (img != null)
        {
            if (ratio < 0.3f)
                img.color = new Color(0.95f, 0.15f, 0.1f, 0.85f);
            else if (ratio < 0.6f)
                img.color = new Color(0.95f, 0.7f, 0.1f, 0.85f);
            else
                img.color = new Color(0.2f, 0.85f, 0.3f, 0.85f);
        }
    }

    private Button CreateEndTurnButton(Font font)
    {
        GameObject btnObj = new GameObject("EndTurnButton", typeof(Image), typeof(Button));
        btnObj.transform.SetParent(_canvas.transform, false);

        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -160);
        rect.sizeDelta = new Vector2(200, 60);

        var image = btnObj.GetComponent<Image>();
        image.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        GameObject textObj = new GameObject("Text", typeof(Text));
        textObj.transform.SetParent(btnObj.transform, false);
        var txt = textObj.GetComponent<Text>();
        txt.text = "结束回合";
        txt.fontSize = 28;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = font;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        btnObj.SetActive(false);
        return btnObj.GetComponent<Button>();
    }

    private GameObject CreateResultPanel(Font font)
    {
        GameObject panel = new GameObject("ResultPanel", typeof(Image), typeof(Button));
        panel.transform.SetParent(_canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600, 200);

        var image = panel.GetComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);

        var btn = panel.GetComponent<Button>();
        btn.onClick.AddListener(OnResultClicked);

        GameObject textObj = new GameObject("ResultText", typeof(Text));
        textObj.transform.SetParent(panel.transform, false);
        var txt = textObj.GetComponent<Text>();
        txt.text = "";
        txt.fontSize = 60;
        txt.color = Color.yellow;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = font;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    private void FindAndCacheComponents(GameObject playerObject, GameObject enemyObject)
    {
        if (playerObject != null)
        {
            _playerHPComponent = playerObject.GetComponent<playerHP>();
            _playerEnergyComponent = playerObject.GetComponent<currentEnergy>();
            _playerBlockComponent = playerObject.GetComponent<playerBlock>();
        }
        if (enemyObject != null)
        {
            _enemyHPComponent = enemyObject.GetComponent<enemyHP>();
            _enemyBlockComponent = enemyObject.GetComponent<enemyBlock>();
        }

        // 多敌人模式：初始化面板
        if (_battleManager?.MultiEnemyManager != null)
            SetupMultiEnemyUI();
    }

    private void OnPhaseChanged(BattlePhase phase)
    {
        int index = (int)phase;
        if (index >= 0 && index < PhaseNames.Length && _phaseText != null)
            _phaseText.text = PhaseNames[index];

        if (_endTurnButton != null)
            _endTurnButton.gameObject.SetActive(phase == BattlePhase.PlayerAction);

        if (phase != BattlePhase.PlayerAction && _pendingTargetCard != null)
            ClearTargetSelection();

        RefreshHand();
        StartCoroutine(DelayedRefresh());
    }

    private void OnTurnStarted(int turn)
    {
        if (_turnText != null)
            _turnText.text = $"第 {turn} 回合";
        UpdatePileCounts();
    }

    private void OnBattleLog(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        _battleLogs.Insert(0, message);
        while (_battleLogs.Count > MaxBattleLogLines)
            _battleLogs.RemoveAt(_battleLogs.Count - 1);

        RefreshBattleLogText();
    }

    private void RefreshBattleLogText()
    {
        if (_battleLogText == null) return;

        if (_battleLogs.Count == 0)
        {
            _battleLogText.text = "战斗日志";
            return;
        }

        _battleLogText.text = "战斗日志\n" + string.Join("\n", _battleLogs);
    }
    private void OnBattleOver(string result)
    {
        _lastBattleResult = result;
        _resultPanel.SetActive(true);

        if (result == "win")
        {
            _resultText.text = "胜利！\n点击继续";
            _resultText.color = Color.yellow;
        }
        else
        {
            _resultText.text = "战败...\n点击返回主菜单";
            _resultText.color = Color.red;
        }
        if (_endTurnButton != null)
            _endTurnButton.gameObject.SetActive(false);
    }

    private void OnResultClicked()
    {
        Debug.Log($"[BattleUI] 点击结果面板，_lastBattleResult={_lastBattleResult}, GameManager.isBattleWin={GameManager.Instance?.isBattleWin}, currentFloor={GameManager.Instance?.currentFloor}");

        if (_lastBattleResult == "win")
        {
            bool isFinalVictory = GameManager.Instance != null
                && GameManager.Instance.currentNodeId == GameManager.NodesPerFloor;
            if (isFinalVictory)
            {
                SceneManager.LoadScene("MainMenuScene");
            }
            else
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.currentDraftMode = GameManager.DraftMode.BattleReward;
                SceneManager.LoadScene("CardDraftScene");
            }
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    private IEnumerator DelayedInitialUpdate()
    {
        yield return new WaitForSeconds(_infoUpdateDelay);
        RefreshAllInfo();
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(_infoUpdateDelay);
        RefreshAllInfo();
    }

    private void RefreshAllInfo()
    {
        if (_playerNameText != null)
            _playerNameText.text = "玩家";
        if (_playerHPComponent != null && _playerHPText != null)
        {
            _playerHPText.text = $"HP: {_playerHPComponent.CurrentHP}/{_playerHPComponent.MaxHP}";
            UpdateHPBar(_playerHPBarFill, _playerHPBarBg, _playerHPComponent.CurrentHP, _playerHPComponent.MaxHP, ref _displayedPlayerHP);
        }
        if (_playerEnergyComponent != null && _playerEnergyText != null)
            _playerEnergyText.text = $"能量: {_playerEnergyComponent.CurrentEnergy}/{_playerEnergyComponent.MaxEnergy}";
        if (_playerWaterText != null)
            _playerWaterText.text = $"水源: {_battleManager?.PlayerState?.WaterSource ?? 0}";
        if (_playerBlockComponent != null && _playerBlockText != null)
        {
            int block = _playerBlockComponent.CurrentBlock;
            _playerBlockText.text = block > 0 ? $"护盾: {block}" : "";
        }
        if (_playerPowerText != null)
        {
            int power = _battleManager?.PlayerState?.power ?? 0;
            _playerPowerText.text = power > 0 ? $"力量: {power}" : "";
        }
        if (_playerPoisonText != null)
        {
            int poison = _battleManager?.PlayerState?.PoisonStacks ?? 0;
            _playerPoisonText.text = poison > 0 ? $"中毒: {poison}" : "";
        }
        if (_playerWeaknessText != null)
        {
            int weakness = _battleManager?.PlayerState?.Weakness ?? 0;
            _playerWeaknessText.text = weakness > 0 ? $"虚弱: {weakness}" : "";
        }
        if (_playerVulnerableText != null)
        {
            int vulnerable = _battleManager?.PlayerState?.Vulnerable ?? 0;
            _playerVulnerableText.text = vulnerable > 0 ? $"易伤: {vulnerable}" : "";
        }
        if (_enemyHPComponent != null && _enemyHPText != null)
        {
            _enemyHPText.text = $"HP: {_enemyHPComponent.CurrentHP}/{_enemyHPComponent.MaxHP}";
            UpdateHPBar(_enemyHPBarFill, _enemyHPBarBg, _enemyHPComponent.CurrentHP, _enemyHPComponent.MaxHP, ref _displayedEnemyHP);
        }
        if (_enemyBlockComponent != null && _enemyBlockText != null)
        {
            int block = _enemyBlockComponent.CurrentBlock;
            _enemyBlockText.text = block > 0 ? $"护盾: {block}" : "";
        }
        if (_enemyPoisonText != null)
        {
            int poison = _battleManager?.EnemyState?.PoisonStacks ?? 0;
            _enemyPoisonText.text = poison > 0 ? $"中毒: {poison}" : "";
        }
        if (_enemyElementText != null)
        {
            var enemyState = _battleManager?.EnemyState;
            string text = "";
            if (enemyState != null)
            {
                var element = enemyState.ElementAttachment;
                if (element != ElementType.None)
                    text = $"附着: {ElementName(element)}";
                if (enemyState.DeepPoison)
                    text = string.IsNullOrEmpty(text) ? "深度中毒" : text + " / 深度中毒";
            }
            _enemyElementText.text = text;
        }
        if (_enemyPowerText != null)
        {
            int power = _battleManager?.EnemyState?.Power ?? 0;
            _enemyPowerText.text = power > 0 ? $"力量: {power}" : "";
        }
        if (_enemyWeaknessText != null)
        {
            int weakness = _battleManager?.EnemyState?.Weakness ?? 0;
            _enemyWeaknessText.text = weakness > 0 ? $"虚弱: {weakness}" : "";
        }

        // 更新敌人名称
        if (_enemyNameText != null)
        {
            string enemyName = GetEnemyName();
            _enemyNameText.text = enemyName;
            if (string.IsNullOrEmpty(enemyName))
                Debug.LogWarning("[BattleUI] 无法获取敌人名称");
        }

        // 更新敌人意图显示（文字由 LateUpdate 每帧更新位置）
        UpdateIntentDisplay();

        // 多敌人模式：刷新多敌人面板
        RefreshMultiEnemyInfo();
    }

    private string GetEnemyName()
    {
        // 方式1：从 BattleManager 获取
        if (_battleManager?.EnemyObject != null)
        {
            var controller = GetControllerWithData(_battleManager.EnemyObject);
            if (controller != null)
            {
                return controller.enemyData.enemyName;
            }

            // 兜底：用 GameObject 名字
            if (!string.IsNullOrEmpty(_battleManager.EnemyObject.name))
                return _battleManager.EnemyObject.name;
        }

        // 方式2：全局搜索任意 EnemyController
        EnemyController anyController = FindObjectOfType<EnemyController>();
        if (anyController != null)
        {
            if (anyController.enemyData != null && !string.IsNullOrEmpty(anyController.enemyData.enemyName))
                return anyController.enemyData.enemyName;
            if (!string.IsNullOrEmpty(anyController.gameObject.name))
                return anyController.gameObject.name;
        }

        // 方式3：直接找 Enemy 对象
        GameObject enemyObj = GameObject.Find("Enemy");
        if (enemyObj != null)
        {
            var controller = GetControllerWithData(enemyObj);
            if (controller != null)
                return controller.enemyData.enemyName;
            if (!string.IsNullOrEmpty(enemyObj.name))
                return enemyObj.name;
        }

        return "???";
    }

    private static EnemyController GetControllerWithData(GameObject enemyObject)
    {
        if (enemyObject == null) return null;

        foreach (EnemyController controller in enemyObject.GetComponents<EnemyController>())
        {
            if (controller.enabled
                && controller.enemyData != null
                && !string.IsNullOrEmpty(controller.enemyData.enemyName))
                return controller;
        }

        return null;
    }

    private void UpdateIntentDisplay()
    {
        if (_enemyIntentObj == null || _enemyIntentText == null || _enemyIntentBg == null) return;
        if (_battleManager?.EnemyObject == null) return;

        var intentUI = _battleManager.EnemyObject.GetComponent<EnemyIntentUI>();
        if (intentUI != null)
        {
            intentUI.GetIntentDisplay(out string intentText, out Color intentColor);
            _enemyIntentText.text = intentText;
            _enemyIntentText.color = Color.white;

            // 内部背景：意图色半透明
            _enemyIntentBg.color = new Color(intentColor.r, intentColor.g, intentColor.b, 0.35f);

            // 箭头跟随意图颜色
            if (_enemyIntentArrow != null)
                _enemyIntentArrow.color = new Color(intentColor.r, intentColor.g, intentColor.b, 0.7f);

            _enemyIntentObj.SetActive(!string.IsNullOrEmpty(intentText));
        }
        else
        {
            _enemyIntentObj.SetActive(false);
        }
    }

    void LateUpdate()
    {
        // 多敌人模式：更新所有敌人意图位置
        if (_battleManager?.MultiEnemyManager != null)
        {
            LateUpdateMultiEnemy();
            return;
        }

        // 传统单敌人模式：每帧将意图 UI 定位到敌人头顶
        if (_enemyIntentObj == null || _battleManager?.EnemyObject == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = _battleManager.EnemyObject.transform.position;
        worldPos += new Vector3(2.0f, -1.5f, 0);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        RectTransform rect = _enemyIntentObj.GetComponent<RectTransform>();
        rect.position = screenPos;
    }

    private GameObject CreateIntentDisplay(Font font)
    {
        // 主容器
        GameObject container = new GameObject("EnemyIntentDisplay");
        container.transform.SetParent(_canvas.transform, false);
        var rootRect = container.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(160, 44);
        rootRect.pivot = new Vector2(0.5f, 1f);

        // 暗色外框（作为描边效果）
        var outlineObj = new GameObject("Outline", typeof(Image));
        outlineObj.transform.SetParent(container.transform, false);
        _enemyIntentOutline = outlineObj.GetComponent<Image>();
        _enemyIntentOutline.color = new Color(0f, 0f, 0f, 0.6f);
        _enemyIntentOutline.raycastTarget = false;
        var outlineRect = outlineObj.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = new Vector2(-3, -3);
        outlineRect.offsetMax = new Vector2(3, 3);

        // 内部背景
        var bgObj = new GameObject("Bg", typeof(Image));
        bgObj.transform.SetParent(container.transform, false);
        _enemyIntentBg = bgObj.GetComponent<Image>();
        _enemyIntentBg.raycastTarget = false;
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 文字
        var textObj = new GameObject("IntentText", typeof(Text));
        textObj.transform.SetParent(container.transform, false);
        _enemyIntentText = textObj.GetComponent<Text>();
        _enemyIntentText.text = "";
        _enemyIntentText.fontSize = 22;
        _enemyIntentText.fontStyle = FontStyle.Bold;
        _enemyIntentText.font = font;
        _enemyIntentText.color = Color.white;
        _enemyIntentText.alignment = TextAnchor.MiddleCenter;
        _enemyIntentText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 2);
        textRect.offsetMax = new Vector2(-8, -2);

        // 向下的三角箭头
        var arrowObj = new GameObject("Arrow", typeof(Image));
        arrowObj.transform.SetParent(container.transform, false);
        _enemyIntentArrow = arrowObj.GetComponent<Image>();
        _enemyIntentArrow.raycastTarget = false;
        _enemyIntentArrow.color = new Color(0f, 0f, 0f, 0.6f);
        var arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0);
        arrowRect.anchorMax = new Vector2(0.5f, 0);
        arrowRect.pivot = new Vector2(0.5f, 0);
        arrowRect.anchoredPosition = new Vector2(0, -1);
        arrowRect.sizeDelta = new Vector2(10, 8);
        arrowRect.localRotation = Quaternion.Euler(0, 0, 45);

        container.SetActive(false);
        return container;
    }

    private string ElementName(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return "火";
            case ElementType.Poison: return "毒";
            case ElementType.Water: return "水";
            default: return "无";
        }
    }

    public void OnEndTurnClicked()
    {
        _turnManager?.EndPlayerTurn();
    }

    // ===== 多敌人 UI 支持 =====

    private class EnemyPanelInfo
    {
        public EnemyUnit unit;
        public GameObject container;
        public Text nameText;
        public Text hpText;
        public RectTransform hpBarFill;
        public RectTransform hpBarBg;
        public Text blockText;
        public Text poisonText;
        public Text powerText;
        public Text weaknessText;
        public Button targetButton;
        public GameObject intentObj;
        public Text intentText;
        public Image intentBg;
        public Image intentOutline;
        public Image intentArrow;
        public float displayedHP = -1;
    }

    private List<EnemyPanelInfo> _multiEnemyPanels = new List<EnemyPanelInfo>();
    private bool _multiEnemyUISetup = false;

    /// <summary>
    /// 为多个敌人创建独立的信息面板
    /// </summary>
    private void SetupMultiEnemyUI()
    {
        if (_multiEnemyUISetup) return;

        var multiMgr = _battleManager?.MultiEnemyManager;
        if (multiMgr == null) return;

        var aliveEnemies = multiMgr.GetAliveEnemies();
        if (aliveEnemies.Count == 0) return;

        _multiEnemyUISetup = true;

        int count = aliveEnemies.Count;
        float startX = 495f;
        float startY = 330f;
        float spacingY = 145f;
        float panelWidth = 300f;
        float panelHeight = 130f;

        // 清理旧面板
        foreach (var old in _multiEnemyPanels)
        {
            if (old?.container != null)
                Destroy(old.container);
        }
        _multiEnemyPanels.Clear();

        SetSingleEnemyUIVisible(false);

        for (int i = 0; i < count; i++)
        {
            var enemy = aliveEnemies[i];
            var panel = new EnemyPanelInfo { unit = enemy };

            float yPos = startY - i * spacingY;

            // 面板背景
            panel.container = CreatePanelObj($"EnemyPanel_{i}",
                new Vector2(startX, yPos),
                new Vector2(panelWidth, panelHeight),
                new Color(0.10f, 0.05f, 0.06f, 0.92f));
            var panelRect = panel.container.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panel.targetButton = panel.container.AddComponent<Button>();
            panel.targetButton.targetGraphic = panel.container.GetComponent<Image>();
            panel.targetButton.transition = Selectable.Transition.None;
            panel.targetButton.interactable = false;
            panel.targetButton.onClick.AddListener(() => ConfirmTargetSelection(panel.unit));

            // 敌人名称
            panel.nameText = CreateText($"EnemyName_{i}", enemy.DisplayName,
                20, new Color(1f, 0.4f, 0.3f),
                Vector2.zero, _font);
            panel.nameText.fontStyle = FontStyle.Bold;
            PlaceInEnemyPanel(panel.nameText, panel.container.transform, new Vector2(-55, 42));

            // HP血条背景
            var hpBg = CreatePanelObj($"EnemyHPBg_{i}",
                Vector2.zero,
                new Vector2(panelWidth - 34, 18),
                new Color(0.3f, 0.1f, 0.1f, 0.7f));
            panel.hpBarBg = hpBg.GetComponent<RectTransform>();
            PlaceInEnemyPanel(panel.hpBarBg, panel.container.transform, new Vector2(0, 13));

            // HP血条填充
            var hpFill = CreatePanelObj($"EnemyHPFill_{i}",
                Vector2.zero,
                new Vector2(panelWidth - 34, 18),
                new Color(0.9f, 0.2f, 0.2f, 0.85f));
            panel.hpBarFill = hpFill.GetComponent<RectTransform>();
            PlaceInEnemyPanel(panel.hpBarFill, panel.container.transform, new Vector2(0, 13));
            panel.hpBarFill.pivot = new Vector2(0, 0.5f);
            panel.hpBarFill.anchorMin = new Vector2(0.5f, 0.5f);
            panel.hpBarFill.anchorMax = new Vector2(0.5f, 0.5f);
            panel.hpBarFill.anchoredPosition = new Vector2(-(panelWidth - 34) / 2, 13);

            // HP文字
            panel.hpText = CreateText($"EnemyHP_{i}", $"HP: {enemy.hp?.CurrentHP}/{enemy.hp?.MaxHP}",
                14, Color.white, Vector2.zero, _font);
            PlaceInEnemyPanel(panel.hpText, panel.container.transform, new Vector2(0, 13));

            // 护盾/中毒（左列）
            panel.blockText = CreateText($"EnemyBlock_{i}", "",
                15, Color.yellow, Vector2.zero, _font);
            panel.poisonText = CreateText($"EnemyPoison_{i}", "",
                15, new Color(0.3f, 0.8f, 0.1f), Vector2.zero, _font);
            PlaceInEnemyPanel(panel.blockText, panel.container.transform, new Vector2(-100, -17));
            PlaceInEnemyPanel(panel.poisonText, panel.container.transform, new Vector2(-100, -45));

            // 力量/虚弱（右列）
            panel.powerText = CreateText($"EnemyPower_{i}", "",
                15, new Color(1f, 0.5f, 0f), Vector2.zero, _font);
            panel.weaknessText = CreateText($"EnemyWeak_{i}", "",
                15, new Color(0.8f, 0.4f, 0.8f), Vector2.zero, _font);
            PlaceInEnemyPanel(panel.powerText, panel.container.transform, new Vector2(-15, -17));
            PlaceInEnemyPanel(panel.weaknessText, panel.container.transform, new Vector2(-15, -45));

            // 先添加面板到列表，再创建意图（CreateIntentDisplayForEnemy 需要列表中已有引用）
            _multiEnemyPanels.Add(panel);

            // 意图显示（箭头指向敌人）
            CreateIntentDisplayForEnemy(i);

            RectTransform intentRect = panel.intentObj.GetComponent<RectTransform>();
            intentRect.SetParent(panel.container.transform, false);
            intentRect.anchorMin = new Vector2(0.5f, 0.5f);
            intentRect.anchorMax = new Vector2(0.5f, 0.5f);
            intentRect.pivot = new Vector2(0.5f, 0.5f);
            intentRect.anchoredPosition = new Vector2(96, -31);
            intentRect.sizeDelta = new Vector2(105, 28);
            if (panel.intentArrow != null)
                panel.intentArrow.gameObject.SetActive(false);
        }

        Debug.Log($"[BattleUI] 多敌人UI已创建，共 {count} 个面板");
        UpdateMultiEnemyTargetButtons();
    }

    private static void PlaceInEnemyPanel(Component component, Transform parent, Vector2 localPosition)
    {
        RectTransform rect = component.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = localPosition;
    }

    private void SetSingleEnemyUIVisible(bool visible)
    {
        GameObject enemyPanel = GameObject.Find("EnemyPanel");
        if (enemyPanel != null) enemyPanel.SetActive(visible);

        Text[] singleEnemyTexts =
        {
            _enemyNameText, _enemyHPText, _enemyBlockText, _enemyPoisonText,
            _enemyElementText, _enemyPowerText, _enemyWeaknessText
        };
        foreach (Text text in singleEnemyTexts)
        {
            if (text != null) text.gameObject.SetActive(visible);
        }

        if (_enemyHPBarBg != null) _enemyHPBarBg.gameObject.SetActive(visible);
        if (_enemyHPBarFill != null) _enemyHPBarFill.gameObject.SetActive(visible);
        if (_enemyIntentObj != null) _enemyIntentObj.SetActive(visible);
    }

    private GameObject CreateIntentDisplayForEnemy(int index)
    {
        GameObject container = new GameObject($"EnemyIntentDisplay_{index}");
        container.transform.SetParent(_canvas.transform, false);
        var rootRect = container.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(140, 36);
        rootRect.pivot = new Vector2(0.5f, 1f);

        // 外框
        var outlineObj = new GameObject("Outline", typeof(Image));
        outlineObj.transform.SetParent(container.transform, false);
        var outline = outlineObj.GetComponent<Image>();
        outline.color = new Color(0f, 0f, 0f, 0.5f);
        outline.raycastTarget = false;
        var outlineRect = outlineObj.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = new Vector2(-2, -2);
        outlineRect.offsetMax = new Vector2(2, 2);

        // 背景
        var bgObj = new GameObject("Bg", typeof(Image));
        bgObj.transform.SetParent(container.transform, false);
        var bg = bgObj.GetComponent<Image>();
        bg.raycastTarget = false;
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 文字
        var textObj = new GameObject("IntentText", typeof(Text));
        textObj.transform.SetParent(container.transform, false);
        var text = textObj.GetComponent<Text>();
        text.text = "";
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.font = _font;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 2);
        textRect.offsetMax = new Vector2(-8, -2);

        // 箭头
        var arrowObj = new GameObject("Arrow", typeof(Image));
        arrowObj.transform.SetParent(container.transform, false);
        var arrow = arrowObj.GetComponent<Image>();
        arrow.raycastTarget = false;
        arrow.color = new Color(0f, 0f, 0f, 0.5f);
        var arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0);
        arrowRect.anchorMax = new Vector2(0.5f, 0);
        arrowRect.pivot = new Vector2(0.5f, 0);
        arrowRect.anchoredPosition = new Vector2(0, -1);
        arrowRect.sizeDelta = new Vector2(8, 6);
        arrowRect.localRotation = Quaternion.Euler(0, 0, 45);

        // 将引用保存到当前正在构建的 panel
        if (_multiEnemyPanels.Count > index)
        {
            var panel = _multiEnemyPanels[index];
            panel.intentObj = container;
            panel.intentText = text;
            panel.intentBg = bg;
            panel.intentOutline = outline;
            panel.intentArrow = arrow;
        }

        container.SetActive(false);
        return container;
    }

    /// <summary>
    /// 刷新多敌人信息面板
    /// </summary>
    private void RefreshMultiEnemyInfo()
    {
        var multiMgr = _battleManager?.MultiEnemyManager;
        if (multiMgr == null) return;

        // 延迟初始化（等待敌人列表完成）
        if (!_multiEnemyUISetup || _multiEnemyPanels.Count == 0)
            SetupMultiEnemyUI();

        for (int i = 0; i < _multiEnemyPanels.Count; i++)
        {
            var panel = _multiEnemyPanels[i];
            var enemy = panel.unit;

            if (enemy == null || !enemy.IsAlive)
            {
                // 敌人已死亡：隐藏面板
                panel.container?.SetActive(false);
                panel.intentObj?.SetActive(false);
                continue;
            }

            panel.container?.SetActive(true);
            panel.unit = enemy;

            // 名称（标记目标指示）
            bool isDefault = enemy == multiMgr.DefaultTarget;
            panel.nameText.text = isDefault
                ? $"▶ {enemy.DisplayName}"
                : $"  {enemy.DisplayName}";
            panel.nameText.color = isDefault
                ? new Color(1f, 0.5f, 0.2f)
                : new Color(1f, 0.4f, 0.3f);

            // HP
            if (enemy.hp != null)
            {
                panel.hpText.text = $"HP: {enemy.hp.CurrentHP}/{enemy.hp.MaxHP}";
                UpdateHPBar(panel.hpBarFill, panel.hpBarBg,
                    enemy.hp.CurrentHP, enemy.hp.MaxHP,
                    ref panel.displayedHP);
            }

            // 护盾
            panel.blockText.text = (enemy.block != null && enemy.block.CurrentBlock > 0)
                ? $"护盾: {enemy.block.CurrentBlock}" : "";

            // 中毒
            panel.poisonText.text = (enemy.state != null && enemy.state.PoisonStacks > 0)
                ? $"中毒: {enemy.state.PoisonStacks}" : "";

            // 力量
            panel.powerText.text = (enemy.state != null && enemy.state.Power > 0)
                ? $"力量: {enemy.state.Power}" : "";

            // 虚弱
            panel.weaknessText.text = (enemy.state != null && enemy.state.Weakness > 0)
                ? $"虚弱: {enemy.state.Weakness}" : "";

            // 意图
            if (panel.intentObj != null && enemy.intentUI != null)
            {
                enemy.intentUI.GetIntentDisplay(out string intentText, out Color intentColor);
                if (!string.IsNullOrEmpty(intentText))
                {
                    panel.intentText.text = intentText;
                    panel.intentBg.color = new Color(intentColor.r, intentColor.g, intentColor.b, 0.35f);
                    if (panel.intentArrow != null)
                        panel.intentArrow.color = new Color(intentColor.r, intentColor.g, intentColor.b, 0.6f);
                    panel.intentObj.SetActive(true);
                }
                else
                {
                    panel.intentObj.SetActive(false);
                }
            }
        }

        UpdateMultiEnemyTargetButtons();
    }

    private void UpdateMultiEnemyTargetButtons()
    {
        bool selecting = _pendingTargetCard != null;
        foreach (EnemyPanelInfo panel in _multiEnemyPanels)
        {
            if (panel == null || panel.targetButton == null) continue;

            bool selectable = selecting && panel.unit != null && panel.unit.IsAlive;
            panel.targetButton.interactable = selectable;
            Image image = panel.container != null ? panel.container.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = selectable
                    ? new Color(0.35f, 0.17f, 0.06f, 0.98f)
                    : new Color(0.10f, 0.05f, 0.06f, 0.92f);
            }
        }
    }

    /// <summary>
    /// 多敌人意图位置跟踪
    /// </summary>
    private void LateUpdateMultiEnemy()
    {
        // 多敌人意图已收纳在各自信息卡内，不再跟随世界坐标，避免重叠。
    }
}
