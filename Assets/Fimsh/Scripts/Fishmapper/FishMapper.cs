using RinCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class FishMapper : MonoBehaviour
{
    #region Move
    public static void MoveNodeUp(FishNode node)
    {
        FishMapper m = instance;
        if (node == null || !m.spawnedObjects.Contains(node))
            return;

        int index = node.transform.GetSiblingIndex();
        if (index > 0)
        {
            node.transform.SetSiblingIndex(index - 1);
            node.fishData.order = node.transform.localPosition.y.Multiply(100f).ToInt();
        }
    }
    public static void MoveNodeDown(FishNode node)
    {
        FishMapper m = instance;
        if (node == null || !m.spawnedObjects.Contains(node))
            return;

        int index = node.transform.GetSiblingIndex();
        int maxIndex = node.transform.parent.childCount - 1;

        if (index < maxIndex)
        {
            node.transform.SetSiblingIndex(index + 1);
            node.fishData.order = node.transform.localPosition.y.Multiply(100f).ToInt();
        }
    }
    #endregion
    [SerializeField] FishNode prefab;
    [SerializeField] Transform nodeNest;
    [SerializeField] TMP_Dropdown nodeSpawner;
    static FishMapper instance;
    HashSet<FishNode> spawnedObjects = new();
    [SerializeField] List<FishNode> nodePrefabs;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        prefab.gameObject.SetActive(false);
        SetupDropdown();
    }
    #region Dropdown
    List<string> nodeNames = new();
    void SetupDropdown()
    {
        nodeSpawner.ClearOptions();

        nodeNames = nodePrefabs.Select(p => p.name.Replace("(Clone)", "").Trim()).ToList();

        var options = new List<string> { "Select Node..." };
        options.AddRange(nodeNames);

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

        StartNode(out FishNode n);
        n.gameObject.SetActive(true);
        spawnedObjects.Add(n);
        nodeSpawner.SetValueWithoutNotify(0);
        nodeSpawner.RefreshShownValue();
    }
    #endregion
    private void ClearSpawnedNodes()
    {
        foreach (var item in spawnedObjects.ToList())
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        spawnedObjects.Clear();
    }
    public static bool StartNode(out FishNode n)
    {
        n = null;
        FishNode prefab = instance.prefab;
        if (instance == null || instance.gameObject == null || prefab == null)
            return false;

        n = Instantiate(prefab, instance.nodeNest);
        n.gameObject.SetActive(true);
        instance.spawnedObjects.Add(n);
        n.fishData.order = n.transform.localPosition.y.Multiply(100f).ToInt();
        n.NodeName.text = n.BuildNodeName();
        return true;
    }
    public static void DeleteNode(FishNode n)
    {
        instance.spawnedObjects.Remove(n);
        Destroy(n.gameObject);
    }
    public static void Import()
    {
        if (TryPaste(out string s) && s.TryFromJson<List<FishNode.FishRunData>>(out List<FishNode.FishRunData> data))
        {
            data = data.OrderByDescending(x => x.order).ToList();

            if (instance is FishMapper f && f.gameObject != null)
            {
                f.ClearSpawnedNodes();

                foreach (var item in data)
                {
                    if (StartNode(out FishNode n))
                    {
                        n.fishData = item;
                        n.NodeName.text = n.BuildNodeName();
                    }
                }
            }
        }
    }
    public static void Export(out List<FishNode.FishRunData> data, out string stringStage)
    {
        data = new();
        foreach (var item in FishNode.SnapshotNodes.OrderBy(x => x.order))
        {
            data.Add(item);
        }
        stringStage = data.ToJson();
        Copy(stringStage);
    }
    public static void Copy(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogWarning("[ClipboardHelper] Attempted to copy null or empty string.");
            return;
        }

        GUIUtility.systemCopyBuffer = value;
    }
    public static string Paste()
    {
        return GUIUtility.systemCopyBuffer;
    }
    public static bool TryPaste(out string value)
    {
        value = GUIUtility.systemCopyBuffer;
        return !string.IsNullOrEmpty(value);
    }
}
