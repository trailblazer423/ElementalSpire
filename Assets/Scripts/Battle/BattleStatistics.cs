using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>整局挑战期间持续存在的战斗统计。</summary>
[DisallowMultipleComponent]
public sealed class BattleStatistics : MonoBehaviour
{
    public static BattleStatistics Instance { get; private set; }

    [Header("Run Statistics")]
    public float TotalBattleTime;
    public int TotalDamageDealt;
    public int TotalDamageTaken;
    public int TotalCardsPlayed;
    public int MaxCombo;
    public int EnemiesKilled;
    public int CurrentGold;
    public int RemainingHP;

    private int _currentCombo;
    private readonly HashSet<int> _countedEnemyDeaths = new HashSet<int>();
    private BattleManager _activeBattle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static BattleStatistics EnsureExists()
    {
        if (Instance != null)
            return Instance;

        BattleStatistics existing = FindObjectOfType<BattleStatistics>();
        if (existing != null)
            return existing;

        return new GameObject("BattleStatistics").AddComponent<BattleStatistics>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        _activeBattle = null;
        _countedEnemyDeaths.Clear();
        _currentCombo = 0;
    }

    private void Update()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.name)
            || !scene.name.StartsWith("BattleScene", System.StringComparison.OrdinalIgnoreCase))
            return;

        if (_activeBattle == null)
            _activeBattle = FindObjectOfType<BattleManager>();
        if (_activeBattle != null && !_activeBattle.IsBattleOver)
            TotalBattleTime += Time.unscaledDeltaTime;
    }

    public void ResetForNewRun()
    {
        TotalBattleTime = 0f;
        TotalDamageDealt = 0;
        TotalDamageTaken = 0;
        TotalCardsPlayed = 0;
        MaxCombo = 0;
        EnemiesKilled = 0;
        CurrentGold = 0;
        RemainingHP = 0;
        _currentCombo = 0;
        _countedEnemyDeaths.Clear();
        _activeBattle = null;
    }

    public void RecordDamageDealt(int amount)
    {
        TotalDamageDealt += Mathf.Max(0, amount);
    }

    public void RecordDamageTaken(int amount)
    {
        TotalDamageTaken += Mathf.Max(0, amount);
    }

    public void RecordCardPlayed(CardData card)
    {
        TotalCardsPlayed++;
        if (card != null && card.HasCardType(CardType.Attack))
        {
            _currentCombo++;
            MaxCombo = Mathf.Max(MaxCombo, _currentCombo);
        }
        else
        {
            _currentCombo = 0;
        }
    }

    public void ResetCombo()
    {
        _currentCombo = 0;
    }

    public void RecordEnemyKilled(GameObject enemy)
    {
        if (enemy == null || !_countedEnemyDeaths.Add(enemy.GetInstanceID()))
            return;
        EnemiesKilled++;
    }

    public void GrantGold(int amount)
    {
        CurrentGold += Mathf.Max(0, amount);
    }

    public void FinalizeRemainingHP(playerHP player)
    {
        RemainingHP = player != null ? player.CurrentHP : 0;
    }
}
