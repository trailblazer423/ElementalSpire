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
    private Text _playerHPText;
    private Text _playerEnergyText;
    private Text _playerWaterText;
    private Text _playerBlockText;
    private Text _playerPowerText;
    private Text _enemyHPText;
    private Text _enemyBlockText;
    private Text _enemyPoisonText;
    private Text _enemyElementText;
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

        _phaseText = CreateText("PhaseText", ">>> 玩家回合开始", 36, Color.white, new Vector2(0, 210), font);
        _turnText = CreateText("TurnText", "第 1 回合", 28, Color.white, new Vector2(0, 160), font);

        _playerHPText = CreateText("PlayerHPText", "HP: 20/20", 24, Color.green, new Vector2(-430, 20), font);
        _playerEnergyText = CreateText("PlayerEnergyText", "能量: 3/3", 24, Color.cyan, new Vector2(-430, -20), font);
        _playerWaterText = CreateText("PlayerWaterText", "水源: 0", 24, new Color(0.35f, 0.75f, 1f), new Vector2(-430, -60), font);
        _playerBlockText = CreateText("PlayerBlockText", "", 24, Color.yellow, new Vector2(-430, -100), font);
        _playerPowerText = CreateText("PlayerPowerText", "", 22, new Color(1f, 0.5f, 0f), new Vector2(-430, -140), font);

        _enemyHPText = CreateText("EnemyHPText", "HP: 15/15", 24, Color.red, new Vector2(430, 20), font);
        _enemyBlockText = CreateText("EnemyBlockText", "", 24, Color.yellow, new Vector2(430, -20), font);
        _enemyPoisonText = CreateText("EnemyPoisonText", "", 22, new Color(0.3f, 0.8f, 0.1f), new Vector2(430, -60), font);
        _enemyElementText = CreateText("EnemyElementText", "", 22, new Color(0.8f, 0.9f, 1f), new Vector2(430, -100), font);

        _drawPileText = CreateText("DrawPileText", "抽牌堆: 0", 18, Color.white, new Vector2(-720, -280), font);
        _discardPileText = CreateText("DiscardPileText", "弃牌堆: 0", 18, Color.white, new Vector2(-720, -315), font);
        _exhaustPileText = CreateText("ExhaustPileText", "消耗区: 0", 18, Color.white, new Vector2(-720, -350), font);
        _powerPileText = CreateText("PowerPileText", "能力区: 0", 18, Color.white, new Vector2(-720, -385), font);
        _battleLogText = CreateText("BattleLogText", "战斗日志", 18, new Color(0.95f, 0.95f, 0.85f), new Vector2(610, -300), font);
        _battleLogText.alignment = TextAnchor.UpperLeft;
        _battleLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _battleLogText.verticalOverflow = VerticalWrapMode.Truncate;
        _battleLogText.rectTransform.sizeDelta = new Vector2(520, 220);

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
        _battleManager?.PlayCard(cardInstance, chosenElement);
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
        rect.sizeDelta = new Vector2(420, 50);

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
    }

    private void OnPhaseChanged(BattlePhase phase)
    {
        int index = (int)phase;
        if (index >= 0 && index < PhaseNames.Length && _phaseText != null)
            _phaseText.text = PhaseNames[index];

        if (_endTurnButton != null)
            _endTurnButton.gameObject.SetActive(phase == BattlePhase.PlayerAction);

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
            SceneManager.LoadScene("MapScene");
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
        if (_playerHPComponent != null && _playerHPText != null)
            _playerHPText.text = $"HP: {_playerHPComponent.CurrentHP}/{_playerHPComponent.MaxHP}";
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
        if (_enemyHPComponent != null && _enemyHPText != null)
            _enemyHPText.text = $"HP: {_enemyHPComponent.CurrentHP}/{_enemyHPComponent.MaxHP}";
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
}
