using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterMenuButton : MonoBehaviour
{
    [Header("目标场景设置")]
    [SerializeField] private string targetSceneName = "A"; // 改为你的A场景名称

    // 按钮点击调用的方法
    public void LoadTargetScene()
    {
        // 检查场景是否在Build Settings中，防止报错
        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("场景 " + targetSceneName + " 未添加到Build Settings中！");
        }
    }
}