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

<<<<<<< HEAD
    // 兼容 UI 版 RankingScene 中已经序列化的关闭按钮事件。
=======
    // Update is called once per frame
    void Update()
    {

    }

>>>>>>> 67e0e46f46e66203445ef3b09758a918232be3bb
    public void Jump()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
