using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单UI交互逻辑
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// 开始游戏按钮点击回调，跳转至地图场景
    /// </summary>
    public void OnStartGameClick()
    {
        // 可选：每次开始新游戏重置全局数据（需要先把GameManager的InitDefaultData改为public）
        // GameManager.Instance.InitDefaultData();

        // 加载地图场景，场景名需与项目中的场景文件名完全一致
        SceneManager.LoadScene("MapScene");
    }
}