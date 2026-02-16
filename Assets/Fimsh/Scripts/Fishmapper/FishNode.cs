using RinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class FishNode : MonoBehaviour
{
    [System.Serializable]
    public class FishRunData
    {
        public enum FishNodeAction
        {
            None,
            SpawnFish,
            Pipebomb,
            Log,
        }
        public float addedPostDelay = 0f;
        public float startX = 0.5f;
        public float endX = 0.5f;
        public float fishLerpDuration = 3.5f;
        public int repeats = 3;
        public float delayBetweenSpawns = 0.2f;
        public FishNodeAction action = FishNodeAction.None;
        public int order;
        public bool runSeperately;
        public IEnumerator RunNode()
        {
            switch (action)
            {
                case FishNodeAction.None:
                    yield break;
                case FishNodeAction.SpawnFish:
                    yield return FishTools.SpawnFishSequence(FishTools.GetItem("0"), this);
                    yield break;
                case FishNodeAction.Pipebomb:
                    yield return FishTools.SpawnFishSequence(FishTools.GetItem("1"), this);
                    yield break;
                default:
                    break;
            }
        }
        public int FishValue => action == FishNodeAction.SpawnFish ? repeats : 0;
    }
    [SerializeField] Button nodeButton, moveUpB, moveDownB, CopyB, DeleteB;
    public TMP_Text NodeName => nodeButton.GetComponentInChildren<TMP_Text>();
    static FishNode selectedNode;
    public FishRunData fishData;
    public abstract string BuildNodeName();
    public static IEnumerable<FishRunData> SnapshotNodes
    {
        get
        {
            foreach (var item in FindObjectsByType<FishNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .ToList())
            {
                {
                    yield return item.fishData;
                }
            }
        }
    }
    public bool IsSelected => IsNodeSelected(this);
    private static bool IsNodeSelected(FishNode n)
    {
        return selectedNode == n && n != null;
    }
    private void Start()
    {
        nodeButton.BindSingleAction(SelectNode);
        moveUpB.BindSingleAction(() => FishMapper.MoveNodeUp(this));
        moveDownB.BindSingleAction(() => FishMapper.MoveNodeDown(this));
        CopyB.BindSingleAction(() =>
        {
            FishMapper.StartNode(out FishNode n);
            n.CopyNode(this);
        });
        DeleteB.BindSingleAction(() =>
        {
            FishMapper.DeleteNode(this);
        });
    }
    private void Update()
    {
        this.fishData.order = ((int)transform.localPosition.y.Multiply(100f));
    }
    private void CopyNode(FishNode other)
    {
        fishData.endX = other.fishData.endX;
        fishData.startX = other.fishData.startX;
        fishData.fishLerpDuration = other.fishData.fishLerpDuration;
        fishData.addedPostDelay = other.fishData.addedPostDelay;
        fishData.action = other.fishData.action;
        fishData.addedPostDelay = other.fishData.addedPostDelay;
        fishData.delayBetweenSpawns = other.fishData.delayBetweenSpawns;
        fishData.repeats = other.fishData.repeats;
        fishData.runSeperately = other.fishData.runSeperately;
        other.NodeName.text = other.BuildNodeName();
    }
    private void OnDestroy()
    {
        nodeButton.RemoveAllClickActions();
        moveUpB.RemoveAllClickActions();
        moveDownB.RemoveAllClickActions();
        CopyB.RemoveAllClickActions();
        DeleteB.RemoveAllClickActions();
    }
    void SelectNode()
    {
        UnselectNode();
        selectedNode = this;
        FishProperties.DrawItem(this);
        selectedNode.nodeButton.image.color = ColorHelper.PastelGreen;
    }
    static void UnselectNode()
    {
        if (selectedNode is FishNode f && f != null && f.gameObject != null)
        {
            f.nodeButton.image.color = ColorHelper.White;
        }
        selectedNode = null;
    }
    public abstract IEnumerator DrawNode(FishProperties propDrawer);
}