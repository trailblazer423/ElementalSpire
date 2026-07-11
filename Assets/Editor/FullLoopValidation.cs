#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FullLoopValidation
{
    public static void Run()
    {
        var errors = new List<string>();
        ValidateBuildSettings(errors);
        ValidateMapScene(errors);
        ValidateChoiceScene<EventSceneController>("Assets/Scenes/EventScene.unity", errors);
        ValidateChoiceScene<RestSceneController>("Assets/Scenes/RestScene.unity", errors);
        ValidateControllerScene<MainMenuController>("Assets/Scenes/MainMenuScene.unity", errors);
        ValidateControllerScene<RankingSceneController>("Assets/Scenes/RankingScene.unity", errors);
        ValidateCardCodec(errors);

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity", OpenSceneMode.Single);

        if (errors.Count > 0)
        {
            foreach (string error in errors)
                Debug.LogError("[FullLoopValidation] " + error);
            throw new BuildFailedException("Full loop validation failed with " + errors.Count + " error(s).");
        }

        Debug.Log("[FullLoopValidation] PASS: scenes, ten-node route, controllers, build order and card codec.");
    }

    private static void ValidateBuildSettings(List<string> errors)
    {
        string[] required =
        {
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/ElementSelectScene.unity",
            "Assets/Scenes/CardDraftScene.unity",
            "Assets/Scenes/MapScene.unity",
            "Assets/Scenes/BattleScene.unity",
            "Assets/Scenes/EventScene.unity",
            "Assets/Scenes/RestScene.unity",
            "Assets/Scenes/RankingScene.unity"
        };

        EditorBuildSettingsScene[] configured = EditorBuildSettings.scenes;
        if (configured.Length < required.Length)
        {
            errors.Add("Build Settings scene count is too small.");
            return;
        }

        for (int index = 0; index < required.Length; index++)
        {
            if (!configured[index].enabled || configured[index].path != required[index])
                errors.Add($"Build Settings index {index} expected {required[index]}.");
        }
    }

    private static void ValidateMapScene(List<string> errors)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity", OpenSceneMode.Single);
        MapNode[] nodes = Object.FindObjectsOfType<MapNode>(true);
        if (nodes.Length != GameManager.NodesPerFloor)
            errors.Add($"MapScene expected 10 MapNode components, found {nodes.Length}.");

        int[] ids = nodes.Select(node => node.NodeId).OrderBy(id => id).ToArray();
        if (!ids.SequenceEqual(Enumerable.Range(1, GameManager.NodesPerFloor)))
            errors.Add("MapScene node IDs are not exactly 1..10.");

        foreach (MapNode node in nodes)
        {
            string expected = MapManager.GetNodeType(node.NodeId);
            if (node.NodeType != expected)
                errors.Add($"Node {node.NodeId} type is {node.NodeType}, expected {expected}.");
        }

        GameObject legacyPanel = FindSceneObject(scene, "EventChoicePanel");
        if (legacyPanel != null && legacyPanel.activeSelf)
            errors.Add("Legacy EventChoicePanel is still active in MapScene.");
    }

    private static void ValidateChoiceScene<T>(string path, List<string> errors) where T : Component
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (Object.FindObjectOfType<T>(true) == null)
            errors.Add(path + " is missing " + typeof(T).Name + ".");
        if (Object.FindObjectOfType<Canvas>(true) == null)
            errors.Add(path + " is missing a Canvas.");
        if (Object.FindObjectOfType<EventSystem>(true) == null)
            errors.Add(path + " is missing an EventSystem.");

        if (typeof(T) == typeof(RestSceneController))
        {
            GameObject background = FindSceneObject(scene, "RestBackground");
            Image image = background != null ? background.GetComponent<Image>() : null;
            if (image == null || image.sprite == null)
                errors.Add("RestScene background image is missing.");
            else
                Debug.Log("[FullLoopValidation] Rest background asset: " + AssetDatabase.GetAssetPath(image.sprite));
        }
    }

    private static void ValidateControllerScene<T>(string path, List<string> errors) where T : Component
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (Object.FindObjectOfType<T>(true) == null)
            errors.Add(path + " is missing " + typeof(T).Name + ".");
    }

    private static void ValidateCardCodec(List<string> errors)
    {
        CardInstance decoded = CardInstanceCodec.Decode("starter_strike+");
        if (decoded == null || decoded.cardId != "starter_strike" || !decoded.isUpgraded)
            errors.Add("CardInstanceCodec failed to decode an upgraded card.");
        if (CardInstanceCodec.Encode(decoded) != "starter_strike+")
            errors.Add("CardInstanceCodec failed to encode an upgraded card.");
        int configuredFloors = GameManager.MaxFloor;
        int configuredNodes = GameManager.NodesPerFloor;
        if (configuredFloors != 1 || configuredNodes != 10)
            errors.Add("GameManager is not configured for one ten-node run.");
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                    return transform.gameObject;
            }
        }
        return null;
    }
}
#endif
