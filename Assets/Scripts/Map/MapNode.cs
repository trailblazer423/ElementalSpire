using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [Header("节点基础信息")]
    public int NodeId;          // 节点唯一标识
    public string NodeType;     // 节点类型：Normal/Elite/Boss/Rest/Event/Reward
    public bool IsUnlocked;     // 是否解锁
    public bool IsCleared;      // 是否通关
    public RewardData ClearReward;

    [Header("UI组件引用")]
    public Image bgImage;       // 节点背景图
    public TextMeshProUGUI nodeNameText; // 节点名称文本
    public GameObject clearMark; // 通关打勾标记

    private Button nodeBtn;
    private Image nodeImage;

    void Awake()
    {
        nodeBtn = GetComponent<Button>();
        nodeImage = GetComponent<Image>();

        if (nodeBtn != null)
        {
            nodeBtn.onClick.AddListener(OnMapNodeClicked);
        }
    }

    void OnDestroy()
    {
        if (nodeBtn != null)
        {
            nodeBtn.onClick.RemoveListener(OnMapNodeClicked);
        }
    }

    // 刷新节点显示状态
    public void RefreshView()
    {
        if (nodeBtn != null)
        {
            nodeBtn.interactable = IsUnlocked && !IsCleared;
        }

        if (nodeImage != null)
        {
            if (IsCleared)
            {
                nodeImage.color = Color.green;
            }
            else if (IsUnlocked)
            {
                nodeImage.color = Color.white;
            }
            else
            {
                nodeImage.color = Color.gray;
            }
        }

        if (clearMark != null)
        {
            clearMark.SetActive(IsCleared);
        }

        if (nodeNameText != null)
        {
            string displayType = NodeType == "Event"
                ? "事件"
                : NodeType == "Rest"
                    ? "休息"
                    : "战斗";
            nodeNameText.text = $"{NodeId}  {displayType}";
        }
    }

    void OnMapNodeClicked()
    {
        if (!IsUnlocked || IsCleared) return;

        // 所有节点逻辑统一交给 MapManager 处理
        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnNodeClicked(NodeId, NodeType);
        }
        else
        {
            Debug.LogError("[MapNode] MapManager 不存在，无法处理节点点击");
        }
    }
}
