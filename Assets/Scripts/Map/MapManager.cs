using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("场景中所有地图节点")]
    public MapNode[] AllMapNodes;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnEnterBattleClicked()
    {
        SceneManager.LoadScene("BattleScene");
    }

    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MapScene") return;

        EnsureRewardManager();

        if (GameManager.Instance != null && GameManager.Instance.isBattleWin)
        {
            int winNodeId = GameManager.Instance.currentNodeId;

            foreach (var node in AllMapNodes)
            {
                if (node == null) continue;

                if (node.NodeId == winNodeId)
                {
                    node.IsCleared = true;
                    RewardManager.Instance?.GrantReward(node.ClearReward);
                }

                if (node.NodeId == winNodeId + 1)
                {
                    node.IsUnlocked = true;
                }
            }

            GameManager.Instance.isBattleWin = false;
        }

        foreach (var node in AllMapNodes)
        {
            if (node != null)
                node.RefreshView();
        }
    }

    public void UnlockNextNodes(int currentNodeId)
    {
        foreach (var node in AllMapNodes)
        {
            if (node != null && node.NodeId == currentNodeId + 1)
            {
                node.IsUnlocked = true;
                node.RefreshView();
            }
        }
    }

    private void EnsureRewardManager()
    {
        if (RewardManager.Instance != null) return;
        new GameObject("RewardManager", typeof(RewardManager));
    }
}
