using RinCore;
using System.Collections;
using UnityEngine;

public class PipeWallNode : FishNode
{
    public PipeBombWallData fishData
    {
        get
        {
            if (baseData == null || baseData is not PipeBombWallData)
                baseData = new PipeBombWallData();

            return (PipeBombWallData)baseData;
        }
    }
    public class PipeBombWallData : FishNode.FishRunData
    {
        public PipeBombWallData()
        {
            nodeType = FishNodeType.PipeWall;
        }
        public override int FishValue => 0;

        public float centeredPosition = 0.5f;
        public float wallSize = 2f;
        public bool gap = true;
        public float addedDelay = 0.35f;
        public float fishDuration = 2.25f;

        public override FishRunData Copy()
        {
            return new PipeBombWallData()
            {
                addedDelay = addedDelay,
                centeredPosition = centeredPosition,
                fishDuration = fishDuration,
                gap = gap,
                IsActive = true,
                order = order,
                runSeperately = runSeperately,
                wallSize = wallSize,
                nodeType = FishNodeType.PipeWall
            };
        }
        public override IEnumerator RunData()
        {
            const float spacing = 0.1f;

            float halfSize = wallSize * 0.5f;
            int count = Mathf.CeilToInt(wallSize / spacing);
            for (int index = 0; index <= count; index++)
            {
                float offset = -halfSize + index * spacing;
                if (gap && Mathf.Abs(offset) < 0.15f)
                    continue;
                float x01 = centeredPosition + offset;
                if (x01 < 0f || x01 > 1f)
                    continue;

                FishSpace.Map(0f, x01, out Vector3 start);
                FishSpace.Map(1f, x01, out Vector3 end);
                GameObject pipeBomb = FishTools.GetItem("1");
                FishTools.SpawnAndMoveItem(pipeBomb, new()
                {
                    fishLifetime = fishDuration,
                    runSeperately = true,
                    x01Start = x01,
                    x01End = x01
                });
            }
            yield return addedDelay.WaitForSeconds();
        }
    }
    public override string BuildNodeName()
    {
        return $"Wall x{fishData.centeredPosition.Multiply(100f).ToString("F0")} : s{(fishData.wallSize.Multiply(100f).ToString("F0"))}";
    }
    public override IEnumerator DrawNode(FishProperties propDrawer)
    {
        var centerSlider = MakeFloatSlider(propDrawer, "Center X", fishData.centeredPosition, 1f, 0f);
        var sizeSlider = MakeFloatSlider(propDrawer, "Wall Size", fishData.wallSize, 2f, 0.1f);
        var gapToggle = propDrawer.StartToggle();
        gapToggle.SetTitle("Has Gap");
        gapToggle.PropGet.isOn = fishData.gap;
        var durationSlider = MakeFloatSlider(propDrawer, "Item Lifetime", fishData.fishDuration, 10f, 0.1f);
        var delaySlider = MakeFloatSlider(propDrawer, "Post Delay", fishData.addedDelay, 2f, 0f);

        var flipXButton = propDrawer.StartButton("Flip X", () => FlipX());
        void FlipX()
        {
            fishData.centeredPosition = 1f - fishData.centeredPosition;
            centerSlider.SliderGet.SetValueWithoutNotify(fishData.centeredPosition);
        }
        while (IsSelected)
        {
            if (flipXButton.WasPressedThisFrame)
            {
                NodeName.text = BuildNodeName();
                yield return null;
                continue;
            }
            BindFloat(centerSlider, v => fishData.centeredPosition = v);
            BindFloat(sizeSlider, v => fishData.wallSize = v);
            BindFloat(durationSlider, v => fishData.fishDuration = v);
            BindFloat(delaySlider, v => fishData.addedDelay = v);
            fishData.gap = gapToggle.PropGet.isOn;
            NodeName.text = BuildNodeName();
            yield return null;
        }
    }
}
