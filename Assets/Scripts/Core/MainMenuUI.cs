using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ���˵�UI�����߼�
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// ��ʼ��Ϸ��ť����ص�����ת����ͼ����
    /// </summary>
    public void OnStartGameClick()
    {
        // ÿ�ο�ʼ���¾���Ϸʱ���ó�ʼ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentFloor = 1;
            GameManager.Instance.currentNodeId = 0;
            GameManager.Instance.currentNodeType = string.Empty;
            GameManager.Instance.isBattleWin = false;
            GameManager.Instance.playerHp = GameManager.Instance.playerMaxHp;
            GameManager.Instance.playerCardBag.Clear();
        }

        ChallengeRunTracker.EnsureExists().StartRun();
        SceneManager.LoadScene("MapScene");
    }
}