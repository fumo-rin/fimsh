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
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FishStageSelector : MonoBehaviour
{
    #region Manual Prop Drawer
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ManualLevelEntry))]
    public class ManualLevelEntryDrawer : PropertyDrawer
    {
        const float VerticalSpacing = 2f;

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            float height = 0f;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true);
                height += VerticalSpacing;

                enterChildren = false;
            }

            return height;
        }

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            float y = position.y;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                float height = EditorGUI.GetPropertyHeight(iterator, true);

                Rect rect = new(
                    position.x,
                    y,
                    position.width,
                    height);

                EditorGUI.PropertyField(rect, iterator, true);

                y += height + VerticalSpacing;

                enterChildren = false;
            }

            EditorGUI.EndProperty();
        }
    }
#endif
    #endregion
    static bool IsWebGL => GeneralManager.IsWebGL;
    #region Level Selector
    public void SelectAct(int index)
    {
        switch ((IsWebGL)
    )
        {
            case true:
                {
                    if (WebGlLevelCollection == null || WebGlLevelCollection.categories.Count == 0)
                        break;
                    int categoryCount = WebGlLevelCollection.categories.Count;
                    int safeIndex = ((index % categoryCount) + categoryCount) % categoryCount;
                    if (GetLevelsOfCategory(index, out List<ManualLevelEntry> manualLevels))
                    {
                        DestroyAll();
                        int iteration = 0;

                        foreach (var item in manualLevels)
                        {
                            Button b = CreateItem(LevelSelectButton);
                            if (b.GetComponentInChildren<TMP_Text>() is TMP_Text t)
                            {
                                t.text = $"{safeIndex + 1}-{iteration + 1}##{item.LevelName()}".ReplaceLineBreaks("##");
                            }
                            int currentLevel = iteration;

                            b.BindSingleAction(() =>
                            {
                                if (GetLevel(index, currentLevel, out var level))
                                {
                                    FishTools.SelectLevel(level.LevelData, new()
                                    {
                                        dialogueStack = null,
                                        forceActivateNodes = true,
                                        shouldDisplayLevelName = true,
                                        levelName = level.LevelName(),
                                        BombPointLoss = 0,
                                        gamemode = FishTools.stageSettings.Gamemode.StageSelect
                                    }, gameScene);
                                }
                            });
                            if (currentLevel == 0)
                                StartCoroutine(SelectNextFrame(b.gameObject));
                            iteration++;
                        }
                        if (GetCategory(index, out var category))
                        {
                            actText.text = category.categoryName;
                            actCreditsText.text = category.Credits.ReplaceLineBreaks("##");
                            actInfoText.text = category.Info.ReplaceLineBreaks("##");
                        }
                        else
                        {
                            actText.text = "Invalid Category";
                            actCreditsText.text = "";
                            actInfoText.text = "";
                        }
                    }
                    break;
                }
            case false:
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
                break;
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

    #region Manual Insert
    #region Field & tings
    [System.Serializable]
    public class ManualLevelEntry
    {
        public string LevelName()
        {
            if (levelFile == null)
                return string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(levelFile.name);
            int dashIndex = fileName.IndexOf('-');
            return dashIndex >= 0 && dashIndex + 1 < fileName.Length
                ? fileName[(dashIndex + 1)..].Trim()
                : fileName.Trim();
        }
        public TextAsset levelFile;
        public string LevelData =>
            levelFile != null
                ? levelFile.text
                : string.Empty;
    }
    [System.Serializable]
    public class ManualLevelCategory
    {
        public string categoryName;

        public TextAsset infoFile;
        public TextAsset creditsFile;

        public string Info =>
            infoFile != null
                ? infoFile.text
                : string.Empty;

        public string Credits =>
            creditsFile != null
                ? creditsFile.text
                : string.Empty;

        public List<ManualLevelEntry> levels = new();
    }

    [System.Serializable]
    public class ManualLevelCollection
    {
        public List<ManualLevelCategory> categories = new();
    }
    #endregion
    #region Get Features
    public bool GetCategory(int categoryIndex, out ManualLevelCategory category)
    {
        category = null;

        if (WebGlLevelCollection == null || WebGlLevelCollection.categories.Count == 0)
            return false;

        categoryIndex =
            ((categoryIndex % WebGlLevelCollection.categories.Count)
            + WebGlLevelCollection.categories.Count)
            % WebGlLevelCollection.categories.Count;

        category = WebGlLevelCollection.categories[categoryIndex];
        return true;
    }

    public bool GetLevelsOfCategory(
        int categoryIndex,
        out List<ManualLevelEntry> levels)
    {
        levels = null;

        if (!GetCategory(categoryIndex, out var category))
            return false;

        levels = category.levels;
        return true;
    }

    public bool GetLevel(
        int categoryIndex,
        int levelIndex,
        out ManualLevelEntry level)
    {
        level = null;

        if (!GetCategory(categoryIndex, out var category))
            return false;

        if (category.levels.Count == 0)
            return false;

        levelIndex =
            ((levelIndex % category.levels.Count)
            + category.levels.Count)
            % category.levels.Count;

        level = category.levels[levelIndex];
        return true;
    }
    #endregion
    [SerializeField] public ManualLevelCollection WebGlLevelCollection;
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