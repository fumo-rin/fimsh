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
            FishCounter.CatchFish();
        }
        return success;
    }
}
