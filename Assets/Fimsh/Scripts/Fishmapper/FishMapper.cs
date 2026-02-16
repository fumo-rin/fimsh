using RinCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishMapper : MonoBehaviour
{
    [SerializeField] Transform nodeNest;
    [SerializeField] TMP_Dropdown nodeSpawner;
    [SerializeField] List<FishNode> nodePrefabs;

    static FishMapper instance;
    HashSet<FishNode> spawnedObjects = new();

    void Awake() => instance = this;

    void Start()
    {
        SetupDropdown();
        foreach (var item in nodePrefabs)
            item.gameObject.SetActive(false);
    }

    #region Dropdown
    void SetupDropdown()
    {
        nodeSpawner.ClearOptions();
        var names = nodePrefabs.Select(p => p.name.Replace("(Clone)", "").Trim()).ToList();
        var options = new List<string> { "Select Node..." };
        options.AddRange(names);
        nodeSpawner.AddOptions(options);
        nodeSpawner.SetValueWithoutNotify(0);
        nodeSpawner.RefreshShownValue();
        nodeSpawner.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDropdownChanged(int index)
    {
        if (index <= 0) return;
        int prefabIndex = index - 1;
        if (prefabIndex < 0 || prefabIndex >= nodePrefabs.Count) return;
        SpawnNodeFromPrefab(nodePrefabs[prefabIndex]);
        nodeSpawner.SetValueWithoutNotify(0);
        nodeSpawner.RefreshShownValue();
    }
    #endregion

    #region Spawn / Delete
    public static FishNode SpawnNodeFromPrefab(FishNode prefab)
    {
        if (instance == null) return null;
        FishNode n = Instantiate(prefab, instance.nodeNest);
        n.gameObject.SetActive(true);
        instance.spawnedObjects.Add(n);
        if (n.baseData != null) n.baseData.order = (int)(n.transform.localPosition.y * 100f);
        n.NodeName.text = n.BuildNodeName();
        n.SetActiveState(true);
        return n;
    }

    void ClearSpawnedNodes()
    {
        foreach (var item in spawnedObjects.ToList())
            if (item != null) Destroy(item.gameObject);
        spawnedObjects.Clear();
    }

    public static void DeleteNode(FishNode n)
    {
        if (instance == null) return;
        instance.spawnedObjects.Remove(n);
        Destroy(n.gameObject);
    }
    #endregion

    #region Export / Import
    [Serializable]
    public class DTOListWrapper
    {
        public List<FishNode.FishRunDataDTO> list;
        public DTOListWrapper(List<FishNode.FishRunDataDTO> l) => list = l;
    }

    public static void Export(out List<FishNode.FishRunDataDTO> data, out string json)
    {
        data = FishNode.SnapshotNodes.OrderByDescending(d => d.nodeType).ToList();
        json = new DTOListWrapper(data).ToJson(true, true);
        Copy(json);
    }

    public static void Import()
    {
        if (!TryPaste(out string json)) return;
        json.TryFromJson(out DTOListWrapper wrapper, true);
        if (wrapper?.list == null) return;

        var ordered = wrapper.list
            .Select(FishNode.FromDTO)
            .Where(d => d != null)
            .OrderByDescending(d => d.order)
            .ToList();

        instance.ClearSpawnedNodes();

        foreach (var data in ordered)
        {
            var prefab = instance.nodePrefabs.FirstOrDefault(p => MatchesType(p, data.nodeType));
            if (prefab == null)
            {
                Debug.LogError($"No prefab found for {data.nodeType}");
                continue;
            }

            var node = SpawnNodeFromPrefab(prefab);
            node.baseData = data;
            node.NodeName.text = node.BuildNodeName();
        }
    }

    static bool MatchesType(FishNode prefab, FishNode.FishNodeType type) =>
        type switch
        {
            FishNode.FishNodeType.FishItem => prefab is FishItemNode,
            FishNode.FishNodeType.MusicNode => prefab is FishMusicNode,
            FishNode.FishNodeType.HazardSpammer => prefab is HazardSpammerNode,
            _ => false
        };

    #endregion

    #region Clipboard
    public static void Copy(string value)
    {
        if (!string.IsNullOrEmpty(value))
            GUIUtility.systemCopyBuffer = value;
    }

    public static bool TryPaste(out string value)
    {
        value = GUIUtility.systemCopyBuffer;
        return !string.IsNullOrEmpty(value);
    }
    #endregion

    #region Move
    public static void MoveNodeUp(FishNode node)
    {
        if (!IsValidNode(node)) return;
        int index = node.transform.GetSiblingIndex();
        if (index > 0) node.transform.SetSiblingIndex(index - 1);
        if (node.baseData != null) node.baseData.order = (int)(node.transform.localPosition.y * 100f);
    }

    public static void MoveNodeDown(FishNode node)
    {
        if (!IsValidNode(node)) return;
        int index = node.transform.GetSiblingIndex();
        int maxIndex = node.transform.parent.childCount - 1;
        if (index < maxIndex) node.transform.SetSiblingIndex(index + 1);
        if (node.baseData != null) node.baseData.order = (int)(node.transform.localPosition.y * 100f);
    }

    static bool IsValidNode(FishNode node) => instance != null && node != null && instance.spawnedObjects.Contains(node);
    #endregion
}
