using RinCore;
using System.Collections;
using UnityEngine;

public class FishItemNode : FishNode
{
    public FishItemRunData fishData
    {
        get
        {
            if (baseData == null || baseData is not FishItemRunData)
                baseData = new FishItemRunData();

            return (FishItemRunData)baseData;
        }
    }
    public override string BuildNodeName()
    {
        return $"{fishData.action.ToSpacedString().WordChop(2)}: " +
               $"{fishData.repeats}x({fishData.xStart * 100f:F0}-{fishData.xEnd * 100f:F0})";
    }

    [System.Serializable]
    public class FishItemRunData : FishRunData
    {
        public enum FishNodeAction
        {
            None,
            SpawnFish,
            Pipebomb,
            Log,
        }

        public float addedPostDelay = 0f;
        public float xStart = 0.5f;
        public float xEnd = 0.5f;
        public float fishLerpDuration = 3.5f;
        public int repeats = 3;
        public float delayBetweenSpawns = 0.2f;
        public FishNodeAction action = FishNodeAction.SpawnFish;

        public FishItemRunData()
        {
            nodeType = FishNodeType.FishItem;
            action = FishNodeAction.SpawnFish;
        }

        public override IEnumerator RunData()
        {
            switch (action)
            {
                case FishNodeAction.SpawnFish:
                    yield return FishTools.SpawnFishSequence(FishTools.GetItem("0"), this);
                    break;

                case FishNodeAction.Pipebomb:
                    yield return FishTools.SpawnFishSequence(FishTools.GetItem("1"), this);
                    break;
                default:
                    yield return addedPostDelay.WaitForSeconds();
                    break;
            }
        }

        public override FishRunData Copy()
        {
            return new FishItemRunData
            {
                runSeperately = this.runSeperately,
                nodeType = FishNodeType.FishItem,
                action = action,
                order = order,
                addedPostDelay = addedPostDelay,
                delayBetweenSpawns = delayBetweenSpawns,
                xEnd = xEnd,
                fishLerpDuration = fishLerpDuration,
                repeats = repeats,
                xStart = xStart,
            };
        }

        public override int FishValue =>
            action == FishNodeAction.SpawnFish ? repeats : 0;
    }

    public override IEnumerator DrawNode(FishProperties propDrawer)
    {
        var addedDelaySlider = MakeFloatSlider(propDrawer, "Added Post Delay", fishData.addedPostDelay, 10f, 0f);
        var startXSlider = MakeFloatSlider(propDrawer, "Start X", fishData.xStart, 1f, 0f);
        var endXSlider = MakeFloatSlider(propDrawer, "End X", fishData.xEnd, 1f, 0f);
        var fishLerpSlider = MakeFloatSlider(propDrawer, "Fish Duration", fishData.fishLerpDuration, 10f, 0.75f);
        var repeatCountSlider = MakeIntSlider(propDrawer, "Fish Spawn Count", fishData.repeats, 10, 1);
        var repeatDelaySlider = MakeFloatSlider(propDrawer, "Fish Repeat Spawn Delay", fishData.delayBetweenSpawns, 1.5f, 0.05f);

        var typeDropdown = propDrawer.StartEnumDropdown();
        typeDropdown.BindEnum(fishData.action);
        typeDropdown.SetTitle("Spawn Pattern");

        var runSeperatelyProp = propDrawer.StartToggle();
        runSeperatelyProp.SetTitle("Run Separately");
        runSeperatelyProp.PropGet.isOn = fishData.runSeperately;

        var flipXButton = propDrawer.StartButton("Flip X", () => FlipX());
        void FlipX()
        {
            fishData.xEnd = 1f - fishData.xEnd;
            fishData.xStart = 1f - fishData.xStart;
            startXSlider.SliderGet.SetValueWithoutNotify(fishData.xStart);
            endXSlider.SliderGet.SetValueWithoutNotify(fishData.xEnd);
        }

        while (IsSelected)
        {
            if (flipXButton.WasPressedThisFrame)
            {
                NodeName.text = BuildNodeName();
                yield return null;
                continue;
            }
            BindFloat(addedDelaySlider, v => fishData.addedPostDelay = v);
            BindFloat(startXSlider, v => fishData.xStart = v);
            BindFloat(endXSlider, v => fishData.xEnd = v);
            BindFloat(fishLerpSlider, v => fishData.fishLerpDuration = v);
            BindInt(repeatCountSlider, v => fishData.repeats = v);
            BindFloat(repeatDelaySlider, v => fishData.delayBetweenSpawns = v);

            fishData.action = typeDropdown.GetValue<FishItemRunData.FishNodeAction>();
            fishData.runSeperately = runSeperatelyProp.PropGet.isOn;

            NodeName.text = BuildNodeName();
            yield return null;
        }
    }
}
