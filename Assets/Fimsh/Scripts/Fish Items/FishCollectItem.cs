using RinCore;
using UnityEngine;

public class FishCollectItem : MonoBehaviour, IFibsh
{
    private void OnEnable()
    {
        IFibsh.BindFibsh(this);
    }
    private void OnDisable()
    {
        IFibsh.ReleaseFibsh(this);
    }
    public bool TryCollect(FishPlayer p)
    {
        bool success = true;
        if (success)
        {
            FishCounter.CatchFish(1);
            if (FishTools.ActiveStageSettings.gamemode == FishTools.stageSettings.Gamemode.Arcade)
            {
                GeneralManager.AddScore(1d, false);
                GeneralManager.AddScoreAnalysisKey("Fibsh", 1d);
            }
        }
        return success;
    }
}
