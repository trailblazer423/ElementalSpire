using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterMenuButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // 兼容 UI 版 RankingScene 中已经序列化的关闭按钮事件。
    public void Jump()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
