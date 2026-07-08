using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [Header("节点基础信息")]
    public int NodeId;          // 节点唯一编号
    public string NodeType;     // 节点类型：Normal/Elite/Boss/Rest/Event/Reward
    public bool IsUnlocked;     // 是否解锁
    public bool IsCleared;      // 是否通关
    public RewardData ClearReward;

    [Header("UI组件引用")]
    public Image bgImage;       // 节点背景图
    public TextMeshProUGUI nodeNameText; // 节点名称文字
    public GameObject clearMark; // 通关打勾标记

    private Button nodeBtn;
    private Image nodeImage;

    void Awake()
    {
        nodeBtn = GetComponent<Button>();
        nodeImage = GetComponent<Image>();
        nodeBtn.onClick.AddListener(OnMapNodeClicked);
    }

    // 刷新节点显示状态
    public void RefreshView()
    {
        nodeBtn.interactable = IsUnlocked;
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

    void OnMapNodeClicked()
    {
        if (!IsUnlocked) return;


        // 鎵�鏈夎妭鐐归�昏緫缁熶竴浜ょ粰 MapManager 澶勭悊
        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnNodeClicked(NodeId, NodeType);
        }

    }
}
