using RinCore;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishMapperTools : MonoBehaviour
{
    [SerializeField] Button startButton, importButton, exportButton;

    private void Start()
    {
        startButton.BindSingleAction(StartFishmapperStage);
        importButton.BindSingleAction(FishMapper.Import);
        exportButton.BindSingleAction(() => FishMapper.Export(out _, out _));
    }

    [Serializable]
    private class DTOListWrapper
    {
        public List<FishNode.FishRunDataDTO> list;
    }

    public static void StartStage(string stageString)
    {
        if (string.IsNullOrEmpty(stageString)) return;
        stageString.TryFromJson(out DTOListWrapper wrapper, true);
        if (wrapper?.list == null) { Debug.LogError("Failed To Load Stage"); return; }
        FishTools.StartStage(wrapper.list);
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
