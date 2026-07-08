using System.Collections;
using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// ȫ����Ϸ������������ģʽ���糡��������
/// �������ȫ�ֹ�����������ݡ��ؿ���ת����
/// </summary>
public class GameManager : MonoBehaviour
{
    // ȫ�ֵ���
    public static GameManager Instance;

    [Header("��Һ�������")]
    /// <summary>��ǰ����ֵ</summary>
    public int playerHp;
    /// <summary>�������ֵ����</summary>
    public int playerMaxHp;
    /// <summary>��ҿ��Ʊ������洢�����ѻ�õĿ���ID��ȫ���������飩</summary>
    public List<string> playerCardBag = new List<string>();

    [Header("ս������ʱ״̬")]
    /// <summary>��ǰ��ֵ</summary>
    public int playerBlock;
    /// <summary>��ǰʣ������</summary>
    public int currentEnergy;
    /// <summary>ÿ�غ������������</summary>
    public int maxEnergy;
    /// <summary>���ƶ�</summary>
    public List<string> drawPile = new List<string>();
    /// <summary>��ǰ����</summary>
    public List<string> handCards = new List<string>();
    /// <summary>���ƶ�</summary>
    public List<string> discardPile = new List<string>();

    [Header("�ؿ���ת���ݣ���ͼ<->ս��ͨ���ã�")]
    /// <summary>��ǰѡ�еĵ�ͼ�ڵ�ID</summary>
    public int currentNodeId;
    /// <summary>��ǰ�ڵ����ͣ�Normal/Elite/Boss/Rest/Event/Reward</summary>
    public string currentNodeType;
    /// <summary>����ս���Ƿ�ʤ����ս���鸳ֵ����ͼ���ȡ</summary>
    public bool isBattleWin;

    [Header("�ؿ����ȣ���ͼ<->���ת���ã�")]
    /// <summary>��ǰλ�ڵڼ��أ���1��ʼ</summary>
    public int currentFloor = 1;
    /// <summary>ÿ�ع��ж��ٸ��ڵ㣨���ڵ� ID 1~10 Ϊһ�أ�</summary>
    public const int NodesPerFloor = 10;
    /// <summary>��Ϸ�ܹ��������</summary>
    public const int MaxFloor = 3;

    private void Awake()
    {
        // ����У�飺ȫ��Ψһ���г���������
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // ��ʼ��Ĭ����ֵ
            InitDefaultData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ��ʼ����Ϸ���ֵ�Ĭ����ֵ
    /// </summary>
    private void InitDefaultData()
    {
        playerMaxHp = 100;
        playerHp = playerMaxHp;
        playerBlock = 0;
        maxEnergy = 3; // ����Ĭ��ÿ�غ�3������
        currentEnergy = maxEnergy;
        // ��ʼ������ɿ������������
        playerCardBag.Clear();
        currentFloor = 1;
        // ս���ƶѳ�ʼ����գ�ս����ʼʱ��ϴ��
        drawPile.Clear();
        handCards.Clear();
        discardPile.Clear();
    }

    /// <summary>
    /// ��ǰ�ڵ��Ƿ�Ϊ���ص����һ�ڣ��ڵ� ID % NodesPerFloor == 0 ʱΪtrue��
    /// ���ع���ͬһ���ڵ㣨ID 1~10�������ÿ�ص�10�ڶ��ᴥ���ƽ���
    /// </summary>
    public bool IsLastNodeOfFloor()
    {
        return currentNodeId % NodesPerFloor == 0;
    }

    /// <summary>
    /// �ж��Ƿ����һ��
    /// </summary>
    public bool IsLastFloor()
    {
        return currentFloor >= MaxFloor;
    }

    /// <summary>
    /// �ƽ�����һ�ء����� true = �ɹ��ƽ��� false = ȫ��ͨ�أ���Ϸʤ����
    /// </summary>
    public bool AdvanceToNextFloor()
    {
        if (IsLastFloor())
        {
            // ���3��ͨ�أ��ص� floor=1
            currentFloor = 1;
            isBattleWin = false;
            return false;
        }

        currentFloor++;
        isBattleWin = false;
        return true;
    }
}