using RinCore;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishMapperTools : MonoBehaviour
{
    [SerializeField] Button startButton, importButton, exportButton;
    TMP_Text startText;
    private void Awake()
    {
        if (startButton != null)
            startText = startButton.GetComponentInChildren<TMP_Text>();
    }
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
    private void Update()
    {
        if (startText is TMP_Text buttonText and not null)
        {
            buttonText.text = FishTools.IsStageRunning ? "Stop" : "Start";
        }
    }
    private static void StageEditorStartStage(string stageString)
    {
        if (string.IsNullOrEmpty(stageString)) return;
        stageString.TryFromJson(out DTOListWrapper wrapper, true);
        if (wrapper?.list == null) { Debug.LogError("Failed To Load Stage"); return; }
        FishTools.StartStage(wrapper.list, new()
        {
            dialogueStack = null,
            forceActivateNodes = false
        });
    }

    public void StartFishmapperStage()
    {
        if (FishTools.IsStageRunning)
        {
            FishTools.StopStage();
            return;
        }
        FishMapper.Export(out _, out string json);
        StageEditorStartStage(json);
    }

    private void OnDestroy()
    {
        startButton.RemoveAllClickActions();
        importButton.RemoveAllClickActions();
        exportButton.RemoveAllClickActions();
    }
}
