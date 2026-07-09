using UnityEngine;

public class SettingButton : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;

    public void OpenSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            Time.timeScale = 0f; // 暂停游戏
        }
    }
}