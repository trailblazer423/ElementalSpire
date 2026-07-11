using UnityEngine;

/// <summary>
/// Installs the run HUD before the first scene loads. No scene asset needs a serialized reference.
/// </summary>
public static class RunHudBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (Object.FindObjectOfType<RunHudController>(true) != null)
            return;

        var root = new GameObject("GlobalRunHud");
        root.AddComponent<RunHudController>();
    }
}
