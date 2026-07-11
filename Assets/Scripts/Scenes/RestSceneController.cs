using ElementalSpire.Cards;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 休息节点：恢复 15 点生命或从当前牌组选择一张尚未升级的牌升级。
/// </summary>
public sealed class RestSceneController : ChoiceSceneControllerBase
{
    private const int HealAmount = 15;
    private bool _choiceCommitted;

    private void Start()
    {
        GameManager gameManager = GameManager.Instance;
        string hpText = gameManager == null
            ? "玩家状态不可用"
            : $"当前生命：{gameManager.playerHp} / {gameManager.playerMaxHp}";

        BuildChoiceScreen("休息点", hpText + "\n请选择一项");
        CreateChoiceButton(
            "RestChoiceButton",
            $"休息\n恢复 {HealAmount} 点生命",
            new Vector2(-230f, -30f),
            ChooseRest);

        Button upgradeButton = CreateChoiceButton(
            "UpgradeChoiceButton",
            "升级卡牌\n从当前牌组选择一张",
            new Vector2(230f, -30f),
            ChooseUpgrade);

        bool hasUpgrade = HasUpgradableCard();
        upgradeButton.interactable = hasUpgrade;
        if (!hasUpgrade)
            SetButtonLabel(upgradeButton, "升级卡牌\n当前没有可升级卡牌");
    }

    public void ChooseRest()
    {
        if (!TryCommitChoice())
            return;

        GameManager gameManager = GameManager.Instance;
        int previousHp = gameManager.playerHp;
        gameManager.playerHp = Mathf.Min(gameManager.playerMaxHp, gameManager.playerHp + HealAmount);
        Debug.Log($"[RestSceneController] 休息恢复 {gameManager.playerHp - previousHp} 点生命。");
        RunFlowCoordinator.CompleteCurrentNodeAndReturnToMap();
    }

    public void ChooseUpgrade()
    {
        if (!HasUpgradableCard())
        {
            Debug.LogWarning("[RestSceneController] 当前牌组没有可升级卡牌。");
            return;
        }

        if (!TryCommitChoice())
            return;

        GameManager.Instance.currentDraftMode = GameManager.DraftMode.RestUpgrade;
        GameManager.Instance.pendingEventToClear = false;
        GameManager.Instance.pendingNodeCompletion = true;
        SceneManager.LoadScene("CardDraftScene");
    }

    private bool TryCommitChoice()
    {
        if (_choiceCommitted)
            return false;

        if (GameManager.Instance == null)
        {
            Debug.LogError("[RestSceneController] GameManager 不存在，无法处理休息节点。");
            return false;
        }

        _choiceCommitted = true;
        SetAllChoicesInteractable(false);
        return true;
    }

    private static bool HasUpgradableCard()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.playerCardBag == null)
            return false;

        foreach (string serializedCardId in gameManager.playerCardBag)
        {
            CardInstance cardInstance = CardInstanceCodec.Decode(serializedCardId);
            CardData cardData = cardInstance?.GetCardData();
            if (cardInstance != null
                && cardData != null
                && !cardInstance.isUpgraded
                && cardData.hasUpgrade)
            {
                return true;
            }
        }

        return false;
    }
}
