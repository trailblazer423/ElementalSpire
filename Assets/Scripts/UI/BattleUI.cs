using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using ElementalSpire.Cards;

/// <summary>
/// 战斗UI可视化 - 自动创建所有UI元素，显示手牌和战斗信息
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
    private Text _playerHPText;
    private Text _playerEnergyText;
    private Text _playerBlockText;
    private Text _playerPowerText;
    private Text _enemyHPText;
    private Text _enemyBlockText;
    private Text _enemyPoisonText;
    private Text _drawPileText;
    private Text _discardPileText;
    private Button _endTurnButton;
    private GameObject _resultPanel;
    private Text _resultText;
    private Transform _handArea;

    private Font _font;
    private bool _uiCreated = false;
    private float _infoUpdateDelay = 0.2f;
    private List<CardView> _cardViews = new List<CardView>();

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

    // ==========================================
    //  生命周期
    // ==========================================

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
        // 首次刷新手牌
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
        }
        if (_endTurnButton != null)
            _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    // ==========================================
    //  自动创建UI
    // ==========================================

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

        _phaseText = CreateText("PhaseText", ">>> 玩家回合开始", 36, Color.white, new Vector2(0, 200), font);
        _turnText = CreateText("TurnText", "第 1 回合", 28, Color.white, new Vector2(0, 150), font);

        _playerHPText = CreateText("PlayerHPText", "HP: 20/20", 24, Color.green, new Vector2(-400, 0), font);
        _playerEnergyText = CreateText("PlayerEnergyText", "能量: 3/3", 24, Color.cyan, new Vector2(-400, -40), font);
        _playerBlockText = CreateText("PlayerBlockText", "", 24, Color.yellow, new Vector2(-400, -80), font);

        _enemyHPText = CreateText("EnemyHPText", "HP: 15/15", 24, Color.red, new Vector2(400, 0), font);
        _enemyBlockText = CreateText("EnemyBlockText", "", 24, Color.yellow, new Vector2(400, -40), font);

        // 状态显示
        _playerPowerText = CreateText("PlayerPowerText", "", 22, new Color(1f, 0.5f, 0f), new Vector2(-400, -120), font);
        _enemyPoisonText = CreateText("EnemyPoisonText", "", 22, new Color(0.3f, 0.8f, 0.1f), new Vector2(400, -80), font);

        // 牌库信息（左下角）
        _drawPileText = CreateText("DrawPileText", "抽牌堆: 0", 18, Color.white, new Vector2(-700, -300), font);
        _discardPileText = CreateText("DiscardPileText", "弃牌堆: 0", 18, Color.white, new Vector2(-700, -340), font);

        // 手牌区域（底部中央）— 用 Image 确保获得 RectTransform
        var handObj = new GameObject("HandArea", typeof(Image));
        _handArea = handObj.transform;
        _handArea.SetParent(_canvas.transform, false);
        var handImage = handObj.GetComponent<Image>();
        handImage.color = Color.clear; // 透明
        handImage.raycastTarget = false;
        var handRect = handObj.GetComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.5f, 0);
        handRect.anchorMax = new Vector2(0.5f, 0);
        handRect.pivot = new Vector2(0.5f, 0.5f);
        handRect.anchoredPosition = new Vector2(0, 120);
        handRect.sizeDelta = new Vector2(1200, 300);

        _endTurnButton = CreateEndTurnButton(font);
        _resultPanel = CreateResultPanel(font);
        _resultText = _resultPanel.GetComponentInChildren<Text>();

        // 确保 EventSystem 存在（IPointerClickHandler 需要）
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        _font = font;
        _uiCreated = true;
    }

    /// <summary>
    /// 刷新手牌显示
    /// </summary>
    private void RefreshHand()
    {
        // 清除旧卡牌
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
            CardData card = hand[i];
            var cardView = CardView.Create(_handArea, card, _font, OnCardClicked);
            _cardViews.Add(cardView);

            // 排列位置
            float totalWidth = hand.Count * 170;
            float startX = -totalWidth / 2f + 80;
            var rect = cardView.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + i * 170, 0);

            // 检查是否可以打出
            bool canPlay = isPlayerAction && CanAffordCard(card);
            cardView.SetInteractable(canPlay);
        }

        // 更新牌库计数
        UpdatePileCounts();

        // 同时刷新 HP / 能量 / 护盾显示
        RefreshAllInfo();
    }

    private bool CanAffordCard(CardData card)
    {
        if (_battleManager?.PlayerEnergy == null) return false;
        return _battleManager.PlayerEnergy.HasEnoughEnergy(card.cost);
    }

    private void UpdatePileCounts()
    {
        if (_battleManager?.DeckManager == null) return;
        if (_drawPileText != null)
            _drawPileText.text = $"抽牌堆: {_battleManager.DeckManager.DrawPileCount}";
        if (_discardPileText != null)
            _discardPileText.text = $"弃牌堆: {_battleManager.DeckManager.DiscardPileCount}";
    }

    /// <summary>
    /// 卡牌点击回调
    /// </summary>
    private void OnCardClicked(CardData cardData)
    {
        _battleManager?.PlayCard(cardData);
    }

    private Font GetFont()
    {
        try
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
        }
        catch { }

        try
        {
            Font f = Font.CreateDynamicFontFromOSFont("Arial", 24);
            if (f != null) return f;
        }
        catch { }

        return null;
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
        rect.sizeDelta = new Vector2(400, 50);

        return txt;
    }

    private Button CreateEndTurnButton(Font font)
    {
        GameObject btnObj = new GameObject("EndTurnButton", typeof(Image), typeof(Button));
        btnObj.transform.SetParent(_canvas.transform, false);

        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -250);
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
        GameObject panel = new GameObject("ResultPanel", typeof(Image));
        panel.transform.SetParent(_canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600, 200);

        var image = panel.GetComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);

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

    // ==========================================
    //  缓存组件
    // ==========================================

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
    }

    // ==========================================
    //  事件响应
    // ==========================================

    private void OnPhaseChanged(BattlePhase phase)
    {
        int index = (int)phase;
        if (index >= 0 && index < PhaseNames.Length && _phaseText != null)
            _phaseText.text = PhaseNames[index];

        if (_endTurnButton != null)
            _endTurnButton.gameObject.SetActive(phase == BattlePhase.PlayerAction);

        // 阶段变化时刷新卡牌可用状态
        RefreshHand();

        StartCoroutine(DelayedRefresh());
    }

    private void OnTurnStarted(int turn)
    {
        if (_turnText != null)
            _turnText.text = $"第 {turn} 回合";
        UpdatePileCounts();
    }

    private void OnBattleOver(string result)
    {
        _resultPanel.SetActive(true);
        if (result == "win")
        {
            _resultText.text = "胜利！";
            _resultText.color = Color.yellow;
        }
        else
        {
            _resultText.text = "战败...";
            _resultText.color = Color.red;
        }
        if (_endTurnButton != null)
            _endTurnButton.gameObject.SetActive(false);
    }

    // ==========================================
    //  刷新显示
    // ==========================================

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
        if (_playerHPComponent != null && _playerHPText != null)
            _playerHPText.text = $"HP: {_playerHPComponent.CurrentHP}/{_playerHPComponent.MaxHP}";
        if (_playerEnergyComponent != null && _playerEnergyText != null)
            _playerEnergyText.text = $"能量: {_playerEnergyComponent.CurrentEnergy}/{_playerEnergyComponent.MaxEnergy}";
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
        if (_enemyHPComponent != null && _enemyHPText != null)
            _enemyHPText.text = $"HP: {_enemyHPComponent.CurrentHP}/{_enemyHPComponent.MaxHP}";
        if (_enemyBlockComponent != null && _enemyBlockText != null)
        {
            int block = _enemyBlockComponent.CurrentBlock;
            _enemyBlockText.text = block > 0 ? $"护盾: {block}" : "";
        }
        if (_enemyPoisonText != null)
        {
            int poison = _battleManager?.EnemyState?.poison ?? 0;
            _enemyPoisonText.text = poison > 0 ? $"中毒: {poison}" : "";
        }
    }

    public void OnEndTurnClicked()
    {
        _turnManager?.EndPlayerTurn();
    }
}
