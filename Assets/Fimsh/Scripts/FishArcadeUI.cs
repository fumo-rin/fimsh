using RinCore;
using TMPro;
using UnityEngine;

public class FishArcadeUI : MonoBehaviour
{
    [SerializeField] TMP_Text scoreDisplay, highscoreDisplay;
    [SerializeField] Transform arcadeUIAnchor;
    private void Start()
    {
        GeneralManager.OnScoreUpdate += UpdateScore;
        GeneralManager.RequestScoreRefresh();
        arcadeUIAnchor.gameObject.SetActive(false);
    }
    private void UpdateScore(double score, double highScore)
    {
        if (FishTools.ActiveStageSettings.gamemode == FishTools.stageSettings.Gamemode.Arcade)
        {
            if (arcadeUIAnchor != null && arcadeUIAnchor.gameObject) arcadeUIAnchor.gameObject.SetActive(true);
        }
        scoreDisplay.text = score.ToString();
        highscoreDisplay.text = highScore.ToString();
    }
    private void OnDestroy()
    {
        GeneralManager.OnScoreUpdate += UpdateScore;
    }
}
