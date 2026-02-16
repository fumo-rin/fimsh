using RinCore;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FishItemNode : FishNode
{
    public override string BuildNodeName()
    {
        return $"{fishData.action.ToSpacedString().WordChop(2)}: {fishData.repeats}x({fishData.startX.Multiply(100f).ToString("F0")}-{fishData.endX.Multiply(100f).ToString("F0")})";
    }
    public override IEnumerator DrawNode(FishProperties propDrawer)
    {
        FishPropSlider addedDelaySlider = propDrawer.StartSlider();
        addedDelaySlider.SliderGet.SetValues(fishData.addedPostDelay, 10f, 0f);
        addedDelaySlider.SetTitle("Added Post Delay");

        FishPropSlider startXSlider = propDrawer.StartSlider();
        startXSlider.SliderGet.SetValues(fishData.startX, 1f, 0f);
        startXSlider.SetTitle("Start X");

        FishPropSlider endXSlider = propDrawer.StartSlider();
        endXSlider.SliderGet.SetValues(fishData.endX, 1f, 0f);
        endXSlider.SetTitle("End X");

        FishPropSlider fishLerpSlider = propDrawer.StartSlider();
        fishLerpSlider.SliderGet.SetValues(fishData.fishLerpDuration, 10f, 0.75f);
        fishLerpSlider.SetTitle("Fish Duration");

        FishPropSlider repeatCountSlider = propDrawer.StartSlider();
        repeatCountSlider.SliderGet.SetValuesInt(fishData.repeats, 10, 1);
        repeatCountSlider.SetTitle("Fish Spawn Count");

        FishPropSlider repeatDelaySlider = propDrawer.StartSlider();
        repeatDelaySlider.SliderGet.SetValues(fishData.delayBetweenSpawns, 1.5f, 0.05f);
        repeatDelaySlider.SetTitle("Fish Repeat Spawn Delay");

        FishEnumDropdown typeDropdown = propDrawer.StartEnumDropdown();
        typeDropdown.BindEnum(fishData.action);
        typeDropdown.SetTitle("Spawn Pattern");

        FishPropToggle runSeperatelyProp = propDrawer.StartToggle();
        runSeperatelyProp.SetTitle("Run Seperately");
        runSeperatelyProp.PropGet.isOn = fishData.runSeperately;

        while (IsSelected)
        {
            fishData.addedPostDelay = addedDelaySlider.SliderGet.value;
            addedDelaySlider.SetValueText(fishData.addedPostDelay.ToString("F2"));

            fishData.startX = startXSlider.SliderGet.value;
            startXSlider.SetValueText(fishData.startX.ToString("F2"));

            fishData.endX = endXSlider.SliderGet.value;
            endXSlider.SetValueText(fishData.endX.ToString("F2"));

            fishData.fishLerpDuration = fishLerpSlider.SliderGet.value;
            fishLerpSlider.SetValueText(fishData.fishLerpDuration.ToString("F2"));

            fishData.repeats = ((int)repeatCountSlider.SliderGet.value);
            repeatCountSlider.SetValueText(fishData.repeats.ToString("F0"));

            fishData.delayBetweenSpawns = repeatDelaySlider.SliderGet.value;
            repeatDelaySlider.SetValueText(fishData.delayBetweenSpawns.ToString("F2"));

            fishData.action = typeDropdown.GetValue<FishNode.FishRunData.FishNodeAction>();

            fishData.runSeperately = runSeperatelyProp.PropGet.isOn;

            NodeName.text = BuildNodeName();
            yield return null;
        }
    }
}
