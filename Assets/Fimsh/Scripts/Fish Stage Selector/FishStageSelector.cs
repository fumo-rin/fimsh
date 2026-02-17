using RinCore;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FishStageSelector : MonoBehaviour
{
    #region Acts + Levels Finder
    [SerializeField] TMP_Text actText;
    private static string cachedActName = "";
    public static string CurrentAct
    {
        get
        {
            return cachedActName == "" ? "Select An Act" : cachedActName;
        }
    }
    private static string ActsRoot =>
        Path.Combine(Application.streamingAssetsPath, "Acts");
    public static bool GetLevelName(int actIndex, int levelIndex, out string levelName)
    {
        levelName = null;
        if (!Directory.Exists(ActsRoot))
        {
            Debug.LogWarning($"Acts directory not found: {ActsRoot}");
            return false;
        }
        string[] actFolders = Directory
            .GetDirectories(ActsRoot)
            .OrderBy(d => d)
            .ToArray();
        if (actFolders.Length == 0)
        {
            Debug.LogWarning("No act folders found.");
            return false;
        }
        actIndex = ((actIndex % actFolders.Length) + actFolders.Length) % actFolders.Length;
        string selectedAct = actFolders[actIndex];
        string[] levelFiles = Directory
            .GetFiles(selectedAct, "*.txt")
            .ToArray();

        if (levelFiles.Length == 0)
        {
            Debug.LogWarning($"No level files found in: {selectedAct}");
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
        if (dashIndex >= 0 && dashIndex + 1 < fileName.Length)
        {
            levelName = fileName.Substring(dashIndex + 1).Trim();
        }
        else
        {
            levelName = fileName.Trim();
        }

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
        levelNames = new List<string>();
        if (!Directory.Exists(ActsRoot))
        {
            Debug.LogWarning($"Acts directory not found: {ActsRoot}");
            return false;
        }

        string[] actFolders = Directory
            .GetDirectories(ActsRoot)
            .OrderBy(d => d)
            .ToArray();

        if (actFolders.Length == 0)
        {
            Debug.LogWarning("No act folders found.");
            return false;
        }
        actIndex = ((actIndex % actFolders.Length) + actFolders.Length) % actFolders.Length;

        string selectedAct = actFolders[actIndex];
        cachedActName = Path.GetFileName(selectedAct);

        string[] levelFiles = Directory
            .GetFiles(selectedAct, "*.txt")
            .OrderBy(f => f)
            .ToArray();

        if (levelFiles.Length == 0)
        {
            Debug.LogWarning($"No level files found in: {selectedAct}");
            return false;
        }

        levelNames = levelFiles.Select(f => new
        {
            Path = f,
            PrefixNumber = ParsePrefixNumber(f)
        })
            .OrderBy(x => x.PrefixNumber)
            .Select(x => Path.GetFileNameWithoutExtension(x.Path))
            .ToList();

        return true;
    }
    public static bool GetLevel(int actIndex, int levelIndex, out string levelString)
    {
        levelString = null;

        if (!Directory.Exists(ActsRoot))
        {
            Debug.LogWarning($"Acts directory not found: {ActsRoot}");
            return false;
        }

        string[] actFolders = Directory
            .GetDirectories(ActsRoot)
            .OrderBy(d => d)
            .ToArray();

        if (actFolders.Length == 0)
        {
            Debug.LogWarning("No act folders found.");
            return false;
        }

        actIndex = ((actIndex % actFolders.Length) + actFolders.Length) % actFolders.Length;

        string selectedAct = actFolders[actIndex];

        string[] levelFiles = Directory
            .GetFiles(selectedAct, "*.txt")
            .OrderBy(f => f)
            .ToArray();

        if (levelFiles.Length == 0)
        {
            Debug.LogWarning($"No level files found in: {selectedAct}");
            return false;
        }

        levelIndex = ((levelIndex % levelFiles.Length) + levelFiles.Length) % levelFiles.Length;

        try
        {
            levelString = File.ReadAllText(levelFiles[levelIndex]);
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
    HashSet<GameObject> createdItems = new();
    #region Level Selector
    public void SelectAct(int index)
    {
        Debug.Log("Building Act : " + index);
        if (GetLevelsOfAct(index, out List<string> levels))
        {
            DestroyAll();
            int iteration = 0;
            foreach (var item in levels)
            {
                Button b = CreateItem(LevelSelectButton);
                if (b.GetComponentInChildren<TMP_Text>() is TMP_Text t)
                {
                    string[] actFolders = Directory
                        .GetDirectories(ActsRoot)
                        .OrderBy(d => d)
                        .ToArray();
                    string actString = ((((index % actFolders.Length) + actFolders.Length) % actFolders.Length) + 1).ToString();
                    GetLevelName(index, iteration, out string levelName);
                    t.text = $"{(actString)}-{iteration + 1}##{levelName}".ReplaceLineBreaks("##");
                }
                int currentLevel = iteration;
                b.BindSingleAction(() => FindAndStartLevel(index, currentLevel));

                if (currentLevel == 0)
                {
                    IEnumerator CO_Select(GameObject b)
                    {
                        yield return null;
                        if (b != null)
                        {
                            Debug.Log("Selecting : " + b);
                            b.Select_WithEventSystem();
                        }
                    }
                    StartCoroutine(CO_Select(b.gameObject));
                }

                iteration = iteration + 1;
            }
            actText.text = CurrentAct;
        }
    }
    void FindAndStartLevel(int act, int level)
    {
        if (!GetLevel(act, level, out string levelString))
        {
            Debug.LogError($"Invalid Level : Act {act} Level {level}");
            return;
        }
        Debug.Log($"Finding Level : {act + 1}-{level + 1}...");
        SelectLevel(levelString, new()
        {
            dialogueStack = null,
            forceActivateNodes = true
        });
    }
    void SelectLevel(string s, FishTools.stageSettings settings)
    {
        if (!s.TryFromJson(out FishMapper.DTOListWrapper wrapper, true) || wrapper?.list == null)
        {
            Debug.LogError("Invalid Level: " + s.DecryptString());
            return;
        }
        foreach (var item in wrapper.list)
        {

        }
        SceneLoader.LoadScenePair(gameScene, () => FishTools.StartStage(wrapper.list, settings));
    }
    private void DestroyAll()
    {
        LevelSelectButton.gameObject.SetActive(false);
        if (createdItems != null && createdItems.Count > 0)
        {
            foreach (var item in createdItems.ToList())
            {
                if (item.GetComponent<Button>() is Button b)
                {
                    b.RemoveAllClickActions();
                }
                Destroy(item.gameObject);
            }
            createdItems.Clear();
        }
    }
    private T CreateItem<T>(T g) where T : MonoBehaviour
    {
        T item = Instantiate<T>(g, createdItemsAnchor);
        createdItems.Add(item.gameObject);
        item.gameObject.SetActive(true);
        return item;
    }
    #endregion

    static int CurrentActSelection = 0;
    Vector2 lastInput;
    private void Update()
    {
        Vector2 input = GenericInput.Move;
        if (lastInput.x.Absolute() <= 0.4f && input.x.Absolute() > 0.4f && input.y.Absolute() < 0.4f)
        {
            Debug.Log(input.x);
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