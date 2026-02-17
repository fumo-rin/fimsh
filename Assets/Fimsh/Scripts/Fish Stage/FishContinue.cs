using UnityEngine;
using System.Collections.Generic;
using RinCore;
using UnityEngine.UI;
using System.Collections;
using NUnit.Framework.Constraints;
using System.Xml;
public class FishContinue : MonoBehaviour
{
    static FishContinue instance;
    [SerializeField] ScenePairSO fishMapperStageLoadCheck;
    private void Awake()
    {
        instance = this;
    }
    private static List<FishNode.FishRunData> cachedStage;
    public static List<FishNode.FishRunData> LastStage
    {
        get
        {
            return cachedStage;
        }
        set
        {
            Debug.Log("Set Continue Stage : " + LastStage);
            cachedStage = value;
        }
    }
    [SerializeField] ScenePairSO stageSelect;
    [SerializeField] Button continueB, menuB;
    [SerializeField] Transform hideAnchor;
    public static void Reload()
    {
        if (instance is not FishContinue n || n.gameObject == null)
        {
            return;
        }
        if (LastStage == null)
        {
            n.stageSelect.Load();
            return;
        }
        FishTools.StartStage(LastStage, new(true));
        Hide();
    }
    public static void Show(float delay = 0f)
    {
        if (instance is not FishContinue n || n.gameObject == null)
        {
            return;
        }
        if (n.fishMapperStageLoadCheck.IsLastLoaded)
        {
            return;
        }
        IEnumerator CO_Show(float delay)
        {
            yield return delay.WaitForSeconds();
            n.hideAnchor.gameObject.SetActive(true);
        }
        n.StartCoroutine(CO_Show(delay));
    }
    public static void Hide()
    {
        if (instance is not FishContinue n || n.gameObject == null)
        {
            return;
        }
        n.hideAnchor.gameObject.SetActive(false);
    }
    private static void Menu()
    {
        if (instance is not FishContinue n || n.gameObject == null)
        {
            return;
        }
        n.stageSelect.Load();
        Hide();
    }
    private void Start()
    {
        hideAnchor.gameObject.SetActive(false);
        continueB.BindSingleAction(Reload);
        menuB.BindSingleAction(Menu);
    }
    private void OnDestroy()
    {
        menuB.RemoveAllClickActions();
        continueB.RemoveAllClickActions();
    }
}
