#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElementalSpire.Cards;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FullLoopRuntimeSmokeProbe : MonoBehaviour
{
    private const string ActiveKey = "ElementalSpire.FullLoopSmoke.Active";
    private const string FinishedKey = "ElementalSpire.FullLoopSmoke.Finished";
    private const string ExitCodeKey = "ElementalSpire.FullLoopSmoke.ExitCode";

    private readonly List<string> _checks = new List<string>();
    private string _testRoot;
    private string _resultPath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!SessionState.GetBool(ActiveKey, false)
            || FindObjectOfType<FullLoopRuntimeSmokeProbe>() != null)
        {
            return;
        }

        GameObject probeObject = new GameObject("FullLoopRuntimeSmokeProbe");
        DontDestroyOnLoad(probeObject);
        probeObject.AddComponent<FullLoopRuntimeSmokeProbe>();
    }

    private void Start()
    {
        StartCoroutine(RunSmoke());
    }

    private IEnumerator RunSmoke()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(RunSmokeSteps());

        while (stack.Count > 0)
        {
            IEnumerator currentRoutine = stack.Peek();
            bool movedNext;
            object yielded = null;
            try
            {
                movedNext = currentRoutine.MoveNext();
                if (movedNext)
                    yielded = currentRoutine.Current;
            }
            catch (Exception exception)
            {
                Finish(false, exception);
                yield break;
            }

            if (!movedNext)
            {
                stack.Pop();
                continue;
            }

            if (yielded is IEnumerator nestedRoutine)
            {
                stack.Push(nestedRoutine);
                continue;
            }

            yield return yielded;
        }

        Finish(true, null);
    }

    private IEnumerator RunSmokeSteps()
    {
        _resultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "full-loop-runtime-smoke-result.txt"));
        _testRoot = Path.Combine(Application.temporaryCachePath, "ElementalSpireFullLoopSmoke");

        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, true);
        Directory.CreateDirectory(_testRoot);
        RunSaveRepository.SetEditorTestPath(Path.Combine(_testRoot, "run_save.json"));
        ChallengeRecordRepository.SetEditorTestPath(Path.Combine(_testRoot, "challenge_records.json"));
        MainMenuController menuController = FindObjectOfType<MainMenuController>();
        if (menuController != null)
            menuController.RefreshContinueButton();

            Button startButton = GameObject.Find("\u5f00\u59cb\u6e38\u620f")?.GetComponent<Button>();
            Button continueButton = GameObject.Find("\u7ee7\u7eed\u6e38\u620f")?.GetComponent<Button>();
            Button rankingButton = GameObject.Find("\u5386\u53f2\u699c")?.GetComponent<Button>();
            Require(startButton != null && continueButton != null && rankingButton != null,
                "main menu exposes start, continue and ranking buttons");
            Require(!continueButton.interactable, "continue is disabled without a safe save");
            startButton.onClick.Invoke();
            yield return WaitForScene("ElementSelectScene");
            Require(GameManager.Instance != null, "New Game creates GameManager");
            Require(ChallengeRunTracker.Instance != null && ChallengeRunTracker.Instance.HasActiveRun,
                "New Game starts challenge timer");

            GameManager gameManager = GameManager.Instance;
            gameManager.mainElementA = ElementType.Fire;
            gameManager.mainElementB = ElementType.Water;
            foreach (CardData starterCard in CardDeckLibrary.GetStarterDeck())
                gameManager.AddCardToBag(starterCard.cardId);
            gameManager.isInitialDraftDone = true;
            gameManager.currentDraftMode = GameManager.DraftMode.BattleReward;
            gameManager.gameInitialized = false;
            SceneManager.LoadScene("MapScene");
            yield return WaitForScene("MapScene");
            yield return WaitUntil(() => MapManager.Instance != null && RunSaveRepository.HasValidSave,
                "initial map safe point");

            Require(MapManager.Instance.AllMapNodes != null && MapManager.Instance.AllMapNodes.Length == 10,
                "MapScene has ten nodes");
            foreach (MapNode node in MapManager.Instance.AllMapNodes)
                Require(node.NodeType == MapManager.GetNodeType(node.NodeId), "node type " + node.NodeId);
            Require(gameManager.IsNodeUnlocked(1) && !gameManager.IsNodeCleared(1),
                "only first route entry starts available");
            RequireHud(false);
            yield return VerifyCardViewer(
                GameObject.Find("DeckButton").GetComponent<Button>(),
                gameManager.playerCardBag.Count,
                "permanent deck viewer renders existing CardView faces");

            GameObject.Find("ExitButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Require(GameObject.Find("ExitConfirmation") != null
                    && GameObject.Find("WhiteConfirmation") != null,
                "gear opens white exit confirmation");
            GameObject.Find("NoButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Require(SceneManager.GetActiveScene().name == "MapScene" && Mathf.Approximately(Time.timeScale, 1f),
                "No closes confirmation and resumes current scene");

            RunSaveData initialSave;
            string loadError;
            Require(RunSaveRepository.TryLoad(out initialSave, out loadError), "initial save is readable");
            Require(initialSave.playerHp == 100 && initialSave.currentNodeId == 0,
                "initial safe point captures HP and pre-node state");

            MapManager.Instance.OnNodeClicked(1, "Normal");
            yield return WaitForScene("BattleScene");
            yield return WaitUntil(() => FindObjectOfType<BattleManager>() != null, "BattleManager ready");
            RequireHud(true);
            BattleManager firstBattle = FindObjectOfType<BattleManager>();
            yield return VerifyCardViewer(
                GameObject.Find("DrawPileButton").GetComponent<Button>(),
                firstBattle.DeckManager.DrawPileCount,
                "battle draw pile viewer renders live CardView faces");
            yield return VerifyCardViewer(
                GameObject.Find("DiscardPileButton").GetComponent<Button>(),
                firstBattle.DeckManager.DiscardPileCount,
                "battle discard pile viewer opens even when empty");
            Require(RunSaveRepository.TryLoad(out initialSave, out loadError)
                    && !initialSave.clearedNodeIds.Contains(1)
                    && initialSave.currentNodeId == 1,
                "battle entry keeps node unfinished in safe save");

            gameManager.playerHp = 1;
            GameObject.Find("ExitButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            GameObject.Find("YesButton").GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("MainMenuScene");
            Require(RunSaveRepository.TryLoad(out initialSave, out loadError)
                    && initialSave.playerHp == 100
                    && !initialSave.clearedNodeIds.Contains(1),
                "battle gear exit updates tracker without overwriting safe HP");
            continueButton = GameObject.Find("\u7ee7\u7eed\u6e38\u620f")?.GetComponent<Button>();
            Require(continueButton != null && continueButton.interactable,
                "continue button is enabled after a safe exit");
            continueButton.onClick.Invoke();
            yield return WaitForScene("MapScene");
            yield return WaitUntil(() => MapManager.Instance != null, "MapManager after continue");
            Require(gameManager.playerHp == 100 && !gameManager.IsNodeCleared(1),
                "continue restores pre-battle HP and retryable node");

            double suspendedSeconds = ChallengeRunTracker.Instance.ActivePlaySeconds;
            ChallengeRunTracker.Instance.SuspendRun();
            yield return new WaitForSecondsRealtime(0.25f);
            Require(Math.Abs(ChallengeRunTracker.Instance.ActivePlaySeconds - suspendedSeconds) < 0.05d,
                "suspended time is excluded");
            ChallengeRunTracker.Instance.ResumeRun();

            MapManager.Instance.OnNodeClicked(1, "Normal");
            yield return WaitForScene("BattleScene");
            yield return WaitUntil(() => FindObjectOfType<BattleManager>() != null, "retry BattleManager ready");
            BattleManager retryBattle = FindObjectOfType<BattleManager>();
            retryBattle.EndBattle("win");
            yield return WaitUntil(
                () => GameObject.Find("ResultPanel") != null && GameObject.Find("ResultPanel").activeSelf,
                "ordinary victory result panel");
            GameObject.Find("ResultPanel").GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("CardDraftScene");
            yield return WaitUntil(() => GameObject.Find("SkipButton") != null, "ordinary reward skip button");
            GameObject.Find("SkipButton").GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("MapScene");
            yield return WaitUntil(() => MapManager.Instance != null, "map after ordinary victory");
            Require(gameManager.IsNodeCleared(1) && gameManager.IsNodeUnlocked(2),
                "ordinary battle victory and reward complete node one");

            MapManager.Instance.OnNodeClicked(1, "Normal");
            yield return null;
            Require(SceneManager.GetActiveScene().name == "MapScene",
                "cleared node cannot be replayed");

            gameManager.MarkNodeCleared(2);
            gameManager.MarkNodeUnlocked(3);
            gameManager.currentNodeId = 3;
            gameManager.currentNodeType = "Event";
            Require(RunSaveRepository.SaveSafePoint(), "event pre-node safe point");
            SceneManager.LoadScene("EventScene");
            yield return WaitForScene("EventScene");
            EventSceneController eventController = FindObjectOfType<EventSceneController>();
            Require(eventController != null && GameObject.Find("EventBackground") != null,
                "event scene and reused card-draft background exist");
            RequireHud(false);
            eventController.ChooseReward();
            yield return WaitForScene("CardDraftScene");
            yield return WaitUntil(() => FindObjectOfType<CardDraftManager>() != null, "event reward draft ready");
            Button skipButton = GameObject.Find("SkipButton")?.GetComponent<Button>();
            Require(skipButton != null, "draft skip button exists");
            TMP_Text skipText = skipButton.GetComponentInChildren<TMP_Text>(true);
            Require(skipText != null && skipText.text == "\u8df3\u8fc7", "draft skip label displays 跳过");
            int deckCountBeforeReward = gameManager.playerCardBag.Count;
            Button rewardButton = GameObject.Find("CardButton1")?.GetComponent<Button>();
            Require(rewardButton != null
                    && rewardButton.GetComponentInChildren<CardView>(true) != null,
                "event reward reuses the existing CardView face");
            rewardButton.onClick.Invoke();
            yield return WaitForScene("MapScene");
            yield return WaitUntil(() => MapManager.Instance != null, "map after event");
            Require(gameManager.playerCardBag.Count == deckCountBeforeReward + 1
                    && gameManager.IsNodeCleared(3)
                    && gameManager.IsNodeUnlocked(4),
                "event reward adds one battle-pool card and advances one node");

            gameManager.playerHp = 50;
            MapManager.Instance.OnNodeClicked(4, "Rest");
            yield return WaitForScene("RestScene");
            RestSceneController restController = FindObjectOfType<RestSceneController>();
            Image restBackground = GameObject.Find("RestBackground")?.GetComponent<Image>();
            Require(restController != null && restBackground != null && restBackground.sprite != null
                    && restBackground.sprite.name == "RestSceneBackground",
                "rest scene uses the provided background");
            RequireHud(false);
            restController.ChooseRest();
            yield return WaitForScene("MapScene");
            yield return WaitUntil(() => MapManager.Instance != null, "map after rest");
            Require(gameManager.playerHp == 65
                    && gameManager.IsNodeCleared(4)
                    && gameManager.IsNodeUnlocked(5),
                "rest heals 15 and advances one node");

            for (int nodeId = 5; nodeId <= 7; nodeId++)
                gameManager.MarkNodeCleared(nodeId);
            gameManager.MarkNodeUnlocked(8);
            gameManager.currentNodeId = 8;
            gameManager.currentNodeType = "Event";
            Require(RunSaveRepository.SaveSafePoint(), "remove event pre-node safe point");
            int deckCountBeforeRemove = gameManager.playerCardBag.Count;
            SceneManager.LoadScene("EventScene");
            yield return WaitForScene("EventScene");
            FindObjectOfType<EventSceneController>().ChooseRemove();
            yield return WaitForScene("CardDraftScene");
            yield return WaitUntil(() => GameObject.Find("OwnedCard_0") != null, "remove choices ready");
            GameObject.Find("OwnedCard_0").GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("MapScene");
            yield return WaitUntil(() => MapManager.Instance != null, "map after remove event");
            Require(gameManager.playerCardBag.Count == deckCountBeforeRemove - 1
                    && gameManager.IsNodeCleared(8)
                    && gameManager.IsNodeUnlocked(9),
                "event removal deletes one exact occurrence and advances");

            gameManager.currentNodeId = 9;
            gameManager.currentNodeType = "Rest";
            gameManager.playerCardBag[0] = "starter_strike";
            Require(RunSaveRepository.SaveSafePoint(), "upgrade rest pre-node safe point");
            SceneManager.LoadScene("RestScene");
            yield return WaitForScene("RestScene");
            FindObjectOfType<RestSceneController>().ChooseUpgrade();
            yield return WaitForScene("CardDraftScene");
            yield return WaitUntil(() => GameObject.Find("OwnedCard_0") != null, "upgrade choices ready");
            GameObject.Find("OwnedCard_0").GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("MapScene");
            yield return WaitUntil(() => MapManager.Instance != null, "map after upgrade");
            Require(gameManager.playerCardBag[0] == "starter_strike+"
                    && gameManager.IsNodeCleared(9)
                    && gameManager.IsNodeUnlocked(10),
                "rest upgrade persists exact card occurrence and advances");

            MapManager.Instance.OnNodeClicked(10, "Normal");
            yield return WaitForScene("BattleScene");
            yield return WaitUntil(() => FindObjectOfType<BattleManager>() != null, "final BattleManager ready");
            FindObjectOfType<BattleManager>().EndBattle("win");
            yield return WaitUntil(
                () => GameObject.Find("ResultPanel") != null && GameObject.Find("ResultPanel").activeSelf,
                "final victory result panel");
            GameObject.Find("ResultPanel").GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("MainMenuScene");
            Require(!File.Exists(RunSaveRepository.SaveFilePath), "final victory deletes continue save");
            List<ChallengeRecord> records = ChallengeRecordRepository.LoadLeaderboard(10);
            Require(records.Count == 1
                    && records[0].IsWin
                    && records[0].HighestProgress == 10,
                "final victory writes one 10/10 leaderboard record");

            MainMenuController.StartNewGame();
            yield return WaitForScene("ElementSelectScene");
            gameManager = GameManager.Instance;
            gameManager.mainElementA = ElementType.Fire;
            gameManager.mainElementB = ElementType.Water;
            foreach (CardData starterCard in CardDeckLibrary.GetStarterDeck())
                gameManager.AddCardToBag(starterCard.cardId);
            gameManager.gameInitialized = true;
            gameManager.isInitialDraftDone = true;
            gameManager.currentNodeId = 1;
            gameManager.currentNodeType = "Normal";
            gameManager.MarkNodeUnlocked(1);
            Require(RunSaveRepository.SaveSafePoint(), "loss run pre-battle safe point");
            SceneManager.LoadScene("BattleScene");
            yield return WaitForScene("BattleScene");
            yield return WaitUntil(() => FindObjectOfType<BattleManager>() != null, "loss BattleManager ready");
            FindObjectOfType<BattleManager>().EndBattle("lose");
            yield return WaitUntil(
                () => GameObject.Find("ResultPanel") != null && GameObject.Find("ResultPanel").activeSelf,
                "defeat result panel");
            GameObject.Find("ResultPanel").GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("MainMenuScene");
            Require(!File.Exists(RunSaveRepository.SaveFilePath), "defeat deletes continue save");
            records = ChallengeRecordRepository.LoadLeaderboard(10);
            Require(records.Count == 2
                    && records.Any(record => record.IsWin && record.HighestProgress == 10)
                    && records.Any(record => !record.IsWin && record.HighestProgress == 0),
                "victory and defeat both write leaderboard records");

            rankingButton = GameObject.Find("\u5386\u53f2\u699c")?.GetComponent<Button>();
            Require(rankingButton != null, "main-menu ranking button remains available");
            rankingButton.onClick.Invoke();
            yield return WaitForScene("RankingScene");
            yield return WaitUntil(() => GameObject.Find("DynamicLeaderboardPanel") != null,
                "ranking UI generated");
            Require(GameObject.Find("DynamicLeaderboardPanel") != null,
                "ranking scene displays stored records");

        yield break;
    }

    private void RequireHud(bool battleScene)
    {
        Require(GameObject.Find("GlobalRunHudRoot") != null, "global HUD exists in " + SceneManager.GetActiveScene().name);
        Canvas hudCanvas = GameObject.Find("GlobalRunHudCanvas")?.GetComponent<Canvas>();
        Require(hudCanvas != null && hudCanvas.isRootCanvas && hudCanvas.sortingOrder == 30000,
            "global HUD uses a dedicated top sorting canvas");
        Require(GameObject.Find("HealthPanel") != null
                && GameObject.Find("DeckButton") != null
                && GameObject.Find("ExitButton") != null,
            "HP, deck and gear controls exist");
        if (battleScene)
        {
            Require(GameObject.Find("DrawPileButton") != null
                    && GameObject.Find("DiscardPileButton") != null,
                "battle draw and discard viewers exist");
        }
    }

    private IEnumerator VerifyCardViewer(Button openButton, int expectedCardCount, string description)
    {
        Require(openButton != null && openButton.interactable, description + " button is clickable");
        openButton.onClick.Invoke();
        yield return null;

        GameObject overlay = GameObject.Find("CardCollectionOverlay");
        Require(overlay != null && overlay.activeSelf, description + " overlay opens");
        Require(overlay.GetComponentsInChildren<CardView>(true).Length == expectedCardCount,
            description + " card count");

        Button closeButton = overlay.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.gameObject.name == "CloseButton");
        Require(closeButton != null, description + " close button exists");
        closeButton.onClick.Invoke();
        yield return null;
        Require(!overlay.activeSelf, description + " overlay closes");
    }

    private IEnumerator WaitForScene(string sceneName)
    {
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == sceneName, "scene " + sceneName);
        yield return null;
        yield return null;
    }

    private IEnumerator WaitUntil(Func<bool> condition, string description, float timeoutSeconds = 8f)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
        Require(condition(), "timeout waiting for " + description);
        _checks.Add("READY: " + description);
    }

    private void Require(bool condition, string description)
    {
        if (!condition)
            throw new InvalidOperationException("Smoke assertion failed: " + description);
        _checks.Add("PASS: " + description);
    }

    private void Finish(bool passed, Exception exception)
    {
        Time.timeScale = 1f;
        string result = passed
            ? "PASS\n" + string.Join("\n", _checks)
            : "FAIL\n" + exception;
        File.WriteAllText(_resultPath, result);
        Debug.Log("[FullLoopRuntimeSmoke] " + (passed ? "PASS" : "FAIL") + " result=" + _resultPath);
        if (!passed)
            Debug.LogError(exception);

        RunSaveRepository.ClearEditorTestPath();
        ChallengeRecordRepository.ClearEditorTestPath();
        try
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }
        catch (Exception cleanupException)
        {
            Debug.LogWarning("[FullLoopRuntimeSmoke] Test cleanup failed: " + cleanupException.Message);
        }

        SessionState.SetInt(ExitCodeKey, passed ? 0 : 1);
        SessionState.SetBool(FinishedKey, true);
        EditorApplication.ExitPlaymode();
    }
}
#endif
