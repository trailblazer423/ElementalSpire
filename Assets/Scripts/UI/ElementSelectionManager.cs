using UnityEngine;
using UnityEngine.UI;
using ElementalSpire.Cards;

public class ElementSelectionManager : MonoBehaviour
{
    [Header("UI按钮引用")]
    public Button fireButton;
    public Button poisonButton;
    public Button waterButton;
    public Button confirmButton;

    private ElementType _firstElement = ElementType.None;
    private ElementType _secondElement = ElementType.None;

    void Start()
    {
        // 给三个元素按钮绑定点击事件
        fireButton.onClick.AddListener(() => OnElementClick(ElementType.Fire));
        poisonButton.onClick.AddListener(() => OnElementClick(ElementType.Poison));
        waterButton.onClick.AddListener(() => OnElementClick(ElementType.Water));

        // ========== 核心修改 ==========
        // 初始状态：确认按钮 隐藏 + 不可点击
        confirmButton.gameObject.SetActive(false);
        confirmButton.interactable = false;

        confirmButton.onClick.AddListener(OnConfirmClick);
    }

    // 点击元素按钮的逻辑：点过的取消，没点的选中
    void OnElementClick(ElementType element)
    {
        if (_firstElement == element)
        {
            _firstElement = ElementType.None;
        }
        else if (_secondElement == element)
        {
            _secondElement = ElementType.None;
        }
        else if (_firstElement == ElementType.None)
        {
            _firstElement = element;
        }
        else if (_secondElement == ElementType.None)
        {
            _secondElement = element;
        }

        // ========== 核心修改 ==========
        // 判断是否选满 2 个元素
        bool hasTwoElements = _firstElement != ElementType.None && _secondElement != ElementType.None;

        if (hasTwoElements)
        {
            // 选满2个：显示按钮 + 可点击
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = true;
        }
        else
        {
            // 没选满：隐藏按钮 + 不可点击
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }
    }

    // 点击确认：发初始牌 + 跳转
    void OnConfirmClick()
    {
        // 1. 把选中的双元素存入全局GameManager
        GameManager.Instance.mainElementA = _firstElement;
        GameManager.Instance.mainElementB = _secondElement;

        // 2. 发放10张初始无色基础牌
        var starterCards = CardDeckLibrary.GetStarterDeck();
        foreach (var card in starterCards)
        {
            GameManager.Instance.AddCardToBag(card.cardId);
        }

        // 3. 跳转到开局选牌场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("CardDraftScene");
    }
}