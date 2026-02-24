using RinCore;
using UnityEngine;

public class FishHazardItem : MonoBehaviour, IFibsh
{
    enum HazardCategory
    {
        PipeBomb,
        PlutoniumBarrel,
        MassiveLog
    }
    [SerializeField] HazardCategory category;
    private void OnEnable()
    {
        IFibsh.BindFibsh(this);
    }
    private void OnDisable()
    {
        IFibsh.ReleaseFibsh(this);
    }
    public static string WithIndefiniteArticle(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return word;

        char firstChar = char.ToLower(word[0]);

        if ("aeiou".IndexOf(firstChar) >= 0)
            return $"an {word}";

        string lower = word.ToLower();

        return $"a {word}";
    }
    static float nextBombTimeScoreLoss = 0f;
    public static void ResetState()
    {
        nextBombTimeScoreLoss = 0f;
    }
    public bool TryCollect(FishPlayer p)
    {
        GeneralManager.FunnyExplosion(new(transform.position, true) { scale = 2f });
        if (!FishTools.IsEditing)
        {
            switch (FishTools.ActiveStageSettings.gamemode)
            {
                case FishTools.stageSettings.Gamemode.StageSelect:
                    FishTools.StopStage();
                    FishContinue.Show(1f);
                    FishCounter.StopSession(FishCounter.FishSessionEnd.Hazard,
                        $"{WithIndefiniteArticle(category.ToSpacedString())}");
                    break;
                case FishTools.stageSettings.Gamemode.Arcade:
                    if (Time.time < nextBombTimeScoreLoss)
                        break;

                    double scoreToRemove = -((float)GeneralManager.actualScore).Min(10f);
                    GeneralManager.AddScoreAnalysisKey("Fibsh", scoreToRemove);
                    GeneralManager.AddScore(scoreToRemove, false);
                    nextBombTimeScoreLoss = Time.time + 0.333f;
                    break;
                default:
                    break;
            }
        }
        return true;
    }
}
