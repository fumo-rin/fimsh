using RinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FishMapperTools : MonoBehaviour
{
    [SerializeField] Button startButton, importButton, exportButton;
    private void Start()
    {
        startButton.BindSingleAction(() => StartFishmapperStage());
        importButton.BindSingleAction(() => FishMapper.Import());
        exportButton.BindSingleAction(() =>
        {
            foreach (var item in FishNode.SnapshotNodes)
            {

            }
            FishMapper.Export(out _, out _);
        });
    }
    public static void StartStage(string stageString)
    {
        if (stageString.TryFromJson(out List<FishNode.FishRunData> result))
        {
            FishTools.StartStage(result);
        }
        else
        {
            Debug.LogError("Failed To Load Stage");
        }
    }
    public static void StartFishmapperStage()
    {
        FishMapper.Export(out _, out string json);
        StartStage(json);
    }
    private void OnDestroy()
    {
        startButton.RemoveAllClickActions();
        importButton.RemoveAllClickActions();
        exportButton.RemoveAllClickActions();
    }
}
