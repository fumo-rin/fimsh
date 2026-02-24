using RinCore;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FishStageSelector : MonoBehaviour
{
    #region Level Selector
    public void SelectAct(int index)
    {
        if (GetLevelsOfAct(index, out List<string> levels))
        {
            DestroyAll();
            int iteration = 0;
            string[] actFolders = GetSortedActFolders();
            string actNumber = (((index % actFolders.Length) + actFolders.Length) % actFolders.Length + 1).ToString();
            foreach (var item in levels)
            {
                Button b = CreateItem(LevelSelectButton);
                if (b.GetComponentInChildren<TMP_Text>() is TMP_Text t)
                {
                    GetLevelName(index, iteration, out string levelName);
                    t.text = $"{actNumber}-{iteration + 1}##{levelName}".ReplaceLineBreaks("##");
                }
                int currentLevel = iteration;
                b.BindSingleAction(() => FindAndStartLevel(index, currentLevel));

                if (currentLevel == 0) StartCoroutine(SelectNextFrame(b.gameObject));
                iteration++;
            }
            actText.text = CurrentAct;
            if (GetActMeta(index, out string actInfo, out string actCredits))
            {
                actCreditsText.text = actCredits.ReplaceLineBreaks("##");
                actInfoText.text = actInfo.ReplaceLineBreaks("##");
            }
            else
            {
                actCreditsText.text = "";
                actInfoText.text = "";
            }
        }
    }
    IEnumerator SelectNextFrame(GameObject obj)
    {
        yield return null;
        if (obj != null)
            obj.Select_WithEventSystem();
    }
    void FindAndStartLevel(int act, int level)
    {
        if (!GetLevel(act, level, out string levelString, out string levelName))
        {
            Debug.LogError($"Invalid Level : Act {act} Level {level}");
            return;
        }
        FishTools.SelectLevel(levelString, new()
        {
            dialogueStack = null,
            forceActivateNodes = true,
            shouldDisplayLevelName = true,
            levelName = levelName,
            BombPointLoss = 0,
            gamemode = FishTools.stageSettings.Gamemode.StageSelect
        }, gameScene);
    }
    private void DestroyAll()
    {
        LevelSelectButton.gameObject.SetActive(false);
        foreach (var item in createdItems.ToList())
        {
            if (item.GetComponent<Button>() is Button b)
                b.RemoveAllClickActions();
            Destroy(item);
        }
        createdItems.Clear();
    }
    private T CreateItem<T>(T g) where T : MonoBehaviour
    {
        T item = Instantiate(g, createdItemsAnchor);
        createdItems.Add(item.gameObject);
        item.gameObject.SetActive(true);
        return item;
    }
    #endregion
    #region Acts + Levels Finder
    [SerializeField] TMP_Text actText;
    private static string cachedActName = "";
    public static string CurrentAct => string.IsNullOrEmpty(cachedActName) ? "Select An Act" : cachedActName;
    private static string ActsRoot => Path.Combine(Application.streamingAssetsPath, "Acts");

    static readonly Regex ActNumberRegex = new(@"^\((\d+)\)", RegexOptions.Compiled);
    static int GetActSortKey(string folderPath)
    {
        string name = Path.GetFileName(folderPath);
        var match = ActNumberRegex.Match(name);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            return number;
        return int.MaxValue;
    }
    static string GetCleanActName(string folderPath)
    {
        string name = Path.GetFileName(folderPath);
        return ActNumberRegex.Replace(name, "").Trim();
    }

    static string[] GetSortedActFolders()
    {
        if (!Directory.Exists(ActsRoot))
            return new string[0];

        return Directory
            .GetDirectories(ActsRoot)
            .OrderBy(d => GetActSortKey(d))
            .ThenBy(d => GetCleanActName(d))
            .ToArray();
    }
    public static bool GetActMeta(int actIndex, out string infoText, out string creditsText)
    {
        infoText = null;
        creditsText = null;

        string[] actFolders = GetSortedActFolders();
        if (actFolders.Length == 0)
        {
            Debug.LogWarning("No act folders found.");
            return false;
        }

        actIndex = ((actIndex % actFolders.Length) + actFolders.Length) % actFolders.Length;
        string selectedAct = actFolders[actIndex];

        try
        {
            string infoPath = Path.Combine(selectedAct, "info.txt");
            string creditsPath = Path.Combine(selectedAct, "credits.txt");

            if (File.Exists(infoPath))
                infoText = File.ReadAllText(infoPath);

            if (File.Exists(creditsPath))
                creditsText = File.ReadAllText(creditsPath);

            return true;
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed reading act meta files: {e.Message}");
            return false;
        }
    }
    public static bool GetLevelName(int actIndex, int levelIndex, out string levelName)
    {
        levelName = null;

        string[] actFolders = GetSortedActFolders();
        if (actFolders.Length == 0)
        {
            Debug.LogWarning("No act folders found.");
            return false;
        }

        actIndex = ((actIndex % actFolders.Length) + actFolders.Length) % actFolders.Length;
        string selectedAct = actFolders[actIndex];

        string[] levelFiles = GetPlayableLevelFiles(selectedAct);

        if (levelFiles.Length == 0)
        {
            Debug.LogWarning($"No playable level files found in: {selectedAct}");
            return false;
        }

        var sortedLevelFiles = levelFiles
            .Select(f => new
            {
                Path = f,
                PrefixNumber = ParsePrefixNumber(f)
            })
            .OrderBy(x => x.PrefixNumber)
            .ToArray();

        levelIndex = ((levelIndex % sortedLevelFiles.Length) + sortedLevelFiles.Length) % sortedLevelFiles.Length;

        string fileName = Path.GetFileNameWithoutExtension(sortedLevelFiles[levelIndex].Path);
        int dashIndex = fileName.IndexOf('-');

        levelName = dashIndex >= 0 && dashIndex + 1 < fileName.Length
            ? fileName.Substring(dashIndex + 1).Trim()
            : fileName.Trim();

        return true;
    }

    private static int ParsePrefixNumber(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        int dashIndex = fileName.IndexOf('-');

        if (dashIndex > 0)
        {
            string prefix = fileName.Substring(0, dashIndex).Trim();
            if (int.TryParse(prefix, out int result))
                return result;
        }
        return int.MaxValue;
    }
    public static bool GetLevelsOfAct(int actIndex, out List<string> levelNames)
    {
        levelNames = new();

        string[] actFolders = GetSortedActFolders();
        if (actFolders.Length == 0)
        {
            Debug.LogWarning("No act folders found.");
            return false;
        }

        actIndex = ((actIndex % actFolders.Length) + actFolders.Length) % actFolders.Length;
        string selectedAct = actFolders[actIndex];

        cachedActName = GetCleanActName(selectedAct);

        string[] levelFiles = GetPlayableLevelFiles(selectedAct);

        if (levelFiles.Length == 0)
        {
            Debug.LogWarning($"No playable level files found in: {selectedAct}");
            return false;
        }

        levelNames = levelFiles
            .Select(f => new
            {
                Path = f,
                PrefixNumber = ParsePrefixNumber(f)
            })
            .OrderBy(x => x.PrefixNumber)
            .Select(x => Path.GetFileNameWithoutExtension(x.Path))
            .ToList();

        return true;
    }
    static readonly HashSet<string> IgnoredLevelFiles = new() { "info", "credits" };
    static string[] GetPlayableLevelFiles(string actFolder)
    {
        return Directory
            .GetFiles(actFolder, "*.txt")
            .Where(f =>
            {
                string name = Path.GetFileNameWithoutExtension(f)
                    .ToLowerInvariant();

                return !IgnoredLevelFiles.Contains(name);
            })
            .ToArray();
    }
    public static bool GetLevel(int actIndex, int levelIndex, out string levelString, out string levelName)
    {
        levelString = null;
        levelName = null;

        string[] actFolders = GetSortedActFolders();
        if (actFolders.Length == 0)
        {
            Debug.LogWarning("No act folders found.");
            return false;
        }

        actIndex = ((actIndex % actFolders.Length) + actFolders.Length) % actFolders.Length;
        string selectedAct = actFolders[actIndex];

        string[] levelFiles = GetPlayableLevelFiles(selectedAct)
            .OrderBy(f => ParsePrefixNumber(f))
            .ToArray();

        if (levelFiles.Length == 0)
        {
            Debug.LogWarning($"No playable level files found in: {selectedAct}");
            return false;
        }

        levelIndex = ((levelIndex % levelFiles.Length) + levelFiles.Length) % levelFiles.Length;

        try
        {
            string fullPath = levelFiles[levelIndex];
            levelString = File.ReadAllText(fullPath);

            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            int dashIndex = fileName.IndexOf('-');

            levelName = dashIndex >= 0 && dashIndex < fileName.Length - 1
                ? fileName.Substring(dashIndex + 1)
                : fileName;

            return true;
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to read level file: {e.Message}");
            return false;
        }
    }
    #endregion
    [SerializeField] Button LevelSelectButton;
    [SerializeField] Transform createdItemsAnchor;
    [SerializeField] ScenePairSO gameScene;
    [SerializeField] Button actUp, actDown;

    [SerializeField] TMP_Text actCreditsText, actInfoText;

    HashSet<GameObject> createdItems = new();
    static int CurrentActSelection = 0;
    [Initialize(-99)]
    static void ResetActSelect() => CurrentActSelection = 0;
    Vector2 lastInput;
    private void Update()
    {
        Vector2 input = GenericInput.Move;
        if (lastInput.x.Absolute() <= 0.4f && input.x.Absolute() > 0.4f && input.y.Absolute() < 0.4f)
        {
            CurrentActSelection += input.x.SignInt();
            SelectAct(CurrentActSelection);
        }
        lastInput = input;
    }
    private void SelectUp()
    {
        CurrentActSelection += 1;
        SelectAct(CurrentActSelection);
    }
    private void SelectDown()
    {
        CurrentActSelection -= 1;
        SelectAct(CurrentActSelection);
    }
    private void Start()
    {
        IEnumerator CO_SelectAfterLoading()
        {
            yield return new WaitUntil(() => !SceneLoader.IsLoading);
            SelectAct(CurrentActSelection);
        }
        StartCoroutine(CO_SelectAfterLoading());
        actUp.BindSingleAction(SelectUp);
        actDown.BindSingleAction(SelectDown);
    }
    private void OnDestroy()
    {
        actUp.RemoveAllClickActions();
        actDown.RemoveAllClickActions();
    }
}