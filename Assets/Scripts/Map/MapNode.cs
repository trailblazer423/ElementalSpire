using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [Header("�ڵ������Ϣ")]
    public int NodeId;          // �ڵ�Ψһ���
    public string NodeType;     // �ڵ����ͣ�Normal/Elite/Boss/Rest/Event/Reward
    public bool IsUnlocked;     // �Ƿ����
    public bool IsCleared;      // �Ƿ�ͨ��
    public RewardData ClearReward;

    [Header("UI�������")]
    public Image bgImage;       // �ڵ㱳��ͼ
    public TextMeshProUGUI nodeNameText; // �ڵ���������
    public GameObject clearMark; // ͨ�ش򹴱��

    private Button nodeBtn;
    private Image nodeImage;

    void Awake()
    {
        nodeBtn = GetComponent<Button>();
        nodeImage = GetComponent<Image>();
        nodeBtn.onClick.AddListener(OnMapNodeClicked);
    }

    // ˢ�½ڵ���ʾ״̬
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


        // 所有节点逻辑统一交给 MapManager 处理
        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnNodeClicked(NodeId, NodeType);
        }

    }
}
