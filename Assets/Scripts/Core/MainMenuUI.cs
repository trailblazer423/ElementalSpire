using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单UI管理逻辑
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// 开始游戏按钮，点击后跳转到地图场景
    /// </summary>
    public void OnStartGameClick()
    {
        // 每次开始新游戏时重置初始数据
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRunState();
        }

        ChallengeRunTracker.EnsureExists().StartRun();
        SceneManager.LoadScene("MapScene");
    }
}