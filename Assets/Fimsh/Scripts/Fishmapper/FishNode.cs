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
        FishItem
    }

    [System.Serializable]
    public abstract class FishRunData
    {
        public FishNodeType nodeType;
        public int order;
        public bool runSeperately;

        public abstract int FishValue { get; }
        public abstract IEnumerator RunData();
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
        switch (dto.nodeType)
        {
            case FishNodeType.FishItem:
                dto.jsonData.TryFromJson(out FishItemNode.FishItemRunData r, true);
                return r;

            default:
                Debug.LogError($"Unsupported node type: {dto.nodeType}");
                return null;
        }
    }

    [SerializeField] Button nodeButton, moveUpB, moveDownB, DeleteB;
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
    }

    private void Update()
    {
        if (baseData != null)
            baseData.order = (int)(transform.localPosition.y * 100f);
    }

    private void OnDestroy()
    {
        nodeButton.RemoveAllClickActions();
        moveUpB.RemoveAllClickActions();
        moveDownB.RemoveAllClickActions();
        DeleteB.RemoveAllClickActions();
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
