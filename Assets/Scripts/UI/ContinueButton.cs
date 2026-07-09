using UnityEngine;

public class ContinueButton : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;

public void OnContinue()
{
        // 关闭设置面板
        settingPanel.SetActive(false);

    // 如果之前暂停了游戏时间，恢复它
    Time.timeScale = 1f;
}
}