using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>主菜单的新游戏/继续游戏入口，并按按钮对象名自动接线。</summary>
public class MainMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenuScene";
    private Button _newGameButton;
    private Button _continueButton;
    private bool _newGameRuntimeListenerAdded;
    private bool _continueRuntimeListenerAdded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, MainMenuSceneName, StringComparison.Ordinal))
            return;

        if (FindObjectOfType<MainMenuController>() != null)
            return;

        new GameObject("MainMenuController").AddComponent<MainMenuController>();
    }

    private void Awake()
    {
        // 回到主菜单即离开主动游玩；保留本局但停止累计时长。
        if (ChallengeRunTracker.Instance != null && ChallengeRunTracker.Instance.HasActiveRun)
        {
            ChallengeRunTracker.Instance.SuspendRun();
            RunSaveRepository.UpdateChallengeStateOnly();
        }

        _newGameButton = FindButtonByName("开始游戏", "StartGame", "NewGame", "Start Game");
        _continueButton = FindButtonByName("继续游戏", "ContinueGame", "Continue Game", "Continue");

        _newGameRuntimeListenerAdded = BindOnlyWhenNoPersistentCall(_newGameButton, OnNewGameClicked);
        _continueRuntimeListenerAdded = BindOnlyWhenNoPersistentCall(_continueButton, OnContinueGameClicked);
        RefreshContinueButton();
    }

    private void OnDestroy()
    {
        if (_newGameRuntimeListenerAdded && _newGameButton != null)
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
        if (_continueRuntimeListenerAdded && _continueButton != null)
            _continueButton.onClick.RemoveListener(OnContinueGameClicked);
    }

    public void OnNewGameClicked()
    {
        StartNewGame();
    }

    public void OnContinueGameClicked()
    {
        ContinueGame();
        RefreshContinueButton();
    }

    /// <summary>统一的新游戏入口：删除旧安全点、重置状态、重新开始主动计时。</summary>
    public static void StartNewGame()
    {
        RunSaveRepository.DeleteSave();

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            GameObject managerObject = new GameObject("GameManager");
            gameManager = managerObject.AddComponent<GameManager>();
        }
        gameManager.ResetRunState();

        ChallengeRunTracker tracker = ChallengeRunTracker.EnsureExists();
        tracker.ResetTracking();
        tracker.StartRun();
        SceneManager.LoadScene("ElementSelectScene");
    }

    /// <summary>统一的继续入口：只恢复最后安全地图状态，然后进入地图。</summary>
    public static void ContinueGame()
    {
        string error;
        if (!RunSaveRepository.TryRestoreLatest(out error))
        {
            Debug.LogWarning("[MainMenuController] 无法继续游戏：" + error);
            return;
        }

        SceneManager.LoadScene("MapScene");
    }

    public void RefreshContinueButton()
    {
        if (_continueButton != null)
            _continueButton.interactable = RunSaveRepository.HasValidSave;
    }

    private static bool BindOnlyWhenNoPersistentCall(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return false;

        // 场景已有持久化回调时不重复追加，避免一次点击发生双重重置或双重跳转。
        if (button.onClick.GetPersistentEventCount() > 0)
            return false;

        button.onClick.AddListener(action);
        return true;
    }

    private static Button FindButtonByName(params string[] acceptedNames)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                foreach (string acceptedName in acceptedNames)
                {
                    if (string.Equals(button.gameObject.name, acceptedName, StringComparison.OrdinalIgnoreCase))
                        return button;
                }
            }
        }
        return null;
    }
}
