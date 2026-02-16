using RinCore;
using System.Collections;
using UnityEngine;

public class FishMusicNode : FishNode
{
    public FishMusicData fishData
    {
        get
        {
            if (baseData == null || baseData is not FishMusicData)
                baseData = new FishMusicData();

            return (FishMusicData)baseData;
        }
    }
    public override string BuildNodeName()
    {
        return $"{FishTools.GetMusic((int)fishData.action).TrackName.ClampLength(24)}";
    }
    public override IEnumerator DrawNode(FishProperties propDrawer)
    {
        var typeDropdown = propDrawer.StartEnumDropdown();
        typeDropdown.BindEnum(fishData.action);
        typeDropdown.SetTitle("Music Selection");
        while (IsSelected)
        {
            fishData.action = typeDropdown.GetValue<FishMusicData.MusicSelection>();
            NodeName.text = BuildNodeName();
            yield return null;
        }
    }

    [System.Serializable]
    public class FishMusicData : FishRunData
    {
        public enum MusicSelection
        {
            Menu = 0,
            Stage1 = 1,
            Stage2 = 2,
            Stage3 = 3,
            Stage4 = 4
        }
        public MusicSelection action;
        public FishMusicData()
        {
            nodeType = FishNodeType.MusicNode;
        }
        public override IEnumerator RunData()
        {
            FishTools.GetMusic((int)action).Play();
            yield break;
        }

        public override FishRunData Copy()
        {
            return new FishMusicData
            {
                runSeperately = this.runSeperately,
                nodeType = FishNodeType.MusicNode,
                action = action,
                order = order
            };
        }

        public override int FishValue => 0;
    }
}
