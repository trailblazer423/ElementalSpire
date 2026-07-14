using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 战斗结算页面：任意失败显示 DEFEAT，仅最终节点胜利显示 VICTORY。
/// 普通节点胜利不拦截原有奖励/地图流程。
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleResultManager : MonoBehaviour
{
    // Unity Canvas 排序值的有效上限是 32767；保留少量余量并高于现有 HUD(30000~32000)。
    private const int ResultCanvasSortingOrder = 32760;

    [Header("Result Rewards")]
    [SerializeField, Min(0)] private int finalVictoryGold = 300;

    [Header("Optional Audio")]
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip defeatSound;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float maskFadeDuration = 0.48f;
    [SerializeField, Min(0.01f)] private float rowRevealInterval = 0.09f;

    private BattleManager _battleManager;
    private GameObject _overlay;
    private CanvasGroup _overlayGroup;
    private RectTransform _window;
    private Text _title;
    private Text _subtitle;
    private Text _trophy;
    private readonly List<GameObject> _statRows = new List<GameObject>();
    private GameObject _rewardRoot;
    private GameObject _buttonRoot;
    private bool _showing;
    private bool _continuing;

    private void Awake()
    {
        _battleManager = GetComponent<BattleManager>();
        if (_battleManager == null)
            _battleManager = FindObjectOfType<BattleManager>();
        if (_battleManager != null)
            _battleManager.OnBattleOver += OnBattleOver;
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
            _battleManager.OnBattleOver -= OnBattleOver;
        if (_showing)
            Time.timeScale = 1f;
    }

    private void Update()
    {
        if (_showing || _continuing || _battleManager == null || _battleManager.IsBattleOver)
            return;

        playerHP player = _battleManager.PlayerObject != null
            ? _battleManager.PlayerObject.GetComponent<playerHP>()
            : null;
        if (player != null && player.CurrentHP <= 0)
        {
            _battleManager.EndBattle("lose");
            return;
        }

        MultiEnemyManager multiEnemy = _battleManager.MultiEnemyManager;
        if (multiEnemy != null)
        {
            if (multiEnemy.AllDead)
                _battleManager.EndBattle("win");
            return;
        }

        GameObject enemyObject = _battleManager.EnemyObject;
        enemyHP enemy = enemyObject != null ? enemyObject.GetComponent<enemyHP>() : null;
        if (enemy != null && enemy.CurrentHP <= 0)
            _battleManager.EndBattle("win");
    }

    private void OnBattleOver(string result)
    {
        if (_showing)
            return;

        if (string.Equals(result, "lose", StringComparison.OrdinalIgnoreCase))
        {
            ShowDefeatResult();
            return;
        }

        if (string.Equals(result, "win", StringComparison.OrdinalIgnoreCase) && IsFinalLevel())
        {
            ShowVictoryResult();
            return;
        }

        ContinueNextBattle();
    }

    /// <summary>
    /// BattleManager 的可靠结算入口。事件监听仍然保留，但最终结果不再只依赖事件顺序。
    /// </summary>
    public void HandleBattleResult(string result)
    {
        Debug.Log($"[BattleResultManager] 收到战斗结果：{result}，最终关={IsFinalLevel()}，节点={CurrentLevel()}。");
        OnBattleOver(result);
    }

    public void ShowVictoryResult()
    {
        if (_showing) return;
        _showing = true;

        Debug.Log("[BattleResultManager] 正在创建最终胜利结算页面。");

        BattleStatistics statistics = BattleStatistics.EnsureExists();
        statistics.FinalizeRemainingHP(_battleManager?.PlayerObject?.GetComponent<playerHP>());
        statistics.GrantGold(finalVictoryGold);
        BuildResultUi(true, statistics);
        PlayResultAudio(true);
        Time.timeScale = 0f;
        StartCoroutine(AnimateResult(true));
    }

    public void ShowDefeatResult()
    {
        if (_showing) return;
        _showing = true;

        Debug.Log("[BattleResultManager] 正在创建失败结算页面。");

        BattleStatistics statistics = BattleStatistics.EnsureExists();
        statistics.FinalizeRemainingHP(_battleManager?.PlayerObject?.GetComponent<playerHP>());
        BuildResultUi(false, statistics);
        PlayResultAudio(false);
        Time.timeScale = 0f;
        StartCoroutine(AnimateResult(false));
    }

    // 供场景按钮或其他脚本直接绑定，名称与结算页语义保持一致。
    public void ShowVictoryPanel()
    {
        ShowVictoryResult();
    }

    public void ShowDefeatPanel()
    {
        ShowDefeatResult();
    }

    public void ContinueNextBattle()
    {
        if (_continuing)
            return;
        _continuing = true;
        StartCoroutine(ContinueNormalVictory());
    }

    private IEnumerator ContinueNormalVictory()
    {
        // 不显示结果面板，仅保留短暂的战斗结束停顿，然后进入原卡牌奖励流程。
        yield return new WaitForSecondsRealtime(0.55f);
        if (GameManager.Instance != null)
            GameManager.Instance.currentDraftMode = GameManager.DraftMode.BattleReward;
        SceneManager.LoadScene("CardDraftScene");
    }

    private bool IsFinalLevel()
    {
        GameManager gameManager = GameManager.Instance;
        bool finalNode = gameManager != null
            && (gameManager.currentNodeId >= RunFlowCoordinator.FinalNodeId
                || string.Equals(gameManager.currentNodeType, "Boss", StringComparison.OrdinalIgnoreCase));
        bool finalScene = string.Equals(
            SceneManager.GetActiveScene().name,
            "BattleScene10",
            StringComparison.OrdinalIgnoreCase);
        return finalNode || finalScene;
    }

    private int CurrentLevel()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentNodeId > 0)
            return GameManager.Instance.currentNodeId;

        string sceneName = SceneManager.GetActiveScene().name;
        if (string.Equals(sceneName, "BattleScene", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (sceneName.StartsWith("BattleScene", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(sceneName.Substring("BattleScene".Length), out int level))
            return level;
        return 1;
    }

    private void BuildResultUi(bool victory, BattleStatistics stats)
    {
        Font font = CardView.GetCompatibleFont();
        GameObject canvasObject = new GameObject("BattleResultCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = ResultCanvasSortingOrder;
        canvas.targetDisplay = 0;
        canvas.enabled = true;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _overlay = new GameObject("BattleResultOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _overlay.transform.SetParent(canvasObject.transform, false);
        Stretch(_overlay.GetComponent<RectTransform>());
        Image mask = _overlay.GetComponent<Image>();
        mask.color = victory
            ? new Color(0.015f, 0.012f, 0.025f, 0.96f)
            : new Color(0.025f, 0.006f, 0.009f, 0.97f);
        _overlayGroup = _overlay.GetComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.interactable = true;
        _overlayGroup.blocksRaycasts = true;

        CreateGradientLayer(_overlay.transform, "UpperGlow", new Vector2(0f, 270f), new Vector2(1920f, 540f),
            victory ? new Color(0.9f, 0.55f, 0.08f, 0.13f) : new Color(0.75f, 0.02f, 0.02f, 0.16f));
        CreateGradientLayer(_overlay.transform, "LowerShade", new Vector2(0f, -350f), new Vector2(1920f, 380f),
            new Color(0f, 0f, 0f, 0.42f));

        GameObject windowObject = new GameObject("ResultWindow", typeof(RectTransform), typeof(Image));
        windowObject.transform.SetParent(_overlay.transform, false);
        _window = windowObject.GetComponent<RectTransform>();
        _window.anchorMin = _window.anchorMax = new Vector2(0.5f, 0.5f);
        _window.pivot = new Vector2(0.5f, 0.5f);
        _window.sizeDelta = new Vector2(900f, 930f);
        windowObject.GetComponent<Image>().color = victory
            ? new Color(0.08f, 0.065f, 0.035f, 0.94f)
            : new Color(0.09f, 0.025f, 0.03f, 0.95f);
        Outline outline = windowObject.AddComponent<Outline>();
        outline.effectColor = victory
            ? new Color(1f, 0.72f, 0.18f, 0.85f)
            : new Color(0.9f, 0.12f, 0.12f, 0.82f);
        outline.effectDistance = new Vector2(4f, -4f);

        _trophy = CreateText(windowObject.transform, "Trophy", victory ? "★" : "×", 92,
            victory ? new Color(1f, 0.78f, 0.2f) : new Color(0.9f, 0.15f, 0.15f),
            new Vector2(0f, 374f), new Vector2(180f, 110f), font);
        _title = CreateText(windowObject.transform, "ResultTitle", victory ? "VICTORY" : "DEFEAT", 72,
            victory ? new Color(1f, 0.76f, 0.18f) : new Color(0.95f, 0.12f, 0.12f),
            new Vector2(0f, 286f), new Vector2(760f, 90f), font);
        _title.fontStyle = FontStyle.Bold;
        _title.rectTransform.localScale = Vector3.zero;
        _trophy.rectTransform.localScale = Vector3.zero;
        _subtitle = CreateText(windowObject.transform, "Subtitle", victory ? "恭喜通关！" : "挑战失败", 31,
            Color.white, new Vector2(0f, 225f), new Vector2(720f, 52f), font);

        _statRows.Clear();
        string enemyName = EnemyName();
        if (victory)
        {
            AddStatRow(windowObject.transform, "最终挑战完成", "", 155f, font, true);
            AddStatRow(windowObject.transform, "击败 Boss", enemyName, 105f, font);
            AddStatRow(windowObject.transform, "通关时间", FormatTime(stats.TotalBattleTime), 55f, font);
            AddStatRow(windowObject.transform, "总造成伤害", stats.TotalDamageDealt.ToString(), 5f, font);
            AddStatRow(windowObject.transform, "使用卡牌数量", stats.TotalCardsPlayed.ToString(), -45f, font);
            AddStatRow(windowObject.transform, "最高连击", stats.MaxCombo.ToString(), -95f, font);
            int maxHp = _battleManager?.PlayerObject?.GetComponent<playerHP>()?.MaxHP
                ?? GameManager.Instance?.playerMaxHp ?? 0;
            AddStatRow(windowObject.transform, "剩余生命", stats.RemainingHP + " / " + maxHp, -145f, font);
        }
        else
        {
            AddStatRow(windowObject.transform, "失败关卡", "第 " + CurrentLevel() + " 关", 155f, font);
            AddStatRow(windowObject.transform, "最终敌人", enemyName, 105f, font);
            AddStatRow(windowObject.transform, "存活时间", FormatTime(stats.TotalBattleTime), 55f, font);
            AddStatRow(windowObject.transform, "造成伤害", stats.TotalDamageDealt.ToString(), 5f, font);
            AddStatRow(windowObject.transform, "受到伤害", stats.TotalDamageTaken.ToString(), -45f, font);
            AddStatRow(windowObject.transform, "使用卡牌", stats.TotalCardsPlayed.ToString(), -95f, font);
            AddStatRow(windowObject.transform, "失败原因", "生命值归零", -145f, font);
        }

        _rewardRoot = new GameObject("RewardArea", typeof(RectTransform), typeof(Image));
        _rewardRoot.transform.SetParent(windowObject.transform, false);
        RectTransform rewardRect = _rewardRoot.GetComponent<RectTransform>();
        rewardRect.anchorMin = rewardRect.anchorMax = new Vector2(0.5f, 0.5f);
        rewardRect.anchoredPosition = new Vector2(0f, -225f);
        rewardRect.sizeDelta = new Vector2(650f, 72f);
        _rewardRoot.GetComponent<Image>().color = victory
            ? new Color(0.5f, 0.3f, 0.04f, 0.75f)
            : new Color(0.22f, 0.04f, 0.05f, 0.72f);
        CreateText(_rewardRoot.transform, "RewardText",
            victory ? "获得奖励：金币 +" + finalVictoryGold : "继续挑战，重新来过",
            25, victory ? new Color(1f, 0.84f, 0.3f) : new Color(0.85f, 0.75f, 0.75f),
            Vector2.zero, new Vector2(620f, 60f), font);
        _rewardRoot.SetActive(false);

        _buttonRoot = new GameObject("ResultButtons", typeof(RectTransform));
        _buttonRoot.transform.SetParent(windowObject.transform, false);
        RectTransform buttonRootRect = _buttonRoot.GetComponent<RectTransform>();
        buttonRootRect.anchorMin = buttonRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRootRect.anchoredPosition = new Vector2(0f, -350f);
        buttonRootRect.sizeDelta = new Vector2(650f, 80f);

        Button mainButton = CreateButton(_buttonRoot.transform, "MainMenuButton", "返回主菜单",
            victory ? new Vector2(-150f, 0f) : Vector2.zero,
            new Vector2(250f, 62f), font,
            victory ? new Color(0.7f, 0.42f, 0.08f, 1f) : new Color(0.55f, 0.1f, 0.12f, 1f));
        mainButton.onClick.AddListener(ReturnToMainMenu);

        if (victory)
        {
            Button rankingButton = CreateButton(_buttonRoot.transform, "RankingButton", "查看排行榜",
                new Vector2(150f, 0f), new Vector2(250f, 62f), font,
                new Color(0.2f, 0.3f, 0.48f, 1f));
            rankingButton.onClick.AddListener(OpenRanking);
        }
        _buttonRoot.SetActive(false);

        CreateParticles(_overlay.transform, victory, font);

        canvasObject.SetActive(true);
        _overlay.SetActive(true);
        canvasObject.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        Debug.Log($"[BattleResultManager] 结算 Canvas 已创建：sortingOrder={canvas.sortingOrder}，active={canvasObject.activeInHierarchy}。");
    }

    private IEnumerator AnimateResult(bool victory)
    {
        float elapsed = 0f;
        while (elapsed < maskFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _overlayGroup.alpha = Mathf.Clamp01(elapsed / maskFadeDuration);
            yield return null;
        }
        _overlayGroup.alpha = 1f;

        _title.rectTransform.localScale = Vector3.zero;
        elapsed = 0f;
        const float titleDuration = 0.38f;
        while (elapsed < titleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / titleDuration);
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
            _title.rectTransform.localScale = Vector3.one * Mathf.Lerp(0f, overshoot, t);
            if (!victory)
            {
                _title.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(elapsed * 76f) * (1f - t) * 18f, 286f);
                _window.anchoredPosition = new Vector2(
                    Mathf.Sin(elapsed * 91f) * (1f - t) * 12f,
                    Mathf.Cos(elapsed * 73f) * (1f - t) * 5f);
            }
            yield return null;
        }
        _title.rectTransform.localScale = Vector3.one;
        _title.rectTransform.anchoredPosition = new Vector2(0f, 286f);
        _window.anchoredPosition = Vector2.zero;

        foreach (GameObject row in _statRows)
        {
            row.SetActive(true);
            CanvasGroup group = row.GetComponent<CanvasGroup>();
            RectTransform rect = row.GetComponent<RectTransform>();
            Vector2 end = rect.anchoredPosition;
            rect.anchoredPosition += Vector2.right * 42f;
            float rowTime = 0f;
            while (rowTime < rowRevealInterval)
            {
                rowTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(rowTime / rowRevealInterval);
                group.alpha = t;
                rect.anchoredPosition = Vector2.Lerp(end + Vector2.right * 42f, end, t);
                yield return null;
            }
            group.alpha = 1f;
            rect.anchoredPosition = end;
        }

        _rewardRoot.SetActive(true);
        RectTransform rewardRect = _rewardRoot.GetComponent<RectTransform>();
        rewardRect.localScale = new Vector3(0f, 1f, 1f);
        elapsed = 0f;
        while (elapsed < 0.28f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.28f);
            rewardRect.localScale = new Vector3(t, 1f, 1f);
            yield return null;
        }
        rewardRect.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(0.18f);
        _buttonRoot.SetActive(true);
        StartCoroutine(AnimateTrophy());
    }

    private IEnumerator AnimateTrophy()
    {
        while (_trophy != null && _overlay != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.09f;
            _trophy.rectTransform.localScale = Vector3.one * pulse;
            _trophy.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin(Time.unscaledTime * 2.2f) * 7f);
            yield return null;
        }
    }

    private void AddStatRow(Transform parent, string label, string value, float y, Font font, bool heading = false)
    {
        GameObject row = new GameObject("Stat_" + label, typeof(RectTransform), typeof(CanvasGroup));
        row.transform.SetParent(parent, false);
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(700f, 46f);
        row.GetComponent<CanvasGroup>().alpha = 0f;

        Text text = CreateText(row.transform, "Value",
            heading ? label : label + "：  " + value,
            heading ? 25 : 23,
            heading ? new Color(1f, 0.76f, 0.25f) : new Color(0.92f, 0.92f, 0.95f),
            Vector2.zero, rect.sizeDelta, font);
        text.fontStyle = heading ? FontStyle.Bold : FontStyle.Normal;
        row.SetActive(false);
        _statRows.Add(row);
    }

    private string EnemyName()
    {
        GameObject enemyObject = _battleManager?.EnemyObject;
        if (enemyObject != null)
        {
            foreach (EnemyController controller in enemyObject.GetComponents<EnemyController>())
            {
                if (controller != null && controller.enemyData != null
                    && !string.IsNullOrEmpty(controller.enemyData.enemyName))
                    return controller.enemyData.enemyName;
            }
            if (!string.IsNullOrEmpty(enemyObject.name))
                return enemyObject.name;
        }
        return "未知敌人";
    }

    private void PlayResultAudio(bool victory)
    {
        if (victory)
        {
            AudioClip clip = victoryMusic != null ? victoryMusic : Resources.Load<AudioClip>("Audio/Victory");
            if (clip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(clip, true);
        }
        else
        {
            AudioClip clip = defeatSound != null ? defeatSound : Resources.Load<AudioClip>("Audio/Defeat");
            if (clip != null)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.ignoreListenerPause = true;
                source.PlayOneShot(clip);
            }
        }
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OpenRanking()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("RankingScene");
    }

    private static string FormatTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, Mathf.RoundToInt(seconds)));
        return time.TotalHours >= 1
            ? string.Format("{0:00}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds)
            : string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
    }

    private static void CreateGradientLayer(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject layer = new GameObject(name, typeof(RectTransform), typeof(Image));
        layer.transform.SetParent(parent, false);
        RectTransform rect = layer.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = layer.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void CreateParticles(Transform parent, bool victory, Font font)
    {
        for (int i = 0; i < 34; i++)
        {
            Text particle = CreateText(parent, "ResultParticle_" + i, victory ? "✦" : "·",
                UnityEngine.Random.Range(15, 32),
                victory ? new Color(1f, 0.7f, 0.15f, 0.7f) : new Color(0.8f, 0.08f, 0.08f, 0.55f),
                new Vector2(UnityEngine.Random.Range(-900f, 900f), UnityEngine.Random.Range(-520f, 520f)),
                new Vector2(44f, 44f), font);
            particle.raycastTarget = false;
            BattleResultParticle motion = particle.gameObject.AddComponent<BattleResultParticle>();
            motion.Initialize(victory);
        }
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position,
        Vector2 size, Font font, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        obj.GetComponent<Image>().color = color;
        CreateText(obj.transform, "Label", label, 24, Color.white, Vector2.zero, size - new Vector2(12f, 10f), font);
        return obj.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, Color color,
        Vector2 position, Vector2 size, Font font)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
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

/// <summary>结算页装饰光点，使用 unscaledTime 保证暂停状态仍播放。</summary>
public sealed class BattleResultParticle : MonoBehaviour
{
    private RectTransform _rect;
    private Vector2 _velocity;
    private bool _victory;

    public void Initialize(bool victory)
    {
        _victory = victory;
        _velocity = victory
            ? new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(18f, 48f))
            : new Vector2(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(-24f, -8f));
    }

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_rect == null) return;
        _rect.anchoredPosition += _velocity * Time.unscaledDeltaTime;
        _rect.localRotation *= Quaternion.Euler(0f, 0f, (_victory ? 35f : -18f) * Time.unscaledDeltaTime);
        if (_rect.anchoredPosition.y > 560f) _rect.anchoredPosition += Vector2.down * 1080f;
        if (_rect.anchoredPosition.y < -560f) _rect.anchoredPosition += Vector2.up * 1080f;
    }
}
