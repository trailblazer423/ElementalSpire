#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FullLoopRuntimeSmokeLauncher
{
    public const string ActiveKey = "ElementalSpire.FullLoopSmoke.Active";
    public const string FinishedKey = "ElementalSpire.FullLoopSmoke.Finished";
    public const string ExitCodeKey = "ElementalSpire.FullLoopSmoke.ExitCode";

    static FullLoopRuntimeSmokeLauncher()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public static void Run()
    {
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetInt(ExitCodeKey, 1);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode
            || !SessionState.GetBool(ActiveKey, false)
            || !SessionState.GetBool(FinishedKey, false))
        {
            return;
        }

        int exitCode = SessionState.GetInt(ExitCodeKey, 1);
        SessionState.SetBool(ActiveKey, false);
        Debug.Log("[FullLoopRuntimeSmokeLauncher] Play Mode smoke finished with exit code " + exitCode);
        EditorApplication.Exit(exitCode);
    }
}
#endif
