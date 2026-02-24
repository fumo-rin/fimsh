using RinCore;
using System.Collections;
using System.Xml;
using UnityEngine;

public class HazardSpammerNode : FishNode
{
    public HazardSpammerData fishData
    {
        get
        {
            if (baseData == null || baseData is not HazardSpammerData)
                baseData = new HazardSpammerData();

            return (HazardSpammerData)baseData;
        }
    }
    public override string BuildNodeName()
    {
        return $"{fishData.spawnItem.ToSpacedString().WordChop(2)} Spammer : {fishData.xStart.Multiply(100f).ToString("F0")}";
    }
    public override IEnumerator DrawNode(FishProperties propDrawer)
    {
        var durationSlider = MakeFloatSlider(propDrawer, "Duration", fishData.duration, 60f, 0.1f);
        var intervalSlider = MakeFloatSlider(propDrawer, "Interval", fishData.interval, 5f, 0.01f);
        var addedDelaySlider = MakeFloatSlider(propDrawer, "Added Post Delay", fishData.addedPostDelay, 10f, 0f);
        var lifetimeSlider = MakeFloatSlider(propDrawer, "Item Lifetime", fishData.fishLifetime, 10f, 0.1f);
        var startXSlider = MakeFloatSlider(propDrawer, "Start X", fishData.xStart, 1f, 0f);
        var endXSlider = MakeFloatSlider(propDrawer, "End X", fishData.xEnd, 1f, 0f);

        var typeDropdown = propDrawer.StartEnumDropdown();
        typeDropdown.BindEnum(fishData.spawnItem);
        typeDropdown.SetTitle("Hazard Selection");

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
            BindFloat(durationSlider, v => fishData.duration = v);
            BindFloat(intervalSlider, v => fishData.interval = v);
            BindFloat(addedDelaySlider, v => fishData.addedPostDelay = v);
            BindFloat(lifetimeSlider, v => fishData.fishLifetime = v);
            BindFloat(startXSlider, v => fishData.xStart = v);
            BindFloat(endXSlider, v => fishData.xEnd = v);

            fishData.spawnItem = typeDropdown.GetValue<HazardSpammerData.HazardItem>();

            NodeName.text = BuildNodeName();
            yield return null;
        }
    }

    [System.Serializable]
    public class HazardSpammerData : FishRunData
    {
        public enum HazardItem
        {
            PipeBomb,
            UraniumRod,
            UraniumBarrel,
        }
        public HazardItem spawnItem;
        public HazardSpammerData()
        {
            nodeType = FishNodeType.HazardSpammer;
        }
        public float duration = 10f;
        public float interval = 0.15f;
        public float addedPostDelay = 0f;
        public float fishLifetime = 3f;
        public float xStart = 0.5f;
        public float xEnd = 0.5f;
        public override IEnumerator RunData()
        {
            GameObject item = null;
            switch (spawnItem)
            {
                case HazardItem.PipeBomb:
                    item = FishTools.GetItem("1");
                    break;
                case HazardItem.UraniumRod:
                    item = FishTools.GetItem("2");
                    break;
                case HazardItem.UraniumBarrel:
                    item = FishTools.GetItem("3");
                    break;
                default:
                    item = FishTools.GetItem("1");
                    break;
            }
            IEnumerator CO_Spam()
            {
                for (float i = duration; i > 0f; i = i - interval)
                {
                    float xLerp = xStart.LerpUnclamped(xEnd, 1f - i / duration.Max(0.05f));
                    FishTools.SpawnAndMoveItem(item, new()
                    {
                        fishLifetime = fishLifetime,
                        runSeperately = true,
                        x01End = xLerp,
                        x01Start = xLerp
                    });
                    yield return interval.WaitForSeconds();
                }
            }
            StageRoutines.StartRoutine("STAGE_SPAWN", CO_Spam(), false);
            yield return addedPostDelay.WaitForSeconds();
        }

        public override FishRunData Copy()
        {
            return new HazardSpammerData
            {
                runSeperately = this.runSeperately,
                nodeType = FishNodeType.HazardSpammer,
                addedPostDelay = addedPostDelay,
                duration = this.duration,
                fishLifetime = this.fishLifetime,
                interval = this.interval,
                spawnItem = this.spawnItem,
                xEnd = this.xEnd,
                xStart = this.xStart,
                order = order
            };
        }

        public override int FishValue => 0;
    }
}
