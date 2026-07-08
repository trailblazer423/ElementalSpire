using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ElementalSpire.Cards;
using System.Linq;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("场景中所有地图节点")]
    public MapNode[] AllMapNodes;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ========== ��ť������������� OnXxxClicked �淶��==========
    /// <summary>
    /// �������˵���ť���
    /// </summary>
    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// ��Ϸ�����������̣�ѡԪ�� �� ��10�Ż����� �� 3����ѡ �� ������1��
    /// </summary>
    private IEnumerator GameStartFlow()
    {
        Debug.Log("[MapManager] ===== �������̿�ʼ =====");

        // 1. ֱ�Ӹ�ֵ����Ԫ�أ���ȫȥ��UI�ȴ�
        ElementType eleA = ElementType.Fire;
        ElementType eleB = ElementType.Poison;
        GameManager.Instance.mainElementA = eleA;
        GameManager.Instance.mainElementB = eleB;
        Debug.Log($"[MapManager] ˫Ԫ�������ã�{eleA} + {eleB}");

        // 2. ����10�ų�ʼ��ɫ��
        var starterCards = CardDeckLibrary.GetStarterDeck();
        foreach (var card in starterCards)
        {
            GameManager.Instance.AddCardToBag(card.cardId);
        }
        Debug.Log($"[MapManager] ��ʼ�Ʒ�����ɣ���ǰ�ƿ�������{GameManager.Instance.playerCardBag.Count}");

        // 3. ִ��3�ο���ѡ��
        Debug.Log("[MapManager] ��ʼ��1�ο���ѡ�ƣ�ƫԪ��A��");
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleA),
            DraftPhase.Start));

        Debug.Log("[MapManager] ��ʼ��2�ο���ѡ�ƣ�ƫԪ��B��");
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleB, eleB),
            DraftPhase.Start));

        Debug.Log("[MapManager] ��ʼ��3�ο���ѡ�ƣ�˫Ԫ�ػ�ϣ�");
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleB),
            DraftPhase.Start));

        Debug.Log("[MapManager] 3��ѡ��ȫ�����");

        // 4. ������1���ڵ�
        UnlockNextNodes(0);
        Debug.Log("[MapManager] ��ִ�н����ڵ�1");

        // 5. ��ǳ�ʼ����ɣ�ˢ�����нڵ���ͼ
        GameManager.Instance.gameInitialized = true;
        RefreshAllNodes();

        Debug.Log($"[MapManager] ===== �������̽��� ===== �����ƿ�����{GameManager.Instance.playerCardBag.Count}");
    }

    /// <summary>
    /// ִ��һ����ѡһ������ѡ��������
    /// </summary>
    private IEnumerator DoDraftSelect(IEnumerable<CardData> fullPool, DraftPhase phase, bool canSkip = true)
    {
        List<CardData> options = GetRandomCardsByRarity(
            fullPool.ToList(), 3, GameManager.Instance.currentFloor, phase);

        // ��ӡ��ѡ�ƣ���������֤�Ƴ��Ƿ���ȷ
        string cardNames = options.Count > 0
            ? string.Join("��", options.Select(c => c.cardName))
            : "�޿�����";
        Debug.Log($"[MapManager] ��ѡ�ƣ�{cardNames}");

        // ����ģʽ��Ĭ��ѡ��һ�ţ����ȴ�UI
        CardData selectedCard = options.Count > 0 ? options[0] : null;

        if (selectedCard != null)
        {
            GameManager.Instance.AddCardToBag(selectedCard.cardId);
            Debug.Log($"[MapManager] ѡ�У�{selectedCard.cardName}");
        }
        else
        {
            Debug.Log("[MapManager] ��������ѡ��");
        }

        yield return null; // ֻ�ȴ�һ֡����������
    }

    /// <summary>
    /// ��ϡ�ж�Ȩ�ش��Ƴ��������ȡָ����������
    /// </summary>
    private List<CardData> GetRandomCardsByRarity(
        List<CardData> pool, int count, int floor, DraftPhase phase)
    {
        if (pool.Count <= count) return new List<CardData>(pool);

        // ���׶�����ϡ�ж�Ȩ�أ���ȫ��Ӧ�����
        (int common, int rare, int precious) = phase switch
        {
            DraftPhase.Start => (80, 20, 0),
            DraftPhase.Battle1_3 => (75, 25, 0),
            DraftPhase.Battle4_7 => (55, 35, 10),
            DraftPhase.Battle8_10 => (35, 40, 25),
            _ => (80, 20, 0)
        };

        List<CardData> result = new List<CardData>();
        List<CardData> remaining = new List<CardData>(pool);

        for (int i = 0; i < count; i++)
        {
            if (remaining.Count == 0) break;

            // �����������ϡ�ж�
            int total = common + rare + precious;
            int roll = Random.Range(0, total);
            string targetRarity;

            if (roll < common)
                targetRarity = CardDeckLibrary.Common;
            else if (roll < common + rare)
                targetRarity = CardDeckLibrary.Rare;
            else
                targetRarity = CardDeckLibrary.Precious;

            // ɸѡ��Ӧϡ�жȵ��ƣ�û�оʹ�ȫ���ﶵ��
            var rarityPool = remaining.Where(c => c.rarity == targetRarity).ToList();
            if (rarityPool.Count == 0)
                rarityPool = remaining;

            // �����һ�ţ������ظ�
            CardData picked = rarityPool[Random.Range(0, rarityPool.Count)];
            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MapScene") return;

        EnsureRewardManager();
        EnsureGameManager();

        // ��������ӡ״̬��ȷ�Ϸ����Ƿ�ִ��
        Debug.Log($"[MapManager] ��ͼ������ɣ�gameInitialized={GameManager.Instance.gameInitialized}");

        if (!GameManager.Instance.gameInitialized)
        {
            Debug.Log("[MapManager] ���뿪�ֳ�ʼ������");
            StartCoroutine(GameStartFlow());
            return; // ��ʼ�����ǰ��ִ�к����߼�
        }

        Debug.Log($"[MapManager] ��ͼ���أ�isBattleWin={GameManager.Instance?.isBattleWin}, currentFloor={GameManager.Instance?.currentFloor}, AllMapNodes����={(AllMapNodes != null ? AllMapNodes.Length : 0)}");

        if (GameManager.Instance != null && GameManager.Instance.isBattleWin)
        {
            int winNodeId = GameManager.Instance.currentNodeId;
            bool isLastNode = GameManager.Instance.IsLastNodeOfFloor();
            Debug.Log($"[MapManager] ����ʤ����winNodeId={winNodeId}, isLastNode={isLastNode}");

            foreach (var node in AllMapNodes)
            {
                if (node == null)
                {
                    Debug.LogWarning("[MapManager] AllMapNodes �д��ڿ����ã����� Inspector ����");
                    continue;
                }

                if (node.NodeId == winNodeId)
                {
                    node.IsCleared = true;
                    RewardManager.Instance?.GrantReward(node.ClearReward);
                }
                else if (!isLastNode && node.NodeId == winNodeId + 1)
                {
                    node.IsUnlocked = true;
                }
            }

            if (isLastNode)
            {
                // �����ƽ�����һ��
                bool hasNextFloor = GameManager.Instance.AdvanceToNextFloor();
                if (!hasNextFloor)
                {
                    // ��3��ͨ�أ��ص����˵�
                    Debug.Log("[MapManager] ȫ��3��ͨ�أ���Ϸʤ����");
                    SceneManager.LoadScene("MainMenuScene");
                    return;
                }

                // ����һ�أ��������нڵ㣨����ͬһ��10���ڵ㣩
                Debug.Log($"[MapManager] ����� {GameManager.Instance.currentFloor} ��");
                ResetNodesForNewFloor();
                RefreshAllNodes();
                return;
            }

            GameManager.Instance.isBattleWin = false;
        }

        RefreshAllNodes();
    }

    private void RefreshAllNodes()
    {
        foreach (var node in AllMapNodes)
        {
            if (node != null)
                node.RefreshView();
        }
    }

    /// <summary>
    /// �������нڵ㣨����ͬһ��10���ڵ㣩��������СNodeId�Ľڵ㣨�ڵ�1����
    /// </summary>
    private void ResetNodesForNewFloor()
    {
        int minNodeId = int.MaxValue;
        int resetCount = 0;

        foreach (var node in AllMapNodes)
        {
            if (node == null)
            {
                Debug.LogWarning("[MapManager] ResetNodesForNewFloor: ����������");
                continue;
            }

            node.IsCleared = false;
            node.IsUnlocked = false;
            resetCount++;

            if (node.NodeId < minNodeId)
                minNodeId = node.NodeId;
        }

        Debug.Log($"[MapManager] ������ {resetCount} ���ڵ㣬��С NodeId={minNodeId}");

        if (minNodeId == int.MaxValue)
        {
            Debug.LogError("[MapManager] û���ҵ��κ���Ч�ڵ㣡���� AllMapNodes ����");
            return;
        }

        // ������СNodeId�Ľڵ㣨���ڵ�1��
        foreach (var node in AllMapNodes)
        {
            if (node != null && node.NodeId == minNodeId)
            {
                node.IsUnlocked = true;
                Debug.Log($"[MapManager] �����ڵ� NodeId={node.NodeId}");
                break;
            }
        }
    }

    public void UnlockNextNodes(int currentNodeId)
    {
        foreach (var node in AllMapNodes)
        {
            if (node != null && node.NodeId == currentNodeId + 1)
            {
                node.IsUnlocked = true;
                node.RefreshView();
            }
        }
    }

    private void EnsureRewardManager()
    {
        if (RewardManager.Instance != null) return;
        new GameObject("RewardManager", typeof(RewardManager));
    }

    private void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[MapManager] GameManager �����ڣ����Զ�����");
    }

    private enum DraftPhase
    {
        Start,      // ����ѡ��
        Battle1_3,  // 1-3��ս������
        Battle4_7,  // 4-7��ս������
        Battle8_10  // 8-10��ս������
    }

    /// <summary>
    /// ���е�ͼ�ڵ�����ͳһ��ڣ�ռλ�������������貹ȫ�߼���
    /// </summary>
    public void OnNodeClicked(int nodeId, string nodeType)
    {
        // ��ʱ���ݣ�ս���ڵ��վ��г��������������Ȳ�����
        if (nodeType == "Normal" || nodeType == "Elite" || nodeType == "Boss")
        {
            GameManager.Instance.currentNodeId = nodeId;
            GameManager.Instance.currentNodeType = nodeType;
            SceneManager.LoadScene("BattleScene");
        }
    }
}
