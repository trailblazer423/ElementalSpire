using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    // 公共退出方法，Inspector按钮事件能直接识别
    public void QuitGame()
    {
#if UNITY_EDITOR
        // 编辑器内停止播放
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 打包后关闭程序
        Application.Quit();
#endif
    }
}