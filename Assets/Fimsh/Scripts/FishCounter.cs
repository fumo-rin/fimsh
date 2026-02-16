using RinCore;
using TMPro;
using UnityEngine;

public class FishCounter : MonoBehaviour
{
    private static FishCounter instance;
    private static int FishRemaining = 0;
    private static int CurrentMaxFishCount;
    [SerializeField] Animator fishAnimator;
    [SerializeField] TMP_Text fishText;
    [SerializeField] string catchFishAnimKey = "CATCHFISH";
    private void Awake()
    {
        instance = this;
        StopSession(FishSessionEnd.Cancel);
    }
    public static void StartSession(int fishCounter, out WaitUntil wait)
    {
        FishRemaining = fishCounter;
        CurrentMaxFishCount = fishCounter;
        FishPopup.TriggerPopup($"Catch##{FishRemaining.ToString().Color(ColorHelper.PastelOrange)} Fishms!".ReplaceLineBreaks("##"));
        if (instance is FishCounter f && f.gameObject != null && f.gameObject.activeInHierarchy)
        {
            f.SetFishCountLeft(CurrentMaxFishCount.ToString());
        }
        wait = new(() => FishRemaining <= 0 && IFibsh.TotalFishItems <= 0);
    }
    public enum FishSessionEnd
    {
        CatchAll,
        MissCatch,
        Hazard,
        Cancel
    }
    public static void StopSession(FishSessionEnd reason, string flavour = "")
    {
        if (instance is FishCounter f && f.gameObject != null && f.gameObject.activeInHierarchy)
        {
            switch (reason)
            {
                case FishSessionEnd.CatchAll:
                    f.SetFishCountLeft("Caught All Fish!".Color(ColorHelper.PastelGreen));
                    break;
                case FishSessionEnd.MissCatch:
                    f.SetFishCountLeft($"Missed {(flavour == "" ? "a fish" : flavour)}!".Color(ColorHelper.PastelRed));
                    break;
                case FishSessionEnd.Hazard:
                    f.SetFishCountLeft($"Fished up {flavour}!".Color(ColorHelper.PastelRed));
                    break;
                case FishSessionEnd.Cancel:
                    f.SetFishCountLeft("");
                    break;
                default:
                    f.SetFishCountLeft("");
                    break;
            }
        }
    }
    private void SetFishCountLeft(string text)
    {
        if (fishText != null)
            fishText.text = text.ToString();
    }
    public static void CatchFish()
    {
        if (instance == null)
            return;

        FishRemaining--;
        FishRemaining = FishRemaining.Clamp(0, CurrentMaxFishCount);
        if (instance is FishCounter f && f.gameObject != null && f.gameObject.activeInHierarchy)
        {
            f.SetFishCountLeft(FishRemaining.ToString());
            f.fishAnimator.SetTrigger(f.catchFishAnimKey);
        }
    }
}
