using RinCore;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class FishNode : MonoBehaviour
{
    #region Prop Builders
    protected FishPropSlider MakeFloatSlider(FishProperties drawer, string title,
        float value, float max, float min)
    {
        var s = drawer.StartSlider();
        s.SliderGet.SetValues(value, max, min);
        s.SetTitle(title);
        return s;
    }

    protected FishPropSlider MakeIntSlider(FishProperties drawer, string title,
        int value, int max, int min)
    {
        var s = drawer.StartSlider();
        s.SliderGet.SetValuesInt(value, max, min);
        s.SetTitle(title);
        return s;
    }

    protected void BindFloat(FishPropSlider slider, System.Action<float> setter)
    {
        float v = slider.SliderGet.value;
        setter(v);
        slider.SetValueText(v.ToString("F2"));
    }

    protected void BindInt(FishPropSlider slider, System.Action<int> setter)
    {
        int v = (int)slider.SliderGet.value;
        setter(v);
        slider.SetValueText(v.ToString("F0"));
    }
    #endregion
    public enum FishNodeType
    {
        FishItem,
        MusicNode,
        HazardSpammer
    }

    [System.Serializable]
    public abstract class FishRunData
    {
        public FishNodeType nodeType;
        public int order;
        public bool runSeperately;
        public bool IsActive = true;

        public abstract int FishValue { get; }
        public abstract IEnumerator RunData();
        public abstract FishRunData Copy();
    }

    [System.Serializable]
    public class FishRunDataDTO
    {
        public FishNodeType nodeType;
        public string jsonData;
    }

    public FishRunDataDTO ToDTO()
    {
        return new FishRunDataDTO
        {
            nodeType = baseData.nodeType,
            jsonData = baseData.ToJson(true, true)
        };
    }

    public static FishRunData FromDTO(FishRunDataDTO dto)
    {
        return dto.nodeType switch
        {
            FishNodeType.FishItem => dto.jsonData.TryFromJson<FishItemNode.FishItemRunData>(out var fish, true) ? fish : null,
            FishNodeType.MusicNode => dto.jsonData.TryFromJson<FishMusicNode.FishMusicData>(out var music, true) ? music : null,
            FishNodeType.HazardSpammer => dto.jsonData.TryFromJson<HazardSpammerNode.HazardSpammerData>(out var hazard, true) ? hazard : null,
            _ => ThrowUnsupportedNode(dto.nodeType)
        };
        FishRunData ThrowUnsupportedNode(FishNodeType type)
        {
            Debug.LogError($"Unsupported node type: {type}");
            return null;
        }
    }

    [SerializeField] Button nodeButton, moveUpB, moveDownB, DeleteB, copyB, toggleButton;
    public TMP_Text NodeName => nodeButton.GetComponentInChildren<TMP_Text>();

    static FishNode selectedNode;
    [SerializeReference]
    public FishRunData baseData;

    public abstract string BuildNodeName();

    public static IEnumerable<FishRunDataDTO> SnapshotNodes
    {
        get
        {
            foreach (var node in FindObjectsByType<FishNode>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None))
            {
                if (node.baseData != null)
                    yield return node.ToDTO();
            }
        }
    }

    public bool IsSelected => selectedNode == this && this != null;

    private void Start()
    {
        nodeButton.BindSingleAction(SelectNode);
        moveUpB.BindSingleAction(() => FishMapper.MoveNodeUp(this));
        moveDownB.BindSingleAction(() => FishMapper.MoveNodeDown(this));
        DeleteB.BindSingleAction(() =>
        {
            FishMapper.DeleteNode(this);
        });
        copyB.BindSingleAction(() =>
        {
            FishNode t = FishMapper.SpawnNodeFromPrefab(this);
            t.baseData = baseData.Copy();
            t.SelectNode();
        });
        toggleButton.BindSingleAction(TogglePress);
    }
    private void Update()
    {
        if (baseData != null)
            baseData.order = (int)(transform.localPosition.y * 100f);

    }
    public void SetActiveState(bool state)
    {
        baseData.IsActive = state;
        if (nodeButton.GetComponentInChildren<TMP_Text>() is TMP_Text t)
        {
            t.color = baseData.IsActive ? ColorHelper.PastelPurple : ColorHelper.Gray6;
        }

    }
    private void OnDestroy()
    {
        nodeButton.RemoveAllClickActions();
        moveUpB.RemoveAllClickActions();
        moveDownB.RemoveAllClickActions();
        DeleteB.RemoveAllClickActions();
        toggleButton.RemoveAllClickActions();
    }
    void TogglePress()
    {
        SetActiveState(!baseData.IsActive);
    }
    void SelectNode()
    {
        if (selectedNode != null && selectedNode.nodeButton != null)
            selectedNode.nodeButton.image.color = ColorHelper.White;

        selectedNode = this;
        FishProperties.DrawItem(this);
        nodeButton.image.color = ColorHelper.PastelGreen;
    }

    public abstract IEnumerator DrawNode(FishProperties propDrawer);
}
