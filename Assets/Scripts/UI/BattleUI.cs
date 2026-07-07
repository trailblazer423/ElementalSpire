using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 战斗UI可视化 - 自动创建所有UI元素，无需手动拖拽绑定
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("战斗管理器（可留空，自动查找）")]
    [SerializeField] private BattleManager _battleManager;

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
    private Text _enemyHPText;
    private Text _enemyBlockText;
    private Button _endTurnButton;
    private GameObject _resultPanel;
    private Text _resultText;

    private bool _uiCreated = false;

    private static readonly string[] PhaseNames = new string[]
    {
        ">>> 玩家回合开始",
        ">>> 获得基础能量",
        ">>> 我方行动",
        ">>> 敌人行动",
        ">>> 护盾清空",
        ">>> 回合结束"
    };

    // ==========================================
    //  生命周期
    // ==========================================

    void Awake()
    {
        Debug.Log("[BattleUI] Awake 开始");

        if (_battleManager == null)
            _battleManager = FindObjectOfType<BattleManager>();

        // 只在第一次运行时创建UI
        if (!_uiCreated)
            CreateUI();

        // 订阅事件
        if (_battleManager != null)
        {
            FindAndCacheComponents(_battleManager.PlayerObject, _battleManager.EnemyObject);
            _battleManager.OnPhaseChanged += OnPhaseChanged;
            _battleManager.OnTurnStarted += OnTurnStarted;
            _battleManager.OnBattleOver += OnBattleOver;
            Debug.Log("[BattleUI] 事件订阅完成");
        }
        else
        {
            Debug.LogError("[BattleUI] 未找到 BattleManager！");
        }

        if (_endTurnButton != null)
            _endTurnButton.onClick.AddListener(OnEndTurnClicked);

        Debug.Log("[BattleUI] Awake 结束");
    }

    void Start()
    {
        Debug.Log("[BattleUI] Start 开始");
        if (_battleManager == null)
        {
            Debug.LogError("[BattleUI] Start: 未找到 BattleManager，UI 无法工作");
            return;
        }
        StartCoroutine(DelayedInitialUpdate());
    }

    void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnPhaseChanged -= OnPhaseChanged;
            _battleManager.OnTurnStarted -= OnTurnStarted;
            _battleManager.OnBattleOver -= OnBattleOver;
        }
        if (_endTurnButton != null)
            _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    // ==========================================
    //  自动创建UI
    // ==========================================

    private void CreateUI()
    {
        Debug.Log("[BattleUI] 开始创建 UI 元素");

        // 查找或创建 Canvas
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasObj.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            Debug.Log("[BattleUI] 创建了新 Canvas");
        }
        else
        {
            Debug.Log("[BattleUI] 使用已有 Canvas");
        }

        // 获取字体
        Font font = GetFont();
        Debug.Log($"[BattleUI] 字体: {(font != null ? font.name : "NULL")}");

        _phaseText = CreateText("PhaseText", ">>> 玩家回合开始", 36, Color.white, new Vector2(0, 200), font);
        _turnText = CreateText("TurnText", "第 1 回合", 28, Color.white, new Vector2(0, 150), font);

        _playerHPText = CreateText("PlayerHPText", "HP: 20/20", 24, Color.green, new Vector2(-400, 0), font);
        _playerEnergyText = CreateText("PlayerEnergyText", "能量: 3/3", 24, Color.cyan, new Vector2(-400, -40), font);
        _playerBlockText = CreateText("PlayerBlockText", "", 24, Color.yellow, new Vector2(-400, -80), font);

        _enemyHPText = CreateText("EnemyHPText", "HP: 15/15", 24, Color.red, new Vector2(400, 0), font);
        _enemyBlockText = CreateText("EnemyBlockText", "", 24, Color.yellow, new Vector2(400, -40), font);

        _endTurnButton = CreateEndTurnButton(font);
        _resultPanel = CreateResultPanel(font);
        _resultText = _resultPanel.GetComponentInChildren<Text>();

        _uiCreated = true;
        Debug.Log("[BattleUI] UI 创建完成");
    }

    /// <summary>
    /// 获取可用字体
    /// </summary>
    private Font GetFont()
    {
        // 方法1: LegacyRuntime.ttf（Unity 2022 内置字体）
        try
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BattleUI] LegacyRuntime.ttf 加载失败: {e.Message}");
        }

        // 方法2: 从系统创建动态字体
        try
        {
            Font f = Font.CreateDynamicFontFromOSFont("Arial", 24);
            if (f != null) return f;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BattleUI] 系统字体加载失败: {e.Message}");
        }

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
        Debug.Log($"[BattleUI] 阶段变化: {phase}");
        int index = (int)phase;
        if (index >= 0 && index < PhaseNames.Length)
        {
            if (_phaseText != null)
                _phaseText.text = PhaseNames[index];
            else
                Debug.LogWarning("[BattleUI] _phaseText 为 null");
        }

        if (_endTurnButton != null)
            _endTurnButton.gameObject.SetActive(phase == BattlePhase.PlayerAction);

        StartCoroutine(DelayedRefresh());
    }

    private void OnTurnStarted(int turn)
    {
        Debug.Log($"[BattleUI] 回合开始: {turn}");
        if (_turnText != null)
            _turnText.text = $"第 {turn} 回合";
    }

    private void OnBattleOver(string result)
    {
        Debug.Log($"[BattleUI] 战斗结束: {result}");
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

    private float _infoUpdateDelay = 0.2f;

    private void RefreshAllInfo()
    {
        UpdatePlayerInfo();
        UpdateEnemyInfo();
    }

    private void UpdatePlayerInfo()
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
    }

    private void UpdateEnemyInfo()
    {
        if (_enemyHPComponent != null && _enemyHPText != null)
            _enemyHPText.text = $"HP: {_enemyHPComponent.CurrentHP}/{_enemyHPComponent.MaxHP}";
        if (_enemyBlockComponent != null && _enemyBlockText != null)
        {
            int block = _enemyBlockComponent.CurrentBlock;
            _enemyBlockText.text = block > 0 ? $"护盾: {block}" : "";
        }
    }

    public void OnEndTurnClicked()
    {
        Debug.Log("[BattleUI] 点击结束回合");
        _battleManager?.EndPlayerTurn();
    }
}
